import contextlib
import copy
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().parents[1] / "dependency_inventory.py"
spec = importlib.util.spec_from_file_location("dependency_inventory", SCRIPT)
inventory = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inventory)

DOCUMENT = {
    "evaluation": {"sdk_versions": ["10.0.400"], "msbuild_versions": ["18.9.6"], "errors": []},
    "projects": [{"path": "src/Example/Example.csproj", "target_variants": []}],
}


class CheckTests(unittest.TestCase):
    def check(self, saved, generated):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "inventory.json"
            output.write_text(saved if isinstance(saved, str) else json.dumps(saved), encoding="utf-8")
            with patch.object(inventory, "generate", return_value=generated), \
                    patch.object(sys, "argv", ["dependency_inventory.py", "--check", "--output", str(output)]), \
                    contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
                return inventory.main()

    def test_a_newer_sdk_alone_does_not_make_the_inventory_stale(self):
        newer = copy.deepcopy(DOCUMENT)
        newer["evaluation"]["sdk_versions"] = ["10.0.500"]
        newer["evaluation"]["msbuild_versions"] = ["18.10.1"]
        self.assertEqual(0, self.check(DOCUMENT, newer))

    def test_a_project_change_makes_the_inventory_stale(self):
        changed = copy.deepcopy(DOCUMENT)
        changed["projects"][0]["path"] = "src/Other/Other.csproj"
        self.assertEqual(1, self.check(DOCUMENT, changed))

    def test_an_evaluation_change_besides_the_tools_makes_the_inventory_stale(self):
        changed = copy.deepcopy(DOCUMENT)
        changed["evaluation"]["errors"] = [{"project": "src/Example/Example.csproj"}]
        self.assertEqual(1, self.check(DOCUMENT, changed))

    def test_an_unreadable_inventory_is_stale(self):
        self.assertEqual(1, self.check("{ not json", DOCUMENT))


if __name__ == '__main__':
    unittest.main()
