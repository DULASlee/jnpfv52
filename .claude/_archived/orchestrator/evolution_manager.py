"""
JNPF V3.0 EvolutionManager — 进化引擎 (完整实现)
===================================================
离线闭环：记录异常 → 收集 → 人工仲裁 → 生效
核心原则：AI 绝不能自己修改规则文件。必须由人类工程师审核。

硬上限：防止进化文件熵增失控，超限自动归档。
"""
import json
import os
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
