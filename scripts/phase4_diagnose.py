#!/usr/bin/env python3
"""
Phase4 pipeline 诊断 — Python 版

  python scripts/phase4_diagnose.py 209
  python scripts/phase4_diagnose.py 209 --json
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from lib.jnpf_client import diagnose, login, write_evidence


def main() -> None:
    parser = argparse.ArgumentParser(description="Phase4 IR/developer 链诊断")
    parser.add_argument("pipeline_id", type=int, help="pipeline ID")
    parser.add_argument("--json", action="store_true", help="仅输出 JSON")
    args = parser.parse_args()

    login()
    report = diagnose(args.pipeline_id)
    out_path = write_evidence(f"phase4-diagnose-{args.pipeline_id}.json", report)

    if args.json:
        print(json.dumps(report, ensure_ascii=False, indent=2))
    else:
        print(f"=== Pipeline {args.pipeline_id} 诊断 ===")
        print("\n[快照]")
        for s in report["snapshots"]:
            print(f"  {s['type']:20} {s['stability']:12} {s['id']}")
        print("\n[Skill 运行]")
        for r in report["skillRuns"]:
            print(f"  {r['skillId']:18} {r['status']:10} {r['error'][:120]}")
        print("\n[关键事件]", [t for t in report["eventTypes"] if t and any(
            x in t for x in ("Code", "Test", "Developer", "Codegen", "Arch", "Fragment")
        )])
        print(f"\n证据 → {out_path}")

        # 常见问题提示
        sk = next((s for s in report["snapshots"] if s["type"] == "IR0_Skeleton"), None)
        if sk and sk["stability"] == "draft":
            print("\n[WARN] IR0_Skeleton 仍为 draft -> 需 confirm-skeleton 后再跑 developer")
        for r in report["skillRuns"]:
            if r["status"] == "failed" and "DDL" in (r["error"] or ""):
                print("\n[WARN] DDL 列解析失败 -> simulate DDL 须 [F_xxx] 格式; 已修后端, 请 restart 后新建 pipeline")


if __name__ == "__main__":
    main()
