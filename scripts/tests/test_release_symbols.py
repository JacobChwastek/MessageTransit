"""Offline regression fixtures for the real portable-PDB reader; requires a .NET 10 SDK."""
import json
from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
TOOL = ROOT / "scripts/release_symbols/bin/Release/net10.0/ReleaseSymbols.dll"
COMMIT = "a" * 40


class SourceLinkChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        result = subprocess.run(["dotnet", "build", str(ROOT / "scripts/release_symbols/ReleaseSymbols.csproj"),
                                 "-c", "Release", "--nologo"], capture_output=True, text=True)
        if result.returncode:
            raise RuntimeError(result.stdout + result.stderr)

    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="mts-symbol-fixture-")
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name).resolve()

    def compile(self, mapping=True, commit=COMMIT, repository="JacobChwastek/MessageTransit", generated=False, generated_only=False):
        self.source = self.root / "Probe.cs"
        self.source.write_text("public class Probe { public int Value => 42; }\n")
        if mapping:
            (self.root / "sourcelink.json").write_text(json.dumps({"documents": {
                str(self.root) + "/*": f"https://raw.githubusercontent.com/{repository}/{commit}/*"}}))
        link = '<SourceLink>$(MSBuildProjectDirectory)/sourcelink.json</SourceLink>' if mapping else ""
        generated_item = ""
        if generated or generated_only:
            (self.root / "obj").mkdir()
            (self.root / "obj/Generated.cs").write_text("public class Generated { }\n")
            generated_item = '<ItemGroup><Compile Include="obj/Generated.cs" /></ItemGroup>'
            if generated_only:
                generated_item = '<ItemGroup><Compile Remove="Probe.cs" /><Compile Include="obj/Generated.cs" /></ItemGroup>'
        (self.root / "Fixture.csproj").write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<TargetFramework>net10.0</TargetFramework><DebugType>embedded</DebugType><EmbedAllSources>true</EmbedAllSources>
<GenerateAssemblyInfo>false</GenerateAssemblyInfo><GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>
<EnableSourceControlManagerQueries>false</EnableSourceControlManagerQueries><EnableSourceLink>false</EnableSourceLink>
{link}</PropertyGroup>{generated_item}</Project>''')
        (self.root / "nuget.config").write_text('<configuration><packageSources><clear/></packageSources></configuration>')
        result = subprocess.run(["dotnet", "build", "-c", "Release", "--nologo"], cwd=self.root, capture_output=True, text=True)
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def inspect(self):
        result = subprocess.run(["dotnet", str(TOOL), "--root", str(self.root), "--commit", COMMIT,
                                 "--assembly", str(self.root / "bin/Release/net10.0/Fixture.dll")],
                                capture_output=True, text=True)
        evidence = json.loads(result.stdout)
        return result.returncode, evidence[0]

    def test_correct_mapping_and_checksum_pass(self):
        self.compile()
        code, evidence = self.inspect()
        self.assertEqual(0, code, evidence)
        self.assertEqual(1, evidence["trackedDocuments"])

    def test_missing_source_link_fails(self):
        self.compile(mapping=False)
        code, evidence = self.inspect()
        self.assertNotEqual(0, code)
        self.assertIn("Source Link", " ".join(evidence["errors"]))

    def test_wrong_source_commit_fails(self):
        self.compile(commit="b" * 40)
        code, evidence = self.inspect()
        self.assertNotEqual(0, code)
        self.assertIn("candidate repository and commit", " ".join(evidence["errors"]))

    def test_wrong_source_repository_fails_even_when_embedded(self):
        self.compile(repository="Other/Repository")
        code, evidence = self.inspect()
        self.assertNotEqual(0, code)
        self.assertIn("candidate repository and commit", " ".join(evidence["errors"]))

    def test_changed_checkout_source_fails_even_when_embedded(self):
        self.compile()
        self.source.write_text("public class Probe { public int Value => 99; }\n")
        code, evidence = self.inspect()
        self.assertNotEqual(0, code)
        self.assertIn("Source checksum mismatch", " ".join(evidence["errors"]))

    def test_embedded_generated_document_is_checksum_verified(self):
        self.compile(generated=True)
        code, evidence = self.inspect()
        self.assertEqual(0, code, evidence)
        self.assertEqual(1, evidence["embeddedDocuments"])
        self.assertEqual(1, evidence["trackedDocuments"])

    def test_all_generated_documents_cannot_bypass_repository_source_evidence(self):
        self.compile(generated_only=True, repository="Other/Repository", commit="b" * 40)
        code, evidence = self.inspect()
        self.assertNotEqual(0, code, evidence)
        self.assertEqual(0, evidence["trackedDocuments"])
        self.assertIn("validated checkout source", " ".join(evidence["errors"]))


if __name__ == "__main__":
    unittest.main()
