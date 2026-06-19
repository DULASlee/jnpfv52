# 前端 SSE / Timer 内存泄漏铁律

> 历史教训：2026-06-18 `PipelineSSEPanel.vue`、`usePipelineSSE.ts`、两个 `AiChatPanel.vue` 同时存在 SSE 无限重连 + setTimeout 未清理。已修复，必须防止复发。

写任何涉及 `setTimeout` / `setInterval` / `EventSource` / `WebSocket` 的前端代码时，MUST 遵循以下 6 条铁律：

---

## 铁律 1：setTimeout/setInterval 的返回值 MUST 保存到变量

```typescript
// ❌ 禁止 — 返回值丢失，无法清理
setTimeout(() => { doSomething(); }, 5000);

// ✅ 必须 — 保存返回值
let myTimer: ReturnType<typeof setTimeout> | null = null;
myTimer = setTimeout(() => { doSomething(); }, 5000);
```

## 铁律 2：onUnmounted MUST 清理所有定时器

```typescript
onUnmounted(() => {
  if (myTimer) { clearTimeout(myTimer); myTimer = null; }
  if (myInterval) { clearInterval(myInterval); myInterval = null; }
});
```

## 铁律 3：EventSource/WebSocket 重连 MUST 有上限

```typescript
// ❌ 禁止 — 无限重连，组件卸载后 setTimeout 仍触发 connect()
eventSource.onerror = () => {
  setTimeout(() => { connect(); }, 5000);  // 永不停止
};

// ✅ 必须 — 重连计数 + 上限 + 组件卸载时取消挂起的重连
const MAX_RETRIES = 5;
let retryCount = 0;
let reconnectTimer: ReturnType<typeof setTimeout> | null = null;

eventSource.onerror = () => {
  eventSource?.close();
  eventSource = null;
  if (retryCount >= MAX_RETRIES) return;
  retryCount++;
  reconnectTimer = setTimeout(() => {
    reconnectTimer = null;
    connect();
  }, 5000);
};

onUnmounted(() => {
  if (reconnectTimer) clearTimeout(reconnectTimer);
  eventSource?.close();
});
```

## 铁律 4：onerror 中 NEVER 直接调用 connect() — 必须经 setTimeout + 计数

`EventSource` 的 `onerror` 会在连接断开时同步触发，直接调 `connect()` 会立即创建新连接，若后端不可达则瞬间再次 error，形成 CPU 占满的 busy loop。

## 铁律 5：EventSource URL MUST 与 axios 共享 apiUrl 前缀（开发环境 /dev）

> 详见 ADR：`openspec/adr/ADR-002-sse-dev-proxy-prefix.md` + `.cursor/rules/sse-dev-proxy.mdc`

```typescript
// ❌ 禁止 — 开发环境无 /dev 前缀，SSE 不到后端
new EventSource(`/api/studio/pipeline/execute/${id}/events`);

// ✅ 必须 — 使用 buildEventSourceUrl
import { buildEventSourceUrl } from '/@/utils/http/sseUrl';
new EventSource(buildEventSourceUrl(`/api/studio/pipeline/execute/${id}/events`));
```

## 铁律 6：EventSource MUST 通过 `?token=` 传递 JWT

`EventSource` 无法设置 `Authorization` 头。`buildEventSourceUrl()` 已自动附加。

**时序**：先 `connectSSE()`，再 `POST /execute`，避免 channel 竞态丢包。
