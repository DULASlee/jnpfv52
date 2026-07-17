#!/usr/bin/env node
/**
 * Stop Hook — 冒烟测试 + E2E 证据验证 (JNPF v5.2 Supreme Iron Law)
 *
 * 四层检测（任一层失败 = BLOCK 会话退出）：
 *   L1: dotnet build（后端变更时）            → 编译失败 = BLOCK
 *   L2: dotnet test（后端变更+测试项目时）     → 测试失败 = BLOCK
 *   L3: vue-tsc --noEmit（前端变更时）       → 类型错误 = BLOCK
 *   L4: E2E 证据新鲜度验证（实质性前端变更时）→ 无有效截图 = BLOCK
 *
 * 80s 超时自毁 → BLOCK（超时不是验证通过，宁可不退出也不放过未验证变更）
 * 用户手动打断 → approve
 * git 命令失败 → 保守跳过（不阻断，git 问题不是代码问题）
 *
 * L4-L6（服务重启+健康检查+登录）属于显式 Step 5 测试，不在此 Stop hook 中执行。
 */

import { execSync } from 'child_process';
import { existsSync, readFileSync, readdirSync, statSync } from 'fs';
import { requiresPillarClaim, loadAndValidateClaim, CLAIM_REL } from './pillar-claim-lib.mjs';

// ─── 项目根目录解析（cwd 可能在子目录）─────────────────────────
function getProjectRoot() {
  try {
    return execSync('git rev-parse --show-toplevel', {
      encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
    }).trim().replace(/\\/g, '/');
  } catch { /* fall through */ }
  // fallback: 向上查找 CLAUDE.md
  let dir = process.cwd();
  for (let i = 0; i < 5; i++) {
    if (existsSync(`${dir}/CLAUDE.md`)) return dir.replace(/\\/g, '/');
    const parent = dir.replace(/[/\\][^/\\]+$/, '');
    if (parent === dir) break;
    dir = parent;
  }
  return process.cwd().replace(/\\/g, '/');
}
const ROOT = getProjectRoot();
function rootPath(rel) { return `${ROOT}/${rel}`; }

// ─── Supreme Iron Law 证据有效性阈值 ─────────────────────────────
const EVIDENCE_MAX_AGE_MIN = 30;
const EVIDENCE_MIN_SIZE_BYTES = 5000;
const SESSION_MAX_AGE_MS = 4 * 60 * 60 * 1000; // 4h — 提升到全局作用域，L0/L4 共用

// ─── 80s 超时自毁 — 必须在 stdin 读取之前注册 ───────────────────
const KILL_TIMER = setTimeout(() => {
  console.log(JSON.stringify({
    decision: 'block',
    reason: 'Guard 超时（80s）—— 构建或检查未在限时内完成。\n'
      + '请手动验证后再次结束会话：\n'
      + '  1. dotnet build backend/application/JNPF.API.Entry/JNPF.API.Entry.csproj\n'
      + '  2. cd jnpf-web-vue3 && npx vue-tsc --noEmit\n'
      + '  3. 确认前端截图证据存在于 .claude/evidence/',
  }));
  process.exit(1); // 非零退出 = 异常终止
}, 80000);

// ─── 安全退出：确保清理定时器 ──────────────────────────────────────
function safeExit(code = 0) {
  clearTimeout(KILL_TIMER);
  process.exit(code);
}

// ─── 读取 stdin（5s 防挂起，超时则空 input 继续）────────────────
let input = {};
try {
  const raw = await Promise.race([
    (async () => {
      const chunks = [];
      for await (const chunk of process.stdin) chunks.push(chunk);
      return Buffer.concat(chunks).toString('utf-8');
    })(),
    new Promise((_, reject) => setTimeout(() => reject(new Error('stdin timeout')), 5000)),
  ]);
  if (raw.trim()) input = JSON.parse(raw);
} catch { input = {}; }

// 用户手动打断 → 直接放行
if (input.stop_reason === 'user_interrupt') {
  console.log(JSON.stringify({ decision: 'approve', reason: '用户手动打断' }));
  safeExit(0);
}

console.error('🛑 Stop hook: 冒烟测试 + E2E 证据检查...');

let hasError = false;
const errorDetails = [];
const checks = [];

// ═══════════════════════════════════════════════════════════════════
// L0-PILLAR: 四大支柱 claim（节点审批态硬门）
// ═══════════════════════════════════════════════════════════════════
{
  if (requiresPillarClaim(ROOT)) {
    const result = loadAndValidateClaim(ROOT);
    if (!result.ok) {
      hasError = true;
      errorDetails.push('⛔ 四大支柱 claim 无效/缺失 — 禁止在节点审批态结束会话并声称完成。');
      for (const e of result.errors) errorDetails.push(`   · ${e}`);
      errorDetails.push(`   填写 ${CLAIM_REL}（模板 .cursor/templates/four-pillars-checkpoint.md）`);
      errorDetails.push('   校验: node .claude/hooks/pillar-claim-check.mjs --force');
      checks.push('L0-PILLAR: ❌ pillar claim');
    } else {
      console.error(`  ✅ 四大支柱 claim 有效 (node=${result.claim.node})`);
      checks.push('L0-PILLAR: ✅ pillar claim');
    }
  } else {
    checks.push('L0-PILLAR: ⏭️ 非节点审批态');
  }
}

// ─── 变更检测（合并 committed + unstaged + staged）─────────────
let allFiles = '';
let hasBackendChanges = false;
let hasFrontendChanges = false;
let hasSubstantiveFrontend = false;

try {
  // committed diff (HEAD~1 可能不存在，需回退到 git show)
  let committedDiff = '';
  try {
    committedDiff = execSync('git diff --name-only HEAD~1 HEAD', {
      encoding: 'utf-8', stdio: 'pipe', timeout: 5000,
    }).trim();
  } catch {
    try {
      committedDiff = execSync('git show --name-only --format=', {
        encoding: 'utf-8', stdio: 'pipe', timeout: 5000,
      }).trim();
    } catch { committedDiff = ''; }
  }

  // unstaged + staged
  let unstaged = '';
  let staged = '';
  try { unstaged = execSync('git diff --name-only', { encoding: 'utf-8', stdio: 'pipe', timeout: 5000 }).trim(); } catch { /* skip */ }
  try { staged = execSync('git diff --name-only --cached', { encoding: 'utf-8', stdio: 'pipe', timeout: 5000 }).trim(); } catch { /* skip */ }

  allFiles = [committedDiff, unstaged, staged].filter(Boolean).join('\n');

  // /m 标志确保 $ 匹配每行末尾（而非整个字符串末尾）
  hasBackendChanges = /\.(cs|csproj|sln)$/m.test(allFiles);
  hasFrontendChanges = /\.(vue|ts|tsx|js|jsx|less|scss)$/m.test(allFiles);

  // 实质性前端变更 = 智能三级判定 + 时效性过滤（仅真正影响 UI 且为本次会话变更才触发 E2E）
  //   Tier 1: .vue/.tsx/.less/.scss/.css → 总是 UI 相关
  //   Tier 2: .ts/.js → 仅在 UI 目录下（views/components/hooks/layouts 等）
  //   Tier 3: 测试/配置/API/工具/类型 → 永不触发
  //   时效性: 文件 mtime > 4h 前 → 存量变更，不触发（避免为旧工作树改动浪费时间）
  const now = Date.now();

  const isSubstantiveFrontend = (f) => {
    // ══ 先排除非前端项目目录和基础设施 ══
    if (!/^(jnpf-web-vue3|jnpf-web-datascreen|jnpf-app-vue3)\//.test(f)) return false;

    // ══ 时效性过滤：排除 4 小时前修改的存量文件 ══
    try {
      const st = statSync(f);
      if (now - st.mtimeMs > SESSION_MAX_AGE_MS) return false;
    } catch { return false; } // 文件不存在 → 跳过

    // ══ 排除测试和配置文件 ══
    if (/\.(spec|test)\.(ts|tsx|js|jsx)$/.test(f)) return false;
    if (/\/__tests__\//.test(f)) return false;
    if (/(vite\.config|tsconfig|package\.json|\.eslintrc|\.prettierrc)/.test(f)) return false;

    // ══ Tier 1: 视觉文件 → 总是 E2E ══
    if (/\.(vue|tsx|less|scss|css)$/.test(f)) return true;

    // ══ Tier 2: .ts/.js 仅限 UI 相关目录 ══
    if (/\.(ts|js|jsx)$/.test(f)) {
      const UI_DIRS = /\/(views?|pages?|components?|widgets?|layouts?|hooks?|composables?|directives?|assets?|styles?)\//;
      if (UI_DIRS.test(f)) return true;

      // src/ 根下的入口文件 (main.ts, App.vue 配对)
      if (/\/(main|App|bootstrap|entry)\.[jt]sx?$/.test(f)) return true;
    }

    // ══ Tier 3: api/utils/types/store/router/locales → 不影响 UI ══
    return false;
  };

  hasSubstantiveFrontend = allFiles.split('\n')
    .filter(Boolean)
    .some(isSubstantiveFrontend);

} catch {
  // git 不可用 → 保守跳过，不阻断
  console.error('  ⚠️ git 命令失败，跳过变更检测');
  checks.push('变更检测: ⚠️ 跳过（git 不可用）');
}

// ═══════════════════════════════════════════════════════════════════
// L0: 错题本强制验证（代码变更时必须追加今日条目）
// ═══════════════════════════════════════════════════════════════════
{
  // 检测是否有实质性代码变更（排除纯文档/证据/配置/摘要文件）
  const codeLines = allFiles.split('\n').filter(line => {
    const f = line.trim();
    if (!f) return false;
    if (/\.claude[\\/]evidence[\\/]/.test(f)) return false;
    if (/\.claude[\\/]memory[\\/]session-summaries[\\/]/.test(f)) return false;
    if (/^tc-result\.txt$/.test(f)) return false;
    if (/\.(md|json|png|jpg|jpeg)$/i.test(f)) {
      if (!/CLAUDE\.md$/i.test(f) && !/\.claude[\\/]rules[\\/]/.test(f)) return false;
    }
    return true;
  });

  if (codeLines.length > 0) {
    console.error('▸ [0/4] 错题本验证...');
    const mistakeLogPath = rootPath('.claude/memory/mistake-log.md');
    if (existsSync(mistakeLogPath)) {
      const content = readFileSync(mistakeLogPath, 'utf-8');
      const now2 = new Date();
      const dateStr = `${now2.getFullYear()}-${String(now2.getMonth() + 1).padStart(2, '0')}-${String(now2.getDate()).padStart(2, '0')}`;
      if (!content.includes(`## ${dateStr}`)) {
        hasError = true;
        errorDetails.push(`⛔ 错题本验证失败: 本会话有 ${codeLines.length} 个代码文件变更，但 .claude/memory/mistake-log.md 无今日 (${dateStr}) 条目。\n请在 todo_write 中将 📝错题本次 标记 completed，并追加条目（格式: 日期 | 类别 | 症状 | 根因 | 修复 | 关键词）。`);
        console.error(`  ❌ 无今日 (${dateStr}) 错题本条目`);
        checks.push('L0: ❌ no mistake-log entry today');
      } else {
        console.error(`  ✅ 错题本已有今日 (${dateStr}) 条目`);
        checks.push('L0: ✅ mistake-log updated');
      }
    } else {
      hasError = true;
      errorDetails.push('⛔ 错题本验证失败: .claude/memory/mistake-log.md 不存在。');
      checks.push('L0: ❌ mistake-log.md missing');
    }
    // Phase 7 + Debug: 从 workflow-state.json 读取所有 Stop-time SP 标记
    const wfPath = rootPath('.claude/workflow-state.json');
    if (existsSync(wfPath)) {
      try {
        const wf = JSON.parse(readFileSync(wfPath, 'utf8'));
        const sp = wf.sp || {};

        // finishing-a-development-branch: Phase 7 强制
        if (!sp['finishing-a-development-branch']) {
          hasError = true;
          errorDetails.push('⛔ Phase 7: Skill("superpowers:finishing-a-development-branch") 未调用。');
          checks.push('L0: ❌ finishing-a-development-branch');
        } else { checks.push('L0: ✅ finishing-a-development-branch'); }

        // pre-commit: Phase 7 提交前检查
        if (!sp['pre-commit']) {
          hasError = true;
          errorDetails.push('⛔ Phase 7: Skill("pre-commit") 未调用。');
          checks.push('L0: ❌ pre-commit');
        } else { checks.push('L0: ✅ pre-commit'); }

        // start-dev + playwright: Phase 5 环境前置 (有前端实质性变更时强制)
        if (hasSubstantiveFrontend) {
          if (!sp['start-dev']) {
            hasError = true;
            errorDetails.push('⛔ Phase 5: 有前端变更但 Skill("start-dev") 未调用。E2E验证必须启动开发环境。');
            checks.push('L0: ❌ start-dev');
          } else { checks.push('L0: ✅ start-dev'); }
          if (!sp['playwright']) {
            hasError = true;
            errorDetails.push('⛔ Phase 5: 有前端变更但 Skill("playwright") 未调用。Supreme Iron Law 要求 E1 浏览器截图。');
            checks.push('L0: ❌ playwright');
          } else { checks.push('L0: ✅ playwright'); }
        }

        // systematic-debugging + data-driven-debug: 有错题本今日条目 → 强制
        const mlPath = rootPath('.claude/memory/mistake-log.md');
        const hasML = existsSync(mlPath);
        const datePrefix = `${new Date().getFullYear()}-${String(new Date().getMonth()+1).padStart(2,'0')}-${String(new Date().getDate()).padStart(2,'0')}`;
        const mlHasToday = hasML && readFileSync(mlPath,'utf8').includes(`## ${datePrefix}`);
        if (mlHasToday) {
          if (!sp['systematic-debugging']) {
            hasError = true;
            errorDetails.push('⛔ Debug: 错题本有今日条目但 Skill("superpowers:systematic-debugging") 未调用。');
            checks.push('L0: ❌ systematic-debugging');
          } else { checks.push('L0: ✅ systematic-debugging'); }
          if (!sp['data-driven-debug']) {
            hasError = true;
            errorDetails.push('⛔ Debug: 错题本有今日条目但 Skill("data-driven-debug") 未调用。3次修复无效/10min无进展时必须。');
            checks.push('L0: ❌ data-driven-debug');
          } else { checks.push('L0: ✅ data-driven-debug'); }
        }
      } catch { checks.push('L0: ⚠️ workflow-state read error'); }
    } else { checks.push('L0: ⚠️ workflow-state.json missing'); }
  } else {
    checks.push('L0: ⏭️ no code changes (skip mistake-log check)');
  }
}

// ═══════════════════════════════════════════════════════════════════
// L1: dotnet build（后端变更时）
// ═══════════════════════════════════════════════════════════════════
if (hasBackendChanges) {
  const csprojPath = rootPath('backend/application/JNPF.API.Entry/JNPF.API.Entry.csproj');

  if (!existsSync(csprojPath)) {
    console.error(`▸ [1/3] 未找到 ${csprojPath}，跳过后端编译`);
    checks.push('L1: ⏭️ csproj 未找到');
  } else {
    console.error('▸ [1/3] 后端编译验证...');
    try {
      // restore + build（不跳过 restore，防止新增 NuGet 包导致误报）
      execSync(`dotnet restore "${csprojPath}" --verbosity quiet`, {
        stdio: ['ignore', 'pipe', 'pipe'], timeout: 30000,
      });
      execSync(`dotnet build "${csprojPath}" --no-restore -p:IsPackable=false --verbosity quiet`, {
        stdio: ['ignore', 'pipe', 'pipe'], timeout: 60000,
      });
      console.error('  ✅ 后端编译通过');
      checks.push('L1: ✅ build passed');
    } catch (e) {
      const stderr = (e.stderr?.toString() || '').slice(-1000);
      const msg = e.message || '';

      // DLL 锁定 = 服务正在运行，不是代码错误
      if (/is being used by another process|正由另一进程|MSB3021|MSB3027/i.test(stderr + msg)) {
        console.error('  ⚠️ DLL 被锁定（服务可能正在运行），跳过');
        checks.push('L1: ⚠️ DLL locked (skipped)');
      } else {
        // 提取真正的编译错误（过滤 NuGet 警告和超时）
        const realErrors = stderr.split('\n').filter(l =>
          /error\s+(CS|NU)\d+/i.test(l) && !/warning/i.test(l)
        );
        if (realErrors.length === 0) {
          // 可能是 timeout 或其他非代码原因
          console.error(`  ⚠️ 编译异常（非代码错误）: ${msg.slice(0, 200)}`);
          checks.push('L1: ⚠️ build error (non-code, skipped)');
        } else {
          hasError = true;
          const detail = `后端编译失败 (${realErrors.length} errors):\n${realErrors.slice(0, 5).join('\n')}`;
          errorDetails.push(detail);
          console.error(`  ❌ ${realErrors.length} 个编译错误`);
          checks.push(`L1: ❌ ${realErrors.length} build errors`);
        }
      }
    }
  }
} else {
  console.error('▸ [1/4] 无后端变更，跳过');
  checks.push('L1: ⏭️ no backend changes');
}

// ═══════════════════════════════════════════════════════════════════
// L2: dotnet test（后端变更 + 测试项目存在时）
// ═══════════════════════════════════════════════════════════════════
if (hasBackendChanges) {
  const testProj = rootPath('backend/tests/JNPF.Tests.Gate/JNPF.Tests.Gate.csproj');
  if (existsSync(testProj)) {
    console.error('▸ [2/4] 后端测试执行...');
    try {
      execSync(`dotnet test "${testProj}" --nologo --verbosity quiet`, {
        stdio: ['ignore', 'pipe', 'pipe'], timeout: 120000,
      });
      console.error('  ✅ 测试通过');
      checks.push('L2: ✅ tests passed');
    } catch (e) {
      const out = (e.stdout?.toString() || '') + (e.stderr?.toString() || '');
      const failedMatch = out.match(/Failed:\s*(\d+)/);
      if (failedMatch && parseInt(failedMatch[1]) > 0) {
        hasError = true;
        errorDetails.push(`⛔ 测试失败: ${failedMatch[1]} 个用例失败。MUST 修复后重试。`);
        checks.push(`L2: ❌ ${failedMatch[1]} tests failed`);
      } else {
        console.error(`  ⚠️ 测试异常: ${(e.message || '').slice(0, 200)}`);
        checks.push('L2: ⚠️ test error (skipped)');
      }
    }
  } else {
    checks.push('L2: ⏭️ no test project');
  }
} else {
  checks.push('L2: ⏭️ no backend changes');
}

// ═══════════════════════════════════════════════════════════════════
// L3: vue-tsc --noEmit（前端变更时）
// ═══════════════════════════════════════════════════════════════════
if (hasFrontendChanges && existsSync(rootPath('jnpf-web-vue3/package.json'))) {
  console.error('▸ [3/4] 前端类型检查...');
  try {
    execSync('npx vue-tsc --noEmit', {
      cwd: rootPath('jnpf-web-vue3'),
      stdio: ['ignore', 'pipe', 'pipe'],
      timeout: 120000,
    });
    console.error('  ✅ vue-tsc 通过');
    checks.push('L3: ✅ type check passed');
  } catch (e) {
    const out = (e.stdout?.toString() || '') + (e.stderr?.toString() || '');
    const errorCount = (out.match(/error TS\d+/g) || []).length;
    if (errorCount > 0) {
      hasError = true;
      const detail = `前端类型检查失败 (${errorCount} TS errors)`;
      errorDetails.push(detail);
      console.error(`  ❌ ${errorCount} 个类型错误`);
      checks.push(`L3: ❌ ${errorCount} type errors`);
    } else {
      // 非 TS 错误（如 OOM、超时）
      console.error(`  ⚠️ vue-tsc 异常: ${(e.message || '').slice(0, 200)}`);
      checks.push('L3: ⚠️ vue-tsc error (non-TS, skipped)');
    }
  }
} else {
  console.error('▸ [3/4] 无前端变更，跳过');
  checks.push('L3: ⏭️ no frontend changes');
}

// ═══════════════════════════════════════════════════════════════════
// L4: E2E 证据新鲜度验证（实质性前端变更时）
// ═══════════════════════════════════════════════════════════════════
if (hasSubstantiveFrontend) {
  console.error('▸ [4/4] E2E 验证证据检查（Supreme Iron Law）...');

  try {
    const evidenceDir = rootPath('.claude/evidence');
    if (!existsSync(evidenceDir)) {
      hasError = true;
      const detail = 'E2E 验证证据缺失: .claude/evidence/ 目录不存在。\n'
        + '前端实质性变更 MUST 使用 playwright 技能产出截图至该目录。';
      errorDetails.push(detail);
      console.error('  ❌ .claude/evidence/ 目录缺失');
      checks.push('L4: ❌ evidence dir missing');
    } else {
      const files = readdirSync(evidenceDir);
      const screenshots = files.filter(f => /\.(png|jpg|jpeg)$/i.test(f));

      if (screenshots.length === 0) {
        hasError = true;
        const detail = 'E2E 验证证据缺失: .claude/evidence/ 中无截图文件。\n'
          + '前端实质性变更 MUST 使用 playwright 技能打开浏览器并截图。';
        errorDetails.push(detail);
        console.error('  ❌ 无截图证据');
        checks.push('L4: ❌ no screenshots');
      } else {
        const now = Date.now();
        const valid = [];
        const invalidReasons = [];

        for (const f of screenshots) {
          const fp = `${evidenceDir}/${f}`;
          try {
            const st = statSync(fp);
            const ageMin = (now - st.mtimeMs) / 60000;

            // 排除 playwright-smoke.png（技能自检产物，不计为业务证据）
            if (f === 'playwright-smoke.png') continue;

            if (st.size < EVIDENCE_MIN_SIZE_BYTES) {
              invalidReasons.push(`${f}: ${st.size} 字节（< ${EVIDENCE_MIN_SIZE_BYTES}，疑似空文件）`);
            } else if (ageMin > EVIDENCE_MAX_AGE_MIN) {
              invalidReasons.push(`${f}: ${ageMin.toFixed(0)} 分钟前产出（> ${EVIDENCE_MAX_AGE_MIN} 分钟，疑似复用旧截图）`);
            } else {
              valid.push(`${f} (${(st.size / 1024).toFixed(1)}KB, ${ageMin.toFixed(0)}min ago)`);
            }
          } catch (e) {
            invalidReasons.push(`${f}: stat 失败 ${e.message}`);
          }
        }

        if (valid.length === 0) {
          hasError = true;
          const detail = `E2E 验证证据无效: ${screenshots.length} 张截图全部未通过新鲜度/尺寸验证。\n`
            + (invalidReasons.length > 0 ? `  - ${invalidReasons.join('\n  - ')}\n` : '')
            + `MUST 使用 playwright 技能在本次会话内重新产出截图（新鲜度 ≤ ${EVIDENCE_MAX_AGE_MIN} 分钟，≥ ${EVIDENCE_MIN_SIZE_BYTES / 1024}KB）。`;
          errorDetails.push(detail);
          console.error(`  ❌ ${invalidReasons.length} 张截图全部无效`);
          checks.push(`L4: ❌ ${invalidReasons.length} invalid screenshots`);
        } else {
          console.error(`  ✅ ${valid.length} 张有效截图: ${valid.join(', ')}`);
          checks.push(`L4: ✅ ${valid.length} valid screenshots`);
        }
      }
    }
  } catch (e) {
    console.error(`  ⚠️ E2E 证据检查异常: ${e.message}`);
    checks.push('L4: ⚠️ check error (skipped)');
  }
} else {
  console.error('▸ [3/3] 无前端实质性变更，跳过 E2E 证据检查');
  checks.push('L4: ⏭️ no substantive frontend changes');
}

// ═══════════════════════════════════════════════════════════════════
// 输出决策
// ═══════════════════════════════════════════════════════════════════
console.error('');
console.error('=== 冒烟测试完成 ===');
checks.forEach(c => console.error(`  ${c}`));

if (hasError) {
  console.log(JSON.stringify({
    decision: 'block',
    reason: 'JNPF 项目健康验证失败，MUST 修复后才能停止：\n\n' + errorDetails.join('\n\n'),
  }));
} else {
  console.log(JSON.stringify({
    decision: 'approve',
    reason: `冒烟测试通过。${checks.join('; ')}。L4-L6 跳过（使用显式测试）。`,
  }));
}

safeExit(0);
