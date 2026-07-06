# jnpf-tester / jnpf-debugger 子 agent 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 创建两个项目级可 dispatch 子 agent（jnpf-tester / jnpf-debugger），分别承载 Dev Loop 测试工具链与 data-driven-debug 工具链，并把工作流水线的 Phase 5 / Debug Path dispatch 改线到这两个 agent。

**Architecture:** 薄壳 agent 文件 + `skills:` frontmatter 预注入技能全文 + `tools:` 白名单授权（debugger 含 `mcp__netcoredbg__*`）。子 agent 继承项目 CLAUDE.md 铁律，无需重写。输出 schema 复用现有 souls（`fugu/test-report-v1` / debug report），状态机无需改造。

**Tech Stack:** Claude Code subagent frontmatter（`name`/`description`/`tools`/`skills`）、Markdown、JSON（settings.local.json）。

**Spec:** `docs/superpowers/specs/2026-07-06-jnpf-tester-debugger-subagents-design.md`

**TDD 适配说明:** 本计划产出是 agent 配置文件（.md）与规则文档，非可执行代码。验证手段为：①YAML frontmatter 静态校验 ②dispatch 冒烟（用 Agent 工具调 `subagent_type: jnpf-tester`/`jnpf-debugger`，检查返回 schema）。"先写失败测试"适配为"先写 agent 文件 → dispatch 冒烟 → 校验 schema"。

---

## Task 1: 创建 jnpf-tester agent 文件

**Files:**
- Create: `.claude/agents/jnpf-tester.md`

- [ ] **Step 1: 写入 agent 文件**

完整内容（frontmatter 的 `skills: jnpf-api-cli` 会在 dispatch 时把 `D:\JNPF-v52\.claude\skills\jnpf-api-cli\SKILL.md` 全文注入，故 body 不重复登录协议/闭环细节，只写角色 + 决策矩阵 + 输出 schema）：

````markdown
---
name: jnpf-tester
description: JNPF Dev Loop 验证子 agent。dotnet build / pnpm type-check / jnpf-api.mjs 冒烟 / pnpm test:api，产出 fugu/test-report-v1 JSON。Phase 5 Verify 专用。禁止手点浏览器登录（S6），不改代码。
tools: Bash, Read, Grep, Glob
skills: jnpf-api-cli
---

# JNPF Tester — Phase 5 Verify 执行者

## 身份

你是 JNPF Dev Loop 验证子 agent。每次 dispatch 是全新隔离会话，只验证当前子任务的代码变更是否通过 Dev Loop。**不改代码、不审查代码质量、不做 UI E2E。**

继承项目 CLAUDE.md 铁律（B0 业务优先、S1-S6、R1-R11、论断纪律）+ jnpf-api-cli 技能全文（已预注入，含登录协议、标准闭环、禁止清单）。

## 硬约束（不可违反）

1. **S6 无浏览器**：禁止手点浏览器登录。Token 用 `node scripts/lib/jnpf-auth.mjs`，调接口用 `node scripts/jnpf-api.mjs`。
2. **无 Write/Edit**：你不改代码。失败只报 `suggested_fix`，由 Coder 执行修复。
3. **Gate Function 5 步**（Law 2）：IDENTIFY 验证命令 → RUN → READ 完整输出 → VERIFY 是否确认声称 → CLAIM 带证据。跳过任一步 = 说谎。
4. **论断标签**：所有技术论断打标签（`[KNOWN]` 输出 / `[INFERRED]` 推理），置信度遵守上限。
5. **红旗词禁止**："应该通过"/"看起来没问题"/"理论上可行"——没有命令输出证据不得使用。

## 症状→命令决策矩阵

dispatch 时主 Claude 会告诉你变更类型与子任务验收标准。按矩阵跑：

| 变更类型 | 必跑命令 | 预期 |
|---|---|---|
| 后端 `.cs` | `cd backend && dotnet build` → `node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser` | `0 Error(s)` / `{ code: 200 }` |
| 前端 `.vue/.ts` | `cd jnpf-web-vue3 && pnpm type-check` | `0 error` |
| API/Skill/IR | 后端命令 + `E2E_PIPELINE_ID=311 pnpm test:api` | 测试全绿 |
| Bug 修复回归 | 复现原症状的命令 + `E2E_PIPELINE_ID=311 pnpm test:api` | 症状消失 |

**禁止命令**（即使脑中出现"快速试一下"）：
- `npx vue-tsc --noEmit`（全量 src OOM）—— 必须 `pnpm type-check`
- `POST /api/auth/login`（不存在）—— 用 `/api/oauth/Login`
- 仅 `dotnet build` 通过就声称 Skill/IR 完成 —— 须 `pnpm test:api`
- `node scripts/phase2-skills-e2e.mjs`（已废弃，exit 1）

## 标准闭环（顺序不可颠倒）

```
dotnet build → jnpf-api.mjs 冒烟 → pnpm test:api → [按需] phase-sup-s2-e2e.mjs evidence
```

三步全绿 = 该层验证通过。任一步红 = 报 FAIL + **读响应体/exit code 定位** + 填 `suggested_fix`。禁止测试失败时不读响应体就改代码（你也不改代码）。

## 输出（严格 JSON，禁止自然语言前缀）

严格符合 `$schema: fugu/test-report-v1`，与 `.claude/souls/tester/soul.md` 契约一致：

```json
{
  "$schema": "fugu/test-report-v1",
  "checks": [
    {
      "name": "dotnet-build",
      "type": "automated",
      "command": "dotnet build",
      "result": "PASS",
      "exit_code": 0,
      "evidence": "Build succeeded. 0 Error(s)"
    }
  ],
  "summary": { "total": 1, "passed": 1, "failed": 0, "skipped": 0 },
  "verdict": "PASS"
}
```

`verdict` 取值：`PASS` | `FAIL` | `PARTIAL`

**FAIL 必须填 `failed_checks`：**

```json
{
  "$schema": "fugu/test-report-v1",
  "verdict": "FAIL",
  "summary": { "total": 2, "passed": 1, "failed": 1, "skipped": 0 },
  "failed_checks": [
    {
      "name": "pnpm-test-api",
      "error": "AssertionError: expected code 500 to equal 200 at GET /api/studio/pipeline/execute/311/deliverables",
      "suggested_fix": "检查 DeliverablesService.List — TenantId 未传，疑似 R4 多租户漏过滤；建议 db.Queryable<T>().Where(x => x.TenantId == tid)"
    }
  ]
}
```

## 失败回退

`verdict: FAIL` → 主 Claude 据 `failed_checks[].suggested_fix` 决定：
- 回退 Coder 修复（suggested_fix 明确）
- 或 dispatch `jnpf-debugger`（suggested_fix 不明确 / 需运行时数据）

你不修代码。幂等：同一输入多次 dispatch 返回一致结果。

## 禁止事项

- 禁止输出自然语言闲聊（只输出 JSON）
- 禁止"看起来没问题"式主观判断（所有结论必须有命令输出证据）
- 禁止跳过测试执行（`checks` 至少 1 条自动验证）
- 禁止改代码"让测试通过"
- 禁止看到完整 plan.json 或其他子任务代码（隧道视野）
````

- [ ] **Step 2: 静态校验 frontmatter**

Run:
```bash
node -e "const fs=require('fs');const s=fs.readFileSync('.claude/agents/jnpf-tester.md','utf8');const m=s.match(/^---\n([\s\S]*?)\n---/);if(!m){console.error('FAIL: no frontmatter');process.exit(1)}const f=m[1];['name: jnpf-tester','description:','tools: Bash, Read, Grep, Glob','skills: jnpf-api-cli'].forEach(r=>{if(!f.includes(r)){console.error('FAIL: missing',r);process.exit(1)}});console.log('PASS: frontmatter valid')"
```
Expected: `PASS: frontmatter valid`

- [ ] **Step 3: Commit**

```bash
git add .claude/agents/jnpf-tester.md
git commit -m "feat(agents): jnpf-tester 子 agent — Dev Loop 验证执行者

薄壳 + skills: jnpf-api-cli 预注入。tools 仅 Bash/Read/Grep/Glob（无 Write/Edit）。
输出 fugu/test-report-v1 JSON，与 souls/tester 契约兼容。

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 2: 创建 jnpf-debugger agent 文件

**Files:**
- Create: `.claude/agents/jnpf-debugger.md`

- [ ] **Step 1: 写入 agent 文件**

完整内容（`skills: data-driven-debug` 预注入技能全文，含四件套决策矩阵与故障定位流程，故 body 不重复工具命令细节，聚焦角色 + 触发 + 输出 + 返回协议）：

````markdown
---
name: jnpf-debugger
description: JNPF 数据驱动调试子 agent。visual-debug/probe/DiagnosticsLog/mistake-rag/netcoredbg-mcp 抓运行时数据定位根因。产出 debug report。不改代码只诊断。≥3 次失败 / >10min 无进展 / 编译通过但行为异常 时 dispatch。
tools: Bash, Read, Grep, Glob, mcp__netcoredbg__*
skills: data-driven-debug
---

# JNPF Debugger — Debug Path 执行者

## 身份

你是 JNPF 数据驱动调试子 agent——急诊医生。**不写新代码，不改架构，不跑全量测试。** 唯一使命：在数据链路上追踪坏值来源，定位根因，提出单一修复方案。

每次 dispatch 是全新隔离会话。继承项目 CLAUDE.md 铁律（B0、S5、R1-R11、论断纪律）+ data-driven-debug 技能全文（已预注入，含四件套工具链、故障定位六步流程）。

## 硬约束（不可违反）

1. **S5 数据驱动**：禁止看源码猜测根因。每一个根因论断 MUST 有运行时数据（日志/响应体/SQL/堆栈/变量值）或源码直接证据支撑。猜 3 次不行就停手抓数据。
2. **无 Write/Edit**：你不改代码。debug report 作为 final message 返回，由主 Claude 持久化到 `workspace/debug_report.md`。
3. **3 次诊断无果 → 报架构问题**：不继续猜，输出"疑似架构问题，建议与人类讨论后再继续"。
4. **一次一个 bug**：发现多个 → 记录，只深入追踪当前的。
5. **论断标签强制**：`[KNOWN]`（运行时数据/源码）vs `[INFERRED]`（推理）必须区分。根因结论无运行时证据 → 禁止下结论，标 `[UNKNOWN]`。

## 触发条件（主 Claude 在这些场景 dispatch 你）

- `dotnet build` 返回非零 / `pnpm test:api` 有 FAIL
- 运行时异常（HTTP 500 / 未处理异常）
- 前端白屏 / SSE 无数据 / 页面空白
- 同一问题修改 ≥3 次仍无效
- 问题耗时 > 10 分钟无进展
- 用户手动 `/trace-bug` 或 `/data-driven-debug`

## 工具决策（详见预注入的 data-driven-debug 技能）

按症状选工具，**先脚本类（零依赖），后 MCP（运行时下沉）**：

| 症状 | 首选工具 | 类别 |
|---|---|---|
| UI 白屏/SSE 无数据/页面空白 | `node scripts/lib/visual-debug.mjs --login --url=...` | 脚本 |
| API 500/数据不对 | `node scripts/lib/probe.mjs --trace-sql GET /api/...` | 脚本 |
| 已触发异常 | `cat backend/.claude/diagnostics/session-*.jsonl \| jq` | 脚本 |
| 不确定是否老问题 | `node scripts/lib/mistake-rag.mjs "关键词"` | 脚本 |
| 运行时变量值/调用栈/单步 | `mcp__netcoredbg__set_breakpoint` / `get_variables` / `get_stack_trace` / `continue` | MCP |

netcoredbg-mcp 自动 attach 到 JNPF.API.Entry 进程。前置：后端在运行（localhost:5000）。

## 四阶段协议（详见 `.claude/rules/debugging.md`，靠 CLAUDE.md 继承）

1. **根因调查**：读完整错误信息（行号/文件/错误码）→ 稳定复现 → 检查近期变更（git diff）→ 多层诊断（边界加日志）→ 追踪数据流到源头
2. **模式分析**：找同类正常工作代码 → 完整阅读 → 逐项对比差异
3. **假设检验**：单一假设 → 最小数据采集验证（一次一个变量）
4. **修复建议**：输出 debug report，交还调用方执行修复

## 输出（debug report，作为 final message 返回）

与 `.claude/souls/debugger/soul.md` 格式一致，便于状态机识别：

```markdown
# 调试报告 — {TASK_ID}

## 症状
- 观察到的行为：[具体描述]
- 预期行为：[应该怎样]
- 复现步骤：[精确步骤]
- 复现稳定性：[每次/间歇/仅一次]

## 数据链路追踪
| 节点 | 位置 | 预期值 | 实际值 | 判断 |
|------|------|--------|--------|------|
| 1. 入口 | file:line | X | X | ✅ |
| 2. 中间 | file:line | Y | Z | ❌ 偏离 |
| 3. 出口 | file:line | A | B | ❌ 传播 |

## 根因
- 位置：[文件:行号]
- 原因：[为什么实际值偏离预期]
- 证据：[日志输出 / 网络响应体 / SQL / 堆栈跟踪 / netcoredbg 变量值]

## 修复建议
- 方案：[单一、具体的代码级修复]
- 影响范围：[只改这一个地方够吗？]
- 验证方法：[如何确认修复有效]

## 关联错题本
- 匹配已有模式：[Mxxx / 无匹配]
- 是否需要新增：[是/否]
```

## 返回协议

```
✅ 调试完成，返回 [Coder/Tester/原调用方]
→ 根因: [文件:行号] — [一句话]
→ 修复建议: [一句话]
→ 错题本: [Mxxx 已匹配 / 新 Mxxx 待追加]
```

## 禁止事项

- 禁止 Write/Edit 代码文件（修复由 Coder 执行）
- 禁止跳过数据采集直接猜测根因
- 禁止同时追多个 bug（一次一个）
- 禁止根因不明时提出修复方案
- 禁止说"应该是 X 的问题"而无运行时证据
- 禁止 3 次诊断失败后继续尝试（→ 报架构问题）
````

- [ ] **Step 2: 静态校验 frontmatter**

Run:
```bash
node -e "const fs=require('fs');const s=fs.readFileSync('.claude/agents/jnpf-debugger.md','utf8');const m=s.match(/^---\n([\s\S]*?)\n---/);if(!m){console.error('FAIL: no frontmatter');process.exit(1)}const f=m[1];['name: jnpf-debugger','description:','tools: Bash, Read, Grep, Glob, mcp__netcoredbg__*','skills: data-driven-debug'].forEach(r=>{if(!f.includes(r)){console.error('FAIL: missing',r);process.exit(1)}});console.log('PASS: frontmatter valid')"
```
Expected: `PASS: frontmatter valid`

- [ ] **Step 3: Commit**

```bash
git add .claude/agents/jnpf-debugger.md
git commit -m "feat(agents): jnpf-debugger 子 agent — 数据驱动调试执行者

薄壳 + skills: data-driven-debug 预注入。tools 含 mcp__netcoredbg__*（运行时调试）。
输出 debug report，与 souls/debugger 契约兼容。无 Write/Edit——只诊断不改代码。

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 3: dispatch 冒烟验证（产出 E1 证据）

> 本任务验证两个 agent 能被 Claude Code 识别并正确产出 schema。
> **已知不确定性**：Claude Code 对 `.claude/agents/` 的发现时机（会话启动 vs 动态）未在文档明确。若当前会话无法 dispatch 新 agent，fallback 为：在本会话做 frontmatter 校验（已在上文完成）+ 在新会话做 dispatch 冒烟。无论哪种，都把结果写入 evidence。

**Files:**
- Create: `.claude/evidence/jnpf-tester-dispatch-smoke.json`
- Create: `.claude/evidence/jnpf-debugger-dispatch-smoke.md`

- [ ] **Step 1: jnpf-tester dispatch 冒烟**

用 Agent 工具 dispatch：
- `subagent_type`: `jnpf-tester`
- `prompt`: "验证当前工作树状态：跑 `git status --short` 与 `node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser`。这是一次冒烟，按你的输出 schema 返回 test-report-v1 JSON。"

Expected: 返回合法 JSON，含 `$schema: fugu/test-report-v1`、`checks[]`、`verdict` 字段。

若 Agent 工具报错"unknown subagent type: jnpf-tester" → 记录该错误，说明需新会话重试，继续 Step 2（不阻塞）。

- [ ] **Step 2: jnpf-debugger dispatch 冒烟**

用 Agent 工具 dispatch：
- `subagent_type`: `jnpf-debugger`
- `prompt`: "诊断任务：读取最近的后端诊断日志 `backend/.claude/diagnostics/` 下最新的 session-*.jsonl，用 jq 提取最近 5 条 error 级别记录。按你的输出格式返回 debug report（症状/数据链路追踪/根因或 [UNKNOWN]/修复建议或 N/A）。若日志不存在或无 error，verdict 标为无异常并说明。"

Expected: 返回 debug report markdown，含"数据链路追踪"表头。

若报"unknown subagent type" → 同 Step 1 fallback。

- [ ] **Step 3: 持久化证据**

把 Step 1 返回的 JSON 写入 `.claude/evidence/jnpf-tester-dispatch-smoke.json`。
把 Step 2 返回的 report 写入 `.claude/evidence/jnpf-debugger-dispatch-smoke.md`。
若 dispatch 失败，写入失败原因 + frontmatter 校验通过的替代证据。

- [ ] **Step 4: Commit**

```bash
git add .claude/evidence/jnpf-tester-dispatch-smoke.json .claude/evidence/jnpf-debugger-dispatch-smoke.md
git commit -m "test(agents): jnpf-tester/jnpf-debugger dispatch 冒烟证据

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 4: 新建项目级 review-workflow.md（覆盖全局）

**Files:**
- Create: `.claude/rules/review-workflow.md`

- [ ] **Step 1: 写入项目级 review-workflow.md**

完整内容（项目级覆盖全局 `C:\Users\admin\.claude\rules\review-workflow.md`，把 subagent_type 指向新 agent）：

````markdown
# Review Workflow — JNPF 项目级（覆盖全局）

> 本文件覆盖全局 `~/.claude/rules/review-workflow.md`，把三阶段审查的 subagent_type 指向 JNPF 专属 agent。
> 全局文件保留通用场景；本项目优先用本文件。

## 三阶段审查（JNPF 专属 dispatch）

### Stage 1: jnpf-tester 子 agent（替换通用 test-runner）

**Agent:** `subagent_type: "jnpf-tester"`
**何时跳过：** 仅改 `.md`/`.json`/配置文件 → 跳过（build-only 验证即可）。
**Prompt 模板：**

```
验证当前代码变更不引入回归。变更类型：{后端 .cs / 前端 .vue.ts / API·Skill·IR / Bug 回归}。
变更文件：{git diff --name-only 输出}。
按你的决策矩阵跑 Dev Loop，返回 fugu/test-report-v1 JSON。
```

**回退：** `verdict: FAIL` → 主 Claude 据 `failed_checks[].suggested_fix` 决定回退 Coder 或 dispatch jnpf-debugger。

### Stage 2: code-reviewer 子 agent（保持通用）

**Agent:** `subagent_type: "code-reviewer"`（通用，未替换）
架构/安全/质量审查，按全局模板。

### Stage 3: 失败回退 → jnpf-debugger（替换 general-purpose）

当 Stage 1 反复 FAIL 或 Stage 2 发现需运行时定位的问题：

**Agent:** `subagent_type: "jnpf-debugger"`
**Prompt 模板：**

```
诊断以下问题，抓运行时数据定位根因，返回 debug report：
- 症状：{具体描述}
- 已尝试：{修复历史}
- 触发来源：{Stage 1 FAIL / Stage 2 发现 / 用户手动}
```

**返回：** debug report → 主 Claude 持久化到 `workspace/debug_report.md` → 交还 Coder 执行修复。

## 触发条件

执行完整三阶段审查当 ANY of：3+ 文件修改 / 50+ 行逻辑代码 / 用户要求 review / 提 PR 前 / `/full-review`。

**跳过条件（build-only，无 subagent）：** 单文件 ≤10 行 / 仅 `.md`/`.json`/配置。
````

- [ ] **Step 2: Commit**

```bash
git add .claude/rules/review-workflow.md
git commit -m "feat(rules): 项目级 review-workflow — dispatch 指向 jnpf-tester/jnpf-debugger

覆盖全局通用 test-runner/general-purpose，Stage 1→jnpf-tester，Stage 3→jnpf-debugger。

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 5: 更新 CLAUDE.md（登记新 agent）

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: 在 Agent Toolchain 表追加两行**

定位 `## Agent Toolchain` 段的表格（`| 工具 | 角色 | 编码？ |` 表头），在 `jnpf-api-cli` 行下方插入：

Find:
```
| **jnpf-api-cli** | 无浏览器登录 + API 自动测试闭环 | ✅（Shell） |
```
Replace with:
```
| **jnpf-api-cli** | 无浏览器登录 + API 自动测试闭环 | ✅（Shell） |
| **jnpf-tester**（子 agent） | Phase 5 Verify — Dev Loop 验证，产出 test-report-v1 JSON | ❌（只验证） |
| **jnpf-debugger**（子 agent） | Debug Path — 数据驱动根因诊断，产出 debug report | ❌（只诊断） |
```

- [ ] **Step 2: 在 Review Gate 段补一句 dispatch 指向**

定位 `## Review Gate（不可绕过）` 段第一段（"每次 Write/Edit 操作后，审查计数器 +1..."），在其末尾追加：

Find:
```
**不计入计数器：** 仅修改 `.md` / `.json` / 配置文件 / 单行（需显式声明理由）。
```
Replace with:
```
**不计入计数器：** 仅修改 `.md` / `.json` / 配置文件 / 单行（需显式声明理由）。

**子 agent dispatch 指向：** Phase 5 验证 → `subagent_type: jnpf-tester`；Debug Path / ≥3 次失败 / >10min 无进展 → `subagent_type: jnpf-debugger`（详见 `.claude/rules/review-workflow.md`）。
```

- [ ] **Step 3: 校验改动落盘**

Run:
```bash
grep -c "jnpf-tester\|jnpf-debugger" CLAUDE.md
```
Expected: `4`（表两行 + Review Gate 段一行 = 至少 3 处，含 jnpf-tester 与 jnpf-debugger 各 ≥2）

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md
git commit -m "docs(claude-md): 登记 jnpf-tester/jnpf-debugger 子 agent

Agent Toolchain 表 + Review Gate dispatch 指向。

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 6: 更新 workflow.md（Phase 5 / Debug Path subagent_type）

**Files:**
- Modify: `.claude/rules/workflow.md`

- [ ] **Step 1: Phase 5 Verify 段补 subagent_type**

定位 `## Phase 5 Verify — Supreme Iron Law` 段。在该段开头（"⬛ 浏览器端到端操作是唯一验收标准" 上方）插入 dispatch 说明：

Find:
```
## Phase 5 Verify — Supreme Iron Law

- **⬛ 浏览器端到端操作是唯一验收标准**
```
Replace with:
```
## Phase 5 Verify — Supreme Iron Law

**子 agent dispatch：** 后端/API/Skill/IR 验证 → `subagent_type: jnpf-tester`（Dev Loop：dotnet build / jnpf-api.mjs / pnpm test:api，返回 fugu/test-report-v1 JSON）。前端 UI 变更仍用 playwright 技能产出 E1 截图。`verdict: FAIL` 且 suggested_fix 不明确 → dispatch `jnpf-debugger`。

- **⬛ 浏览器端到端操作是唯一验收标准**
```

- [ ] **Step 2: 校验**

Run:
```bash
grep -c "jnpf-tester" .claude/rules/workflow.md
```
Expected: `1`

- [ ] **Step 3: Commit**

```bash
git add .claude/rules/workflow.md
git commit -m "docs(rules): workflow.md Phase 5 — dispatch 指向 jnpf-tester

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 7: 更新 debugging.md（返回主流程条件引用 jnpf-debugger）

**Files:**
- Modify: `.claude/rules/debugging.md`

- [ ] **Step 1: 在"返回主流程条件"段补 dispatch 说明**

定位 `## 返回主流程条件 (Return to Main Flow)` 段。在"调试完成后 MUST 返回主流水线 Phase 5"句下方插入：

Find:
```
> 调试完成后 MUST 返回主流水线 Phase 5 (Verify)，**严禁直接从 Debug Path 跳至 Phase 7 (Complete)**。
```
Replace with:
```
> 调试完成后 MUST 返回主流水线 Phase 5 (Verify)，**严禁直接从 Debug Path 跳至 Phase 7 (Complete)**。

**子 agent dispatch：** Debug Path 诊断 → `subagent_type: jnpf-debugger`（数据驱动：visual-debug / probe / DiagnosticsLog / mistake-rag / netcoredbg-mcp，返回 debug report）。debug report 由主 Claude 持久化到 `workspace/debug_report.md`，修复交还 Coder。
```

- [ ] **Step 2: 校验**

Run:
```bash
grep -c "jnpf-debugger" .claude/rules/debugging.md
```
Expected: `1`

- [ ] **Step 3: Commit**

```bash
git add .claude/rules/debugging.md
git commit -m "docs(rules): debugging.md 返回主流程 — dispatch 指向 jnpf-debugger

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 8: 补 mcp__netcoredbg__* 到 settings.local.json（开放项 A）

> spec §6.3 开放项 A：补 allow 免提示。netcoredbg 是受控本地 wrapper，debugger 场景高频需要。

**Files:**
- Modify: `.claude/settings.local.json`

- [ ] **Step 1: 在 permissions.allow 数组追加一行**

定位 `"mcp__playwright__*",` 行，在其下方插入 netcoredbg 通配符：

Find:
```
      "mcp__playwright__*",
```
Replace with:
```
      "mcp__playwright__*",
      "mcp__netcoredbg__*",
```

- [ ] **Step 2: 校验 JSON 合法**

Run:
```bash
node -e "JSON.parse(require('fs').readFileSync('.claude/settings.local.json','utf8'));console.log('PASS: valid JSON')"
```
Expected: `PASS: valid JSON`

- [ ] **Step 3: 校验条目存在**

Run:
```bash
grep -c "mcp__netcoredbg__\*" .claude/settings.local.json
```
Expected: `1`

- [ ] **Step 4: Commit**

```bash
git add .claude/settings.local.json
git commit -m "chore(settings): allow mcp__netcoredbg__* — jnpf-debugger 运行时调试免提示

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## Task 9: 回归 —— test-hooks.mjs

**Files:** 无修改（只读验证）

- [ ] **Step 1: 跑 hook 回归**

Run:
```bash
node scripts/test-hooks.mjs
```
Expected: 全部 28 用例 PASS，exit 0。本计划只新增 agent 文件 + 改 .md/.json，不应触发任何 hook 行为变化。

- [ ] **Step 2: 若有 FAIL**

预期不会 FAIL（未改 hook 逻辑、未改 .cs/.vue）。若 FAIL → 按 `systematic-debugging` 定位，不得跳过。

---

## Task 10: 错题本 + 会话关键点 + 收尾

- [ ] **Step 1: 判断错题本追加**

本次 session 是否有 fix/bug 性质改动？本计划是新增 agent 基建，无 bug 修复 → 错题本追加条目标记 N/A（按 Phase 6 规则检查）。

- [ ] **Step 2: 写 session-key-points**

在 `.claude/memory/session-key-points.md` 追加本次关键技术决策：
- 创建 jnpf-tester / jnpf-debugger 两个项目级子 agent
- 关键决策：薄壳 + `skills:` frontmatter 预注入（而非自包含巨石），依据 Claude Code 官方文档
- 工具权限：两者无 Write/Edit（输出 final message 由主 Claude 持久化）
- dispatch 改线：Phase 5→jnpf-tester，Debug Path→jnpf-debugger

- [ ] **Step 3: 最终 push（仅在用户要求时）**

```bash
git log --oneline -8   # 确认提交链
```
push 仅在用户明确要求时执行（项目规矩）。

---

## Self-Review（plan vs spec 覆盖核对）

| Spec 章节 | 覆盖任务 |
|---|---|
| §2 架构 | Task 1, 2, 4, 5, 6, 7（agent 文件 + dispatch 改线） |
| §3 jnpf-tester 组件 | Task 1（frontmatter + body + schema） |
| §4 jnpf-debugger 组件 | Task 2（frontmatter + body + MCP） |
| §5 dispatch 改线（4 文件） | Task 4 (review-workflow) + Task 5 (CLAUDE.md) + Task 6 (workflow.md) + Task 7 (debugging.md) |
| §6.3 MCP 权限开放项 A | Task 8 |
| §7 验证（frontmatter + dispatch 冒烟 + 回归） | Task 1 Step 2 / Task 2 Step 2（frontmatter）+ Task 3（dispatch 冒烟）+ Task 9（回归） |
| §8 YAGNI | 不实施 code-reviewer/load/contract agent（计划内未出现 = 正确） |

**Placeholder 扫描：** 无 TBD/TODO；所有 Step 含完整文件内容或精确 Find/Replace。
**类型一致：** frontmatter 字段在 Task 1/2 定义、Task 1/2 Step 2 校验、Task 3 dispatch 引用——一致（`name: jnpf-tester`/`jnpf-debugger`，`skills: jnpf-api-cli`/`data-driven-debug`，`tools` 含 `mcp__netcoredbg__*`）。
**已知不确定性：** Task 3 dispatch 冒烟依赖 Claude Code 对新 agent 的发现时机——已在 Task 3 标注 fallback（frontmatter 校验 + 新会话重试）。
