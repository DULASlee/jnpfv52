#!/usr/bin/env python3
"""Recover ALL historically tracked files under docs/架构迭代/ from git."""
from __future__ import annotations

import csv
import os
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
TARGET = "docs/架构迭代/"
MANIFEST = REPO / "docs" / "_recovery_manifest" / "full-architecture-iteration-recovery.csv"


def run(*args: str) -> str:
    r = subprocess.run(
        ["git", "-C", str(REPO), "-c", "core.quotepath=false", *args],
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    if r.returncode != 0:
        raise RuntimeError(r.stderr.strip() or r.stdout.strip())
    return r.stdout


def main() -> int:
    os.chdir(REPO)
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)

    out = run("log", "--all", "--pretty=format:", "--name-only", "--", TARGET)
    files = sorted({l.strip() for l in out.splitlines() if l.strip()})
    rows = []
    recovered = 0
    for rel in files:
        commit = run("log", "--all", "-1", "--format=%H", "--", rel).strip()
        blob_line = run("ls-tree", "-r", commit, "--", rel).strip()
        blob = blob_line.split()[2] if blob_line else ""
        blob_size = int(run("cat-file", "-s", blob).strip()) if blob else 0
        path = REPO / rel
        cur = path.stat().st_size if path.exists() else -1
        status = "OK" if cur == blob_size else ("MISSING" if cur < 0 else "MISMATCH")
        if status != "OK":
            run("checkout", commit, "--", rel)
            recovered += 1
            status = "RECOVERED"
        rows.append((rel, commit[:8], blob_size, cur, status))

    with MANIFEST.open("w", newline="", encoding="utf-8-sig") as fp:
        w = csv.writer(fp)
        w.writerow(["file", "last_commit", "blob_size", "was_size", "status"])
        w.writerows(rows)

    print(f"Historical files under {TARGET}: {len(files)}")
    print(f"Recovered this run: {recovered}")
    for rel, c, bs, cs, st in rows:
        if st != "OK":
            print(f"  {st:10} {c} {Path(rel).name}")
    print(f"Manifest: {MANIFEST}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
