---
name: data-driven-debug
description: 数据驱动调试——禁止看源码猜测，必须抓取运行时数据定位问题。当问题耗时超过 10 分钟仍无进展时强制触发。v2 集成 visual-debug / agent-probe / DiagnosticsLog / netcoredbg-mcp 四大新工具。
---

# Data-Driven Debug v2: Evidence Over Assumption

## 核心铁律

**源码告诉你代码意图，运行时数据告诉你代码行为。两者不一致时，数据是对的，源码分析是错的。**

禁止模式：
```
看源码 → 猜测根因 → 改代码 → 编译 → 测试 → 不行 → 再看源码 → 再猜 → 再改 → ...
```

正确模式：
```
在数据链路关键节点采集实际值 → 定位哪个节点的输出偏离预期 → 修复那个节点。一次改对。
```

## 触发条件

以下任一条件满足，MUST 切换到数据采集模式，停止修改源码：

| 条件 |
|---|
| 同一问题耗时超过 10 分钟仍未解决 |
| 同一个 bug 尝试了 3 次修复仍无效 |
| 对根因有分歧——你说 X，数据可能说 Y |
| 编译通过但运行时行为与预期不一致 |
| 前端无响应、SSE 无数据、页面空白 |

触发后 MUST 声明：
```
🛑 切换到数据采集模式。停止修改源码，开始抓取运行时数据。
```

---

## 🔧 Debug 工具链 v2（四件套）

### 快速决策：用什么工具？

| 症状 | 用哪个 | 命令 |
|------|--------|------|
| UI 错位/白屏/动效异常 | **visual-debug** | `node scripts/lib/visual-debug.mjs --login --url=...` |
| API 返回 500/数据不对 | **agent-probe** | `node scripts/lib/probe.mjs --trace-sql GET /api/...` |
| 后端运行时状态（变量值/调用栈） | **netcoredbg-mcp** | Agent 直接 attach 到 JNPF 进程 |
| 任何异常（自动记录） | **DiagnosticsLog** | `cat backend/.claude/diagnostics/session-*.jsonl` |
| 快速回归验证 | **pnpm test:api** | `E2E_PIPELINE_ID=311 pnpm test:api` |

### 1. Visual Debug（录屏分析）

**用途：** UI 层问题——页面白屏、组件不渲染、样式错乱、交互无响应。

**产出：** 截图 PNG + 诊断 JSON（console errors / network errors / WebSocket events）

```bash
# PC 端录屏（带登录）
node scripts/lib/visual-debug.mjs --login --url "http://localhost:3100/#/onlineDev/webDesign" --duration 10

# 移动端录屏
node scripts/lib/visual-debug.mjs --login --mobile --url "http://localhost:3800/#/pages/index/message"

# 指定输出名
node scripts/lib/visual-debug.mjs --login --url "..." --output my-bug
```

**Agent 用法：**
```
1. 运行 visual-debug 生成 .json 诊断文件
2. Read 诊断 JSON → 看 consoleErrors / networkErrors / wsEvents
3. 看截图确认 UI 实际状态
4. 根据错误信息定位根因
```

### 2. agent-probe（诊断探针注入）

**用途：** API 层问题——返回 500、数据不对、SQL 异常。对单个请求注入 TRACE 级别诊断。

```bash
# 基础注入
node scripts/lib/probe.mjs --category my-debug GET "/api/visualdev/Base?type=1"

# 追踪 SQL
node scripts/lib/probe.mjs --trace-sql --category sql-debug GET "/api/visualdev/Base?type=1"

# POST 请求带 body
node scripts/lib/probe.mjs --category create-debug POST "/api/visualdev/Base" '{"fullName":"test"}'
```

**机制：** 发请求时带 `X-Diagnostics` header → 后端 `RequestActionFilter` 识别 → 为该请求开启详细日志 → 写入 `backend/.claude/diagnostics/session-*.jsonl`

**Agent 用法：**
```
1. 运行 probe 触发诊断
2. cat backend/.claude/diagnostics/session-*.jsonl | jq 'select(.category=="my-debug")'
3. 根据日志中的 SQL / 参数 / 返回值定位问题
```

### 3. DiagnosticsLog（统一诊断日志）

**用途：** 所有后端异常和诊断事件自动记录到 `.claude/diagnostics/session-*.jsonl`。Agent 可以直接 Read + jq 分析。

**位置：** `backend/.claude/diagnostics/session-{启动时间}.jsonl`

**Agent 用法：**
```bash
# 查看当前 session 所有日志
cat backend/.claude/diagnostics/session-*.jsonl | jq .

# 按分类过滤
cat backend/.claude/diagnostics/session-*.jsonl | jq 'select(.category=="IM")'

# 只看 error
cat backend/.claude/diagnostics/session-*.jsonl | jq 'select(.level=="error")'

# 看最新的 5 条
ls -t backend/.claude/diagnostics/session-*.jsonl | head -1 | xargs tail -5
```

**代码集成：**
```csharp
// 记录事件
DiagnosticsLog.Log("IM", "SendMessage", new { toUserId, content });

// 记录异常
DiagnosticsLog.Error("IM", "SendMessage", ex, new { toUserId });

// 记录 SQL
DiagnosticsLog.Sql("UserQuery", sql, parameters);
```

### 4. netcoredbg-mcp（.NET 运行时调试）

**用途：** 需要查看运行时变量值、调用栈、单步执行时使用。Agent 通过 MCP 直接 attach 到 JNPF 进程。

**前置：** 后端必须运行中。Agent 通过 `mcp.json` 中配置的 wrapper 自动发现进程 PID。

**Agent 用法：**
```
1. 确认后端在运行（localhost:5000）
2. 使用 netcoredbg MCP 工具：
   - set_breakpoint: 在指定文件:行号设断点
   - get_variables: 查看当前栈帧的变量值
   - get_stack_trace: 查看调用栈
   - continue: 继续执行
```

---

## 经典数据采集通道

### 前端：Playwright 网络抓包

```javascript
// 捕获特定请求的响应体
page.on('response', async (resp) => {
  if (resp.url().includes('/events')) {
    console.log('Status:', resp.status());
    console.log('Body:', await resp.text());
  }
});
// 推荐直接用 visual-debug，自动收集 console + network + WS
```

### 后端：HTTP 直连验证

```bash
# 快速探 API
node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser

# 结构化断言（日常默认）
cd D:/JNPF-v52 && E2E_PIPELINE_ID=311 pnpm test:api

# 带诊断注入
node scripts/lib/probe.mjs --trace-sql GET /api/visualdev/Base?type=1
```

### SQL：输出实际 SQL

```csharp
// SqlSugar 输出生成的 SQL — 加到可疑查询前
var sql = db.Queryable<T>().Where(...).ToSqlString();
DiagnosticsLog.Sql("SuspectQuery", sql);
```

### 浏览器：F12 Network 面板

| 采集点 | 操作 |
|---|---|
| 请求 URL / 响应体 | Network → 点击请求 → Headers / Response |
| SSE 事件流 | Network → 点击 `/events` → EventStream |
| WebSocket 消息 | Network → WS → Messages |

---

## 故障定位流程

### Step 1: 视觉先行

**UI 问题 → visual-debug 录屏。** 先确认页面实际渲染了什么。大部分"后端问题"其实是前端没渲染。

```bash
node scripts/lib/visual-debug.mjs --login --url "问题页面URL" --output bug-01
```

### Step 2: 探针定位

**API 问题 → agent-probe 注入。** 对可疑 API 开启 TRACE 日志，看完整请求/响应/SQL。

```bash
node scripts/lib/probe.mjs --trace-sql --category bug-01 GET "/api/可疑路径"
```

### Step 3: 诊断分析

**看 DiagnosticsLog。** 如果问题已经触发过异常，日志已在 `.claude/diagnostics/` 中。

```bash
cat backend/.claude/diagnostics/session-*.jsonl | jq 'select(.level=="error")'
```

### Step 4: 运行时下沉

**需要看变量/调用栈 → netcoredbg-mcp。** Agent attach 到进程设断点。

### Step 5: 最小化修复

只修改导致偏差的那个节点，不动上下游。

### Step 6: 回归验证

```bash
cd D:/JNPF-v52 && E2E_PIPELINE_ID=311 pnpm test:api   # 1 秒出结果
```

---

## 与 trace-bug 配合

- `trace-bug`：提供调试流程框架（四阶段：复现→假设→插桩→修复）
- `data-driven-debug`：提供数据采集具体方法（visual-debug / probe / DiagnosticsLog / netcoredbg）

当 `trace-bug` 进入阶段 3（插桩验证）时，使用本 skill 的工具箱采集数据。

## 与 systematic-debugging 配合

- `systematic-debugging`：四阶段强制流程（根因调查→模式分析→假设检验→实现修复）
- `data-driven-debug`：在 Phase 1（根因调查）和 Phase 3（假设检验）中提供数据采集手段

当 `systematic-debugging` 要求"read error messages"、"reproduce"、"gather evidence" 时 → 用本 skill 的工具箱。
