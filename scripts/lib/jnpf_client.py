"""JNPF API 客户端（供 phase4 Python 脚本复用）"""
from __future__ import annotations

import json
import sys
import time
from pathlib import Path

SCRIPTS_DIR = Path(__file__).resolve().parent.parent
if str(SCRIPTS_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPTS_DIR))

from jnpf_auth import api_request, login  # noqa: E402

REPO_ROOT = SCRIPTS_DIR.parent
EVIDENCE_DIR = REPO_ROOT / ".claude" / "evidence"


def pick(obj: dict | None, *keys: str):
    if not isinstance(obj, dict):
        return None
    lower = {k.lower(): v for k, v in obj.items()}
    for k in keys:
        if k in obj:
            return obj[k]
        v = lower.get(k.lower())
        if v is not None:
            return v
    return None


def unwrap(resp: dict):
    data = resp.get("data")
    if isinstance(data, dict) and "data" in data:
        return data["data"]
    return data


def is_ok(resp: dict) -> bool:
    data = resp.get("data")
    if isinstance(data, dict) and data.get("code") == 200:
        return resp.get("status") == 200
    return bool(resp.get("ok"))


def wait_for(fn, label: str, timeout_s: float = 120, interval_s: float = 1.5):
    deadline = time.time() + timeout_s
    while time.time() < deadline:
        hit = fn()
        if hit:
            return hit
        time.sleep(interval_s)
    raise TimeoutError(f"timeout: {label}")


def get_events(pipeline_id: int) -> list:
    return unwrap(api_request("GET", f"/api/studio/ir/{pipeline_id}/events")) or []


def get_snapshots(pipeline_id: int) -> list:
    return unwrap(api_request("GET", f"/api/studio/ir/{pipeline_id}/snapshots")) or []


def get_event_types(pipeline_id: int) -> list[str]:
    return [pick(e, "eventType", "EventType") for e in get_events(pipeline_id)]


def simulate(pipeline_id: int, body: dict):
    resp = api_request("POST", f"/api/studio/ir/{pipeline_id}/simulate", body)
    if not is_ok(resp):
        raise RuntimeError(f"simulate {body.get('eventType')}: {resp}")
    return resp


def setup_ir1_stable(pipeline_id: int):
    simulate(pipeline_id, {"eventType": "SkeletonCreated"})
    simulate(pipeline_id, {"eventType": "EventSpecConfirmed", "fragmentId": "eventspec:BE-001"})
    resp = api_request(
        "POST",
        f"/api/studio/skills/pm/{pipeline_id}/confirm-skeleton",
        {"autoRunAnalyst": False},
    )
    if not is_ok(resp):
        raise RuntimeError(f"confirm-skeleton: {resp}")


def setup_ir2_clean(pipeline_id: int):
    simulate(pipeline_id, {"eventType": "ArchitectureDecisionRecorded"})
    simulate(pipeline_id, {"eventType": "DDLStabilized"})
    simulate(pipeline_id, {"eventType": "UIDesignStabilized"})


def wait_skill_terminal(pipeline_id: int, skill_id: str, timeout_s: float = 120):
    def poll():
        runs = unwrap(api_request("GET", f"/api/studio/skills/{pipeline_id}/runs")) or []
        run = next((r for r in runs if pick(r, "skillId", "SkillId") == skill_id), None)
        st = pick(run, "status", "Status")
        if st in ("completed", "failed", "cancelled"):
            return {"status": st, "error": pick(run, "errorMessage", "ErrorMessage") or ""}
        return None

    return wait_for(poll, f"skill {skill_id}", timeout_s)


def setup_ir2_locked(pipeline_id: int):
    setup_ir1_stable(pipeline_id)
    setup_ir2_clean(pipeline_id)
    resp = api_request("POST", f"/api/studio/skills/system-design/{pipeline_id}/run", {})
    if not is_ok(resp):
        raise RuntimeError(f"system-design run: {resp}")
    terminal = wait_skill_terminal(pipeline_id, "system-design-skill")
    types = get_event_types(pipeline_id)
    if terminal["status"] != "completed" or "SystemDesignLocked" not in types:
        raise RuntimeError(
            f"IR-2 lock failed: run={terminal['status']}, err={terminal['error']}"
        )


def probe_developer_api(pipeline_id: int):
    resp = api_request("GET", f"/api/studio/skills/developer/{pipeline_id}/status")
    if resp.get("status") == 404:
        raise RuntimeError("developer/status 404 — 请 start-dev.ps1 重启后端")
    return resp


def run_developer(pipeline_id: int):
    resp = api_request("POST", f"/api/studio/skills/developer/{pipeline_id}/run", {})
    if resp.get("status") == 404:
        raise RuntimeError("developer/run 404 — 请重启后端加载 DeveloperSkillsApiService")
    if not is_ok(resp):
        msg = unwrap(resp) or resp.get("data")
        raise RuntimeError(f"developer run HTTP {resp.get('status')}: {msg}")
    return pick(unwrap(resp), "runId", "RunId")


def wait_developer_green(pipeline_id: int, timeout_s: float = 1800):
    last_types: list[str] = []

    def poll():
        nonlocal last_types
        last_types = get_event_types(pipeline_id)
        if "CodegenFailed" in last_types:
            return {"ok": False, "reason": "CodegenFailed", "types": last_types}
        if "CodeGeneratedStablePromoted" in last_types and "TestSuiteGenerated" in last_types:
            return {"ok": True, "types": last_types}
        return None

    try:
        return wait_for(poll, "developer green", timeout_s, 2.0)
    except TimeoutError as e:
        e.last_types = last_types  # type: ignore[attr-defined]
        raise


def diagnose(pipeline_id: int) -> dict:
    """一键诊断 pipeline 开发链状态"""
    snaps = get_snapshots(pipeline_id)
    runs = unwrap(api_request("GET", f"/api/studio/skills/{pipeline_id}/runs")) or []
    types = get_event_types(pipeline_id)
    dev_runs = [
        {
            "skillId": pick(r, "skillId", "SkillId"),
            "status": pick(r, "status", "Status"),
            "error": (pick(r, "errorMessage", "ErrorMessage") or "")[:300],
        }
        for r in runs
        if pick(r, "skillId", "SkillId") in ("developer-skill", "tester-skill")
    ]
    return {
        "pipelineId": pipeline_id,
        "snapshots": [
            {
                "type": pick(s, "fragmentType", "FragmentType"),
                "stability": pick(s, "stabilityState", "StabilityState"),
                "id": pick(s, "fragmentId", "FragmentId"),
            }
            for s in snaps
        ],
        "skillRuns": dev_runs,
        "eventTypes": sorted(set(types)),
        "developerStatus": unwrap(api_request("GET", f"/api/studio/skills/developer/{pipeline_id}/status")),
    }


def write_evidence(name: str, data: dict) -> Path:
    EVIDENCE_DIR.mkdir(parents=True, exist_ok=True)
    path = EVIDENCE_DIR / name
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False), encoding="utf-8")
    return path
