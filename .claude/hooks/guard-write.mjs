#!/usr/bin/env node
/**
 * PreToolUse Hook — 物理级防线
 * 职责：保护敏感文件 + 拦截清空文件
 * 阻断：exit 2 + stderr（自动反馈给 Claude）
 */
let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch { input = {}; }

const filePath = process.env.CLAUDE_FILE_PATH
  || input.tool_input?.file_path || '';

const toolName = process.env.CLAUDE_TOOL_NAME
  || input.tool_name || '';

const toolInput = process.env.CLAUDE_TOOL_INPUT
  ? JSON.parse(process.env.CLAUDE_TOOL_INPUT)
  : (input.tool_input || {});

const content = toolInput.content || toolInput.new_string || '';

if (!filePath) process.exit(0);

// 绝对禁止写入的路径
const FORBIDDEN = [
  /\.env(\.[a-zA-Z0-9]+)?$/,
  /\.pem$/, /\.key$/, /\.p12$/, /\.pfx$/, /\.crt$/, /\.cer$/,
  /id_rsa/, /id_ecdsa/, /id_ed25519/,
  /docker-compose\.ya?ml$/i,
  /Dockerfile$/i,
  /\.github\/workflows\//,
];

for (const p of FORBIDDEN) {
  if (p.test(filePath)) {
    console.error(`BLOCKED: Cannot write to protected file: ${filePath}`);
    process.exit(2);
  }
}

// 拦截清空源文件（仅 Write 操作）
if (toolName === 'Write' && typeof content === 'string'
    && content.trim() === '' && /^(src|lib)\//.test(filePath)) {
  console.error(`BLOCKED: Attempting to empty source file: ${filePath}`);
  process.exit(2);
}

process.exit(0);
