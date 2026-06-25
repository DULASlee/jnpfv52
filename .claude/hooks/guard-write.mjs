#!/usr/bin/env node
/**
 * PreToolUse Hook — Unified Write Guardian (JNPF V3.0)
 *
 * 吸收 V1.0 的 6 个独立守卫为统一八层检查，单次 stdin 读取，单 pass 完成。
 *
 * 八层防护：
 *   L1 (exit 2): 禁止写入密钥/凭证/部署文件
 *   L2 (exit 2): 禁止清空源码文件
 *   L3 (exit 2): 安全模式扫描 — 高危阻断 + 中危警告
 *   L4 (exit 2): R5 模块边界 — OA禁用 + IoT/MES不存在
 *   L5 (exit 2): R4 多租户隔离 — SQL/Updateable/Deleteable
 *   L6 (exit 2): R7 SQL注入 — DROP/DELETE/SELECT/INSERT拼接
 *   L7 (exit 2): R8 API权限 — IDynamicApiController无权限声明
 *   L8 (exit 2): R6 前端内存泄漏 — setTimeout/EventSource无清理
 *
 * 合并收益：6进程×50ms→1进程×50ms，stdin读取从6次→1次，JSON.parse从6次→1次
 */

// ─── 输入解析（stdin 一次读取，所有检查共享）─────────────────
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

const isCsFile = /\.cs$/i.test(filePath);
const isVueOrTsFile = /\.(vue|tsx|ts|jsx|js)$/i.test(filePath);
const blocks = [];

// ═══════════════════════════════════════════════════════════════
// L1: 禁止写入敏感文件
// ═══════════════════════════════════════════════════════════════
const FORBIDDEN = [
  /\.env(\.[a-zA-Z0-9]+)?$/, /\.pem$/, /\.key$/, /\.p12$/, /\.pfx$/, /\.crt$/, /\.cer$/,
  /id_rsa/, /id_ecdsa/, /id_ed25519/,
  /docker-compose\.ya?ml$/i, /Dockerfile$/i, /\.github\/workflows\//,
];
for (const p of FORBIDDEN) {
  if (p.test(filePath)) {
    blocks.push(`[L1] 禁止写入受保护文件: ${filePath}`);
  }
}

// ═══════════════════════════════════════════════════════════════
// L2: 禁止清空源码文件
// ═══════════════════════════════════════════════════════════════
const SOURCE_EXT = /\.(ts|tsx|js|jsx|mjs|cjs|vue|svelte|cs|py|go|rs|java)$/i;
if (toolName === 'Write' && typeof content === 'string'
    && content.trim() === '' && SOURCE_EXT.test(filePath)) {
  blocks.push(`[L2] 试图清空源文件: ${filePath}`);
}

// ═══════════════════════════════════════════════════════════════
// L3-L8: 内容检查（需有内容且非基础设施/测试文件）
// ═══════════════════════════════════════════════════════════════
const isInfraOrTest = /(?:\.claude[\\/]|[\\/]scripts[\\/]|[\\/]tests?[\\/]|\.test\.)/.test(filePath);
if (!isInfraOrTest && SOURCE_EXT.test(filePath) && typeof content === 'string' && content.trim()) {
  const lines = content.split('\n');

  // ── L3: 安全模式扫描 ──────────────────────────────────────
  // 硬编码密钥
  if (/(?:api[_-]?key|apikey|secret|token|password|passwd|connectionString)\s*[:=]\s*['"][A-Za-z0-9_\-!@#$%^&*+=\/]{16,}['"]/i.test(content)) {
    blocks.push('[L3] 硬编码密钥/密码/Token');
  }
  // eval()
  if (/\beval\s*\(/.test(content)) {
    blocks.push('[L3] eval() 动态代码执行');
  }
  // 命令注入
  if (/\b(child_process\.exec|child_process\.spawn|os\.system|subprocess\.call|Process\.Start)\s*\(\s*\$/.test(content)) {
    blocks.push('[L3] 命令注入 — shell命令拼接用户输入');
  }

  // ── L4: R5 模块边界 (.cs only) ────────────────────────────
  if (isCsFile) {
    const oaPatterns = [/backend\/application\/jnpf\.oa\.api\.entry/i, /backend\/modularity\/oa\//i, /jnpf\.oa\.api\.entry\//i];
    for (const pat of oaPatterns) {
      if (pat.test(filePath)) { blocks.push('[L4] R5 — 写入禁用模块 OA'); break; }
    }
    // IoT/MES 防幻觉
    if (/backend\/(?:modularity|application)\/[^/]*\b(iot|mes)\b[^/]*\//i.test(filePath)
        || /jnpf\.(iot|mes)\.api\.entry\//i.test(filePath)) {
      blocks.push('[L4] R5 — scaffold 不存在模块 IoT/MES');
    }
  }

  // ── L5: R4 多租户隔离 (.cs only) ──────────────────────────
  if (isCsFile) {
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const trimmed = line.trim();
      if (trimmed.startsWith('//') || trimmed.startsWith('*') || /r4-safe/i.test(line)) continue;

      // DisableGlobalFilter
      if (/DisableGlobalFilter\s*\(\s*"?(TenantFilter|ITenantFilter|Tenant)"?\s*\)/i.test(line)) {
        blocks.push(`[L5] R4 — 第${i+1}行: DisableGlobalFilter(TenantFilter)`);
        continue;
      }
      // 原生SQL无WHERE
      if (/(Ado\.SqlQuery|SqlQueryable|GetDataTable)\s*\(\s*\$?@?"[^"]*(?:SELECT|select)[^"]*(?:FROM|from)\s+\w+[^"]*"\s*\)/i.test(line) && !/where/i.test(line)) {
        blocks.push(`[L5] R4 — 第${i+1}行: 原生SQL无WHERE子句`);
        continue;
      }
      // Updateable/Deleteable无Where
      if (/\.(Updateable|Deleteable)\s*</i.test(line)) {
        const block = lines.slice(i, Math.min(i + 6, lines.length)).join(' ');
        if (!/\.Where\s*\(/i.test(block)) {
          blocks.push(`[L5] R4 — 第${i+1}行: Updateable/Deleteable无.Where()`);
        }
      }
    }
  }

  // ── L6: R7 SQL注入 (.cs only) ──────────────────────────────
  if (isCsFile) {
    for (const line of lines) {
      if (/\$"([^"]*\b(DROP\s+(TABLE|DATABASE|INDEX)|TRUNCATE\s+TABLE)\b[^"]*)"/i.test(line)) {
        blocks.push(`[L6] R7 — SQL注入: DROP/TRUNCATE拼接`);
        break;
      }
      if (/\$"([^"]*\bDELETE\s+FROM\b[^"]*)"/i.test(line)) {
        blocks.push(`[L6] R7 — SQL注入: DELETE FROM拼接`);
        break;
      }
      if (/\$"([^"]*\b(SELECT|INSERT\s+INTO|UPDATE\s+\w+\s+SET)\b[^"]*)"/i.test(line)) {
        blocks.push(`[L6] R7 — SQL注入: DML拼接`);
        break;
      }
    }
    if (/string\.Format\(\s*"[^"]*\b(SELECT|INSERT|UPDATE|DELETE|DROP)\b/i.test(content)) {
      blocks.push('[L6] R7 — SQL注入: string.Format(SQL)');
    }
    if (/\b(Ado\.SqlQuery|Ado\.ExecuteCommand)\s*\(\s*\$"/i.test(content)) {
      blocks.push('[L6] R7 — SQL注入: Ado+$');
    }
  }

  // ── L7: R8 API权限 (.cs only, 新增IDynamicApiController) ──
  if (isCsFile && /:\s*IDynamicApiController\b/i.test(content)) {
    if (!/\[SecurityDefine\]/i.test(content)
        && !/\[AllowAnonymous\]/i.test(content)
        && !/\[Authorize\]/i.test(content)) {
      blocks.push('[L7] R8 — IDynamicApiController缺少权限声明');
    }
  }

  // ── L8: R6 前端内存泄漏 (.vue/.ts only) ────────────────────
  if (isVueOrTsFile) {
    let code = content;
    const scriptMatch = content.match(/<script[^>]*>([\s\S]*?)<\/script>/i);
    if (scriptMatch) code = scriptMatch[1];

    const hasSetTimeout = /\bsetTimeout\s*\(/.test(code);
    const hasSetInterval = /\bsetInterval\s*\(/.test(code);
    const hasClearTimeout = /\bclearTimeout\s*\(/.test(code);
    const hasClearInterval = /\bclearInterval\s*\(/.test(code);
    const hasOnUnmounted = /\bonUnmounted\s*[\(\{]/.test(code) || /\bonBeforeUnmount\s*[\(\{]/.test(code);

    if (hasSetTimeout && !hasClearTimeout && !hasOnUnmounted) {
      blocks.push('[L8] R6 — setTimeout无clearTimeout/onUnmounted');
    }
    if (hasSetInterval && !hasClearInterval && !hasOnUnmounted) {
      blocks.push('[L8] R6 — setInterval无clearInterval/onUnmounted');
    }

    const hasEventSource = /\bnew\s+EventSource\s*\(/.test(code);
    if (hasEventSource) {
      if (!hasOnUnmounted) {
        blocks.push('[L8] R6 — EventSource无onUnmounted清理');
      }
      if (!/MAX_RETRIES|maxRetries|retryCount|reconnectLimit/i.test(code)) {
        blocks.push('[L8] R6 — EventSource无retry上限');
      }
    }
  }
}

// ═══════════════════════════════════════════════════════════════
// 输出决策
// ═══════════════════════════════════════════════════════════════
if (blocks.length > 0) {
  console.error(`BLOCKED: ${blocks.length} 个安全问题 in ${filePath}`);
  blocks.forEach(b => console.error(`  ${b}`));
  console.error(`  修复后重新写入。误报时检查对应红线豁免注释。`);
  process.exit(2);
}

process.exit(0);
