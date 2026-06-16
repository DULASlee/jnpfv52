#!/usr/bin/env node
/**
 * PreToolUse Hook (Bash) — 危险命令拦截器
 *
 * 职责：拦截高危 Bash 命令，防止误操作导致数据丢失。
 * 覆盖：Windows / Linux / 数据库 / Git / 安全
 *
 * 退出行为：
 *   危险命令 → exit 2（硬阻断，不重试）
 *   安全命令 → exit 0（放行）
 *   脚本异常 → exit 0（不阻断 AI）
 */

// ─── 读取 stdin（异步，Windows 兼容）─────────────────────────
let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch {
  input = {};
}

if ((input.tool_name || '') !== 'Bash') process.exit(0);

const cmd = (input.tool_input?.command || '').trim();
if (!cmd) process.exit(0);

// ─── 危险命令规则表 ─────────────────────────────────────────
const rules = [
  // Windows 专用
  { pattern: /\brmdir\s+\/[sS]\s+\/[qQ]\b/i, label: 'Windows rmdir /s /q（递归强制删除）' },
  { pattern: /\bdel\s+\/[sS]\s+\/[qQ]\b/i, label: 'Windows del /s /q（递归强制删除文件）' },
  { pattern: /\bRemove-Item\b.*-[Rr]ecurse\b.*-[Ff]orce\b/i, label: 'PowerShell Remove-Item -Recurse -Force' },

  // Linux / macOS
  { pattern: /\brm\s+(-[a-zA-Z]*r[a-zA-Z]*f|-[a-zA-Z]*f[a-zA-Z]*r)\b/, label: 'rm -rf（递归强制删除）' },
  { pattern: /\brm\s+-rf\s+\//, label: 'rm -rf /（删除根目录）' },
  { pattern: /\brm\s+-r\s+\//, label: 'rm -r /（删除根目录）' },

  // 数据库
  { pattern: /\bDROP\s+DATABASE\b/i, label: 'DROP DATABASE' },
  { pattern: /\bDROP\s+TABLE\b/i, label: 'DROP TABLE' },
  { pattern: /\bTRUNCATE\s+TABLE\b/i, label: 'TRUNCATE TABLE' },
  { pattern: /\bDELETE\s+FROM\b.*\bWHERE\b.*1\s*=\s*1/i, label: 'DELETE FROM ... WHERE 1=1（全表删除）' },

  // Git 高危
  { pattern: /\bgit\s+push\b.*--force\b/, label: 'git push --force' },
  { pattern: /\bgit\s+reset\b.*--hard\b/, label: 'git reset --hard' },
  { pattern: /\bgit\s+clean\b.*-[a-zA-Z]*f/, label: 'git clean -f（强制清理未跟踪文件）' },
  { pattern: /\bgit\s+checkout\b.*--\s*\.\s*$/, label: 'git checkout -- .（丢弃所有修改）' },
  { pattern: /\bgit\s+checkout\s+\.(\s|$)/, label: 'git checkout .（丢弃所有修改）' },
  { pattern: /\bgit\s+restore\s+\.(\s|$)/, label: 'git restore .（丢弃所有修改）' },

  // 安全
  { pattern: /\beval\s*\(/, label: 'eval()（代码注入风险）' },
  { pattern: /\bcurl\b.*\|\s*(bash|sh)\b/, label: 'curl | bash（远程代码执行）' },
];

// ─── 匹配检测 ───────────────────────────────────────────────
for (const rule of rules) {
  if (rule.pattern.test(cmd)) {
    console.error(`🚫 危险命令拦截：${rule.label}`);
    console.error(`   命令: ${cmd.slice(0, 120)}`);
    // 硬阻断：exit 2 = 不重试
    process.exit(2);
  }
}

// 安全命令放行
process.exit(0);
