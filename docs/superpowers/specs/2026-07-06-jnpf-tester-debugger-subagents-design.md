# JNPF 专属子 agent —— jnpf-tester / jnpf-debugger 设计

> **日期**：2026-07-06
> **优先级**：P2 基建债（服务于 Dev Loop 与 Debug Path 两条主链）
> **类型**：agent 基建 / dispatch 改线
> **业务锚点（B0）**：jnpf-tester 服务 Dev Loop（`jnpf-api.mjs` / `pnpm test:api` 模拟真实用户 API 路径）；jnpf-debugger 服务 Debug Path（`≥3 次失败` / `>10min 无进展` 时抓运行时数据，对应 S5 铁律）。两者都把"用户操作路径 + 可感知产物"作为验收锚。

---

## 1. 背景与问题诊断

### 1.1 现状（[KNOWN]）

项目里"子 agent"概念目前是**断开的两套体系**：

| 体系 | 位置 | 性质 | 现状 |
|---|---|---|---|
| **Souls（角色上下文）** | `.claude/souls/{tester,debugger,...}/soul.md` | 注入**主 Claude 循环**的角色说明书，**不是**可 dispatch 的子 agent | tester/debugger soul 已口头引用测试工具与 data-driven-debug 技能 |
| **Dispatchable 子 agent** | `subagent_type`（Agent 工具）/ `.claude/agents/*.md` | 隔离的独立 agent | 项目级 `.claude/agents/` **不存在**；只有用户级 3 个**通用** agent（`~/.claude/agents/` 的 test-runner/code-reviewer/security-scanner），对 JNPF 工具链**一无所知** |

`review-workflow.md`（全局）的 Stage 1 用 `subagent_type: "general-purpose"` + prompt 模板 dispatch 验证子 agent。该 agent 不继承 JNPF 工具链知识、无 `jnpf-api.mjs` / `pnpm test:api` / `data-driven-debug` 任何上下文——每次 dispatch 都要主 Claude 在 prompt 里现拼工具说明，且子 agent 无正确工具权限（如 netcoredbg MCP）。

### 1.2 要解决什么

把流水线 Phase 5（Verify）与 Debug Path 用的凑合 dispatch，升级为**两个 JNPF 专属、工具权限正确、技能预注入、输出 schema 固定**的可 dispatch 子 agent：

- **jnpf-tester**：装测试工具链（Dev Loop 核心），Phase 5 用
- **jnpf-debugger**：装 data-driven-debug 工具链（脚本 + netcoredbg-mcp + 补充手段），Debug Path 用

### 1.3 关键事实依据（[KNOWN, HIGH]，来自 Claude Code 官方文档）

| 事实 | 来源 |
|---|---|
| 子 agent 自动继承项目 CLAUDE.md 层级 + git 状态（仅 Explore/Plan 例外） | `code.claude.com/docs/en/sub-agents.md#what-loads-at-startup` |
| `skills:` frontmatter 在启动时把技能全文注入子 agent 上下文 | `code.claude.com/docs/en/sub-agents.md#preload-skills-into-subagents` |
| MCP 工具默认由子 agent 继承主会话；可用 `tools: mcp__<server>__*` 授权 | `code.claude.com/docs/en/sub-agents.md#available-tools` |
| 项目级 agent 同名覆盖用户级 | `code.claude.com/docs/en/sub-agents.md#choose-the-subagent-scope` |
| `tools:` 省略 = 继承全部；列出 = 仅白名单 | 同上 |

这些事实使"薄壳 + 技能预注入"方案可行：agent 文件不必复制铁律与技能正文，靠继承 + `skills:` 字段承载。

---

## 2. 架构设计

### 2.1 与现有体系的关系

```
现有                                      新增
────                                      ────
.claude/souls/tester/soul.md     ←→      .claude/agents/jnpf-tester.md
  角色上下文，注入主循环                    独立 dispatch，Phase 5 用
.claude/souls/debugger/soul.md   ←→      .claude/agents/jnpf-debugger.md
  角色上下文，注入主循环                    独立 dispatch，Debug Path 用
.claude/skills/jnpf-api-cli         ──→  skills: frontmatter 预注入 → jnpf-tester
.claude/skills/data-driven-debug    ──→  skills: frontmatter 预注入 → jnpf-debugger
```

设计原则：

- **Souls 不动**。它们是主 Claude 扮演角色时的上下文，保留。
- **新 agent 是 soul 的"可执行孪生"**：soul 描述职责，agent 承载工具权限 + 预注入技能 + 输出 schema，能被 `subagent_type` dispatch。
- **命名 `jnpf-*`**：避免与全局 `test-runner` 同名覆盖，不污染用户在其他项目对通用 `test-runner` 的依赖。
- **铁律不重写**：子 agent 继承项目 CLAUDE.md（论断纪律、工程铁律、B0 业务优先、S1-S6）+ git 状态。

### 2.2 两个 agent 的共同契约

- **物理隔离**：每次 dispatch 全新会话，无跨 dispatch 记忆。跨会话上下文靠 mistake-rag / 错题本。
- **均无 Write/Edit**：测试员不改代码，诊断员不改代码。输出作为 final message 返回，由主 Claude（dispatcher）持久化。
  - tester → 返回 `fugu/test-report-v1` JSON
  - debugger → 返回 debug report，主 Claude 写入 `workspace/debug_report.md`
- **论断纪律强制**：所有技术论断打标签（`[KNOWN]/[INFERRED]/...`），置信度上限遵守。
- **铁律继承**：S6（无浏览器 API 验证）、S5（数据驱动调试）、Law 2（验证即完成）、Law 4（无捷径）等通过 CLAUDE.md 继承生效。

---

## 3. 组件设计 —— jnpf-tester

### 3.1 职责

Phase 5 Verify 执行者。跑 Dev Loop 核心验证（dotnet build / pnpm type-check / jnpf-api.mjs 冒烟 / pnpm test:api），产出结构化测试报告，**不改代码**。

### 3.2 frontmatter

```yaml
---
name: jnpf-tester
description: JNPF Dev Loop 验证子 agent。dotnet build / pnpm type-check / jnpf-api.mjs 冒烟 / pnpm test:api，产出 fugu/test-report-v1 JSON。Phase 5 Verify 专用。禁止手点浏览器登录（S6），不改代码。
tools: Bash, Read, Grep, Glob
skills: jnpf-api-cli
---
```

**字段理由**：
- `tools: Bash, Read, Grep, Glob` —— 跑命令 + 读文件 + 搜索引用；**无 Write/Edit**（不改代码）；截图/证据由 Bash 调脚本自行落盘 `.claude/evidence/`。
- `skills: jnpf-api-cli` —— 启动即注入 jnpf-api-cli 技能全文（Token 获取、登录协议、标准闭环、禁止清单）。

### 3.3 body 内容大纲（~120 行）

1. **身份与硬约束**
   - S6 铁律：禁止手点浏览器登录，用 `scripts/lib/jnpf-auth.mjs` 或 `jnpf-api.mjs`
   - Gate Function 5 步（IDENTIFY→RUN→READ→VERIFY→CLAIM）
   - 无 Write/Edit——不改代码；失败只报 `suggested_fix`
   - 论断标签强制

2. **症状→命令决策矩阵**

| 变更类型 | 必跑命令 | 预期 |
|---|---|---|
| 后端 `.cs` | `cd backend; dotnet build` → `node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser` | 0 error / API 200 |
| 前端 `.vue/.ts` | `cd jnpf-web-vue3; pnpm type-check`（**禁止**裸 `vue-tsc --noEmit`，OOM） | 0 error |
| API/Skill/IR | 上述两条 + `$env:E2E_PIPELINE_ID="311"; pnpm test:api` | 测试全绿 |
| Bug 修复回归 | 复现原症状的命令 + `pnpm test:api` | 症状消失 |

3. **标准命令清单**（与 `.claude/rules/testing.md` 一致，避免漂移）

4. **输出 schema** —— 复用 `souls/tester/soul.md` 的 `fugu/test-report-v1` JSON：
   ```json
   {
     "$schema": "fugu/test-report-v1",
     "checks": [{ "name": "dotnet-build", "type": "automated", "command": "...", "result": "PASS", "exit_code": 0, "evidence": "..." }],
     "summary": { "total": N, "passed": N, "failed": N, "skipped": N },
     "verdict": "PASS | FAIL | PARTIAL",
     "failed_checks": [{ "name": "...", "error": "...", "suggested_fix": "..." }]
   }
   ```
   与现有状态机契约兼容，便于 orchestrator 自动判断回退。

5. **失败回退契约**：`verdict: FAIL` → 必须填 `failed_checks[].suggested_fix` → 主 Claude 据此决定是否 dispatch `jnpf-debugger`。

### 3.4 边界

- 不审查代码质量（那是 jnpf-code-reviewer / 全局 code-reviewer 的职责，未来扩展）
- 不做 UI E2E（前端 UI 验证由主 Claude 用 playwright 技能产出 E1 截图，不在本 agent）
- 不跑 promptfoo / k6 / artillery / Pact（按需由主 Claude 手动调，保持 agent 职责纯）

---

## 4. 组件设计 —— jnpf-debugger

### 4.1 职责

Debug Path 执行者。抓运行时数据定位根因，产出 debug report，**不改代码**。触发：`≥3 次修复无效` / `>10min 无进展` / `编译通过但行为异常` / `前端无响应/SSE 无数据/页面空白` / 用户 `/trace-bug` 或 `/data-driven-debug`。

### 4.2 frontmatter

```yaml
---
name: jnpf-debugger
description: JNPF 数据驱动调试子 agent。visual-debug/probe/DiagnosticsLog/mistake-rag/netcoredbg-mcp 抓运行时数据定位根因。产出 debug report。不改代码只诊断。≥3 次失败 / >10min 无进展 / 编译通过但行为异常 时 dispatch。
tools: Bash, Read, Grep, Glob, mcp__netcoredbg__*
skills: data-driven-debug
---
```

**字段理由**：
- `mcp__netcoredbg__*` —— 授予 netcoredbg MCP 全部工具（断点/变量/单步/call stack，43 个）。netcoredbg 已在项目 `mcp.json` 配置，子 agent 默认继承主会话 MCP，此处显式列入白名单。
- `skills: data-driven-debug` —— 启动即注入 data-driven-debug 技能全文（四件套工具链、故障定位流程、决策矩阵）。
- **无 Write/Edit** —— debugger 不改代码；report 作为 final message 返回给 dispatcher 持久化到 `workspace/debug_report.md`。

### 4.3 body 内容大纲（~140 行）

1. **身份与硬约束**
   - S5 铁律：数据驱动，**禁止看源码猜测**；每一个根因论断必须有运行时数据或源码证据
   - 无 Write/Edit——不改代码，修复交还 Coder
   - 3 次诊断无果 → 输出"疑似架构问题"，不继续猜
   - 论断标签强制（`[KNOWN]` 数据 / `[INFERRED]` 推理 区分）

2. **症状→工具决策矩阵**（来自 data-driven-debug SKILL）

| 症状 | 工具 | 命令 |
|---|---|---|
| UI 白屏/SSE 无数据/页面空白 | visual-debug | `node scripts/lib/visual-debug.mjs --login --url=...` |
| API 500/数据不对 | agent-probe | `node scripts/lib/probe.mjs --trace-sql GET /api/...` |
| 已触发异常 | DiagnosticsLog | `cat backend/.claude/diagnostics/session-*.jsonl \| jq 'select(.level=="error")'` |
| 不确定是否老问题 | mistake-rag | `node scripts/lib/mistake-rag.mjs "关键词"` |
| 运行时变量值/调用栈 | netcoredbg-mcp | `mcp__netcoredbg__set_breakpoint` / `get_variables` / `get_stack_trace` |
| SQL 可疑 | （报告建议） | 报告中建议主 Claude 加 `ToSql()` / `DiagnosticsLog.Sql()` 注入 |

3. **四阶段协议摘要**（详见 `.claude/rules/debugging.md`，靠 CLAUDE.md 继承）
   - Phase 1 根因调查 → Phase 2 模式分析 → Phase 3 假设检验 → Phase 4 修复建议

4. **输出格式** —— debug report（作为 final message 返回）：
   ```markdown
   # 调试报告 — {TASK_ID}
   ## 症状（观察/预期/复现步骤/稳定性）
   ## 数据链路追踪（节点 | 位置 | 预期值 | 实际值 | 判断）
   ## 根因（位置 | 原因 | 证据——日志/响应体/SQL/堆栈）
   ## 修复建议（单一方案 | 影响范围 | 验证方法）
   ## 关联错题本（匹配 Mxxx / 新增建议）
   ```
   与 `souls/debugger/soul.md` 输出格式一致，便于状态机识别。

5. **返回协议**：`✅ 调试完成 → 根因/修复建议/错题本`，交还 Coder/Tester 执行修复。

### 4.4 边界

- 不修代码（无 Write/Edit）
- 不一次追多个 bug（一次一个，其余记录）
- 不在根因不明时开处方
- netcoredbg-mcp 首次调用触发权限提示（见 §6.3）

---

## 5. dispatch 改线

| 文件 | 改动 |
|---|---|
| **`CLAUDE.md`** | Agent Toolchain 表 + Review Gate 段 + Workflow Pipeline Phase 5/Debug Path：登记 `jnpf-tester` / `jnpf-debugger` 及触发条件 |
| **`.claude/rules/workflow.md`** | Phase 5 Verify：`subagent_type: jnpf-tester`；Debug Path：`subagent_type: jnpf-debugger` |
| **`.claude/rules/debugging.md`** | "返回主流程条件" 段：明确 debug report 由 jnpf-debugger 产出 |
| **`.claude/rules/review-workflow.md`**（**新建项目级**，覆盖全局） | Stage 1 → `jnpf-tester`；Stage 3 失败回退 → `jnpf-debugger` |

**不改全局** `C:\Users\admin\.claude\rules\review-workflow.md`（避免污染其他项目）。

---

## 6. 错误处理与边界

### 6.1 agent 自身的失败回退

- **tester**：`verdict: FAIL` + `suggested_fix` → 主 Claude 决定回退到 Coder 或 dispatch jnpf-debugger
- **debugger**：≥3 次诊断无果 → 输出"疑似架构问题，建议与人类讨论"，停止猜测
- 两者均幂等：同一输入多次 dispatch 返回一致结果

### 6.2 与状态机的契约

- tester 的 `fugu/test-report-v1` 与现有 `souls/tester/soul.md` schema 一致 → 状态机 `verdict` 识别无需改造
- debugger 的 debug report 与 `souls/debugger/soul.md` 格式一致 → 状态机识别无需改造

### 6.3 MCP 权限现实（开放项）

`settings.local.json` 的 permissions allow 列表当前**不含** `mcp__netcoredbg__*`：

- jnpf-debugger 首次调 netcoredbg 会触发权限提示，需用户批准
- **两个选项**（spec 登记，实施阶段定）：
  - **A**：补 `mcp__netcoredbg__*` 到 `.claude/settings.local.json` allow → 免提示，但任何 agent/主循环都能用
  - **B**：保持提示，每次由用户批准 → 更保守
- 推荐 **A**（netcoredbg 已是受控本地 wrapper，且 debugger 场景高频需要）

### 6.4 不引入的风险

- 不修改任何后端 .cs / 前端 .vue 代码
- 不修改 .vm 模板
- 不触碰 OA/IoT/MES 模块（R5）
- 不改 mcp.json（netcoredbg 已配）
- 不改全局用户级配置

---

## 7. 验证计划

### 7.1 agent 文件有效性

- frontmatter 合法（`name`/`description`/`tools`/`skills` 字段被 Claude Code 识别）
- 通过 Claude Code 列出可 dispatch agent，确认 `jnpf-tester` / `jnpf-debugger` 出现

### 7.2 dispatch 冒烟

- **jnpf-tester**：dispatch 一个真实小验证任务（`dotnet build` + `jnpf-api.mjs GET /api/oauth/CurrentUser` + `pnpm test:api`）→ 确认返回合法 `fugu/test-report-v1` JSON，`verdict` 字段存在
- **jnpf-debugger**：dispatch 一个种子 bug（造一个 API 500 或读已有 DiagnosticsLog）→ 确认返回 debug report 含"数据链路追踪表 + 根因 + 证据"

### 7.3 证据形态

本任务是 agent 基建，非前端 UI 变更，E1 截图**不强制**（`guard-finish.mjs` 仅对前端 UI 目录要求截图）。证据形态：

- 两个 agent 文件本身（`.claude/agents/jnpf-{tester,debugger}.md`）
- dispatch 返回的 JSON / report 存档到 `.claude/evidence/`

### 7.4 回归

- `node scripts/test-hooks.mjs` → 确认未破坏现有 28 用例
- 现有 `pnpm test:api` 主链不受影响（agent 是新增，不改测试脚本）

---

## 8. 不在本次范围（YAGNI）

- jnpf-code-reviewer（架构审查子 agent）—— 未来单独设计，本次仅 tester + debugger
- jnpf-load-tester / jnpf-contract-tester（k6/artillery/Pact 专用）—— 低频，主 Claude 按需手动调即可
- agent 版本化 / A-B 测试机制
- 将 souls 体系废弃（souls 与 agents 并存，职责不同）

---

## 9. 实施顺序（供 writing-plans 展开）

1. 写 `.claude/agents/jnpf-tester.md`
2. 写 `.claude/agents/jnpf-debugger.md`
3. 新建项目级 `.claude/rules/review-workflow.md`
4. 改 `CLAUDE.md` / `.claude/rules/workflow.md` / `.claude/rules/debugging.md`
5. 补 `mcp__netcoredbg__*` 到 `settings.local.json`（开放项 A）
6. dispatch 冒烟验证 → 存证据
7. `node scripts/test-hooks.mjs` 回归
