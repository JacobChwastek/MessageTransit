#!/usr/bin/env python3
"""Inventory tracked MSBuild projects without restoring, building, or packing.

Requires Python 3.9+, Git, and a .NET SDK with MSBuild -getProperty/-getItem.
Run from any directory. --check re-evaluates and compares the complete document.
Only the inventory output is written; restored obj imports are disabled.
"""

import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT = ROOT / "docs/architecture/dependency-inventory.json"
ITEM_TYPES = (
    "ProjectReference", "PackageReference", "FrameworkReference", "Reference",
    "PackageVersion", "GlobalPackageReference",
)
PROPERTY_NAMES = (
    "TargetFramework", "TargetFrameworks", "PackageId", "IsPackable",
    "PackageLicenseExpression", "IsTestProject", "OutputType",
    "TargetFrameworkIdentifier", "TargetFrameworkVersion",
    "ManagePackageVersionsCentrally", "LangVersion", "NETCoreSdkVersion",
    "MSBuildVersion",
)
SOURCE_PROPERTIES = set(PROPERTY_NAMES) | {"IsWindows", "IsMac", "Configuration"}
BUILTIN_METADATA = {
    "Identity", "FullPath", "RootDir", "Filename", "Extension", "RelativeDir",
    "Directory", "RecursiveDir", "ModifiedTime", "CreatedTime", "AccessedTime",
    "DefiningProjectFullPath", "DefiningProjectDirectory", "DefiningProjectName",
    "DefiningProjectExtension",
}
PLATFORMS = {
    "non_windows": {"IsWindows": "false", "IsMac": "false"},
    "windows": {"IsWindows": "true", "IsMac": "false"},
}
CONTEXTS = [
    {"platform": platform, "configuration": configuration}
    for platform in PLATFORMS
    for configuration in ("Release",)
]


def run(command):
    return subprocess.run(command, cwd=ROOT, capture_output=True, text=True)


def tracked_files():
    result = run(["git", "ls-files", "-z"])
    if result.returncode:
        raise RuntimeError("git ls-files failed")
    return sorted(set(result.stdout.rstrip("\0").split("\0")))


def portable_origin(value):
    if not value:
        return None
    path = Path(value)
    try:
        return path.resolve().relative_to(ROOT).as_posix()
    except ValueError:
        # SDK item origins are useful; installation locations are not.
        return "sdk:" + path.name


def source_declarations(path):
    """Preserve source order, item operations, all inherited conditions/metadata."""
    tree = ET.parse(ROOT / path)
    declarations = []
    conditions = []

    def walk(element, inherited=(), parent_tag=None):
        tag = element.tag.rsplit("}", 1)[-1]
        condition = element.get("Condition")
        chain = inherited + ((condition,) if condition else ())
        if condition:
            conditions.append({"element": tag, "condition": condition})
        if tag in ITEM_TYPES or (parent_tag == "PropertyGroup" and tag in SOURCE_PROPERTIES) or tag == "Import":
            record = {"element": tag}
            if element.attrib:
                record["attributes"] = dict(sorted(element.attrib.items()))
            if chain:
                record["conditions"] = list(chain)
            if element.text and element.text.strip():
                record["value"] = element.text.strip()
            children = []
            for child in element:
                children.append({
                    "name": child.tag.rsplit("}", 1)[-1],
                    "value": (child.text or "").strip(),
                    **({"attributes": dict(sorted(child.attrib.items()))} if child.attrib else {}),
                })
            if children:
                record["metadata"] = children
            declarations.append(record)
        for child in element:
            walk(child, chain, tag)

    walk(tree.getroot())
    return {"declarations": declarations, "conditions": conditions}


def evaluate(project, context, target=None):
    command = [
        "dotnet", "msbuild", project,
        "-getProperty:" + ",".join(PROPERTY_NAMES),
        "-getItem:" + ",".join(ITEM_TYPES),
        "-p:Configuration=" + context["configuration"],
        "-p:GITHUB_ACTIONS=false",
        "-p:ImportProjectExtensionProps=false",
        "-p:ImportProjectExtensionTargets=false",
    ]
    command.extend("-p:" + key + "=" + value for key, value in PLATFORMS[context["platform"]].items())
    if target:
        command.append("-p:TargetFramework=" + target)
    result = run(command)
    if result.returncode:
        return {"error": {
            "message": "MSBuild evaluation failed; rerun the documented command for diagnostics",
            "exit_code": result.returncode,
            "diagnostic_codes": sorted(set(re.findall(r"\b(?:MSB|NETSDK|NU)\d{4}\b", result.stdout + result.stderr))),
        }}
    try:
        return json.loads(result.stdout)
    except json.JSONDecodeError:
        return {"error": {"message": "MSBuild did not return the expected JSON document"}}


def metadata(item):
    return {key: value for key, value in sorted(item.items()) if key not in BUILTIN_METADATA and value != ""}


def clean_item(item):
    record = {"id": item["Identity"]}
    values = metadata(item)
    if values:
        record["metadata"] = values
    origin = portable_origin(item.get("DefiningProjectFullPath"))
    if origin:
        record["origin"] = origin
    return record


def effective_dependencies(raw):
    items = raw["Items"]
    central = {}
    for item in items["PackageVersion"]:
        central.setdefault(item["Identity"].casefold(), []).append(item)
    globals_by_id = {item["Identity"].casefold(): item for item in items["GlobalPackageReference"]}
    packages = []
    for item in items["PackageReference"]:
        record = clean_item(item)
        key = item["Identity"].casefold()
        versions = central.get(key, [])
        if key in globals_by_id:
            record["kind"] = "global"
        elif item.get("IsImplicitlyDefined", "").lower() == "true":
            record["kind"] = "sdk_implicit"
        else:
            record["kind"] = "direct"
        if item.get("VersionOverride"):
            record["version"] = item["VersionOverride"]
            record["version_source"] = "VersionOverride"
        elif item.get("Version"):
            record["version"] = item["Version"]
            record["version_source"] = "PackageReference"
        elif len(versions) == 1 and versions[0].get("Version"):
            record["version"] = versions[0]["Version"]
            record["version_source"] = "evaluated PackageVersion"
        else:
            record["version"] = None
            record["version_source"] = "unresolved or ambiguous during evaluation"
        if versions:
            record["central_versions"] = [clean_item(version) for version in versions]
        packages.append(record)
    project_references = []
    for item in items["ProjectReference"]:
        record = clean_item(item)
        record["path"] = portable_origin(item["FullPath"])
        project_references.append(record)
    return {
        "project_references": sorted(project_references, key=lambda item: (item["path"], item["id"])),
        "package_references": sorted(packages, key=lambda item: item["id"].casefold()),
        "framework_references": sorted([clean_item(item) for item in items["FrameworkReference"]], key=lambda item: item["id"]),
        "assembly_references": sorted([clean_item(item) for item in items["Reference"]], key=lambda item: item["id"]),
        "global_package_references": sorted([clean_item(item) for item in items["GlobalPackageReference"]], key=lambda item: item["id"].casefold()),
    }


def group_evaluations(records):
    grouped = {}
    for context, value in records:
        signature = json.dumps(value, sort_keys=True)
        if signature not in grouped:
            grouped[signature] = {"contexts": [], **value}
        grouped[signature]["contexts"].append(context)
    return list(grouped.values())


def inventory_project(path, solutions):
    outer = []
    inner = []
    sdk_versions = set()
    msbuild_versions = set()
    errors = []
    for context in CONTEXTS:
        raw = evaluate(path, context)
        if "error" in raw:
            outer.append((context, raw))
            errors.append({"project": path, "context": context, **raw})
            continue
        properties = raw["Properties"]
        sdk_versions.add(properties.pop("NETCoreSdkVersion"))
        msbuild_versions.add(properties.pop("MSBuildVersion"))
        frameworks = [target for target in (properties["TargetFrameworks"] or properties["TargetFramework"]).split(";") if target]
        if not frameworks:
            errors.append({"project": path, "context": context, "error": {"message": "No target framework was found during evaluation"}})
        outer.append((context, {"properties": properties, "target_frameworks": frameworks}))
        for target in frameworks:
            raw = evaluate(path, context, target)
            if "error" in raw:
                inner.append((context, {"target_framework": target, **raw}))
                errors.append({"project": path, "context": context, "target_framework": target, **raw})
                continue
            properties = raw["Properties"]
            properties.pop("NETCoreSdkVersion")
            properties.pop("MSBuildVersion")
            properties.pop("TargetFrameworks")
            inner.append((context, {"target_framework": target, "properties": properties, **effective_dependencies(raw)}))
    kind = "source" if path.startswith("src/") else ("test" if Path(path).stem.endswith(".Tests") else "benchmark")
    return {
        "path": path,
        "name": Path(path).stem,
        "kind": kind,
        "solutions": [solution for solution, members in solutions.items() if path in members],
        "declarations": path,
        "framework_and_package_variants": group_evaluations(outer),
        "target_variants": group_evaluations(inner),
        "status": {
            "build": "evaluation incomplete; not built" if errors else "evaluated only; not built",
            "package": "evaluation incomplete; not packed or published" if errors else "identity and packability evaluated only; not packed or published",
            "dependencies": "evaluation incomplete; not restored" if errors else "direct and SDK evaluation only; not restored; no transitive resolution",
            "test": "references inventoried only; not executed",
            "consumer": {"repository": "see referenced_by and test_references", "external_applications": "not assessed"},
        },
    }, sdk_versions, msbuild_versions, errors


def reverse_references(projects):
    by_path = {project["path"]: project for project in projects}
    edges = {path: set() for path in by_path}
    for project in projects:
        for variant in project["target_variants"]:
            edges[project["path"]].update(reference["path"] for reference in variant.get("project_references", []))
    for project in projects:
        project["referenced_by"] = sorted(path for path, targets in edges.items() if project["path"] in targets)
        direct_tests = []
        transitive_tests = []
        benchmarks = []
        for candidate in projects:
            path = candidate["path"]
            if candidate["kind"] == "benchmark" and project["path"] in edges[path]:
                benchmarks.append(path)
            if candidate["kind"] != "test":
                continue
            if project["path"] in edges[path]:
                direct_tests.append(path)
            visited = set()
            pending = list(edges[path])
            while pending:
                target = pending.pop()
                if target in visited:
                    continue
                visited.add(target)
                pending.extend(edges.get(target, set()) - visited)
            if project["path"] in visited:
                transitive_tests.append(path)
        project["test_references"] = {
            "direct_tests": direct_tests,
            "tests_reaching_project_including_transitive": transitive_tests,
            "direct_benchmarks": benchmarks,
            "basis": "union of evaluated platform/configuration/TFM edges; reference reachability is not test coverage or execution",
        }


def generate(jobs):
    tracked = tracked_files()
    paths = [path for path in tracked if path.endswith(".csproj")]
    input_paths = [path for path in tracked if path.endswith((".csproj", ".props", ".targets", ".sln", ".slnx")) or path.startswith(".github/workflows/") or path in ("global.json", "NuGet.Config", "nuget.config")]
    # SDK selection affects evaluation even before a new global.json is staged.
    if (ROOT / "global.json").is_file() and "global.json" not in input_paths:
        input_paths = sorted([*input_paths, "global.json"])
    sources = {path: source_declarations(path) for path in input_paths if path.endswith((".csproj", ".props", ".targets"))}
    solutions = {}
    for path in input_paths:
        if path.endswith(".sln"):
            solutions[path] = sorted(set(
                (Path(path).parent / match.replace("\\", "/")).as_posix()
                for match in re.findall(r'Project\([^\n]+?\)\s*=\s*"[^"]+",\s*"([^"]+\.csproj)"', (ROOT / path).read_text(encoding="utf-8-sig"))
            ))
        elif path.endswith(".slnx"):
            solutions[path] = sorted(set((Path(path).parent / element.get("Path")).as_posix() for element in ET.parse(ROOT / path).iter("Project") if element.get("Path", "").endswith(".csproj")))
    projects = []
    sdks = set()
    msbuilds = set()
    errors = []
    with ThreadPoolExecutor(max_workers=jobs) as pool:
        for index, (project, sdk, msbuild, failures) in enumerate(pool.map(lambda path: inventory_project(path, solutions), paths), 1):
            projects.append(project)
            sdks.update(sdk)
            msbuilds.update(msbuild)
            errors.extend(failures)
            if index % 10 == 0 or index == len(paths):
                print(f"Evaluated {index}/{len(paths)} projects", file=sys.stderr)
    reverse_references(projects)
    outside = [project["path"] for project in projects if not project["solutions"]]
    counts = {kind: sum(project["kind"] == kind for project in projects) for kind in ("source", "test", "benchmark")}
    return {
        "schema_version": 1,
        "generator": "scripts/dependency_inventory.py",
        "evaluation": {
            "method": "dotnet msbuild -getProperty/-getItem, evaluation only, no targets",
            "sdk_versions": sorted(sdks),
            "msbuild_versions": sorted(msbuilds),
            "contexts": CONTEXTS,
            "global_properties": {
                "GITHUB_ACTIONS": "false",
                "ImportProjectExtensionProps": "false",
                "ImportProjectExtensionTargets": "false",
            },
            "platform_properties": PLATFORMS,
            "limitations": [
                "Platform names mean explicit IsWindows/IsMac property overrides, not native OS execution; SDK host behavior is not simulated. IsMac is defined but unused by current dependency/TFM declarations.",
                "SDK evaluation includes implicit references but excludes restored obj props/targets; package build assets and transitive dependencies are not resolved.",
                "Evaluated central version metadata is preserved separately; targets may later change metadata during restore/build/pack.",
                "Release is evaluated. Current Debug/Release conditions only change compiler flags; Debug, custom configurations, and GITHUB_ACTIONS=true are not evaluated. Source conditions remain in source_files.",
                "Benchmark kind is classified by path/name; tests/Directory.Build.props also sets IsTestProject=true on both benchmarks.",
                "Packability and package license expressions describe current declarations/evaluation; build, publication, legal clearance, and test execution are unverified.",
                "Test reachability uses the union of evaluated reference graphs, not assertions, runtime coverage, or compatible-framework validation.",
                "Consumer status describes repository references only; application deployments and persisted data are not assessed.",
            ],
            "errors": errors,
        },
        "input_sha256": {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest() for path in input_paths},
        "summary": {
            "tracked_projects": len(paths),
            **counts,
            "test_and_benchmark": counts["test"] + counts["benchmark"],
            "solution_project_counts": {path: len(members) for path, members in solutions.items()},
            "outside_solution": outside,
            "solution_members_without_tracked_project": {path: sorted(set(members) - set(paths)) for path, members in solutions.items()},
        },
        "source_files": sources,
        "solutions": solutions,
        "projects": projects,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Re-evaluate and fail if the saved inventory differs (no writes)")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--jobs", type=int, default=4, help="Concurrent project evaluations (default: 4)")
    args = parser.parse_args()
    if args.jobs < 1:
        parser.error("--jobs must be at least 1")
    try:
        document = generate(args.jobs)
        content = json.dumps(document, indent=2, ensure_ascii=False) + "\n"
        if args.check:
            if not args.output.is_file() or args.output.read_text(encoding="utf-8") != content:
                print("Inventory is stale or missing; run python3 scripts/dependency_inventory.py", file=sys.stderr)
                return 1
            print("Inventory matches current tracked inputs and MSBuild evaluations")
        else:
            args.output.parent.mkdir(parents=True, exist_ok=True)
            args.output.write_text(content, encoding="utf-8")
            print(f"Wrote inventory: {document['summary']['tracked_projects']} projects")
        if document["evaluation"]["errors"]:
            print("Inventory has evaluation failures; inspect evaluation.errors", file=sys.stderr)
            return 2
        unresolved = [reference for project in document["projects"] for variant in project["target_variants"] for reference in variant.get("package_references", []) if reference["version"] is None]
        if unresolved:
            print(f"Inventory has {len(unresolved)} unresolved package-version entries", file=sys.stderr)
            return 2
        return 0
    except (OSError, RuntimeError, ET.ParseError, KeyError) as error:
        print(f"Inventory generation failed: {type(error).__name__}: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
