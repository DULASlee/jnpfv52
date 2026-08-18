"""
JNPF V3.0 端到端测试
=====================
C级快速通道 + S级完整流水线 + 安全熔断 + DAG并发调度
"""
import sys
import os
import json
import tempfile
from pathlib import Path
from datetime import datetime

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from state_machine import FuguPipeline, Phase, TaskLevel
from task_router import TaskRouter
from quality_gate_engine import QualityGateEngine
from concurrent_scheduler import ConcurrentScheduler
from evolution_manager import EvolutionManager


# ============================================================================
# Mock LLM Client
# ============================================================================

class MockLLMClient:
    """模拟 LLM 响应，按 phase 返回预定义合法 JSON"""

    RESPONSES = {
        "brainstorm": {
            "$schema": "fugu/architecture-v1",
            "options": [
                {"name": "方案A-事务脚本", "failure_boundary": "超过5状态时需重构",
                 "estimated_effort": "1天", "redlines_checked": ["R1", "R4", "R7", "R8"]},
                {"name": "方案B-DDD", "failure_boundary": "团队学习成本高",
                 "estimated_effort": "3天", "redlines_checked": ["R1", "R4", "R7", "R8"]},
                {"name": "方案C-不做/零代码", "failure_boundary": "业务方明确拒绝",
                 "estimated_effort": "0天", "redlines_checked": []},
            ],
            "recommendation": {"chosen_option": "方案A-事务脚本",
                               "reason": "时间紧，后续有重构窗口期"},
            "requirements": [{"id": "REQ-001", "source": "测试需求"}],
            "impact_assessment": {"change_type": "Entity", "exploration_depth": 2,
                                  "symbols_touched": 5, "truncated": False},
        },
        "build": {
            "$schema": "fugu/code-v1",
            "changed_files": [
                {"path": "Domain/Entities/TestEntity.cs", "operation": "create",
                 "lines_added": 30, "content_hash": "sha256:test"}
            ],
            "self_verification": {
                "build": {"command": "dotnet build", "result": "PASS"},
                "tests": {"command": "dotnet test", "result": "PASS", "coverage": "85%"},
            },
            "compliance_checklist": {
                "trap_2_mapster_audit": "PASS",
                "trap_8_updateable_tenant": "PASS",
                "r4_tenant_isolation": "PASS",
            },
        },
        "verify": {
            "$schema": "fugu/test-report-v1",
            "checks": [
                {"name": "dotnet-build", "result": "PASS", "exit_code": 0},
                {"name": "dotnet-test", "result": "PASS", "exit_code": 0},
                {"name": "acceptance-criteria", "result": "PASS"},
            ],
            "summary": {"total": 3, "passed": 3, "failed": 0, "skipped": 0},
            "verdict": "PASS",
            "coverage": {"line": "85%", "branch": "72%"},
        },
        "review": {
            "$schema": "fugu/review-report-v1",
            "findings": [
                {
                    "id": "REV-001", "level": "WARN", "confidence": "MED",
                    "dimension": "D4", "rule_id": "D4-LENGTH",
                    "file": "TestEntity.cs", "line": 1, "message": "Method > 50 lines",
                    "recurrence_count": 1,
                }
            ],
            "hook_audit": {
                "guard_coverage_verified": True,
                "missed_by_guard": [],
                "guard_improvement_suggestions": [],
            },
            "rule_evolution": {"new_patterns": [], "rule_updates": []},
            "coder_feedback": {"reminders": []},
            "metrics": {"block_count": 0, "warn_count": 1, "note_count": 0,
                        "files_reviewed": 1, "lines_reviewed": 30, "new_patterns": 0},
        },
        "report": "# Test Delivery Report\n\nAll checks passed.",
    }

    def call(self, system: str = "", user: str = "", **kwargs):
        """返回Mock响应 — 从 system prompt 中检测 phase"""
        for phase_name, response in self.RESPONSES.items():
            if phase_name in system.lower() or phase_name in user.lower():
                return json.dumps(response, ensure_ascii=False)
        return json.dumps({"status": "unknown_phase"}, ensure_ascii=False)


# ============================================================================
# Test 1: C级快速通道
# ============================================================================

def test_c_level():
    """C级任务: ALIGN→BUILD→VERIFY→END，跳过Brainstorm/Explore/Review"""
    print("\n=== Test 1: C-Level Fast Track ===")

    router = TaskRouter()
    level = router.classify("fix typo in README", ["README.md"])
    assert level == TaskLevel.C, f"Expected C, got {level}"

    pipeline = FuguPipeline(".", None)
    c_pipeline = pipeline.get_pipeline(TaskLevel.C)

    expected = [Phase.ALIGN, Phase.BUILD, Phase.VERIFY, Phase.END]
    assert c_pipeline == expected, f"Expected {expected}, got {c_pipeline}"
    assert Phase.BRAINSTORM not in c_pipeline, "C级不应包含Brainstorm"
    assert Phase.REVIEW not in c_pipeline, "C级不应包含Review"

    print(f"  C-Level pipeline: {'→'.join(p.value for p in c_pipeline)}")
    print("  [PASS]")


# ============================================================================
# Test 2: S级完整流水线
# ============================================================================

def test_s_level():
    """S级任务: 完整11阶段流水线"""
    print("\n=== Test 2: S-Level Full Pipeline ===")

    router = TaskRouter()
    level = router.classify(
        "new module with database migration and 12 files",
        list(range(12))
    )
    assert level == TaskLevel.S, f"Expected S, got {level}"

    pipeline = FuguPipeline(".", None)
    s_pipeline = pipeline.get_pipeline(TaskLevel.S)

    assert Phase.BRAINSTORM in s_pipeline
    assert Phase.REVIEW in s_pipeline
    assert len(s_pipeline) > len(pipeline.get_pipeline(TaskLevel.C)), \
        "S级流水线应长于C级"
    assert Phase.END == s_pipeline[-1], "最后阶段应为END"

    print(f"  S-Level pipeline: {'→'.join(p.value for p in s_pipeline)}")
    print(f"  Total phases: {len(s_pipeline)}")
    print("  [PASS]")


# ============================================================================
# Test 3: 状态机推进 + 熔断
# ============================================================================

def test_state_advancement_and_halt():
    """状态推进正常 + 连续失败触发熔断"""
    print("\n=== Test 3: State Advancement + Halt ===")

    pipeline = FuguPipeline(".", None)
    state = pipeline.init_state("HALT-TEST", TaskLevel.A)

    # 正常推进 ALIGN→BRAINSTORM→EXPLORE
    pipeline.advance_phase(state, True)
    assert state["current_phase"] == Phase.BRAINSTORM.value
    pipeline.advance_phase(state, True)
    assert state["current_phase"] == Phase.EXPLORE.value

    # 模拟连续失败
    state["current_phase"] = Phase.BUILD.value  # 重置到BUILD
    state["retry_count"] = 0
    for i in range(4):
        pipeline.advance_phase(state, False)
    assert state["current_phase"] == "halted", f"应触发熔断，实际: {state['current_phase']}"

    print("  [PASS]")


# ============================================================================
# Test 4: 安全扫描
# ============================================================================

def test_security_scanner():
    """安全扫描: 检测SQL注入 + 租户隔离 + 权限缺失"""
    print("\n=== Test 4: Security Scanner ===")

    from security_scanner import SecurityScanner
    import tempfile
    import os

    tests = [
        # (代码, 期望通过?, 描述)
        (
            'var sql = $"SELECT * FROM Users WHERE Name = \'" + userName + "\'";',
            False,
            "SQL injection via string interpolation"
        ),
        (
            'var users = db.Queryable<User>().Where(u => u.Name == name).ToList();',
            True,
            "Safe parameterized query"
        ),
        (
            'db.Updateable<OrderEntity>().ExecuteCommand();',
            False,
            "Updateable without Where"
        ),
        (
            'public class OrderService : IDynamicApiController { public Task Get() {} }',
            False,
            "IDynamicApiController without auth attribute"
        ),
    ]

    for code, expected_pass, desc in tests:
        with tempfile.NamedTemporaryFile(mode='w', suffix='.cs', delete=False, encoding='utf-8') as f:
            f.write(code)
            tmp = f.name

        scanner = SecurityScanner(os.path.dirname(tmp))
        passed, findings = scanner.scan_all([os.path.basename(tmp)])

        assert passed == expected_pass, \
            f"{desc}: expected pass={expected_pass}, got pass={passed}, findings={[(f.rule_id, f.message) for f in findings]}"
        os.unlink(tmp)

    print(f"  Tested {len(tests)} scenarios")
    print("  [PASS]")


# ============================================================================
# Test 5: Concurrent Scheduler
# ============================================================================

def test_concurrent_scheduler():
    """DAG调度: 拓扑排序 + 环检测 + 冲突检测"""
    print("\n=== Test 5: Concurrent Scheduler ===")

    scheduler = ConcurrentScheduler(None, 3)

    dag = {
        "nodes": ["ST-001", "ST-002", "ST-003", "ST-004"],
        "edges": [
            {"from": "ST-001", "to": "ST-002"},
            {"from": "ST-001", "to": "ST-003"},
            {"from": "ST-002", "to": "ST-004"},
            {"from": "ST-003", "to": "ST-004"},
        ]
    }

    # 初始就绪
    ready = scheduler._get_ready_subtasks(dag, set(), {})
    assert ready == ["ST-001"], f"Expected [ST-001], got {ready}"

    # ST-001完成后
    ready = scheduler._get_ready_subtasks(dag, {"ST-001"}, {})
    assert set(ready) == {"ST-002", "ST-003"}, f"Expected ST-002,ST-003, got {ready}"

    # 环检测
    dag_cycle = {
        "nodes": ["ST-001", "ST-002", "ST-003"],
        "edges": [
            {"from": "ST-001", "to": "ST-002"},
            {"from": "ST-002", "to": "ST-003"},
            {"from": "ST-003", "to": "ST-001"},
        ]
    }
    try:
        scheduler._get_ready_subtasks(dag_cycle, set(), {})
        assert False, "Should detect cycle"
    except ValueError:
        pass  # Expected

    # 伪并发执行
    plan = {
        "subtasks": [
            {"id": "ST-001", "name": "Entity", "dependencies": [], "output_files": ["Entity.cs"]},
            {"id": "ST-002", "name": "Service", "dependencies": ["ST-001"], "output_files": ["Service.cs"]},
            {"id": "ST-003", "name": "API", "dependencies": ["ST-001"], "output_files": ["Controller.cs"]},
        ],
        "dag": dag if False else {  # 使用简单DAG
            "nodes": ["ST-001", "ST-002", "ST-003"],
            "edges": [{"from": "ST-001", "to": "ST-002"}, {"from": "ST-001", "to": "ST-003"}],
        }
    }

    result = scheduler.run_concurrent_build("TEST-CONC", plan)
    assert result["results"]["ST-001"]["status"] == "SUCCESS"
    assert result["results"]["ST-002"]["status"] == "SUCCESS"
    assert result["results"]["ST-003"]["status"] == "SUCCESS"
    assert result["mode"] == "pseudo-concurrent"

    print(f"  Execution order: {result['execution_order']}")
    print(f"  Conflicts: {len(result['conflicts'])}")
    print("  [PASS]")


# ============================================================================
# Test 6: Evolution Manager
# ============================================================================

def test_evolution_manager():
    """进化引擎: 处理Reviewer报告 + 生成草案 + 硬上限"""
    print("\n=== Test 6: Evolution Manager ===")

    with tempfile.TemporaryDirectory() as tmp:
        evo = EvolutionManager(f"{tmp}/evolution")

        report = {
            "findings": [
                {
                    "rule_id": "TRAP-002", "level": "BLOCK", "recurrence_count": 3,
                    "message": "Mapster Adapt 未排除审计字段",
                    "why_hook_missed": "guard-reviewer 仅扫描字符串级 Adapt",
                    "fix_code": "dto.Adapt(entity, c => c.Ignore(x => x.CreateTime))",
                },
                {
                    "rule_id": "D4-LENGTH", "level": "WARN", "recurrence_count": 1,
                    "message": "方法过长",
                },
            ],
            "hook_audit": {
                "guard_improvement_suggestions": [
                    {"guard_file": "guard-reviewer.mjs",
                     "suggestion": "增加 Roslyn 语法树扫描", "priority": "HIGH"},
                ],
            },
            "coder_feedback": {
                "reminders": [
                    {"trigger": "使用Mapster.Adapt映射到Entity",
                     "checklist": ["检查 .Ignore(x => x.CreateTime)", "检查嵌套DTO递归Ignore"],
                     "source_finding": "TRAP-002"},
                ],
            },
            "metrics": {"block_count": 1, "warn_count": 1,
                        "lines_reviewed": 145, "new_patterns": 1},
        }

        result = evo.process_review_report("TEST-EVO", report)

        assert result["anomalies_recorded"] == 1  # 只记录 recurrence>=2
        assert result["coder_reminders_updated"] == 1
        assert result["human_review_required"] == True
        assert result["hook_backlog_updated"] == 1
        assert result["metrics_appended"] == True

        # 验证文件生成
        assert os.path.exists(f"{tmp}/evolution/anomalies/TEST-EVO.json")
        assert os.path.exists(f"{tmp}/evolution/coder-reminders.md")
        assert os.path.exists(result["rule_change_draft"])

        # 验证 coder-reminders 内容格式
        with open(f"{tmp}/evolution/coder-reminders.md", encoding="utf-8") as f:
            content = f.read()
            assert "Mapster.Adapt" in content
            assert "[ ]" in content  # checklist 格式

        # 验证草案含人工审核区
        with open(result["rule_change_draft"], encoding="utf-8") as f:
            draft = f.read()
            assert "人工审核" in draft
            assert "已审核" in draft
            assert "AI 绝不能自己修改规则文件" in draft

        # 硬上限测试
        for i in range(35):
            evo._append_coder_reminder({
                "trigger": f"test-{i}",
                "checklist": [f"check-{i}"],
                "source_finding": f"REV-{i}",
            })
        evo.enforce_limits()
        with open(f"{tmp}/evolution/coder-reminders.md", encoding="utf-8") as f:
            entries = [e for e in f.read().split("---") if "test-" in e]
            assert len(entries) <= 30, f"Expected <=30, got {len(entries)}"

        # 验证归档
        archived = os.listdir(f"{tmp}/evolution/_archived")
        assert len(archived) > 0

        print(f"  Draft: {result['rule_change_draft']}")
        print(f"  Archived months: {archived}")
        print("  [PASS]")


# ============================================================================
# Test 7: Rule ID Mapping
# ============================================================================

def test_rule_id_mapping():
    """规则ID → 文件路径映射"""
    print("\n=== Test 7: Rule ID Mapping ===")

    evo = EvolutionManager("/tmp")

    assert "sql-safety" in evo._map_to_rule_file("SEC-SQL-001")
    assert "expert-traps" in evo._map_to_rule_file("TRAP-002")
    assert "reviewer-discipline" in evo._map_to_rule_file("D2-TODO")
    assert "architecture-redlines" in evo._map_to_rule_file("ARCH-R1")
    assert "engineering-laws" in evo._map_to_rule_file("UNKNOWN-001")

    print("  [PASS]")


# ============================================================================
# Runner
# ============================================================================

if __name__ == "__main__":
    test_c_level()
    test_s_level()
    test_state_advancement_and_halt()
    test_security_scanner()
    test_concurrent_scheduler()
    test_evolution_manager()
    test_rule_id_mapping()

    print("\n" + "=" * 60)
    print("  V3.0 End-to-End Tests: ALL PASSED")
    print("=" * 60)
