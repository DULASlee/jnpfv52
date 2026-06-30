# 七阶段工作流水线 — AI 工程师强制执行清单

> 每 Phase: AI MUST 完成的动作 + Hook 阻断条件。缺任一 = BLOCK (exit 2)。

---

## Phase 1: Align

| # | AI 必须完成 | Hook |
|---|-----------|------|
| 1.1 | 输出 `╔═ 🔵 Phase 1: Align ═╗` | — |
| 1.2 | 输出 `🔄 Workflow 启动 - 分级: S/A/B - 理由: ...` | — |
| 1.3 | Write `workflow-state.json` `{phase:1, level, editCount:0, sp:{}}` | guard-workflow GATE0: 无 sp → `exit 2` ⛔ |

```
✅ Phase 1 完成
  任务: [重述]  分级: [S/A/B]  范围: [模块/文件]
  约束: [architecture-redlines / mistake-log 关键词]
```

---

## Phase 2: Brainstorm (ALL 强制)

| # | AI 必须完成 | Hook |
|---|-----------|------|
| 2.1 | 输出 `╔═ 🟡 Phase 2: Brainstorm ═╗` | — |
| 2.2 | `Skill("superpowers:brainstorming")` | — |
| 2.3 | Update state: `sp.brainstorming=true, phase=2` | GATE1: `sp.brainstorming≠true` → `exit 2` ⛔ |

```
✅ Phase 2 完成
  方案: [选定+理由]  备选: [放弃原因]  风险: [应对]
  陷阱: [mistake-log 匹配结果]  红线: [相关条款]
```

---

## Phase 2.5: Call-Chain Exploration (S/A 强制, B 推荐)

| # | AI 必须完成 | Hook |
|---|-----------|------|
| 2.5.1 | 输出 `╔═ 🟡 Phase 2.5: Call-Chain Exploration ═╗` | — |
| 2.5.2 | 确定探索范围：目标符号 + 变更性质 + 涉及模块 | — |
| 2.5.3 | `codegraph callers <symbol>` — 查上游调用方 | — |
| 2.5.4 | `codegraph callees <symbol>` — 查下游依赖 | — |
| 2.5.5 | `codegraph impact <symbol>` — 查影响面（文件+测试） | — |
| 2.5.6 | 新增功能时：`codegraph explore <concept>` 查相似实现 | — |
| 2.5.7 | 输出探索报告（调用方N个/被调用N个/影响文件N个/风险点） | — |
| 2.5.8 | Update state: `sp.codegraph-explore=true, phase=2.5` | GATE2.5: S/A+`sp.codegraph-explore≠true` → `exit 2` ⛔ |

```
✅ Phase 2.5 完成
  探索符号: [符号名]  调用方: [N个]  被调用: [N个]
  影响文件: [N个]  测试影响: [N个]  相似实现: [参考]
  风险点: [跨模块断裂 / 签名不兼容 / 测试遗漏]
```

> **反降级铁律：** 涉及跨模块/Entity变更/API签名变更/复杂逻辑 → MUST 完整探索后完整实现。
> 禁止只改当前文件不管调用方。禁止简化/降级复杂业务逻辑。
> 详细规则 → `.claude/rules/codegraph-exploration.md`

---

## Phase 3: Plan (S/A, B skip)

| # | AI 必须完成 | Hook |
|---|-----------|------|
| 3.1 | 输出 `╔═ 🟠 Phase 3: Plan ═╗` (S/A) | — |
| 3.2 | `Skill("superpowers:writing-plans")` (S/A) | — |
| 3.3 | 输出 `📋 需求提取清单` (逐条→文件/函数) | — |
| 3.4 | Update state: `sp.writing-plans=true, phase=3` | GATE2: S/A+≥2edits+`sp.writing-plans≠true` → `exit 2` ⛔ |

```
✅ Phase 3 完成
  计划: [路径]  清单: [N条]  文件: [新建N/修改N/~N行]
```

---

## Phase 4: Build

| # | AI 必须完成 | Hook |
|---|-----------|------|
| 4.1 | 输出 `╔═ 🟢 Phase 4: Build ═╗` | — |
| 4.2 | Phase4 四选一 SP (executing-plans/subagent/dispatching/worktrees) | — |
| 4.3 | todo 注入: `🔍 代码审查` + `📝 错题本` | — |
| 4.4 | Update state: `sp.{技能}=true, phase=4, editCount++` | GATE3: S/A+≥2edits+四选一全false → `exit 2` ⛔ |

> TDD 红绿按需, 不强制。单元测试: guard-finish L2 每次强制。

```
✅ Task N: [名称]  文件: [路径] (±N行)  验证: [build✅/test✅]
```

---

## Phase 5: Verify

| # | AI 必须完成 | Hook |
|---|-----------|------|
| 5.1 | 输出 `╔═ 🔴 Phase 5: Verify ═╗` | — |
| 5.2 | `Skill("superpowers:verification-before-completion")` — Gate Function 5步 | — |
| 5.3 | `Skill("start-dev")` — 启动开发环境 (前端变更时) | — |
| 5.4 | `Skill("playwright")` — E2E截图 → `.claude/evidence/` (前端变更时) | — |
| 5.5 | `dotnet build` + `dotnet test` / `vue-tsc` | guard-finish L1/L2/L3 自动 |
| 5.6 | Update state: `sp.verification=true, sp.start-dev/playwright, phase=5` | GATE4: ≥4edits+verify≠true → ⛔ |
| | | L0: 前端+`start-dev≠true` → ⛔ |
| | | L0: 前端+`playwright≠true` → ⛔ |
| | | L1: build失败 → ⛔ |
| | | L2: test失败 → ⛔ (每次强制) |
| | | L3: vue-tsc错误 → ⛔ |
| | | L4: 无E2E截图 → ⛔ |

```
✅ Phase 5 完成
  Gate: IDENTIFY→RUN→READ→VERIFY→CLAIM ✅
  Build: [0 errors]  Test: [N/N]  vue-tsc: [✅/skip]
  E1: [路径] (NKB,Nmin)  E2: [操作步骤]  E3: [UI状态]
```

---

## Phase 6: Review (max 3 cycles)

| # | AI 必须完成 | Hook |
|---|-----------|------|
| 6.1 | 输出 `╔═ 🟣 Phase 6: Review ═╗` | — |
| 6.2 | Phase6 四选一 SP (requesting-code-review/full-review/security-review/health-check) | — |
| 6.3 | 修复 Critical → 重跑 test → 重审 (max 3) | — |
| 6.4 | `Skill("superpowers:receiving-code-review")` | — |
| 6.5 | `📝错题本` todo → completed | — |
| 6.6 | Update state: `sp.{技能}=true, sp.receiving=true, phase=6` | GATE5: ≥5edits+四选一全false → ⛔ |
| | | GATE6: ≥6edits+receiving≠true → ⛔ |

```
✅ Phase 6 完成
  审查: [类型] → PASS (N cycle)  Critical:[N修复]  Warning:[N评估]
  错题本: [Mxxx]
```

---

## Phase 7: Complete

| # | AI 必须完成 | Hook |
|---|-----------|------|
| 7.1 | 输出 `╔═ ⚫ Phase 7: Complete ═╗` | — |
| 7.2 | `Skill("superpowers:finishing-a-development-branch")` | — |
| 7.3 | `Skill("pre-commit")` | — |
| 7.4 | 追加 `mistake-log.md` 今日条目 | — |
| 7.5 | Write `session-key-points.md` | — |
| 7.6 | Update state: `sp.finishing=true, sp.pre-commit=true, phase=7` | L0: finishing≠true → ⛔ |
| | | L0: pre-commit≠true → ⛔ |
| | | L0: mistake-log 无今日 → ⛔ |

```
✅ Phase 7 完成
  变更: [摘要]  文件: [新建N/修改N]  测试: [N/N]
  E2E: [截图]  错题本: [Mxxx]  决策: [session-key-points]
  Hook: guard-finish L0-L4 [全部✅]
```

---

## Debug Path (→ 返回 Phase 5)

| # | AI 必须完成 | Hook |
|---|-----------|------|
| D.1 | 输出 `╔═ ⚡ Debug ═╗` + 问题+复现+阶段 | — |
| D.2 | `Skill("superpowers:systematic-debugging")` — 4阶段 | — |
| D.3 | `Skill("data-driven-debug")` — **每次强制** 抓数据 | — |
| D.4 | 追加 `mistake-log.md` | — |
| D.5 | Update state: `sp.systematic-debugging=true, sp.data-driven-debug=true` | guard-workflow: systematic=true+data-drv≠true → ⛔ |
| | | L0: 错题本有今日+任一false → ⛔ |
| D.6 | **debug_reentry += 1**；IF ≤ 2 → **返回 Phase 5**；ELSE → **触发 PHASE_HALT** | — |

### PHASE_HALT 熔断协议（新增）

```
╔═ 🔴 PHASE_HALT: Circuit Breaker ═╗
```

**进入条件：** `debug_reentry > 2`（同一任务 Debug→Phase 5 循环 ≥ 3 次）

**进入后 AI MUST：**
1. **停止所有文件修改** — 文件系统进入只读模式（除复盘报告外）
2. **输出《失败复盘报告》**：
   - 原始任务目标
   - 已尝试的 N 次修复方案及失败原因
   - 当前代码状态（Git commit hash）
   - 建议的人工介入方向（架构级重构 / 依赖第三方库修复 / 需求重新评估）
3. **设置 `session_flag = HALTED`** — 阻止任何自动重试
4. **通知用户** — "任务已熔断，请查看复盘报告"

**恢复机制：**
- 人类工程师可手动重置 `debug_reentry = 0` 并指定新修复策略
- 或关闭当前 session，基于复盘报告创建新任务

```
✅ Debug 完成（正常路径）
  问题: [描述]  根因: [证据]  数据: [手段→发现]
  修复: [单变量]  验证: [症状消失✅]  错题本: [Mxxx]  返回: Phase 5 (重入 #N/2)

🛑 PHASE_HALT 触发（熔断路径）
  重入次数: [N/2]  失败原因: [摘要]
  复盘报告: [路径]  人工介入方向: [建议]
```

---

## SessionStart (自动)

| Hook | 职责 |
|------|------|
| `session-scheduler.mjs` | **唯一入口** — 60s 防抖 + 子进程防重入 + 轻量错题本；禁止批量加载 MCP/Skill |
| `guard-skill-load.mjs` | PreToolUse(Skill) — 15s/6次限速，防 Skill 风暴 |

> 插件级 SessionStart（superpowers / episodic-memory）由 Claude Code 自动注册，项目 hook 通过 `.session-init-lock.json` 防抖联动。
