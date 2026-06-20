"""Judge Agent - 验证 SA 资产质量"""
from __future__ import annotations
from typing import Any
import time
import json

from .base import BaseAgent, AgentContext, AgentResult


class JudgeAgent(BaseAgent):
    """判断 Builder 产出是否符合业务语义"""

    def __init__(self, llm_client: Any, config: dict):
        super().__init__("Judge", llm_client, config)

    async def run(self, context: AgentContext) -> AgentResult:
        start = time.time()
        try:
            build_data = context.previous_result or {}
            requirement_data = context.previous_result or {}  # Attacker 的输出

            # 1. 跑 Validator(后端 API)
            validator_url = self.config.get("backend", {}).get("validator_url", "http://localhost:3001")
            project_id = build_data.get("project_id")
            validation_resp = await self.http_client.post(
                f"{validator_url}/api/validate-all/{project_id}",
            )
            validation_resp.raise_for_status()
            validation = validation_resp.json()

            # 2. 跑业务语义测试(基于 Attacker 提供的 test_assertions)
            test_assertions = requirement_data.get("test_assertions", [])
            semantic_results = await self._run_semantic_tests(
                build_data, test_assertions,
            )

            # 3. 生成因果图(分析失败根因)
            causal_graph = self._build_causal_graph(validation, semantic_results, build_data)

            # 4. 综合判定
            all_passed = validation.get("passed", False) and all(r.get("passed", False) for r in semantic_results)
            errors = validation.get("errors", []) + [
                err for r in semantic_results if not r.get("passed") for err in r.get("errors", [])
            ]

            return AgentResult(
                agent_name=self.name,
                success=True,
                data={
                    "passed": all_passed,
                    "errors": errors,
                    "validation_result": validation,
                    "semantic_results": semantic_results,
                    "causal_graph": causal_graph,
                },
                duration_ms=int((time.time() - start) * 1000),
            )
        except Exception as e:
            return AgentResult(
                agent_name=self.name,
                success=False,
                error=f"Judge failed: {str(e)}",
                duration_ms=int((time.time() - start) * 1000),
            )

    async def _run_semantic_tests(self, build_data: dict, test_assertions: list) -> list:
        """基于 Attacker 提供的断言,跑业务语义测试"""
        results = []
        for assertion in test_assertions:
            # 简化版:用 LLM 判断 assertion 是否被满足
            system_prompt = """你是业务语义测试专家。判断 SA 资产是否满足给定断言。
返回 JSON: { "passed": bool, "errors": [string], "reason": string }"""
            user_prompt = f"""断言: {assertion}

SA 资产: {json.dumps(build_data.get('sa_output', {}), ensure_ascii=False)[:2000]}

判断上述 SA 资产是否满足断言。"""
            try:
                response = await self.call_llm(system_prompt, user_prompt, temperature=0.0)
                # 解析
                start = response.find("{")
                end = response.rfind("}") + 1
                if start != -1 and end > start:
                    result = json.loads(response[start:end])
                else:
                    result = {"passed": False, "errors": ["LLM 输出无法解析"]}
            except Exception as e:
                result = {"passed": False, "errors": [str(e)]}

            results.append({"assertion": assertion, **result})

        return results

    def _build_causal_graph(self, validation: dict, semantic: list, build_data: dict) -> dict:
        """构建失败因果图(简化版)"""
        if validation.get("passed", False) and all(r.get("passed", False) for r in semantic):
            return {"status": "all_passed", "nodes": [], "edges": []}

        # 收集所有失败原因,构建简单的因果链
        nodes = []
        edges = []

        for err in validation.get("errors", []):
            nodes.append({
                "id": f"v_{len(nodes)}",
                "type": "validation_error",
                "code": err.get("code"),
                "message": err.get("message"),
            })

        for r in semantic:
            if not r.get("passed"):
                nodes.append({
                    "id": f"s_{len(nodes)}",
                    "type": "semantic_failure",
                    "assertion": r.get("assertion"),
                    "reason": r.get("reason"),
                })

        # 简化:所有错误归因到根节点
        if nodes:
            edges.append({"from": "root", "to": nodes[0]["id"]})
            for i in range(1, len(nodes)):
                edges.append({"from": nodes[0]["id"], "to": nodes[i]["id"]})

        return {
            "status": "has_failures",
            "root_cause": nodes[0] if nodes else None,
            "nodes": nodes,
            "edges": edges,
        }
