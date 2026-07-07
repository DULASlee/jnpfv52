#!/usr/bin/env node
/**
 * agent-probe — JNPF 诊断探针注入工具
 *
 * 在 API 请求中注入诊断头，后端识别后对该请求开启 TRACE 级别日志。
 * Agent 采集完数据后自动清理，不留痕迹。
 *
 * 用法:
 *   # 对单个 API 注入探针
 *   node scripts/lib/probe.mjs GET /api/visualdev/Base?type=1
 *
 *   # 指定诊断分类
 *   node scripts/lib/probe.mjs --category=IM POST /api/message/ImReplyService { ... }
 *
 *   # 追踪 SQL
 *   node scripts/lib/probe.mjs --trace-sql GET /api/visualdev/Base?type=1
 *
 *   # 设置诊断级别
 *   node scripts/lib/probe.mjs --level=trace POST /api/visualdev/OnlineDev/xxx/List { ... }
 *
 * 机制:
 *   1. 发 API 请求时带 header: X-Diagnostics: { category, level, traceSql }
 *   2. 后端 RequestActionFilter / DiagnosticsLog 识别后为该请求开启详细日志
 *   3. 日志写入 backend/.claude/diagnostics/session-*.jsonl
 *   4. Agent 通过 Read 工具直接分析日志
 */

import { apiRequest, loadCachedSession } from './jnpf-auth.mjs';
import { readFileSync, writeFileSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));

// 解析参数
const args = process.argv.slice(2);
let category = 'probe';
let level = 'trace';
let traceSql = false;
let method = '';
let path = '';
let body = null;

for (let i = 0; i < args.length; i++) {
  const a = args[i];
  if (a === '--category' || a === '-c') {
    category = args[++i];
  } else if (a.startsWith('--category=')) {
    category = a.split('=')[1];
  } else if (a === '--level' || a === '-l') {
    level = args[++i];
  } else if (a.startsWith('--level=')) {
    level = a.split('=')[1];
  } else if (a === '--trace-sql') {
    traceSql = true;
  } else if (a === '--output' || a === '-o') {
    args[++i]; // skip output arg, not used by probe
  } else if (!method) {
    method = a;
  } else if (!path) {
    path = a;
  } else {
    try { body = JSON.parse(a); } catch { body = a; }
  }
}

if (!method || !path) {
  console.log('用法: node scripts/lib/probe.mjs [--category=C] [--level=L] [--trace-sql] GET|POST <path> [body]');
  console.log('  --category=C   诊断分类 (默认 probe)');
  console.log('  --level=L      诊断级别: trace/info/warn/error (默认 trace)');
  console.log('  --trace-sql    追踪 SQL 查询');
  process.exit(1);
}

// 构建诊断头
const diagHeader = JSON.stringify({ category, level, traceSql, ts: new Date().toISOString() });

const headers = {
  'X-Diagnostics': diagHeader,
};

const session = loadCachedSession();
const opts = { headers };
if (session?.token) opts.token = session.token;
if (body) opts.body = body;
const result = await apiRequest(method, path, opts);

// 输出结果
console.log(JSON.stringify({
  probe: { category, level, traceSql },
  response: result,
  diagnosticsFile: 'backend/.claude/diagnostics/session-*.jsonl (latest)'
}, null, 2));

// 非 2xx 时输出诊断提示
if (result.code && result.code !== 200) {
  console.log('\n⚠️  非 200 响应 — 查看诊断日志:');
  console.log('   grep "category.*' + category + '" backend/.claude/diagnostics/session-*.jsonl | tail -20');
}
