#!/usr/bin/env node
/**
 * PreToolUse Hook — SQL Injection Defense
 * 拦截 .cs 文件中 SQL 拼接/注入模式
 * BLOCK (exit 2): 明确注入模式 (DROP/DELETE/string interpolation + SQL)
 * WARN  (exit 1): 可疑但不确定的模式
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

// Only check C# files
if (!filePath.endsWith('.cs')) process.exit(0);

// Get content being written
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

// === CRITICAL: Pattern-based detection ===

const lines = content.split('\n');

// Pattern 1: DROP / TRUNCATE via string interpolation or format
const dropPattern = /\$"([^"]*\b(DROP\s+(TABLE|DATABASE|INDEX)|TRUNCATE\s+TABLE)\b[^"]*)"/i;
for (const line of lines) {
  if (dropPattern.test(line)) {
    console.error(`BLOCKED: SQL injection risk — DROP/TRUNCATE via string interpolation in ${filePath}`);
    console.error(`  Offending line: ${line.trim().substring(0, 200)}`);
    console.error(`  Rule: .claude/rules/sql-safety.md — NEVER concatenate table names into SQL`);
    process.exit(2);
  }
}

// Pattern 2: DELETE FROM via string interpolation
const deletePattern = /\$"([^"]*\bDELETE\s+FROM\b[^"]*)"/i;
for (const line of lines) {
  if (deletePattern.test(line)) {
    console.error(`BLOCKED: SQL injection risk — DELETE FROM via string interpolation in ${filePath}`);
    console.error(`  Offending line: ${line.trim().substring(0, 200)}`);
    process.exit(2);
  }
}

// Pattern 3: SELECT/INSERT/UPDATE via string interpolation
const dmlPattern = /\$"([^"]*\b(SELECT|INSERT\s+INTO|UPDATE\s+\w+\s+SET)\b[^"]*)"/i;
for (const line of lines) {
  if (dmlPattern.test(line)) {
    console.error(`BLOCKED: SQL injection risk — DML via string interpolation in ${filePath}`);
    console.error(`  Offending line: ${line.trim().substring(0, 200)}`);
    console.error(`  Use parameterized queries: SqlSugar.Where() or SqlSugarParameter`);
    process.exit(2);
  }
}

// Pattern 4: string.Format("SELECT...
const formatSqlPattern = /string\.Format\(\s*"[^"]*\b(SELECT|INSERT|UPDATE|DELETE|DROP)\b/i;
for (const line of lines) {
  if (formatSqlPattern.test(line)) {
    console.error(`BLOCKED: SQL injection risk — string.Format with SQL in ${filePath}`);
    console.error(`  Offending line: ${line.trim().substring(0, 200)}`);
    process.exit(2);
  }
}

// Pattern 5: Ado.SqlQuery / Ado.ExecuteCommand with string interpolation
const adoInjectionPattern = /\b(Ado\.SqlQuery|Ado\.ExecuteCommand)\s*\(\s*\$"/i;
for (const line of lines) {
  if (adoInjectionPattern.test(line)) {
    console.error(`BLOCKED: SQL injection risk — raw SQL with string interpolation in ${filePath}`);
    console.error(`  Offending line: ${line.trim().substring(0, 200)}`);
    console.error(`  Use parameterized SqlSugarParameter instead`);
    process.exit(2);
  }
}

// Pattern 6 (WARN): Plain string concatenation with SQL keywords
const concatSqlPattern = /"\s*\+\s*.*\b(SELECT|INSERT|UPDATE|DELETE|DROP)\b|SELECT.*"\s*\+/i;
let warned = false;
for (const line of lines) {
  if (concatSqlPattern.test(line) && !line.trim().startsWith('//') && !line.trim().startsWith('*')) {
    console.error(`WARNING: Potential SQL concatenation detected in ${filePath}`);
    console.error(`  Line: ${line.trim().substring(0, 200)}`);
    console.error(`  Please verify this uses parameterized queries or SqlSugar LINQ`);
    warned = true;
  }
}
if (warned) process.exit(1);

process.exit(0);
