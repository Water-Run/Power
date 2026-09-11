# Copyright (C) 2026 Power! contributors
# Licensed under GPL-3.0-or-later with the Unity Linking Exception.
# See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

"""Serial Zig verification, foreign ABI checks, and migration evidence."""

import argparse
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import platform
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
NATIVE = ROOT / "legacy/native"


def run(*arguments):
    display = " ".join(map(str, arguments[:6]))
    if len(arguments) > 6:
        display += f" ... ({len(arguments) - 6} more arguments)"
    print("Running: " + display, flush=True)
    subprocess.run(list(map(str, arguments)), cwd=ROOT, check=True)


def source_audit():
    # Include new files during development, and exclude deleted tracked files.
    inventory = subprocess.check_output(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"], cwd=ROOT
    ).decode().split("\0")
    files = sorted({name for name in inventory if name and (ROOT / name).is_file()})
    forbidden = {".c", ".h", ".cc", ".hh", ".cpp", ".hpp", ".cxx", ".hxx", ".c++", ".h++", ".inl", ".inc"}
    c_files = [name for name in files if Path(name).suffix.lower() in forbidden]
    if c_files:
        raise ValueError("C/C++ source or headers remain: " + ", ".join(c_files))
    zig_files = [name for name in files if name.endswith(".zig")]
    for name in zig_files:
        contents = (ROOT / name).read_text()
        if re.search(r"@cImport\s*\(|@cInclude\s*\(|addCSource|addTranslateC|link_libc\s*=\s*true|linkSystemLibrary", contents):
            raise ValueError(f"{name}: native code must not reintroduce C compilation or libc linkage")
    manifest = json.loads((NATIVE / "migration-manifest.json").read_text())
    for item in manifest["sources"]:
        if (ROOT / item["path"]).exists():
            raise ValueError(f"Original source still exists: {item['path']}")
        if not (ROOT / item["zig_path"]).is_file():
            raise ValueError(f"Missing migrated source: {item['zig_path']}")
    for name in ("COPYING.NOTICE", "LICENSE", "UNITY-LINKING-EXCEPTION.md"):
        if not (ROOT / name).is_file():
            raise ValueError(f"Required license notice missing: {name}")
    return {"c_source_files": 0, "zig_source_files": len(zig_files), "migrated_files": len(manifest["sources"])}


def baseline_check(library):
    spec = importlib.util.spec_from_file_location("power_native_lab", NATIVE / "tools/model_lab.py")
    lab = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(lab)
    document = NATIVE / "assets/labs/electrothermal.power.json"
    baseline = json.loads((NATIVE / "tests/fixtures/c-baseline.json").read_text())
    if hashlib.sha256(document.read_bytes()).hexdigest() != baseline["source_sha256"]:
        raise ValueError("The native baseline model changed; provide new reviewed evidence before updating the fixture")
    report = lab.evaluate(lab.read_document(document), library)
    if not report["passed"] or not report["replay"]["sample_hashes_match"]:
        raise ValueError("Native KPIs or same-binary replay failed")
    if report["model"] != baseline["model"] or report["channels"] != baseline["channels"]:
        raise ValueError("Native model fingerprint, fidelity or channel contract changed")
    if len(report["samples"]) != len(baseline["samples"]):
        raise ValueError("Native baseline sampling changed")
    maximum_error = 0.0
    values_checked = 0
    for actual, expected in zip(report["samples"], baseline["samples"]):
        if actual["time_ns"] != expected["time_ns"] or actual["values"].keys() != expected["values"].keys():
            raise ValueError("Native baseline time/channel mapping changed")
        for channel, value in actual["values"].items():
            reference = expected["values"][channel]
            if not math.isfinite(value) or not math.isclose(value, reference,
                    rel_tol=baseline["relative_tolerance"], abs_tol=baseline["absolute_tolerance"]):
                raise ValueError(f"Native baseline diverged at {actual['time_ns']} ns, channel {channel}: {value} vs {reference}")
            maximum_error = max(maximum_error, abs(value - reference))
            values_checked += 1
    return report, {"source_revision": baseline["source_revision"], "values_checked": values_checked,
                    "maximum_absolute_error": maximum_error,
                    "absolute_tolerance": baseline["absolute_tolerance"], "relative_tolerance": baseline["relative_tolerance"]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-only", action="store_true")
    args = parser.parse_args()
    inventory = source_audit()
    if args.source_only:
        print(json.dumps(inventory))
        return
    cached = ROOT / ".cache/zig" / ("zig.exe" if os.name == "nt" else "zig")
    zig = os.environ.get("POWER_ZIG") or (str(cached) if cached.is_file() else shutil.which("zig"))
    if not zig:
        raise ValueError("Zig is missing. Run python tools/InstallZig.py or set POWER_ZIG to the pinned compiler executable")
    version = subprocess.check_output([zig, "version"], text=True).strip()
    if version != (ROOT / ".zig-version").read_text().strip():
        raise ValueError(f"Zig {version} does not match .zig-version; run python tools/InstallZig.py or update POWER_ZIG")
    run(zig, "fmt", "--check", *[str(p) for p in NATIVE.rglob("*.zig") if ".zig-cache" not in p.parts and "zig-out" not in p.parts])
    destination = ROOT / "artifacts/native"
    build = [zig, "build", "--build-file", str(NATIVE / "build.zig"), "-j1", "-Doptimize=ReleaseSafe", "--summary", "all"]
    run(*build, "--prefix", destination)
    run(*build, "test")
    executable_suffix = ".exe" if os.name == "nt" else ""
    for name in ("power_host", "power_model_host"):
        run(destination / "bin" / (name + executable_suffix))
    library = destination / {"Windows": "bin/power.dll", "Darwin": "lib/libpower.dylib", "Linux": "lib/libpower.so"}[platform.system()]
    run(sys.executable, NATIVE / "tests/test_model_lab.py", library)
    exports = None
    if platform.system() == "Linux":
        nm = shutil.which("nm")
        if not nm:
            raise ValueError("Install binutils to verify the native export boundary with nm")
        symbols = subprocess.check_output([nm, "-D", "--defined-only", str(library)], text=True)
        exports = sorted(set(re.findall(r"\bpwr_\w+\b", symbols)))
        if exports != ["pwr_get_api"]:
            raise ValueError(f"Unexpected public native exports: {exports}")
        undefined = subprocess.check_output([nm, "-D", "--undefined-only", str(library)], text=True).strip()
        if undefined:
            raise ValueError(f"Native runtime has external symbol dependencies: {undefined}")
    experiment, comparison = baseline_check(library)
    reports = ROOT / "artifacts/reports"
    reports.mkdir(parents=True, exist_ok=True)
    (reports / "native-electrothermal.json").write_text(json.dumps(experiment, indent=2) + "\n")
    evidence = {"schema": "power.native_verification.v1", "passed": True,
                "zig_version": version, "platform": platform.platform(), "inventory": inventory,
                "exports": exports, "baseline_comparison": comparison, "calibration": "unverified",
                "library_sha256": hashlib.sha256(library.read_bytes()).hexdigest()}
    (reports / "native-verification.json").write_text(json.dumps(evidence, indent=2) + "\n")
    print(json.dumps(evidence))


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        print(json.dumps({"schema": "power.native_verification.v1", "passed": False, "error": str(error)}), file=sys.stderr)
        sys.exit(1)
