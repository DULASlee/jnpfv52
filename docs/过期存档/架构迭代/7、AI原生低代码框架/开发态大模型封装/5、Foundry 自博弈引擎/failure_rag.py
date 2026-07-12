"""Failure RAG - 历史失败案例的向量检索"""
from __future__ import annotations
import time
import json
from typing import Optional


class FailureRAG:
    """失败案例的简单向量存储(生产用 ChromaDB)"""

    def __init__(self, path: str = "./data/vector_store"):
        self.failures: list[dict] = []  # 简化:用 list 存,生产用向量数据库
        self.path = path

    async def add(self, failure: dict):
        """添加失败案例"""
        failure["id"] = len(self.failures) + 1
        self.failures.append(failure)

    async def query(self, query: str, top_k: int = 3) -> list[dict]:
        """查询相似失败案例(简化:基于关键词匹配)"""
        if not self.failures:
            return []

        # 简化:基于关键词重叠度
        query_words = set(query.lower().split())
        scored = []
        for f in self.failures:
            f_text = (f.get("requirement", "") + " " + f.get("failure_mode", "")).lower()
            f_words = set(f_text.split())
            overlap = len(query_words & f_words)
            scored.append((overlap, f))

        scored.sort(key=lambda x: x[0], reverse=True)
        return [f for _, f in scored[:top_k] if _ > 0]

    async def count(self) -> int:
        return len(self.failures)
