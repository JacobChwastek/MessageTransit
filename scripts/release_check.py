#!/usr/bin/env python3
"""Calculate the next release version, inspect candidate packages, and smoke-test them in a clean consumer."""

import argparse
import datetime
import functools
import hashlib
import json
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile
from xml.sax.saxutils import escape


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "release" / "packages.json"
SMOKE_SOURCES = ROOT / "scripts" / "release_smoke"
SYMBOLS_TOOL = ROOT / "scripts" / "release_symbols" / "bin" / "Release" / "net10.0" / "ReleaseSymbols.dll"

PROJECT_URL = "https://github.com/JacobChwastek/MessageTransit"
REPOSITORY_URL = PROJECT_URL + ".git"
REQUIRED_AUTHOR = "Jacob Chwastek"
LICENSES = {"Apache-2.0", "Apache-2.0 AND MIT"}
REQUIRED_FILES = ("LICENSE", "NOTICE", "COPYRIGHT", "NuGet.README.md")
TARGET_FRAMEWORK = "net10.0"
# files that credit the upstream project by name
ATTRIBUTION_FILES = {"LICENSE", "NOTICE", "COPYRIGHT", "THIRD-PARTY-NOTICES", "NuGet.README.md"}
# Consumers audit the graph resolved from the candidate packages and also fail when advisory data is unavailable.
CONSUMER_PROPS = """<Project>
  <PropertyGroup>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>low</NuGetAuditLevel>
    <WarningsAsErrors>NU1900;NU1903;NU1904;NU1905</WarningsAsErrors>
  </PropertyGroup>
{accepted}</Project>
"""
BUILD_ERROR = re.compile(r"\berror [A-Z]+[0-9]+\b")
AUDIT_ERROR = re.compile(r"\berror NU190[0-9]\b")
AUDIT_CODE = re.compile(r"\bNU190[0-9]\b")
# only these files may set the shared NuGet audit properties
AUDIT_SETTINGS_FILES = {"Directory.Build.props", "scripts/release_symbols/Directory.Build.props"}
AUDIT_PROPERTIES = {"NuGetAudit", "NuGetAuditMode", "NuGetAuditLevel"}
UPSTREAM_NAMES = [name.encode(encoding) for name in ("masstransit", "MassTransit", "MASSTRANSIT", "massTransit") for encoding in ("utf-8", "utf-16-le")]


def public_key_token(key_file):
    """Strong-name public key token of a CAPI private key blob, as recorded in signed assembly identities."""
    data = key_file.read_bytes()
    bit_length, exponent = struct.unpack_from("<II", data, 12)
    sign_algorithm = 0x2400
    public_key = struct.pack("<BBHI4sII", 6, 2, 0, sign_algorithm, b"RSA1", bit_length, exponent) + data[20:20 + bit_length // 8]
    blob = struct.pack("<III", sign_algorithm, 0x8004, len(public_key)) + public_key
    return hashlib.sha1(blob).digest()[-8:][::-1].hex()


FORK_TOKEN = public_key_token(ROOT / "MessageTransit.snk")


def load_manifest():
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    publish, held = manifest["publish"], manifest["held"]
    if len(set(publish)) != len(publish) or set(publish) & set(held):
        raise SystemExit("release/packages.json lists a package twice or as both published and held")
    return publish, held


def resolve_version(arguments):
    """Keep the original command as an alias of the single release version calculator."""
    command = [sys.executable, str(ROOT / "scripts/release_version.py"),
               "--channel", arguments.channel, "--bump", arguments.bump]
    if arguments.version:
        command += ["--version", arguments.version]
    subprocess.run(command, check=True)


def local_name(element):
    return element.tag.rsplit("}", 1)[-1]


def child(metadata, name):
    return next((element for element in metadata if local_name(element) == name), None)


@functools.cache
def source_packages():
    """Expected assets and dependencies from the projects being packed, including central versions."""
    central = {item.get("Include").casefold(): item.get("Version")
               for item in ET.parse(ROOT / "Directory.Packages.props").iter("PackageVersion")}
    projects = {}
    for path in sorted((ROOT / "src").rglob("*.csproj")):
        project = ET.parse(path).getroot()
        if (project.findtext(".//IsPackable") or "").casefold() == "false":
            continue
        package = project.findtext(".//PackageId") or path.stem
        projects[path.resolve()] = (package, project)
    result = {}
    for path, (package, project) in projects.items():
        dependencies = {}
        for reference in project.iter("ProjectReference"):
            if (reference.get("PrivateAssets") or reference.findtext("PrivateAssets") or "").casefold() == "all":
                continue
            referenced_path = (path.parent / reference.get("Include").replace("\\", "/")).resolve()
            if referenced_path not in projects:
                raise ValueError(f"Unknown packaged project reference: {referenced_path}")
            dependencies[projects[referenced_path][0]] = None
        for reference in project.iter("PackageReference"):
            name = reference.get("Include")
            if not name or (reference.get("PrivateAssets") or reference.findtext("PrivateAssets") or "").casefold() == "all":
                continue
            dependencies[name] = reference.get("Version") or central[name.casefold()]
        assembly = project.findtext(".//AssemblyName") or path.stem
        asset = f"analyzers/dotnet/cs/{assembly}.dll" if package == "MessageTransit.Analyzers" else f"lib/{TARGET_FRAMEWORK}/{assembly}.dll"
        signed = (project.findtext(".//SignAssembly") or "").casefold() == "true" or any(
            item.get("Project", "").replace("\\", "/").endswith("/signing.props") for item in project.iter("Import"))
        result[package] = {"asset": asset, "assembly": assembly, "dependencies": dependencies,
                           "token": FORK_TOKEN if signed else ""}
    return result


def inspect_package(path, version, commit, publish, failures):
    package_id = path.name.removesuffix(f".{version}.nupkg")
    problems = []
    with zipfile.ZipFile(path) as archive:
        names = archive.namelist()
        nuspec_name = next(name for name in names if name.endswith(".nuspec") and "/" not in name)
        metadata = next(element for element in ET.fromstring(archive.read(nuspec_name)) if local_name(element) == "metadata")
        text = lambda name: (child(metadata, name).text or "").strip() if child(metadata, name) is not None else ""

        if text("id") != package_id:
            problems.append(f"nuspec id is {text('id')}")
        if text("version") != version:
            problems.append(f"nuspec version is {text('version')}")
        if REQUIRED_AUTHOR not in text("authors"):
            problems.append(f"authors '{text('authors')}' omit {REQUIRED_AUTHOR}")
        license_element = child(metadata, "license")
        if license_element is None or license_element.get("type") != "expression" or text("license") not in LICENSES:
            problems.append(f"license is '{text('license')}'")
        if text("projectUrl") != PROJECT_URL:
            problems.append(f"projectUrl is '{text('projectUrl')}'")
        if child(metadata, "icon") is not None or child(metadata, "iconUrl") is not None:
            problems.append("declares an icon")
        if text("readme") != "NuGet.README.md":
            problems.append(f"readme is '{text('readme')}'")
        repository = child(metadata, "repository")
        if repository is None or repository.get("url") != REPOSITORY_URL or repository.get("commit") != commit:
            problems.append(f"repository is {None if repository is None else repository.attrib}")

        for required in REQUIRED_FILES + (("THIRD-PARTY-NOTICES",) if "MIT" in text("license") else ()):
            if required not in names:
                problems.append(f"missing {required}")
            elif archive.read(required) != (ROOT / required).read_bytes():
                problems.append(f"{required} differs from the checkout file")

        expected = source_packages().get(package_id)
        if expected is None:
            problems.append("has no corresponding packaged source project")
        elif expected["asset"] not in names:
            problems.append(f"missing assembly {expected['asset']}")
        if expected is not None:
            extra_assemblies = sorted(name for name in names if name.endswith(".dll") and name != expected["asset"])
            if extra_assemblies:
                problems.append(f"unexpected assemblies: {', '.join(extra_assemblies)}")

        frameworks = {name.split("/")[1] for name in names if name.startswith(("lib/", "ref/")) and name.count("/") >= 2}
        analyzers = [name for name in names if name.startswith("analyzers/")]
        if frameworks - {TARGET_FRAMEWORK}:
            problems.append(f"framework assets for {sorted(frameworks - {TARGET_FRAMEWORK})}")
        if not frameworks and not analyzers:
            problems.append("contains no assemblies")
        if any(not name.startswith("analyzers/dotnet/cs/") for name in analyzers):
            problems.append("analyzer assets outside analyzers/dotnet/cs")

        dependencies = []
        group_frameworks = set()
        dependency_groups = child(metadata, "dependencies")
        for element in ([] if dependency_groups is None else dependency_groups):
            if local_name(element) == "group":
                group_frameworks.add(element.get("targetFramework"))
                dependencies.extend((dependency.get("id", ""), dependency.get("version", "")) for dependency in element
                                    if local_name(dependency) == "dependency")
            elif local_name(element) == "dependency":
                dependencies.append((element.get("id", ""), element.get("version", "")))
        # the analyzer package targets .NET Standard 2.0 for the compiler host and declares an empty group for it
        allowed_groups = {TARGET_FRAMEWORK, ".NETStandard2.0"} if analyzers and not frameworks else {TARGET_FRAMEWORK}
        if group_frameworks - allowed_groups:
            problems.append(f"dependency groups for {sorted(group_frameworks - allowed_groups)}")
        if analyzers and not frameworks and dependencies:
            problems.append("the analyzer package declares dependencies")
        publish_ids = {package.casefold() for package in publish}
        actual_dependencies = {name.casefold(): value for name, value in dependencies}
        for dependency_id, dependency_version in (expected or {}).get("dependencies", {}).items():
            actual = actual_dependencies.get(dependency_id.casefold())
            required_version = dependency_version or version
            if actual is None:
                problems.append(f"missing dependency {dependency_id}")
            elif actual not in (required_version, f"[{required_version}, )", f"[{required_version}]"):
                problems.append(f"dependency {dependency_id} is {actual}, expected {required_version}")
        for dependency_id, dependency_version in dependencies:
            if dependency_id.lower().startswith("masstransit"):
                problems.append(f"depends on {dependency_id}")
            elif dependency_id.casefold().startswith("messagetransit"):
                if dependency_version not in (version, f"[{version}, )", f"[{version}]"):
                    problems.append(f"depends on {dependency_id} {dependency_version}")
                if package_id.casefold() in publish_ids and dependency_id.casefold() not in publish_ids:
                    problems.append(f"depends on held package {dependency_id}")

        for name in names:
            if Path(name).name in ATTRIBUTION_FILES or not name.endswith((".dll", ".xml", ".nuspec", ".json", ".props", ".targets")):
                continue
            content = archive.read(name)
            if any(upstream in content for upstream in UPSTREAM_NAMES):
                problems.append(f"{name} contains the upstream product name")

    failures.extend(f"{package_id}: {problem}" for problem in problems)
    return {
        "id": package_id,
        "license": text("license"),
        "frameworks": sorted(frameworks) or ["analyzer"],
        "dependencies": sorted(f"{dependency_id} {dependency_version}" for dependency_id, dependency_version in dependencies if dependency_id.startswith("MessageTransit")),
        "external_dependencies": sorted(f"{name} {value}" for name, value in dependencies if not name.casefold().startswith("messagetransit")),
        "asset": None if expected is None else expected["asset"],
        "problems": problems,
    }


def inspect_symbols(packages, results, commit, tool, failures):
    """Validate each expected managed DLL and its embedded symbols, never trust nuspec provenance alone."""
    if not tool.is_file():
        failures.append(f"Source Link: build scripts/release_symbols/ReleaseSymbols.csproj first; missing {tool}")
        return []
    with tempfile.TemporaryDirectory(prefix="messagetransit-symbols-") as directory:
        extracted = {}
        for result in results:
            if not result["asset"]:
                continue
            with zipfile.ZipFile(packages[result["id"]]) as archive:
                if result["asset"] not in archive.namelist():
                    continue
                path = Path(directory) / result["id"] / Path(result["asset"]).name
                path.parent.mkdir()
                path.write_bytes(archive.read(result["asset"]))
                extracted[str(path)] = result["id"]
        if not extracted:
            failures.append("Source Link: no expected package assemblies to inspect")
            return []
        arguments = ["dotnet", str(tool.resolve()), "--root", str(ROOT), "--commit", commit]
        for path in extracted:
            arguments += ["--assembly", path]
        code, output = run_captured(arguments, ROOT)
        try:
            evidence = json.loads(output)
            if not isinstance(evidence, list) or len(evidence) != len(extracted) or {item["path"] for item in evidence} != set(extracted):
                raise ValueError("symbol evidence does not cover the extracted assembly set")
            for item in evidence:
                item["package"] = extracted[item["path"]]
                expected = source_packages()[item["package"]]
                if item["name"] != expected["assembly"] or item["token"] != expected["token"]:
                    failures.append(f"{item['package']}: unexpected assembly identity {item['name']} / {item['token']}")
                failures.extend(f"{item['package']}: Source Link: {problem}" for problem in item["errors"])
            if code and not any(item["errors"] for item in evidence):
                failures.append("Source Link helper failed without diagnostic evidence")
            return evidence
        except (ValueError, KeyError, TypeError) as error:
            failures.append(f"Source Link helper did not produce complete evidence: {error}\n{output[-4000:]}")
            return []


def audit_policy(failures, today=None):
    """Accept advisories only through NuGetAuditSuppress items with a reason and a current review date, and keep
    projects from weakening the shared audit settings. Returns the suppressions with whether each is accepted."""
    today = today or datetime.date.today()
    tracked = subprocess.run(["git", "ls-files", "*.props", "*.targets", "*.csproj"], cwd=ROOT, check=True,
                             text=True, stdout=subprocess.PIPE).stdout.split()
    suppressions = []
    for name in tracked:
        for element in ET.parse(ROOT / name).iter():
            tag = local_name(element)
            values = [(tag, element.text or "")] + list(element.attrib.items())
            for key, value in values:
                if key in ("NoWarn", "WarningsNotAsErrors") and AUDIT_CODE.search(value):
                    failures.append(f"{name}: {key} hides NuGet audit warnings; accept an advisory with NuGetAuditSuppress instead")
            if name not in AUDIT_SETTINGS_FILES:
                if tag in AUDIT_PROPERTIES:
                    failures.append(f"{name}: sets {tag}; NuGet audit settings belong in Directory.Build.props")
                elif tag == "WarningsAsErrors" and "$(WarningsAsErrors)" not in (element.text or ""):
                    failures.append(f"{name}: replaces WarningsAsErrors instead of extending $(WarningsAsErrors)")
            if tag != "NuGetAuditSuppress" or "Include" not in element.attrib:
                continue
            metadata = {local_name(child): (child.text or "").strip() for child in element} | \
                       {key: value.strip() for key, value in element.attrib.items()}
            advisory = metadata["Include"]
            suppression = {"file": name, "advisory": advisory, "reason": metadata.get("Reason", ""),
                           "reviewBy": metadata.get("ReviewBy", ""), "accepted": False}
            suppressions.append(suppression)
            if not suppression["reason"]:
                failures.append(f"{name}: NuGetAuditSuppress {advisory} has no Reason")
            try:
                review = datetime.date.fromisoformat(suppression["reviewBy"])
            except ValueError:
                failures.append(f"{name}: NuGetAuditSuppress {advisory} needs ReviewBy as YYYY-MM-DD")
                continue
            if review < today:
                failures.append(f"{name}: NuGetAuditSuppress {advisory} passed its review date {review.isoformat()}")
            else:
                suppression["accepted"] = bool(suppression["reason"])
    return suppressions


def inspect(arguments):
    if arguments.approved:
        Path(arguments.approved).unlink(missing_ok=True)
    publish, held = load_manifest()
    packages_directory = Path(arguments.packages)
    packages = {path.name.removesuffix(f".{arguments.version}.nupkg"): path for path in packages_directory.glob(f"*.{arguments.version}.nupkg")}
    failures = []
    if arguments.skip_smoke and arguments.approved:
        failures.append("Cannot produce an approval manifest when package consumer smoke checks are skipped")

    unexpected = sorted(set(packages) - set(publish) - set(held))
    missing = sorted((set(publish) | set(held)) - set(packages))
    stray = sorted(path.name for path in packages_directory.glob("*.nupkg") if not path.name.endswith(f".{arguments.version}.nupkg"))
    failures += [f"{package}: not listed in release/packages.json" for package in unexpected]
    failures += [f"{package}: listed in release/packages.json but not produced" for package in missing]
    failures += [f"{name}: package does not have version {arguments.version}" for name in stray]

    results = [inspect_package(packages[package], arguments.version, arguments.commit, publish, failures) for package in sorted(packages)]
    symbols = inspect_symbols(packages, results, arguments.commit, Path(getattr(arguments, "symbols_tool", None) or SYMBOLS_TOOL), failures)
    suppressions = audit_policy(failures)
    accepted = [item["advisory"] for item in suppressions if item["accepted"]]
    smoke = smoke_test(packages_directory, arguments.version, publish, failures, accepted) if not arguments.skip_smoke else None

    report = render_report(arguments, publish, held, results, smoke, failures)
    if arguments.skip_smoke:
        report += "\nPackage consumer smoke checks were skipped; this inspection does not establish release readiness.\n"
    report += "\n## Accepted NuGet advisories\n\n"
    report += "".join(f"- {item['advisory']} in {item['file']}, review by {item['reviewBy']}: {item['reason']}\n" for item in suppressions) or "None.\n"
    report += "\n## Source Link and source checksums\n\n"
    report += "These checks compare symbol documents with the current checkout or verified embedded generated source; they do not establish that uncommitted source is reproducible from the recorded commit.\n\n"
    for item in symbols:
        report += f"- {item['package']}: {item['trackedDocuments']} checkout documents; {item['embeddedDocuments']} verified generated documents; {len(item['errors'])} error(s).\n"
    if arguments.report:
        Path(arguments.report).write_text(report, encoding="utf-8")
    approved = sorted(packages[package].name for package in publish if package in packages)
    if arguments.approved and not failures:
        Path(arguments.approved).write_text("\n".join(approved) + "\n", encoding="utf-8")
    elif arguments.approved:
        Path(arguments.approved).unlink(missing_ok=True)
    print(report)
    if failures:
        raise SystemExit(f"{len(failures)} release check(s) failed")


def run_captured(arguments, cwd):
    result = subprocess.run(arguments, cwd=cwd, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    return result.returncode, result.stdout


CORE_CHECKS = {
    "consumer consumed SubmitOrder", "saga created for the order", "consumer published OrderAccepted",
    "application URN unchanged", "framework URN uses MessageTransit", "message payload round trip", "saga reached Accepted state",
}


def consumer_recipes(publish):
    central = {item.get("Include"): item.get("Version")
               for item in ET.parse(ROOT / "Directory.Packages.props").iter("PackageVersion")}
    di = {"Microsoft.Extensions.DependencyInjection": central["Microsoft.Extensions.DependencyInjection"]}
    return [
        ("co-install", publish, {}, ["CoInstall.cs", "Contracts.cs", "AnalyzerProbe.cs"], "", {"all packages restored together"}),
        ("core", ["MessageTransit"], di, ["Program.cs", "Contracts.cs"], "", CORE_CHECKS),
        ("Newtonsoft", ["MessageTransit.Newtonsoft"], di, ["Program.cs", "Contracts.cs"], "NEWTONSOFT", CORE_CHECKS | {"Newtonsoft serializer used"}),
        ("MessagePack", ["MessageTransit.MessagePack"], di, ["Program.cs", "Contracts.cs"], "MESSAGEPACK", CORE_CHECKS | {"MessagePack serializer used"}),
        ("RabbitMQ", ["MessageTransit.RabbitMQ"], di | {"Testcontainers.RabbitMq": central["Testcontainers.RabbitMq"]},
         ["Program.cs", "Contracts.cs"], "RABBITMQ", CORE_CHECKS | {"RabbitMQ transport used"}),
        ("EF Core SQLite", ["MessageTransit.EntityFrameworkCore"],
         {"Microsoft.EntityFrameworkCore.Sqlite": central["Microsoft.EntityFrameworkCore.Relational"]},
         ["Persistence.cs", "PersistenceTypes.cs"], "",
         {"EF Core SQLite saga round trip", "EF Core application assigned correlation key"}),
    ]


def smoke_test(packages_directory, version, publish, failures, accepted=()):
    """Keep co-install coverage and independently exercise top-level packages in minimal consumers."""
    smoke = {"built": False, "scenario": [], "analyzer": False, "consumers": []}
    # consumers accept the same advisories as the repository while their review date has not passed
    accepted_items = "".join(f'    <NuGetAuditSuppress Include="{escape(advisory, {chr(34): "&quot;"})}" />\n' for advisory in accepted)
    suppressions = f"  <ItemGroup>\n{accepted_items}  </ItemGroup>\n" if accepted_items else ""
    with tempfile.TemporaryDirectory(prefix="messagetransit-smoke-") as directory:
        base = Path(directory)
        for index, (name, packages, external, sources, constants, expected) in enumerate(consumer_recipes(publish)):
            result = {"name": name, "built": False, "references": packages, "checks": []}
            smoke["consumers"].append(result)
            consumer = base / f"consumer-{index}"
            consumer.mkdir()
            for source in sources:
                shutil.copy(SMOKE_SOURCES / source, consumer / source)
            references = {package: version for package in packages} | external
            items = "\n".join(f'    <PackageReference Include="{escape(package)}" Version="{escape(value)}" />'
                              for package, value in references.items())
            (consumer / "ReleaseSmoke.csproj").write_text(f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{TARGET_FRAMEWORK}</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <DefineConstants>{constants}</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
{items}
  </ItemGroup>
</Project>
""", encoding="utf-8")
            # Each project has independent restore assets but shares this run's initially empty cache.
            config = ET.Element("configuration")
            settings = ET.SubElement(config, "config")
            ET.SubElement(settings, "add", key="globalPackagesFolder", value=str(base / "packages"))
            feeds = ET.SubElement(config, "packageSources")
            ET.SubElement(feeds, "clear")
            ET.SubElement(feeds, "add", key="candidates", value=str(packages_directory.resolve()))
            ET.SubElement(feeds, "add", key="nuget.org", value="https://api.nuget.org/v3/index.json")
            mapping = ET.SubElement(config, "packageSourceMapping")
            local = ET.SubElement(mapping, "packageSource", key="candidates")
            ET.SubElement(local, "package", pattern="MessageTransit")
            ET.SubElement(local, "package", pattern="MessageTransit.*")
            public = ET.SubElement(mapping, "packageSource", key="nuget.org")
            ET.SubElement(public, "package", pattern="*")
            audit_sources = ET.SubElement(config, "auditSources")
            ET.SubElement(audit_sources, "clear")
            ET.SubElement(audit_sources, "add", key="nuget.org", value="https://api.nuget.org/v3/index.json")
            ET.ElementTree(config).write(consumer / "nuget.config", encoding="utf-8", xml_declaration=True)
            (consumer / "Directory.Build.props").write_text(CONSUMER_PROPS.format(accepted=suppressions), encoding="utf-8")
            (consumer / "Directory.Packages.props").write_text("<Project />\n", encoding="utf-8")

            code, output = run_captured(["dotnet", "build", "-c", "Release", "--nologo"], consumer)
            if code != 0:
                errors = {line.strip() for line in output.splitlines() if BUILD_ERROR.search(line)}
                audit_errors = sorted(error for error in errors if AUDIT_ERROR.search(error))
                if audit_errors and len(audit_errors) == len(errors):
                    failures.append(f"smoke {name}: NuGet audit failed\n" + "\n".join(audit_errors))
                else:
                    failures.append(f"smoke {name}: build failed\n" + output[-4000:])
                continue
            result["built"] = True
            if name == "co-install":
                smoke["analyzer"] = bool(re.search(r"AnalyzerProbe\.cs\(\d+,\d+\): warning MTA0001\b", output))
                if not smoke["analyzer"]:
                    failures.append("smoke co-install: packaged analyzer did not diagnose the AnalyzerProbe source")
            code, output = run_captured(["dotnet", "run", "-c", "Release", "--no-build"], consumer)
            checks = [line.split("\t", 1) for line in output.splitlines() if "\t" in line]
            result["checks"] = checks
            smoke["scenario"].extend((status, f"{name}: {check}") for status, check in checks)
            if code != 0 or len(checks) != len(expected) or {check for _, check in checks} != expected or any(status != "pass" for status, _ in checks):
                failures.append(f"smoke {name}: scenario did not pass all expected assertions\n" + output[-4000:])
        smoke["built"] = all(consumer["built"] for consumer in smoke["consumers"])
    return smoke


def render_report(arguments, publish, held, results, smoke, failures):
    lines = [f"# Release candidate {arguments.version}", "", f"Commit `{arguments.commit}`.", ""]
    lines += ["## Result", "", "**All checks passed.**" if not failures else f"**{len(failures)} check(s) failed:**", ""]
    lines += [f"- {failure.splitlines()[0]}" for failure in failures]
    diagnostics = [failure for failure in failures if "\n" in failure]
    if diagnostics:
        lines += ["", "## Failure diagnostics", ""]
        for failure in diagnostics:
            summary, details = failure.split("\n", 1)
            lines += [f"### {summary}", "", "````text", details, "````", ""]
    lines += ["", "## Packages to publish", "", "| Package | License | Frameworks | Dependencies |", "| --- | --- | --- | --- |"]
    by_id = {result["id"]: result for result in results}
    for package in publish:
        result = by_id.get(package)
        if result:
            lines.append(f"| {package} | {result['license']} | {', '.join(result['frameworks'])} | {', '.join(result['dependencies'] + result['external_dependencies']) or 'none'} |")
    lines += ["", "## Held back", "", "| Package | Reason |", "| --- | --- |"]
    lines += [f"| {package} | {reason} |" for package, reason in sorted(held.items())]
    if smoke is not None:
        lines += ["", "## Clean package consumers", ""]
        for consumer in smoke["consumers"]:
            lines.append(f"- {consumer['name']}: {'built' if consumer['built'] else 'build failed'}; top-level fork references: {', '.join(consumer['references'])}.")
        lines += [f"- {status}: {check}" for status, check in smoke["scenario"]]
        lines += [f"- {'pass' if smoke['analyzer'] else 'FAIL'}: packaged analyzer diagnosed AnalyzerProbe with MTA0001"]
    return "\n".join(lines) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)

    version = commands.add_parser("version", help="Calculate the next release version from tags and Conventional Commits")
    version.add_argument("--bump", choices=("auto", "patch", "minor", "major"), default="auto",
                         help="Override the bump derived from commit messages")
    version.add_argument("--channel", choices=("preview", "stable"), default="stable")
    version.add_argument("--version", help="Explicit version matching the selected channel")
    version.set_defaults(handler=resolve_version)

    check = commands.add_parser("inspect", help="Inspect candidate packages and smoke-test them")
    check.add_argument("--version", required=True)
    check.add_argument("--commit", required=True, help="The commit the packages must record as their source")
    check.add_argument("--packages", required=True, help="The folder containing the candidate .nupkg files")
    check.add_argument("--report", help="Write the Markdown report to this file")
    check.add_argument("--approved", help="Write the file names of the packages to publish to this file")
    check.add_argument("--symbols-tool", help="Path to the built ReleaseSymbols.dll helper")
    check.add_argument("--skip-smoke", action="store_true", help="Inspect package contents only")
    check.set_defaults(handler=inspect)

    arguments = parser.parse_args()
    arguments.handler(arguments)


if __name__ == "__main__":
    main()
