#!/usr/bin/env node
/**
 * 将 scripts/.jnpf-session.json 同步到 REST Client 环境文件
 *
 * 用法：
 *   node scripts/lib/jnpf-auth.mjs --json   # 先登录
 *   node scripts/sync-http-env.mjs          # 写入 api-tests/http/http-client.env.json
 *
 * VS Code REST Client / Thunder Client 读取 http-client.env.json 中的 @token
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadCachedSession, login } from './lib/jnpf-auth.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(__dirname, '../api-tests/http/http-client.env.json');
const SESSION = path.join(__dirname, '.jnpf-session.json');

async function main() {
  let session = loadCachedSession();
  if (!session?.token) {
    console.log('[sync-http-env] 无缓存 token，正在登录…');
    session = await login();
  }

  const env = {
    dev: {
      baseUrl: session.apiUrl || process.env.JNPF_API_URL || 'http://localhost:5000',
      token: session.token.replace(/^Bearer\s+/i, ''),
      pipelineId: String(
        process.env.E2E_PIPELINE_ID
        || JSON.parse(fs.existsSync(path.join(__dirname, '.sup-e2e-state.json'))
          ? fs.readFileSync(path.join(__dirname, '.sup-e2e-state.json'), 'utf8')
          : '{}').pipelineId
        || '',
      ),
    },
  };

  fs.mkdirSync(path.dirname(OUT), { recursive: true });
  fs.writeFileSync(OUT, JSON.stringify(env, null, 2), 'utf8');
  console.log('[sync-http-env] OK →', OUT);
  console.log('[sync-http-env] baseUrl=', env.dev.baseUrl, 'pipelineId=', env.dev.pipelineId);
}

main().catch(e => {
  console.error('[sync-http-env] FAIL', e.message);
  process.exit(1);
});
