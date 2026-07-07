#!/usr/bin/env python3
"""
D14 leave-simple Green path — Python 版（推荐）

  python scripts/phase4_green_path.py
  python scripts/phase4_green_path.py --pipeline-id 209
  python scripts/phase4_diagnose.py 209          # 失败时先诊断

依赖: pip install requests pycryptodome
产出: .claude/evidence/phase4-d14-green-path.json
"""
from __future__ import annotations

import argparse
import json
import os
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from lib.jnpf_client import (
    REPO_ROOT,
    diagnose,
    get_event_types,
    get_snapshots,
    is_ok,
    login,
    pick,
    probe_developer_api,
    run_developer,
    setup_ir2_locked,
    unwrap,
    wait_developer_green,
    write_evidence,
)
from jnpf_auth import api_request


def create_pipeline(name: str) -> int:
    req = (
        f"{name}：员工请假审批 leave-simple Green path，含 LeaveRequest 单表 MVP。"
        + "测" * 400
    )
    resp = api_request(
        "POST",
        "/api/studio/pipeline/execute/create",
        {"name": name, "userRequirement": req},
    )
    if not is_ok(resp):
        raise RuntimeError(f"create pipeline: {resp}")
    return int(pick(unwrap(resp), "pipelineId", "PipelineId"))


def assert_artifacts(tenant_id: str, project_id: str) -> tuple[bool, str]:
    backend = REPO_ROOT / "workspace" / "generated" / tenant_id / project_id / "backend"
    if not backend.exists():
        return False, f"missing {backend}"
    entity = list((backend / "Entitys").glob("*Entity.cs")) if (backend / "Entitys").exists() else []
    service = [
        p for p in (backend / "Services").glob("*Service.cs")
        if (backend / "Services").exists() and not p.name.endswith(".custom.cs")
    ] if (backend / "Services").exists() else []
    iface = list((backend / "Interfaces").glob("I*Service.cs")) if (backend / "Interfaces").exists() else []
    ok = len(entity) >= 1 and len(service) >= 1 and len(iface) >= 1
    return ok, f"entity={len(entity)} service={len(service)} iface={len(iface)} @ {backend}"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--pipeline-id", type=int, default=0)
    parser.add_argument("--skip-artifacts", action="store_true")
    args = parser.parse_args()
    timeout_s = float(os.environ.get("PHASE4_DEVELOPER_TIMEOUT_MS", "1800000")) / 1000

    login()
    steps: list[dict] = []

    def step(name: str, ok: bool, detail: str, **extra):
        steps.append({"name": name, "pass": ok, "detail": detail, **extra, "at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())})
        print(f"[{'PASS' if ok else 'FAIL'}] {name}: {detail}")

    pipeline_id = args.pipeline_id or create_pipeline(f"P4-Green-Py-{int(time.time())}")
    step("pipeline", True, f"pipelineId={pipeline_id}", pipelineId=pipeline_id)

    if not args.pipeline_id:
        setup_ir2_locked(pipeline_id)
        step("ir2-locked", True, "SystemDesignLocked + skeleton stable")
    else:
        d = diagnose(pipeline_id)
        sk = next((s for s in d["snapshots"] if s["type"] == "IR0_Skeleton"), {})
        sys_ok = next((s for s in d["snapshots"] if s["type"] == "IR2_SystemDesign"), {})
        ok = sk.get("stability") == "stable" and sys_ok.get("stability") == "locked"
        step("ir2-locked", ok, f"skeleton={sk.get('stability')}, systemDesign={sys_ok.get('stability')}")
        if not ok:
            sys.exit(1)

    probe_developer_api(pipeline_id)
    step("developer-api", True, "developer/status OK")

    run_id = run_developer(pipeline_id)
    step("developer-run", True, f"runId={run_id}")

    try:
        green = wait_developer_green(pipeline_id, timeout_s)
    except TimeoutError as e:
        d = diagnose(pipeline_id)
        write_evidence("phase4-d14-green-path-fail.json", d)
        step("developer-green", False, str(e))
        print("\n失败诊断（最近 developer/tester 错误）:")
        for r in d["skillRuns"]:
            print(f"  {r['skillId']}: {r['error']}")
        print(f"\n运行: python scripts/phase4_diagnose.py {pipeline_id}")
        sys.exit(1)

    if not green.get("ok"):
        step("developer-green", False, green.get("reason", "unknown"))
        sys.exit(1)
    step("developer-green", True, "promote + TestSuiteGenerated")

    status = unwrap(api_request("GET", f"/api/studio/skills/developer/{pipeline_id}/status"))
    cs = pick(status, "codegenStability", "CodegenStability")
    sb = pick(status, "sandboxBuildPassed", "SandboxBuildPassed")
    step("developer-status", cs == "stable" and sb is True, f"codegenStability={cs}, sandbox={sb}")

    snaps = get_snapshots(pipeline_id)
    codegen = next((s for s in snaps if pick(s, "fragmentType", "FragmentType") == "IR3_GeneratedCode"), {})
    test = next((s for s in snaps if pick(s, "fragmentType", "FragmentType") == "IR3_TestSuite"), {})
    tp = test.get("payload") or test.get("Payload")
    if isinstance(tp, str):
        tp = json.loads(tp)
    count = int((tp or {}).get("scenarioCount") or 0)
    step(
        "ir3-snapshots",
        pick(codegen, "stabilityState", "StabilityState") == "stable" and count >= 3,
        f"GeneratedCode stable, scenarios={count}",
    )

    if not args.skip_artifacts:
        diag = unwrap(api_request("GET", f"/api/studio/ir/{pipeline_id}/diagnostics"))
        tenant = str(pick(diag, "tenantId", "TenantId") or "0")
        ok, detail = assert_artifacts(tenant, str(pipeline_id))
        step("workspace-artifacts", ok, detail)

    all_pass = all(s["pass"] for s in steps)
    report = {
        "phase": "phase4-d14-green-path",
        "tool": "python",
        "pass": all_pass,
        "pipelineId": pipeline_id,
        "steps": steps,
        "eventTypes": sorted(set(get_event_types(pipeline_id))),
    }
    path = write_evidence("phase4-d14-green-path.json", report)
    print(f"\n证据 → {path}")
    print("[D14]", "PASS" if all_pass else "FAIL")
    sys.exit(0 if all_pass else 1)


if __name__ == "__main__":
    main()
