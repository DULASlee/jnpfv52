# CLAUDE.md 角色规则下沉设计

> **日期：** 2026-07-07
> **主题：** 将 CLAUDE.md 中角色专属规则下沉到 `souls/{role}/soul.md`，CLAUDE.md 精简为全局元规则 + 入口路由
> **备份：** `CLAUDE.md.bak.20260707`（已生成）
> **分支：** `frontend-architecture-refactor`（非 main，安全）

---

## 1. 背景与目标

### 现状问题
- `CLAUDE.md` = **563 行 / 32K**，角色专属规则与全局元规则混杂，每次会话全量注入主上下文，成本高且信噪比低。
- 角色专属规则（调试工具链、自动测试闭环、七阶段流水线明细、角色切换状态机、Review Gate）堆在 CLAUDE.md，但这些只在扮演对应角色时才需要。
- 项目已存在完整的三层文档架构，但 CLAUDE.md 没有充分利用它，反而重复承载了角色内容。

### 目标
1. **角色专属规则下沉**到 `souls/{role}/soul.md`（全角色已存在，采用「指针 + 速记卡」模式）
2. **CLAUDE.md 精简**到 ~300 行（保留全局元规则 + 角色体系入口路由 + 索引）
3. **核心架构原则保留**（Evidence Over Assumption、三元组 R12、S1-S6、Supreme Iron Law 等「每次响应」级规则不下沉）
4. **活性注入保证**：下沉的 soul 必须有可靠机制被加载调用，不能成为死文件

### 非目标
- 不动 `agents/*.md`（jnpf-tester / jnpf-debugger 保持现状）
- 不改 hook 注册结构（settings.json hooks 不变）
- 不改 rules/*.md 内容（它们是自动加载的完整信源，保持）
- 不改 Cursor 镜像规则（.cursor/rules/）

---

## 2. 关键洞察（决定方案可行性）

### 2.1 自动加载边界（已核实）

| 文件 | 自动加载？ | 机制 |
|---|---|---|
| `CLAUDE.md` | ✅ 是 | Claude Code 默认项目指令 |
| `.claude/rules/*.md` | ✅ 是 | project instructions（当前 session 上下文已确认）|
| `souls/{role}/soul.md` | ❌ 否 | 按需 Read |
| `agents/*.md` | ❌ 否 | Agent 工具 dispatch 时加载 |
| `skills/*/SKILL.md` | ❌ 否 | Skill 工具调用时活性注入 |

### 2.2 推论
- **rules/ 已承载完整规则且自动加载** → CLAUDE.md 与 rules/ 重复的部分（如论断纪律摘要）可直接改为指针，无需下沉到 souls/。
- **souls/ 不自动加载** → 下沉到 souls/ 的内容主 Claude 默认看不到，必须配活性注入机制。
- **skills/ 是 Claude Code 原生活性注入机制** → Skill 工具调用时把内容注入当前上下文，最适合「主 Claude 扮演某角色」场景。

---

## 3. 迁移矩阵

### 3.1 🟢 下沉到 `souls/{role}/soul.md`（CLAUDE.md 独有的角色专属内容）

| CLAUDE.md 内容块 | 估行 | 去向 soul |
|---|---|---|
| Data-Driven Debug 工具链四件套表 | ~18 | `souls/debugger/soul.md`（去重，已预注入 data-driven-debug 技能）|
| 自动测试·自动修复闭环（标准闭环/登录协议/工具链表/禁止清单）| ~55 | `souls/tester/soul.md`（主）+ `souls/coder/soul.md`（编码步骤引用）|
| Workflow Pipeline 各 Phase 的 SP/Rule/Skill 明细 | ~45 | `souls/{architect,planner,coder,tester,reviewer,reporter}/soul.md`（各自阶段）|
| Phase 抬头声明模板（颜色/SP 映射表）| ~20 | 各角色 soul 顶部 |
| 角色切换（产出物驱动）状态机 + workspace/ 流转规则 | ~40 | `souls/orchestrator/soul.md` |
| Review Gate（审查计数器 / 子代理 dispatch 路由）| ~15 | `souls/reviewer/soul.md` + `souls/orchestrator/soul.md` |
| Debug Path（第 8 角色中断驱动章节）| ~10 | `souls/debugger/soul.md`（已有骨架，补全）|

### 3.2 🟡 CLAUDE.md 改为指针（rules/ 已自动加载，删除重复正文）

| 内容 | 当前 | 改为 | 全文位置 |
|---|---|---|---|
| 论断纪律章节（~25 行摘要）| 摘要正文 | **速记卡（~10 行 9 条铁律一句话）+ 指针** | `rules/assertion-discipline.md`（已自动加载）|

> 保留速记卡而非纯 1 行指针：论断纪律是「每条响应强制 [RULES I BROKE] 自审」的宪法级规则，主上下文必须保留可见性，否则会失效。

### 3.3 🔵 CLAUDE.md 保留（全局元规则 + 入口 + 索引）

**保留完整（核心架构原则，不下沉）：**
- Core Identity
- Core Principle: Evidence Over Assumption（完整保留 — 数据驱动调试是项目第一性原则）
- 🔴 Superpowers Mandatory S1-S6（每次响应铁律）
- ⬛ Supreme Iron Law（E1/E2/E3 + 无效声称清单）
- Architecture Redlines R1-R12 概要表（含三元组 R12）

**保留精简：**
- Workflow Pipeline 7 阶段骨架图（全局颜色视图，明细下沉各 soul）
- 论断纪律速记卡（见 3.2）

**保留不变（索引/参考）：**
- Build & Run / Context at a Glance / Agent Toolchain / Hooks / Slash Commands / Technical Preferences / Git Iron Law
- On-Demand Rules 表（**强化**：新增「角色 soul 路由」列）

**新增：**
- 🆕 **角色体系入口**节（~30 行）— souls/ 机制说明 + 活性注入路由表 + workspace/ 流转图指针

### 3.4 行数估算

```
当前:  563 行
下沉:  -203 行（3.1 表）
指针:  -15 行（3.2，论断纪律 25→10）
新增:  +30 行（角色体系入口节）
─────────────────
目标:  ~375 行（精简约 33%）
```

> **诚实声明：** 实际精简到 ~375 行而非最初估的 ~250 行。原因：用户要求「核心架构原则不下沉」（Evidence/三元组/S1-S6/Supreme 全保留），这些占约 120 行。**核心原则可见性优先于行数指标。** 若后续要求进一步精简，可再把 S1-S6/Supirme 压成纯指针（激进方案）。

---

## 4. 活性注入机制设计

### 4.1 各角色注入路径

| 角色 | 注入机制 | 文件 | 状态 |
|---|---|---|---|
| tester | Agent 工具 dispatch | `agents/jnpf-tester.md` | ✅ 已有，继承 CLAUDE.md + 预注入 jnpf-api-cli |
| debugger | Agent 工具 dispatch | `agents/jnpf-debugger.md` | ✅ 已有，继承 CLAUDE.md + 预注入 data-driven-debug |
| coder | **Skill 活性注入** | `skills/coder-mode/SKILL.md`（新建）| 🆕 |
| architect | **Skill 活性注入** | `skills/architect-mode/SKILL.md`（新建）| 🆕 |
| planner | **Skill 活性注入** | `skills/planner-mode/SKILL.md`（新建）| 🆕 |
| reviewer | 全局 code-reviewer agent + 项目 soul 引用 | `souls/reviewer/soul.md`（被 code-reviewer 引用）| ⚠️ dispatch code-reviewer 时 prompt MUST 含「先 Read `.claude/souls/reviewer/soul.md` 再审查」；可选 `reviewer-mode` skill 供主 Claude 自审 |
| reporter | **Skill 活性注入** | `skills/reporter-mode/SKILL.md`（新建）| 🆕 |
| orchestrator | 主 Claude 默认扮演（角色判定逻辑）| `souls/orchestrator/soul.md` | CLAUDE.md 角色体系入口节强制 Read |

### 4.2 Skill 化设计（coder/architect/planner/reporter）

每个 `skills/{role}-mode/SKILL.md` 结构：

```yaml
---
name: {role}-mode          # 如 coder-mode
description: <触发条件描述，让主 Claude 知道何时调用>
---

# {Role} Mode — 活性加载 souls/{role}/soul.md

调用此 skill 即进入 {Role} 角色。立即 Read 以下文件并按其约束行动：

1. `D:\JNPF-v52\.claude\souls\{role}\soul.md`（角色定义：身份/约束/输入/输出/禁止/回退）
2. （按需）该角色阶段的 rules 文件（已在 soul.md「输入格式」列出）

## 触发场景
- <具体触发条件，如 coder: 「写/改 .cs 或 .vue/.ts 代码时」>
- <如 architect: 「收到新需求、设计架构、输出 architecture.md 时」>
```

**触发条件（description）映射：**
- `coder-mode`: 写后端 C# / 前端 Vue 代码、修改 .cs/.vue/.ts
- `architect-mode`: 收到新需求、产出 architecture.md、架构决策
- `planner-mode`: 产出 plan.md、任务分级、需求提取清单
- `reporter-mode`: 产出 delivery_report.md、归档、提交前

### 4.3 单一信源原则

- **souls/{role}/soul.md = 角色定义权威信源**（含身份/约束/输入/输出格式/禁止/回退）
- **skills/{role}-mode/SKILL.md = 轻量注入入口**（不复制 soul 内容，只指向）
- **agents/*.md = dispatch 入口**（tester/debugger，已声明继承）
- 三者不重复内容，各司其职

---

## 5. 文件变更清单

### 5.1 新建文件（5 个）

```
.claude/skills/coder-mode/SKILL.md
.claude/skills/architect-mode/SKILL.md
.claude/skills/planner-mode/SKILL.md
.claude/skills/reporter-mode/SKILL.md
.claude/skills/reviewer-mode/SKILL.md   # 可选，统一模式
```

### 5.2 修改文件（9 个）

```
CLAUDE.md                                  # 精简 563→~375，新增角色体系入口节
.claude/souls/architect/soul.md            # +Phase 1-2 明细 + 抬头
.claude/souls/planner/soul.md              # +Phase 3 明细 + 需求提取清单 + 抬头
.claude/souls/coder/soul.md                # +Phase 4 明细 + 自动测试闭环(编码侧) + 抬头
.claude/souls/tester/soul.md               # +Phase 5 明细 + 自动测试闭环(主) + 抬头
.claude/souls/reviewer/soul.md             # +Phase 6 明细 + Review Gate + 全局 agent 继承指引 + 抬头
.claude/souls/reporter/soul.md             # +Phase 7 明细 + session-key-points 强制项 + 抬头
.claude/souls/debugger/soul.md             # +Data-Driven Debug 工具链表 + Debug Path 补全
.claude/souls/orchestrator/soul.md         # +角色切换状态机 + workspace/ 流转 + Review Gate dispatch 路由
```

### 5.3 不变文件

- `.claude/agents/jnpf-tester.md` / `jnpf-debugger.md`（dispatch 入口，保持）
- `.claude/rules/*.md`（自动加载信源，保持）
- `.claude/settings.json`（hooks 注册，保持）
- `.cursor/rules/*`（Cursor 镜像，本次不动）

---

## 6. CLAUDE.md 精简后目录

```
# CLAUDE.md
## Core Identity
## Core Principle: Evidence Over Assumption          ← 保留完整
## 🔴 Superpowers Mandatory (S1-S6)                  ← 保留
## ⬛ Supreme Iron Law                                ← 保留
## 论断纪律                                           ← 改速记卡+指针
## 🆕 角色体系入口 (souls + skills 活性注入路由)      ← 新增
## Workflow Pipeline (7 阶段骨架图)                   ← 明细下沉，留骨架
## Architecture Redlines (R1-R12 概要表)              ← 保留
## Build & Run
## Context at a Glance
## Agent Toolchain
## On-Demand Rules                                    ← 强化(加角色路由列)
## Technical Preferences
## Hooks
## Slash Commands
## Git Iron Law
```

**移除的章节（下沉到 souls）：**
- 🔄 自动测试·自动修复闭环 → souls/tester + coder
- Data-Driven Debug 工具链四件套表 → souls/debugger
- 角色切换（产出物驱动）详细状态机 → souls/orchestrator
- Review Gate 详细规则 → souls/reviewer + orchestrator
- Debug Path → souls/debugger
- Workflow Pipeline 各 Phase 的 SP/Rule/Skill 明细 → 各角色 soul

---

## 7. 回滚策略

1. **完整备份**：`CLAUDE.md.bak.20260707` 已存在（32K，与原文件一致）
2. **分支安全**：当前在 `frontend-architecture-refactor`，非 main
3. **分步提交**：每个文件独立 commit，可逐文件 revert
4. **souls 增量**：souls/ 修改是「追加内容」（不删除现有定义），风险低
5. **skills 新建**：新文件不影响现有流程，可整目录删除回滚
6. **回滚命令**：`git checkout CLAUDE.md && rm -rf .claude/skills/{coder,architect,planner,reporter,reviewer}-mode && git checkout .claude/souls/`

---

## 8. 验证方法

| 验证项 | 命令 | 预期 |
|---|---|---|
| CLAUDE.md 精简 | `wc -l CLAUDE.md` | ~375 行（< 450）|
| 备份完整 | `diff CLAUDE.md.bak.20260707 <(git show HEAD:CLAUDE.md)` | 无差异（备份 = 改前）|
| souls 文件存在 | `ls .claude/souls/*/soul.md` | 8 个文件 |
| 新 skills 存在 | `ls .claude/skills/*-mode/SKILL.md` | 4-5 个文件 |
| Skill frontmatter 合法 | 各 SKILL.md 含 `name:` + `description:` | Claude Code 可发现 |
| 关键内容未丢失 | grep「Evidence Over Assumption」「三元组」「R12」CLAUDE.md | 命中（核心原则保留）|
| 下沉内容存在 | grep「自动测试闭环」souls/tester/soul.md / 「Data-Driven Debug」souls/debugger/soul.md | 命中 |
| docs build 不破坏 | 文档变更，无 dotnet/pnpm 影响 | N/A |

---

## 9. 实施顺序（供 writing-plans 细化）

1. **Phase A**：souls/ 追加内容（低风险，纯追加）
   - debugger soul 补 Data-Driven Debug 表 + Debug Path
   - tester soul 补自动测试闭环
   - coder soul 补 Phase 4 + 闭环引用
   - 各角色 soul 补 Phase 明细 + 抬头
   - orchestrator soul 补状态机 + Review Gate
2. **Phase B**：新建 skills/{role}-mode/（5 个轻量文件）
3. **Phase C**：CLAUDE.md 精简（最危险，最后做，有备份）
   - 先增「角色体系入口」节
   - 再删下沉章节
   - 论断纪律改速记卡
4. **Phase D**：验证（第 8 节全部检查项）
