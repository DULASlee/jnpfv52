"""Foundry 自博弈测试 - 验证 4 Agent 闭环和指标追踪"""
import asyncio
import pytest
from unittest.mock import AsyncMock, MagicMock

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent.parent))

from src.agents.base import AgentContext
from src.agents.attacker import AttackerAgent, FAILURE_MODES
from src.agents.builder import BuilderAgent
from src.agents.judge import JudgeAgent
from src.agents.distiller import DistillerAgent
from src.knowledge.pattern_store import PatternStore
from src.knowledge.failure_rag import FailureRAG
from src.metrics.tracker import MetricsTracker


@pytest.fixture
def config():
    return {
        "llm": {"provider": "openai", "model": "gpt-4"},
        "agents": {
            "attacker": {"temperature": 0.9},
            "builder": {"temperature": 0.1},
            "judge": {"temperature": 0.0},
            "distiller": {"temperature": 0.3},
        },
        "backend": {
            "sa_orchestrator_url": "http://localhost:3000",
            "validator_url": "http://localhost:3001",
        },
    }


@pytest.fixture
def pattern_store():
    return PatternStore()


@pytest.fixture
def failure_rag():
    return FailureRAG()


# ========================================================
# 1. Attacker 测试
# ========================================================
class TestAttackerAgent:
    @pytest.mark.asyncio
    async def test_failure_modes_loaded(self):
        assert len(FAILURE_MODES) >= 10
        assert any(m["id"] == "concurrent_report" for m in FAILURE_MODES)

    @pytest.mark.asyncio
    async def test_attacker_runs_with_mock_llm(self, config, failure_rag):
        agent = AttackerAgent(llm_client=None, config=config, failure_rag=failure_rag)
        # Mock LLM 返回
        agent.call_llm = AsyncMock(return_value='{"text": "测试需求", "expected_weakness": "并发", "test_assertions": ["断言1"]}')

        context = AgentContext(iteration=1)
        result = await agent.run(context)

        assert result.success
        assert "test" in result.data["requirement_text"] or result.data["requirement_text"] == "测试需求"
        assert "failure_mode_name" in result.data


# ========================================================
# 2. Builder 测试(集成 HTTP,跳过)
# ========================================================
class TestBuilderAgent:
    @pytest.mark.asyncio
    async def test_builder_handles_http_error(self, config):
        agent = BuilderAgent(llm_client=None, config=config)
        # 不启动真实 HTTP,直接看错误处理
        context = AgentContext(iteration=1, previous_result={"requirement_text": "test"})
        result = await agent.run(context)
        # 预期失败(因为 localhost:3000 没服务)
        assert not result.success
        assert "Builder failed" in result.error


# ========================================================
# 3. Pattern Store 测试
# ========================================================
class TestPatternStore:
    @pytest.mark.asyncio
    async def test_upsert_creates_pattern(self, pattern_store):
        pattern = {
            "type": "field_naming",
            "industry": "manufacturing",
            "content": {"commonFields": ["X"]},
            "source": "self-play",
            "status": "candidate",
        }
        pattern_id = await pattern_store.upsert(pattern)
        assert pattern_id == 1
        assert await pattern_store.count() == 1

    @pytest.mark.asyncio
    async def test_log_usage_updates_score(self, pattern_store):
        pattern_id = await pattern_store.upsert({"type": "x", "industry": "y", "content": {}, "source": "self-play"})
        # 3 次成功
        for _ in range(3):
            await pattern_store.log_usage(pattern_id, success=True)
        p = pattern_store.patterns[pattern_id]
        assert p["usage_count"] == 3
        assert p["success_count"] == 3
        assert p["score"] > 0.5  # 应该涨分

    @pytest.mark.asyncio
    async def test_forgetting_deprecates_low_score(self, pattern_store):
        # 创建一个旧的 pattern(手动设置时间)
        pattern_id = await pattern_store.upsert({"type": "x", "industry": "y", "content": {}, "source": "self-play"})
        # 模拟使用 5 次都失败
        for _ in range(5):
            await pattern_store.log_usage(pattern_id, success=False)
        # 手动把 created_at 设为 200 天前
        import time
        pattern_store.patterns[pattern_id]["created_at"] = time.time() - 200 * 86400
        # 应用遗忘
        deprecated = await pattern_store.apply_forgetting(half_life_days=180)
        assert pattern_id in deprecated


# ========================================================
# 4. Failure RAG 测试
# ========================================================
class TestFailureRAG:
    @pytest.mark.asyncio
    async def test_add_and_query(self, failure_rag):
        await failure_rag.add({
            "requirement": "5 个工人同时报工同一工单",
            "failure_mode": "并发报工",
            "errors": [],
        })
        await failure_rag.add({
            "requirement": "夜班跨日 24 小时报工",
            "failure_mode": "跨班次",
            "errors": [],
        })

        results = await failure_rag.query("并发报工 5 个工人", top_k=1)
        assert len(results) > 0
        assert "并发" in results[0]["failure_mode"] or "并" in results[0]["requirement"]


# ========================================================
# 5. Metrics Tracker 测试
# ========================================================
class TestMetricsTracker:
    def test_pass_rate_calculation(self):
        tracker = MetricsTracker()
        for i in range(100):
            tracker.record(i, passed=(i % 2 == 0), failure_mode="test", pattern_count=0, duration_ms=100)
        # 50% pass rate
        assert abs(tracker.get_overall_pass_rate() - 0.5) < 0.01
        assert abs(tracker.get_pass_rate(100) - 0.5) < 0.01

    def test_report(self):
        tracker = MetricsTracker()
        tracker.start()
        for i in range(10):
            tracker.record(i, passed=True, failure_mode="test", pattern_count=5, duration_ms=100)
        report = tracker.report()
        assert report["total_iterations"] == 10
        assert report["pass_rate_overall"] == 1.0
        assert report["current_pattern_count"] == 5


# ========================================================
# 6. 端到端(集成测试,需 mock 整个 4 Agent 链)
# ========================================================
class TestSelfPlayEnd2End:
    @pytest.mark.asyncio
    async def test_full_cycle_with_mocks(self, config, pattern_store, failure_rag):
        """验证 4 Agent 闭环:Attacker → Builder → Judge → Distiller"""
        # 1. 创建 4 个 Agent,全部 mock LLM
        attacker = AttackerAgent(None, config, failure_rag)
        builder = BuilderAgent(None, config)
        judge = JudgeAgent(None, config)
        distiller = DistillerAgent(None, config, pattern_store, failure_rag)

        # Mock 所有 LLM 调用
        attacker.call_llm = AsyncMock(return_value='{"text": "测试并发报工", "expected_weakness": "并发", "test_assertions": []}')
        # Builder 模拟网络错误
        builder.http_client.post = AsyncMock(side_effect=Exception("Mock network error"))
        # Judge 也 mock
        judge.http_client.post = AsyncMock(side_effect=Exception("Mock network error"))
        # Distiller 不依赖 LLM 时也能跑
        distiller._extract_pattern = AsyncMock(return_value=None)

        context = AgentContext(iteration=1)

        # 跑闭环
        attacker_result = await attacker.run(context)
        assert attacker_result.success

        context.previous_result = attacker_result.data
        builder_result = await builder.run(context)
        assert not builder_result.success  # 预期失败(因为 mock 了网络错误)

        # 模拟 Judge 直接用 attacker + builder 的结果
        judgment_data = {
            "passed": False,
            "errors": [{"code": "MOCK", "message": "Test failure"}],
            "causal_graph": {},
        }

        full = {**attacker_result.data, "errors": judgment_data["errors"]}
        context.previous_result = full
        distiller_result = await distiller.run(context)

        # 失败应该被加到 failure_rag
        assert await failure_rag.count() == 1
        # 验证:Distiller 成功完成
        assert distiller_result.success


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
