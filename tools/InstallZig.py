# Copyright (C) 2026 Power! contributors
# Licensed under GPL-3.0-or-later with the Unity Linking Exception.
# See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

"""Install the pinned Zig toolchain locally, checking the committed SHA-256."""

import hashlib
import json
from pathlib import Path
import platform
import shutil
import subprocess
import tarfile
import tempfile
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    lock = json.loads((ROOT / "tools/zig-toolchains.json").read_text())
    version = (ROOT / ".zig-version").read_text().strip()
    if version != lock["version"]:
        raise ValueError(".zig-version and tools/zig-toolchains.json disagree")
    system = {"Linux": "linux", "Darwin": "macos", "Windows": "windows"}[platform.system()]
    machine = platform.machine().lower()
    arch = {"amd64": "x86_64", "x86_64": "x86_64", "arm64": "aarch64", "aarch64": "aarch64"}[machine]
    destination = ROOT / ".cache/zig"
    executable = destination / ("zig.exe" if system == "windows" else "zig")
    if executable.exists():
        installed = subprocess.check_output([str(executable), "version"], text=True).strip()
        if installed != version:
            raise ValueError(f"{executable} is {installed}; move that directory aside before installing {version}")
        print(executable)
        return
    if destination.exists():
        raise ValueError(f"{destination} already exists without Zig; move it aside and retry")
    spec = lock["platforms"][f"{arch}-{system}"]
    destination.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="power-zig-", dir=destination.parent) as temporary:
        temporary = Path(temporary)
        archive = temporary / Path(spec["tarball"]).name
        print(f"Downloading Zig {version} for {arch}-{system}", flush=True)
        with urllib.request.urlopen(spec["tarball"], timeout=120) as response, archive.open("wb") as output:
            shutil.copyfileobj(response, output)
        if hashlib.sha256(archive.read_bytes()).hexdigest() != spec["shasum"]:
            raise ValueError("Zig archive SHA-256 does not match the committed toolchain manifest")
        extracted = temporary / "extracted"
        if archive.suffix == ".zip":
            with zipfile.ZipFile(archive) as package:
                package.extractall(extracted)
        else:
            with tarfile.open(archive) as package:
                package.extractall(extracted, filter="data")
        directories = list(extracted.iterdir())
        if len(directories) != 1:
            raise ValueError("Unexpected Zig archive layout")
        shutil.move(str(directories[0]), destination)
    print(executable)


if __name__ == "__main__":
    main()
