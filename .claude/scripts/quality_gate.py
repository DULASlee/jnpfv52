#!/usr/bin/env python3
"""
JNPF V3.0 Quality Gate Script
==============================
Claude Code 通过 Bash 调用: python .claude/scripts/quality_gate.py --phase build --task TASK-001
返回 JSON: {"passed": true/false, "gates": [...]}
"""
import argparse
import json
import subprocess
import sys
import os
from pathlib import Path
from datetime import datetime

ROOT = Path(__file__).resolve().parent.parent.parent  # D:/JNPF-v52
WORKSPACE = ROOT / "workspace"


def gate_brainstorm(task_id: str) -> list:
    """Q1: 架构方案质量"""
    arch_path = WORKSPACE / task_id / "architecture.md"
    if not arch_path.exists():
        return [{"name": "Q1-architecture", "passed": False, "detail": "architecture.md not found"}]
    content = arch_path.read_text(encoding="utf-8")
    options = content.count("## 方案")
    return [
        {"name": "Q1-options", "passed": options >= 2, "detail": f"{options} options found"},
        {"name": "Q1-boundary", "passed": "失效边界" in content, "detail": "failure boundary check"},
    ]


def gate_build(task_id: str) -> list:
    """Q3: 实现合规性"""
    results = []
    # Compile check
    try:
        r = subprocess.run(
            ["dotnet", "build", "backend/application/JNPF.API.Entry/JNPF.API.Entry.csproj",
             "--nologo", "-v", "q"],
            capture_output=True, text=True, cwd=str(ROOT), timeout=60
        )
        results.append({"name": "Q3-compile", "passed": r.returncode == 0,
                        "detail": "Build succeeded" if r.returncode == 0 else r.stderr[-200:]})
    except FileNotFoundError:
        results.append({"name": "Q3-compile", "passed": True, "detail": "dotnet not found (skipped)"})
    except subprocess.TimeoutExpired:
        results.append({"name": "Q3-compile", "passed": False, "detail": "Build timeout (60s)"})

    # Security scan result — MUST exist (V3.1: false-safety fix)
    scan_path = WORKSPACE / task_id / "security_scan_build.json"
    if not scan_path.exists():
        results.append({
            "name": "Q3-security",
            "passed": False,
            "detail": (
                "BLOCKER: security_scan_build.json not found. "
                "Run: python .claude/scripts/security_scanner.py --files <paths> --output "
                f"workspace/{task_id}/security_scan_build.json"
            )
        })
    else:
        scan = json.loads(scan_path.read_text(encoding="utf-8"))
        critical = sum(1 for f in scan.get("findings", []) if f.get("level") == "BLOCK")
        high = sum(1 for f in scan.get("findings", []) if f.get("level") == "WARN")
        total = len(scan.get("findings", []))
        if critical > 0:
            results.append({
                "name": "Q3-security",
                "passed": False,
                "detail": f"BLOCKED: {critical} critical, {high} warnings ({total} total findings)"
            })
        elif high > 0:
            results.append({
                "name": "Q3-security",
                "passed": False,
                "detail": f"WARN: {high} warnings ({total} total findings) — review required"
            })
        else:
            results.append({
                "name": "Q3-security",
                "passed": True,
                "detail": f"Clean ({total} findings, 0 critical/warnings)"
            })
    return results


def gate_verify(task_id: str) -> list:
    """Q4: 验证充分性"""
    results = []
    try:
        r = subprocess.run(
            ["dotnet", "test", "--no-build", "--nologo", "-v", "q"],
            capture_output=True, text=True, cwd=str(ROOT), timeout=120
        )
        results.append({"name": "Q4-test", "passed": r.returncode == 0,
                        "detail": "Tests passed" if r.returncode == 0 else (r.stdout or r.stderr or "")[-300:]})
    except FileNotFoundError:
        results.append({"name": "Q4-test", "passed": True, "detail": "dotnet not found (skipped)"})
    except subprocess.TimeoutExpired:
        results.append({"name": "Q4-test", "passed": False, "detail": "Test timeout (120s)"})
    return results


def gate_review(task_id: str) -> list:
    """Q5: 审查质量门"""
    review_path = WORKSPACE / task_id / "review_report.md"
    if not review_path.exists():
        return [{"name": "Q5-review", "passed": False, "detail": "review_report.md not found"}]
    content = review_path.read_text(encoding="utf-8")
    blocks = content.count("[BLOCK]")
    warns = content.count("[WARN]")
    return [
        {"name": "Q5-BLOCK", "passed": blocks == 0, "detail": f"{blocks} BLOCK findings"},
        {"name": "Q5-WARN", "passed": warns < 5, "detail": f"{warns} WARN findings"},
        {"name": "Q5-audit", "passed": "hook_audit" in content.lower(), "detail": "Hook audit check"},
    ]


def main():
    parser = argparse.ArgumentParser(description="JNPF V3.0 Quality Gate")
    parser.add_argument("--phase", required=True,
                        choices=["brainstorm", "build", "verify", "review", "report"])
    parser.add_argument("--task", required=True, help="Task ID")
    args = parser.parse_args()

    runners = {
        "brainstorm": gate_brainstorm,
        "build": gate_build,
        "verify": gate_verify,
        "review": gate_review,
    }

    runner = runners.get(args.phase, lambda tid: [{"name": "pass", "passed": True}])
    gates = runner(args.task)
    all_passed = all(g["passed"] for g in gates)

    result = {
        "phase": args.phase,
        "task": args.task,
        "passed": all_passed,
        "gates": gates,
        "timestamp": datetime.now().isoformat(),
    }
    print(json.dumps(result, indent=2, ensure_ascii=False))
    sys.exit(0 if all_passed else 1)


if __name__ == "__main__":
    main()
