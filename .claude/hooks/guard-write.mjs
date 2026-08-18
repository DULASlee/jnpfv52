#!/usr/bin/env node
/**
 * PreToolUse Hook — File Write Guardian (JNPF v5.2)
 *
 * 统一十层守卫（matcher: Write|Edit|MultiEdit）：
 *   L1  (exit 2): 禁止写入密钥/凭证/部署文件
 *   L2  (exit 2): 禁止清空源码文件（按扩展名匹配）
 *   L3  (分级):   通用安全扫描 — eval/命令注入(阻断) + XSS/弱加密(警告)
 *   L4  (exit 2): R5 模块边界 — OA 禁用 / IoT·MES 不存在
 *   L5  (exit 2): R4 多租户 — DisableGlobalFilter / 原生SQL无WHERE / Updateable无Where
 *   L6  (exit 2): R7 SQL 注入 — DROP/DELETE/SELECT/string.Format/Ado+$ 字符串拼接
 *   L7  (exit 2): R8 API 权限 — IDynamicApiController 无权限声明
 *   L8  (exit 2): R6 前端泄漏 — Timer/EventSource 无清理
 *   L9  (exit 2): AI 开发态工作区隔离 — pipeline 模式下限定可写前缀
 *   L10 (exit 2): 需求分析子链铁律 — CR审批/废止模块/mjs禁止/第二源（req-analysis-iron-law.md）
 *   L11 (exit 2): 零占位符硬失败 — TODO implement / NotImplementedException / placeholder 假实现
 *   L12 (exit 2): ADF 写入锁 — adfPhase 为 P0–P3（或 currentSg 未升 P4）时禁止写业务源码
 *   L13 (exit 2): 降级/兜底硬拦截 — inteAssistant .cs 禁止新增 LLM 降级/兜底（33、降级兜底硬禁令开发计划）
 *
 * 输入：env var (CLAUDE_FILE_PATH/TOOL_NAME/TOOL_INPUT) + stdin fallback
 * 误报豁免：R4 用 // r4-safe: <理由>；R6 用 // r6-safe: <理由>；L10 用 // cr-safe: <理由>
 *           L11 用 // placeholder-ok: <理由>
 *           L12：workflow-state adfPhase=P4|exempt
 *           L13 用 // degradation-ok: <理由>
 */

import { scanFileContent } from './placeholder-scan.mjs';
import { checkAdfWrite } from './adf-gate-lib.mjs';

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
const fsSync = await import('fs');
const AI_DEV_CONTEXT_PATH = path.join(process.cwd(), '.claude', 'ai-dev-context.json');
let aiDevContext = null;
try {
  if (fsSync.existsSync(AI_DEV_CONTEXT_PATH)) {
    const raw = fsSync.readFileSync(AI_DEV_CONTEXT_PATH, 'utf-8');
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

// ═══════════════════════════════════════════════════════════════
// L10: 需求分析子链铁律 — CR 审批 + 废止模块 + mjs 禁止 + 数据第二源 (exit 2)
//   规则来源：.claude/rules/req-analysis-iron-law.md（七禁令）
//   误报豁免：// cr-safe: <理由>（禁令六/七）；新文件豁免无（禁令一）
// ═══════════════════════════════════════════════════════════════
{
  // 统一豁免标记（cr-safe 覆盖 L10a/L10b/L10d 业务规则违规）
  const hasCrSafe = /\/\/\s*cr-safe\s*:/i.test(content);

  // ── L10c: 禁止新增 .mjs 文件（除 hooks 目录）── 禁令一 ──
  if (/\.mjs$/i.test(filePath)) {
    const isHookFile = /(?:^|[\\/])\.claude[\\/]hooks[\\/]/.test(filePath)
      || /(?:^|[\\/])\.cursor[\\/]hooks[\\/]/.test(filePath);
    if (!isHookFile) {
      console.error(`BLOCKED: 禁止新增 .mjs 脚本 (需求分析子链铁律·禁令一) — ${filePath}`);
      console.error(`  规则：.claude/rules/req-analysis-iron-law.md 禁令一`);
      console.error(`  原因：严禁用 mjs 做 E2E/冒烟测试；后端测试用 xUnit(backend/tests/)，前端用 Vitest .ts`);
      console.error(`  现有 .mjs 冻结逐步迁移；仅 .claude/hooks/ 和 .cursor/hooks/ 允许新增 hook 基础设施`);
      process.exit(2);
    }
  }

  // ── L10b: 禁止复活废止模块（.cs 文件）── 禁令七 ──
  if (isCs && !hasCrSafe) {
    const prohibitedPatterns = [
      { re: /\bclass\s+ScannerValidator\b/, label: 'ScannerValidator（25 §0.2 废止 → LightStructureValidator）' },
      { re: /\bclass\s+EventDependencyBuilder\b/, label: 'EventDependencyBuilder（25 §0.2 废止 → 全量重编译）' },
      { re: /\bclass\s+PSpecEnhancer\b/, label: 'PSpecEnhancer（25 §0.2 废止 → Round2 联合 LLM）' },
      { re: /\bclass\s+DecisionTableEnhancer\b/, label: 'DecisionTableEnhancer（25 §0.2 废止 → Round2 联合 LLM）' },
      { re: /\bcascadeUpdate\s*\(/, label: 'cascadeUpdate（25 决策7 废止 → 全量重编译）' },
      { re: /\bISaStepEnhancer\b/, label: 'ISaStepEnhancer（25 §0.2 废止 → Assumption+StepEnhancement record）' },
      { re: /\bNoopEnhancer\b/, label: 'NoopEnhancer（25 §0.2 废止）' },
    ];
    for (const { re, label } of prohibitedPatterns) {
      if (re.test(content)) {
        console.error(`BLOCKED: 禁止复活废止模块 (需求分析子链铁律·禁令七) — ${filePath}`);
        console.error(`  命中：${label}`);
        console.error(`  规则：25 §0.2 v1.2→v2.1 废止清单 + .claude/rules/req-analysis-iron-law.md 禁令七`);
        console.error(`  修复：使用 25 号标注的现行替代方案；确需例外加 // cr-safe: <理由> 并提交 CR`);
        process.exit(2);
      }
    }
    // 普通 SINGLE 题型赋值（排除 MATRIX_SINGLE / MATRIX_MULTI）
    const singleAssign = content.match(/QuestionFormat\s*=\s*["']SINGLE["']/g);
    if (singleAssign) {
      const matrixSingle = content.match(/QuestionFormat\s*=\s*["']MATRIX_SINGLE["']/g);
      if (singleAssign.length > (matrixSingle ? matrixSingle.length : 0)) {
        console.error(`BLOCKED: 禁止普通 SINGLE 题型 (需求分析子链铁律·禁令七) — ${filePath}`);
        console.error(`  规则：25 红线1 + 31 D-E — 仅允许 MULTI / MATRIX_SINGLE / MATRIX_MULTI`);
        console.error(`  修复：改为 MULTI 或 MATRIX_SINGLE；确需例外加 // cr-safe: <理由>`);
        process.exit(2);
      }
    }
  }

  // ── L10b-sql: 禁止新建 sa_ddd_* 表（DDL 文件）── 禁令七 ──
  if (/\.(sql)$/i.test(filePath) && !hasCrSafe) {
    if (/CREATE\s+TABLE\s+sa_ddd/i.test(content)) {
      console.error(`BLOCKED: 禁止新建 sa_ddd_* 表 (需求分析子链铁律·禁令七) — ${filePath}`);
      console.error(`  规则：25 决策5/红线5 — DDD 不落表，渲染时 DddProjection 实时推导`);
      console.error(`  修复：删除 CREATE TABLE sa_ddd_*；用 Studio/DddProjection.cs 渲染时推导`);
      process.exit(2);
    }
    if (/CREATE\s+TABLE\s+sa_scanner_validation/i.test(content)) {
      console.error(`BLOCKED: 禁止新建 sa_scanner_validation 表 (需求分析子链铁律·禁令七) — ${filePath}`);
      console.error(`  规则：25 §0.2 废止 — 轻量校验器输出是内存列表，不需要表`);
      process.exit(2);
    }
    if (/CREATE\s+TABLE\s+sa_event_dependencies/i.test(content)) {
      console.error(`BLOCKED: 禁止新建 sa_event_dependencies 表 (需求分析子链铁律·禁令七) — ${filePath}`);
      console.error(`  规则：25 §0.2 废止 — 全量重编译，无增量依赖图`);
      process.exit(2);
    }
  }

  // ── L10a: 关键业务方法变更需 CR 审批（.cs 文件）── 禁令六 ──
  if (isCs && !hasCrSafe) {
    // 关键业务文件保护清单（路径模式匹配）
    const protectedFiles = [
      /Skills[\\/]PmSkillService\.cs$/i,
      /Skills[\\/]RequirementAnalysisOrchestrator\.cs$/i,
      /Skills[\\/]AnalystSkillService\.cs$/i,
      /Skills[\\/]SkillsApiService\.cs$/i,
      /Skills[\\/]DesignSkillOrchestrator\.cs$/i,
      /Gates[\\/]AnalysisFinalizedGate\.cs$/i,
      /Gates[\\/]QualityScoreCalculator\.cs$/i,
      /Gates[\\/]ConsistencyChecker\.cs$/i,
    ];
    const isProtected = protectedFiles.some(p => p.test(filePath));

    if (isProtected) {
      // 读 workflow-state.json 的 cr-approved 字段
      let crApproved = null;
      try {
        const wfPath = path.join(process.cwd(), '.claude', 'workflow-state.json');
        if (fsSync.existsSync(wfPath)) {
          const wf = JSON.parse(fsSync.readFileSync(wfPath, 'utf-8'));
          crApproved = wf['cr-approved'] || (wf.sp && wf.sp['cr-approved']) || null;
        }
      } catch {
        crApproved = null;
      }

      if (!crApproved) {
        console.error(`BLOCKED: 关键业务方法修改未经 CR 审批 (需求分析子链铁律·禁令六) — ${filePath}`);
        console.error(`  规则：.claude/rules/req-analysis-iron-law.md 禁令六`);
        console.error(`  此文件在"关键业务方法保护清单"中，修改前 MUST：`);
        console.error(`    1. 在 .claude/change-requests/ 写 CR-{日期}-{NN}.md（目标方法/原因/对照决策/影响）`);
        console.error(`    2. 提交用户审批`);
        console.error(`    3. 批准后在 .claude/workflow-state.json 写入 "cr-approved": "CR-XXXXXXXX-NN"`);
        console.error(`  纯格式/注释修改：写内容含 // cr-safe: <理由> 即可豁免`);
        process.exit(2);
      }
    }
  }

  // ── L10d: 数据第二源检测（.cs 文件）── 禁令二 ──
  if (isCs && !hasCrSafe) {
    // 编排器直连 _llm.ChatAsync 出题（25 红线9 / 31 D-D）
    if (/RequirementAnalysisOrchestrator\.cs$/i.test(filePath)) {
      // 检测：编排器内 _llm.ChatAsync 且上下文含 Clarif/Question/GenerateRound（出题相关）
      const llmQuestion = content.match(/_llm\s*\.\s*ChatAsync\s*\([^)]*(?:[Cc]larif|[Qq]uestion|[Rr]ound)/);
      if (llmQuestion) {
        console.error(`BLOCKED: 编排器禁止直连 LLM 出题 (需求分析子链铁律·禁令一/七) — ${filePath}`);
        console.error(`  命中：_llm.ChatAsync 出题调用 — 掏空 PM 专家职责（25 红线9 / 31 D-D）`);
        console.error(`  规则：出题 MUST 经 PmSkillService.GenerateClarificationAsync`);
        console.error(`  修复：删除编排器出题 _llm 调用，改调 pmSkill.GenerateClarificationAsync`);
        console.error(`  确需例外加 // cr-safe: <理由> 并提交 CR`);
        process.exit(2);
      }
    }

    // ── L10e: 非编排器直接调用 analyst-skill / pm-skill（.cs 文件）── 禁令六 ──
    if (isCs && !hasCrSafe) {
      const isOrchestrator = /RequirementAnalysisOrchestrator\.cs$/i.test(filePath)
        || /DesignSkillOrchestrator\.cs$/i.test(filePath);
      if (!isOrchestrator) {
        // 检测 _harness.RunAsync("analyst-skill") 或 harness.RunAsync("pm-skill")
        const directSkillCall = content.match(
          /(?:harness|_harness)\s*\.\s*RunAsync\s*\(\s*"(?:analyst-skill|pm-skill)"/
        );
        if (directSkillCall) {
          console.error(`BLOCKED: 非编排器代码禁止直接调用 analyst-skill/pm-skill (需求分析子链铁律·禁令六) — ${filePath}`);
          console.error(`  命中：${directSkillCall[0].trim()} — 绕过三轮编排器`);
          console.error(`  规则：25 号方案决策1/2 — DemandAnalysisSkill 调度 MUST 经 RequirementAnalysisOrchestrator`);
          console.error(`  修复：删除直接调用，改走 POST /api/studio/skills/requirement-analysis/{pipelineId}/run`);
          console.error(`  确需例外加 // cr-safe: <理由> 并提交 CR`);
          process.exit(2);
        }
      }
    }
  }
}

// ═══════════════════════════════════════════════════════════════
// L11: 零占位符硬失败（ADF / engineering-laws Law 4）
// ═══════════════════════════════════════════════════════════════
{
  const hits = scanFileContent(filePath, content);
  if (hits.length > 0) {
    console.error(`BLOCKED: 零占位符硬失败 (L11) — ${filePath}`);
    for (const h of hits) {
      console.error(`  L${h.line} [${h.rule}] ${h.match}`);
    }
    console.error(`  规则：.claude/rules/architecture-design-interface-first.md + engineering-laws Law 4`);
    console.error(`  修复：完成实现后再写入；确属例外加 // placeholder-ok: <理由>`);
    process.exit(2);
  }
}

// ═══════════════════════════════════════════════════════════════
// L12: ADF 写入锁（P0–P3 / currentSg 未升 P4）
// ═══════════════════════════════════════════════════════════════
{
  const adf = checkAdfWrite(filePath);
  if (adf.block) {
    console.error(`BLOCKED: ADF 写入锁 (L12) — ${filePath}`);
    console.error(`  ${adf.reason}`);
    console.error(`  状态文件：.claude/workflow-state.json → adfPhase`);
    process.exit(2);
  }
}

// ═══════════════════════════════════════════════════════════════
// L13: 降级/兜底硬拦截 (exit 2) — 仅 inteAssistant .cs 业务源码
//     禁止新增任何 LLM 降级/兜底/fallback；LLM 失败 MUST 抛 Oops.Bah()。
//     误报豁免：// degradation-ok: <理由>
//     存量清理标记：写入 throw Oops.Bah 替换的代码，不会触发本规则
//     （因为 throw 不是降级；规则只拦 return 伪成功 / 返回兜底对象 / 静默吞异常）
// ═══════════════════════════════════════════════════════════════
if (isCs && typeof content === 'string' && content.trim()) {
  const isInteAssistant = /inteAssistant/i.test(filePath);
  if (isInteAssistant && !/degradation-ok:/i.test(content)) {
    const lines = content.split('\n');
    const violations = [];

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const trimmed = line.trim();
      // 跳过注释行
      if (trimmed.startsWith('//') || trimmed.startsWith('*') || trimmed.startsWith('/*')) continue;
      // 行级豁免标记
      if (/degradation-ok:/i.test(line)) continue;

      // B1: "降级" 关键词出现在代码中（非注释）— 仅当行同时含 return/= 或 throw 时才视为可疑
      //     豁免：throw Oops.Bah(..."LLM xxx 降级..." ) 里含"降级"是错误信息文本，允许
      if (/降级/.test(line) && !/throw\s+Oops\.(Bah|Oh)/i.test(line)
          && /\breturn\b|\b=\s*new\b|Status\s*=\s*"completed"/i.test(line)) {
        violations.push({
          line: i + 1,
          rule: 'L13-B1',
          detail: `代码含"降级"且返回伪成功/兜底对象。禁止新增 LLM 降级/兜底逻辑。\n  LLM 失败 MUST 抛 Oops.Bah()。\n  豁免：// degradation-ok: <理由> 或 throw Oops.Bah(..."降级...") 错误信息`
        });
      }

      // B2: 新增 BuildFallback* 方法调用（非定义处）
      //     方法定义 (`private ... BuildFallbackXxx(...) =>`) 也要拦——禁止创建新兜底构建器
      if (/\bBuildFallback\w+\s*\(/.test(line) && !/throw\s+Oops/i.test(line)) {
        violations.push({
          line: i + 1,
          rule: 'L13-B2',
          detail: `新增 BuildFallback* 方法。禁止创建/调用兜底构建器。\n  LLM 失败 MUST 抛异常。`
        });
      }

      // B3: catch (JsonException) 后直接 return 兜底对象（而非 throw）
      if (/catch\s*\(\s*JsonException\b/i.test(line)) {
        const nextLines = lines.slice(i + 1, Math.min(i + 6, lines.length)).join('\n');
        // 如果 catch 块内是 throw，允许；如果是 return + 含降级/fallback 关键词，拦
        if (!/throw\s+Oops/i.test(nextLines)
            && /\breturn\s+(new\s+)?/i.test(nextLines)
            && /降级|fallback|兜底|默认值|默认题|BuildFallback/i.test(nextLines)) {
          violations.push({
            line: i + 1,
            rule: 'L13-B3',
            detail: `JsonException 捕获后返回兜底对象。JSON 解析失败 MUST 抛 Oops.Bah()。`
          });
        }
      }

      // B4: !response.IsSuccess → return new XxxResult { Status = "completed" } 伪成功
      if (/!\w*\.IsSuccess\b/.test(trimmed) && !/throw\s+Oops/i.test(trimmed)) {
        const nextLines = lines.slice(i + 1, Math.min(i + 10, lines.length)).join('\n');
        if (/Status\s*=\s*"completed"/i.test(nextLines)
            && /\breturn\s+new\s+\w+/i.test(nextLines)
            && !/throw\s+Oops/i.test(nextLines)) {
          violations.push({
            line: i + 1,
            rule: 'L13-B4',
            detail: `LLM !IsSuccess 后返回 Status="completed" 伪成功。LLM 失败 MUST 抛异常。`
          });
        }
      }

      // B5: catch (Exception ...) when (...) 后 log + continue/skip（静默吞异常）
      //     仅当 catch 块不含 throw，且含 Log + continue/跳过/降级 关键词时触发
      //     豁免：返回 Status="failed"/"error" 或 IsSuccess=false 是错误传播不是降级
      if (/catch\s*\(\s*Exception\b.*\)\s*(?:when\s*\([^)]*\))?\s*\{?/i.test(line)) {
        const block = lines.slice(i, Math.min(i + 8, lines.length)).join('\n');
        if (!/throw\s+Oops/i.test(block)
            && /Log(Warning|Error)/i.test(block)
            && /continue|return|跳过|降级|不阻断/i.test(block)
            && !/OutOfMemoryException|OperationCanceledException/i.test(line)
            && !/Status\s*=\s*"(failed|error)"/i.test(block)
            && !/IsSuccess\s*=\s*false/i.test(block)) {
          violations.push({
            line: i + 1,
            rule: 'L13-B5',
            detail: `catch (Exception) 后静默 Log + continue/return 吞异常。LLM 异常 MUST 向上传播或抛 Oops.Bah()。\n  豁免：返回 Status="failed"/"error" 或 IsSuccess=false 是错误传播，加 // degradation-ok: 可豁免`
          });
        }
      }
    }

    if (violations.length > 0) {
      console.error(`BLOCKED: 降级/兜底代码 (L13) — ${filePath}`);
      for (const v of violations) {
        console.error(`  L${v.line} [${v.rule}]: ${v.detail}`);
      }
      console.error(`  规则：需求分析子链铁律 §降级/兜底禁令 + 33、降级兜底硬禁令开发计划`);
      console.error(`  LLM 失败 MUST 抛 Oops.Bah()，禁止返回降级/兜底/默认结果。`);
      console.error(`  误报豁免：在该行或文件头加 // degradation-ok: <理由>`);
      process.exit(2);
    }
  }
}

process.exit(0);
