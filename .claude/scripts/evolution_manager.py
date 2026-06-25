"""
JNPF V3.0 EvolutionManager — 进化引擎 (完整实现)
===================================================
离线闭环：记录异常 → 收集 → 人工仲裁 → 生效
核心原则：AI 绝不能自己修改规则文件。必须由人类工程师审核。

硬上限：防止进化文件熵增失控，超限自动归档。

CLI (V3.1):
  python evolution_manager.py record-anomaly --type pattern --description "..." --source "..."
  python evolution_manager.py generate-reminders
  python evolution_manager.py deduplicate
"""
import argparse
import json
import os
import re
import sys
from datetime import datetime
from pathlib import Path
from typing import Dict, List


class EvolutionManager:
    """进化引擎生命周期管理"""

    HARD_LIMITS = {
        "mistake-genes.md": 50,
        "coder-reminders.md": 30,
        "reviewer-metrics.md": 20,
        "coordination-log.md": 100,
    }

    def __init__(self, evolution_dir: str):
        self.dir = Path(evolution_dir)
        self.dir.mkdir(parents=True, exist_ok=True)
        self.anomalies_dir = self.dir / "anomalies"
        self.anomalies_dir.mkdir(parents=True, exist_ok=True)
        self.drafts_dir = self.dir / "drafts"
        self.drafts_dir.mkdir(parents=True, exist_ok=True)
        self._archived_dir = self.dir / "_archived"
        self._archived_dir.mkdir(parents=True, exist_ok=True)

    # ================================================================
    # 主流程：处理 Reviewer 报告
    # ================================================================

    def process_review_report(self, task_id: str, report: dict) -> dict:
        """处理 Reviewer 报告，驱动进化闭环"""
        findings = report.get("findings", [])
        hook_audit = report.get("hook_audit", {})
        coder_feedback = report.get("coder_feedback", {})
        metrics = report.get("metrics", {})

        # 1. 记录异常（recurrence_count >= 2）
        anomaly_count = 0
        for f in findings:
            if f.get("recurrence_count", 0) >= 2:
                self.record_anomaly(task_id, f)
                anomaly_count += 1

        # 2. Coder 提醒（即时生效，无需审核）
        reminder_count = 0
        for reminder in coder_feedback.get("reminders", []):
            self._append_coder_reminder(reminder)
            reminder_count += 1

        # 3. Hook 改进建议 → backlog
        hook_count = 0
        for suggestion in hook_audit.get("guard_improvement_suggestions", []):
            self._append_hook_backlog(suggestion)
            hook_count += 1

        # 4. 生成规则变更草案（需人工审核）
        draft_path = None
        if anomaly_count > 0:
            draft_path = self.generate_rule_change_draft(task_id)

        # 5. 更新 Reviewer 指标
        metrics_appended = False
        if metrics:
            self._update_reviewer_metrics(metrics)
            metrics_appended = True

        # 6. 强制硬上限
        self.enforce_limits()

        return {
            "anomalies_recorded": anomaly_count,
            "coder_reminders_updated": reminder_count,
            "rule_change_draft": draft_path,
            "human_review_required": draft_path is not None,
            "hook_backlog_updated": hook_count,
            "metrics_appended": metrics_appended,
        }

    # ================================================================
    # 异常记录
    # ================================================================

    def record_anomaly(self, task_id: str, finding: dict):
        """单条异常记录到 anomalies/{task_id}.json"""
        anomaly_file = self.anomalies_dir / f"{task_id}.json"

        record = {
            "timestamp": datetime.now().isoformat(),
            "task_id": task_id,
            "phase": finding.get("phase", "review"),
            "role": "reviewer",
            "rule_id": finding.get("rule_id", ""),
            "symptom": finding.get("message", ""),
            "root_cause": finding.get("why_hook_missed", "Unknown"),
            "suggested_fix": finding.get("fix_code", finding.get("fix_hint", "")),
            "recurrence_count": finding.get("recurrence_count", 1),
        }

        anomalies = []
        if anomaly_file.exists():
            try:
                anomalies = json.loads(anomaly_file.read_text(encoding="utf-8"))
            except Exception:
                anomalies = []
        anomalies.append(record)
        anomaly_file.write_text(json.dumps(anomalies, ensure_ascii=False, indent=2), encoding="utf-8")

    # ================================================================
    # Coder 提醒（即时生效，无需审核）
    # ================================================================

    def _append_coder_reminder(self, reminder: dict):
        """追加到 coder-reminders.md — 格式兼容状态机 _assemble_prompt 加载"""
        reminders_file = self.dir / "coder-reminders.md"

        entry = f"""
## {datetime.now().strftime('%Y-%m-%d')} | 触发: {reminder.get('trigger', 'Unknown')}

**来源 Finding**: {reminder.get('source_finding', 'Unknown')}

**检查清单**:
{chr(10).join(f'- [ ] {item}' for item in reminder.get('checklist', []))}

---
"""
        mode = "a" if reminders_file.exists() else "w"
        with open(reminders_file, mode, encoding="utf-8") as f:
            f.write(entry)

    # ================================================================
    # Hook 改进建议
    # ================================================================

    def _append_hook_backlog(self, suggestion: dict):
        """追加 Hook 改进建议到 hook-backlog.md"""
        backlog_file = self.dir / "hook-backlog.md"

        entry = f"""
## {datetime.now().strftime('%Y-%m-%d')} | 优先级: {suggestion.get('priority', 'MED')}

**目标文件**: {suggestion.get('guard_file', 'Unknown')}
**建议**: {suggestion.get('suggestion', '')}

---
"""
        mode = "a" if backlog_file.exists() else "w"
        with open(backlog_file, mode, encoding="utf-8") as f:
            f.write(entry)

    # ================================================================
    # 规则变更草案
    # ================================================================

    def generate_rule_change_draft(self, task_id: str) -> str:
        """从 anomalies 生成规则变更草案，返回文件路径"""
        anomaly_file = self.anomalies_dir / f"{task_id}.json"
        if not anomaly_file.exists():
            return ""

        try:
            anomalies = json.loads(anomaly_file.read_text(encoding="utf-8"))
        except Exception:
            return ""

        items = anomalies if isinstance(anomalies, list) else [anomalies]

        lines = [
            f"# 规则变更草案 — 任务 {task_id}",
            f"生成时间: {datetime.now().isoformat()}",
            f"异常数量: {len(items)}",
            "",
            "## 建议修改清单",
            "",
        ]

        for a in items:
            lines.extend([
                f"### {a['rule_id']} | {a.get('phase', 'review')} | {a.get('role', 'reviewer')}",
                f"- **症状**: {a['symptom']}",
                f"- **根因**: {a['root_cause']}",
                f"- **建议修复**: {a['suggested_fix']}",
                f"- **目标规则文件**: {self._map_to_rule_file(a['rule_id'])}",
                f"- **复发次数**: {a.get('recurrence_count', 1)}",
                "",
            ])

        lines.extend([
            "---",
            "## 人工审核区",
            "",
            "- [ ] 已审核所有建议修改清单",
            "- [ ] 已修改对应规则文件",
            "- [ ] 已提交 Git",
            "",
            "> ⚠️ **AI 绝不能自己修改规则文件。必须由人类工程师审核后手动修改。**",
        ])

        draft_path = self.drafts_dir / f"rule-change-{task_id}.md"
        draft_path.write_text("\n".join(lines), encoding="utf-8")

        return str(draft_path)

    # ================================================================
    # Reviewer 指标
    # ================================================================

    def _update_reviewer_metrics(self, metrics: dict):
        """追加 Reviewer 自评指标到 reviewer-metrics.md"""
        metrics_file = self.dir / "reviewer-metrics.md"

        entry = {
            "timestamp": datetime.now().isoformat(),
            "block_count": metrics.get("block_count", 0),
            "warn_count": metrics.get("warn_count", 0),
            "note_count": metrics.get("note_count", 0),
            "files_reviewed": metrics.get("files_reviewed", 0),
            "lines_reviewed": metrics.get("lines_reviewed", 0),
            "new_patterns": metrics.get("new_patterns", 0),
        }

        mode = "a" if metrics_file.exists() else "w"
        with open(metrics_file, mode, encoding="utf-8") as f:
            if f.tell() == 0:
                f.write("# Reviewer 指标记录\n\n")
            f.write(f"| {entry['timestamp']} | BLOCK:{entry['block_count']} | "
                    f"WARN:{entry['warn_count']} | NOTE:{entry['note_count']} | "
                    f"Files:{entry['files_reviewed']} | Lines:{entry['lines_reviewed']} | "
                    f"New:{entry['new_patterns']} |\n")

    # ================================================================
    # 去重 (V3.1 — MED-003 fix)
    # ================================================================

    def deduplicate_reminders(self) -> int:
        """移除 coder-reminders.md 中的重复条目，返出去重数量"""
        reminders_file = self.dir / "coder-reminders.md"
        if not reminders_file.exists():
            return 0

        content = reminders_file.read_text(encoding="utf-8")
        blocks = content.split("---")

        # 提取活跃条目块（跳过空块和头部）
        header = ""
        active_blocks = []
        for block in blocks:
            stripped = block.strip()
            if not stripped:
                continue
            if stripped.startswith("#") and not stripped.startswith("##"):
                header = stripped
                continue
            if stripped.startswith("##"):
                active_blocks.append(stripped)

        # 按内容规范化去重
        seen = set()
        unique_blocks = []
        removed = 0
        for block in active_blocks:
            # 规范化：移除日期行（不同时间触发的同一提醒）
            normalized = re.sub(
                r'## \d{4}-\d{2}-\d{2} \| 触发: ',
                '## DATE | 触发: ',
                block
            )
            if normalized in seen:
                removed += 1
                continue
            seen.add(normalized)
            unique_blocks.append(block)

        # 重写文件
        new_content = ""
        if header:
            new_content += header + "\n\n"
        new_content += "\n---\n".join(unique_blocks) + "\n---\n"
        reminders_file.write_text(new_content, encoding="utf-8")

        return removed

    # ================================================================
    # 硬上限强制
    # ================================================================

    def enforce_limits(self):
        """强制执行硬上限，超限自动归档"""
        for filename, limit in self.HARD_LIMITS.items():
            file_path = self.dir / filename
            if not file_path.exists():
                continue

            content = file_path.read_text(encoding="utf-8")
            entries = content.split("---")
            active_entries = [e.strip() for e in entries if e.strip() and e.strip().startswith("##")]

            if len(active_entries) <= limit:
                continue

            # 排序并保留最重要的
            active_entries.sort(key=lambda e: (
                e.count("recurrence_count") * 100 + len(e)
            ), reverse=True)

            keep = active_entries[:limit]
            archive_entries = active_entries[limit:]

            # 写回保留的
            new_content = "\n---\n".join(keep) + "\n---\n"
            file_path.write_text(new_content, encoding="utf-8")

            # 归档
            archive_month = datetime.now().strftime("%Y-%m")
            archive_dir = self._archived_dir / archive_month
            archive_dir.mkdir(parents=True, exist_ok=True)
            archive_path = archive_dir / f"{filename}.{datetime.now().strftime('%Y%m%d')}.md"
            archive_content = "\n---\n".join(archive_entries)
            archive_path.write_text(archive_content, encoding="utf-8")

    # ================================================================
    # Rule ID 映射
    # ================================================================

    def _map_to_rule_file(self, rule_id: str) -> str:
        """rule_id 映射到目标规则文件路径"""
        mapping = {
            "SEC-": ".claude/souls/coder/rules/sql-safety.md",
            "SQL-": ".claude/souls/coder/rules/sql-safety.md",
            "TRAP-": ".claude/souls/coder/rules/jnpf-expert-traps.md",
            "D2-": ".claude/souls/reviewer/rules/reviewer-discipline.md",
            "D3-": ".claude/souls/reviewer/rules/reviewer-discipline.md",
            "D4-": ".claude/souls/reviewer/rules/reviewer-discipline.md",
            "D5-": ".claude/souls/reviewer/rules/reviewer-discipline.md",
            "ARCH-": ".claude/souls/architect/rules/architecture-redlines.md",
            "R": ".claude/souls/architect/rules/architecture-redlines.md",
        }
        for prefix, path in mapping.items():
            if rule_id.startswith(prefix):
                return path
        return ".claude/souls/_shared/engineering-laws.md"


# ═══════════════════════════════════════════════════════════════
# CLI (V3.1 — HIGH-001 + MED-003 fix)
# ═══════════════════════════════════════════════════════════════

DEFAULT_EVOLUTION_DIR = str(Path(__file__).resolve().parent.parent / "evolution")


def main():
    parser = argparse.ArgumentParser(
        description="JNPF V3.1 Evolution Manager — rule evolution lifecycle"
    )
    parser.add_argument(
        "--evolution-dir", default=DEFAULT_EVOLUTION_DIR,
        help=f"Evolution directory path (default: {DEFAULT_EVOLUTION_DIR})"
    )
    subparsers = parser.add_subparsers(dest="command", help="Subcommands")

    # Subcommand: record-anomaly
    record_parser = subparsers.add_parser("record-anomaly", help="Record a new anomaly")
    record_parser.add_argument(
        "--type", required=True,
        choices=["security", "quality", "pattern", "trap"],
        help="Type of anomaly"
    )
    record_parser.add_argument("--description", required=True, help="Anomaly description")
    record_parser.add_argument("--source", required=True, help="Source file or review ID")
    record_parser.add_argument("--rule-id", default="UNKNOWN", help="Associated rule ID")
    record_parser.add_argument("--recurrence", type=int, default=1, help="Recurrence count")

    # Subcommand: generate-reminders
    subparsers.add_parser("generate-reminders", help="Generate coder reminders from anomalies")

    # Subcommand: deduplicate
    subparsers.add_parser("deduplicate", help="Remove duplicate entries from coder-reminders.md")

    # Subcommand: enforce-limits
    subparsers.add_parser("enforce-limits", help="Enforce hard limits and archive overflow")

    # Subcommand: process-review
    review_parser = subparsers.add_parser("process-review", help="Process a review report JSON")
    review_parser.add_argument("--task-id", required=True, help="Task ID")
    review_parser.add_argument("--report", required=True, help="Path to review report JSON")

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        sys.exit(1)

    mgr = EvolutionManager(args.evolution_dir)

    if args.command == "record-anomaly":
        finding = {
            "phase": "review",
            "rule_id": args.rule_id,
            "message": args.description,
            "why_hook_missed": f"Manual report from {args.source}",
            "fix_hint": f"Review {args.type} anomaly: {args.description}",
            "recurrence_count": args.recurrence,
        }
        task_id = re.sub(r'[^a-zA-Z0-9_-]', '_', args.source)[:40]
        mgr.record_anomaly(task_id, finding)
        print(f"[OK] Anomaly recorded: {task_id} ({args.type})")
        mgr.enforce_limits()

    elif args.command == "generate-reminders":
        # Scan anomalies and generate consolidated reminders
        count = 0
        if mgr.anomalies_dir.exists():
            for f in mgr.anomalies_dir.iterdir():
                if not f.suffix == '.json':
                    continue
                try:
                    anomalies = json.loads(f.read_text(encoding="utf-8"))
                except Exception:
                    continue
                items = anomalies if isinstance(anomalies, list) else [anomalies]
                for a in items:
                    if a.get("recurrence_count", 0) < 2:
                        continue
                    reminder = {
                        "trigger": a.get("symptom", "Unknown")[:80],
                        "source_finding": a.get("rule_id", "UNKNOWN"),
                        "checklist": [
                            f"Verify: {a.get('symptom', 'check')[:100]}",
                            f"Fix hint: {a.get('suggested_fix', 'review')[:100]}",
                        ],
                    }
                    mgr._append_coder_reminder(reminder)
                    count += 1
        print(f"[OK] Generated {count} coder reminders from anomalies")
        mgr.enforce_limits()

    elif args.command == "deduplicate":
        removed = mgr.deduplicate_reminders()
        print(f"[OK] Deduplication complete: {removed} duplicate entries removed")
        mgr.enforce_limits()

    elif args.command == "enforce-limits":
        mgr.enforce_limits()
        print("[OK] Hard limits enforced (overflow archived)")

    elif args.command == "process-review":
        report_path = Path(args.report)
        if not report_path.exists():
            print(f"[ERROR] Review report not found: {args.report}", file=sys.stderr)
            sys.exit(1)
        try:
            report = json.loads(report_path.read_text(encoding="utf-8"))
        except Exception as e:
            print(f"[ERROR] Failed to parse report: {e}", file=sys.stderr)
            sys.exit(1)
        result = mgr.process_review_report(args.task_id, report)
        print(json.dumps(result, indent=2, ensure_ascii=False))

    sys.exit(0)


if __name__ == "__main__":
    main()
