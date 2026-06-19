#!/usr/bin/env node
/**
 * Stop Hook — 极速冒烟测试 (JNPF 专用版 + Windows 兼容)
 *
 * 定位：冒烟测试，不是全量回归
 * 性能预算：≤ 30 秒
 *
 * 策略：
 *   后端 → 只编译主入口项目（增量编译，5-15 秒）
 *   前端 → 不跑 vue-tsc（太慢），只检查 Git 状态
 *
 * 退出行为：
 *   成功 → stdout 输出 JSON（decision: "approve"）+ exit 0
 *   失败 → stdout 输出 JSON（decision: "block"）+ exit 0
 *   脚本异常 → stderr 输出错误 + exit 0（不阻断 AI）
 */

import { execSync } from 'child_process';
import { existsSync, readdirSync, statSync } from 'fs';

// ─── Supreme Iron Law 证据有效性阈值 ─────────────────────────
// 防止 AI 用旧截图复用 / 用 0 字节假文件绕过 E1 证据要求
const EVIDENCE_MAX_AGE_MIN = 30;   // 截图必须最近 30 分钟内产出
const EVIDENCE_MIN_SIZE_BYTES = 5000; // 截图必须 >5KB（真实渲染产物）

// ─── 读取 stdin ──────────────────────────────────────────────
let input = {};
try {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const raw = Buffer.concat(chunks).toString('utf-8');
  if (raw.trim()) input = JSON.parse(raw);
} catch {
  input = {};
}

// 人类手动打断 → 直接放行
if (input.stop_reason === 'user_interrupt') {
  console.log(JSON.stringify({ decision: 'approve', reason: 'User interrupted' }));
  process.exit(0);
}

console.error('🛑 AI 请求停止。正在执行极速冒烟测试...');

let hasError = false;
const errorDetails = [];

// ─── 检测是否有后端代码变更 ──────────────────────────────────
let hasBackendChanges = false;
let hasFrontendChanges = false;

try {
  // 收集已提交变更（HEAD~1 可能不存在，如首次提交，需容错）
  let committedDiff = '';
  try {
    committedDiff = execSync('git diff --name-only HEAD~1 HEAD', {
      encoding: 'utf-8',
      stdio: 'pipe',
      timeout: 5000,
    }).trim();
  } catch {
    // HEAD~1 不存在（首次提交或浅克隆），改用 git show 获取本次提交的文件
    try {
      committedDiff = execSync('git show --name-only --format=', {
        encoding: 'utf-8',
        stdio: 'pipe',
        timeout: 5000,
      }).trim();
    } catch {
      committedDiff = '';
    }
  }

  const unstaged = execSync('git diff --name-only', {
    encoding: 'utf-8',
    stdio: 'pipe',
    timeout: 5000,
  }).trim();

  const allFiles = committedDiff + '\n' + unstaged;

  hasBackendChanges = /\.(cs|csproj|sln)$/.test(allFiles);
  hasFrontendChanges = /\.(vue|ts|tsx|js|jsx|less|scss)$/.test(allFiles);
} catch {
  // git 命令失败，保守处理：假设都有变更
  hasBackendChanges = true;
  hasFrontendChanges = true;
}

// ─── 规则 1：后端增量编译 ────────────────────────────────────
if (hasBackendChanges) {
  try {
    console.error('▸ [1/3] 后端核心项目增量编译...');

    // Windows 兼容：不用 find/dir 命令，直接检查已知路径
    // JNPF 项目结构固定，已知路径足以覆盖
    const csprojCandidates = [
      'backend/application/JNPF.API.Entry/JNPF.API.Entry.csproj',
      'application/JNPF.API.Entry/JNPF.API.Entry.csproj',
      'backend/src/application/JNPF.API.Entry/JNPF.API.Entry.csproj',
    ];

    let csprojPath = '';
    for (const candidate of csprojCandidates) {
      if (existsSync(candidate)) {
        csprojPath = candidate;
        break;
      }
    }

    if (!csprojPath) {
      console.error('  ⚠️ 未找到 Entry.csproj，跳过后端验证');
    } else {
      execSync(`dotnet build "${csprojPath}" --no-restore -p:IsPackable=false --verbosity quiet`, {
        stdio: ['ignore', 'ignore', 'pipe'],
        timeout: 30000,
      });
      console.error('  ✅ 后端核心编译通过');
    }
  } catch (e) {
    const stderr = (e.stderr?.toString() || '').slice(-500);

    // DLL 被锁定或超时 = 服务正在运行，不算代码错误
    if (stderr.includes('is being used by another process') || e.message?.includes('ETIMEDOUT')) {
      console.error('  ⚠️ DLL 被锁定或编译超时（服务可能正在运行），跳过');
    } else {
      hasError = true;
      errorDetails.push(`后端编译失败: ${stderr.slice(0, 300) || e.message?.slice(0, 200)}`);
      console.error(`  ❌ ${errorDetails[errorDetails.length - 1]}`);
    }
  }
} else {
  console.error('▸ [1/3] 无后端代码变更，跳过');
}

// ─── 规则 2：前端变更状态检查（不跑 vue-tsc）─────────────────
if (hasFrontendChanges) {
  try {
    console.error('▸ [2/3] 前端变更状态检查...');

    const status = execSync('git status --porcelain -- jnpf-web-vue3/', {
      encoding: 'utf-8',
      stdio: 'pipe',
      timeout: 5000,
    }).trim();

    if (status === '') {
      console.error('  ✅ 无未提交的前端变更');
    } else {
      const fileCount = status.split('\n').filter(Boolean).length;
      console.error(`  ⚠️ ${fileCount} 个未提交前端文件（pre-push 阶段做完整验证）`);
    }
  } catch {
    console.error('  ⚠️ Git 状态检查跳过');
  }
} else {
  console.error('▸ [2/3] 无前端代码变更，跳过');
}

// ─── 规则 3：E2E 验证证据检查（Supreme Iron Law 强制执行）─────
// 检查是否有前端代码变更需要 E2E 验证
let needsE2E = false;
if (hasFrontendChanges) {
  // 判断是否是实质性前端变更（排除纯样式/文案）
  try {
    const allFiles = execSync('git diff --name-only HEAD~1 HEAD 2>nul || git show --name-only --format= 2>nul || echo ""', {
      encoding: 'utf-8',
      stdio: 'pipe',
      timeout: 5000,
    }).trim();
    
    const unstaged = execSync('git diff --name-only 2>nul || echo ""', {
      encoding: 'utf-8',
      stdio: 'pipe',
      timeout: 5000,
    }).trim();
    
    const combinedFiles = allFiles + '\n' + unstaged;
    // 排除纯 .md / .json / .css 变更
    const substantiveChanges = combinedFiles
      .split('\n')
      .filter(Boolean)
      .filter(f => /\.(vue|ts|tsx|js|jsx)$/.test(f) && !/\.css$/.test(f));
    
    if (substantiveChanges.length > 0) {
      needsE2E = true;
    }
  } catch {
    needsE2E = hasFrontendChanges;
  }
}

if (needsE2E) {
  console.error('▸ [3/3] E2E 验证证据检查（Supreme Iron Law）...');
  
  try {
    const evidenceDir = '.claude/evidence';
    if (!existsSync(evidenceDir)) {
      hasError = true;
      errorDetails.push(
        'E2E 验证证据缺失：.claude/evidence/ 目录不存在。' +
        '前端变更 MUST 产出 Playwright 截图至该目录。使用 playwright 技能打开浏览器验证。'
      );
      console.error('  ❌ .claude/evidence/ 目录缺失');
    } else {
      const files = readdirSync(evidenceDir);
      const screenshots = files.filter(f => /\.(png|jpg|jpeg)$/i.test(f));

      if (screenshots.length === 0) {
        hasError = true;
        errorDetails.push(
          'E2E 验证证据缺失：.claude/evidence/ 中无截图文件。' +
          '前端变更 MUST 使用 playwright 技能打开浏览器并截图。'
        );
        console.error('  ❌ 无截图证据（需 .png/.jpg）');
      } else {
        // ─── 新鲜度 + 尺寸双重验证（防复用旧截图 / 0字节假文件）───
        // 排除 playwright-smoke.png（技能自检产物，不计为业务证据）
        const now = Date.now();
        const valid = [];
        const invalidReasons = [];

        for (const f of screenshots) {
          const fp = `${evidenceDir}/${f}`;
          try {
            const st = statSync(fp);
            const ageMin = (now - st.mtimeMs) / 60000;
            if (f === 'playwright-smoke.png') {
              // 技能自检产物，跳过（不算业务证据）
              continue;
            }
            if (st.size < EVIDENCE_MIN_SIZE_BYTES) {
              invalidReasons.push(`${f}: 文件仅 ${st.size} 字节（< ${EVIDENCE_MIN_SIZE_BYTES}，疑似 0 字节假文件）`);
            } else if (ageMin > EVIDENCE_MAX_AGE_MIN) {
              invalidReasons.push(`${f}: 产出于 ${ageMin.toFixed(0)} 分钟前（> ${EVIDENCE_MAX_AGE_MIN}，疑似复用旧截图）`);
            } else {
              valid.push(`${f} (${(st.size/1024).toFixed(1)}KB, ${ageMin.toFixed(0)}min ago)`);
            }
          } catch (e) {
            invalidReasons.push(`${f}: stat 失败 ${e.message}`);
          }
        }

        if (valid.length === 0) {
          hasError = true;
          const detail = invalidReasons.length > 0
            ? `发现 ${screenshots.length} 张截图但全部无效：\n    - ${invalidReasons.join('\n    - ')}`
            : '无有效截图（playwright-smoke.png 是技能自检产物，不计为业务证据）。';
          errorDetails.push(
            'E2E 验证证据无效：' + detail +
            '\n  MUST 使用 playwright 技能在本次会话内重新产出截图（新鲜度 ≤ ' + EVIDENCE_MAX_AGE_MIN +
            ' 分钟，文件 ≥ ' + (EVIDENCE_MIN_SIZE_BYTES/1024) + 'KB）。'
          );
          console.error(`  ❌ ${invalidReasons.length} 张截图全部无效`);
        } else {
          console.error(`  ✅ ${valid.length} 张有效截图: ${valid.join(', ')}`);
        }
      }
    }
  } catch (e) {
    console.error(`  ⚠️ E2E 证据检查异常: ${e.message}`);
  }
} else {
  console.error('▸ [3/3] 无前端实质性变更，跳过 E2E 证据检查');
}

// ─── 输出标准 JSON 响应（Claude Code 要求）───────────────────
if (hasError) {
  console.log(JSON.stringify({
    decision: 'block',
    reason: `JNPF 项目健康验证失败，你 MUST 修复后才能停止：\n\n${errorDetails.join('\n')}`,
  }));
} else {
  console.log(JSON.stringify({
    decision: 'approve',
    reason: 'Smoke test passed',
  }));
}

process.exit(0);
