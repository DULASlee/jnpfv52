/**
 * 占位符硬失败扫描（全项目共享）
 * 供：guard-write L11、Cursor guard-placeholder、.githooks/pre-commit
 *
 * 豁免：内容含 `// placeholder-ok: <理由>` 或 `# placeholder-ok: <理由>`
 * 排除：docs / rules / hooks / 测试夹具（避免自测与文档误杀）
 */
import { execSync } from 'child_process';
import { readFileSync, existsSync } from 'fs';
import { resolve } from 'path';

/** @typedef {{ line: number, match: string, rule: string }} PlaceholderHit */

const PATTERNS = [
  { rule: 'TODO-implement', re: /(?:\/\/|#)\s*TODO\s*:?\s*implement\b/i },
  { rule: 'rest-of-code', re: /(?:\/\/|#)\s*rest of the code\b/i },
  { rule: 'placeholder-comment', re: /(?:\/\/|#)\s*.*\bplaceholder\b/i },
  { rule: 'not-implemented-throw', re: /\bthrow\s+new\s+NotImplementedException\b/ },
  { rule: 'not-implemented-error', re: /\bthrow\s+new\s+Error\s*\(\s*['"`](?:TODO|Not implemented|not implemented)/i },
  { rule: 'return-null-placeholder', re: /\breturn\s+null\s*;\s*\/\/\s*.*placeholder/i },
  { rule: 'return-empty-placeholder', re: /\breturn\s+(?:\[\s*\]|\{\s*\})\s*;\s*\/\/\s*.*placeholder/i },
  { rule: 'pass-todo', re: /\bpass\s+#\s*TODO\b/i },
  { rule: 'ellipsis-stub', re: /(?:\/\/|#)\s*\.\.\.\s*(?:implement|stub|placeholder)\b/i },
];

const EXEMPT_RE = /(?:\/\/|#)\s*placeholder-ok\s*:/i;

/** 仅扫描业务源码路径 */
export function isScannablePath(filePath) {
  const p = (filePath || '').replace(/\\/g, '/');
  if (!p) return false;

  // 排除区
  const exclude = [
    /(^|\/)\.claude\//,
    /(^|\/)\.cursor\//,
    /(^|\/)docs\//,
    /(^|\/)openspec\//,
    /(^|\/)workspace\//,
    /(^|\/)node_modules\//,
    /(^|\/)\.git\//,
    /(^|\/)graphify-out\//,
    /\/__tests__\//,
    /\/fixtures\//,
    /\.test\.(ts|tsx|js|mjs|cs)$/i,
    /\.spec\.(ts|tsx|js|mjs)$/i,
    /Tests\.cs$/i,
    /test-hooks\.mjs$/i,
    /placeholder-scan\.mjs$/i,
    /guard-placeholder\.mjs$/i,
    /\.md$/i,
    /\.mdc$/i,
    /\.json$/i,
    /\.yaml$/i,
    /\.yml$/i,
    /\.sql$/i,
    /\.http$/i,
  ];
  if (exclude.some((re) => re.test(p))) return false;

  // 包含区：后端 / 前端 / sa-service / 根 scripts 业务库（不含 hooks）
  const include = [
    /(^|\/)backend\/.*\.(cs)$/i,
    /(^|\/)jnpf-web-vue3\/src\/.*\.(vue|ts|tsx|js)$/i,
    /(^|\/)jnpf-web-datascreen\/src\/.*\.(vue|ts|tsx|js)$/i,
    /(^|\/)jnpf-app-vue3\/.*\.(vue|ts|js)$/i,
    /(^|\/)sa-service\/(?!.*node_modules).*\.(ts|js)$/i,
    /(^|\/)studio-preview\/.*\.(vue|ts|js)$/i,
  ];
  return include.some((re) => re.test(p));
}

/**
 * @param {string} content
 * @returns {PlaceholderHit[]}
 */
export function findPlaceholders(content) {
  if (!content || EXEMPT_RE.test(content)) return [];

  const hits = [];
  const lines = content.split(/\r?\n/);
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    // 跳过纯字符串/文档式 UI placeholder 属性（Vue/HTML）
    if (/\bplaceholder\s*=\s*["'`]/.test(line) && !/\/\/|\/\*/.test(line)) continue;
    // 跳过 import / 类型名误伤
    if (/^\s*using\s+/.test(line) || /^\s*import\s+/.test(line)) continue;

    for (const { rule, re } of PATTERNS) {
      // placeholder-comment 过宽：要求同时像「假实现」口吻
      if (rule === 'placeholder-comment') {
        if (!/(?:\/\/|#)\s*(?:.*\b(?:fake|stub|temp|临时|占位)|.*placeholder\b.*(?:implement|return|here|for now))/i.test(line)
          && !/(?:\/\/|#)\s*placeholder\b/i.test(line)) {
          continue;
        }
        // 仍排除 HTML/Vue 属性行
        if (/\bplaceholder\s*=/.test(line)) continue;
      }
      const m = line.match(re);
      if (m) {
        hits.push({ line: i + 1, match: m[0].slice(0, 80), rule });
        break;
      }
    }
  }
  return hits;
}

/**
 * @param {string} filePath
 * @param {string} content
 * @returns {PlaceholderHit[]}
 */
export function scanFileContent(filePath, content) {
  if (!isScannablePath(filePath)) return [];
  return findPlaceholders(content);
}

/**
 * @param {string[]} filePaths absolute or repo-relative
 * @param {string} [cwd]
 * @returns {{ file: string, hits: PlaceholderHit[] }[]}
 */
export function scanFilesOnDisk(filePaths, cwd = process.cwd()) {
  const results = [];
  for (const f of filePaths) {
    const abs = resolve(cwd, f);
    const norm = f.replace(/\\/g, '/');
    if (!isScannablePath(norm) && !isScannablePath(abs.replace(/\\/g, '/'))) continue;
    if (!existsSync(abs)) continue;
    let content;
    try {
      content = readFileSync(abs, 'utf8');
    } catch {
      continue;
    }
    const hits = findPlaceholders(content);
    if (hits.length) results.push({ file: norm, hits });
  }
  return results;
}

/** CLI：--staged 扫描 git 暂存区；否则扫描 argv 文件列表 */
function main() {
  const args = process.argv.slice(2);
  let files = [];
  if (args.includes('--staged')) {
    try {
      const out = execSync('git diff --cached --name-only --diff-filter=ACMR', {
        encoding: 'utf8',
        stdio: ['pipe', 'pipe', 'pipe'],
      });
      files = out.split(/\r?\n/).map((s) => s.trim()).filter(Boolean);
    } catch (e) {
      console.error('placeholder-scan: 无法读取 git staged 文件');
      process.exit(1);
    }
  } else {
    files = args.filter((a) => !a.startsWith('-'));
  }

  const results = scanFilesOnDisk(files);
  if (results.length === 0) {
    process.exit(0);
  }

  console.error('BLOCKED: 检测到占位符/假实现（零占位符硬失败）');
  for (const { file, hits } of results) {
    for (const h of hits) {
      console.error(`  ${file}:${h.line}  [${h.rule}] ${h.match}`);
    }
  }
  console.error('修复：完成实现后再提交；确属例外加 // placeholder-ok: <理由>');
  process.exit(1);
}

const isDirect = process.argv[1] && /placeholder-scan\.mjs$/i.test(process.argv[1].replace(/\\/g, '/'));
if (isDirect) main();
