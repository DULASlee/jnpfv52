"""Base Agent - 4 Agent 的共同基类"""
from __future__ import annotations
import abc
from typing import Any, Optional
from pydantic import BaseModel
import httpx
import os


class AgentContext(BaseModel):
    """Agent 运行上下文"""
    iteration: int
    industry: str = "manufacturing"
    past_failures: list[dict] = []
    kg_patterns: list[dict] = []
    previous_result: Optional[dict] = None


class AgentResult(BaseModel):
    """Agent 输出基类"""
    agent_name: str
    success: bool
    data: dict = {}
    error: Optional[str] = None
    duration_ms: int = 0


class BaseAgent(abc.ABC):
    """4 Agent 共同基类"""

    def __init__(
        self,
        name: str,
        llm_client: Any,
        config: dict,
        http_client: Optional[httpx.AsyncClient] = None,
    ):
        self.name = name
        self.llm_client = llm_client
        self.config = config
        self.http_client = http_client or httpx.AsyncClient(timeout=30.0)

    @abc.abstractmethod
    async def run(self, context: AgentContext) -> AgentResult:
        """Agent 主入口"""
        pass

    async def call_llm(self, system_prompt: str, user_prompt: str, temperature: float = 0.5) -> str:
        """统一 LLM 调用入口"""
        if self.config.get("llm", {}).get("provider") == "openai":
            from openai import AsyncOpenAI
            client = AsyncOpenAI(api_key=os.environ.get("OPENAI_API_KEY"))
            response = await client.chat.completions.create(
                model=self.config.get("llm", {}).get("model", "gpt-4"),
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt},
                ],
                temperature=temperature,
            )
            return response.choices[0].message.content
        else:
            raise NotImplementedError(f"LLM provider {self.config.get('llm', {}).get('provider')} not implemented")

    async def close(self):
        await self.http_client.aclose()
