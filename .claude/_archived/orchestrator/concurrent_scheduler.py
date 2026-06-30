"""
JNPF V3.0 ConcurrentScheduler — 并发调度器 (伪并发实现)
==========================================================
基于 plan.json 的 DAG 拓扑排序调度子任务。
伪并发模式：顺序执行 + 静态冲突检测（Git 分支操作降级）。

Session 3b 交付：完整 DAG 解析 + 子任务调度 + 冲突检测。
Session 4 升级：真并发 (ThreadPoolExecutor) + Git 分支隔离。
"""
import json
import os
from typing import Dict, List, Set
from pathlib import Path
from datetime import datetime


class ConcurrentScheduler:
    """并发调度器：基于 DAG 解析和调度子任务"""

    def __init__(self, pipeline=None, max_workers: int = 3):
        self.pipeline = pipeline
        self.max_workers = max_workers
        self.root = Path(pipeline.root) if pipeline else Path(".")

    # ================================================================
    # 主调度入口
    # ================================================================

    def run_concurrent_build(self, task_id: str, plan: dict) -> dict:
        """
        伪并发模式：按 DAG 拓扑排序顺序执行子任务。
        每个子任务执行: BUILD → VERIFY → REVIEW (B级以上)

        返回: {
            "results": {subtask_id: {"status": "SUCCESS"|"FAILED", ...}},
            "execution_order": [subtask_ids],
            "conflicts": [conflict_descriptions],
            "mode": "pseudo-concurrent"
        }
        """
        subtasks = {s["id"]: s for s in plan.get("subtasks", [])}
        dag = plan.get("dag", {"nodes": [], "edges": []})
        completed: Set[str] = set()
        results = {}
        order = []
        conflicts = []

        while len(completed) < len(subtasks):
            ready = self._get_ready_subtasks(dag, completed, subtasks)

            if not ready:
                remaining = set(subtasks.keys()) - completed
                raise ValueError(
                    f"DAG deadlock: no ready subtasks but {len(remaining)} remaining: {remaining}"
                )

            # 伪并发：顺序执行就绪子任务
            for sid in sorted(ready):  # 确定性顺序
                order.append(sid)
                result = self._run_subtask_pipeline(task_id, sid, subtasks[sid])
                results[sid] = result
                completed.add(sid)

                if result["status"] == "FAILED":
                    # 不阻塞其他就绪任务，但记录失败
                    continue

        # 静态冲突检测
        all_files = {}
        for sid, r in results.items():
            if r["status"] == "SUCCESS":
                for f in r.get("changed_files", []):
                    fpath = f["path"] if isinstance(f, dict) else str(f)
                    if fpath in all_files:
                        conflicts.append({
                            "file": fpath,
                            "modified_by": [all_files[fpath], sid],
                            "severity": "WARN",
                            "resolution": "需要人工检查合并"
                        })
                    all_files[fpath] = sid

        return {
            "results": results,
            "execution_order": order,
            "conflicts": conflicts,
            "mode": "pseudo-concurrent",
            "timestamp": datetime.now().isoformat(),
        }

    # ================================================================
    # DAG 就绪检测
    # ================================================================

    def _get_ready_subtasks(self, dag: dict, completed: Set[str],
                            subtasks: Dict[str, dict]) -> List[str]:
        """返回依赖已全部完成的子任务 ID 列表"""
        nodes = set(dag.get("nodes", []))
        edges = dag.get("edges", [])

        # 构建依赖图
        deps = {n: set() for n in nodes}
        for e in edges:
            deps[e["to"]].add(e["from"])

        # 环检测
        self._check_cycle(nodes, deps)

        ready = []
        for node in nodes:
            if node in completed:
                continue
            if deps[node].issubset(completed):
                ready.append(node)

        return ready

    def _check_cycle(self, nodes: Set[str], deps: Dict[str, Set[str]]):
        """DFS 环检测"""
        WHITE, GRAY, BLACK = 0, 1, 2
        color = {n: WHITE for n in nodes}

        def dfs(node):
            color[node] = GRAY
            for neighbor in deps.get(node, set()):
                if color[neighbor] == GRAY:
                    raise ValueError(f"DAG contains a cycle involving node '{node}' -> '{neighbor}'")
                if color[neighbor] == WHITE:
                    if dfs(neighbor):
                        return True
            color[node] = BLACK
            return False

        for node in nodes:
            if color[node] == WHITE:
                dfs(node)

    # ================================================================
    # 子任务流水线执行
    # ================================================================

    def _run_subtask_pipeline(self, task_id: str, subtask_id: str,
                              subtask: dict) -> dict:
        """
        执行单个子任务的完整流水线: BUILD → VERIFY → REVIEW
        会话 3b: Mock 实现（返回 SUCCESS）
        会话 4: 集成真实状态机调用
        """
        if self.pipeline:
            try:
                from state_machine import Phase
                state = {
                    "task_id": task_id,
                    "task_level": "A",
                    "current_phase": Phase.BUILD.value,
                    "current_subtask_id": subtask_id,
                    "requirement": subtask.get("name", ""),
                }

                # Build
                self.pipeline._assemble_prompt(Phase.BUILD, state)
                # Verify
                self.pipeline._assemble_prompt(Phase.VERIFY, state)
                # Review (A级以上)
                self.pipeline._assemble_prompt(Phase.REVIEW, state)

            except Exception as e:
                return {
                    "status": "FAILED",
                    "subtask_id": subtask_id,
                    "error": str(e),
                    "changed_files": [],
                }

        return {
            "status": "SUCCESS",
            "subtask_id": subtask_id,
            "subtask_name": subtask.get("name", subtask_id),
            "changed_files": subtask.get("output_files", []),
        }

    # ================================================================
    # 冲突报告
    # ================================================================

    def _generate_conflict_report(self, conflicts: List[dict]) -> str:
        """生成结构化冲突报告"""
        if not conflicts:
            return "No conflicts detected."

        lines = [
            "# Git 合并冲突报告",
            f"生成时间: {datetime.now().isoformat()}",
            f"冲突文件数: {len(conflicts)}",
            "",
        ]
        for c in conflicts:
            lines.append(f"## {c['file']}")
            lines.append(f"- 严重程度: {c['severity']}")
            lines.append(f"- 被分支修改: {', '.join(c['modified_by'])}")
            lines.append(f"- 解决建议: {c.get('resolution', '需要人工检查合并')}")
            lines.append("")

        return "\n".join(lines)
