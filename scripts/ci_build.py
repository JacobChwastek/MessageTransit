#!/usr/bin/env python3
"""Build every tracked project and smoke-check both benchmark entry points."""

from pathlib import Path
import re
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[1]
SOLUTION = ROOT / "MassTransit.sln"
BENCHMARKS = (
    ("MassTransit.Benchmark", ["--help"], "Usage: mtbench [OPTIONS]+"),
    ("MassTransit.BenchmarkConsole", ["--list", "flat"], "MassTransit.BenchmarkConsole.Benchmarker.GetNext"),
)


def run(*arguments):
    print("+ " + " ".join(arguments), flush=True)
    subprocess.run(arguments, cwd=ROOT, check=True)


def main():
    tracked = set(subprocess.check_output(["git", "ls-files", "*.csproj"], cwd=ROOT, text=True).splitlines())
    included = {path.replace("\\", "/") for path in re.findall(r'"([^"\r\n]+\.csproj)"', SOLUTION.read_text(encoding="utf-8-sig"))}
    if not included or included - tracked:
        raise RuntimeError("The solution contains missing or untracked projects, or could not be parsed")
    outside = sorted(tracked - included)
    print(f"Building {len(tracked)} tracked projects: {len(included)} in the solution and {len(outside)} outside it.", flush=True)
    run("dotnet", "restore", str(SOLUTION))
    run("dotnet", "build", str(SOLUTION), "--configuration", "Release", "--no-restore")
    for project in outside:
        run("dotnet", "restore", project)
        run("dotnet", "build", project, "--configuration", "Release", "--framework", "net10.0", "--no-restore")

    for project, arguments, expected in BENCHMARKS:
        command = ["dotnet", "run", "--project", f"tests/{project}/{project}.csproj",
                   "--configuration", "Release", "--framework", "net10.0", "--no-build", "--no-restore", "--", *arguments]
        result = subprocess.run(command, cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=60)
        print(result.stdout, flush=True)
        if result.returncode != 0:
            raise RuntimeError(f"{project} exited with code {result.returncode} during the startup check")
        if expected not in result.stdout or "Crashed:" in result.stdout or "mtbench: " in result.stdout:
            raise RuntimeError(f"{project} did not produce the expected startup output")
        print(f"{project}: startup check passed (no benchmark workload executed).", flush=True)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except subprocess.TimeoutExpired as error:
        print(error.output or "", flush=True)
        print(f"CI build failed: {error}", file=sys.stderr)
        sys.exit(1)
    except (OSError, RuntimeError, subprocess.SubprocessError) as error:
        print(f"CI build failed: {error}", file=sys.stderr)
        sys.exit(1)
