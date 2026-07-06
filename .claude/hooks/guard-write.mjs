#!/usr/bin/env node
/**
 * PreToolUse Hook — File Write Guardian (JNPF v5.2)
 *
 * 统一九层守卫（matcher: Write|Edit|MultiEdit）：
 *   L1 (exit 2): 禁止写入密钥/凭证/部署文件
 *   L2 (exit 2): 禁止清空源码文件（按扩展名匹配）
 *   L3 (分级):   通用安全扫描 — eval/命令注入(阻断) + XSS/弱加密(警告)
 *   L4 (exit 2): R5 模块边界 — OA 禁用 / IoT·MES 不存在
 *   L5 (exit 2): R4 多租户 — DisableGlobalFilter / 原生SQL无WHERE / Updateable无Where
 *   L6 (exit 2): R7 SQL 注入 — DROP/DELETE/SELECT/string.Format/Ado+$ 字符串拼接
 *   L7 (exit 2): R8 API 权限 — IDynamicApiController 无权限声明
 *   L8 (exit 2): R6 前端泄漏 — Timer/EventSource 无清理
 *   L9 (exit 2): AI 开发态工作区隔离 — pipeline 模式下限定可写前缀
 *
 * 输入：env var (CLAUDE_FILE_PATH/TOOL_NAME/TOOL_INPUT) + stdin fallback
 * 误报豁免：R4 用 // r4-safe: <理由>；R6 用 // r6-safe: <理由>
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

const isCs = /\.cs$/i.test(filePath);
const isFrontend = /\.(vue|ts|tsx|js|jsx)$/i.test(filePath);

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
    console.error(`  规则: L1 密钥/凭证/部署文件不可由 AI 写入`);
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
  console.error(`  规则: L2 禁止将源码文件覆盖为空内容`);
  process.exit(2);
}

// ═══════════════════════════════════════════════════════════════
// L3: 通用安全扫描（分级：阻断 / 警告）
//     SQL 注入已移至 L6 (R7) 全覆盖，本层只管 eval/命令注入/XSS/弱加密
// ═══════════════════════════════════════════════════════════════
const isInfraOrTestFile = /(?:\.claude[\\/]|[\\/]scripts[\\/]|[\\/]tests?[\\/]|\.test\.)/.test(filePath);

if (!isInfraOrTestFile && SOURCE_EXT.test(filePath) && typeof content === 'string' && content.trim()) {
  const blocks = [];
  const warns = [];

  // ── BLOCK 级：高危模式 (exit 2) ──────────────────────────────

  // 硬编码密钥/密码/Token（高置信度）
  if (/(?:api[_-]?key|apikey|secret|token|password|passwd|connectionString)\s*[:=]\s*['"][A-Za-z0-9_\-!@#$%^&*+=\/]{16,}['"]/i.test(content)) {
    blocks.push('硬编码密钥/密码/Token — 使用环境变量或密钥管理服务替代');
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

  // ── WARN 级：中危模式 (stderr，不阻断) ─────────────────────

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
    console.error(`BLOCKED: L3 安全扫描发现 ${blocks.length} 个高危模式 in ${filePath}`);
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
// L4: R5 模块边界 (exit 2)
//     OA 禁用 / IoT·MES 不存在 — 防幻觉 scaffold
// ═══════════════════════════════════════════════════════════════
{
  const p = filePath.toLowerCase();

  // OA 禁用区
  const oaPatterns = [
    /backend\/application\/jnpf\.oa\.api\.entry/,
    /backend\/modularity\/oa\//,
    /jnpf\.oa\.api\.entry\//,
  ];
  for (const pat of oaPatterns) {
    if (pat.test(p)) {
      console.error(`BLOCKED: 写入禁用模块 OA (R5) — ${filePath}`);
      console.error(`  JNPF.OA.API.Entry 模块已禁用，NEVER 修改。`);
      console.error(`  Rule: CLAUDE.md R5 + AGENTS.md "OA — disabled"。`);
      process.exit(2);
    }
  }

  // IoT / MES 不存在模块（仅 backend 路径，避免误伤前端含 iot/mes 字样的文件名）
  const isBackendPath = /backend\//.test(p) || isCs;
  if (isBackendPath) {
    const iotMatch = p.match(/backend\/(?:modularity|application)\/[^/]*\b(iot)\b[^/]*\//)
      || p.match(/jnpf\.(iot)\.api\.entry\//);
    if (iotMatch) {
      console.error(`BLOCKED: scaffold 不存在模块 IoT (R5) — ${filePath}`);
      console.error(`  JNPF 仓库中不存在 IoT 模块，NEVER scaffold（防 AI 幻觉）。`);
      console.error(`  Rule: CLAUDE.md R5 + AGENTS.md "IoT/MES modules don't exist"。`);
      process.exit(2);
    }
    const mesMatch = p.match(/backend\/(?:modularity|application)\/[^/]*\b(mes)\b[^/]*\//)
      || p.match(/jnpf\.(mes)\.api\.entry\//);
    if (mesMatch) {
      console.error(`BLOCKED: scaffold 不存在模块 MES (R5) — ${filePath}`);
      console.error(`  JNPF 仓库中不存在 MES 模块，NEVER scaffold。`);
      console.error(`  Rule: CLAUDE.md R5。`);
      process.exit(2);
    }
  }
}

// ═══════════════════════════════════════════════════════════════
// L5: R4 多租户隔离 (exit 2) — 仅 .cs
//     B1 原生SQL无WHERE / B2 DisableGlobalFilter / B3 Updateable·Deleteable 无Where 或 1=1
// ═══════════════════════════════════════════════════════════════
if (isCs && typeof content === 'string' && content.trim()) {
  const lines = content.split('\n');
  const issues = [];

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const trimmed = line.trim();
    if (trimmed.startsWith('//') || trimmed.startsWith('*') || trimmed.startsWith('/*')) continue;
    if (/r4-safe/i.test(line)) continue;

    // B1: 原生 SQL 查询无 WHERE（绕过 ITenantFilter）
    const rawSqlNoWhere = /(Ado\.SqlQuery|SqlQueryable|GetDataTable)\s*\(\s*\$?@?"[^"]*(?:SELECT|select)[^"]*(?:FROM|from)\s+\w+[^"]*"\s*\)/i.test(line)
      && !/where/i.test(line);
    if (rawSqlNoWhere) {
      issues.push({ line: i + 1, rule: 'R4-B1', detail: `原生 SQL 查询疑似无 WHERE 子句。\n  Ado.SqlQuery/SqlQueryable 绕过 ITenantFilter = 跨租户数据泄漏。\n  MUST 改用 Queryable<T>().Where(...) 或显式 .Where("TenantId = @tid", ...)` });
      continue;
    }

    // B2: 显式禁用租户全局过滤器
    if (/DisableGlobalFilter\s*\(\s*"?(TenantFilter|ITenantFilter|Tenant)"?\s*\)/i.test(line)) {
      issues.push({ line: i + 1, rule: 'R4-B2', detail: `显式禁用租户全局过滤器。\n  这会完全绕过跨租户隔离 = 数据泄漏。\n  除非 DBA 级跨租户管理，否则 NEVER。如确需，加 // r4-safe: <理由>` });
      continue;
    }

    // B3: Updateable/Deleteable 无 Where 或 .Where("1=1")
    const isUpdateDelete = /\.(Updateable|Deleteable)\s*</i.test(line);
    if (isUpdateDelete) {
      const block = lines.slice(i, Math.min(i + 6, lines.length)).join(' ');
      const hasWhere = /\.Where\s*\(/i.test(block);
      const hasOneEqualsOne = /['"]?\s*1\s*=\s*1\s*['"]?/.test(block);
      if (!hasWhere) {
        issues.push({ line: i + 1, rule: 'R4-B3', detail: `Updateable/Deleteable 链未发现 .Where()。\n  无 Where 的更新/删除 = 跨租户修改/删除全部数据。\n  MUST 链式调用 .Where(...) 限定租户范围` });
      } else if (hasOneEqualsOne) {
        issues.push({ line: i + 1, rule: 'R4-B3', detail: `Updateable/Deleteable 使用 .Where("1=1") = 全表操作。\n  MUST 改为限定租户的真实条件` });
      }
    }
  }

  if (issues.length > 0) {
    console.error(`BLOCKED: 多租户隔离风险 (R4) in ${filePath}`);
    for (const it of issues) console.error(`  [${it.rule}] 第 ${it.line} 行: ${it.detail}`);
    console.error(`  多租户是 JNPF 最严重安全红线。漏过滤 = 跨租户数据泄漏。`);
    console.error(`  修复后重写。误报时加 // r4-safe: <理由> 豁免。`);
    process.exit(2);
  }
}

// ═══════════════════════════════════════════════════════════════
// L6: R7 SQL 注入 (exit 2 / WARN) — 仅 .cs
//     DROP/TRUNCATE/DELETE/SELECT·INSERT·UPDATE 字符串插值 + string.Format(SQL) + Ado+$ +
//     WARN: 字符串拼接 + SQL 关键字
// ═══════════════════════════════════════════════════════════════
if (isCs && typeof content === 'string' && content.trim()) {
  const lines = content.split('\n');

  // BLOCK 级模式
  const blockPatterns = [
    { re: /\$"([^"]*\b(DROP\s+(TABLE|DATABASE|INDEX)|TRUNCATE\s+TABLE)\b[^"]*)"/i, tag: 'DROP/TRUNCATE via string interpolation' },
    { re: /\$"([^"]*\bDELETE\s+FROM\b[^"]*)"/i, tag: 'DELETE FROM via string interpolation' },
    { re: /\$"([^"]*\b(SELECT|INSERT\s+INTO|UPDATE\s+\w+\s+SET)\b[^"]*)"/i, tag: 'DML via string interpolation' },
    { re: /string\.Format\(\s*"[^"]*\b(SELECT|INSERT|UPDATE|DELETE|DROP)\b/i, tag: 'string.Format with SQL' },
    { re: /\b(Ado\.SqlQuery|Ado\.ExecuteCommand)\s*\(\s*\$"/i, tag: 'Ado.SqlQuery/ExecuteCommand with string interpolation' },
  ];
  for (const line of lines) {
    for (const { re, tag } of blockPatterns) {
      if (re.test(line)) {
        console.error(`BLOCKED: SQL injection risk — ${tag} in ${filePath}`);
        console.error(`  Offending line: ${line.trim().substring(0, 200)}`);
        console.error(`  Rule: .claude/rules/sql-safety.md — 用参数化查询或 SqlSugar LINQ 替代`);
        process.exit(2);
      }
    }
  }

  // WARN 级：字符串拼接 + SQL 关键字（不阻断）
  const concatSqlPattern = /"\s*\+\s*.*\b(SELECT|INSERT|UPDATE|DELETE|DROP)\b|SELECT.*"\s*\+/i;
  let warned = false;
  for (const line of lines) {
    if (concatSqlPattern.test(line) && !line.trim().startsWith('//') && !line.trim().startsWith('*')) {
      console.error(`WARNING: Potential SQL concatenation in ${filePath}`);
      console.error(`  Line: ${line.trim().substring(0, 200)}`);
      warned = true;
    }
  }
  if (warned) console.error(`  请确认使用参数化查询（WARN 不阻断）。`);
}

// ═══════════════════════════════════════════════════════════════
// L7: R8 API 权限声明 (exit 2) — 仅 .cs
//     IDynamicApiController 实现类 MUST 声明 [AllowAnonymous]/[SecurityDefine]/[Authorize]
// ═══════════════════════════════════════════════════════════════
if (isCs && typeof content === 'string' && content.trim()) {
  const hasApiController = /:\s*IDynamicApiController\b/i.test(content);
  const hasSecurityDefine = /\[SecurityDefine\]/i.test(content);
  const hasAllowAnonymous = /\[AllowAnonymous\]/i.test(content);
  const hasAuthorize = /\[Authorize\]/i.test(content);

  if (hasApiController && !hasSecurityDefine && !hasAllowAnonymous && !hasAuthorize) {
    console.error(`BLOCKED: IDynamicApiController 类缺少权限声明 in ${filePath}`);
    console.error(`  类实现了 IDynamicApiController 但未声明任何权限属性 (R8 红线)。`);
    console.error(`  MUST 在 class 声明上方添加以下其一：`);
    console.error(`    - [AllowAnonymous]              公开端点（登录、健康检查）`);
    console.error(`    - [SecurityDefine(\"权限码\")]    角色受限端点`);
    console.error(`    - [Authorize]                   已认证即可访问`);
    process.exit(2);
  }
}

// ═══════════════════════════════════════════════════════════════
// L8: R6 前端内存泄漏 (exit 2) — 仅 .vue/.ts/.tsx/.js/.jsx
//     Timer 无 clear / EventSource 无 onUnmounted·retry cap / onerror 直连
// ═══════════════════════════════════════════════════════════════
if (isFrontend && typeof content === 'string' && content.trim()) {
  // 提取 <script> 内容（去掉 template）
  let code = content;
  const scriptMatch = content.match(/<script[^>]*>([\s\S]*?)<\/script>/i);
  if (scriptMatch) code = scriptMatch[1];

  // r6-safe 整体豁免
  if (!/r6-safe/i.test(code)) {
    const issues = [];

    const hasSetTimeout = /\bsetTimeout\s*\(/.test(code);
    const hasSetInterval = /\bsetInterval\s*\(/.test(code);
    const hasClearTimeout = /\bclearTimeout\s*\(/.test(code);
    const hasClearInterval = /\bclearInterval\s*\(/.test(code);
    const hasOnUnmounted = /\bonUnmounted\s*[\(\{]/.test(code) || /\bonBeforeUnmount\s*[\(\{]/.test(code);

    if (hasSetTimeout && !hasClearTimeout && !hasOnUnmounted) {
      issues.push({ rule: 'R6.2', detail: '调用了 setTimeout() 但本文件未发现 clearTimeout() 或 onUnmounted()。\n  定时器返回值 MUST 保存并在 onUnmounted 中清除 → 否则内存泄漏' });
    }
    if (hasSetInterval && !hasClearInterval && !hasOnUnmounted) {
      issues.push({ rule: 'R6.2', detail: '调用了 setInterval() 但本文件未发现 clearInterval() 或 onUnmounted()。\n  interval 不会自动停止 → 严重内存泄漏' });
    }

    const hasEventSource = /\bnew\s+EventSource\s*\(/.test(code);
    if (hasEventSource) {
      if (!hasOnUnmounted) {
        issues.push({ rule: 'R6.2', detail: '创建了 new EventSource() 但本文件未发现 onUnmounted()。\n  EventSource MUST 在组件销毁时 .close()' });
      }
      const hasRetryCap = /MAX_RETRIES|maxRetries|retryCount|reconnectLimit/i.test(code);
      if (!hasRetryCap) {
        issues.push({ rule: 'R6.3', detail: 'EventSource 重连未发现 retry 上限（MAX_RETRIES/maxRetries/retryCount）。\n  MUST 有重试上限，否则网络故障时无限重连 → 浏览器卡死' });
      }
      const onerrorDirectReconnect = /onerror\s*[:=]\s*(?:function\s*)?\(?[^)]*\)?\s*=>?\s*\{[\s\S]*?\b(connect|reconnect)\s*\(/i.test(code);
      if (onerrorDirectReconnect) {
        issues.push({ rule: 'R6.4', detail: 'EventSource.onerror 中疑似直接同步调用 connect()/reconnect() = busy loop。\n  MUST 用 setTimeout(() => connect(), delay) + 计数器实现指数退避' });
      }
    }

    if (issues.length > 0) {
      console.error(`BLOCKED: 前端内存泄漏风险 (R6) in ${filePath}`);
      for (const it of issues) console.error(`  [${it.rule}] ${it.detail}`);
      console.error(`  完整规则见 .claude/rules/frontend-memory-leak.md（6 条铁律）。`);
      console.error(`  误报时加 // r6-safe: <理由> 后重写。`);
      process.exit(2);
    }
  }
}

// ═══════════════════════════════════════════════════════════════
// L9: AI 开发态工作区隔离 — pipeline 模式下限定可写前缀 (exit 2)
// ═══════════════════════════════════════════════════════════════
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
    /StudioWorkspace[/\\]/,
    /\.claude[/\\]/,
    /docs[/\\]/,
    /workspace[/\\]/,
  ];

  const normalizedPath = filePath.replace(/\\/g, '/');
  const isAllowed = allowedPatterns.some(p => p.test(normalizedPath))
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
