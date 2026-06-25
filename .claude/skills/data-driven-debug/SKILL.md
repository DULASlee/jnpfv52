---
name: data-driven-debug
description: 数据驱动调试——禁止看源码猜测，必须抓取运行时数据定位问题。当问题耗时超过 10 分钟仍无进展时强制触发。
---

# Data-Driven Debug: Evidence Over Assumption

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

## 数据采集工具箱

### 前端：Playwright 网络抓包

```javascript
// 最常用：捕获特定请求的响应体
page.on('response', async (resp) => {
  const url = resp.url();
  if (url.includes('/events')) {
    const body = await resp.text();
    console.log('Status:', resp.status());
    console.log('Body:', body.substring(0, 500));
  }
});
```

| 采集点 | Playwright API |
|---|---|
| SSE 响应体 | `page.on('response', ...)` + `resp.text()` |
| fetch URL | `page.on('request', r => console.log(r.url(), r.postData()))` |
| 前端 console | `page.on('console', msg => console.log(msg.text()))` |
| DOM 状态 | `page.textContent('body')` |
| localStorage | `page.evaluate(() => localStorage.getItem('KEY'))` |
| JS 变量值 | `page.evaluate(() => someGlobalVar)` |

### 后端：HTTP 直连验证

```bash
# 绕过前端，直接测 API——排除前端干扰
curl -s -X POST http://localhost:5000/api/oauth/Login \
  -H "Content-Type: application/json" \
  -d '{"account":"admin","password":"123456","grant_type":"password"}'

# 带 token 测试需要认证的端点
curl -s http://localhost:5000/api/studio/pipeline/execute/42/events \
  -H "Authorization: Bearer <token>" \
  -H "Accept: text/event-stream"
```

| 采集点 | 方法 |
|---|---|
| API 响应状态+体 | `curl -s -w "%{http_code}" -o /dev/null URL` |
| SSE 流内容 | `curl -N -H "Accept: text/event-stream" URL` |
| 后端日志 | 控制台输出 / Serilog 文件 |

### 浏览器：F12 Network 面板

| 采集点 | 操作 |
|---|---|
| 请求 URL | Network → 点击请求 → Headers → Request URL |
| 请求体 | Network → 点击请求 → Payload |
| 响应状态 | Network → Status 列 |
| 响应体 | Network → 点击请求 → Response 标签 |
| SSE 事件 | Network → 点击 `/events` → EventStream 标签 |

### SQL：输出实际 SQL

```csharp
// SqlSugar 输出生成的 SQL
var sql = db.Queryable<T>().Where(...).ToSql();
_logger.LogInformation("Generated SQL: {Sql}", sql);
```

## 故障定位流程

### Step 1: 画数据链路

```
[浏览器] → fetch() → [Vite 代理] → [后端 API] → [LLM Gateway] → [DeepSeek]
    ↑                      ↑              ↑               ↑
  检查点1                检查点2        检查点3         检查点4
```

### Step 2: 从两端向中间收缩

1. **检查点 1（浏览器）**：Network 面板看请求发了没，URL 对不对
2. **检查点 3（后端）**：后端日志看请求到了没，处理了没
3. 如果 1 和 3 都对 → 问题在网络链路（CORS、代理、Token）
4. 如果 3 不对 → 从后端日志向上追溯
5. 如果 1 不对 → 从浏览器向下追溯

### Step 3: 在故障节点采集实际值

```javascript
// 示例：怀疑 Authorization header 不对
const token = getToken();
console.log('Token:', token);                           // 输出: "Bearer eyJ..."
console.log('Has prefix:', token.startsWith('Bearer')); // 输出: true
const header = `Bearer ${token}`;
console.log('Final header:', header);                   // 输出: "Bearer Bearer eyJ..." ← BINGO
```

### Step 4: 最小化修复

只修改导致偏差的那个节点，不动上下游。

## 常见数据链路及检查点

| 场景 | 链路 | 关键检查点 |
|---|---|---|
| AI 无回复 | 前端 sendMessage → POST /execute → Channel → GET /events → SSE → 前端渲染 | `/events` 响应体是 SSE 还是 JSON 错误？`getToken()` 实际返回值？ |
| 登录失败 | 前端 → POST /login → JWT 生成 → localStorage | JWT payload 中 TenantId 是 "0" 还是 "default"？ |
| 数据不对 | 前端 → API → SqlSugar → SQL → DB | `ToSql()` 输出的 SQL 是否包含 TenantId？ |
| 页面空白 | 前端 → Vite → Vue Router → 组件渲染 | Console 有无 JS 错误？Vue Router 是否匹配到路由？ |

## 修复后验证

修复后 MUST 用同样的数据采集方式验证：

```
修复前采集的数据（异常） → 修复后采集的数据（正常） → 对比确认差异消失
```

## 与 trace-bug 配合

- `trace-bug`：提供调试流程框架（四阶段：复现→假设→插桩→修复）
- `data-driven-debug`：提供数据采集具体方法（Playwright/curl/SQL/Network）

当 `trace-bug` 进入阶段 3（插桩验证）时，使用本 skill 的工具箱采集数据。
