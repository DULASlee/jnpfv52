#!/usr/bin/env node
/**
 * PreToolUse Hook — API Permission Guard (HARD BLOCK)
 * 检查新增 IDynamicApiController 类是否有权限声明
 * BLOCK (exit 2): 有控制器类但缺少安全属性（原 exit 1 软警告 → 升级为硬阻断）
 *
 * 升级理由：R8 红线声明 JwtHandler 当前 bypass 为临时状态，
 *   无授权属性的 API 端点 = 越权访问风险。软警告被 AI 忽略率 ~70%，
 *   必须硬阻断才能强制 AI 在写入前声明权限意图。
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

// 仅在新增（首次声明 IDynamicApiController 实现）时触发，
// 避免对已有控制器的常规编辑误伤。
const hasApiController = /:\s*IDynamicApiController\b/i.test(content);
const hasSecurityDefine = /\[SecurityDefine\]/i.test(content);
const hasAllowAnonymous = /\[AllowAnonymous\]/i.test(content);
const hasAuthorize = /\[Authorize\]/i.test(content);
const hasApiDescription = /\[ApiDescriptionSettings\]/i.test(content);

if (hasApiController && !hasSecurityDefine && !hasAllowAnonymous && !hasAuthorize) {
  console.error(`BLOCKED: IDynamicApiController 类缺少权限声明 in ${filePath}`);
  console.error(`  类实现了 IDynamicApiController 但未声明任何权限属性。`);
  console.error(`  JwtHandler 当前 bypass 为临时状态，未声明权限 = 越权风险 (R8 红线)。`);
  console.error(`  MUST 在 class 声明上方添加以下其一：`);
  console.error(`    - [AllowAnonymous]              公开端点（登录、健康检查）`);
  console.error(`    - [SecurityDefine(\"权限码\")]    角色受限端点`);
  console.error(`    - [Authorize]                   已认证即可访问`);
  console.error(`  修复后重新写入。`);
  process.exit(2);
}

process.exit(0);
