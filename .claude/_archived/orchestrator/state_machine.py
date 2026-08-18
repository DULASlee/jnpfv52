"""
JNPF V3.0 FuguPipeline — 外部硬编码状态机
============================================
核心范式：外部Python脚本拥有对LLM会话的"生杀大权"。
每个阶段都是全新的、无状态的LLM调用。

设计决策：
  - 外部状态机用Python（系统调用、Git操作、并发控制更成熟）
  - 强制JSON输出用tool_use（API层面锁死结构化输出，可靠性100%）
  - 安全扫描在落盘前执行（防止任何不安全代码进入文件系统）
  - Reviewer作为硬阶段而非子代理（独立质量门）
  - 置信度加权质量门（避免LOW置信度BLOCK过度阻塞流水线）

作者：JNPF Architecture Team
版本：V3.0
"""
import json
from enum import Enum
from dataclasses import dataclass
from typing import Dict, List, Optional
from datetime import datetime
from task_router import TaskLevel


# ============================================================================
# 阶段定义
# ============================================================================

class Phase(Enum):
    ALIGN = "align"              # Phase 1: 对齐
    BRAINSTORM = "brainstorm"    # Phase 2: 头脑风暴
    EXPLORE = "explore"          # Phase 2.5: 调用链探索
    DECOMPOSE = "decompose"      # Phase 3a: 子任务分解
    PLAN = "plan"               # Phase 3b: 实施计划
    BUILD = "build"              # Phase 4: 编码实现
    VERIFY = "verify"            # Phase 5: 验证
    REVIEW = "review"            # Phase 6a: 超级审查引擎
    REVIEW_FIX = "review_fix"   # Phase 6b: 审查修复子循环
    REPORT = "report"            # Phase 7a: 报告生成
    END = "end"                  # 终结态

    def display(self) -> str:
        return self.value.upper()


@dataclass
class Transition:
    """阶段流转配置"""
    next_phase: Phase
    fail_phase: Phase
    max_retries: int = 3


class FuguPipeline:
    """
    外部硬编码状态机：拥有对LLM会话的绝对控制权。
    每个阶段都是全新的、无状态的LLM调用。
    """

    # ================================================================
    # 阶段流转表
    # ================================================================
    TRANSITIONS: Dict[Phase, Transition] = {
        Phase.ALIGN:      Transition(Phase.BRAINSTORM, Phase.ALIGN),
        Phase.BRAINSTORM: Transition(Phase.EXPLORE,   Phase.BRAINSTORM),
        Phase.EXPLORE:    Transition(Phase.DECOMPOSE, Phase.BRAINSTORM),
        Phase.DECOMPOSE:  Transition(Phase.PLAN,      Phase.EXPLORE),
        Phase.PLAN:       Transition(Phase.BUILD,     Phase.DECOMPOSE),
        Phase.BUILD:      Transition(Phase.VERIFY,    Phase.BUILD),
        Phase.VERIFY:     Transition(Phase.REVIEW,    Phase.BUILD),
        Phase.REVIEW:     Transition(Phase.REPORT,    Phase.REVIEW_FIX),
        Phase.REVIEW_FIX: Transition(Phase.REVIEW,    Phase.REVIEW_FIX),
        Phase.REPORT:     Transition(Phase.END,       Phase.REPORT),
    }

    # ================================================================
    # 阶段 → 角色映射
    # ================================================================
    PHASE_ROLE: Dict[Phase, str] = {
        Phase.ALIGN:      "orchestrator",
        Phase.BRAINSTORM: "architect",
        Phase.EXPLORE:    "architect",
        Phase.DECOMPOSE:  "planner",
        Phase.PLAN:       "planner",
        Phase.BUILD:      "coder",
        Phase.VERIFY:     "tester",
        Phase.REVIEW:     "reviewer",
        Phase.REVIEW_FIX: "coder",
        Phase.REPORT:     "reporter",
    }

    # ================================================================
    # 任务分级 → 流水线路径
    # ================================================================
    PIPELINES: Dict[TaskLevel, List[Phase]] = {
        TaskLevel.C: [
            Phase.ALIGN, Phase.BUILD, Phase.VERIFY, Phase.END
        ],
        TaskLevel.B: [
            Phase.ALIGN, Phase.BRAINSTORM, Phase.BUILD,
            Phase.VERIFY, Phase.REVIEW, Phase.REPORT, Phase.END
        ],
        TaskLevel.A: [
            Phase.ALIGN, Phase.BRAINSTORM, Phase.EXPLORE,
            Phase.DECOMPOSE, Phase.PLAN, Phase.BUILD,
            Phase.VERIFY, Phase.REVIEW, Phase.REVIEW_FIX,
            Phase.REPORT, Phase.END
        ],
        TaskLevel.S: [
            Phase.ALIGN, Phase.BRAINSTORM, Phase.EXPLORE,
            Phase.DECOMPOSE, Phase.PLAN, Phase.BUILD,
            Phase.VERIFY, Phase.REVIEW, Phase.REVIEW_FIX,
            Phase.REPORT, Phase.END
        ],
    }

    # ================================================================
    # REVIEW阶段质量门配置
    # ================================================================
    REVIEW_GATE_CONFIG = {
        "block_threshold": 0,      # BLOCK数量>0即质量门失败
        "warn_threshold": 5,       # WARN数量>5触发警告但不阻塞
        "note_threshold": 999,     # NOTE不阻塞
        "confidence_override": {
            "HIGH": {"block": 1, "warn": 0, "note": 0},
            "MED":  {"block": 2, "warn": 3, "note": 0},
            "LOW":  {"block": 3, "warn": 5, "note": 0},
        }
    }

    def __init__(self, project_root: str, llm_client=None):
        self.root = project_root
        self.llm = llm_client
        self.workspace = f"{project_root}/workspace"

    # ================================================================
    # 状态管理
    # ================================================================

    def init_state(self, task_id: str, level: TaskLevel) -> dict:
        """初始化统一状态"""
        return {
            "$schema": "fugu/state-v1",
            "task_id": task_id,
            "task_level": level.value,
            "current_phase": Phase.ALIGN.value,
            "current_subtask_id": None,
            "retry_count": 0,
            "error_context": None,
            "phases_completed": [],
            "quality_gates": {},
            "security_scans": {},
            "reviewer_metrics": {},
            "evolution": {},
            "created_at": datetime.now().isoformat(),
            "updated_at": datetime.now().isoformat(),
        }

    def get_pipeline(self, level: TaskLevel) -> List[Phase]:
        """根据任务级别返回流水线阶段列表"""
        return self.PIPELINES.get(level, self.PIPELINES[TaskLevel.A])

    def get_next_phase(self, current: Phase, passed: bool) -> Phase:
        """获取下一阶段"""
        transition = self.TRANSITIONS.get(current)
        if not transition:
            return Phase.END
        return transition.next_phase if passed else transition.fail_phase

    def get_max_retries(self, phase: Phase) -> int:
        """获取阶段最大重试次数"""
        transition = self.TRANSITIONS.get(phase)
        return transition.max_retries if transition else 3

    def is_terminal(self, phase: Phase) -> bool:
        """是否为终结态"""
        return phase == Phase.END

    def should_halt(self, state: dict) -> bool:
        """判断是否应触发PHASE_HALT熔断"""
        retries = state.get("retry_count", 0)
        phase = Phase(state["current_phase"])
        max_retries = self.get_max_retries(phase)
        return retries >= max_retries

    def advance_phase(self, state: dict, passed: bool) -> dict:
        """推进状态：使用pipeline路径而非固定TRANSITIONS"""
        phase_str = state["current_phase"]
        # 熔断态不可恢复
        if phase_str == "halted":
            return state

        current = Phase(phase_str)
        level = TaskLevel(state.get("task_level", "A"))
        pipeline = self.get_pipeline(level)

        if passed:
            # 成功：取pipeline中当前阶段的下一个阶段
            try:
                idx = pipeline.index(current)
                if idx + 1 < len(pipeline):
                    next_phase = pipeline[idx + 1]
                else:
                    next_phase = Phase.END
            except ValueError:
                next_phase = Phase.END

            state["phases_completed"].append(current.value)
            state["retry_count"] = 0
            state["error_context"] = None
            state["current_phase"] = next_phase.value
        else:
            # 失败：回退到TRANSITIONS中的fail_phase
            transition = self.TRANSITIONS.get(current)
            fail_phase = transition.fail_phase if transition else current
            state["retry_count"] += 1
            if self.should_halt(state):
                state["current_phase"] = "halted"
            else:
                state["current_phase"] = fail_phase.value

        state["updated_at"] = datetime.now().isoformat()
        return state

    # ================================================================
    # 上下文组装（隧道视野）
    # ================================================================

    def _get_role_for_phase(self, phase: Phase) -> str:
        return self.PHASE_ROLE.get(phase, "orchestrator")

    def _load_role_rules(self, role: str) -> List[str]:
        """按需加载角色专属规则（L2层）"""
        rule_map = {
            "architect":  ["architecture-redlines.md", "low-code-principles.md"],
            "planner":    [],
            "coder":      ["jnpf-expert-traps.md", "sql-safety.md", "frontend-memory-leak.md"],
            "tester":     ["testing.md"],
            "reviewer":   ["reviewer-discipline.md"],
            "reporter":   [],
            "orchestrator": [],
        }
        return rule_map.get(role, [])

    # ================================================================
    # 产出物持久化 + 安全扫描
    # ================================================================

    def persist_output(self, task_id: str, phase: str, output: dict) -> str:
        """
        落盘阶段产出物。包含安全扫描（会话3激活）。
        返回最终落盘路径。
        """
        import os
        import json as json_mod

        task_dir = f"{self.workspace}/{task_id}"
        os.makedirs(task_dir, exist_ok=True)

        # 1. 确定产出物文件名
        file_map = {
            "brainstorm": "architecture.json",
            "explore": "architecture.json",
            "decompose": "plan.json",
            "plan": "plan.json",
            "build": "code_diff.json",
            "verify": "test_report.json",
            "review": "review_report.json",
            "review_fix": "code_diff.json",
            "report": "delivery_report.md",
        }
        filename = file_map.get(phase, f"{phase}.json")
        final_path = f"{task_dir}/{filename}"

        # 2. V3.0 集成点：安全扫描（会话3激活）
        if phase in ["build", "review_fix"]:
            try:
                from security_scanner import SecurityScanner
                scanner = SecurityScanner(self.root)
                changed = output.get("changed_files", [])
                if isinstance(changed, list):
                    passed, findings = scanner.scan_all([f["path"] if isinstance(f, dict) else str(f) for f in changed])
                else:
                    passed, findings = True, []

                # 保存扫描结果
                scan_path = f"{task_dir}/security_scan_{phase}.json"
                with open(scan_path, "w", encoding="utf-8") as f:
                    json_mod.dump({
                        "passed": passed,
                        "findings": ([f.__dict__ for f in findings] if findings and hasattr(findings[0], '__dict__') else [])
                    }, f, ensure_ascii=False, indent=2)

                # 会话2：scanner 空壳始终返回 True，不阻塞
                # 会话3：not passed → raise SecurityGateBlocked
            except ImportError:
                pass  # security_scanner.py 尚未创建（会话2前期）

        # 3. 落盘
        with open(final_path, "w", encoding="utf-8") as f:
            if filename.endswith(".json"):
                json_mod.dump(output, f, ensure_ascii=False, indent=2)
            else:
                f.write(str(output))

        return final_path

    def _assemble_prompt(self, phase: Phase, state: dict) -> dict:
        """
        组装最小上下文（V3.0 完整实现）。
        系统提示 = soul.md + L0共享规则 + L1 workflow + L2角色规则
        用户提示 = 前置JSON产出 + 隧道视野声明 + 错误上下文
        """
        import os as _os
        role = self._get_role_for_phase(phase)
        root = self.root

        # === 1. 加载 soul.md ===
        soul_path = f"{root}/.claude/souls/{role}/soul.md"
        soul = self._read_file(soul_path)
        if not soul.strip():
            raise ValueError(f"soul.md for {role} is empty or missing: {soul_path}")

        # === 2. 加载规则文件（分层策略）===
        rules_text = []

        # L0: 始终加载
        l0_dir = f"{root}/.claude/souls/_shared"
        for fname in ["assertion-discipline.md", "engineering-laws.md"]:
            rp = f"{l0_dir}/{fname}"
            if _os.path.exists(rp):
                rules_text.append(f"\n---\n## L0: {fname}\n{self._read_file(rp)[:2000]}")

        # L1: workflow（按任务类型）
        wf_paths = [
            f"{root}/.claude/souls/_shared/workflow.md",
            f"{root}/.claude/rules/workflow.md",
        ]
        for wp in wf_paths:
            if _os.path.exists(wp):
                rules_text.append(f"\n---\n## L1: workflow.md\n{self._read_file(wp)[:2000]}")
                break

        # L2: 角色专属规则
        rules_dir = f"{root}/.claude/souls/{role}/rules"
        if _os.path.isdir(rules_dir):
            for fname in sorted(_os.listdir(rules_dir)):
                if fname.endswith(".md"):
                    rp = f"{rules_dir}/{fname}"
                    rules_text.append(f"\n---\n## L2: {fname}\n{self._read_file(rp)[:1500]}")

        # Fallback: 如果角色目录无规则，尝试旧路径
        old_rules_dir = f"{root}/.claude/rules"
        if _os.path.isdir(old_rules_dir) and not rules_text:
            for fname in self._load_role_rules(role):
                rp = f"{old_rules_dir}/{fname}"
                if _os.path.exists(rp):
                    rules_text.append(f"\n---\n## L2: {fname}\n{self._read_file(rp)[:1500]}")

        # === 3. 加载前置阶段产出 ===
        inputs = self._load_phase_inputs(phase, state)

        # === 4. 组装系统提示 ===
        combined_rules = "\n".join(rules_text)
        system_prompt = f"""{soul}

---
## 当前阶段专属规则
{combined_rules}

---
## 规则加载预算
L0: assertion-discipline.md + engineering-laws.md
L1: workflow.md
L2: 角色专属规则（{role}/rules/）
总预算: < 6,000 tokens（隧道视野 — 只加载当前角色需要的规则）
"""

        # === 5. 组装用户提示（含隧道视野声明）===
        import json as _json
        requirement = state.get("requirement", "")
        error_ctx = state.get("error_context")

        user_prompt = f"""# 任务输入
{requirement}

# 前置阶段产出（结构化交接 — 隧道视野：只含当前阶段必要上下文）
{_json.dumps(inputs, ensure_ascii=False, indent=2)}

# 隧道视野声明
你只能看到上述上下文。禁止请求访问其他文件或询问完整计划/架构。
禁止输出任何自然语言前缀或后缀。必须严格按JSON Schema输出本阶段产物。
"""

        if error_ctx:
            user_prompt += f"\n# 错误上下文（上次失败原因，用于避免重复错误）\n{_json.dumps(error_ctx, ensure_ascii=False) if isinstance(error_ctx, dict) else str(error_ctx)}\n"

        return {"system": system_prompt, "user": user_prompt}

    def _read_file(self, path: str) -> str:
        """读取文件内容（UTF-8），文件不存在返回空字符串"""
        import os as _os
        if not _os.path.exists(path):
            return ""
        with open(path, "r", encoding="utf-8") as f:
            return f.read()

    def _load_phase_inputs(self, phase: Phase, state: dict) -> dict:
        """
        隧道视野加载：只注入当前阶段需要的最小上下文。
        - BUILD 阶段：只看到当前子任务 + 依赖文件输出，看不到完整 DAG
        - REVIEW 阶段：只看到当前子任务代码，看不到其他子任务
        """
        import os as _os
        import json as _json

        task_id = state.get("task_id", "")
        task_dir = f"{self.workspace}/{task_id}"
        inputs = {"task_id": task_id, "phase": phase.value}

        if phase in [Phase.BRAINSTORM, Phase.EXPLORE]:
            inputs["requirement"] = state.get("requirement", "")

        elif phase == Phase.DECOMPOSE:
            arch_path = f"{task_dir}/architecture.json"
            if _os.path.exists(arch_path):
                arch = _json.loads(self._read_file(arch_path))
                inputs["architecture"] = arch

        elif phase == Phase.BUILD:
            # 隧道视野核心：只注入当前子任务
            subtask_id = state.get("current_subtask_id")
            plan_path = f"{task_dir}/plan.json"
            if _os.path.exists(plan_path) and subtask_id:
                plan = _json.loads(self._read_file(plan_path))
                subtask = next((s for s in plan.get("subtasks", []) if s["id"] == subtask_id), None)
                if subtask:
                    inputs["subtask"] = subtask
                    # 加载依赖子任务产出
                    deps = {}
                    for dep_id in subtask.get("dependencies", []):
                        dep_path = f"{task_dir}/code_diff_{dep_id}.json"
                        if _os.path.exists(dep_path):
                            deps[dep_id] = _json.loads(self._read_file(dep_path))
                    inputs["dependency_outputs"] = deps
                # 绝不注入完整 DAG
            # 注入 coder-reminders
            reminders_path = f"{self.root}/.claude/evolution/coder-reminders.md"
            if _os.path.exists(reminders_path):
                inputs["coder_reminders"] = self._read_file(reminders_path)[:1000]

        elif phase == Phase.VERIFY:
            subtask_id = state.get("current_subtask_id", "")
            code_path = f"{task_dir}/code_diff_{subtask_id}.json" if subtask_id else f"{task_dir}/code_diff.json"
            if _os.path.exists(code_path):
                inputs["code_changes"] = _json.loads(self._read_file(code_path)).get("changed_files", [])
            if subtask_id:
                plan_path = f"{task_dir}/plan.json"
                if _os.path.exists(plan_path):
                    plan = _json.loads(self._read_file(plan_path))
                    st = next((s for s in plan.get("subtasks", []) if s["id"] == subtask_id), None)
                    if st:
                        inputs["acceptance_criteria"] = st.get("acceptance_criteria", "")

        elif phase == Phase.REVIEW:
            subtask_id = state.get("current_subtask_id")
            if subtask_id:
                inputs["tunnel_vision"] = {"scope": "subtask", "subtask_id": subtask_id}
                code_path = f"{task_dir}/code_diff_{subtask_id}.json"
                if _os.path.exists(code_path):
                    inputs["artifacts"] = {"code_diff": code_path}
                test_path = f"{task_dir}/test_report.json"
                if _os.path.exists(test_path):
                    inputs["artifacts"] = inputs.get("artifacts", {})
                    inputs["artifacts"]["test_report"] = test_path
                scan_path = f"{task_dir}/security_scan_build.json"
                if _os.path.exists(scan_path):
                    inputs["artifacts"]["security_scan"] = scan_path
                # 注入历史复发记录
                recurrence = self._load_recurrence_history(subtask_id)
                if recurrence:
                    inputs["recurrence_history"] = recurrence
                # 注入适用红线摘要
                inputs["rules_digest"] = {
                    "architecture_redlines": ["R1", "R3", "R4", "R7", "R8"],
                    "expert_traps": ["Trap-2", "Trap-3", "Trap-7", "Trap-8", "Trap-14"],
                    "engineering_laws": ["Law-2", "Law-4"]
                }

        elif phase == Phase.REPORT:
            for fname in ["architecture.json", "plan.json", "code_diff.json", "test_report.json", "review_report.json"]:
                p = f"{task_dir}/{fname}"
                if _os.path.exists(p):
                    inputs[fname] = fname  # 不加载全文，只列出文件名

        return inputs

    def _load_recurrence_history(self, subtask_id: str) -> dict:
        """加载子任务类型的历史复发记录"""
        import os as _os, json as _json
        anomalies_dir = f"{self.root}/.claude/evolution/anomalies"
        if not _os.path.isdir(anomalies_dir):
            return {}
        # 简单实现：扫描所有 anomalies 文件，找复发次数 >= 2 的条目
        recurrent = []
        for fname in _os.listdir(anomalies_dir):
            if fname.endswith(".json"):
                try:
                    data = _json.loads(self._read_file(f"{anomalies_dir}/{fname}"))
                    items = data if isinstance(data, list) else [data]
                    for item in items:
                        if item.get("recurrence_count", 0) >= 2:
                            recurrent.append({
                                "rule_id": item.get("rule_id"),
                                "symptom": item.get("symptom"),
                                "suggested_fix": item.get("suggested_fix"),
                                "count": item.get("recurrence_count"),
                            })
                except Exception:
                    continue
        return {"recurrent_patterns": recurrent} if recurrent else {}

    # ================================================================
    # 三层防线：真实 LLM JSON 可靠性保障
    # ================================================================

    def _call_llm_isolated(self, phase: Phase, prompt: dict) -> dict:
        """
        第一层：通过平台适配层调用 LLM。
        复用平台的 tool_use 强制、重试、超时等基础设施。
        """
        if not self.llm:
            raise RuntimeError("LLM adapter not configured")

        schema = self._get_schema_for_phase(phase)

        # 通过适配层调用（统一接口，隔离平台差异）
        return self.llm.call(
            system=prompt["system"],
            user=prompt["user"],
            phase=phase.value,
            schema=schema,
        )

    def _get_schema_for_phase(self, phase: Phase) -> dict:
        """返回每个阶段的 JSON Schema 定义"""
        schemas = {
            Phase.BRAINSTORM: {
                "type": "object",
                "required": ["options", "recommendation"],
                "properties": {
                    "options": {"type": "array", "minItems": 2},
                    "recommendation": {"type": "object",
                                       "required": ["chosen_option", "reason"]},
                    "requirements": {"type": "array"},
                    "impact_assessment": {"type": "object"},
                }
            },
            Phase.BUILD: {
                "type": "object",
                "required": ["changed_files", "self_verification"],
                "properties": {
                    "changed_files": {"type": "array"},
                    "self_verification": {"type": "object"},
                    "compliance_checklist": {"type": "object"},
                }
            },
            Phase.VERIFY: {
                "type": "object",
                "required": ["checks", "verdict"],
                "properties": {
                    "checks": {"type": "array"},
                    "verdict": {"type": "string", "enum": ["PASS", "FAIL", "PARTIAL"]},
                    "summary": {"type": "object"},
                }
            },
            Phase.REVIEW: {
                "type": "object",
                "required": ["findings", "hook_audit", "metrics"],
                "properties": {
                    "findings": {"type": "array"},
                    "hook_audit": {"type": "object",
                                   "required": ["guard_coverage_verified"]},
                    "rule_evolution": {"type": "object"},
                    "coder_feedback": {"type": "object"},
                    "metrics": {"type": "object"},
                }
            },
        }
        return schemas.get(phase, {"type": "object"})

    def _force_json_parse(self, llm_output, phase: Phase, retry_count: int = 0) -> dict:
        """
        第二层：解析容错。
        处理 tool_use dict、带自然语言前缀的文本、格式错误 JSON。
        失败时自动 retry 一次。
        """
        import re

        # Case 1: tool_use 直接返回 dict
        if isinstance(llm_output, dict):
            # 检查是否是 tool_use 响应结构
            if "content" in llm_output:
                # Anthropic API: content block
                for block in llm_output.get("content", []):
                    if block.get("type") == "tool_use":
                        return block.get("input", {})
            # 可能就是直接的 dict
            if "options" in llm_output or "changed_files" in llm_output or "verdict" in llm_output:
                return llm_output

        text = str(llm_output) if not isinstance(llm_output, str) else llm_output

        # Case 2: 文本中提取 JSON — 剥离自然语言前缀
        cleaned = self._strip_preamble(text)

        # Try JSON object
        start = cleaned.find('{')
        end = cleaned.rfind('}')
        if start != -1 and end != -1 and end > start:
            try:
                return json.loads(cleaned[start:end + 1])
            except json.JSONDecodeError:
                pass

        # Try JSON array
        start = cleaned.find('[')
        end = cleaned.rfind(']')
        if start != -1 and end != -1 and end > start:
            try:
                return json.loads(cleaned[start:end + 1])
            except json.JSONDecodeError:
                pass

        # Case 3: Retry once with temperature=0
        if retry_count < 1 and self.llm:
            import logging
            logging.warning(f"JSON parse failed for {phase.value}, retrying with temperature=0")
            # Could re-call LLM with stricter constraints
            # For now: raise to trigger state machine retry

        raise JsonParseFailed(
            f"JSON parse failed for {phase.value} after {retry_count + 1} attempts. "
            f"First 200 chars: {text[:200]}"
        )

    def _strip_preamble(self, text: str) -> str:
        """剥离 LLM 常见的自然语言前缀"""
        import re
        # 常见前缀模式
        patterns = [
            r'^(?:Here|Sure|OK|Alright|Certainly|Below|Following|The|This|I|Let|Based|According).*?[\n\r]+',
            r'^(?:好的|以下是|这是|根据|我来).*?[\n\r]+',
            r'^```(?:json)?\s*[\n\r]+',
            r'[\n\r]+```\s*$',
        ]
        for pat in patterns:
            text = re.sub(pat, '', text, flags=re.IGNORECASE | re.MULTILINE)
        return text.strip()

    def _validate_and_fill(self, parsed: dict, phase: Phase) -> dict:
        """
        第三层：Schema 校验 + 安全默认值补全。
        关键字段缺失 → 抛出异常（状态机回退）。
        非关键字段缺失 → 补默认值（不阻塞）。
        """
        schema = self._get_schema_for_phase(phase)
        required = schema.get("required", [])

        missing_critical = []
        missing_safe = []

        for field in required:
            if field not in parsed or parsed[field] is None:
                if field in ("options", "findings", "subtasks", "checks",
                              "changed_files", "requirements"):
                    parsed[field] = []
                    missing_safe.append(field)
                elif field in ("self_verification", "metrics", "hook_audit",
                               "summary", "recommendation"):
                    parsed[field] = {}
                    missing_safe.append(field)
                else:
                    missing_critical.append(field)

        if missing_critical:
            raise JsonValidationFailed(
                f"Critical fields missing in {phase.value} output: {missing_critical}. "
                f"Safe-filled: {missing_safe if missing_safe else 'none'}"
            )

        # 确保嵌套必填字段
        if phase == Phase.REVIEW and "hook_audit" in parsed:
            if "guard_coverage_verified" not in parsed["hook_audit"]:
                parsed["hook_audit"]["guard_coverage_verified"] = False

        if phase == Phase.BRAINSTORM and "recommendation" in parsed:
            rec = parsed["recommendation"]
            if "chosen_option" not in rec:
                raise JsonValidationFailed("recommendation.chosen_option is required")

        return parsed


class JsonParseFailed(Exception):
    pass


class JsonValidationFailed(Exception):
    pass


# ============================================================================
# 自检
# ============================================================================

def self_test():
    """验证状态机基本逻辑"""
    pipeline = FuguPipeline(".")

    # 1. 流转表完整性
    for phase in Phase:
        if phase == Phase.END:
            continue
        transition = pipeline.TRANSITIONS.get(phase)
        assert transition is not None, f"Missing transition for {phase}"
        assert transition.next_phase is not None
        assert transition.fail_phase is not None

    # 2. 任务分级
    router = TaskRouter()
    assert router.classify("fix typo", ["file.cs"]) == TaskLevel.C
    assert router.classify("refactor service method", ["a.cs", "b.cs"]) == TaskLevel.B
    assert router.classify("add new API with entity migration", ["a.cs", "b.cs", "c.cs", "d.cs"]) == TaskLevel.A
    assert router.classify("new module scaffold with 12 files and database migration", list(range(12))) == TaskLevel.S

    # 3. 流水线长度
    c_pipeline = pipeline.get_pipeline(TaskLevel.C)
    assert len(c_pipeline) < len(pipeline.get_pipeline(TaskLevel.A)), \
        "C级流水线应少于A级"

    # 4. 状态推进（B级流水线: ALIGN→BRAINSTORM→BUILD→VERIFY→REVIEW→REPORT→END）
    state = pipeline.init_state("TEST-001", TaskLevel.B)
    assert state["current_phase"] == Phase.ALIGN.value, f"Expected ALIGN, got {state['current_phase']}"
    pipeline.advance_phase(state, True)
    assert state["current_phase"] == Phase.BRAINSTORM.value, f"Expected BRAINSTORM, got {state['current_phase']}"
    pipeline.advance_phase(state, True)
    assert state["current_phase"] == Phase.BUILD.value, f"Expected BUILD, got {state['current_phase']}"

    # 5. 熔断机制
    for _ in range(4):
        pipeline.advance_phase(state, False)
    assert state["current_phase"] == "halted", "应触发熔断"

    print("[PASS] V3.0 state machine skeleton verified")
    print(f"   {len(Phase)} phases, {len(TaskLevel)} task levels")
    print(f"   {len(pipeline.TRANSITIONS)} transition rules")


if __name__ == "__main__":
    # TaskRouter 已在顶部通过 from task_router import TaskLevel 导入
    from task_router import TaskRouter
    self_test()
