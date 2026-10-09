#!/usr/bin/env python3
"""Summarize VSTest results, including reported skip reasons, in GitHub Actions."""

import argparse
import html
import os
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
FAILED_OUTCOMES = {"Failed", "Error", "Timeout", "Aborted"}


def cell(value):
    return html.escape(str(value)).replace("|", "&#124;").replace("\n", "<br>")


def report(path, project, test_filter):
    lines = [f"## {cell(project)}", "", f"Framework: `net10.0`; OS: {cell(os.environ.get('RUNNER_OS', sys.platform))}.",
             f"Filter: `{cell(test_filter)}`.", "",
             "Tests excluded by the filter are not represented in the result counts. "
             "The default excludes tests marked `Flaky` because they are not reliable for unattended CI. "
             "NUnit also skips tests marked Explicit unless deliberately selected, and tests on unsupported platforms; "
             "reported skips are listed below.", ""]
    if not path.is_file():
        lines.append("**No TRX result was produced. Test execution is unverified; inspect the preceding test step.**")
        return "\n".join(lines) + "\n", 1

    root = ET.parse(path).getroot()
    counters = root.find("t:ResultSummary/t:Counters", NS)
    results = root.findall("t:Results/t:UnitTestResult", NS)
    if counters is None:
        lines.append("**The result file has no test counters.**")
        return "\n".join(lines) + "\n", 1

    lines.extend(["| Total | Executed | Passed | Failed | Not executed |",
                  "| ---: | ---: | ---: | ---: | ---: |",
                  "| " + " | ".join(cell(counters.get(key, "0")) for key in
                                      ("total", "executed", "passed", "failed", "notExecuted")) + " |", ""])
    skipped = [result for result in results if result.get("outcome") == "NotExecuted"]
    if skipped:
        lines.extend(["### Reported skips", "", "| Test | Reason |", "| --- | --- |"])
        for result in skipped:
            reason = result.findtext("t:Output/t:ErrorInfo/t:Message", default="", namespaces=NS)
            if not reason:
                reason = result.findtext("t:Output/t:StdOut", default="", namespaces=NS)
            lines.append(f"| {cell(result.get('testName', 'Unnamed test'))} | "
                         f"{cell(reason.strip() or 'The test adapter did not supply a reason; inspect the NUnit attributes.')} |")
    else:
        lines.append("No skipped tests were reported by the adapter.")

    # An empty selection can otherwise return success without exercising a suite.
    executed = int(counters.get("executed", "0"))
    failed = sum(int(counters.get(key, "0")) for key in ("failed", "error", "timeout", "aborted"))
    outcome_failed = root.find("t:ResultSummary", NS).get("outcome", "") in FAILED_OUTCOMES
    if executed == 0:
        lines.extend(["", "**No tests executed. This suite does not satisfy CI coverage.**"])
    if outcome_failed:
        lines.extend(["", "**The test run did not complete successfully.**"])
    return "\n".join(lines) + "\n", int(executed == 0 or failed != 0 or outcome_failed)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("results", type=Path)
    parser.add_argument("--project", required=True)
    parser.add_argument("--filter", default="Category!=Flaky")
    args = parser.parse_args()
    try:
        summary, status = report(args.results, args.project, args.filter)
    except (OSError, ET.ParseError, ValueError) as error:
        summary, status = f"## {cell(args.project)}\n\n**Unable to read test results:** {cell(error)}\n", 1
    print(summary)
    if destination := os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(destination, "a", encoding="utf-8") as output:
            output.write(summary + "\n")
    return status


if __name__ == "__main__":
    sys.exit(main())
