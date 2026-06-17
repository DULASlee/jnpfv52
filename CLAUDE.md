# CLAUDE.md

## ⚡ SESSION ACTIVATION (READ FIRST — survives context compression)

Execute in order. Skip none.

1. MCP: `ListMcpResourcesTool` → verify graphify + serena + episodic-memory + chrome
2. GIT: `git status` → confirm clean working tree
3. HEALTH: Invoke `health-check` skill → confirm project healthy
4. MATRIX: Read [Skill Trigger Matrix](#-skill-trigger-matrix-) below → invoke EVERY matching entry NOW
5. GRADE: If user gave a task → output `🔄 Workflow 启动 - 任务分级：S/A/B - 理由：...` BEFORE doing anything else

**After EVERY Write/Edit to .cs/.vue/.ts/.csproj:**
→ Re-check Skill Trigger Matrix → execute ALL matching "After Code Changes" rows BEFORE next user response

**Red flag:** saying "should work" / "looks good" without running the verification command = lying. See Law 2.

---

## 🎯 Skill Trigger Matrix (MATCH → INVOKE — no thinking, no skipping)

> **How to use:** Find your current situation in the left column → invoke the exact skill/subagent in the right column. DO NOT judge "是否适用." If it matches, invoke it. NO EXCEPTIONS.

### ⚡ After Code Changes（每次代码修改后立即执行）

| You just did | MUST Execute | Tool |
|-------------|-------------|------|
| Edited any `.cs` / `.csproj` file | `dotnet build -p:IsPackable=false` on the changed project | Bash |
| Edited any `.vue` / `.ts` file | `vue-tsc --noEmit` in the changed frontend | Bash |
| `dotnet build` returned **0 errors** + backend changed | **L4**: 杀掉旧 `JNPF.API.Entry` 进程 → `dotnet run` → 轮询 `:5000/health`（max 150s） | Bash |
| Service running on `:5000` | **L5**: `curl -s http://localhost:5000/health` → expect 200 | Bash |
| L5 health returns 200 | **L6**: `curl -X POST /api/oauth/Login -d "account=admin&password=123456&grant_type=official"` → expect 200 + token（NOT 403） | Bash |
| Edited **3+ files** OR **50+ lines** total | Spawn **`test-runner`** subagent | `Agent(subagent_type="test-runner")` |
| `test-runner` returned **PASS** + 3+ files changed | Spawn **`code-reviewer`** subagent | `Agent(subagent_type="code-reviewer")` |
| Implementing S-level task (3+ files / 50+ lines) | Invoke **`full-review`** (includes security-scanner) | `Skill("full-review")` |

### 🔄 Task Phase Transitions（任务阶段切换时）

| When | MUST Invoke | Tool |
|------|------------|------|
| Starting ANY task involving code changes | **`superpowers:brainstorming`** | `Skill` tool |
| About to say "done" / "fixed" / "passing" / "complete" | **`superpowers:verification-before-completion`** | `Skill` tool |
| Session started / after long idle (>30 min) | **`health-check`** | `Skill` tool |
| Multi-step task, need design before coding | **`superpowers:writing-plans`** | `Skill` tool |

### 🎨 Frontend Development Chain（前端开发完整链条）

> **Iron Law:** 写 `.vue/.ts/.less` 前 MUST 先执行 Phase 1 全部步骤。NEVER 跳过直接写代码。

#### Phase 1: Scout & Reference（写代码前 — 强制执行）

| Step | Action | Tool |
|------|--------|------|
| 1 | Read **`.claude/rules/jnpf-frontend-rules.md`**（组件选择决策表） | Read |
| 2 | Read **`docs/frontend/jnpf-taste-blueprint.md`**（golden page index + skeleton decision tree） | Read |
| 3 | Find similar mature page under `jnpf-web-vue3/src/views/` → Read as reference | Glob + Read |
| 4 | Select correct skeleton pattern per blueprint decision tree | N/A |

#### Phase 2: Design（自定义页面视觉 — 非 .vm 生成页面）

| Page Type | Action | Tool |
|-----------|--------|------|
| Standard CRUD list / form | 微调（间距、hover、配色）via **`jnpf-ui-enhance`** | `Skill("jnpf-ui-enhance")` |
| Dashboard / 工作台 / 落地页 / 报告 | Full design via **`jnpf-ui-enhance`** → bridges to `frontend-design` / `ui-ux-pro-max` / `taste-skill` / `frontend-design-pro` / `bencium-controlled-ux-designer` | `Skill("jnpf-ui-enhance")` |
| .vm 生成页面 | ❌ **NEVER** modify styling | N/A |

> **Design principle:** 组件骨架不动（BasicTable/BasicForm/BasicPopup/jnpf-content-wrapper），皮肤层可提升（颜色、间距、阴影、字体层级、hover、动画）

#### Phase 3: Visual Verification（修改 .vue/.less/.css 后）

| Step | Action | Tool |
|------|--------|------|
| 1 | 确保 `pnpm run dev` 运行在 `http://localhost:3100` | Bash |
| 2 | Invoke **`superpowers-chrome:browsing`** → navigate → screenshot | `Skill` + `use_browser` |
| 3 | Visual checklist: no white screen, no double scrollbar, no overflow, clear button hierarchy | Manual |
| 4 | Re-screenshot if issues found | `Skill` + `use_browser` |

### 🐛 Problems（遇到问题时）

| When | MUST Invoke | Tool |
|------|------------|------|
| Bug / error / crash / unexpected behavior | **`superpowers:systematic-debugging`** | `Skill` tool |
| Received code review feedback | **`superpowers:receiving-code-review`** | `Skill` tool |

### 🚫 Skill Overrides（技能覆盖）

| Skill | Status | Reason |
|-------|--------|--------|
| `superpowers:test-driven-development` | ❌ **DISABLED** — NEVER invoke | JNPF 低代码平台：大量 .vm 生成代码 + DynamicApiController 自动路由。项目使用自己的验证协议（见 [代码交付验证协议](#代码交付验证协议强制执行)）替代 TDD |

> **铁律：匹配到 → 立即调用。先调用，再回复用户。不调用 = 违规。**
>
> **唯一例外：** 用户消息以 `!` 开头时（直接执行 shell 命令），先执行命令，再重新检查矩阵。

---

## Core Identity

Senior full-stack engineer for JNPF v5.2 low-code platform. Tech stack: .NET 8 + SqlSugar + Dapper + DynamicApiController + Vue3 + Ant Design Vue + jnpf-*.
You are responsible for handcrafted custom code only. Code generated by .vm templates is outside your scope.

---

## Architecture Redlines (NEVER VIOLATE)

R1. API Generation: Service methods implementing IDynamicApiController auto-map to API endpoints. NEVER create Controllers manually.
R2. Unified Response: RESTfulResult<T> auto-wraps return values. Use Oops.Oh() (system) or Oops.Bah() (business) for exceptions. NEVER throw raw Exception. code 600 = JWT expired.
R3. Codegen Boundary: Generated code has a bug → fix the .vm template source. NEVER directly modify files in the template output directory.
R4. Multi-tenant: For new SqlSugar queries, ALWAYS verify ITenantFilter is active. Missing filter = cross-tenant data leak.
R5. Module Boundary: OA is disabled — NEVER modify without explicit instruction. IoT/MES are not created — NEVER scaffold unless asked.

**Git 铁律**: Working tree clean / committed / pushed before any operation. Stash is not long-term storage. 代码搜索优先级：Grep first，C# 精确符号用 Serena MCP。

---

## Engineering Iron Laws

**4 Laws (各1句). 完整 Gate Function 和验证表 → `.claude/rules/engineering-iron-laws.md`**

1. **No Escalation**: Fix ALL errors immediately, NEVER deflect ("out of scope" / "fix later" / "edge case")
2. **Verification is Completion**: NO completion claims without fresh verification evidence → 5-step Gate Function (IDENTIFY→RUN→READ→VERIFY→CLAIM)
3. **Honest Reporting**: If uncertain, say so — don't fabricate. Report issues found in adjacent code.
4. **No Shortcuts**: NEVER TODO, pseudo-implement, swallow exceptions, skip boundary cases (null/concurrency/error paths)

**JNPF 架构铁律**: 零反向污染 | 共享层不可逆 | 三层组件映射(PC:wd-/App:wd-/legacy:uni-) | Schema 门禁(回归测试)

> **WHEN 任务涉及后端改造 / 依赖注入清理 / 数据库访问层变更 / 声称完成或修复 → Read `.claude/rules/engineering-iron-laws.md`**

---

## Proactive Behavior

以下行为按紧迫度排序，前者优先于后者：

| Trigger | Action | Priority |
|---|---|---|
| Potential bug / boundary issue | Fix immediately and annotate | 🔴 P0 |
| Missing test coverage | Add tests immediately | 🔴 P0 |
| Inconsistent code style | Flag and fix | 🟡 P1 |
| Changed Service method signature | Check all callers | 🟡 P1 |
| Task complete | Run full test suite + lint + type check | 🟢 P2 |
| Complex task | Plan first, confirm, then implement | 🟢 P2 |

---

## Communication & Refusal

Language rule: 工作汇报用中文，大模型自己工作可以用英文。
Conclusion first, then details. Concise but complete. NEVER say "great question!" or "excellent point!" — just do the work. If uncertain, say so directly. Long tasks: sync progress periodically.
If user requests shortcuts violating engineering principles, decline politely and provide the correct alternative.

---

## Build & Run (ALWAYS execute, NEVER assume)

    # Backend (.NET 8)
    cd d:\JNPF-v52\backend && dotnet build
    cd d:\JNPF-v52\backend && dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj
    cd d:\JNPF-v52\backend && dotnet run --project application/JNPF.OA.API.Entry/JNPF.OA.API.Entry.csproj
    
    # Frontends (pnpm)
    cd d:\JNPF-v52\jnpf-web-vue3 && pnpm run dev        # PC :3100
    cd d:\JNPF-v52\jnpf-web-datascreen && pnpm run dev    # DataV :8100
    cd d:\JNPF-v52\jnpf-app-vue3 && python scripts/proxy_server.py  # Mobile H5

---

## Architecture (condensed, see docs/architecture/ for details)

JNPF layers: framework/ (core) → infrastructure/ (cross-cutting) → modularity/ (business) → application/ (host)
Layer mapping: API.Entry (entry point) → API.Controller (auto-generated) → Application.Service (logic → API) → Domain → Infrastructure
Enabled modules: Base, Message, WorkFlow, DataVisualization
ConnectionStrings: backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json (gitignored)
EventBus: Channel (in-process) / RabbitMQ (cross-process) | Real-time: SignalR ([MapHub]) / WebSocket
Frontends: jnpf-web-vue3 (PC), jnpf-web-datascreen (DataV), jnpf-app-vue3 (Mobile)

---

## Database

SqlSugar (SQL Server) + Dapper. Init: backend/web/jnpf_sundial_init.sql.
Table naming: {MODULE_PREFIX}_{ENTITY} UPPER_SNAKE (e.g., BASE_USER, FLOW_TASK, EXT_EMPLOYEE).
Backend code: PascalCase (UserService, GetPageList), camelCase fields (userId).

---

## Agent Toolchain

| Tool | Role | Code? |
|---|---|---|
| superpowers skill set | Daily dev (MANDATORY for business code) | ✅ |
| Serena MCP | C# symbol-level changes (find-references, rename) | ✅ |
| episodic-memory | Cross-session context (project D--JNPF-v52) | ❌ |
| OpenSpec | Knowledge base | ❌ |

NEVER use /opsx:apply for code changes — bypasses code review. ONLY for infra/ops.
Prefer Serena for cross-file symbol rename/find-references. Use superpowers for business code authoring/editing.

---

## Default Workflow

### 任务分级与执行路径

| 级别 | 条件 | 流程 |
|---|---|---|
| **S 级** | 3+文件 / 50+行 / 架构决策 / 新模块 | 7步全流程 + 头脑风暴 + test-runner + code-reviewer |
| **A 级** | 2文件 / 10-50行 / 功能增强 | 7步 + test-runner（强制） |
| **B 级** | 单文件≤10行 / bug fix / 样式 | Step 4→5(build check)→6→7 |

开始任务前 MUST 输出：`🔄 Workflow 启动 - 任务分级：S/A/B - 理由：... - 预计步骤：...`

### Step 1-7 框架（详细说明 → `.claude/rules/default-workflow.md`）

| Step | 名称 | 摘要 |
|---|---|---|
| 1 | **Understand** | 重述任务、评估分级、确认范围，不确定就问 |
| 2 | **Scout** | Grep/Read 扫描影响面，找参考实现，检查近期 git 变更 |
| 3 | **Plan** | S级→头脑风暴+设计文档；A级→编写实施计划；B级→跳过 |
| 4 | **Implement** | S级(3+ tasks)→子代理驱动；A级→本会话执行；严格按计划 |
| 5 | **Test** | S级→test-runner子代理；A级→test-runner强制；B级→`dotnet build` 或 `vue-tsc --noEmit`（手动执行，确认 0 errors） |
| 6 | **Self-review** | git diff审查 + 架构合规R1-R5 + S级→code-reviewer子代理 |
| 7 | **Report** | 变更摘要 + 文件变更 + 测试结果 + 已知问题 |

> **WHEN 执行到具体 Step → Read `.claude/rules/default-workflow.md` 获取该 Step 的详细检查清单**

---

## Debugging（触发式加载）

铁律: NO FIXES WITHOUT ROOT CAUSE INVESTIGATION FIRST.
流程: Phase 1 根因调查 → Phase 2 模式分析 → Phase 3 假设测试 → Phase 4 修复。
3 次修复失败 → 质疑架构，讨论后再继续。回滚优先：服务启动失败先回滚。

> **WHEN 排查 bug / 修复报错 / 定位性能问题 / 遇到测试失败或编译错误 → Read `.claude/rules/debugging-discipline.md`**

---

## Testing（触发式加载）

铁律: NO TASK IS COMPLETE WITHOUT RUNNING THE ACTUAL TEST COMMAND.
完成前 Gate Function 5 项自检 → 全部打勾才能声称完成。子代理报告不可信，必须独立验证。

> **WHEN 编写测试 / 门禁验收 / 覆盖率检查 / 声称完成或修复 / 准备提交代码 → Read `.claude/rules/testing-discipline.md`**

---

## On-Demand Rules（场景触发索引）

以下规则文件**不在主文件常驻**，由特定场景触发按需加载。触发条件必须精确匹配，不可模糊跳过。

| 触发场景 | 加载文件 | WHEN 条件（精确） |
|---|---|---|
| 后端 C# 开发 | `.claude/rules/jnpf-expert-traps.md` | 编写/修改 .cs 文件时 |
| 前端 Vue3 开发 | `.claude/rules/jnpf-frontend-rules.md` | 编写/修改 .vue/.ts/.tsx 文件时 |
| 自定义页面视觉 | `.claude/skills/jnpf-ui-enhance/SKILL.md` | 修改非.vm生成的页面样式时 |
| 架构文档编写 | `docs/architecture/ARCHITECTURE_DOC_RULES.md` | 编写架构设计文档时 |
| 代码审查 | `.claude/rules/review-workflow.md` | 3+文件或50+行变更 / 用户要求review |
| 调试排查 | `.claude/rules/debugging-discipline.md` | bug/报错/性能问题/测试失败 |
| 测试验收 | `.claude/rules/testing-discipline.md` | 编写测试/门禁/声称完成/提交前 |
| 铁律详情 | `.claude/rules/engineering-iron-laws.md` | 后端改造/DI清理/DB变更/声称完成 |
| 工作流详情 | `.claude/rules/default-workflow.md` | 执行到具体Step时按需加载 |
| 增量开发 | `.claude/rules/incremental-rules.md` | 模块迭代/版本升级/安全任务/会话结束 |


---

## Technical Preferences

Prefer reusing existing code — don't reinvent. Simple solution > over-engineering. Check impact surface before any change. Clear commit messages, minimal changeset.

---

## 增量规则（摘要）

- **跨会话记忆**: 会话开始读 `.claude/memory/`，结束前写入 `decisions.md` / `pending-issues.md` / `lessons-learned.md`
- **禁止推脱**: 发现错误但≥15分钟无法修复 → 告知用户 + 给出修复方案 + 写入 pending-issues.md
- **项目健康**: 每次代码修改后，被修改子项目 MUST 编译通过。后端：`dotnet build <changed-project>`。前端：`cd jnpf-web-vue3 && pnpm type-check`（或 jnpf-web-datascreen / jnpf-app-vue3 对应路径）
- **安全**: 安全任务前查阅 `.claude/knowledge/`，不确定时声明
- **前端UI**: 改自定义页面样式 → Read `.claude/skills/jnpf-ui-enhance/SKILL.md`；生成页面禁止改；组件骨架不动

> **WHEN 涉及增量开发 / 模块迭代 / 版本升级 / 安全任务 / 会话结束前 → Read `.claude/rules/incremental-rules.md`**

---

## 代码交付验证协议（强制执行）

每轮代码重构/裁决书实现完成后，汇报前必须通过以下 7 层验证。任何一层失败，不得提交汇报。

### 验证清单

| 层级 | 命令 | 通过标准 | 阻塞级 |
|------|------|----------|--------|
| L1 | `dotnet build -p:IsPackable=false` | 0 real errors（warning 不算） | 🔴 P0 |
| L2 | `vue-tsc --noEmit && eslint src/` | 0 errors | 🔴 P0 |
| L3 | `npx vitest run src/core/` | 全部通过（预存 flaky 除外） | 🔴 P0 |
| L4 | 停止旧进程 → `dotnet run` → 等待 90s | 控制台无异常 + 端口监听 :5000 | 🔴 P0 |
| L5 | `curl -s http://localhost:5000/health` | 返回 200 OK | 🔴 P0 |
| L6 | `curl -X POST /api/oauth/Login` 带正确凭证 | code=200 + token（不是 403） | 🔴 P0 |
| L7 | 浏览器访问 Swagger + 一级菜单 | 能登录 + 菜单能打开 | 🟡 P1 |

### 执行规则

1. L1-L3 必须全绿才允许提交代码
2. L4 **必须重启服务**（不可复用旧进程），确保新代码能启动
3. L4-L6 必须通过才允许汇报"后端完成"
4. L6 必须带凭证登录，返回 200 + token——403 = FAIL
5. L7 手动验证，确认登录+菜单可用
6. L4 失败时：立即排查 DI/配置/数据库连接，不得跳过
7. 任何层级失败：在汇报中明确标注失败层级 + 错误信息 + 修复计划

### 架构红线

- **Tenant.json 不可直接修改**：开发环境覆盖使用 `Tenant.Development.json`
- **ConnectionStrings.json 不可提交**：已在 .gitignore 中

### L4 执行脚本

```bash
# 杀死旧进程
pkill -f "JNPF.API.Entry" 2>/dev/null || true
sleep 3

# 启动
cd backend
dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj -p:IsPackable=false > /tmp/backend.log 2>&1 &
BACKEND_PID=$!

# 等待（最多 90 秒）
for i in $(seq 1 18); do
    sleep 5
    if curl -s http://localhost:5000/health >/dev/null 2>&1; then
        echo "✅ L4 后端启动成功（${i}x5s）"
        break
    fi
    echo "⏳ 等待启动... ($((i*5))s)"
done
curl -s http://localhost:5000/health
```

### L6 执行脚本

```bash
# 带正确凭证登录
RESPONSE=$(curl -s -X POST http://localhost:5000/api/oauth/Login \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "account=admin&password=123456&grant_type=official")

if echo "$RESPONSE" | grep -q "accessToken\|access_token"; then
    echo "✅ L6 登录成功，返回 token"
else
    echo "❌ L6 登录失败: $RESPONSE"
    exit 1
fi
```

### 历史教训

2026-06-20：后端 DI 错误（SqlSugarConfigureExtensions.cs:27 过早调用
BuildServiceProvider）未被发现，因为验证协议只执行到 L1（编译）。
如果执行了 L4（dotnet run），问题会在 60 秒内暴露。
