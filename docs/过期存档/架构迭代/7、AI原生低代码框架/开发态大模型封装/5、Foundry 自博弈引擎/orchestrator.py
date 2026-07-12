"""Foundry Orchestrator - 4 Agent 闭环主循环"""
from __future__ import annotations
import asyncio
import time
import json
from pathlib import Path
from typing import Optional

import yaml

from .agents.base import AgentContext
from .agents.attacker import AttackerAgent
from .agents.builder import BuilderAgent
from .agents.judge import JudgeAgent
from .agents.distiller import DistillerAgent
from .knowledge.pattern_store import PatternStore
from .knowledge.failure_rag import FailureRAG
from .metrics.tracker import MetricsTracker


class FoundryOrchestrator:
    """Foundry 自博弈主循环:Attacker → Builder → Judge → Distiller"""

    def __init__(self, config_path: str = "config.yaml"):
        # 加载配置
        with open(config_path) as f:
            self.config = yaml.safe_load(f)

        # 初始化 4 个 Agent
        self.attacker = AttackerAgent(None, self.config)
        self.builder = BuilderAgent(None, self.config)
        self.judge = JudgeAgent(None, self.config)
        self.distiller = DistillerAgent(
            None, self.config,
            pattern_store=None,  # 后设
            failure_rag=None,    # 后设
        )

        # 初始化知识存储
        self.pattern_store = PatternStore(
            db_path=self.config.get("knowledge", {}).get("pattern_db_path", ":memory:")
        )
        self.failure_rag = FailureRAG(
            path=self.config.get("knowledge", {}).get("vector_store_path", "./data/vector_store")
        )
        # 重新设置 distiller 的依赖
        self.distiller.pattern_store = self.pattern_store
        self.distiller.failure_rag = self.failure_rag
        self.attacker.failure_rag = self.failure_rag

        # 指标
        self.tracker = MetricsTracker()

        # 控制
        self.checkpoint_dir = Path("./checkpoints")
        self.checkpoint_dir.mkdir(exist_ok=True)

    # ============================================================
    # 主循环
    # ============================================================
    async def run(self):
        total = self.config.get("foundry", {}).get("total_iterations", 10000)
        checkpoint_interval = self.config.get("foundry", {}).get("checkpoint_interval", 100)
        report_interval = self.config.get("foundry", {}).get("report_interval", 10)
        convergence_window = self.config.get("foundry", {}).get("convergence_window", 100)
        convergence_threshold = self.config.get("foundry", {}).get("convergence_threshold", 0.95)

        print(f"🏭 Foundry 自博弈引擎启动")
        print(f"   总迭代: {total}")
        print(f"   收敛条件: 连续 {convergence_window} 次 pass rate >= {convergence_threshold}")
        print(f"   报告间隔: 每 {report_interval} 次")
        print(f"   检查点: 每 {checkpoint_interval} 次")
        print("─" * 60)

        self.tracker.start()
        convergence_start = 0

        for i in range(1, total + 1):
            iter_start = time.time()
            try:
                # 1. 准备 context
                context = AgentContext(
                    iteration=i,
                    industry="manufacturing",
                    past_failures=[],
                    kg_patterns=[p for p in self.pattern_store.patterns.values()][:5],
                )

                # 2. Attacker: 生成对抗需求
                attacker_result = await self.attacker.run(context)
                if not attacker_result.success:
                    print(f"\n[Iter {i}] Attacker failed: {attacker_result.error}")
                    continue

                # 3. Builder: 构建 SA 资产
                context.previous_result = attacker_result.data
                builder_result = await self.builder.run(context)
                if not builder_result.success:
                    print(f"\n[Iter {i}] Builder failed: {builder_result.error}")
                    continue

                # 4. Judge: 判定
                context.previous_result = builder_result.data
                judge_result = await self.judge.run(context)
                if not judge_result.success:
                    print(f"\n[Iter {i}] Judge failed: {judge_result.error}")
                    continue

                # 5. Distiller: 提炼 Pattern
                # 合并 Attacker + Builder + Judge 的输出给 Distiller
                full_result = {
                    **attacker_result.data,
                    **builder_result.data,
                    **judge_result.data,
                }
                context.previous_result = full_result
                distiller_result = await self.distiller.run(context)

                # 6. 记录指标
                iter_duration = int((time.time() - iter_start) * 1000)
                pattern_count = distiller_result.data.get("pattern_count_after", 0)
                self.tracker.record(
                    iteration=i,
                    passed=judge_result.data.get("passed", False),
                    failure_mode=attacker_result.data.get("failure_mode_name", "unknown"),
                    pattern_count=pattern_count,
                    duration_ms=iter_duration,
                )

                # 7. 进度条
                if i % report_interval == 0 or i == 1:
                    self.tracker.print_progress(i, total)

                # 8. 检查点
                if i % checkpoint_interval == 0:
                    await self._save_checkpoint(i)

                # 9. 收敛检查
                if i >= convergence_window:
                    recent_rate = self.tracker.get_pass_rate(convergence_window)
                    if recent_rate >= convergence_threshold:
                        print(f"\n\n🎉 收敛!最近 {convergence_window} 次 pass rate: {recent_rate:.1%}")
                        print(f"   提前结束于 iter {i}/{total}")
                        break

            except Exception as e:
                print(f"\n[Iter {i}] Unexpected error: {e}")
                continue

        # 收尾
        await self._final_report()

    # ============================================================
    # 检查点
    # ============================================================
    async def _save_checkpoint(self, iteration: int):
        checkpoint = {
            "iteration": iteration,
            "report": self.tracker.report(),
            "pattern_stats": self.pattern_store.get_stats(),
            "failure_count": await self.failure_rag.count(),
        }
        path = self.checkpoint_dir / f"checkpoint_{iteration}.json"
        with open(path, "w") as f:
            json.dump(checkpoint, f, indent=2, default=str)

    # ============================================================
    # 最终报告
    # ============================================================
    async def _final_report(self):
        report = self.tracker.report()
        pattern_stats = self.pattern_store.get_stats()
        failure_count = await self.failure_rag.count()

        print("\n\n" + "=" * 60)
        print("🏭 Foundry 自博弈训练完成 - 最终报告")
        print("=" * 60)
        print(f"总迭代次数:    {report['total_iterations']}")
        print(f"总耗时:        {report['elapsed_seconds']} 秒")
        print(f"迭代速度:      {report['iterations_per_second']} iter/s")
        print("─" * 60)
        print(f"整体 pass rate:      {report['pass_rate_overall']:.2%}")
        print(f"最近 100 次 pass rate:  {report['pass_rate_last_100']:.2%}")
        print(f"最近 1000 次 pass rate: {report['pass_rate_last_1000']:.2%}")
        print("─" * 60)
        print(f"知识图谱 Pattern:")
        print(f"  - 总数:     {pattern_stats['total']}")
        print(f"  - 已验证:   {pattern_stats['verified']}")
        print(f"  - 候选:     {pattern_stats['candidate']}")
        print(f"  - 平均评分: {pattern_stats['avg_score']}")
        print(f"  - 失败案例: {failure_count}")
        print("=" * 60)

        # 保存最终报告
        with open(self.checkpoint_dir / "final_report.json", "w") as f:
            json.dump({
                "metrics": report,
                "patterns": pattern_stats,
                "failure_count": failure_count,
            }, f, indent=2, default=str)
