#!/usr/bin/env node
/**
 * PreToolUse Hook (Bash) — 依赖治理拦截器 (稳定版)
 *
 * 职责：拦截依赖安装命令，自动注入国内镜像源。
 * 熔断机制：网络查询超过 3 秒直接放弃，绝不阻塞 AI。
 *
 * 清言指出的致命坑：npm view 在国内网络环境下
 * 遇到某些包会挂起 30 秒到 2 分钟，必须严格熔断。
 */

import { execSync } from 'child_process';

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

const command = input.tool_input?.command || '';
if (!command) process.exit(0);

// 已指定镜像源则放行
if (command.includes('--registry') || command.includes('--source') || command.includes('-i ')) {
  process.exit(0);
}

// ─── 检测安装命令 ────────────────────────────────────────────
const isNpm = /\bnpm\s+(install|i|add)\b/.test(command);
const isPnpm = /\bpnpm\s+(add|install|i)\b/.test(command);
const isDotnet = /\bdotnet\s+add\s+package\b/.test(command);
const isPip = /\bpip(?:3)?\s+install\b/.test(command);

if (!isNpm && !isPnpm && !isDotnet && !isPip) process.exit(0);

console.error('📦 依赖治理拦截：检测到安装命令，注入镜像源...');

// ─── NPM / PNPM 处理 ────────────────────────────────────────
if (isNpm || isPnpm) {
  const REG = 'https://registry.npmmirror.com';
  let fixed = command.replace(/\b(npm|pnpm)\s+/, `$&--registry ${REG} `);
  console.error(`  ✅ 已注入镜像: ${REG}`);

  // 尝试查询版本，但 3 秒查不到就放弃
  const pkgMatch = command.match(/(?:npm|pnpm)\s+(?:install|i|add)\s+(@[\w\-./]+|[\w\-]+)/);
  if (pkgMatch?.[1] && !pkgMatch[1].startsWith('-')) {
    try {
      const ver = execSync(`npm view ${pkgMatch[1]} version --registry=${REG}`, {
        encoding: 'utf-8',
        timeout: 3000, // 3 秒熔断
        stdio: 'pipe',
      }).trim();
      console.error(`  ✅ ${pkgMatch[1]} 最新稳定版: ${ver}`);
    } catch {
      console.error(`  ⚠️ 版本查询超时，跳过校验。`);
    }
  }

  console.log(JSON.stringify({
    decision: 'block',
    reason: `依赖治理：已注入 npmmirror 镜像。请执行：\n${fixed}`,
  }));
  process.exit(0);
}

// ─── .NET 处理 ───────────────────────────────────────────────
if (isDotnet) {
  const SRC = 'https://repo.huaweicloud.com/repository/nuget/v3/index.json';
  console.error(`  ✅ 已注入华为云 NuGet: ${SRC}`);

  console.log(JSON.stringify({
    decision: 'block',
    reason: `依赖治理：已注入华为云 NuGet 镜像。请执行：\n${command} --source ${SRC}`,
  }));
  process.exit(0);
}

// ─── Python pip 处理 ─────────────────────────────────────────
if (isPip) {
  const IDX = 'https://mirrors.aliyun.com/pypi/simple/';
  console.error(`  ✅ 已注入阿里云 PyPI: ${IDX}`);

  console.log(JSON.stringify({
    decision: 'block',
    reason: `依赖治理：已注入阿里云 PyPI 镜像。请执行：\n${command} -i ${IDX} --trusted-host mirrors.aliyun.com`,
  }));
  process.exit(0);
}

process.exit(0);
