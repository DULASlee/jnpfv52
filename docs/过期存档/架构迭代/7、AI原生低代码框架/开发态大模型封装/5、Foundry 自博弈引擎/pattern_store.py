"""Pattern Store - kg_pattern 表的封装"""
from __future__ import annotations
import time
import math
from typing import Optional
import json


class PatternStore:
    """管理 kg_pattern 表的增删改查 + 评分"""

    def __init__(self, db_path: str = ":memory:"):
        # 简化:用内存实现(生产用 SQLAlchemy + SQL Server)
        self.patterns: dict[int, dict] = {}
        self.next_id = 1
        self.usage_log: list[dict] = []

    async def upsert(self, pattern: dict) -> int:
        """插入或更新 Pattern"""
        # 简化版:永远新建(生产应该按 type+industry 合并)
        pattern_id = self.next_id
        self.next_id += 1
        self.patterns[pattern_id] = {
            "id": pattern_id,
            "type": pattern.get("type"),
            "industry": pattern.get("industry", "manufacturing"),
            "content": pattern.get("content"),
            "tags": pattern.get("tags", []),
            "source": pattern.get("source", "self-play"),
            "status": pattern.get("status", "candidate"),
            "score": self._initial_score(pattern),
            "usage_count": 0,
            "success_count": 0,
            "created_at": time.time(),
            "last_used_at": None,
        }
        return pattern_id

    async def get_by_type(self, pattern_type: str, top_n: int = 5) -> list[dict]:
        """取某类型的 Top N Pattern"""
        candidates = [p for p in self.patterns.values() if p["type"] == pattern_type and p["score"] >= 0.6]
        candidates.sort(key=lambda p: p["score"], reverse=True)
        return candidates[:top_n]

    async def count(self) -> int:
        return len(self.patterns)

    async def log_usage(self, pattern_id: int, success: bool):
        """记录使用情况"""
        self.usage_log.append({
            "pattern_id": pattern_id,
            "success": success,
            "at": time.time(),
        })
        if pattern_id in self.patterns:
            p = self.patterns[pattern_id]
            p["usage_count"] += 1
            if success:
                p["success_count"] += 1
            p["score"] = self._recalculate_score(p)

    async def apply_forgetting(self, half_life_days: int = 180) -> list[int]:
        """应用半衰期:长时间未使用且评分低的 Pattern 标记 deprecated"""
        now = time.time()
        deprecated = []
        for p in self.patterns.values():
            if p.get("deprecated_at"):
                continue
            age_days = (now - p["created_at"]) / 86400
            recency = math.pow(0.5, age_days / half_life_days)
            # 重新计算 score
            new_score = self._recalculate_score(p, recency_override=recency)
            p["score"] = new_score
            if new_score < 0.3 and p["usage_count"] >= 5:
                p["deprecated_at"] = now
                deprecated.append(p["id"])
        return deprecated

    # ========================================================
    # 内部:评分
    # ========================================================
    def _initial_score(self, pattern: dict) -> float:
        return 0.5  # 初始分

    def _recalculate_score(self, p: dict, recency_override: Optional[float] = None) -> float:
        """评分公式:
        score = 0.30 * log(1+usage_count)
              + 0.25 * success_rate
              + 0.20 * source_weight
              + 0.15 * log(1+cross_industry_count)
              + 0.10 * recency_score
        """
        usage_count = p.get("usage_count", 0)
        success_rate = p.get("success_count", 0) / max(usage_count, 1)
        source_weight = {"human-created": 1.0, "ai-discovered": 0.6, "self-play": 0.4}.get(
            p.get("source", "self-play"), 0.4
        )
        cross_industry = 0  # 简化

        if recency_override is not None:
            recency = recency_override
        else:
            age_days = (time.time() - p["created_at"]) / 86400
            recency = math.pow(0.5, age_days / 180)

        return (
            0.30 * math.log(1 + usage_count) +
            0.25 * success_rate +
            0.20 * source_weight +
            0.15 * math.log(1 + cross_industry) +
            0.10 * recency
        )

    def get_stats(self) -> dict:
        if not self.patterns:
            return {"total": 0, "verified": 0, "candidate": 0, "avg_score": 0}

        verified = [p for p in self.patterns.values() if p.get("status") == "verified"]
        candidates = [p for p in self.patterns.values() if p.get("status") == "candidate"]
        avg_score = sum(p["score"] for p in self.patterns.values()) / len(self.patterns)

        return {
            "total": len(self.patterns),
            "verified": len(verified),
            "candidate": len(candidates),
            "avg_score": round(avg_score, 3),
        }
