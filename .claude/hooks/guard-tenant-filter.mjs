#!/usr/bin/env node
/**
 * PreToolUse Hook — Multi-Tenant Filter Guard (R4 硬化)
 *
 * 职责：拦截 SqlSugar 查询中可能漏过滤租户隔离的模式。
 *   CLAUDE.md R4：漏过滤 = 跨租户数据泄漏（最严重安全风险）。
 *
 * BLOCK (exit 2): 命中以下高风险模式之一
 *   B1. .SqlQueryable<>/Ado.SqlQuery 原生 SQL 查询无 WHERE（全表扫描，绕过框架过滤）
 *   B2. TenantId 被显式硬编码为常量/0/忽略（绕过租户隔离）
 *   B3. Updateable/Deleteable 显式 .Where("1=1") 或无 Where（跨租户修改/删除）
 *
 * 设计取舍：
 *   - SqlSugar 的 ITenantFilter 是全局查询过滤器，正常 Queryable<T>() 会自动附加。
 *     但原生 SQL (Ado.SqlQuery) 和 DisableGlobalFilter 会绕过它。
 *   - 仅检测最危险的"绕过"模式，不检测正常 LINQ 查询（避免误伤）。
 *   - 误报可加 // r4-safe: <理由> 注释豁免。
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
} else if (toolName === 'MultiEdit') {
  const edits = input.tool_input?.edits || [];
  content = edits.map(e => e.new_string || e.newText || '').filter(Boolean).join('\n');
}
if (!content) process.exit(0);

const lines = content.split('\n');
const issues = [];

for (let i = 0; i < lines.length; i++) {
  const line = lines[i];
  const trimmed = line.trim();

  // 跳过注释行（但保留行内注释检测）
  if (trimmed.startsWith('//') || trimmed.startsWith('*') || trimmed.startsWith('/*')) continue;

  // 显式 r4-safe 豁免标记
  if (/r4-safe/i.test(line)) continue;

  // B1: 原生 SQL 查询无 WHERE（最危险 —— 完全绕过 ITenantFilter）
  //   Ado.SqlQuery<T>("SELECT ... FROM ...") 无 WHERE 子句
  const rawSqlNoWhere = /(Ado\.SqlQuery|SqlQueryable|GetDataTable)\s*\(\s*\$?@?"[^"]*(?:SELECT|select)[^"]*(?:FROM|from)\s+\w+[^"]*"\s*\)/i.test(line)
    && !/where/i.test(line);
  if (rawSqlNoWhere) {
    issues.push({
      line: i + 1,
      rule: 'R4-B1',
      detail: `原生 SQL 查询疑似无 WHERE 子句。\n` +
              `  Ado.SqlQuery/SqlQueryable 绕过 ITenantFilter 全局过滤器 = 跨租户数据泄漏。\n` +
              `  MUST 改用 Queryable<T>().Where(...)（自动附加租户过滤），或显式 .Where("TenantId = @tid", new { tid = ... })。`
    });
    continue;
  }

  // B2: 显式禁用全局过滤器（包括租户过滤器）
  if (/DisableGlobalFilter\s*\(\s*"?(TenantFilter|ITenantFilter|Tenant)"?\s*\)/i.test(line)) {
    issues.push({
      line: i + 1,
      rule: 'R4-B2',
      detail: `显式禁用租户全局过滤器 (DisableGlobalFilter("Tenant..."))。\n` +
              `  这会完全绕过跨租户隔离 = 数据泄漏。\n` +
              `  除非是 DBA 级跨租户管理操作，否则 NEVER 这样做。如确需，加注释 // r4-safe: <跨租户管理操作的理由>。`
    });
    continue;
  }

  // B3: Updateable/Deleteable 无 Where 或 .Where("1=1")（跨租户批量修改）
  const isUpdateDelete = /\.(Updateable|Deleteable)\s*</i.test(line);
  if (isUpdateDelete) {
    // 往后扫 5 行找 Where
    const block = lines.slice(i, Math.min(i + 6, lines.length)).join(' ');
    const hasWhere = /\.Where\s*\(/i.test(block);
    const hasOneEqualsOne = /['"]?\s*1\s*=\s*1\s*['"]?/.test(block);
    if (!hasWhere) {
      issues.push({
        line: i + 1,
        rule: 'R4-B3',
        detail: `Updateable/Deleteable 链未发现 .Where()。\n` +
                `  无 Where 的更新/删除 = 跨租户修改/删除全部数据。\n` +
                `  MUST 链式调用 .WhereColumns(...) 或 .Where(...) 显式限定租户范围。`
      });
    } else if (hasOneEqualsOne) {
      issues.push({
        line: i + 1,
        rule: 'R4-B3',
        detail: `Updateable/Deleteable 使用 .Where("1=1") = 全表操作。\n` +
                `  1=1 等于无过滤 = 跨租户修改/删除全部数据。\n` +
                `  MUST 改为限定租户的真实条件。`
      });
    }
  }
}

// ─── 输出 ───────────────────────────────────────────────────
if (issues.length > 0) {
  console.error(`BLOCKED: 多租户隔离风险 (R4) in ${filePath}`);
  for (const it of issues) {
    console.error(`  [${it.rule}] 第 ${it.line} 行: ${it.detail}`);
  }
  console.error(`  多租户是 JNPF 最严重安全红线。漏过滤 = 跨租户数据泄漏。`);
  console.error(`  修复后重写。误报时加 // r4-safe: <理由> 注释豁免。`);
  process.exit(2);
}

process.exit(0);
