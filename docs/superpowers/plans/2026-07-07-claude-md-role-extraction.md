# CLAUDE.md 角色规则下沉 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 CLAUDE.md 中角色专属规则下沉到 `souls/{role}/soul.md` + 新建 `{role}-mode` skill 活性注入，CLAUDE.md 从 563 行精简到 ~375 行，核心架构原则（Evidence/三元组/S1-S6/Supreme）保留。

**Architecture:** 三层文档 — rules/（自动加载信源，不动）+ souls/（角色定义信源，追加角色专属内容）+ skills/{role}-mode（活性注入入口，调用时加载 soul）。CLAUDE.md 保留全局元规则 + 新增「角色体系入口」路由节。souls 修改是纯追加，CLAUDE.md 精简最后做（有备份）。

**Tech Stack:** Markdown / YAML frontmatter（skills）/ Claude Code skill 加载机制

**Spec:** `docs/superpowers/specs/2026-07-07-claude-md-role-extraction-design.md`
**Backup:** `CLAUDE.md.bak.20260707`（已存在，与改前 CLAUDE.md 一致）

---

## Phase A — souls/ 追加内容（低风险，纯追加）

> 每个 task：读源（CLAUDE.md 章节）→ 读目标 soul 结构 → 在指定 section 后追加 → grep 验证 → 精确 commit。
> souls 现有结构（8 文件统一）：§1 身份定义 / §2 核心约束 / §3 输入格式 / §4 输出格式 / §5 禁止事项 / §6 失败回退。追加内容放 §6 之后作 §7+。

### Task A1: debugger soul 追加 Data-Driven Debug 工具链表 + Debug Path

**Files:**
- Modify: `.claude/souls/debugger/soul.md`（当前 125 行，在 §9 返回协议后追加 §10）
- Source: `CLAUDE.md` §「Core Principle: Evidence Over Assumption」的「Data-Driven Debug 工具链（四件套 + Phase B 增强）」表 + §「Debug Path（第 8 角色 Debugger）」

- [ ] **Step 1: 读 CLAUDE.md 源内容定位**

Run: `grep -n "Data-Driven Debug 工具链" CLAUDE.md` 和 `grep -n "Debug Path（第 8 角色" CLAUDE.md`
记录两个章节的起止行号。

- [ ] **Step 2: 读 debugger soul 当前末尾结构**

Run: `tail -20 .claude/souls/debugger/soul.md`
确认 §9 返回协议是最后一节。

- [ ] **Step 3: 追加 §10 Data-Driven Debug 工具链 + §11 Debug Path**

在 `souls/debugger/soul.md` 末尾（§9 之后）追加。内容从 CLAUDE.md 对应章节搬运，做以下适配：
- 标题改为 `## 10. Data-Driven Debug 工具链（四件套 + Phase B 增强）` 和 `## 11. Debug Path（中断驱动）`
- 把「> **完整技能：** `data-driven-debug`」指针保留（与现有 §4 输入格式一致）
- 完整复制「症状→工具→命令」表、「数据采集优先级」行、「错误发生后 MUST 先查错题本」代码块
- §11 复制「自动切入/手动切入/产出/返回」Debug Path 描述

- [ ] **Step 4: grep 验证关键内容已迁移**

Run: `grep -c "full-fidelity-debug\|visual-debug\|mistake-rag\|agent-probe" .claude/souls/debugger/soul.md`
Expected: ≥ 4（四件套工具名都在）

Run: `grep -c "Debug Path" .claude/souls/debugger/soul.md`
Expected: ≥ 1

- [ ] **Step 5: 精确 commit**

```bash
git add .claude/souls/debugger/soul.md
git commit -m "docs(souls): debugger soul 追加 Data-Driven Debug 工具链 + Debug Path (Task A1)"
```

---

### Task A2: tester soul 追加自动测试闭环 + Phase 5 明细

**Files:**
- Modify: `.claude/souls/tester/soul.md`（当前 123 行）
- Source: `CLAUDE.md` §「🔄 自动测试 · 自动修复闭环（Dev-Deploy-Debug Loop）」全文 + Workflow Pipeline §「Phase 5: Verify」

- [ ] **Step 1: 读源定位**

Run: `grep -n "自动测试 · 自动修复闭环\|Phase 5: Verify" CLAUDE.md`

- [ ] **Step 2: 读 tester soul 末尾结构**

Run: `tail -15 .claude/souls/tester/soul.md`

- [ ] **Step 3: 追加 §N 自动测试闭环（tester 为主承载）**

在 tester soul 末尾追加。从 CLAUDE.md「自动测试闭环」章节搬运：
- 「标准闭环（每次改代码后）」6 步流程
- 「工具链」表（`tests/api/studio-s2.test.mjs` 等）
- 「登录协议（与 PC 前端一致）」段（明文密码 → MD5 → AES → POST /api/oauth/Login）
- 「环境变量」行（`JNPF_API_URL` 等）
- 「禁止」清单（❌ 8 条）
- Workflow Pipeline Phase 5 的 SP/Rule/Skill 明细（`verification-before-completion` / `testing.md` / `jnpf-api-cli` / `playwright`）

适配：标题用 `## N. 自动测试闭环（Dev-Deploy-Debug Loop）` 和 `## N+1. Phase 5 Verify 明细`。

- [ ] **Step 4: grep 验证**

Run: `grep -c "api/oauth/Login\|jnpf-api.mjs\|pnpm test:api\|MD5" .claude/souls/tester/soul.md`
Expected: ≥ 3

- [ ] **Step 5: 精确 commit**

```bash
git add .claude/souls/tester/soul.md
git commit -m "docs(souls): tester soul 追加自动测试闭环 + Phase 5 明细 (Task A2)"
```

---

### Task A3: coder soul 追加 Phase 4 明细 + 闭环编码侧引用

**Files:**
- Modify: `.claude/souls/coder/soul.md`（当前 112 行）
- Source: `CLAUDE.md` Workflow Pipeline §「Phase 4: Build」+ §「Review Gate」计数器规则

- [ ] **Step 1: 读源定位**

Run: `grep -n "Phase 4: Build\|Review Gate（不可绕过）" CLAUDE.md`

- [ ] **Step 2: 追加 §N Phase 4 Build 明细**

在 coder soul 末尾追加。从 CLAUDE.md「Phase 4: Build」搬运：
- SP 技能：`executing-plans` / `subagent-driven-development`(S级) / `dispatching-parallel-agents` / `using-git-worktrees`
- Hooks：7 guard hooks（L0 自动阻断）+ `format-and-lint.mjs`
- Rule：`sql-safety.md`(if .cs) + `frontend-memory-leak.md`(if SSE/timer)
- todo 强制注入：`🔍 代码审查(子代理)` + `📝 错题本追加`
- Review Gate 计数器规则：Write/Edit 后 +1，≥2 触发 code-reviewer，Step 7 重置；不计入计数器的例外（.md/.json/配置/单行）

- [ ] **Step 3: 追加 §N+1 自动测试闭环（编码侧引用）**

简短引用（不重复 tester 全文）：`编码完成后按 souls/tester/soul.md 的自动测试闭环验证（dotnet build → jnpf-api.mjs → pnpm test:api）。`

- [ ] **Step 4: grep 验证**

Run: `grep -c "executing-plans\|sql-safety\|frontend-memory-leak\|代码审查" .claude/souls/coder/soul.md`
Expected: ≥ 3

- [ ] **Step 5: 精确 commit**

```bash
git add .claude/souls/coder/soul.md
git commit -m "docs(souls): coder soul 追加 Phase 4 明细 + Review Gate + 闭环引用 (Task A3)"
```

---

### Task A4: architect soul 追加 Phase 1-2 明细

**Files:**
- Modify: `.claude/souls/architect/soul.md`（当前 115 行）
- Source: `CLAUDE.md` Workflow Pipeline §「Phase 1: Align」+ §「Phase 2: Brainstorm」+ Entry Gate

- [ ] **Step 1: 读源定位**

Run: `grep -n "Entry Gate\|Phase 1: Align\|Phase 2: Brainstorm" CLAUDE.md`

- [ ] **Step 2: 追加 §N Phase 1-2 明细**

在 architect soul 末尾追加：
- Entry Gate：Hook `superpowers-check.mjs` → SP 激活验证；共享约束自动加载（assertion-discipline + mistake-avoidance）；SP `using-superpowers`(自动)；Rule `memory.md`
- Phase 1 Align：重述任务、S/A/B 分级、确认范围；Rule `architecture-redlines.md`；Skill `spec`（可选）
- Phase 2 Brainstorm：SP `brainstorming`（S1 铁律，ALL 级别强制不可跳过）；Rule `jnpf-expert-traps.md`；Grep `mistake-log.md`
- 抬头声明模板（Phase 1 🔵 / Phase 2 🟡）

- [ ] **Step 3: grep 验证**

Run: `grep -c "brainstorming\|architecture-redlines\|jnpf-expert-traps\|S1 铁律" .claude/souls/architect/soul.md`
Expected: ≥ 3

- [ ] **Step 4: 精确 commit**

```bash
git add .claude/souls/architect/soul.md
git commit -m "docs(souls): architect soul 追加 Phase 1-2 明细 (Task A4)"
```

---

### Task A5: planner soul 追加 Phase 3 明细 + 需求提取清单

**Files:**
- Modify: `.claude/souls/planner/soul.md`（当前 123 行）
- Source: `CLAUDE.md` Workflow Pipeline §「Phase 3: Plan」+ workflow.md §「Phase 2 Brainstorm 后 → Phase 3 Plan 时的需求提取清单」

- [ ] **Step 1: 读源定位**

Run: `grep -n "Phase 3: Plan\|需求提取清单" .claude/rules/workflow.md CLAUDE.md`

- [ ] **Step 2: 追加 §N Phase 3 Plan 明细**

- SP `writing-plans`
- Rule `workflow.md`（需求提取清单）+ `jnpf-frontend-rules.md`（按需）
- B 级跳过此 Phase
- 需求提取清单模板（📋 表格：#/需求原文/实现映射/歧义风险）+ 「清单为空不得编码」「歧义必先提问」
- Phase 6 Review 对照标注（✅已实现/⚠️偏离/❌未实现）
- 抬头模板（Phase 3 🟠）

- [ ] **Step 3: grep 验证**

Run: `grep -c "writing-plans\|需求提取清单\|✅已实现" .claude/souls/planner/soul.md`
Expected: ≥ 2

- [ ] **Step 4: 精确 commit**

```bash
git add .claude/souls/planner/soul.md
git commit -m "docs(souls): planner soul 追加 Phase 3 明细 + 需求提取清单 (Task A5)"
```

---

### Task A6: reviewer soul 追加 Phase 6 明细 + Review Gate + 全局 agent 继承指引

**Files:**
- Modify: `.claude/souls/reviewer/soul.md`（当前 141 行）
- Source: `CLAUDE.md` §「Review Gate（不可绕过）」+ Workflow Pipeline §「Phase 6: Review」

- [ ] **Step 1: 读源定位**

Run: `grep -n "Phase 6: Review\|Review Gate（不可绕过）" CLAUDE.md`

- [ ] **Step 2: 在 §1 身份定义后插入「全局 code-reviewer 继承指引」**

在 reviewer soul §1 末尾追加一段：
```
## 全局 code-reviewer agent 继承指引

主 Claude dispatch 全局 `code-reviewer` subagent 时，prompt MUST 含：
「先 Read `.claude/souls/reviewer/soul.md` 再按其 §4 输出格式与 §2 审查维度审查」
确保 code-reviewer 加载本 soul 的 fugu/review-report-v1 契约 + 5 维度×3 级别标准。
```

- [ ] **Step 3: 追加 §N Phase 6 Review 明细**

- SP `requesting-code-review` → `receiving-code-review`（max 3 cycles）
- Rule `review-workflow.md`（子代理编排）+ `architecture-redlines.md`（R1-R10 合规）+ `reviewer-discipline.md`
- Skill `security-review`（可选）
- 错题本检查：`📝错题本追加` todo 必须 completed
- 抬头模板（Phase 6 🟣）

- [ ] **Step 4: grep 验证**

Run: `grep -c "requesting-code-review\|review-workflow\|code-reviewer.*Read\|错题本追加" .claude/souls/reviewer/soul.md`
Expected: ≥ 3

- [ ] **Step 5: 精确 commit**

```bash
git add .claude/souls/reviewer/soul.md
git commit -m "docs(souls): reviewer soul 追加 Phase 6 + 全局 agent 继承指引 (Task A6)"
```

---

### Task A7: reporter soul 追加 Phase 7 明细 + session-key-points 强制项

**Files:**
- Modify: `.claude/souls/reporter/soul.md`（当前 122 行）
- Source: `CLAUDE.md` Workflow Pipeline §「Phase 7: Complete」

- [ ] **Step 1: 读源定位**

Run: `grep -n "Phase 7: Complete\|session-key-points" CLAUDE.md`

- [ ] **Step 2: 追加 §N Phase 7 Complete 明细**

- SP `finishing-a-development-branch`
- Skill `pre-commit`（提交前检查）
- Hook `guard-finish.mjs`（冒烟 + E2E 证据 + 错题本验证）+ `collect-summary.mjs`（会话摘要）
- Rule `workflow.md`（报告模板）
- 🟠 强制写入 `session-key-points.md`：关键技术决策+理由 / Bug 根因 / 踩坑+避免策略；未写入 → collect-summary 无法收录
- 抬头模板（Phase 7 ⚫）

- [ ] **Step 3: grep 验证**

Run: `grep -c "finishing-a-development-branch\|guard-finish\|session-key-points\|collect-summary" .claude/souls/reporter/soul.md`
Expected: ≥ 3

- [ ] **Step 4: 精确 commit**

```bash
git add .claude/souls/reporter/soul.md
git commit -m "docs(souls): reporter soul 追加 Phase 7 + session-key-points 强制项 (Task A7)"
```

---

### Task A8: orchestrator soul 追加角色切换状态机 + Review Gate dispatch 路由

**Files:**
- Modify: `.claude/souls/orchestrator/soul.md`（当前 76 行）
- Source: `CLAUDE.md` §「角色切换（产出物驱动 — 零配置自动流转）」全文 + §「Review Gate」dispatch 路由

- [ ] **Step 1: 读源定位**

Run: `grep -n "角色切换（产出物驱动\|Review Gate（不可绕过）\|子 agent dispatch 指向" CLAUDE.md`

- [ ] **Step 2: 追加 §N 角色切换状态机**

从 CLAUDE.md「角色切换」章节搬运：
- workspace/ 8 文件结构图（requirements/architecture/plan/code_changes/test_report/review_report/delivery_report/debug_report）
- 角色判定表（requirements.md 不存在→Orchestrator / 缺 architecture→Architect / ... / 全部就位→Reporter / 编译失败等→Debugger）
- 入口（在 requirements.md 描述任务 → 状态机启动）
- 隔离（同一时间只一个任务；开新任务前归档或丢弃旧任务）
- 收尾（Reporter 产出 delivery_report.md → 移入 `workspace/_completed/{任务名}-{YYYYMMDD-HHmm}/`）
- 自动流转（默认全自动，产出物落盘后立即检查缺哪个文件→自动切下一角色）
- 人工介入表（发送任意消息/「切换到 {角色}」/「重做 {阶段}」）

- [ ] **Step 3: 追加 §N+1 Review Gate dispatch 路由**

- 审查计数器规则（≥2 触发 code-reviewer，Step 7 重置）
- 不计入计数器例外
- 子 agent dispatch 指向：Phase 5 → `jnpf-tester`；Debug Path / ≥3 次失败 / >10min → `jnpf-debugger`
- todo_write 强制注入：`🔍 代码审查(子代理)` + `📝 错题本追加`，pending 阻塞

- [ ] **Step 4: grep 验证**

Run: `grep -c "workspace/_completed\|jnpf-tester\|jnpf-debugger\|角色判定\|审查计数器" .claude/souls/orchestrator/soul.md`
Expected: ≥ 4

- [ ] **Step 5: 精确 commit**

```bash
git add .claude/souls/orchestrator/soul.md
git commit -m "docs(souls): orchestrator soul 追加角色切换状态机 + Review Gate dispatch (Task A8)"
```

---

## Phase B — 新建 skills/{role}-mode/（活性注入入口）

> 每个 skill 是轻量指针文件（不复制 soul 全文，只指向）。frontmatter 的 description 决定触发可见性。

### Task B1: 新建 coder-mode skill

**Files:**
- Create: `.claude/skills/coder-mode/SKILL.md`

- [ ] **Step 1: 创建目录**

```bash
mkdir -p .claude/skills/coder-mode
```

- [ ] **Step 2: 写 SKILL.md（完整内容）**

```markdown
---
name: coder-mode
description: 进入 Coder 角色（写/改后端 .cs 或前端 .vue/.ts 代码时）。活性加载 souls/coder/soul.md 角色定义，按 Phase 4 Build 约束、sql-safety/frontend-memory-leak 红线、Review Gate 计数器行动。
---

# Coder Mode — 活性加载 souls/coder/soul.md

调用此 skill 即进入 **Coder** 角色。立即 Read 以下文件并按其约束行动：

1. `D:\JNPF-v52\.claude\souls\coder\soul.md` — 角色定义（身份/约束/Phase 4 明细/输入输出/禁止/回退）
2. 编码前按 soul §「输入格式」列出的 Rule 文件（写 .cs → `sql-safety.md`；写 SSE/timer → `frontend-memory-leak.md`）

## 触发场景

- 写或改后端 C# 代码（.cs）
- 写或改前端代码（.vue / .ts）
- 任何 Phase 4 Build 实施动作

## 退出条件

代码变更完成 → 按 soul 的「自动测试闭环引用」走 Dev Loop（dotnet build → jnpf-api.mjs → pnpm test:api）→ 交还 Orchestrator 进入 Phase 5 Verify。

## 硬约束（来自 soul）

- Review Gate 计数器：Write/Edit 后 +1，≥2 触发 code-reviewer 子代理
- todo 强制含 `🔍 代码审查(子代理)` + `📝 错题本追加`，code-reviewer PASS 前保持 pending
- 禁止吞异常 / TODO / 无根因改动
```

- [ ] **Step 3: 验证 frontmatter 合法**

Run: `head -4 .claude/skills/coder-mode/SKILL.md`
Expected: 前 4 行是 `---` / `name: coder-mode` / `description: ...` / `---`

- [ ] **Step 4: 精确 commit**

```bash
git add .claude/skills/coder-mode/SKILL.md
git commit -m "docs(skills): 新建 coder-mode 活性注入入口 (Task B1)"
```

---

### Task B2: 新建 architect-mode skill

**Files:**
- Create: `.claude/skills/architect-mode/SKILL.md`

- [ ] **Step 1: 创建目录 + 写文件**

```bash
mkdir -p .claude/skills/architect-mode
```

写 `.claude/skills/architect-mode/SKILL.md`：

```markdown
---
name: architect-mode
description: 进入 Architect 角色（收到新需求、设计架构、产出 architecture.md、做架构决策时）。活性加载 souls/architect/soul.md，按 Phase 1 Align + Phase 2 Brainstorm 约束行动，含 S/A/B 分级与红线预加载。
---

# Architect Mode — 活性加载 souls/architect/soul.md

调用此 skill 即进入 **Architect** 角色。立即 Read：

1. `D:\JNPF-v52\.claude\souls\architect\soul.md` — 角色定义（Phase 1-2 明细/输入输出/禁止/回退）
2. `.claude/rules/architecture-redlines.md` — R1-R12 红线预加载
3. `.claude/rules/jnpf-expert-traps.md` — 陷阱预检（Phase 2）

## 触发场景

- 收到新开发需求
- 产出 `workspace/architecture.md`
- 架构决策（"为什么这样设计"）

## 硬约束

- S1 铁律：编码/设计前 MUST brainstorm（不可跳过）
- 需求提取清单为空不得推进 Planner
- [FRAME] 方案不得当 [KNOWN] 承诺；虚构 JNPF 能力 = 违规
```

- [ ] **Step 2: 验证 + commit**

Run: `head -4 .claude/skills/architect-mode/SKILL.md` → 确认 frontmatter

```bash
git add .claude/skills/architect-mode/SKILL.md
git commit -m "docs(skills): 新建 architect-mode 活性注入入口 (Task B2)"
```

---

### Task B3: 新建 planner-mode skill

**Files:**
- Create: `.claude/skills/planner-mode/SKILL.md`

- [ ] **Step 1: 创建目录 + 写文件**

```bash
mkdir -p .claude/skills/planner-mode
```

写 `.claude/skills/planner-mode/SKILL.md`：

```markdown
---
name: planner-mode
description: 进入 Planner 角色（产出 plan.md、任务分级、需求提取清单时）。活性加载 souls/planner/soul.md，按 Phase 3 Plan 约束（S/A 级；B 级跳过）行动。
---

# Planner Mode — 活性加载 souls/planner/soul.md

调用此 skill 即进入 **Planner** 角色。立即 Read：

1. `D:\JNPF-v52\.claude\souls\planner\soul.md` — 角色定义（Phase 3 明细/需求提取清单/输入输出/禁止/回退）
2. `.claude/rules/workflow.md` — 任务分级 + 七阶段流水线映射

## 触发场景

- 产出 `workspace/plan.md`
- 任务 S/A/B 分级判定
- 编码前需求提取清单

## 硬约束

- A 级及以上 MUST 输出需求提取清单（📋 表）
- 清单为空不得推进 Coder
- 歧义项必先提问澄清，获准后才编码
- B 级可跳过 Phase 3，但不可跳过 Phase 2 Brainstorm
```

- [ ] **Step 2: 验证 + commit**

Run: `head -4 .claude/skills/planner-mode/SKILL.md`

```bash
git add .claude/skills/planner-mode/SKILL.md
git commit -m "docs(skills): 新建 planner-mode 活性注入入口 (Task B3)"
```

---

### Task B4: 新建 reporter-mode skill

**Files:**
- Create: `.claude/skills/reporter-mode/SKILL.md`

- [ ] **Step 1: 创建目录 + 写文件**

```bash
mkdir -p .claude/skills/reporter-mode
```

写 `.claude/skills/reporter-mode/SKILL.md`：

```markdown
---
name: reporter-mode
description: 进入 Reporter 角色（产出 delivery_report.md、归档、提交前、会话收尾时）。活性加载 souls/reporter/soul.md，按 Phase 7 Complete 约束行动，含 session-key-points 强制写入与 guard-finish 门控。
---

# Reporter Mode — 活性加载 souls/reporter/soul.md

调用此 skill 即进入 **Reporter** 角色。立即 Read：

1. `D:\JNPF-v52\.claude\souls\reporter\soul.md` — 角色定义（Phase 7 明细/报告模板/输入输出/禁止/回退）

## 触发场景

- 产出 `workspace/delivery_report.md`
- 会话收尾 / 提交前
- 归档到 `workspace/_completed/`

## 硬约束

- 🟠 MUST 写入 `.claude/memory/session-key-points.md`（技术决策+理由 / Bug 根因 / 踩坑+避免策略）
- Hook `guard-finish.mjs` 会检查 E1/E2/E3 证据 + 错题本
- `📝 错题本追加` todo 必须 completed（否则流程阻塞）
- 禁止美化未完成项为"已完成"；禁止虚构性能数据
```

- [ ] **Step 2: 验证 + commit**

Run: `head -4 .claude/skills/reporter-mode/SKILL.md`

```bash
git add .claude/skills/reporter-mode/SKILL.md
git commit -m "docs(skills): 新建 reporter-mode 活性注入入口 (Task B4)"
```

---

### Task B5: 新建 reviewer-mode skill（统一模式，可选但推荐）

**Files:**
- Create: `.claude/skills/reviewer-mode/SKILL.md`

- [ ] **Step 1: 创建目录 + 写文件**

```bash
mkdir -p .claude/skills/reviewer-mode
```

写 `.claude/skills/reviewer-mode/SKILL.md`：

```markdown
---
name: reviewer-mode
description: 进入 Reviewer 角色（主 Claude 自审代码、3+ 文件变更、提 PR 前、/full-review 时）。活性加载 souls/reviewer/soul.md，按 5 维度×3 级别审查。注：隔离子代理审查仍走 dispatch code-reviewer（prompt 含 Read soul）。
---

# Reviewer Mode — 活性加载 souls/reviewer/soul.md

调用此 skill 即进入 **Reviewer** 角色（主 Claude 自审场景）。立即 Read：

1. `D:\JNPF-v52\.claude\souls\reviewer\soul.md` — 角色定义（5 维度×3 级别/fugu 契约/输入输出/禁止/回退）

## 触发场景

- 主 Claude 自审当前变更（非 dispatch 子代理）
- 3+ 文件修改 / 50+ 行逻辑代码 / 提 PR 前 / `/full-review`

## 两条审查路径

- **主 Claude 自审**（本 skill）：加载本 soul，按 D1-D5 维度审查
- **隔离子代理审查**：dispatch 全局 `code-reviewer` agent，prompt MUST 含「先 Read `.claude/souls/reviewer/soul.md` 再审查」

## 硬约束

- 反谄媚：不放过 Critical；"整体很好只有小问题" = 违规
- D1 架构合规由 Hook L0 已拦截，只确认漏检
- 每个 finding 必须含置信度 + fix_code/fix_hint
```

- [ ] **Step 2: 验证 + commit**

Run: `head -4 .claude/skills/reviewer-mode/SKILL.md`

```bash
git add .claude/skills/reviewer-mode/SKILL.md
git commit -m "docs(skills): 新建 reviewer-mode 活性注入入口 (Task B5)"
```

---

## Phase C — CLAUDE.md 精简（最后做，有备份）

> 风险最高，每步独立 commit，可逐节回滚。备份 `CLAUDE.md.bak.20260707` 已在。

### Task C1: CLAUDE.md 新增「角色体系入口」节

**Files:**
- Modify: `CLAUDE.md`（在「论断纪律」节之后插入新节）

- [ ] **Step 1: 读 CLAUDE.md 定位插入点**

Run: `grep -n "^## 论断纪律\|^## Workflow Pipeline" CLAUDE.md`
插入点：在「论断纪律」节结束、「Workflow Pipeline」节开始之间。

- [ ] **Step 2: 插入「角色体系入口」节**

新节内容（约 30 行）：

```markdown
## 角色体系入口（souls + skills 活性注入）

工作流角色定义存放于 `.claude/souls/{role}/soul.md`（8 角色：orchestrator/architect/planner/coder/tester/reviewer/reporter/debugger）。souls 不自动加载 — 按下表触发条件 **主动调用对应 skill 或 Read soul**。

### 活性注入路由表

| 触发条件 | 角色 | 注入方式 |
|---|---|---|
| 写/改 `.cs` `.vue` `.ts` 代码 | Coder | 调用 `coder-mode` skill |
| 收到新需求 / 设计架构 / 产出 architecture.md | Architect | 调用 `architect-mode` skill |
| 产出 plan.md / 任务分级 / 需求提取清单 | Planner | 调用 `planner-mode` skill |
| 主 Claude 自审代码 / 3+ 文件变更 / 提 PR | Reviewer | 调用 `reviewer-mode` skill（隔离审查走 dispatch `code-reviewer`）|
| 产出 delivery_report.md / 会话收尾 / 归档 | Reporter | 调用 `reporter-mode` skill |
| 后端/API/Skill/IR 验证 · Dev Loop | Tester | dispatch `jnpf-tester` agent |
| 编译失败/测试失败/≥3次修复无效/>10min 无进展 | Debugger | dispatch `jnpf-debugger` agent 或 `/data-driven-debug` |
| 任务流转判定（缺哪个产出物）| Orchestrator | Read `souls/orchestrator/soul.md`（主 Claude 默认扮演）|

### workspace/ 产出物流转

```
requirements.md → architecture.md → plan.md → code_changes.md → test_report.md → review_report.md → delivery_report.md → _completed/{任务名}-{时间戳}/
```

任一阶段编译失败/测试失败/运行时异常 → 切 Debugger（产出 debug_report.md）→ 返回断点。

> 角色切换状态机详情：`.claude/souls/orchestrator/soul.md` §N
```

- [ ] **Step 3: 验证插入**

Run: `grep -c "角色体系入口\|活性注入路由表\|coder-mode skill" CLAUDE.md`
Expected: ≥ 3

- [ ] **Step 4: 精确 commit**

```bash
git add CLAUDE.md
git commit -m "docs(claude.md): 新增角色体系入口节 + 活性注入路由表 (Task C1)"
```

---

### Task C2: CLAUDE.md 删除已下沉到 souls 的章节

**Files:**
- Modify: `CLAUDE.md`（删除 6 个已下沉章节）

- [ ] **Step 1: 确认 Phase A 已完成（前置依赖）**

Run: `grep -l "Dev-Deploy-Debug Loop\|自动测试闭环" .claude/souls/tester/soul.md .claude/souls/coder/soul.md`
Expected: 两个文件都命中（内容已下沉）。若未命中 → 停止，先回 Phase A 补。

- [ ] **Step 2: 删除「自动测试·自动修复闭环」章节**

删除 CLAUDE.md 中 `## 🔄 自动测试 · 自动修复闭环（Dev-Deploy-Debug Loop）` 整节（到下一个 `## ` 前），替换为 1 行指针：

```markdown
## 🔄 自动测试闭环 → souls/tester/soul.md

Dev-Deploy-Debug Loop（dotnet build → jnpf-api.mjs → pnpm test:api）详见 `.claude/souls/tester/soul.md`。编码侧引用见 `.claude/souls/coder/soul.md`。
```

- [ ] **Step 3: 删除 Data-Driven Debug 工具链表（Core Principle 章节内）**

Core Principle 章节保留「Evidence Over Assumption 原则 + 错误做法/正确做法表 + 排除步骤」（核心原则不下沉），只删除其下的「🔧 Data-Driven Debug 工具链（四件套 + Phase B 增强）」子表，替换为指针：

```markdown
### Data-Driven Debug 工具链 → souls/debugger/soul.md

四件套（full-fidelity-debug / visual-debug / agent-probe / netcoredbg-mcp）+ mistake-rag + 采集优先级详见 `.claude/souls/debugger/soul.md` §10。
```

- [ ] **Step 4: 删除「角色切换（产出物驱动）」详细状态机**

整节删除（已下沉 orchestrator soul），替换为指针（指向 C1 新增的「角色体系入口」节）：

```markdown
## 角色切换（产出物驱动）→ 角色体系入口节 + souls/orchestrator/soul.md

状态机详情见上方「角色体系入口」节 + `.claude/souls/orchestrator/soul.md`。
```

- [ ] **Step 5: 删除「Review Gate」详细规则**

替换为指针（dispatch 路由已在 C1 路由表，详细规则在 orchestrator/reviewer soul）：

```markdown
## Review Gate → souls/orchestrator/soul.md + souls/reviewer/soul.md

审查计数器 + 子代理 dispatch 路由（Phase 5→jnpf-tester，Debug→jnpf-debugger）详见 `.claude/souls/orchestrator/soul.md`。
```

- [ ] **Step 6: Workflow Pipeline 章节瘦身（留骨架，删各 Phase 明细）**

保留：Phase 1-7 + Debug 的「颜色/名称/SP 技能」总表 + 抬头声明格式要求。
删除：每个 Phase 的详细 Rule/Skill/Hook 明细（已下沉各 soul），每 Phase 留 1 行指针。
例：`Phase 4: Build → 明细见 souls/coder/soul.md`

- [ ] **Step 7: 行数验证**

Run: `wc -l CLAUDE.md`
Expected: 较原 563 明显下降（目标 < 450，最终 C3 后 ~375）

- [ ] **Step 8: 关键内容未丢失验证**

Run: `grep -c "Evidence Over Assumption\|三元组\|R12\|Supreme Iron Law\|S1.*brainstorming" CLAUDE.md`
Expected: ≥ 5（核心原则全保留）

- [ ] **Step 9: 精确 commit**

```bash
git add CLAUDE.md
git commit -m "docs(claude.md): 删除已下沉章节（自动测试闭环/Data-Driven Debug表/角色切换/Review Gate/Phase明细）(Task C2)"
```

---

### Task C3: CLAUDE.md 论断纪律改速记卡 + 指针

**Files:**
- Modify: `CLAUDE.md`（「论断纪律」章节）

- [ ] **Step 1: 定位论断纪律章节**

Run: `grep -n "^## 论断纪律" CLAUDE.md`

- [ ] **Step 2: 替换为速记卡 + 指针**

保留约 10 行速记卡（9 条铁律一句话），删除详细分级表/红旗表/速查矩阵（这些在 `rules/assertion-discipline.md` 自动加载）。章节改为：

```markdown
## 论断纪律（宪法级 — 速记卡）

> **完整条款（自动加载）：** `.claude/rules/assertion-discipline.md`
> **souls 加载指针：** `.claude/souls/_shared/assertion-discipline.md`

**9 条铁律一句话：**
1. 打标签 — API/库/类/版本 MUST 前置 [KNOWN]/[COMPUTED]/[INFERRED]/[COMMON]/[FRAME]/[GUESS]
2. 定置信度 — HIGH≥80% / MED 50-80% / LOW 20-50% / VERY LOW <20% / UNKNOWN
3. 硬上限 — [FRAME] 和 [GUESS] 置信度上限 LOW
4. [FRAME→现实] 跨越必标注假设
5. 不知道 = "我不知道。"（不接"但是"）
6. 反谄媚 — 用户反驳 ≠ 你错，无新证据不妥协
7. 不编造引用 / 有错必改（公开修正）
8. 事后归因标 [INFERRED, post-hoc]
9. 每次响应末尾 `[RULES I BROKE]:` 自审
```

- [ ] **Step 3: 验证速记卡 + 指针**

Run: `grep -c "assertion-discipline.md\|RULES I BROKE\|9 条铁律" CLAUDE.md`
Expected: ≥ 3

- [ ] **Step 4: 精确 commit**

```bash
git add CLAUDE.md
git commit -m "docs(claude.md): 论断纪律改为速记卡+指针（全文在 rules/ 自动加载）(Task C3)"
```

---

### Task C4: CLAUDE.md 强化 On-Demand Rules 表

**Files:**
- Modify: `CLAUDE.md`（「On-Demand Rules」表）

- [ ] **Step 1: 定位 On-Demand Rules 表**

Run: `grep -n "^## On-Demand Rules\|前端类型检查 / Dev Loop 验证" CLAUDE.md`

- [ ] **Step 2: 表格新增「角色 soul 路由」列**

在现有 On-Demand Rules 表的每行补一列「角色 soul」（指向该触发条件对应的 soul/skill）。例：

| 触发条件 | 读取文件 | 角色 soul |
|---|---|---|
| 任何编码任务（架构约束）| architecture-redlines.md | architect-mode / coder-mode |
| 写后端 C# 代码 | jnpf-expert-traps.md + sql-safety.md | coder-mode |
| 写前端 Vue3 代码 | jnpf-frontend-rules.md | coder-mode |
| 后端/API/Skill/IR 验证 | jnpf-api-cli SKILL | jnpf-tester (dispatch) |
| 遇到 bug/测试失败/异常 | debugging.md | jnpf-debugger (dispatch) |
| ... | ... | ... |

- [ ] **Step 3: 验证**

Run: `grep -c "coder-mode\|architect-mode\|jnpf-tester\|jnpf-debugger" CLAUDE.md`
Expected: ≥ 4

- [ ] **Step 4: 精确 commit**

```bash
git add CLAUDE.md
git commit -m "docs(claude.md): On-Demand Rules 表新增角色 soul 路由列 (Task C4)"
```

---

## Phase D — 全量验证

### Task D1: 完整性验证（对照 spec §8）

**Files:** 无修改，仅验证

- [x] **Step 1: CLAUDE.md 行数达标**

Run: `wc -l CLAUDE.md`
Expected: ~375 行（接受区间 300-450）

- [x] **Step 2: 备份未被破坏**

Run: `diff CLAUDE.md.bak.20260707 <(git show HEAD:CLAUDE.md 2>/dev/null) || echo "NOTE: 备份是改前版，与 HEAD 不同属正常"`
确认备份 = 改前版本（备份在第一个 CLAUDE.md commit 之前生成）。

- [x] **Step 3: souls 8 文件齐全 + 增厚**

Run: `wc -l .claude/souls/*/soul.md | sort -n`
Expected: 8 个文件，debugger/tester/coder/orchestrator 明显增厚（较原 125/123/112/76 行增加）

- [x] **Step 4: 5 个新 skill 存在 + frontmatter 合法**

Run:
```bash
for f in coder architect planner reporter reviewer; do
  echo "=== $f-mode ==="
  head -4 .claude/skills/$f-mode/SKILL.md
done
```
Expected: 每个文件前 4 行含 `---` / `name: $f-mode` / `description: ...` / `---`

- [x] **Step 5: 核心原则保留（最关键）**

Run:
```bash
grep -c "Evidence Over Assumption" CLAUDE.md    # Expected: ≥1
grep -c "三元组\|R12" CLAUDE.md                 # Expected: ≥1
grep -c "Supreme Iron Law" CLAUDE.md            # Expected: ≥1
grep -c "S1.*brainstorming\|S1 铁律" CLAUDE.md  # Expected: ≥1
```

- [x] **Step 6: 下沉内容确实在 souls（非丢失）**

Run:
```bash
grep -l "api/oauth/Login\|pnpm test:api" .claude/souls/tester/soul.md   # 自动测试闭环
grep -l "full-fidelity-debug\|mistake-rag" .claude/souls/debugger/soul.md # Data-Driven Debug
grep -l "workspace/_completed\|角色判定" .claude/souls/orchestrator/soul.md # 状态机
grep -l "requesting-code-review\|错题本追加" .claude/souls/reviewer/soul.md # Review Gate
```
Expected: 全部命中。

- [x] **Step 7: 活性注入路由在 CLAUDE.md 可见**

Run: `grep -c "coder-mode skill\|architect-mode skill\|活性注入路由表" CLAUDE.md`
Expected: ≥ 3

- [x] **Step 8: 提交验证报告**

无需 commit。在会话输出验证结论（PASS/FAIL 逐项）。

---

## Self-Review（plan 写完后自查）

**1. Spec 覆盖：**
- spec §3.1 下沉 7 类 → Tasks A1-A8 ✅
- spec §3.2 论断纪律指针 → Task C3 ✅
- spec §3.3 保留 + 新增角色入口 → Tasks C1, C4 ✅
- spec §4 活性注入 skill 化 → Tasks B1-B5 ✅
- spec §8 验证 → Task D1 ✅
- 无遗漏。

**2. 占位符扫描：** 无 TBD/TODO；skills 内容完整；souls 搬运任务明确指明源章节 + 适配要点（搬运类不重复全文，因内容已在 CLAUDE.md，执行者直接读源）。

**3. 类型/命名一致：** skill 名统一 `{role}-mode`（coder-mode/architect-mode/planner-mode/reporter-mode/reviewer-mode）；soul 引用路径统一 `souls/{role}/soul.md`；agent 名 `jnpf-tester`/`jnpf-debugger` 与现有一致。

**4. 依赖顺序：** Phase C2 Step 1 显式检查 Phase A 完成（前置依赖），防止删早了内容丢失。
