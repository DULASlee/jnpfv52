#!/usr/bin/env node
/**
 * Cursor preToolUse — ADF P0–P3 禁止写业务 .cs/.vue
 */
import { checkAdfWrite } from '../../.claude/hooks/adf-gate-lib.mjs';

let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch {
  input = {};
}

const toolInput = input.tool_input || input.arguments || input || {};
const filePath = (
  toolInput.file_path
  || toolInput.path
  || toolInput.target_notebook
  || ''
).replace(/\\/g, '/');

const result = checkAdfWrite(filePath);
if (!result.block) {
  process.stdout.write(JSON.stringify({ permission: 'allow' }));
  process.exit(0);
}

process.stdout.write(JSON.stringify({
  permission: 'deny',
  user_message: result.reason,
  agent_message: result.reason,
}));
process.exit(0);
