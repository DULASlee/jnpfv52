#!/usr/bin/env node
/**
 * Cursor preToolUse — 占位符硬失败
 * matcher: Write|StrReplace|EditNotebook（及兼容 Edit）
 */
import { scanFileContent } from '../../.claude/hooks/placeholder-scan.mjs';

let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch {
  input = {};
}

const toolName = input.tool_name || input.toolName || '';
const toolInput = input.tool_input || input.arguments || input || {};

const filePath = (
  toolInput.file_path
  || toolInput.path
  || toolInput.target_notebook
  || ''
).replace(/\\/g, '/');

let content = '';
if (toolInput.content != null) content = String(toolInput.content);
else if (toolInput.new_string != null) content = String(toolInput.new_string);
else if (toolInput.new_str != null) content = String(toolInput.new_str);
else if (toolInput.contents != null) content = String(toolInput.contents);

const hits = scanFileContent(filePath, content);
if (hits.length === 0) {
  process.stdout.write(JSON.stringify({ permission: 'allow' }));
  process.exit(0);
}

const detail = hits.map((h) => `L${h.line} [${h.rule}] ${h.match}`).join('; ');
const msg = `占位符硬失败：${filePath} — ${detail}。完成实现或加 // placeholder-ok: <理由>`;

process.stdout.write(JSON.stringify({
  permission: 'deny',
  user_message: msg,
  agent_message: msg,
}));
process.exit(0);
