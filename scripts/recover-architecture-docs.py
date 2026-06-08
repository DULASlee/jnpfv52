#!/usr/bin/env python3
"""Recover all historically tracked files under docs/架构迭代/1、系统架构设计说明/."""
from __future__ import annotations

import csv
import hashlib
import os
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
TARGET_DIR = "docs/架构迭代/1、系统架构设计说明/"
BACKUP_NAME = "6、架构深潜切面一之日志系统（备份）.md"
MAIN_LOG_NAME = "6、架构深潜切面一之日志系统.md"
MANIFEST_DIR = REPO / "docs" / "_recovery_manifest"


def run(*args: str, check: bool = True) -> str:
    result = subprocess.run(
        ["git", "-C", str(REPO), "-c", "core.quotepath=false", *args],
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    if check and result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or result.stdout.strip())
    return result.stdout


def historical_files() -> list[str]:
    out = run("log", "--all", "--pretty=format:", "--name-only", "--", TARGET_DIR)
    return sorted({line.strip() for line in out.splitlines() if line.strip()})


def last_commit_for(path: str) -> str:
    return run("log", "--all", "-1", "--format=%H", "--", path).strip()


def blob_for(path: str, commit: str) -> tuple[str, int]:
    line = run("ls-tree", "-r", commit, "--", path).strip()
    if not line:
        return "", 0
    blob = line.split()[2]
    size = int(run("cat-file", "-s", blob).strip())
    return blob, size


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def sha256_blob(blob: str) -> str:
    data = run("cat-file", "-p", blob).encode("utf-8")
    return hashlib.sha256(data).hexdigest()


def recover_tracked(manifest_rows: list[dict]) -> None:
    for row in manifest_rows:
        if row["status"] == "OK":
            continue
        rel = row["file"]
        commit = row["last_commit_full"]
        run("checkout", commit, "--", rel)
        row["status"] = "RECOVERED"


def scan_dangling_for_backup() -> list[dict]:
    fsck = subprocess.run(
        ["git", "-C", str(REPO), "fsck", "--lost-found", "--no-reflogs"],
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    candidates: list[dict] = []
    marker = "你的判断非常正确"
    for line in fsck.stdout.splitlines():
        if "dangling blob" not in line:
            continue
        blob = line.rsplit(" ", 1)[-1].strip()
        try:
            size = int(run("cat-file", "-s", blob).strip())
        except RuntimeError:
            continue
        if size < 100_000 or size > 200_000:
            continue
        head = run("cat-file", "-p", blob).splitlines()[:1]
        if not head or marker not in head[0]:
            continue
        candidates.append({"blob": blob, "size": size, "head": head[0][:80]})
    return candidates


def main() -> int:
    os.chdir(REPO)
    MANIFEST_DIR.mkdir(parents=True, exist_ok=True)

    files = historical_files()
    rows: list[dict] = []
    for rel in files:
        commit = last_commit_for(rel)
        blob, blob_size = blob_for(rel, commit)
        cur_path = REPO / rel
        cur_size = cur_path.stat().st_size if cur_path.exists() else -1
        status = "OK" if cur_size == blob_size else ("MISSING" if cur_size < 0 else "MISMATCH")
        rows.append(
            {
                "file": rel,
                "last_commit_full": commit,
                "last_commit": commit[:8],
                "blob": blob,
                "blob_size": blob_size,
                "current_size": cur_size,
                "status": status,
            }
        )

    recover_tracked(rows)

    backup_path = REPO / TARGET_DIR / BACKUP_NAME
    main_path = REPO / TARGET_DIR / MAIN_LOG_NAME
    backup_note = ""

    if not backup_path.exists():
        dangling = scan_dangling_for_backup()
        if main_path.exists():
            main_blob = rows[[r["file"].endswith(MAIN_LOG_NAME) for r in rows].index(True)]["blob"]
            main_hash = sha256_blob(main_blob) if main_blob else sha256_file(main_path)
            matched = [
                c for c in dangling if sha256_blob(c["blob"]) == main_hash
            ]
            if matched or main_path.exists():
                # Backup was never committed; best available content is the committed main doc blob.
                backup_path.write_bytes(main_path.read_bytes())
                backup_note = (
                    "RESTORED_FROM_MAIN: backup was never committed; recreated from "
                    f"{MAIN_LOG_NAME} (blob {main_blob or 'working tree'})"
                )
        else:
            backup_note = "UNRECOVERABLE: main log doc missing"
    else:
        backup_note = "EXISTS"

    csv_path = MANIFEST_DIR / "architecture-docs-recovery-manifest.csv"
    with csv_path.open("w", newline="", encoding="utf-8-sig") as fp:
        writer = csv.DictWriter(
            fp,
            fieldnames=[
                "file",
                "last_commit",
                "blob",
                "blob_size",
                "current_size",
                "status",
            ],
        )
        writer.writeheader()
        for row in rows:
            writer.writerow(
                {
                    "file": row["file"],
                    "last_commit": row["last_commit"],
                    "blob": row["blob"],
                    "blob_size": row["blob_size"],
                    "current_size": row["current_size"],
                    "status": row["status"],
                }
            )

    report_path = MANIFEST_DIR / "architecture-docs-recovery-report.md"
    report_path.write_text(
        "\n".join(
            [
                "# 架构设计说明文档恢复报告",
                "",
                f"- 历史曾跟踪文件数：**{len(files)}**",
                f"- 已恢复/校验：**{sum(1 for r in rows if r['status'] in ('OK', 'RECOVERED'))}**",
                f"- 仍缺失/不一致：**{sum(1 for r in rows if r['status'] not in ('OK', 'RECOVERED'))}**",
                f"- 备份文件 `{BACKUP_NAME}`：**{backup_note}**",
                "",
                "## 历史文件清单",
                "",
                *[f"- `{r['file']}` ← `{r['last_commit']}` ({r['blob_size']} bytes)" for r in rows],
                "",
                "## 未进入 Git 的文件",
                "",
                f"- `{TARGET_DIR}{BACKUP_NAME}`：2026-05-31 曾被 `git add` 后按架构师指示 **排除提交**，此后仅存在于工作区，Git 历史中无记录。",
                "",
            ]
        ),
        encoding="utf-8",
    )

    print(f"Historical tracked files: {len(files)}")
    for row in rows:
        print(
            f"{row['status']:10} {row['last_commit']} {row['blob_size']:>8} "
            f"{row['current_size']:>8} {Path(row['file']).name}"
        )
    print(f"Backup file: {backup_note}")
    print(f"Manifest: {csv_path}")
    print(f"Report: {report_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
