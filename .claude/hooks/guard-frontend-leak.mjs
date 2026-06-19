#!/usr/bin/env node
/**
 * PreToolUse Hook — Frontend Memory Leak Guard (R6 硬化)
 *
 * 职责：拦截 Vue 组件中的 SSE/Timer 泄漏模式。
 *   CLAUDE.md R6 + .claude/rules/frontend-memory-leak.md 的 6 条铁律，
 *   原纯靠 AI 自觉（长会话漂移率 ~50%），现升级为可执行谓词。
 *
 * BLOCK (exit 2): 命中明确的泄漏模式
 *   - setTimeout/setInterval/EventSource 创建但同文件无 clear/onUnmounted
 *   - EventSource.onerror 中直接同步调用 connect()（busy loop）
 *
 * 设计取舍：只拦截"明确的泄漏模式"，避免误伤。
 *   - 单文件级检测（无法跨文件追踪），保守判定
 *   - 仅检查 .vue/.ts/.tsx/.js 文件
 *   - 误报时 AI 可在 PR 中说明并临时降级
 */
let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch { input = {}; }

const filePath = (input.tool_input?.file_path || '').replace(/\\/g, '/');
const toolName = input.tool_name || '';

// 仅检查前端文件
if (!/\.(vue|ts|tsx|js|jsx)$/.test(filePath)) process.exit(0);

let content = '';
if (toolName === 'Write') {
  content = input.tool_input?.content || '';
} else if (toolName === 'Edit') {
  content = input.tool_input?.newText || input.tool_input?.new_string || '';
}
if (!content) process.exit(0);

// ─── 提取代码（去掉 <template> 块，只看 <script>）──────────
let code = content;
const scriptMatch = content.match(/<script[^>]*>([\s\S]*?)<\/script>/i);
if (scriptMatch) code = scriptMatch[1];

// ─── 检测项 ─────────────────────────────────────────────────
const issues = [];

// 1. setTimeout / setInterval 创建，但无对应 clear / onUnmounted
const hasSetTimeout = /\bsetTimeout\s*\(/.test(code);
const hasSetInterval = /\bsetInterval\s*\(/.test(code);
const hasClearTimeout = /\bclearTimeout\s*\(/.test(code);
const hasClearInterval = /\bclearInterval\s*\(/.test(code);
const hasOnUnmounted = /\bonUnmounted\s*[\(\{]/.test(code) || /\bonBeforeUnmount\s*[\(\{]/.test(code);

if (hasSetTimeout && !hasClearTimeout && !hasOnUnmounted) {
  issues.push({
    rule: 'R6.2',
    detail: '调用了 setTimeout() 但本文件未发现 clearTimeout() 或 onUnmounted()。\n' +
            '  定时器返回值 MUST 保存，并在 onUnmounted 中清除，否则组件销毁后仍执行 → 内存泄漏。'
  });
}
if (hasSetInterval && !hasClearInterval && !hasOnUnmounted) {
  issues.push({
    rule: 'R6.2',
    detail: '调用了 setInterval() 但本文件未发现 clearInterval() 或 onUnmounted()。\n' +
            '  interval 不会自动停止，组件销毁后持续触发 → 严重内存泄漏。'
  });
}

// 2. EventSource 创建，但无 onUnmounted 关闭 / 无 retry cap
const hasEventSource = /\bnew\s+EventSource\s*\(/.test(code);
if (hasEventSource) {
  if (!hasOnUnmounted) {
    issues.push({
      rule: 'R6.2',
      detail: '创建了 new EventSource() 但本文件未发现 onUnmounted()。\n' +
              '  EventSource MUST 在组件销毁时 .close()，否则持续占用连接。'
    });
  }
  // 检查是否有 retry 计数上限（防无限重连）
  const hasRetryCap = /MAX_RETRIES|maxRetries|retryCount|reconnectLimit/i.test(code);
  if (!hasRetryCap) {
    issues.push({
      rule: 'R6.3',
      detail: 'EventSource 重连未发现 retry 上限（MAX_RETRIES / maxRetries / retryCount）。\n' +
              '  EventSource onerror 中 MUST 有重试计数上限（如 MAX_RETRIES=5），否则网络故障时无限重连 → 浏览器卡死。'
    });
  }
  // 检查 onerror 是否直接同步调用 connect/reconnect
  const onerrorDirectReconnect = /onerror\s*[:=]\s*(?:function\s*)?\(?[^)]*\)?\s*=>?\s*\{[\s\S]*?\b(connect|reconnect)\s*\(/i.test(code);
  if (onerrorDirectReconnect) {
    issues.push({
      rule: 'R6.4',
      detail: 'EventSource.onerror 中疑似直接同步调用 connect()/reconnect()。\n' +
              '  onerror 同步回调中直接重连 = busy loop（错误同步触发新连接）。\n' +
              '  MUST 用 setTimeout(() => connect(), delay) + 计数器实现指数退避。'
    });
  }
}

// ─── 输出 ───────────────────────────────────────────────────
if (issues.length > 0) {
  console.error(`BLOCKED: 前端内存泄漏风险 (R6) in ${filePath}`);
  for (const it of issues) {
    console.error(`  [${it.rule}] ${it.detail}`);
  }
  console.error(`  完整规则见 .claude/rules/frontend-memory-leak.md（6 条铁律）。`);
  console.error(`  若为误报（如定时器在外部 store 中管理），请在代码中添加注释 // r6-safe: <理由> 后重写。`);
  process.exit(2);
}

process.exit(0);
