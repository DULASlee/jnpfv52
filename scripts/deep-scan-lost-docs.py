#!/usr/bin/env python3
"""Deep scan: git history, dangling objects, conversation archive, Cursor history."""
from __future__ import annotations

import json
import os
import re
import subprocess
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
TARGET = "docs/架构迭代/1、系统架构设计说明/"
ARCHIVE_DIRS = [
    Path(r"C:\Users\admin\.config\superpowers\conversation-archive\D--JNPF-v52"),
    Path(r"C:\Users\admin\.claude\projects\D--JNPF-v52"),
]
CURSOR_HISTORY = Path(r"C:\Users\admin\AppData\Roaming\Cursor\User\History")
OUT = REPO / "docs" / "_recovery_manifest" / "deep-scan-report.txt"


def run(*args: str) -> str:
    r = subprocess.run(
        ["git", "-C", str(REPO), "-c", "core.quotepath=false", *args],
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    return r.stdout if r.returncode == 0 else ""


def git_historical(prefix: str) -> set[str]:
    out = run("log", "--all", "--pretty=format:", "--name-only", "--", prefix)
    return {l.strip() for l in out.splitlines() if l.strip()}


def disk_files(prefix: str) -> set[str]:
    base = REPO / prefix
    if not base.exists():
        return set()
    files = set()
    for p in base.rglob("*"):
        if p.is_file():
            files.add(str(p.relative_to(REPO)).replace("\\", "/"))
    return files


def scan_archive_paths() -> set[str]:
    pat = re.compile(
        r"(?:docs[/\\]架构迭代[/\\]1、系统架构设计说明[/\\][^\s\"'`\\|<>]+\.(?:md|pdf))"
    )
    found: set[str] = set()
    for ad in ARCHIVE_DIRS:
        if not ad.exists():
            continue
        for jf in ad.rglob("*.jsonl"):
            try:
                text = jf.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            for m in pat.findall(text):
                found.add(m.replace("\\", "/"))
    return found


def scan_cursor_history() -> list[tuple[str, str]]:
    results: list[tuple[str, str]] = []
    if not CURSOR_HISTORY.exists():
        return results
    for entries in CURSOR_HISTORY.rglob("entries.json"):
        try:
            data = json.loads(entries.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            continue
        for e in data.values() if isinstance(data, dict) else []:
            if not isinstance(e, dict):
                continue
            resource = e.get("resource", "")
            if "架构迭代" in resource and "系统架构设计说明" in resource:
                results.append((resource, str(entries.parent / e.get("id", "?"))))
    return results


def scan_dangling_md() -> list[str]:
    fsck = subprocess.run(
        ["git", "-C", str(REPO), "fsck", "--lost-found", "--no-reflogs"],
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    hits: list[str] = []
    markers = ("架构深潜", "切面", "系统架构", "日志系统", "龙隐")
    for line in fsck.stdout.splitlines():
        if "dangling blob" not in line:
            continue
        blob = line.rsplit(" ", 1)[-1].strip()
        try:
            head = run("cat-file", "-p", blob).splitlines()[:3]
        except Exception:
            continue
        text = "\n".join(head)
        if any(m in text for m in markers):
            size = run("cat-file", "-s", blob).strip()
            hits.append(f"{blob} size={size} head={head[0][:60] if head else ''}")
    return hits


def main() -> None:
    os.chdir(REPO)
    OUT.parent.mkdir(parents=True, exist_ok=True)

    hist = git_historical(TARGET)
    disk = disk_files(TARGET)
    archive = scan_archive_paths()
    cursor = scan_cursor_history()
    dangling = scan_dangling_md()

    # Also check entire docs/ for missing git files
    docs_hist = git_historical("docs/")
    docs_disk = disk_files("docs/")
    docs_missing = sorted(docs_hist - docs_disk)

    lines = [
        "=== DEEP SCAN REPORT ===",
        f"Target: {TARGET}",
        f"Git historical: {len(hist)}",
        f"On disk: {len(disk)}",
        f"Missing from disk (git): {sorted(hist - disk)}",
        f"Extra on disk (not in git): {sorted(disk - hist)}",
        "",
        f"Archive mentions ({len(archive)}):",
        *[f"  {p}" for p in sorted(archive)],
        "",
        f"Archive only (not git, not disk): {sorted(archive - hist - disk)}",
        "",
        f"Cursor history hits ({len(cursor)}):",
        *[f"  {r} -> {f}" for r, f in cursor[:30]],
        "",
        f"Dangling architecture blobs ({len(dangling)}):",
        *[f"  {h}" for h in dangling[:20]],
        "",
        f"ALL docs/ missing from git history ({len(docs_missing)}):",
        *[f"  {p}" for p in docs_missing],
    ]
    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(OUT.read_text(encoding="utf-8"))


if __name__ == "__main__":
    main()
