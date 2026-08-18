"""Distiller Agent - 从博弈结果中提炼知识"""
from __future__ import annotations
from typing import Any
import time
import json

from .base import BaseAgent, AgentContext, AgentResult


class DistillerAgent(BaseAgent):
    """从 passed/failed 结果中提炼 Pattern,写入 kg_pattern"""

    def __init__(
        self,
        llm_client: Any,
        config: dict,
        pattern_store: Any,
        failure_rag: Any,
    ):
        super().__init__("Distiller", llm_client, config)
        self.pattern_store = pattern_store
        self.failure_rag = failure_rag

    async def run(self, context: AgentContext) -> AgentResult:
        start = time.time()
        try:
            attacker_data = context.previous_result or {}  # Attacker 输出(在最前)
            build_data = context.previous_result or {}      # Builder 输出
            judgment_data = context.previous_result or {}    # Judge 输出

            # 注意:实际需要从 context.history 拿全 4 个 Agent 的输出
            # 简化:这里假设 context 包含全部历史
            history = context.past_failures  # 重用这个字段存历史
            # 实际应该用 context.history,这里为了示例简化

            passed = judgment_data.get("passed", False)
            requirement = attacker_data.get("requirement_text", "")
            failure_mode = attacker_data.get("failure_mode_name", "")

            new_patterns = []
            if passed:
                # 1. 成功 → 提炼 verified Pattern
                pattern = await self._extract_pattern(build_data, failure_mode, "verified")
                if pattern:
                    await self.pattern_store.upsert(pattern)
                    new_patterns.append(pattern)
            else:
                # 2. 失败 → 加到 failure RAG(供 Attacker 后续参考)
                failure = {
                    "requirement": requirement,
                    "failure_mode": failure_mode,
                    "errors": judgment_data.get("errors", []),
                    "causal_graph": judgment_data.get("causal_graph"),
                    "timestamp": time.time(),
                }
                await self.failure_rag.add(failure)

                # 3. 失败也尝试提炼 candidate Pattern(部分正确的部分)
                candidate = await self._extract_pattern(build_data, failure_mode, "candidate")
                if candidate:
                    await self.pattern_store.upsert(candidate)
                    new_patterns.append(candidate)

            # 4. 应用遗忘机制(半衰期)
            deprecated = await self.pattern_store.apply_forgetting(half_life_days=180)

            return AgentResult(
                agent_name=self.name,
                success=True,
                data={
                    "new_patterns": new_patterns,
                    "deprecated_patterns": deprecated,
                    "pattern_count_after": await self.pattern_store.count(),
                },
                duration_ms=int((time.time() - start) * 1000),
            )
        except Exception as e:
            return AgentResult(
                agent_name=self.name,
                success=False,
                error=f"Distiller failed: {str(e)}",
                duration_ms=int((time.time() - start) * 1000),
            )

    async def _extract_pattern(self, build_data: dict, failure_mode: str, status: str) -> dict:
        """从 SA 资产中提取 Pattern"""
        # 用 LLM 提炼
        system_prompt = """你是知识提炼专家。从 SA 资产中提炼可复用的 Pattern。

返回 JSON:
{
  "type": "field_naming" | "decision_rule" | "state_machine" | "process_pattern",
  "industry": "manufacturing",
  "content": {...},  // 具体内容
  "tags": ["..."]
}"""
        user_prompt = f"""失败模式: {failure_mode}
状态: {status} (verified=通过,candidate=部分正确)

SA 资产:
{json.dumps(build_data.get('sa_output', {}), ensure_ascii=False)[:3000]}

提炼可复用的 Pattern:"""

        try:
            response = await self.call_llm(system_prompt, user_prompt, temperature=0.3)
            start = response.find("{")
            end = response.rfind("}") + 1
            if start != -1 and end > start:
                pattern = json.loads(response[start:end])
                pattern["status"] = status
                pattern["source"] = "self-play"
                return pattern
        except Exception:
            pass
        return None
