# 类fugu超级联合智能体 — 重构实施计划（V3.0）

> **版本**：V3.0 超级审查引擎集成版  
> **基准**：专家组五条建议 + 首席专家深化加固 + 超级审查引擎（Reviewer L1）硬集成  
> **核心范式**：外部硬编码状态机调度 + 内部纯专家角色执行 + 强制结构化交接 + 双防线质量进化  
> **目标**：从"赛博官僚系统"进化为"机械级可靠、专家级智能、越审查越聪明"的工业流水线

---

## 一、重构宣言：为什么必须推翻V1.0

V1.0（原方案）的本质缺陷：**用Prompt模拟流程控制**。

| 维度           | V1.0（原方案）                   | V3.0（重构后）                                               |
| :------------- | :------------------------------- | :----------------------------------------------------------- |
| **流程控制**   | 协调者（LLM角色）软调度          | 外部Python状态机硬编码调度                                   |
| **上下文管理** | 单一会话内角色切换，污染不可避免 | 每阶段独立会话，物理隔离                                     |
| **信息传递**   | 自然语言交接，歧义、遗漏、幻觉   | JSON Schema文件级交接，确定性解析                            |
| **质量门**     | 协调者主观判断                   | 脚本硬执行（编译/测试/安全扫描/审查分级）                    |
| **安全红线**   | 依赖Hook拦截，有漏报风险         | **双防线**：Hook L0前置强制扫描 + Reviewer L1语义级审计，失败即熔断 |
| **进化引擎**   | 角色直接改写规则，基因自噬风险   | 离线闭环：记录→收集→人工仲裁→生效                            |
| **小任务**     | 强制走完整六角色，过度工程       | C级快速通道，秒级闭环                                        |
| **并发能力**   | 单线程串行                       | DAG并行，多LLM实例异步执行                                   |
| **审查深度**   | 纯自然语言，无自动化验证         | 5维度×3级别×置信度加权，Hook审计义务，规则进化闭环           |

**结论**：V1.0是"聪明的单智能体扮演多个角色"，V3.0是"多个无状态专家函数被外部机械精确调度，并由超级审查引擎驱动质量飞轮"。后者才是工业级系统的唯一正道。

---

## 二、架构总览：外部大脑 + 内部专家 + 双防线质量进化

```
+-----------------------------------------------------------------------------+
|                           人类工程师（最终仲裁者）                              |
|                    审核《规则变更草案》-> 手动修改规则文件                        |
+-----------------------------------------------------------------------------+
                                      ^
                                      | 离线闭环
+-----------------------------------------------------------------------------+
|                         外部状态机（Orchestrator）                          |
|  +--------------+  +--------------+  +--------------+  +--------------+     |
|  |  任务路由器   |  |  阶段状态机   |  |  质量门引擎   |  |  并发调度器   |     |
|  |  (复杂度分级) |  |  (流转/回退)  |  |  (硬执行)     |  |  (DAG并行)    |     |
|  +--------------+  +--------------+  +--------------+  +--------------+     |
|         |                 |                 |                 |              |
|         v                 v                 v                 v              |
|  +-----------------------------------------------------------------------+   |
|  |                      文件级交接契约（IPC）                           |   |
|  |  workspace/requirements.md -> architecture.json -> plan.json -> ...    |   |
|  |  -> code_diff.json -> test_report.json -> [review_input.json] ->      |   |
|  |     review_report.json -> delivery_report.md                          |   |
|  +-----------------------------------------------------------------------+   |
+-----------------------------------------------------------------------------+
                                      |
                    +-----------------+-----------------+
                    v                 v                 v
+--------------+ +--------------+ +--------------+ +--------------+ +--------------+
|  架构师       | |  规划师       | |  开发者       | |  测试员       | |  审查员       |
|  (独立会话1)  | |  (独立会话2)  | |  (独立会话N)  | |  (独立会话M)  | | (独立会话P)   |
|  只加载soul   | |  只加载soul   | |  只加载soul   | |  只加载soul   | | 只加载soul    |
|  + 专属规则   | |  + 专属规则   | |  + 专属规则   | |  + 专属规则   | | + 专属规则    |
+--------------+ +--------------+ +--------------+ +--------------+ +--------------+
                    |                 |                 |                 |
                    +-----------------+-----------------+-----------------+
                                      v
+-----------------------------------------------------------------------------+
|                         双防线安全守卫体系（前置强制）                      |
|  +--------------------+  +--------------------+  +--------------------+     |
|  |  L0 Hook硬约束      |  |  L1 Reviewer语义审计 |  |  进化引擎闭环       |     |
|  | SQL注入扫描         |  | 5维度×3级别×置信度   |  | anomalies.json    |     |
|  | 租户隔离校验        |  | Hook漏检/误报审计    |  | -> 规则变更草案    |     |
|  | 权限属性检查        |  | 规则进化建议         |  | -> 人工仲裁 -> Git |     |
|  | 红线合规扫描        |  | Coder提醒生成        |  |                   |     |
|  +--------------------+  +--------------------+  +--------------------+     |
+-----------------------------------------------------------------------------+
```

---

## 三、核心机制详解

### 3.1 建议1：补齐安全自动化检查 — 「生命线级修复」

**原则**：安全红线不可退化。状态机在**每个阶段产出物落盘前**，必须执行强制安全扫描。失败即BLOCK，直接熔断当前阶段。

#### 3.1.1 安全守卫矩阵（L0 Hook）

| 守卫项       | 扫描目标                                              | 检测时机                    | 失败处理              | 实现方式             |
| :----------- | :---------------------------------------------------- | :-------------------------- | :-------------------- | :------------------- |
| **SQL注入**  | 所有`.cs`文件中的SQL拼接、动态SQL、原生SQL            | 每次Write/Edit落盘前        | BLOCK，退回开发者阶段 | AST解析 + 正则双保险 |
| **租户隔离** | Entity类是否含`TenantId`、查询是否带`TenantId`过滤    | 每次Entity/Repository变更后 | BLOCK，退回开发者阶段 | Roslyn语法树扫描     |
| **权限校验** | Controller/Service方法是否标注`[Authorize]`或权限属性 | 每次API层变更后             | BLOCK，退回开发者阶段 | 反射 + 属性扫描      |
| **架构红线** | 跨层调用、循环依赖、私有类暴露                        | 每次编译前                  | BLOCK，退回架构师阶段 | 依赖图分析           |

#### 3.1.2 安全守卫实现（Python）

```python
# .claude/guards/security_scanner.py
import re
import json
from pathlib import Path
from typing import List, Dict, Tuple
from dataclasses import dataclass

@dataclass
class SecurityFinding:
    rule_id: str          # 如 SEC-SQL-001
    level: str            # BLOCK / WARN / NOTE
    file: str
    line: int
    message: str
    evidence: str         # 代码片段
    fix_hint: str

class SecurityScanner:
    # 安全守卫：状态机在每个阶段落盘前强制调用
    
    def __init__(self, project_root: str):
        self.root = Path(project_root)
        self.findings: List[SecurityFinding] = []
    
    def scan_all(self, changed_files: List[str]) -> Tuple[bool, List[SecurityFinding]]:
        # 返回: (是否通过, 发现列表)
        self.findings = []
        
        for f in changed_files:
            path = self.root / f
            if not path.exists():
                continue
            content = path.read_text(encoding='utf-8')
            
            self._scan_sql_injection(path, content)
            self._scan_tenant_isolation(path, content)
            self._scan_auth_attributes(path, content)
        
        blocks = [f for f in self.findings if f.level == "BLOCK"]
        return len(blocks) == 0, self.findings
    
    def _scan_sql_injection(self, path: Path, content: str):
        # SQL注入扫描：AST级 + 正则双保险
        patterns = [
            (r'\$"\s*SELECT\s+.*\+', "SEC-SQL-001", "检测到字符串拼接SQL"),
            (r'SqlQueryable\s*\(\s*\$', "SEC-SQL-002", "检测到动态SQL参数未转义"),
            (r'ExecuteSqlCommand\s*\(\s*[^,)]*\+', "SEC-SQL-003", "ExecuteSqlCommand拼接SQL"),
            (r'\.Sql\s*\(\s*\$"', "SEC-SQL-004", "SqlSugar.Sql使用字符串插值"),
        ]
        
        lines = content.split('\n')
        for i, line in enumerate(lines, 1):
            for pattern, rule_id, msg in patterns:
                if re.search(pattern, line, re.IGNORECASE):
                    next_line = lines[i] if i < len(lines) else ""
                    level = "WARN" if "// PARAM_SAFE" in next_line else "BLOCK"
                    
                    self.findings.append(SecurityFinding(
                        rule_id=rule_id,
                        level=level,
                        file=str(path),
                        line=i,
                        message=msg,
                        evidence=line.strip(),
                        fix_hint="使用参数化查询或SqlSugar表达式树"
                    ))
    
    def _scan_tenant_isolation(self, path: Path, content: str):
        # 租户隔离扫描
        if "Entity" not in str(path):
            return
        
        if "class" in content and "TenantId" not in content:
            if "BaseEntity" not in content and "ITenant" not in content:
                self.findings.append(SecurityFinding(
                    rule_id="SEC-TENANT-001",
                    level="BLOCK",
                    file=str(path),
                    line=1,
                    message="Entity类缺少TenantId字段，违反多租户隔离红线",
                    evidence="class声明",
                    fix_hint="继承BaseEntity或添加TenantId属性"
                ))
        
        if "Repository" in str(path) or "Service" in str(path):
            if ("GetList" in content or "Queryable" in content) and "TenantId" not in content:
                self.findings.append(SecurityFinding(
                    rule_id="SEC-TENANT-002",
                    level="BLOCK",
                    file=str(path),
                    line=1,
                    message="查询方法未过滤TenantId，存在数据越权风险",
                    evidence="查询方法",
                    fix_hint="在查询条件中追加 .Where(x => x.TenantId == currentTenantId)"
                ))
    
    def _scan_auth_attributes(self, path: Path, content: str):
        # 权限属性扫描
        if "Controller" not in str(path):
            return
        
        lines = content.split('\n')
        for i, line in enumerate(lines, 1):
            if re.search(r'public\s+async\s+Task.*\(', line):
                context = '\n'.join(lines[max(0, i-5):i])
                if not re.search(r'\[Authorize|Permission|ActionPermission', context):
                    self.findings.append(SecurityFinding(
                        rule_id="SEC-AUTH-001",
                        level="BLOCK",
                        file=str(path),
                        line=i,
                        message="公开API方法缺少权限校验属性",
                        evidence=line.strip(),
                        fix_hint="添加[Authorize]或[Permission(\"xxx\")]属性"
                    ))
```

#### 3.1.3 状态机集成点

```python
# 在状态机的 persist_output 方法中强制插入
class FuguPipeline:
    def persist_output(self, task_id: str, phase: str, output: dict):
        # 1. 先落盘到临时区
        temp_path = self.get_temp_path(task_id, phase)
        self.save_json(temp_path, output)
        
        # 2. 提取变更文件列表
        changed_files = output.get("changed_files", [])
        
        # 3. 强制安全扫描（生命线）
        scanner = SecurityScanner(self.project_root)
        passed, findings = scanner.scan_all(changed_files)
        
        # 4. 扫描结果写入证据
        self.save_json(
            f"workspace/{task_id}/security_scan_{phase}.json",
            {"passed": passed, "findings": [f.__dict__ for f in findings]}
        )
        
        # 5. 失败即熔断
        if not passed:
            blocks = [f for f in findings if f.level == "BLOCK"]
            raise SecurityGateBlocked(
                f"安全门拦截：发现{len(blocks)}个BLOCK级问题",
                findings=findings
            )
        
        # 6. 通过后才正式落盘
        self.move_to_final(temp_path, task_id, phase)
```

---

### 3.2 建议2&3：状态机 + 物理隔离 + 文件级交接 + 阶段性会话重建

这是整个重构的**核心骨架**。状态机必须是外部硬编码的Python脚本，拥有对LLM会话的"生杀大权"。

#### 3.2.1 状态机核心实现（含Reviewer硬阶段）

```python
# .claude/orchestrator/state_machine.py
import json
from enum import Enum
from dataclasses import dataclass
from typing import Dict, List
from datetime import datetime

class Phase(Enum):
    ALIGN = "align"
    BRAINSTORM = "brainstorm"
    EXPLORE = "explore"
    DECOMPOSE = "decompose"
    PLAN = "plan"
    BUILD = "build"
    VERIFY = "verify"
    REVIEW = "review"          # ← 超级审查引擎阶段（新增）
    REVIEW_FIX = "review_fix"  # ← 审查修复子循环（新增）
    REPORT = "report"
    END = "end"

class TaskLevel(Enum):
    S = "S"
    A = "A"
    B = "B"
    C = "C"

@dataclass
class Transition:
    next_phase: Phase
    fail_phase: Phase
    max_retries: int = 3

class FuguPipeline:
    # 外部硬编码状态机：拥有对LLM会话的绝对控制权
    # 每个阶段都是全新的、无状态的LLM调用
    
    TRANSITIONS: Dict[Phase, Transition] = {
        Phase.ALIGN:      Transition(Phase.BRAINSTORM, Phase.ALIGN),
        Phase.BRAINSTORM: Transition(Phase.EXPLORE,   Phase.BRAINSTORM),
        Phase.EXPLORE:    Transition(Phase.DECOMPOSE, Phase.BRAINSTORM),
        Phase.DECOMPOSE:  Transition(Phase.PLAN,      Phase.EXPLORE),
        Phase.PLAN:       Transition(Phase.BUILD,     Phase.DECOMPOSE),
        Phase.BUILD:      Transition(Phase.VERIFY,    Phase.BUILD),
        Phase.VERIFY:     Transition(Phase.REVIEW,    Phase.BUILD),     # ← VERIFY通过→REVIEW
        Phase.REVIEW:     Transition(Phase.REPORT,    Phase.REVIEW_FIX),  # ← REVIEW通过→REPORT，失败→REVIEW_FIX
        Phase.REVIEW_FIX: Transition(Phase.REVIEW,    Phase.REVIEW_FIX),  # ← 修复后→REVIEW（循环）
        Phase.REPORT:     Transition(Phase.END,       Phase.REPORT),
    }
    
    PHASE_ROLE: Dict[Phase, str] = {
        Phase.BRAINSTORM: "architect",
        Phase.EXPLORE:    "architect",
        Phase.DECOMPOSE:  "planner",
        Phase.PLAN:       "planner",
        Phase.BUILD:      "coder",
        Phase.VERIFY:     "tester",
        Phase.REVIEW:     "reviewer",      # ← 新增
        Phase.REVIEW_FIX: "coder",         # ← 修复阶段由Coder执行
        Phase.REPORT:     "reporter",
    }
    
    # REVIEW阶段专属质量门配置
    REVIEW_GATE_CONFIG = {
        "block_threshold": 0,      # BLOCK数量>0即质量门失败
        "warn_threshold": 5,       # WARN数量>5触发警告但不阻塞（可配置）
        "note_threshold": 999,     # NOTE不阻塞
        "confidence_override": {   # 置信度覆盖规则
            "HIGH": {"block": 1, "warn": 0, "note": 0},  # 1个HIGH级BLOCK即熔断
            "MED":  {"block": 2, "warn": 3, "note": 0},   # MED级需累积
            "LOW":  {"block": 3, "warn": 5, "note": 0},   # LOW级需更多累积
        }
    }
    
    def __init__(self, project_root: str, llm_client):
        self.root = project_root
        self.llm = llm_client
        self.workspace = f"{project_root}/workspace"
        
    def run_task(self, task_id: str, requirement: str):
        state = self._load_or_init_state(task_id)
        state["requirement"] = requirement
        
        while state["phase"] != Phase.END.value:
            current = Phase(state["phase"])
            print(f"\n[状态机] 进入阶段: {current.value.upper()}")
            
            try:
                # 1. 组装最小上下文（物理隔离）
                prompt = self._assemble_prompt(current, state)
                
                # 2. 调用LLM（全新会话，无历史消息）
                llm_output = self._call_llm_isolated(current, prompt)
                
                # 3. 强制JSON解析（解析失败 = 质量门失败）
                parsed = self._force_json_parse(llm_output)
                
                # 4. 执行确定性质量门（硬执行）
                qg_result = self._run_quality_gates(current, parsed, state)
                
                if qg_result["passed"]:
                    # 5. 安全扫描（生命线）
                    self._security_gate(current, parsed)
                    
                    # 6. 持久化产出物到文件系统
                    self._persist_output(task_id, current, parsed)
                    
                    # 7. 如果是REVIEW阶段，触发进化闭环
                    if current == Phase.REVIEW:
                        self._trigger_evolution(task_id, parsed)
                    
                    # 8. 推进状态
                    state["phase"] = self.TRANSITIONS[current].next_phase.value
                    state["error_context"] = None
                    state["retry_count"] = 0
                    
                else:
                    # 质量门失败：回退
                    state["error_context"] = qg_result["errors"]
                    state["phase"] = self.TRANSITIONS[current].fail_phase.value
                    state["retry_count"] = state.get("retry_count", 0) + 1
                    
                    # 熔断机制：同一阶段连续失败3次
                    if state["retry_count"] >= self.TRANSITIONS[current].max_retries:
                        self._alert_human_intervention(task_id, current, state)
                        break
                
            except Exception as e:
                state["error_context"] = str(e)
                state["phase"] = self.TRANSITIONS[current].fail_phase.value
            
            self._save_state(task_id, state)
        
        print(f"\n[状态机] 任务 {task_id} 完成，最终状态: {state['phase']}")
        return state
    
    def _assemble_prompt(self, phase: Phase, state: dict) -> dict:
        # 组装最小上下文：只加载当前角色的soul + 前置阶段的JSON产出
        role = self.PHASE_ROLE.get(phase, "orchestrator")
        
        soul_path = f"{self.root}/.claude/souls/{role}/soul.md"
        soul = self._read_file(soul_path)
        
        rules = self._load_role_rules(role, phase)
        inputs = self._load_phase_inputs(phase, state)
        decisions = self._load_relevant_decisions(state.get("requirement", ""))
        
        system_prompt = f"""{soul}

---
## 当前阶段专属规则
{rules}

---
## 决策追溯（相关）
{decisions}
"""
        
        user_prompt = f"""# 任务输入
{state.get('requirement', '')}

# 前置阶段产出（结构化交接）
{json.dumps(inputs, ensure_ascii=False, indent=2)}

# 错误上下文（如有）
{json.dumps(state.get('error_context', {}), ensure_ascii=False)}

---
请严格按JSON Schema输出本阶段产物。禁止输出任何自然语言前缀或后缀。
"""
        return {"system": system_prompt, "user": user_prompt}
    
    def _call_llm_isolated(self, phase: Phase, prompt: dict) -> str:
        # 全新会话调用：绝不携带任何历史消息
        return self.llm.call(
            system=prompt["system"],
            user=prompt["user"],
            temperature=0.1,
            max_tokens=8000,
            tools=[{
                "name": f"output_{phase.value}",
                "description": f"Output for {phase.value}",
                "input_schema": self._get_schema_for_phase(phase)
            }],
            tool_choice={"type": "tool", "name": f"output_{phase.value}"}
        )
    
    def _force_json_parse(self, llm_output: str) -> dict:
        # 强制JSON解析：解析失败 = 质量门失败
        try:
            if isinstance(llm_output, dict):
                return llm_output
            
            start = llm_output.find('{')
            end = llm_output.rfind('}')
            if start != -1 and end != -1 and end > start:
                return json.loads(llm_output[start:end+1])
            
            start = llm_output.find('[')
            end = llm_output.rfind(']')
            if start != -1 and end != -1 and end > start:
                return json.loads(llm_output[start:end+1])
            
            raise ValueError("无法提取有效JSON")
            
        except (json.JSONDecodeError, ValueError) as e:
            raise JsonParseFailed(f"JSON解析熔断: {e}")
    
    def _run_quality_gates(self, phase: Phase, output: dict, state: dict) -> dict:
        # 确定性质量门：由脚本硬执行
        gates = []
        
        if phase == Phase.BRAINSTORM:
            options = output.get("options", [])
            gates.append({
                "name": "Q1-方案数量",
                "passed": len(options) >= 2,
                "detail": f"提供了{len(options)}个方案"
            })
            gates.append({
                "name": "Q1-失效边界",
                "passed": all("failure_boundary" in opt for opt in options),
                "detail": "所有方案都标注了失效边界"
            })
        
        elif phase == Phase.BUILD:
            changed_files = output.get("changed_files", [])
            gates.append({
                "name": "Q3-编译通过",
                "passed": self._check_compile(),
                "detail": "dotnet build 0 errors"
            })
            gates.append({
                "name": "Q3-安全扫描",
                "passed": True,
                "detail": "前置安全扫描已通过"
            })
        
        elif phase == Phase.VERIFY:
            gates.append({
                "name": "Q4-测试通过",
                "passed": self._run_tests(),
                "detail": "dotnet test 0 failures"
            })
        
        elif phase == Phase.REVIEW:
            # Q5: 审查质量门 — 置信度加权硬执行
            findings = output.get("findings", [])
            blocks = [f for f in findings if f["level"] == "BLOCK"]
            warns = [f for f in findings if f["level"] == "WARN"]
            
            # 置信度加权计算
            weighted_block = sum(
                3 if f.get("confidence") == "HIGH" else 
                2 if f.get("confidence") == "MED" else 1 
                for f in blocks
            )
            weighted_warn = sum(
                1 if f.get("confidence") == "HIGH" else 
                0.5 if f.get("confidence") == "MED" else 0.2 
                for f in warns
            )
            
            gate_passed = (
                weighted_block <= self.REVIEW_GATE_CONFIG["block_threshold"] and
                weighted_warn < self.REVIEW_GATE_CONFIG["warn_threshold"]
            )
            
            gates.append({
                "name": "Q5-BLOCK阈值",
                "passed": weighted_block == 0,
                "detail": f"weighted_block={weighted_block}"
            })
            gates.append({
                "name": "Q5-WARN阈值",
                "passed": weighted_warn < 5,
                "detail": f"weighted_warn={weighted_warn}"
            })
            gates.append({
                "name": "Q5-Hook审计",
                "passed": output.get("hook_audit", {}).get("guard_coverage_verified", False),
                "detail": "Reviewer已审计Hook覆盖"
            })
        
        all_passed = all(g["passed"] for g in gates)
        return {
            "passed": all_passed,
            "gates": gates,
            "errors": [g for g in gates if not g["passed"]]
        }
    
    def _trigger_evolution(self, task_id: str, review_output: dict):
        # REVIEW阶段通过后，触发进化闭环
        from .evolution_manager import EvolutionManager
        evo = EvolutionManager(f"{self.root}/.claude/evolution")
        evo.process_review_report(task_id, review_output)
```

#### 3.2.2 文件级交接契约（JSON Schema）

每个阶段的输入输出必须是严格Schema的JSON，不含自然语言闲聊。

```json
// workspace/{task_id}/architecture.json (架构师产出)
{
  "$schema": "fugu/architecture-v1",
  "task_id": "TASK-20260625-001",
  "phase": "brainstorm",
  "timestamp": "2026-06-25T10:00:00Z",
  "role": "architect",
  "requirements": [
    {
      "id": "REQ-001",
      "source": "用户原始需求",
      "business_value": "核心业务流程",
      "technical_constraints": ["必须租户隔离"],
      "ambiguities": ["\"订单\"范围不清"]
    }
  ],
  "options": [
    {
      "name": "方案A-领域驱动",
      "description": "采用DDD聚合根模式",
      "pros": ["业务逻辑内聚", "易于单元测试"],
      "cons": ["引入复杂度", "学习成本高"],
      "failure_boundary": "若订单状态机超过5种状态，维护成本将指数增长",
      "estimated_effort": "3天"
    },
    {
      "name": "方案B-事务脚本",
      "description": "传统Service+Repository模式",
      "pros": ["简单直接", "团队熟悉"],
      "cons": ["业务逻辑分散", "难以应对复杂规则"],
      "failure_boundary": "若后续需要支持订单拆分/合并，需大规模重构",
      "estimated_effort": "1.5天"
    },
    {
      "name": "方案C-不做",
      "description": "复用现有通用表单模块",
      "pros": ["零开发成本"],
      "cons": ["无法满足业务规则校验"],
      "failure_boundary": "业务方明确拒绝",
      "estimated_effort": "0天"
    }
  ],
  "recommendation": {
    "chosen_option": "方案B-事务脚本",
    "reason": "当前需求简单，时间紧，后续有重构窗口期",
    "risks": ["状态流转规则硬编码在Service中"],
    "mitigations": ["在decisions.md中记录技术债，排期重构"]
  },
  "impact_assessment": {
    "change_type": "Entity",
    "exploration_depth": 3,
    "symbols_touched": 12,
    "truncated": false,
    "boundary_symbols": ["PaymentService", "InventoryService"]
  }
}
```

```json
// workspace/{task_id}/plan.json (规划师产出)
{
  "$schema": "fugu/plan-v1",
  "task_id": "TASK-20260625-001",
  "phase": "decompose",
  "role": "planner",
  "subtasks": [
    {
      "id": "ST-001",
      "name": "数据库迁移",
      "layer": "data",
      "input_files": [],
      "output_files": ["Migrations/20260625_AddOrderTable.cs"],
      "acceptance_criteria": "dotnet ef migrations script 生成SQL无错误",
      "estimated_tokens": 1200,
      "dependencies": []
    },
    {
      "id": "ST-002",
      "name": "Entity定义",
      "layer": "data",
      "input_files": ["Migrations/20260625_AddOrderTable.cs"],
      "output_files": ["Domain/Entities/OrderEntity.cs"],
      "acceptance_criteria": "编译通过，字段与迁移一致，含TenantId",
      "estimated_tokens": 800,
      "dependencies": ["ST-001"]
    },
    {
      "id": "ST-003",
      "name": "DTO定义",
      "layer": "logic",
      "input_files": ["Domain/Entities/OrderEntity.cs"],
      "output_files": ["Application/Dtos/OrderDto.cs"],
      "acceptance_criteria": "Mapster配置可正确映射，不覆盖审计字段",
      "estimated_tokens": 600,
      "dependencies": ["ST-002"]
    }
  ],
  "dag": {
    "nodes": ["ST-001", "ST-002", "ST-003"],
    "edges": [
      {"from": "ST-001", "to": "ST-002"},
      {"from": "ST-002", "to": "ST-003"}
    ]
  },
  "rollback_strategy": "若ST-002失败，回滚Migration并删除Entity文件"
}
```

```json
// workspace/{task_id}/code_diff.json (开发者产出)
{
  "$schema": "fugu/code-v1",
  "task_id": "TASK-20260625-001",
  "phase": "build",
  "subtask_id": "ST-002",
  "role": "coder",
  "changed_files": [
    {
      "path": "Domain/Entities/OrderEntity.cs",
      "operation": "create",
      "lines_added": 45,
      "lines_removed": 0,
      "content_hash": "sha256:abc123..."
    }
  ],
  "self_verification": {
    "build": {"command": "dotnet build", "result": "PASS", "log_snippet": "Build succeeded."},
    "tests": {"command": "dotnet test --filter OrderEntityTests", "result": "PASS", "coverage": "85%"},
    "lint": {"command": "dotnet format --verify-no-changes", "result": "PASS"}
  },
  "known_risks": [
    "TenantId默认值在集成测试中可能需要Mock"
  ],
  "compliance_checklist": {
    "trap_2_mapster_audit": "PASS - 未使用Adapt覆盖审计字段",
    "trap_3_nav_property": "N/A - 无导航属性",
    "trap_8_updateable_tenant": "PASS - Entity含TenantId"
  }
}
```

#### 3.2.3 隧道视野（Tunnel Vision）实现

**核心原则**：开发者（Build阶段）和审查者（Review阶段）绝不看到完整的architecture.json或plan.json。状态机只注入当前子任务的极简上下文。

```python
def _load_phase_inputs(self, phase: Phase, state: dict) -> dict:
    # 隧道视野：只加载当前阶段需要的最小上下文
    inputs = {}
    
    if phase == Phase.BRAINSTORM:
        inputs["requirement"] = state.get("requirement", "")
    
    elif phase == Phase.EXPLORE:
        arch = self._load_json(f"workspace/{state['task_id']}/architecture.json")
        inputs["architecture"] = {
            "recommendation": arch["recommendation"],
            "impact_assessment": arch.get("impact_assessment", {})
        }
    
    elif phase == Phase.DECOMPOSE:
        arch = self._load_json(f"workspace/{state['task_id']}/architecture.json")
        inputs["architecture"] = arch
    
    elif phase == Phase.BUILD:
        # 关键：开发者只看到一个子任务
        plan = self._load_json(f"workspace/{state['task_id']}/plan.json")
        current_subtask_id = state.get("current_subtask_id")
        
        subtask = next(
            (s for s in plan["subtasks"] if s["id"] == current_subtask_id),
            None
        )
        
        if not subtask:
            raise ValueError(f"未找到子任务 {current_subtask_id}")
        
        inputs["subtask"] = subtask
        inputs["dependency_outputs"] = self._load_dependency_files(
            state["task_id"], subtask["dependencies"]
        )
        # 绝不注入：其他子任务、完整DAG、架构决策理由
    
    elif phase == Phase.VERIFY:
        code = self._load_json(f"workspace/{state['task_id']}/code_diff.json")
        plan = self._load_json(f"workspace/{state['task_id']}/plan.json")
        inputs["code_changes"] = code["changed_files"]
        inputs["acceptance_criteria"] = plan["subtasks"][0]["acceptance_criteria"]
    
    elif phase == Phase.REVIEW:
        # 关键：审查者只审查当前子任务的代码变更
        plan = self._load_json(f"workspace/{state['task_id']}/plan.json")
        current_subtask_id = state.get("current_subtask_id")
        
        subtask = next(
            (s for s in plan["subtasks"] if s["id"] == current_subtask_id),
            None
        )
        
        # 仅加载当前子任务的代码变更
        code_diff = self._load_json(
            f"workspace/{state['task_id']}/code_diff_{current_subtask_id}.json"
        )
        
        # 仅加载相关guard标志文件
        guard_flags = []
        for changed_file in code_diff.get("changed_files", []):
            flag_path = f".claude/review/flags/{changed_file.replace('/', '_')}.json"
            if os.path.exists(flag_path):
                guard_flags.append(self._load_json(flag_path))
        
        inputs = {
            "subtask": subtask,
            "code_changes": code_diff["changed_files"],
            "test_result": self._load_json(f"workspace/{state['task_id']}/test_report.json"),
            "security_scan": self._load_json(f"workspace/{state['task_id']}/security_scan_build.json"),
            "guard_flags": guard_flags,
            "recurrence_history": self._load_recurrence_history(current_subtask_id)
        }
        # 绝不注入：其他子任务的代码、完整DAG、架构决策的完整理由
    
    return inputs
```

---

### 3.3 建议4：协调者减负 + 质量门下沉 + 进化文件生命周期管理

#### 3.3.1 质量门下沉为脚本硬执行

协调者（状态机）不再"判断"质量门是否通过，而是**调用确定性脚本**获取结果。

```python
class QualityGateEngine:
    # 质量门引擎：纯脚本硬执行，零LLM参与判断
    
    def __init__(self, project_root: str):
        self.root = project_root
    
    def run_gate(self, gate_id: str, context: dict) -> dict:
        runners = {
            "Q1": self._gate_architecture,
            "Q2": self._gate_decomposition,
            "Q3": self._gate_implementation,
            "Q4": self._gate_verification,
            "Q5": self._gate_review,
            "Q6": self._gate_delivery,
        }
        return runners.get(gate_id, lambda x: {"passed": False, "error": "Unknown gate"})(context)
    
    def _gate_implementation(self, ctx: dict) -> dict:
        # Q3: 实现合规性 — 编译 + 安全 + 规范
        results = []
        
        # 3.1 编译检查
        build_result = subprocess.run(
            ["dotnet", "build", "backend/application/JNPF.API.Entry/JNPF.API.Entry.csproj"],
            capture_output=True, text=True, cwd=self.root
        )
        results.append({
            "check": "compile",
            "passed": build_result.returncode == 0,
            "evidence": build_result.stdout[-500:] if build_result.returncode == 0 else build_result.stderr[-500:]
        })
        
        # 3.2 前端类型检查
        if any(".vue" in f or ".ts" in f for f in ctx.get("changed_files", [])):
            tsc_result = subprocess.run(
                ["npx", "vue-tsc", "--noEmit"],
                capture_output=True, text=True, cwd=f"{self.root}/jnpf-web-vue3"
            )
            results.append({
                "check": "typescript",
                "passed": tsc_result.returncode == 0,
                "evidence": tsc_result.stdout[-300:] if tsc_result.returncode == 0 else tsc_result.stderr[-300:]
            })
        
        # 3.3 安全扫描汇总
        results.append({
            "check": "security",
            "passed": ctx.get("security_scan_passed", False),
            "evidence": f"{ctx.get('security_block_count', 0)} BLOCK findings"
        })
        
        all_passed = all(r["passed"] for r in results)
        return {
            "passed": all_passed,
            "checks": results,
            "errors": [r for r in results if not r["passed"]]
        }
    
    def _gate_verification(self, ctx: dict) -> dict:
        # Q4: 验证充分性 — 测试执行
        results = []
        
        test_result = subprocess.run(
            ["dotnet", "test", "--no-build", "--logger", "trx"],
            capture_output=True, text=True, cwd=self.root
        )
        results.append({
            "check": "unit_test",
            "passed": test_result.returncode == 0,
            "evidence": test_result.stdout[-500:]
        })
        
        coverage = self._parse_coverage(test_result.stdout)
        results.append({
            "check": "coverage",
            "passed": coverage >= 80,
            "evidence": f"Coverage: {coverage}%"
        })
        
        all_passed = all(r["passed"] for r in results)
        return {"passed": all_passed, "checks": results}
    
    def _gate_review(self, ctx: dict) -> dict:
        # Q5: 审查质量门 — 置信度加权
        findings = ctx.get("findings", [])
        blocks = [f for f in findings if f["level"] == "BLOCK"]
        warns = [f for f in findings if f["level"] == "WARN"]
        
        weighted_block = sum(
            3 if f.get("confidence") == "HIGH" else 
            2 if f.get("confidence") == "MED" else 1 
            for f in blocks
        )
        weighted_warn = sum(
            1 if f.get("confidence") == "HIGH" else 
            0.5 if f.get("confidence") == "MED" else 0.2 
            for f in warns
        )
        
        return {
            "passed": weighted_block == 0 and weighted_warn < 5,
            "weighted_score": {"block": weighted_block, "warn": weighted_warn},
            "checks": [
                {"name": "BLOCK阈值", "passed": weighted_block == 0, "detail": f"weighted_block={weighted_block}"},
                {"name": "WARN阈值", "passed": weighted_warn < 5, "detail": f"weighted_warn={weighted_warn}"},
                {"name": "Hook审计", "passed": ctx.get("hook_audit", {}).get("guard_coverage_verified", False)}
            ]
        }
```

#### 3.3.2 进化文件生命周期管理

防止规则熵增失控，进化文件设硬上限，超限自动归档。

```python
# .claude/orchestrator/evolution_manager.py
from datetime import datetime
from pathlib import Path

class EvolutionManager:
    # 进化引擎生命周期管理
    
    HARD_LIMITS = {
        "mistake-genes.md": 50,
        "coder-reminders.md": 30,
        "reviewer-metrics.md": 20,
        "coordination-log.md": 100,
    }
    
    def __init__(self, evolution_dir: str):
        self.dir = Path(evolution_dir)
    
    def record_anomaly(self, task_id: str, anomaly: dict):
        # 运行时记录：轻量、快速、不占上下文
        anomaly_file = self.dir / "anomalies" / f"{task_id}.json"
        anomaly_file.parent.mkdir(parents=True, exist_ok=True)
        
        record = {
            "timestamp": datetime.now().isoformat(),
            "task_id": task_id,
            "phase": anomaly["phase"],
            "role": anomaly["role"],
            "rule_id": anomaly["rule_id"],
            "symptom": anomaly["symptom"],
            "root_cause": anomaly.get("root_cause", ""),
            "suggested_fix": anomaly.get("suggested_fix", ""),
            "recurrence_count": 1
        }
        
        anomalies = []
        if anomaly_file.exists():
            anomalies = json.loads(anomaly_file.read_text())
        anomalies.append(record)
        anomaly_file.write_text(json.dumps(anomalies, indent=2, ensure_ascii=False))
    
    def process_review_report(self, task_id: str, report: dict):
        """处理Reviewer输出，驱动规则进化闭环"""
        
        # 1. 记录异常到运行时库
        for finding in report.get("findings", []):
            if finding.get("recurrence_count", 0) >= 2:
                self.record_anomaly(task_id, {
                    "phase": "review",
                    "role": "reviewer",
                    "rule_id": finding["rule_id"],
                    "symptom": finding["message"],
                    "root_cause": finding.get("why_hook_missed", "Unknown"),
                    "suggested_fix": finding.get("fix_code", finding.get("fix_hint", "")),
                    "recurrence_count": finding["recurrence_count"]
                })
        
        # 2. 处理Hook改进建议
        for suggestion in report.get("hook_audit", {}).get("guard_improvement_suggestions", []):
            self._append_hook_backlog(suggestion)
        
        # 3. 生成Coder提醒（即时生效，无需人工审核）
        for reminder in report.get("coder_feedback", {}).get("reminders", []):
            self._append_coder_reminder(reminder)
        
        # 4. 生成规则变更草案（需人工审核）
        draft_path = self.generate_rule_change_draft(task_id)
        
        # 5. 更新Reviewer指标
        self._update_reviewer_metrics(report.get("metrics", {}))
        
        return {
            "anomalies_recorded": len(report.get("findings", [])),
            "coder_reminders_updated": len(report.get("coder_feedback", {}).get("reminders", [])),
            "rule_change_draft": draft_path,
            "human_review_required": draft_path != ""
        }
    
    def _append_coder_reminder(self, reminder: dict):
        """Coder提醒直接追加，无需人工审核（低风险）"""
        reminders_file = self.dir / "coder-reminders.md"
        
        entry = f"""
## {datetime.now().strftime('%Y-%m-%d')} | 来源: {reminder['source_finding']}

**触发条件**: {reminder['trigger']}

**检查清单**:
{chr(10).join(f"- [ ] {item}" for item in reminder['checklist'])}

---
"""
        with open(reminders_file, "a", encoding="utf-8") as f:
            f.write(entry)
        
        self.enforce_limits()
    
    def _update_reviewer_metrics(self, metrics: dict):
        """更新Reviewer自评指标"""
        metrics_file = self.dir / "reviewer-metrics.md"
        
        entry = {
            "timestamp": datetime.now().isoformat(),
            "block_rate_per_100lines": (metrics.get("block_count", 0) / max(metrics.get("lines_reviewed", 1), 1)) * 100,
            "warn_rate_per_100lines": (metrics.get("warn_count", 0) / max(metrics.get("lines_reviewed", 1), 1)) * 100,
            "new_patterns": len(metrics.get("new_patterns", [])),
            "recurrence_triggered": sum(1 for f in metrics.get("findings", []) if f.get("recurrence_count", 0) > 1)
        }
        
        self._append_metric_entry(metrics_file, entry)
    
    def generate_rule_change_draft(self, task_id: str) -> str:
        # 离线生成《规则变更草案》
        anomaly_file = self.dir / "anomalies" / f"{task_id}.json"
        if not anomaly_file.exists():
            return ""
        
        anomalies = json.loads(anomaly_file.read_text())
        
        draft_lines = [
            f"# 规则变更草案 — 任务 {task_id}",
            f"生成时间: {datetime.now().isoformat()}",
            f"异常数量: {len(anomalies)}",
            "",
            "## 建议修改清单",
            ""
        ]
        
        for a in anomalies:
            draft_lines.extend([
                f"### {a['rule_id']} | {a['phase']} | {a['role']}",
                f"- **症状**: {a['symptom']}",
                f"- **根因**: {a['root_cause']}",
                f"- **建议修复**: {a['suggested_fix']}",
                f"- **目标规则文件**: {self._map_to_rule_file(a['rule_id'])}",
                ""
            ])
        
        draft_lines.extend([
            "---",
            "## 人工审核区",
            "- [ ] 已审核",
            "- [ ] 已修改对应规则文件",
            "- [ ] 已提交Git",
            "",
            "> 警告 AI绝不能自己修改规则文件。必须由人类工程师审核后手动修改。"
        ])
        
        draft = "\n".join(draft_lines)
        draft_path = self.dir / "drafts" / f"rule-change-{task_id}.md"
        draft_path.parent.mkdir(parents=True, exist_ok=True)
        draft_path.write_text(draft, encoding='utf-8')
        
        return str(draft_path)
    
    def enforce_limits(self):
        # 强制执行硬上限
        for filename, limit in self.HARD_LIMITS.items():
            file_path = self.dir / filename
            if not file_path.exists():
                continue
            
            content = file_path.read_text(encoding='utf-8')
            entries = self._parse_entries(content)
            
            if len(entries) <= limit:
                continue
            
            entries.sort(key=lambda e: (e.get("recurrence_count", 0), e.get("last_seen", "")), reverse=True)
            
            keep = entries[:limit]
            archive = entries[limit:]
            
            self._write_entries(file_path, keep)
            
            archive_dir = self.dir / "_archived" / datetime.now().strftime("%Y-%m")
            archive_dir.mkdir(parents=True, exist_ok=True)
            archive_file = archive_dir / f"{filename}.{datetime.now().strftime('%Y%m%d')}.md"
            self._write_entries(archive_file, archive)
    
    def _map_to_rule_file(self, rule_id: str) -> str:
        mapping = {
            "SEC-": ".claude/souls/coder/rules/sql-safety.md",
            "TRAP-": ".claude/souls/coder/rules/jnpf-expert-traps.md",
            "D2-": ".claude/souls/reviewer/rules/reviewer-discipline.md",
            "D4-": ".claude/souls/reviewer/rules/reviewer-discipline.md",
            "ARCH-": ".claude/souls/architect/rules/architecture-redlines.md",
        }
        for prefix, path in mapping.items():
            if rule_id.startswith(prefix):
                return path
        return ".claude/souls/_shared/engineering-laws.md"
```

---

### 3.4 建议5：简化流水线 + 多任务并发支持

#### 3.4.1 任务路由器（复杂度分级）

```python
class TaskRouter:
    # 任务路由器：按复杂度自动选择流水线路径
    
    def classify(self, requirement: str, changed_files_hint: List[str] = None) -> TaskLevel:
        # 自动分级规则
        file_count = len(changed_files_hint) if changed_files_hint else 0
        
        has_migration = any(kw in requirement.lower() for kw in ["迁移", "migration", "数据库", "表"])
        has_entity = any(kw in requirement.lower() for kw in ["entity", "实体", "表结构"])
        has_api = any(kw in requirement.lower() for kw in ["api", "接口", "controller"])
        is_cross_module = any(kw in requirement.lower() for kw in ["跨模块", "集成", "调用"])
        
        if file_count <= 3 and not has_entity and not has_api and not has_migration and not is_cross_module:
            return TaskLevel.C
        elif file_count <= 5 and not has_entity and not has_migration:
            return TaskLevel.B
        elif file_count <= 10 and not has_migration:
            return TaskLevel.A
        else:
            return TaskLevel.S
    
    def get_pipeline(self, level: TaskLevel) -> List[Phase]:
        pipelines = {
            TaskLevel.C: [Phase.ALIGN, Phase.BUILD, Phase.VERIFY, Phase.END],  # C级跳过REVIEW
            TaskLevel.B: [Phase.ALIGN, Phase.BRAINSTORM, Phase.BUILD, Phase.VERIFY, Phase.REVIEW, Phase.REPORT, Phase.END],
            TaskLevel.A: [Phase.ALIGN, Phase.BRAINSTORM, Phase.EXPLORE, Phase.DECOMPOSE,
                         Phase.PLAN, Phase.BUILD, Phase.VERIFY, Phase.REVIEW, Phase.REPORT, Phase.END],
            TaskLevel.S: [Phase.ALIGN, Phase.BRAINSTORM, Phase.EXPLORE, Phase.DECOMPOSE,
                         Phase.PLAN, Phase.BUILD, Phase.VERIFY, Phase.REVIEW, Phase.REPORT, Phase.END],
        }
        return pipelines.get(level, pipelines[TaskLevel.A])
```

#### 3.4.2 多任务并发调度（DAG并行）

```python
import asyncio
from concurrent.futures import ThreadPoolExecutor

class ConcurrentScheduler:
    # 并发调度器：基于DAG将无依赖子任务分发到不同LLM实例并行处理
    
    def __init__(self, pipeline: FuguPipeline, max_workers: int = 3):
        self.pipeline = pipeline
        self.max_workers = max_workers
        self.executor = ThreadPoolExecutor(max_workers=max_workers)
    
    async def run_concurrent_build(self, task_id: str, plan: dict) -> dict:
        dag = plan["dag"]
        subtasks = {s["id"]: s for s in plan["subtasks"]}
        completed = set()
        results = {}
        
        while len(completed) < len(subtasks):
            ready = [
                sid for sid in subtasks 
                if sid not in completed 
                and all(dep in completed for dep in self._get_dependencies(dag, sid))
            ]
            
            if not ready:
                raise ValueError("DAG存在环，无法调度")
            
            tasks = []
            for sid in ready:
                branch_name = f"fugu-{task_id}-{sid}"
                self._create_branch(branch_name, task_id, sid)
                
                future = self.executor.submit(
                    self._run_subtask_pipeline,  # ← 修改为完整子任务流水线
                    task_id=task_id,
                    subtask_id=sid,
                    branch=branch_name
                )
                tasks.append((sid, future))
            
            for sid, future in tasks:
                try:
                    result = future.result(timeout=300)
                    results[sid] = result
                    completed.add(sid)
                    self._merge_branch(f"fugu-{task_id}-{sid}", task_id)
                except Exception as e:
                    results[sid] = {"status": "FAILED", "error": str(e)}
        
        return results
    
    def _run_subtask_pipeline(self, task_id: str, subtask_id: str, branch: str) -> dict:
        """执行单个子任务的完整流水线（BUILD + VERIFY + REVIEW）"""
        subprocess.run(["git", "checkout", branch], cwd=self.pipeline.root, check=True)
        
        # 子任务状态隔离
        subtask_state = self.pipeline._load_state(task_id)
        subtask_state["current_subtask_id"] = subtask_id
        
        # 阶段1: BUILD
        subtask_state["phase"] = Phase.BUILD.value
        build_result = self.pipeline.run_task(task_id)
        
        # 阶段2: VERIFY
        subtask_state["phase"] = Phase.VERIFY.value
        verify_result = self.pipeline.run_task(task_id)
        
        # 阶段3: REVIEW（B级以上任务）
        task_level = subtask_state.get("task_level", "A")
        if task_level in ["A", "S"]:
            subtask_state["phase"] = Phase.REVIEW.value
            review_result = self.pipeline.run_task(task_id)
        
        return {
            "status": "SUCCESS",
            "changed_files": build_result.get("changed_files", []),
            "branch": branch
        }
    
    def _merge_branch(self, branch: str, task_id: str):
        result = subprocess.run(
            ["git", "merge", "--no-ff", branch, "-m", f"Auto-merge {branch}"],
            cwd=self.pipeline.root,
            capture_output=True, text=True
        )
        
        if result.returncode != 0:
            conflict_report = self._generate_conflict_report(branch, result.stderr)
            raise MergeConflictException(conflict_report)
```

---

### 3.5 超级审查引擎（Reviewer L1）— 核心新增

#### 3.5.1 双防线定位

| 维度         | Hook L0                        | Reviewer L1                                    |
| :----------- | :----------------------------- | :--------------------------------------------- |
| **执行时机** | 每次Write/Edit落盘前（实时）   | 阶段BUILD完成后（批量）                        |
| **能力边界** | 正则/AST静态扫描，无上下文理解 | 全量上下文语义分析，跨文件关联                 |
| **判定精度** | 确定性：命中即BLOCK            | 概率性：置信度分级（HIGH/MED/LOW）             |
| **失败处理** | 硬熔断，强制回退               | 状态机根据分级执行不同流转策略                 |
| **进化能力** | 无，规则静态                   | 有，发现新模式→更新规则→预防复发               |
| **关系**     | **第一道防线**：拦截已知模式   | **第二道防线**：发现未知模式，验证Hook是否漏检 |

**核心原则**：Hook负责"已知危险的确定性拦截"，Reviewer负责"未知风险的语义级发现 + 规则进化"。两者互补，不可替代。

#### 3.5.2 Reviewer输入Schema（状态机组装）

```json
// workspace/{task_id}/review_input.json — 状态机组装，非LLM生成
{
  "$schema": "fugu/review-input-v1",
  "task_id": "TASK-20260625-001",
  "phase": "review",
  "role": "reviewer",
  
  "tunnel_vision": {
    "scope": "subtask",
    "subtask_id": "ST-002",
    "subtask_name": "Entity定义",
    "acceptance_criteria": "编译通过，字段与迁移一致，含TenantId"
  },
  
  "artifacts": {
    "code_diff": "workspace/TASK-20260625-001/code_diff_ST-002.json",
    "test_report": "workspace/TASK-20260625-001/test_report.json",
    "security_scan": "workspace/TASK-20260625-001/security_scan_build.json",
    "guard_flags": ".claude/review/flags/Domain_Entities_OrderEntity.cs.json"
  },
  
  "context_budget": {
    "max_files": 5,
    "max_lines_per_file": 100,
    "include_dependency_outputs": true
  },
  
  "rules_digest": {
    "architecture_redlines": ["R1", "R3", "R7"],
    "expert_traps": ["Trap-2", "Trap-8"],
    "engineering_laws": ["Law-2"]
  }
}
```

#### 3.5.3 Reviewer输出Schema（LLM必须严格输出）

```json
// workspace/{task_id}/review_report.json
{
  "$schema": "fugu/review-report-v1",
  "task_id": "TASK-20260625-001",
  "phase": "review",
  "subtask_id": "ST-002",
  "timestamp": "2026-06-25T10:30:00Z",
  "role": "reviewer",
  
  "pre_screen": {
    "guard_flags_read": true,
    "flagged_files_count": 2,
    "focus_priority": ["Domain/Entities/OrderEntity.cs", "Application/Services/OrderService.cs"]
  },
  
  "findings": [
    {
      "id": "REV-001",
      "level": "BLOCK",
      "confidence": "HIGH",
      "dimension": "D3",
      "rule_id": "TRAP-002",
      "file": "Domain/Entities/OrderEntity.cs",
      "line": 32,
      "message": "Mapster Adapt未排除审计字段，CreateTime可被覆盖",
      "evidence": "dto.Adapt(entity); // 无.Ignore配置",
      "fix_code": "dto.Adapt(entity, c => c.Ignore(x => x.CreateTime).Ignore(x => x.CreateUserId));",
      "why_hook_missed": "guard-reviewer仅扫描字符串级Adapt，未解析类型映射",
      "recurrence_count": 3
    },
    {
      "id": "REV-002",
      "level": "WARN",
      "confidence": "MED",
      "dimension": "D4",
      "rule_id": "D4-LENGTH",
      "file": "Application/Services/OrderService.cs",
      "line": 45,
      "message": "方法OrderProcessing spans 68 lines (>50)",
      "evidence": "public async Task<OrderDto> OrderProcessing(...) { ... }",
      "fix_hint": "拆分为ValidateOrder/CalculatePrice/CreateOrder三个私有方法",
      "recurrence_count": 1
    }
  ],
  
  "hook_audit": {
    "guard_coverage_verified": true,
    "missed_by_guard": ["REV-001"],
    "false_positive_by_guard": [],
    "guard_improvement_suggestions": [
      {
        "guard_file": "guard-reviewer.mjs",
        "suggestion": "增加Roslyn语法树级Mapster配置扫描，替代字符串匹配",
        "priority": "HIGH"
      }
    ]
  },
  
  "rule_evolution": {
    "new_patterns": [
      {
        "pattern_id": "TRAP-015",
        "category": "expert_trap",
        "symptom": "Mapster Adapt在嵌套DTO时未递归排除审计字段",
        "root_cause": "开发者只关注顶层DTO，忽略嵌套对象映射",
        "suggested_fix": "在coder-reminders.md中增加'嵌套DTO映射必须递归配置Ignore'",
        "target_rule_file": ".claude/souls/coder/rules/jnpf-expert-traps.md"
      }
    ],
    "rule_updates": [
      {
        "rule_id": "TRAP-002",
        "update_type": "intensify",
        "reason": "第3次复发，需从WARN升级为BLOCK",
        "new_severity": "BLOCK"
      }
    ]
  },
  
  "coder_feedback": {
    "reminders": [
      {
        "trigger": "使用Mapster.Adapt映射到Entity",
        "checklist": [
          "检查是否.Ignore(x => x.CreateTime)",
          "检查是否.Ignore(x => x.CreateUserId)",
          "检查是否.Ignore(x => x.UpdateTime)",
          "嵌套DTO时检查递归Ignore配置"
        ],
        "source_finding": "REV-001"
      }
    ]
  },
  
  "metrics": {
    "block_count": 1,
    "warn_count": 1,
    "note_count": 0,
    "files_reviewed": 2,
    "lines_reviewed": 145,
    "review_duration_ms": 45000
  }
}
```

#### 3.5.4 Reviewer专用Hook（预筛选）

```javascript
#!/usr/bin/env node
/**
 * PostToolUse Hook — Reviewer质量门触发器
 * 
 * 职责：在代码写入后，自动触发轻量级审查检查，为Reviewer子代理预筛选。
 * 不是替代Reviewer，而是"预处理"——帮Reviewer排除明显问题，聚焦深度审查。
 * 
 * 触发条件：Write/Edit/MultiEdit完成后
 * 执行时间：< 200ms（不阻塞编辑流程）
 * 输出：写入.claude/review/flags/{file}.json，供Reviewer读取
 */

import { readStdin } from './hook-lib.mjs';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';

const STDIN_MS = 1000;
const FLAGS_DIR = '.claude/review/flags';

async function quickAudit({ filePath, content }) {
  const flags = [];
  const lines = content.split('\n');

  // 快速扫描：TODO/FIXME
  for (let i = 0; i < lines.length; i++) {
    if (/TODO|FIXME|HACK|XXX/.test(lines[i]) && !lines[i].trim().startsWith('//')) {
      flags.push({ line: i+1, rule: 'D2-TODO', level: 'WARN', msg: 'Found TODO/FIXME in code' });
    }
  }

  // 快速扫描：空 catch
  for (let i = 0; i < lines.length; i++) {
    if (/catch\s*\([^)]*\)\s*\{\s*\}/.test(lines[i])) {
      flags.push({ line: i+1, rule: 'D2-SWALLOW', level: 'BLOCK', msg: 'Empty catch block swallows exception' });
    }
  }

  // 快速扫描：方法长度（粗略）
  let methodStart = -1;
  let braceCount = 0;
  for (let i = 0; i < lines.length; i++) {
    if (/^\s*(public|private|protected|internal)\s+/.test(lines[i]) && /\{/.test(lines[i])) {
      methodStart = i;
      braceCount = 1;
    } else if (methodStart >= 0) {
      braceCount += (lines[i].match(/\{/g) || []).length;
      braceCount -= (lines[i].match(/\}/g) || []).length;
      if (braceCount === 0) {
        const methodLen = i - methodStart + 1;
        if (methodLen > 50) {
          flags.push({ line: methodStart+1, rule: 'D4-LENGTH', level: 'WARN', msg: `Method spans ${methodLen} lines (>50)` });
        }
        methodStart = -1;
      }
    }
  }

  // 快速扫描：魔法数字（粗略）
  for (let i = 0; i < lines.length; i++) {
    const magic = lines[i].match(/[^\"'](\b\d{3,}\b)/);
    if (magic && !lines[i].trim().startsWith('//')) {
      flags.push({ line: i+1, rule: 'D4-MAGIC', level: 'NOTE', msg: `Magic number: ${magic[1]}` });
    }
  }

  return flags;
}

try {
  let input = {};
  try {
    const raw = await readStdin(STDIN_MS);
    if (raw.trim()) input = JSON.parse(raw);
  } catch {
    process.exit(0);
  }

  const filePath = (input.tool_input?.file_path || '').replace(/\\/g, '/');
  const toolName = input.tool_name || '';

  if (!['Write', 'Edit', 'MultiEdit'].includes(toolName)) {
    process.exit(0);
  }

  let content = '';
  if (toolName === 'Write') {
    content = input.tool_input?.content || '';
  } else if (toolName === 'Edit') {
    content = input.tool_input?.newText || input.tool_input?.new_string || '';
  } else if (toolName === 'MultiEdit') {
    const edits = input.tool_input?.edits || [];
    content = edits.map(e => e.new_string || e.newText || '').filter(Boolean).join('\n');
  }

  if (!content) process.exit(0);

  const flags = await quickAudit({ filePath, content });

  const flagPath = join(process.cwd(), FLAGS_DIR, `${filePath.replace(/[\\/]/g, '_')}.json`);
  mkdirSync(join(process.cwd(), FLAGS_DIR), { recursive: true });
  writeFileSync(flagPath, JSON.stringify({
    filePath,
    timestamp: Date.now(),
    flags,
    summary: {
      BLOCK: flags.filter(f => f.level === 'BLOCK').length,
      WARN: flags.filter(f => f.level === 'WARN').length,
      NOTE: flags.filter(f => f.level === 'NOTE').length,
    }
  }, null, 2));

  const blocks = flags.filter(f => f.level === 'BLOCK');
  if (blocks.length > 0) {
    console.error(`[guard-reviewer] ⚠️ ${blocks.length} BLOCK-level issues pre-detected in ${filePath}:`);
    blocks.forEach(b => console.error(`  Line ${b.line}: ${b.msg}`));
    console.error(`  Reviewer MUST verify in Phase 6.`);
  }

  process.exit(0);

} catch (e) {
  console.error('[guard-reviewer] Error:', e.message);
  process.exit(0);
}
```

#### 3.5.5 Reviewer审查维度（5维度×3级别）

```markdown
# .claude/souls/reviewer/rules/reviewer-discipline.md

# Reviewer 纪律 — 质量审查专用规则包

> 定位：Reviewer角色的唯一规则来源。其他规则文件的审查维度已聚合于此。
> 加载时机：Phase REVIEW阶段，按需加载（L2层）。
> 设计原则：Reviewer不需要知道"规则为什么存在"，只需要知道"如何检查"和"如何分级"。

---

## 审查维度速查表（5维度 × 3级别）

| 维度 | 检查项 | 自动验证 | 人工确认 | 置信度 |
|:---|:---|:---:|:---:|:---:|
| **D1 架构合规** | R1-R10红线 | Hook L0已拦截 | Reviewer复核 | HIGH |
| **D2 工程铁律** | TODO/吞异常/未验证假设 | grep扫描 | Reviewer判断 | MED |
| **D3 专家陷阱** | Trap 1-14 | 部分可工具验证 | Reviewer深度检查 | LOW-MED |
| **D4 代码质量** | 方法长度/重复/命名 | 工具可部分覆盖 | Reviewer判断 | MED |
| **D5 测试覆盖** | 新增代码是否有测试 | 文件存在性检查 | Reviewer判断 | HIGH |

> **关键优化**：D1（架构合规）的R1-R10已由Hook L0在写入时硬阻断，Reviewer**不需要重复检查**，只需确认"Hook是否漏检"（极低概率）。Reviewer的精力应集中在D2-D5。

---

## 审查输出格式（三级质量门）

### 🔴 BLOCK（必须修复，阻塞流程）

触发条件：
- 发现Hook L0漏检的架构红线违规
- 发现可导致生产事故的代码（如：SQL注入绕过参数化查询）
- 发现严重性能问题（如：无分页的全表查询）

输出格式：
```
[BLOCK] {规则ID} | 置信度: {HIGH/MED/LOW} | 文件:行号
  问题: {一句话描述}
  证据: {代码片段}
  修复: {具体代码}
  为什么Hook没拦住: {分析原因，用于优化Hook}
```

### 🟡 WARN（建议修复，不阻塞但需记录）

触发条件：
- 代码异味（方法>50行、重复代码、魔法值）
- 边界条件未处理（null检查、并发安全）
- 测试覆盖不足（新增逻辑无对应测试）

输出格式：
```
[WARN] {规则ID} | 置信度: {HIGH/MED/LOW} | 文件:行号
  问题: {描述}
  风险: {不修复的后果}
  建议: {具体改进方案}
  是否记录到tech-debt: {是/否}
```

### 🟢 NOTE（信息提示，仅记录）

触发条件：
- 代码风格偏好（与项目惯例不一致但不影响功能）
- 可优化的实现方式（有更简洁的写法）
- 文档缺失（公共方法无XML注释）

输出格式：
```
[NOTE] {规则ID} | 文件:行号
  提示: {描述}
  参考: {可选的改进示例}
```

---

## 自动验证工具链（Reviewer专用）

Reviewer在审查时，MUST调用以下工具验证，不能仅靠肉眼：

| 工具 | 用途 | 命令 |
|:---|:---|:---|
| `grep-audit` | 扫描TODO/吞异常/硬编码 | `grep -n "TODO\\|FIXME\\|catch.*{}\\|throw new Exception"` |
| `mapster-check` | 验证Adapt是否覆盖审计字段 | 自定义脚本：检查`Adapt`调用前后是否有`.Ignore` |
| `pagination-check` | 验证列表查询是否有分页 | `grep -n "ToListAsync\\|ToPageListAsync"` |
| `async-suffix-check` | 验证IDynamicApiController方法无Async后缀 | `grep -n "Async\\s*("` |
| `tenant-filter-verify` | 验证原生SQL是否含TenantId | `grep -n "SqlQuery\\|SqlQueryable"`后人工确认 |

---

## 反馈闭环协议（Reviewer → 规则进化）

Reviewer发现的问题，MUST按以下路径反馈：

```
发现新问题（不在现有规则中）
  │
  ├─ 是架构红线遗漏？ → 更新architecture-redlines.md（R11+）
  │                     更新Hook覆盖矩阵
  │                     更新test-hooks.mjs用例
  │
  ├─ 是专家陷阱遗漏？ → 更新jnpf-expert-traps.md（Trap 15+）
  │                     在reviewer-discipline.md中标记检查方法
  │
  ├─ 是Hook误报/漏报？ → 更新对应guard-*.mjs
  │                      更新Hook的测试用例
  │
  └─ 是代码模式问题？ → 更新reviewer-discipline.md的自动验证工具链
                        更新code-reviewer子代理Prompt
```

---

## 关联文件

- 架构红线（R1-R10）→ architecture-redlines.md（Reviewer不复检，只确认Hook覆盖）
- 工程铁律 → engineering-laws.md（Reviewer加载Law 2的验证方法论）
- 专家陷阱 → jnpf-expert-traps.md（Reviewer加载Trap检查清单）
- 审查工作流 → review-workflow.md（Reviewer的执行流程）
```

#### 3.5.6 Reviewer Soul：无状态专家函数

```markdown
# .claude/souls/reviewer/soul.md

## 身份定义

你是**质量进化引擎（Quality Evolution Engine）**，不是"找茬的"。
你的唯一使命：在最小上下文中，发现Hook硬约束无法捕获的语义级风险，并驱动规则进化防止复发。

## 核心约束

1. **物理隔离**：你是无状态函数。每次调用都是全新会话。你不记得任何历史审查。
2. **隧道视野**：你只审查当前子任务的代码变更。看不到其他子任务、看不到完整架构。
3. **确定性输出**：你必须输出严格符合`fugu/review-report-v1`Schema的JSON。禁止任何自然语言前缀。
4. **Hook审计义务**：你必须显式审计guard-reviewer的覆盖情况，标注漏检和误报。
5. **进化义务**：你发现的新模式必须转化为结构化规则进化建议。禁止只报告不进化。

## 输入

- 当前子任务的代码变更（JSON）
- 测试结果（JSON）
- 安全扫描结果（JSON）
- guard-reviewer标志文件（JSON，仅相关文件）
- 该子任务类型的历史复发记录（JSON）

## 输出

- review_report.json（严格Schema）

## 禁止

- 输出自然语言闲聊
- 重复检查Hook L0已拦截的内容（除非确认漏检）
- 看到完整plan.json或architecture.json
- 直接修改任何规则文件（只输出建议草案）
```

---

## 四、施工计划：分阶段落地

### 阶段一：基础设施铸造（Day 1-2，16小时）

**目标**：搭建外部状态机骨架 + 安全守卫 + 文件级交接契约 + Reviewer硬阶段。

| 步骤 | 任务                   | 产出物                                                       | 验收标准                                                 |
| :--- | :--------------------- | :----------------------------------------------------------- | :------------------------------------------------------- |
| 1.1  | 创建项目目录结构       | `.claude/orchestrator/`, `workspace/`, `.claude/guards/`     | `ls -la`验证                                             |
| 1.2  | 实现状态机核心         | `state_machine.py`                                           | 能执行完整S级流水线（Mock LLM），含REVIEW/REVIEW_FIX阶段 |
| 1.3  | 实现安全守卫           | `security_scanner.py`                                        | SQL注入/租户隔离/权限扫描全部通过单元测试                |
| 1.4  | 定义JSON Schema        | `schemas/architecture-v1.json`, `plan-v1.json`, `code-v1.json`, `review-input-v1.json`, `review-report-v1.json` | JSON Schema验证通过                                      |
| 1.5  | 实现质量门引擎         | `quality_gate_engine.py`                                     | 编译/测试/安全/审查门可硬执行，含置信度加权              |
| 1.6  | 实现任务路由器         | `task_router.py`                                             | C/S级任务走不同路径，C级跳过REVIEW                       |
| 1.7  | 实现Reviewer预筛选Hook | `guard-reviewer.mjs`                                         | PostToolUse触发，<200ms，生成标志文件                    |

### 阶段二：角色灵魂迁移（Day 3，8小时）

**目标**：将V1.0的灵魂文件适配为V3.0的"无状态专家函数"格式，新增Reviewer角色。

| 步骤 | 任务                   | 产出物                                                | 验收标准                           |
| :--- | :--------------------- | :---------------------------------------------------- | :--------------------------------- |
| 2.1  | 重写协调者灵魂         | `.claude/souls/orchestrator/soul.md`                  | 明确声明"我不做决策，我只调度"     |
| 2.2  | 重写六角色灵魂         | 6个`soul.md`                                          | 每个文件增加"输出JSON Schema"章节  |
| 2.3  | **新建Reviewer灵魂**   | `.claude/souls/reviewer/soul.md`                      | 含"质量进化引擎"身份定义 + 5大约束 |
| 2.4  | **新建Reviewer规则包** | `.claude/souls/reviewer/rules/reviewer-discipline.md` | 5维度×3级别×工具链 + 反馈闭环协议  |
| 2.5  | 迁移规则文件           | `.claude/souls/{role}/rules/`                         | 规则按角色隔离                     |
| 2.6  | 创建共享规则           | `.claude/souls/_shared/`                              | assertion-discipline等             |

### 阶段三：进化引擎与生命周期（Day 4，6小时）

| 步骤 | 任务             | 产出物                                                       | 验收标准                                               |
| :--- | :--------------- | :----------------------------------------------------------- | :----------------------------------------------------- |
| 3.1  | 实现进化管理器   | `evolution_manager.py`                                       | 能记录异常、生成草案、强制执行硬上限、处理Reviewer报告 |
| 3.2  | 创建进化文件模板 | `mistake-genes.md`, `coder-reminders.md`, `reviewer-metrics.md` | 含Schema模板                                           |
| 3.3  | 实现归档机制     | `_archived/evolution/`                                       | 超限自动归档                                           |
| 3.4  | 集成到状态机     | 修改`state_machine.py`                                       | REVIEW阶段通过后自动调用`process_review_report`        |

### 阶段四：并发与快速通道（Day 5，6小时）

| 步骤 | 任务            | 产出物                    | 验收标准                                                  |
| :--- | :-------------- | :------------------------ | :-------------------------------------------------------- |
| 4.1  | 实现并发调度器  | `concurrent_scheduler.py` | 能并行执行2+无依赖子任务，每个子任务含BUILD+VERIFY+REVIEW |
| 4.2  | 实现Git分支管理 | 分支创建/合并逻辑         | 无冲突时自动合并                                          |
| 4.3  | 实现合并Agent   | `merge_agent.py`          | 冲突时生成结构化报告                                      |
| 4.4  | 端到端测试      | 完整C级 + S级任务测试     | C级<5分钟，S级<30分钟                                     |

### 阶段五：整合验证（Day 6，4小时）

| 步骤 | 任务                   | 验收标准                                                     |
| :--- | :--------------------- | :----------------------------------------------------------- |
| 5.1  | 安全守卫拦截测试       | 故意注入SQL拼接代码，状态机必须BLOCK并回退                   |
| 5.2  | 物理隔离验证           | 检查LLM调用日志，确认每次调用都是全新会话                    |
| 5.3  | 隧道视野验证           | 检查Build/Review阶段的prompt，确认不含完整plan.json          |
| 5.4  | 熔断机制验证           | 连续3次失败，状态机必须报警并停止                            |
| 5.5  | **Reviewer双防线验证** | Hook漏检Mapster问题，Reviewer必须发现并标注`why_hook_missed` |
| 5.6  | **进化闭环验证**       | Reviewer发现新问题 -> anomalies.json -> 规则变更草案 -> 人工确认 -> coder-reminders更新 |
| 5.7  | **质量门分级验证**     | HIGH级BLOCK立即熔断，MED级WARN累积5个才警告                  |

---

## 五、最终文件结构

```
.claude/
├── CLAUDE.md                              <- 人类工程师入口（精简版，<=100行）
├── orchestrator/                          <- 外部状态机（新增核心）
│   ├── state_machine.py                   <- 阶段流转、回退、熔断、REVIEW硬阶段
│   ├── task_router.py                     <- 复杂度分级、流水线选择（C级跳过REVIEW）
│   ├── quality_gate_engine.py             <- 质量门硬执行（含Q5置信度加权）
│   ├── evolution_manager.py               <- 进化引擎生命周期（含Reviewer报告处理）
│   ├── concurrent_scheduler.py            <- DAG并行调度（含子任务完整流水线）
│   ├── merge_agent.py                     <- 分支冲突处理
│   └── schemas/                           <- JSON Schema定义
│       ├── architecture-v1.json
│       ├── plan-v1.json
│       ├── code-v1.json
│       ├── review-input-v1.json           <- 新增
│       └── review-report-v1.json          <- 新增
│
├── souls/                                 <- 七角色灵魂（精简、无状态化）
│   ├── orchestrator/
│   │   └── soul.md                        <- "我只调度，不决策"
│   ├── architect/
│   │   ├── soul.md
│   │   └── rules/
│   │       ├── architecture-redlines.md
│   │       └── low-code-principles.md
│   ├── planner/
│   │   ├── soul.md
│   │   └── rules/
│   ├── coder/
│   │   ├── soul.md
│   │   └── rules/
│   │       ├── jnpf-expert-traps.md
│   │       ├── sql-safety.md
│   │       └── frontend-memory-leak.md
│   ├── tester/
│   │   ├── soul.md
│   │   └── rules/
│   ├── reviewer/                          <- 新增：超级审查引擎
│   │   ├── soul.md                        <- "质量进化引擎"身份定义
│   │   └── rules/
│   │       └── reviewer-discipline.md     <- 5维度×3级别×工具链
│   ├── reporter/
│   │   ├── soul.md
│   │   └── rules/
│   └── _shared/                           <- 共享规则
│       ├── assertion-discipline.md
│       ├── engineering-laws.md
│       └── workflow-pipeline.md
│
├── guards/                                <- 安全守卫 + 精简Hook + Reviewer预筛选
│   ├── security_scanner.py              <- SQL注入/租户隔离/权限扫描
│   ├── guard-reviewer.mjs               <- 新增：PostToolUse预筛选标志
│   ├── guard-bash.mjs
│   ├── guard-skill-load.mjs
│   ├── guard-finish.mjs
│   ├── session-scheduler.mjs
│   └── hook-lib.mjs
│
├── evolution/                             <- 进化引擎（离线闭环）
│   ├── README.md
│   ├── mistake-genes.md                   <- 硬上限50条
│   ├── coder-reminders.md                 <- 硬上限30条
│   ├── reviewer-metrics.md                <- 硬上限20条
│   ├── coordination-log.md                <- 硬上限100条
│   ├── anomalies/                         <- 运行时异常记录（任务级）
│   │   └── TASK-xxx.json
│   ├── drafts/                            <- 规则变更草案（待人工审核）
│   │   └── rule-change-TASK-xxx.md
│   └── _archived/                         <- 自动归档区
│       └── 2026-06/
│           └── mistake-genes.md.20260625.md
│
├── review/                                <- 新增：Reviewer运行时数据
│   └── flags/                             <- guard-reviewer生成的标志文件
│       └── Domain_Entities_OrderEntity.cs.json
│
├── brain/                                 <- 协调者灵魂（保留，但弱化）
│   └── orchestrator.md                    <- 仅作为soul.md的备用参考
│
└── _archived/                             <- V1.0历史归档
    ├── rules/
    └── hooks/

workspace/                                 <- 文件级交接契约（运行时生成）
└── TASK-20260625-001/
    ├── state.json                         <- 状态机持久化状态
    ├── requirements.md                    <- 原始需求
    ├── architecture.json                  <- 架构师产出
    ├── plan.json                          <- 规划师产出
    ├── code_diff.json                     <- 开发者产出（按子任务）
    ├── test_report.json                   <- 测试员产出
    ├── review_input.json                  <- 新增：审查员输入（状态机组装）
    ├── review_report.json                 <- 新增：审查员产出
    ├── delivery_report.md                 <- 报告员产出
    └── security_scan_*.json               <- 安全扫描证据
```

---

## 六、关键设计决策记录（Decisions）

| 决策                               | 理由                                                   | 放弃的方案                             | 风险                         |
| :--------------------------------- | :----------------------------------------------------- | :------------------------------------- | :--------------------------- |
| 外部状态机用Python而非Node.js      | Python在系统调用、Git操作、并发控制上更成熟            | Node.js（团队更熟悉但生态弱于Python）  | 团队需要学习Python           |
| 强制JSON输出用tool_use而非文本解析 | tool_use从API层面锁死结构化输出，可靠性100%            | 依赖正则提取JSON（V1.0方案，经常失败） | 绑定Anthropic API，迁移成本  |
| 安全扫描在落盘前执行               | 防止任何不安全代码进入文件系统                         | 依赖Hook拦截（有漏报窗口）             | 扫描耗时可能增加流水线时间   |
| **Reviewer作为硬阶段而非子代理**   | 符合V2.0"外部状态机硬编码调度"核心范式，拥有独立质量门 | V1.0的"协调者spawn子代理"方案          | 状态机复杂度增加             |
| **置信度加权质量门**               | 避免LOW置信度BLOCK过度阻塞流水线                       | 二元PASS/FAIL                          | 阈值调参成本                 |
| **Reviewer审计Hook义务**           | 形成"软审查反哺硬约束"的闭环                           | Hook和Reviewer各自为政                 | Reviewer负载增加10%          |
| 进化引擎离线闭环                   | 防止AI自噬基因，确保规则质量                           | 允许AI直接修改规则文件（V1.0方案）     | 人工审核延迟，规则更新慢     |
| C级任务跳过架构师和Reviewer        | 小任务不需要架构设计和深度审查，避免过度工程           | 所有任务强制走完整七角色（V1.0方案）   | 可能遗漏简单任务的跨模块影响 |
| 并发用Git分支隔离                  | 物理隔离写入，冲突可追踪                               | 同一分支并发写入（冲突不可控）         | Git合并冲突需要人工介入      |
| 隧道视野限制Reviewer上下文         | 防止上下文污染，确保可扩展性                           | 给Reviewer全量代码库                   | 可能遗漏跨子任务风险         |
| Coder提醒即时生效                  | 低风险控制，加速预防复发                               | 所有进化都走人工审核                   | 人工审核延迟导致问题复发     |

---

## 七、风险与兜底

| 风险                           | 概率 | 影响                      | 兜底方案                                                     |
| :----------------------------- | :--- | :------------------------ | :----------------------------------------------------------- |
| LLM JSON输出格式错误           | 中   | 阶段回退，浪费时间        | 解析熔断：失败即回退，3次熔断报警                            |
| 安全扫描误报（BLOCK正常代码）  | 低   | 阻塞流水线                | 白名单机制：`// PARAM_SAFE`注释降级                          |
| 并发子任务Git冲突              | 中   | 合并失败                  | 冲突报告生成，交由合并Agent或人工处理                        |
| 进化文件硬上限导致重要记录丢失 | 低   | 知识流失                  | 按复发次数+时间双维度排序，重要记录自然保留                  |
| 状态机本身Bug导致死循环        | 低   | 资源耗尽                  | 全局超时（S级任务30分钟强制终止）+ 人工报警                  |
| 小任务快速通道遗漏风险         | 中   | 技术债累积                | C级任务强制记录到tech-debt.md，定期人工Review                |
| **Reviewer置信度误判**         | 中   | 高风险问题被标记为LOW逃脱 | 复发次数自动升级机制：第3次出现强制升级为BLOCK               |
| **Reviewer Hook审计遗漏**      | 低   | Hook漏检未被发现          | 状态机强制检查`guard_coverage_verified`字段，未审计即质量门失败 |

---

## 八、一句话总结

> **V3.0的本质**：把LLM从"流程控制者"降级为"无状态的计算函数"，由外部Python状态机接管所有确定性逻辑（流转、回退、熔断、安全、质量门、并发），让LLM只专注于它唯一擅长的事——**在最小上下文中，用专家思维解决单一问题**。同时，通过**超级审查引擎（Reviewer L1）**构建第二道质量防线，实现"Hook硬约束守底线 + Reviewer语义审计发现未知风险 + 进化闭环越审查越聪明"的工业级质量飞轮。

按此计划施工，预计 **6天/40小时** 完成核心骨架，第7天可投入生产试用。





 我来分析现有的21个Hook和15个规则文件，然后输出**V3.0重构后的完整整理方案**。

---

## 一、现状诊断：乱在哪

### Hook层（21个文件）问题

| 问题                   | 具体表现                                                     |
| :--------------------- | :----------------------------------------------------------- |
| **职责重叠**           | `guard-finish.mjs`（L0-L4验证）与 `post-build-verify.mjs`（build→test标志）与 `verify-mistake-log.mjs`（错题本）三重验证逻辑分散 |
| **触发时机混乱**       | 有的PreToolUse、有的PostToolUse、有的StopHook、有的SessionStart，没有统一调度 |
| **状态管理碎片化**     | `workflow-state.json`（guard-workflow）、`.build-verify-flag.json`（post-build-verify）、`.session-init-lock.json`（session-scheduler）、`.skill-load-state.json`（hook-lib）四个状态文件各自为政 |
| **硬编码规则散落**     | R4/R5/R6/R7/R8的红线分别在不同的guard-*文件中，没有统一配置源 |
| **Reviewer预筛选缺失** | 只有`guard-reviewer.mjs`是概念，未实际集成到V1.0体系         |

### Rules层（15个文件）问题

| 问题                     | 具体表现                                                     |
| :----------------------- | :----------------------------------------------------------- |
| **重复内容严重**         | Gate Function在`engineering-laws.md`和`testing.md`重复；架构红线R1-R10在`architecture-redlines.md`和`jnpf-expert-traps.md`重复；Phase流水线在`workflow.md`、`workflow-pipeline.md`、`CLAUDE.md`三重定义 |
| **加载策略混乱**         | `rules-loader.md`设计了L0-L3分层，但实际没有执行机制，AI长会话后加载量失控 |
| **缺乏Schema约束**       | 所有规则都是自然语言，没有结构化字段（rule_id、level、trigger、hook_file、test_case），无法被状态机程序化消费 |
| **Reviewer无独立规则包** | `review-workflow.md`只是Prompt模板，没有独立的`reviewer-discipline.md`规则子集 |

---

## 二、V3.0整理原则

```
┌─────────────────────────────────────────────────────────────┐
│  整理目标：从"21+15个散装文件" → "状态机统一调度 + Schema化规则库" │
├─────────────────────────────────────────────────────────────┤
│  1. Hook职能下沉：状态机硬编码 > Hook脚本分散拦截              │
│  2. 规则Schema化：每条规则必须有[规则ID][级别][触发条件][Hook映射] │
│  3. 状态统一：所有运行时状态归一到workspace/{task_id}/state.json │
│  4. 去重归一：重复内容只保留单一信源，其他文件改为引用链接      │
│  5. 隧道视野：Reviewer/Coder等角色只加载其专属规则子集         │
└─────────────────────────────────────────────────────────────┘
```

---

## 三、Hook整理方案：21个 → 6个核心 + 1个库

### 3.1 删除/合并清单

| 原文件                   | 处置                           | 理由                                                |
| :----------------------- | :----------------------------- | :-------------------------------------------------- |
| `guard-workflow.mjs`     | **删除，职能上移状态机**       | 状态机硬编码阶段流转，不再需要Hook检查SP调用        |
| `post-build-verify.mjs`  | **删除，职能上移状态机**       | 状态机Q3/Q4质量门直接执行编译/测试，不需要标志文件  |
| `verify-mistake-log.mjs` | **删除，职能上移状态机**       | 状态机Phase 7强制检查，不需要独立Hook               |
| `guard-finish.mjs`       | **保留但精简为L4-E2E证据检查** | L0-L3已由状态机质量门覆盖，只保留前端截图新鲜度检查 |
| `format-and-lint.mjs`    | **删除，职能上移状态机**       | 状态机Q3硬执行`dotnet format --verify-no-changes`   |
| `smart-post-hook.mjs`    | **删除，职能上移状态机**       | 状态机统一调度eslint，不需要分散Hook                |
| `skill-reminder.mjs`     | **删除，职能上移状态机**       | 状态机根据任务级别自动提醒，不需要Hook              |
| `superpowers-check.mjs`  | **删除**                       | 状态机不依赖superpowers插件，改用原生LLM调用        |
| `collect-summary.mjs`    | **保留，但改为状态机触发**     | SessionEnd时状态机调用，非自动Hook                  |
| `load-mistakes.mjs`      | **删除，职能上移状态机**       | 状态机组装prompt时注入`coder-reminders.md`          |
| `session-scheduler.mjs`  | **保留，精简为入口守卫**       | 只负责防重入+轻量初始化，不再加载规则               |
| `guard-skill-load.mjs`   | **保留**                       | Skill风暴防护仍有价值                               |

### 3.2 保留的6个核心Hook + 1个库

```
.claude/hooks/
├── hook-lib.mjs                    # 共享库（保留，精简）
├── session-scheduler.mjs           # SessionStart：防重入+轻量初始化
├── guard-skill-load.mjs           # PreToolUse(Skill)：限速
├── guard-bash.mjs                 # PreToolUse(Bash)：危险命令拦截
├── guard-write.mjs                # PreToolUse(Write/Edit)：L1安全扫描
├── guard-reviewer.mjs             # PostToolUse：预筛选标志（V3.0新增）
└── guard-finish.mjs               # StopHook：精简为L4-E2E证据检查
```

### 3.3 保留Hook的精简后源码

#### `session-scheduler.mjs`（精简版）

```javascript
#!/usr/bin/env node
/**
 * session-scheduler.mjs — V3.0 精简入口
 * 职责：仅防重入 + 标记会话开始。所有规则加载由状态机控制。
 */
import { shouldSkipSessionInit, markSessionInit, getProjectRoot } from './hook-lib.mjs';

const skip = shouldSkipSessionInit('startup');
if (skip.skip) {
  console.error(`[session-scheduler] 跳过 (${skip.reason})`);
  process.exit(0);
}

markSessionInit('startup');
console.error('[session-scheduler] JNPF V3.0 SessionStart');
process.exit(0);
```

#### `guard-reviewer.mjs`（V3.0核心新增）

```javascript
#!/usr/bin/env node
/**
 * PostToolUse Hook — Reviewer L0 预筛选
 * 职责：代码写入后生成轻量级审查标志，供Reviewer L1读取
 * 执行时间：<200ms，不阻塞编辑流程
 */
import { readStdin } from './hook-lib.mjs';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';

const FLAGS_DIR = '.claude/review/flags';
const STDIN_MS = 1000;

async function quickAudit({ filePath, content }) {
  const flags = [];
  const lines = content.split('\n');

  // D2: TODO/FIXME
  for (let i = 0; i < lines.length; i++) {
    if (/TODO|FIXME|HACK|XXX/.test(lines[i]) && !lines[i].trim().startsWith('//')) {
      flags.push({ line: i+1, rule: 'D2-TODO', level: 'WARN', msg: 'Found TODO/FIXME' });
    }
  }

  // D2: 空catch
  for (let i = 0; i < lines.length; i++) {
    if (/catch\s*\([^)]*\)\s*\{\s*\}/.test(lines[i])) {
      flags.push({ line: i+1, rule: 'D2-SWALLOW', level: 'BLOCK', msg: 'Empty catch block' });
    }
  }

  // D4: 方法长度
  let methodStart = -1, braceCount = 0;
  for (let i = 0; i < lines.length; i++) {
    if (/^\s*(public|private|protected|internal)\s+/.test(lines[i]) && /\{/.test(lines[i])) {
      methodStart = i; braceCount = 1;
    } else if (methodStart >= 0) {
      braceCount += (lines[i].match(/\{/g) || []).length;
      braceCount -= (lines[i].match(/\}/g) || []).length;
      if (braceCount === 0) {
        const methodLen = i - methodStart + 1;
        if (methodLen > 50) {
          flags.push({ line: methodStart+1, rule: 'D4-LENGTH', level: 'WARN', msg: `Method ${methodLen} lines` });
        }
        methodStart = -1;
      }
    }
  }

  // D4: 魔法数字
  for (let i = 0; i < lines.length; i++) {
    const magic = lines[i].match(/[^\"'](\b\d{3,}\b)/);
    if (magic && !lines[i].trim().startsWith('//')) {
      flags.push({ line: i+1, rule: 'D4-MAGIC', level: 'NOTE', msg: `Magic number: ${magic[1]}` });
    }
  }

  return flags;
}

try {
  let input = {};
  try {
    const raw = await readStdin(STDIN_MS);
    if (raw.trim()) input = JSON.parse(raw);
  } catch { process.exit(0); }

  const filePath = (input.tool_input?.file_path || '').replace(/\\/g, '/');
  const toolName = input.tool_name || '';
  if (!['Write', 'Edit', 'MultiEdit'].includes(toolName)) process.exit(0);

  let content = '';
  if (toolName === 'Write') content = input.tool_input?.content || '';
  else if (toolName === 'Edit') content = input.tool_input?.newText || '';
  else if (toolName === 'MultiEdit') {
    const edits = input.tool_input?.edits || [];
    content = edits.map(e => e.new_string || '').filter(Boolean).join('\n');
  }
  if (!content) process.exit(0);

  const flags = await quickAudit({ filePath, content });
  const flagPath = join(process.cwd(), FLAGS_DIR, `${filePath.replace(/[\\/]/g, '_')}.json`);
  mkdirSync(join(process.cwd(), FLAGS_DIR), { recursive: true });
  
  writeFileSync(flagPath, JSON.stringify({
    filePath, timestamp: Date.now(), flags,
    summary: {
      BLOCK: flags.filter(f => f.level === 'BLOCK').length,
      WARN: flags.filter(f => f.level === 'WARN').length,
      NOTE: flags.filter(f => f.level === 'NOTE').length,
    }
  }, null, 2));

  const blocks = flags.filter(f => f.level === 'BLOCK');
  if (blocks.length > 0) {
    console.error(`[guard-reviewer] ⚠️ ${blocks.length} BLOCK in ${filePath}`);
    blocks.forEach(b => console.error(`  Line ${b.line}: ${b.msg}`));
  }
  process.exit(0);
} catch (e) {
  console.error('[guard-reviewer] Error:', e.message);
  process.exit(0);
}
```

#### `guard-finish.mjs`（精简为仅L4）

```javascript
#!/usr/bin/env node
/**
 * Stop Hook — V3.0 精简版：仅保留L4 E2E证据检查
 * L0-L3已由状态机质量门覆盖
 */
import { execSync } from 'child_process';
import { existsSync, readdirSync, statSync } from 'fs';
import { join } from 'path';

const EVIDENCE_MAX_AGE_MIN = 30;
const EVIDENCE_MIN_SIZE_BYTES = 5000;

function getProjectRoot() {
  try {
    return execSync('git rev-parse --show-toplevel', {
      encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
    }).trim().replace(/\\/g, '/');
  } catch {
    return process.cwd().replace(/\\/g, '/');
  }
}

const ROOT = getProjectRoot();

// 80s超时
const KILL_TIMER = setTimeout(() => {
  console.log(JSON.stringify({
    decision: 'block',
    reason: 'Guard timeout (80s)',
  }));
  process.exit(1);
}, 80000);

function safeExit(code = 0) {
  clearTimeout(KILL_TIMER);
  process.exit(code);
}

// 读取stdin
let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch { input = {}; }

if (input.stop_reason === 'user_interrupt') {
  console.log(JSON.stringify({ decision: 'approve', reason: 'User interrupted' }));
  safeExit(0);
}

// 检测是否有前端实质性变更
let hasSubstantiveFrontend = false;
try {
  const allFiles = execSync('git diff --name-only HEAD', {
    encoding: 'utf-8', stdio: 'pipe', timeout: 5000,
  }).trim();
  
  const now = Date.now();
  hasSubstantiveFrontend = allFiles.split('\n').filter(Boolean).some(f => {
    if (!/^(jnpf-web-vue3|jnpf-web-datascreen|jnpf-app-vue3)\//.test(f)) return false;
    if (/\.(vue|tsx|less|scss|css)$/.test(f)) return true;
    if (/\.(ts|js|jsx)$/.test(f) && /\/(views?|pages?|components?|layouts?|hooks?)\//.test(f)) return true;
    return false;
  });
} catch {
  console.log(JSON.stringify({ decision: 'approve', reason: 'git unavailable' }));
  safeExit(0);
}

if (!hasSubstantiveFrontend) {
  console.log(JSON.stringify({ decision: 'approve', reason: 'No substantive frontend changes' }));
  safeExit(0);
}

// L4: E2E证据检查
console.error('▸ [L4] E2E evidence check...');
const evidenceDir = join(ROOT, '.claude', 'evidence');
if (!existsSync(evidenceDir)) {
  console.log(JSON.stringify({
    decision: 'block',
    reason: 'E2E evidence missing: .claude/evidence/ directory not found',
  }));
  safeExit(0);
}

const files = readdirSync(evidenceDir).filter(f => /\.(png|jpg|jpeg)$/i.test(f));
if (files.length === 0) {
  console.log(JSON.stringify({
    decision: 'block',
    reason: 'E2E evidence missing: no screenshots in .claude/evidence/',
  }));
  safeExit(0);
}

const now = Date.now();
const valid = [];
for (const f of files) {
  if (f === 'playwright-smoke.png') continue;
  const fp = join(evidenceDir, f);
  const st = statSync(fp);
  const ageMin = (now - st.mtimeMs) / 60000;
  if (st.size >= EVIDENCE_MIN_SIZE_BYTES && ageMin <= EVIDENCE_MAX_AGE_MIN) {
    valid.push(f);
  }
}

if (valid.length === 0) {
  console.log(JSON.stringify({
    decision: 'block',
    reason: `E2E evidence invalid: no fresh screenshots (≤${EVIDENCE_MAX_AGE_MIN}min, ≥${EVIDENCE_MIN_SIZE_BYTES/1024}KB)`,
  }));
  safeExit(0);
}

console.log(JSON.stringify({
  decision: 'approve',
  reason: `E2E evidence valid: ${valid.length} screenshots`,
}));
safeExit(0);
```

---

## 四、Rules整理方案：15个 → 7个核心 + 4个角色专属

### 4.1 删除/合并/归档清单

| 原文件                     | 处置                                                         | 理由                                             |
| :------------------------- | :----------------------------------------------------------- | :----------------------------------------------- |
| `workflow-pipeline.md`     | **合并入`workflow.md`**                                      | 重复定义Phase流水线，统一为单一信源              |
| `rules-loader.md`          | **删除，职能上移状态机**                                     | 状态机`_assemble_prompt`硬编码加载策略           |
| `review-workflow.md`       | **拆分**：审查维度→`reviewer-discipline.md`，子代理模板→状态机Schema | 原文件是Prompt模板，非结构化规则                 |
| `communication.md`         | **归档到`_archived/`**                                       | 软约束，靠AI自觉，长会话漂移率~50%，不纳入状态机 |
| `memory.md`                | **归档到`_archived/`**                                       | 跨会话记忆由状态机`evolution_manager`管理        |
| `codegraph-exploration.md` | **保留，但改为L3工具规范**                                   | 从"规则"降级为"工具使用手册"，按需加载           |

### 4.2 保留的7个核心规则文件

```
.claude/souls/_shared/                    # 共享规则（L0-L1始终加载）
├── assertion-discipline.md              # 论断纪律（反幻觉、标签、置信度）
├── engineering-laws.md                  # 工程铁律（Law 1-4，Gate Function单一信源）
└── workflow.md                          # 工作流（合并后的Phase流水线+任务分级）

.claude/souls/{role}/rules/              # 角色专属规则（L2按需加载）
├── architect/rules/
│   └── architecture-redlines.md         # 架构红线R1-R10（单一信源）
├── coder/rules/
│   ├── jnpf-expert-traps.md             # 专家陷阱（去重后，标注"详见红线Rx"）
│   ├── sql-safety.md                    # SQL注入防御
│   └── frontend-memory-leak.md          # 前端内存安全
├── reviewer/rules/
│   └── reviewer-discipline.md           # V3.0新增：5维度×3级别×工具链
└── _shared/                             # 跨角色共享但按需加载
    ├── low-code-principles.md           # 低代码准则
    ├── debugging.md                     # 调试纪律
    └── testing.md                       # 测试纪律（引用Gate Function链接）
```

### 4.3 规则Schema化：每条规则必须有结构化字段

**改造示例：`architecture-redlines.md` → 结构化JSON配置**

```json
// .claude/souls/architect/rules/architecture-redlines.json
// 状态机可直接消费的规则配置
{
  "$schema": "fugu/rules-v1",
  "rules": [
    {
      "id": "R1",
      "name": "API Generation",
      "level": "L2",
      "category": "architecture",
      "description": "Service实现IDynamicApiController自动映射API，NEVER手写Controller",
      "consequence": "手写Controller→重复路由注册/绕过RESTfulResult包装/API文档缺失",
      "hook_file": null,
      "hook_level": null,
      "test_cases": ["test-controller-creation"],
      "related_traps": ["Trap-1", "Trap-6", "Trap-9"],
      "auto_verify": false,
      "reviewer_check": "D1-架构合规"
    },
    {
      "id": "R4",
      "name": "Multi-Tenant Isolation",
      "level": "L0",
      "category": "security",
      "description": "新SqlSugar查询MUST确保租户过滤生效",
      "consequence": "漏过滤=跨租户数据泄漏（最严重安全风险）",
      "hook_file": "guard-tenant-filter.mjs",
      "hook_level": "BLOCK",
      "test_cases": ["test-tenant-filter-disable", "test-updateable-no-where", "test-raw-sql-no-where"],
      "related_traps": ["Trap-7", "Trap-8", "Trap-13"],
      "auto_verify": true,
      "reviewer_check": "D1-架构合规"
    },
    {
      "id": "R7",
      "name": "SQL Injection Defense",
      "level": "L0",
      "category": "security",
      "description": "动态SQL MUST参数化，NEVER字符串拼接用户输入",
      "consequence": "SQL注入→数据泄露/删除/权限提升",
      "hook_file": "guard-sql-injection.mjs",
      "hook_level": "BLOCK",
      "test_cases": ["test-sql-interpolation", "test-string-format-sql", "test-ado-injection"],
      "related_traps": [],
      "auto_verify": true,
      "reviewer_check": "D1-架构合规"
    }
  ]
}
```

**对应的Markdown文件改为人类可读+机器可解析的混合格式：**

```markdown
# JNPF Architecture Redlines (架构铁律)

> **定位：** 本文档是 JNPF v5.2 项目**唯一**的架构级铁律清单。
> **机器配置：** `.claude/souls/architect/rules/architecture-redlines.json`

---

## R1 — API Generation

- **ID:** R1
- **Level:** L2
- **Hook:** 无
- **Reviewer:** D1-架构合规
- **关联陷阱:** Trap-1, Trap-6, Trap-9

**规则：** Service 实现 `IDynamicApiController` 自动映射 API。NEVER 手写 Controller 类。

**理由：** JNPF 的路由、参数绑定、响应包装全部由框架根据接口自动生成。

**后果：** 手写 Controller → 重复路由注册 / 绕过 RESTfulResult 包装 / API 文档缺失。

---

## R4 — Multi-Tenant Isolation ⚠️ 最高安全风险

- **ID:** R4
- **Level:** L0
- **Hook:** `guard-tenant-filter.mjs` (BLOCK)
- **Reviewer:** D1-架构合规
- **关联陷阱:** Trap-7, Trap-8, Trap-13

**规则：** 新 SqlSugar 查询 MUST 确保租户过滤生效。漏过滤 = 跨租户数据泄漏。

**强制要求：**
1. `Queryable<T>()` 自动附加 `ITenantFilter`
2. `Ado.SqlQuery` / `SqlQueryable` / 原生 SQL → MUST 手动加 `WHERE TenantId = @tid`
3. `Updateable<T>` / `Deleteable<T>` → MUST 链式调用 `.Where(...)` 限定租户范围
4. NEVER 调用 `DisableGlobalFilter("TenantFilter")`（除非加 `// r4-safe` 豁免）

**Hook 覆盖：** `guard-tenant-filter.mjs` 拦截 DisableGlobalFilter / Updateable无Where / 原生SQL无WHERE
```

### 4.4 `reviewer-discipline.md`（V3.0新增核心规则）

```markdown
# Reviewer 纪律 — 质量审查专用规则包

> **定位：** Reviewer角色的唯一规则来源。其他规则文件的审查维度已聚合于此。
> **加载时机：** Phase REVIEW阶段，按需加载（L2层）。
> **设计原则：** Reviewer不需要知道"规则为什么存在"，只需要知道"如何检查"和"如何分级"。

---

## 审查维度速查表（5维度 × 3级别）

| 维度 | 检查项 | 自动验证 | 人工确认 | 置信度 |
|:---|:---|:---:|:---:|:---:|
| **D1 架构合规** | R1-R10红线 | Hook L0已拦截 | Reviewer复核 | HIGH |
| **D2 工程铁律** | TODO/吞异常/未验证假设 | grep扫描 | Reviewer判断 | MED |
| **D3 专家陷阱** | Trap 1-14 | 部分可工具验证 | Reviewer深度检查 | LOW-MED |
| **D4 代码质量** | 方法长度/重复/命名 | 工具可部分覆盖 | Reviewer判断 | MED |
| **D5 测试覆盖** | 新增代码是否有测试 | 文件存在性检查 | Reviewer判断 | HIGH |

> **关键优化：** D1（架构合规）的R1-R10已由Hook L0在写入时硬阻断，Reviewer**不需要重复检查**，只需确认"Hook是否漏检"（极低概率）。Reviewer的精力应集中在D2-D5。

---

## 审查输出格式（三级质量门）

### 🔴 BLOCK（必须修复，阻塞流程）

触发条件：
- 发现Hook L0漏检的架构红线违规
- 发现可导致生产事故的代码（如：SQL注入绕过参数化查询）
- 发现严重性能问题（如：无分页的全表查询）

输出格式：
```
[BLOCK] {规则ID} | 置信度: {HIGH/MED/LOW} | 文件:行号
  问题: {一句话描述}
  证据: {代码片段}
  修复: {具体代码}
  为什么Hook没拦住: {分析原因，用于优化Hook}
```

### 🟡 WARN（建议修复，不阻塞但需记录）

触发条件：
- 代码异味（方法>50行、重复代码、魔法值）
- 边界条件未处理（null检查、并发安全）
- 测试覆盖不足（新增逻辑无对应测试）

输出格式：
```
[WARN] {规则ID} | 置信度: {HIGH/MED/LOW} | 文件:行号
  问题: {描述}
  风险: {不修复的后果}
  建议: {具体改进方案}
  是否记录到tech-debt: {是/否}
```

### 🟢 NOTE（信息提示，仅记录）

触发条件：
- 代码风格偏好（与项目惯例不一致但不影响功能）
- 可优化的实现方式（有更简洁的写法）
- 文档缺失（公共方法无XML注释）

---

## 自动验证工具链（Reviewer专用）

Reviewer在审查时，MUST调用以下工具验证，不能仅靠肉眼：

| 工具 | 用途 | 命令 |
|:---|:---|:---|
| `grep-audit` | 扫描TODO/吞异常/硬编码 | `grep -n "TODO\\|FIXME\\|catch.*{}\\|throw new Exception"` |
| `mapster-check` | 验证Adapt是否覆盖审计字段 | 自定义脚本：检查`Adapt`调用前后是否有`.Ignore` |
| `pagination-check` | 验证列表查询是否有分页 | `grep -n "ToListAsync\\|ToPageListAsync"` |
| `async-suffix-check` | 验证IDynamicApiController方法无Async后缀 | `grep -n "Async\\s*("` |
| `tenant-filter-verify` | 验证原生SQL是否含TenantId | `grep -n "SqlQuery\\|SqlQueryable"`后人工确认 |

---

## 反馈闭环协议（Reviewer → 规则进化）

发现新问题，MUST按以下路径反馈：

```
发现新问题（不在现有规则中）
  │
  ├─ 是架构红线遗漏？ → 更新architecture-redlines.md（R11+）
  │                     更新Hook覆盖矩阵
  │                     更新test-hooks.mjs用例
  │
  ├─ 是专家陷阱遗漏？ → 更新jnpf-expert-traps.md（Trap 15+）
  │                     在reviewer-discipline.md中标记检查方法
  │
  ├─ 是Hook误报/漏报？ → 更新对应guard-*.mjs
  │                      更新Hook的测试用例
  │
  └─ 是代码模式问题？ → 更新reviewer-discipline.md的自动验证工具链
                        更新coder-reminders.md
```

---

## 关联文件

- 架构红线（R1-R10）→ `architecture-redlines.md`（Reviewer不复检，只确认Hook覆盖）
- 工程铁律 → `engineering-laws.md`（Reviewer加载Law 2验证方法论）
- 专家陷阱 → `jnpf-expert-traps.md`（Reviewer加载Trap检查清单）
```

---

## 五、状态机统一状态管理

### 5.1 删除的碎片化状态文件

| 原文件                            | 替代                                        |
| :-------------------------------- | :------------------------------------------ |
| `.claude/workflow-state.json`     | `workspace/{task_id}/state.json`            |
| `.claude/.build-verify-flag.json` | 状态机内存状态`state["build_verified"]`     |
| `.claude/.session-init-lock.json` | `session-scheduler.mjs`内部使用，不暴露给AI |
| `.claude/.skill-load-state.json`  | `guard-skill-load.mjs`内部使用              |

### 5.2 统一状态Schema

```json
// workspace/{task_id}/state.json
{
  "$schema": "fugu/state-v1",
  "task_id": "TASK-20260625-001",
  "task_level": "A",
  "current_phase": "review",
  "current_subtask_id": "ST-002",
  "retry_count": 0,
  "error_context": null,
  
  "phases_completed": ["align", "brainstorm", "explore", "decompose", "plan", "build", "verify"],
  
  "quality_gates": {
    "Q1": { "passed": true, "timestamp": "2026-06-25T10:00:00Z" },
    "Q2": { "passed": true, "timestamp": "2026-06-25T10:05:00Z" },
    "Q3": { "passed": true, "timestamp": "2026-06-25T10:15:00Z" },
    "Q4": { "passed": true, "timestamp": "2026-06-25T10:20:00Z" },
    "Q5": { "passed": false, "timestamp": "2026-06-25T10:30:00Z", "findings": ["REV-001", "REV-002"] }
  },
  
  "security_scans": {
    "build": { "passed": true, "findings": [] },
    "review_fix": { "passed": true, "findings": [] }
  },
  
  "reviewer_metrics": {
    "block_count": 1,
    "warn_count": 1,
    "new_patterns_found": 1,
    "hook_improvements_suggested": 1
  },
  
  "evolution": {
    "anomalies_recorded": 2,
    "coder_reminders_updated": 1,
    "rule_change_draft": "workspace/TASK-20260625-001/rule-change-draft.md",
    "human_review_required": true
  }
}
```

---

## 六、最终文件结构（V3.0整理后）

```
.claude/
├── CLAUDE.md                              # 人类工程师入口（精简至100行，引用状态机）
│
├── orchestrator/                          # 外部状态机（新增核心）
│   ├── state_machine.py                   # 阶段流转、回退、熔断、REVIEW硬阶段
│   ├── task_router.py                     # 复杂度分级、流水线选择
│   ├── quality_gate_engine.py             # Q1-Q6质量门硬执行（含Q5置信度加权）
│   ├── evolution_manager.py               # 进化引擎（处理Reviewer报告）
│   ├── concurrent_scheduler.py            # DAG并行调度
│   ├── merge_agent.py                     # 分支冲突处理
│   └── schemas/                           # JSON Schema定义
│       ├── architecture-v1.json
│       ├── plan-v1.json
│       ├── code-v1.json
│       ├── review-input-v1.json
│       ├── review-report-v1.json
│       ├── rules-v1.json                  # 规则Schema（新增）
│       └── state-v1.json                  # 统一状态Schema（新增）
│
├── souls/                                 # 七角色灵魂（精简、无状态化）
│   ├── orchestrator/
│   │   └── soul.md                        # "我只调度，不决策"
│   ├── architect/
│   │   ├── soul.md
│   │   └── rules/
│   │       ├── architecture-redlines.md   # 人类可读版本
│   │       └── architecture-redlines.json # 机器可消费版本（新增）
│   ├── planner/
│   │   ├── soul.md
│   │   └── rules/
│   ├── coder/
│   │   ├── soul.md
│   │   └── rules/
│   │       ├── jnpf-expert-traps.md       # 去重后，标注"详见红线Rx"
│   │       ├── sql-safety.md
│   │       └── frontend-memory-leak.md
│   ├── tester/
│   │   ├── soul.md
│   │   └── rules/
│   ├── reviewer/                          # V3.0新增角色
│   │   ├── soul.md                        # 质量进化引擎
│   │   └── rules/
│   │       └── reviewer-discipline.md     # 5维度×3级别×工具链
│   ├── reporter/
│   │   ├── soul.md
│   │   └── rules/
│   └── _shared/                           # 共享规则（L0-L1始终加载）
│       ├── assertion-discipline.md
│       ├── engineering-laws.md            # Gate Function单一信源
│       └── workflow.md                    # 合并后的Phase流水线
│
├── hooks/                                 # 精简至6个核心Hook + 1库
│   ├── hook-lib.mjs                       # 共享库（精简）
│   ├── session-scheduler.mjs             # SessionStart：防重入+轻量初始化
│   ├── guard-skill-load.mjs             # PreToolUse(Skill)：限速
│   ├── guard-bash.mjs                   # PreToolUse(Bash)：危险命令拦截
│   ├── guard-write.mjs                  # PreToolUse(Write/Edit)：L1安全扫描
│   ├── guard-reviewer.mjs               # PostToolUse：预筛选标志（V3.0新增）
│   └── guard-finish.mjs                 # StopHook：精简为L4-E2E证据检查
│
├── evolution/                             # 进化引擎（离线闭环）
│   ├── README.md
│   ├── mistake-genes.md                 # 硬上限50条
│   ├── coder-reminders.md               # 硬上限30条
│   ├── reviewer-metrics.md              # 硬上限20条
│   ├── anomalies/                       # 运行时异常记录（任务级）
│   │   └── TASK-xxx.json
│   ├── drafts/                          # 规则变更草案（待人工审核）
│   │   └── rule-change-TASK-xxx.md
│   └── _archived/                       # 自动归档区
│
├── review/                              # Reviewer运行时数据
│   └── flags/                           # guard-reviewer生成的标志文件
│       └── {file_path_escaped}.json
│
├── _archived/                           # V1.0历史归档（新增）
│   ├── rules/                           # 删除/合并的旧规则
│   │   ├── workflow-pipeline.md
│   │   ├── rules-loader.md
│   │   ├── review-workflow.md
│   │   ├── communication.md
│   │   └── memory.md
│   └── hooks/                           # 删除的旧Hook
│       ├── guard-workflow.mjs
│       ├── post-build-verify.mjs
│       ├── verify-mistake-log.mjs
│       ├── format-and-lint.mjs
│       ├── smart-post-hook.mjs
│       ├── skill-reminder.mjs
│       ├── superpowers-check.mjs
│       ├── load-mistakes.mjs
│       └── codegraph-auto-sync.sh
│
└── brain/                               # 协调者灵魂（保留备用）
    └── orchestrator.md

workspace/                                 # 文件级交接契约（运行时生成）
└── TASK-20260625-001/
    ├── state.json                         # 统一状态（替代workflow-state.json）
    ├── requirements.md
    ├── architecture.json
    ├── plan.json
    ├── code_diff_ST-001.json              # 按子任务隔离
    ├── test_report.json
    ├── review_input.json                  # 状态机组装
    ├── review_report.json                 # Reviewer产出
    ├── delivery_report.md
    ├── security_scan_build.json
    └── rule-change-draft.md               # 规则变更草案
```

---

## 七、关键收益量化

| 维度              | V1.0现状         | V3.0整理后                | 收益                           |
| :---------------- | :--------------- | :------------------------ | :----------------------------- |
| **Hook数量**      | 21个             | 6个核心 + 1库             | **-70%**，消除职责重叠         |
| **Rules数量**     | 15个             | 7个核心 + 4个角色专属     | **-53%**，消除重复内容         |
| **状态文件**      | 4个碎片化        | 1个统一Schema             | **-75%**，状态一致性           |
| **规则加载Token** | ~22,000（失控）  | <6,000（硬上限）          | **-73%**，释放上下文给代码工作 |
| **Reviewer覆盖**  | 无（纯自然语言） | 5维度×3级别×置信度加权    | **从0到1**，双防线质量飞轮     |
| **Hook误报处理**  | 无反馈闭环       | Reviewer审计Hook+进化引擎 | **持续优化**，越审查越聪明     |