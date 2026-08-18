#!/usr/bin/env node
/**
 * PreToolUse Hook — Module Boundary Guard (R5 硬化)
 *
 * 职责：拦截对禁用/不存在模块的修改。
 *   CLAUDE.md R5：
 *     - OA 模块已禁用 → NEVER 修改
 *     - IoT/MES 模块未创建 → NEVER scaffold（防 AI 幻觉）
 *
 * BLOCK (exit 2): 写入路径落在禁用/不存在模块
 *
 * 判定规则（路径前缀匹配）：
 *   - OA 禁用区：backend/application/JNPF.OA.API.Entry/、backend/modularity/oa/（含各种变体）
 *   - IoT 不存在：任何匹配 *IoT* / *iot* 的 backend 新建路径
 *   - MES 不存在：任何匹配 *MES* / *mes* 的 backend 新建路径
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

if (!filePath) process.exit(0);

// 归一化路径用于匹配（统一小写比较）
const p = filePath.toLowerCase();

// ─── OA 模块禁用区 ──────────────────────────────────────────
// JNPF.OA.API.Entry 已禁用（见 AGENTS.md: "OA — disabled"）
const oaPatterns = [
  /backend\/application\/jnpf\.oa\.api\.entry/,
  /backend\/modularity\/oa\//,
  /jnpf\.oa\.api\.entry\//,
];

for (const pat of oaPatterns) {
  if (pat.test(p)) {
    console.error(`BLOCKED: 写入禁用模块 OA (R5) — ${filePath}`);
    console.error(`  JNPF.OA.API.Entry 模块已禁用，NEVER 修改。`);
    console.error(`  如需启用 OA 功能，先与团队确认启用方案，不要直接改禁用代码。`);
    console.error(`  Rule: CLAUDE.md R5 + AGENTS.md "OA — disabled"。`);
    process.exit(2);
  }
}

// ─── IoT / MES 不存在模块（防 AI 幻觉 scaffold）──────────────
// 仅对 backend 路径生效（避免误伤前端含 "mes"/"iot" 字样的文件名）
const isBackendPath = /backend\//.test(p) || /\.cs$/.test(filePath);

if (isBackendPath) {
  // IoT 检测（排除合法的 IotLike/iother 等无关词，要求路径片段明确是 IoT 模块）
  const iotMatch = p.match(/backend\/(?:modularity|application)\/[^/]*\b(iot)\b[^/]*\//)
    || p.match(/jnpf\.(iot)\.api\.entry\//);
  if (iotMatch) {
    console.error(`BLOCKED: scaffold 不存在模块 IoT (R5) — ${filePath}`);
    console.error(`  JNPF 仓库中不存在 IoT 模块，NEVER scaffold（防 AI 幻觉创建不存在的模块）。`);
    console.error(`  如确需新建 IoT 模块，先创建模块基础结构并获用户审批，不要在 .cs 文件中引用不存在的命名空间。`);
    console.error(`  Rule: CLAUDE.md R5 + AGENTS.md "IoT/MES modules don't exist"。`);
    process.exit(2);
  }

  // MES 检测
  const mesMatch = p.match(/backend\/(?:modularity|application)\/[^/]*\b(mes)\b[^/]*\//)
    || p.match(/jnpf\.(mes)\.api\.entry\//);
  if (mesMatch) {
    console.error(`BLOCKED: scaffold 不存在模块 MES (R5) — ${filePath}`);
    console.error(`  JNPF 仓库中不存在 MES 模块，NEVER scaffold（防 AI 幻觉创建不存在的模块）。`);
    console.error(`  Rule: CLAUDE.md R5 + AGENTS.md "IoT/MES modules don't exist"。`);
    process.exit(2);
  }
}

process.exit(0);
