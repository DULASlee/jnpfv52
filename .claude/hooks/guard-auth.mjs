#!/usr/bin/env node
/**
 * PreToolUse Hook — API Permission Guard
 * 检查新增 IDynamicApiController 类是否有权限声明
 * WARN (exit 1): 有控制器类但缺少安全属性
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

if (!filePath.endsWith('.cs')) process.exit(0);

let content = '';
if (toolName === 'Write') {
  content = input.tool_input?.content || '';
} else if (toolName === 'Edit') {
  content = input.tool_input?.newText || input.tool_input?.new_string || '';
}
if (!content) process.exit(0);

// Check for IDynamicApiController class definition without security attributes
const hasApiController = /:\s*IDynamicApiController\b/i.test(content);
const hasSecurityDefine = /\[SecurityDefine\]/i.test(content);
const hasAllowAnonymous = /\[AllowAnonymous\]/i.test(content);
const hasAuthorize = /\[Authorize\]/i.test(content);
const hasApiDescription = /\[ApiDescriptionSettings\]/i.test(content);

if (hasApiController && !hasSecurityDefine && !hasAllowAnonymous && !hasAuthorize) {
  console.error(`WARNING: IDynamicApiController class without auth attributes in ${filePath}`);
  console.error(`  The class implements IDynamicApiController but no [SecurityDefine],`);
  console.error(`  [Authorize], or [AllowAnonymous] attribute was found.`);
  console.error(`  Current JwtHandler bypass is temporary. Explicitly declare intent:`);
  console.error(`    - [AllowAnonymous] if this endpoint is public (login, health check)`);
  console.error(`    - [SecurityDefine] + permission code if role-restricted`);
  console.error(`  Rule: CLAUDE.md R8 + .cursor/rules/toolchain-division.mdc`);
  process.exit(1);
}

process.exit(0);
