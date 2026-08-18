#!/usr/bin/env node
/**
 * JNPF API CLI — 无浏览器、自动登录、带 Token 调任意接口
 *
 * 示例：
 *   node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser
 *   node scripts/jnpf-api.mjs POST /api/studio/pipeline/execute/create '{"name":"t","userRequirement":"..."}'
 *   node scripts/jnpf-api.mjs GET /api/studio/ir/42/events
 *   node scripts/jnpf-api.mjs login --force
 *
 * 环境变量：JNPF_API_URL JNPF_ACCOUNT JNPF_PASSWORD JNPF_CIPHER_KEY
 */

import { apiRequest, login } from './lib/jnpf-auth.mjs';

const args = process.argv.slice(2);

function usage() {
  console.log(`用法:
  node scripts/jnpf-api.mjs login [--force] [--json]
  node scripts/jnpf-api.mjs <METHOD> <path> [json-body]

示例:
  node scripts/jnpf-api.mjs GET /api/studio/skills/1/runs
  node scripts/jnpf-api.mjs POST /api/studio/skills/pm/1/run '{}'
`);
}

async function main() {
  if (args.length === 0 || args[0] === '-h' || args[0] === '--help') {
    usage();
    process.exit(0);
  }

  if (args[0] === 'login') {
    const session = await login({ force: args.includes('--force') });
    if (args.includes('--json')) console.log(JSON.stringify(session, null, 2));
    else console.log(session.token);
    return;
  }

  const method = args[0].toUpperCase();
  const urlPath = args[1];
  if (!urlPath) {
    usage();
    process.exit(1);
  }

  let body;
  if (args[2]) {
    try {
      body = JSON.parse(args[2]);
    } catch {
      body = args[2];
    }
  }

  const result = await apiRequest(method, urlPath, { body });
  const out = result.json ?? result.text;
  console.log(JSON.stringify({ status: result.status, ok: result.ok, data: out }, null, 2));
  if (!result.ok) process.exit(1);
}

main().catch(err => {
  console.error(err.message || err);
  process.exit(1);
});
