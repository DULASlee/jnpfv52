#!/usr/bin/env node
/**
 * PreToolUse Hook — File Write Guardian (JNPF v5.2)
 *
 * 三层防护：
 *   L1 (exit 2): 禁止写入密钥/凭证/部署文件
 *   L2 (exit 2): 禁止清空源码文件（按扩展名匹配）
 *   L3 (分级):   安全模式扫描 — 高危阻断 + 中危警告
 *
 * 输入：env var (CLAUDE_FILE_PATH/TOOL_NAME/TOOL_INPUT) + stdin fallback
 */

// ─── 输入解析（双源：env var + stdin fallback）─────────────────
let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch { input = {}; }

const filePath = (process.env.CLAUDE_FILE_PATH
  || input.tool_input?.file_path || '').replace(/\\/g, '/');

const toolName = process.env.CLAUDE_TOOL_NAME
  || input.tool_name || '';

const toolInput = process.env.CLAUDE_TOOL_INPUT
  ? JSON.parse(process.env.CLAUDE_TOOL_INPUT)
  : (input.tool_input || {});

let content = '';
if (toolName === 'Write') {
  content = toolInput.content || '';
} else if (toolName === 'Edit') {
  content = toolInput.newText || toolInput.new_string || '';
} else if (toolName === 'MultiEdit') {
  const edits = toolInput.edits || [];
  content = edits.map(e => e.new_string || e.newText || '').filter(Boolean).join('\n');
}

if (!filePath) process.exit(0);

// ═══════════════════════════════════════════════════════════════
// L1: 禁止写入敏感文件 (exit 2)
// ═══════════════════════════════════════════════════════════════
const FORBIDDEN = [
  /\.env(\.[a-zA-Z0-9]+)?$/,
  /\.pem$/, /\.key$/, /\.p12$/, /\.pfx$/, /\.crt$/, /\.cer$/,
  /id_rsa/, /id_ecdsa/, /id_ed25519/,
  /docker-compose\.ya?ml$/i,
  /Dockerfile$/i, /Containerfile$/i,
  /\.github\/workflows\//,
];

for (const p of FORBIDDEN) {
  if (p.test(filePath)) {
    console.error(`BLOCKED: 禁止写入受保护文件: ${filePath}`);
    console.error(`  规则: CLAUDE.md guard-write — 密钥/凭证/部署文件不可由 AI 写入`);
    process.exit(2);
  }
}

// ═══════════════════════════════════════════════════════════════
// L2: 禁止清空源码文件 (exit 2)
// ═══════════════════════════════════════════════════════════════
const SOURCE_EXT = /\.(ts|tsx|js|jsx|mjs|cjs|vue|svelte|cs|py|go|rs|java|rb|php|swift|kt|scala)$/i;
if (toolName === 'Write' && typeof content === 'string'
    && content.trim() === '' && SOURCE_EXT.test(filePath)) {
  console.error(`BLOCKED: 试图清空源文件: ${filePath}`);
  console.error(`  规则: CLAUDE.md guard-write — 禁止将源码文件覆盖为空内容`);
  process.exit(2);
}

// ═══════════════════════════════════════════════════════════════
// L3: 安全模式扫描（分级：阻断 / 警告）
// ═══════════════════════════════════════════════════════════════
// 跳过基础设施和测试文件（自身含检测模式关键字或测试 payload）
const isInfraOrTestFile = /(?:\.claude[\\/]|[\\/]scripts[\\/]|[\\/]tests?[\\/]|\.test\.)/.test(filePath);

if (!isInfraOrTestFile && SOURCE_EXT.test(filePath) && typeof content === 'string' && content.trim()) {
  const blocks = [];
  const warns = [];

  // ── BLOCK 级：高危模式 (exit 2) ──────────────────────────────

  // 硬编码密钥/密码/Token（高置信度）
  if (/(?:api[_-]?key|apikey|secret|token|password|passwd|connectionString)\s*[:=]\s*['"][A-Za-z0-9_\-!@#$%^&*+=\/]{16,}['"]/i.test(content)) {
    blocks.push('硬编码密钥/密码/Token — 使用环境变量或密钥管理服务替代');
  }

  // SQL 字符串拼接 — 仅限 .cs 文件
  if (/\.cs$/i.test(filePath)
      && /(\$"|"\s*\+\s*)\s*.*\b(SELECT|INSERT|UPDATE|DELETE|DROP|TRUNCATE|ALTER|EXEC|EXECUTE)\b/i.test(content)) {
    blocks.push('SQL 字符串拼接 — 使用参数化查询或 SqlSugar LINQ 替代');
  }

  // eval() / 动态代码执行
  if (/\beval\s*\(/.test(content)) {
    blocks.push('eval() 动态代码执行 — 代码注入风险，用 JSON.parse 或白名单替代');
  }

  // 命令注入（child_process.exec / os.system / subprocess 等）
  if (/\b(child_process\.exec|child_process\.spawn|os\.system|subprocess\.call|shell_exec|popen|Process\.Start)\s*\(\s*\$/.test(content)
      || /cmd\.exe\s.*\+|powershell\s.*\+/i.test(content)) {
    blocks.push('命令注入 — shell 命令拼接用户输入，用参数数组形式替代');
  }

  // ── WARN 级：中危模式 (stderr) ─────────────────────────────

  // XSS (v-html / innerHTML / dangerouslySetInnerHTML)
  if (/\b(innerHTML|dangerouslySetInnerHTML|v-html|outerHTML|document\.write)\s*[=({]/.test(content)) {
    warns.push('XSS 风险: 原始 HTML 插入 — 用户数据使用 textContent 或消毒后再渲染');
  }

  // 弱加密 (MD5 / SHA-1 用于安全场景)
  if (/(?:\bMD5\b|\bSHA-?1\b)/i.test(content)
      && /\b(hash|digest|encrypt|crypto|password|secret)\b/i.test(content)) {
    warns.push('弱加密: MD5/SHA-1 — 安全场景改用 SHA-256/512 或 bcrypt/scrypt');
  }

  // ── 输出 ──────────────────────────────────────────────────────
  if (blocks.length > 0) {
    console.error(`BLOCKED: 安全扫描发现 ${blocks.length} 个高危模式 in ${filePath}`);
    blocks.forEach((b, i) => console.error(`  [BLOCK ${i + 1}] ${b}`));
    console.error(`  修复后重新写入。误报时请人工审核后移除触发代码。`);
    process.exit(2);
  }

  if (warns.length > 0) {
    console.error(`SECURITY WARNING in ${filePath}:`);
    warns.forEach(w => console.error(`  ⚠ ${w}`));
  }
}

// ═══════════════════════════════════════════════════════════════
// L4: AI 开发态工作区隔离 — 拦截写入主仓库路径 (exit 2)
// ═══════════════════════════════════════════════════════════════
// 通过文件桥接读取 AI 开发上下文（由 AIDevelopmentPipelineService 写入）
const path = await import('path');
const AI_DEV_CONTEXT_PATH = path.join(process.cwd(), '.claude', 'ai-dev-context.json');
let aiDevContext = null;
try {
  const fs = await import('fs');
  if (fs.existsSync(AI_DEV_CONTEXT_PATH)) {
    const raw = fs.readFileSync(AI_DEV_CONTEXT_PATH, 'utf-8');
    aiDevContext = JSON.parse(raw);
  }
} catch {
  aiDevContext = null;
}

if (aiDevContext && aiDevContext.pipelineId) {
  const workspacePrefix = (aiDevContext.workspacePath || '').replace(/\\/g, '/');

  const allowedPatterns = [
    /StudioWorkspace[/\\]/,           // 工作区文件
    /\.claude[/\\]/,                  // 项目配置 + ai-dev-context
    /docs[/\\]/,                      // 设计文档
    /workspace[/\\]/,                 // 流水线 workspace 目录
  ];

  const normalizedPath = filePath.replace(/\\/g, '/');
  const isAllowed = allowedPatterns.some(p => p.test(filePath))
    || (workspacePrefix && normalizedPath.startsWith(workspacePrefix));

  if (!isAllowed) {
    console.error(`BLOCKED: AI 开发态禁止写入主仓库路径: ${filePath}`);
    console.error(`  当前 pipelineId: ${aiDevContext.pipelineId}`);
    console.error(`  工作区: ${aiDevContext.workspacePath}`);
    console.error(`  允许前缀: StudioWorkspace/, .claude/, docs/, workspace/, 工作区路径`);
    process.exit(2);
  }
}

process.exit(0);
