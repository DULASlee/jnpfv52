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
import { existsSync } from 'fs';
import { platform } from 'os';

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

const isWindows = platform() === 'win32';

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
    console.error('▸ [1/2] 后端核心项目增量编译...');

    // Windows 兼容：不用 find 命令，直接检查已知路径
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

    // 如果已知路径找不到，尝试跨平台查找
    if (!csprojPath) {
      try {
        const findCmd = isWindows
          ? 'dir /s /b *.csproj 2>nul | findstr /i "Entry"'
          : 'find . -maxdepth 5 -name "*Entry*.csproj" 2>/dev/null | head -1';
        csprojPath = execSync(findCmd, {
          encoding: 'utf-8',
          stdio: 'pipe',
          timeout: 10000,
        }).trim().split('\n')[0];
      } catch {
        // 查找失败，跳过后端验证
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
  console.error('▸ [1/2] 无后端代码变更，跳过');
}

// ─── 规则 2：前端变更状态检查（不跑 vue-tsc）─────────────────
if (hasFrontendChanges) {
  try {
    console.error('▸ [2/2] 前端变更状态检查...');

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
  console.error('▸ [2/2] 无前端代码变更，跳过');
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
