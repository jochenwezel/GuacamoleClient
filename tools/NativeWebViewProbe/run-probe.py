"""Runs both real-browser phases and fails if a check fails or the app exits early."""

import argparse
import json
import os
from pathlib import Path
import platform
import subprocess
import sys
import time
import urllib.request


def run_phase(dll, output, port, phase):
    report_path = output / f"results-{phase}.json"
    if report_path.exists():
        raise RuntimeError(f"Refusing to reuse an existing {phase} report: {report_path}")
    startup = None
    if os.name == "nt":
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
    with (output / f"stdout-{phase}.log").open("w") as stdout, (
        output / f"stderr-{phase}.log"
    ).open("w") as stderr:
        process = subprocess.Popen(
            ["dotnet", str(dll), "--output", str(output), "--phase", phase, "--port", str(port)],
            stdout=stdout, stderr=stderr, startupinfo=startup,
        )
        try:
            deadline = time.monotonic() + 150
            while not report_path.exists():
                if process.poll() is not None:
                    raise RuntimeError(f"{phase}: browser process exited with {process.returncode}")
                if time.monotonic() > deadline:
                    raise TimeoutError(f"{phase}: no result within 150 seconds")
                time.sleep(0.5)
            report = json.loads(report_path.read_text(encoding="utf-8-sig"))
            if platform.system() == "Darwin":
                subprocess.run(["screencapture", "-x", str(output / f"desktop-{phase}.png")],
                               timeout=10, check=False)
            failures = [r for r in report["results"] if not r["passed"]]
            print(f"{phase}: {len(report['results']) - len(failures)} passed, {len(failures)} failed", flush=True)
            for failure in failures:
                print(f"FAIL {failure['name']}: {failure['detail']}", flush=True)
            return not failures
        finally:
            if process.poll() is None:
                try:
                    request = urllib.request.Request(f"http://127.0.0.1:{port}/shutdown", method="POST")
                    with urllib.request.urlopen(request, timeout=5) as response:
                        response.read()
                    process.wait(timeout=15)
                except Exception:
                    process.terminate()
                    process.wait(timeout=10)
            # Let the native browser finish shutting down before reopening its profile.
            time.sleep(3)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("dll", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--port", type=int, default=49171)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    outcomes = []
    for phase in ("seed", "verify"):
        try:
            outcomes.append(run_phase(args.dll.resolve(), args.output.resolve(), args.port, phase))
        except Exception as error:
            print(f"ERROR {phase}: {error}", file=sys.stderr, flush=True)
            outcomes.append(False)
    return 0 if all(outcomes) else 1


if __name__ == "__main__":
    sys.exit(main())
