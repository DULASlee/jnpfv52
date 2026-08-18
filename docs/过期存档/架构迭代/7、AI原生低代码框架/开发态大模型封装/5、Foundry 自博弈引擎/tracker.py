"""Metrics Tracker - 跟踪 pass rate、Pattern 数量等指标"""
from __future__ import annotations
import time
import json
from typing import Optional


class MetricsTracker:
    """记录每次迭代的指标,生成训练报告"""

    def __init__(self):
        self.history: list[dict] = []
        self.start_time: Optional[float] = None

    def start(self):
        self.start_time = time.time()

    def record(
        self,
        iteration: int,
        passed: bool,
        failure_mode: str,
        pattern_count: int,
        duration_ms: int,
    ):
        self.history.append({
            "iteration": iteration,
            "passed": passed,
            "failure_mode": failure_mode,
            "pattern_count": pattern_count,
            "duration_ms": duration_ms,
            "at": time.time(),
        })

    def get_pass_rate(self, last_n: int = 100) -> float:
        """最近 N 次的 pass rate"""
        recent = self.history[-last_n:]
        if not recent:
            return 0.0
        return sum(1 for r in recent if r["passed"]) / len(recent)

    def get_overall_pass_rate(self) -> float:
        if not self.history:
            return 0.0
        return sum(1 for r in self.history if r["passed"]) / len(self.history)

    def report(self) -> dict:
        """生成当前训练报告"""
        if not self.start_time:
            return {}

        elapsed = time.time() - self.start_time
        return {
            "total_iterations": len(self.history),
            "pass_rate_overall": round(self.get_overall_pass_rate(), 4),
            "pass_rate_last_100": round(self.get_pass_rate(100), 4),
            "pass_rate_last_1000": round(self.get_pass_rate(1000), 4),
            "elapsed_seconds": int(elapsed),
            "iterations_per_second": round(len(self.history) / max(elapsed, 1), 3),
            "current_pattern_count": self.history[-1]["pattern_count"] if self.history else 0,
        }

    def print_progress(self, iteration: int, total: int):
        """打印进度条"""
        report = self.report()
        pct = iteration / total * 100
        bar_len = 40
        filled = int(bar_len * pct / 100)
        bar = "█" * filled + "░" * (bar_len - filled)
        print(
            f"\r[{bar}] {pct:.1f}% ({iteration}/{total}) | "
            f"Pass Rate (100): {report.get('pass_rate_last_100', 0):.1%} | "
            f"Patterns: {report.get('current_pattern_count', 0)} | "
            f"Speed: {report.get('iterations_per_second', 0):.2f} iter/s",
            end="", flush=True,
        )
