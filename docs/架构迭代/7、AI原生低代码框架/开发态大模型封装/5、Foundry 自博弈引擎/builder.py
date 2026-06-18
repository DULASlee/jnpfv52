"""Builder Agent - 调用 SAOrchestrator 构建 SA 资产"""
from __future__ import annotations
from typing import Any
import time

from .base import BaseAgent, AgentContext, AgentResult


class BuilderAgent(BaseAgent):
    """调用后端 SAOrchestrator,跑 9 步 SA 流水线生成 SA 资产"""

    def __init__(self, llm_client: Any, config: dict):
        super().__init__("Builder", llm_client, config)

    async def run(self, context: AgentContext) -> AgentResult:
        start = time.time()
        try:
            requirement_data = context.previous_result or {}
            requirement_text = requirement_data.get("requirement_text", "")

            # 调用后端 SAOrchestrator 跑 SA 流水线
            sa_orchestrator_url = self.config.get("backend", {}).get("sa_orchestrator_url", "http://localhost:3000")

            # 1. 创建项目
            project_resp = await self.http_client.post(
                f"{sa_orchestrator_url}/api/projects",
                json={
                    "tenantId": "foundry_test",
                    "requirementText": requirement_text,
                    "userId": f"foundry_iter_{context.iteration}",
                },
            )
            project_resp.raise_for_status()
            project = project_resp.json()
            project_id = project["projectId"]

            # 2. 触发 SA 流水线(同步跑完)
            run_resp = await self.http_client.post(
                f"{sa_orchestrator_url}/api/projects/{project_id}/run-sa",
                timeout=300.0,  # SA 流水线可能跑 5 分钟
            )
            run_resp.raise_for_status()
            sa_output = run_resp.json()

            # 3. 提取关键产物
            result_data = {
                "project_id": project_id,
                "sa_output": sa_output,
                "scope": sa_output.get("scope"),
                "dfd": sa_output.get("dfd"),
                "dict": sa_output.get("dict"),
                "decision_table": sa_output.get("decisionTable"),
                "state_machine": sa_output.get("stateMachine"),
                "validation_stats": sa_output.get("metadata", {}).get("validationStats", []),
                "total_retries": sa_output.get("metadata", {}).get("totalRetries", 0),
            }

            return AgentResult(
                agent_name=self.name,
                success=True,
                data=result_data,
                duration_ms=int((time.time() - start) * 1000),
            )
        except Exception as e:
            return AgentResult(
                agent_name=self.name,
                success=False,
                error=f"Builder failed: {str(e)}",
                duration_ms=int((time.time() - start) * 1000),
            )
