"""Attacker Agent - 生成对抗需求,挖掘系统弱点"""
from __future__ import annotations
from typing import Any
import json
import time

from .base import BaseAgent, AgentContext, AgentResult


# =====================================================
# 失败模式库(种子知识)
# =====================================================
FAILURE_MODES = [
    {
        "id": "concurrent_report",
        "name": "并发报工",
        "description": "5 个工人同时报工同一张工单",
        "category": "concurrency",
        "test_requirements": "高并发场景,需要事务控制和锁机制",
    },
    {
        "id": "cross_shift",
        "name": "跨班次跨日",
        "description": "工人夜班跨 2 天报工",
        "category": "time_boundary",
        "test_requirements": "跨日时间处理,工时计算",
    },
    {
        "id": "substitute_material",
        "name": "替代料组合",
        "description": "10 种候选替代料,BOM 替代组爆炸",
        "category": "data_explosion",
        "test_requirements": "替代料组的笛卡尔积处理",
    },
    {
        "id": "lot_tracing",
        "name": "批次追溯链",
        "description": "1000 张工单共用 1 批钢卷,出问题要追溯",
        "category": "data_lineage",
        "test_requirements": "批次→工单的完整溯源链",
    },
    {
        "id": "scrap_recovery",
        "name": "报废料回收",
        "description": "报废品回收再用,库存账混乱",
        "category": "inventory_logic",
        "test_requirements": "报废/回收/复用的库存流水",
    },
    {
        "id": "phantom_bom",
        "name": "虚项 BOM",
        "description": "BOM 中虚项没展开,直接扣减虚项库存",
        "category": "bom_logic",
        "test_requirements": "虚项递归展开成子件再扣减",
    },
    {
        "id": "rework_loop",
        "name": "返工循环",
        "description": "质检驳回→工人返工→再次报工,状态机死循环",
        "category": "state_machine",
        "test_requirements": "返工状态机有界,不无限循环",
    },
    {
        "id": "multi_tenant_iso",
        "name": "多租户隔离",
        "description": "租户 A 的数据泄漏到租户 B",
        "category": "security",
        "test_requirements": "所有查询强制带 TenantId 过滤",
    },
    {
        "id": "decision_inconsistency",
        "name": "判定表跨事件不一致",
        "description": "事件 A 报废率>5% 让步,事件 B 报废率>5% 驳回",
        "category": "rule_consistency",
        "test_requirements": "跨事件判定表条件必须一致",
    },
    {
        "id": "backflush_timing",
        "name": "倒冲时机错位",
        "description": "质检驳回时倒冲已经执行,反冲不一致",
        "category": "transaction",
        "test_requirements": "倒冲必须在质检通过后才触发",
    },
]


class AttackerAgent(BaseAgent):
    """生成对抗需求,模拟真实世界中容易出问题的场景"""

    def __init__(self, llm_client: Any, config: dict, failure_rag: Any = None):
        super().__init__("Attacker", llm_client, config)
        self.failure_rag = failure_rag  # 历史失败 RAG

    async def run(self, context: AgentContext) -> AgentResult:
        start = time.time()
        try:
            # 1. 采样失败模式
            failure_mode = self._sample_failure_mode(context.iteration)

            # 2. RAG 查相似的历史失败(如果可用)
            similar_failures = []
            if self.failure_rag:
                similar_failures = await self.failure_rag.query(
                    failure_mode["description"],
                    top_k=3,
                )

            # 3. 构造 prompt,让 LLM 生成对抗需求
            system_prompt = self._build_system_prompt()
            user_prompt = self._build_user_prompt(failure_mode, similar_failures, context)

            response = await self.call_llm(
                system_prompt, user_prompt,
                temperature=self.config.get("agents", {}).get("attacker", {}).get("temperature", 0.9),
            )

            # 4. 解析 LLM 输出
            requirement = self._parse_response(response, failure_mode)

            return AgentResult(
                agent_name=self.name,
                success=True,
                data={
                    "requirement_id": context.iteration,
                    "failure_mode_id": failure_mode["id"],
                    "failure_mode_name": failure_mode["name"],
                    "requirement_text": requirement["text"],
                    "expected_weakness": requirement.get("expected_weakness", ""),
                    "test_assertions": requirement.get("test_assertions", []),
                },
                duration_ms=int((time.time() - start) * 1000),
            )
        except Exception as e:
            return AgentResult(
                agent_name=self.name,
                success=False,
                error=str(e),
                duration_ms=int((time.time() - start) * 1000),
            )

    # ========================================================
    # 内部:采样失败模式
    # ========================================================
    def _sample_failure_mode(self, iteration: int) -> dict:
        """基于迭代次数和已发现的失败模式,智能采样"""
        # 简化版:轮流 + 偶尔随机
        idx = iteration % len(FAILURE_MODES)
        return FAILURE_MODES[idx]

    # ========================================================
    # 内部:构造 prompt
    # ========================================================
    def _build_system_prompt(self) -> str:
        return """你是 Foundry 自博弈引擎的 Attacker Agent。

你的职责:生成对抗性业务需求,用来测试 SA 流水线 + Validator 在极端情况下的表现。

要求:
1. 需求要具体(行业、规模、特殊约束都要写清楚)
2. 重点暴露某一类失败模式(并发/时序/数据爆炸/状态机等)
3. 包含明确的"预期弱点"——你希望 SA 流水线在哪里出问题
4. 包含 1-3 条"测试断言"——后期 Judge Agent 用它来验证"""

    def _build_user_prompt(self, failure_mode: dict, similar_failures: list, context: AgentContext) -> str:
        similar_text = "\n".join([
            f"- {f.get('requirement', '')[:100]}" for f in similar_failures
        ]) if similar_failures else "无历史失败参考"

        kg_text = "\n".join([
            f"- {p.get('type')}: {json.dumps(p.get('content', {}))[:80]}"
            for p in context.kg_patterns[:5]
        ]) if context.kg_patterns else "无"

        return f"""【失败模式】
名称: {failure_mode['name']}
类别: {failure_mode['category']}
描述: {failure_mode['description']}
测试要点: {failure_mode['test_requirements']}

【类似历史失败】
{similar_text}

【当前知识图谱里的 Pattern(供参考)】
{kg_text}

【你的任务】
生成一个具体的、真实的、能暴露上述失败模式的需求描述。

返回 JSON 格式:
{{
  "text": "需求描述(3-5 句话,具体到行业/规模/特殊约束)",
  "expected_weakness": "你希望 SA 流水线在哪里出问题",
  "test_assertions": [
    "断言1: [具体可验证的检查,如 '跨班次报工的 ReportTime 应该正确处理跨日']",
    "断言2: ..."
  ]
}}"""

    def _parse_response(self, response: str, failure_mode: dict) -> dict:
        """解析 LLM 输出"""
        # 尝试提取 JSON
        try:
            # 找到第一个 { 和最后一个 }
            start = response.find("{")
            end = response.rfind("}") + 1
            if start != -1 and end > start:
                return json.loads(response[start:end])
        except json.JSONDecodeError:
            pass
        # Fallback: 用整个 response 当 text
        return {
            "text": response,
            "expected_weakness": failure_mode["name"],
            "test_assertions": [],
        }
