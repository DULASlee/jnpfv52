#!/usr/bin/env node
/**
 * 集成测试：超管能查到 admin 用户（R4 方案 A — ITenantFilter 超管豁免）
 *
 * 背景：admin BASE_USER.F_TENANT_ID 与 JWT TenantId 不一致时，ITenantFilter 误杀
 * → UserEntity null → UserManager.GetUserInfo():421 NRE → CurrentUser 500。
 * AdminBypassGuard.IsAdministrator() 对超管（Administrator claim=1）豁免 ITenantFilter。
 *
 * 运行：node scripts/test-admin-bypass.mjs（后端 :5000 需运行）
 */
import { execSync } from 'node:child_process';

const BASE = process.env.JNPF_API_URL || 'http://localhost:5000';
const ROOT = execSync('git rev-parse --show-toplevel', { encoding: 'utf-8' }).trim();

function sh(cmd) {
  return execSync(cmd, { encoding: 'utf-8', cwd: ROOT, stdio: ['pipe', 'pipe', 'pipe'] });
}

let exitCode = 0;
function assert(name, pass, detail) {
  console.log(`${pass ? '✓' : '✗'} ${name}${detail ? ' — ' + detail : ''}`);
  if (!pass) exitCode = 1;
}

const token = JSON.parse(sh('node scripts/jnpf-api.mjs login --force --json')).token;
const body = sh(`curl -s -m 30 "${BASE}/api/oauth/CurrentUser?type=Web&systemCode=mainSystem" -H "Authorization: Bearer ${token}"`);
const res = JSON.parse(body);

assert('CurrentUser code == 200 (无 NRE)', res.code === 200, `code=${res.code} msg=${(res.msg || '').slice(0, 80)}`);
assert('userName == 管理员', res.data?.userInfo?.userName === '管理员', `got=${res.data?.userInfo?.userName}`);
assert('systemIds >= 2 (mainSystem + devDemoSystem)', (res.data?.userInfo?.systemIds?.length || 0) >= 2, `count=${res.data?.userInfo?.systemIds?.length}`);
assert('menuList 非空 (菜单加载成功)', (res.data?.menuList?.length || 0) > 0, `count=${res.data?.menuList?.length}`);

console.log(exitCode === 0 ? '\nPASS: 超管能查到 admin 用户（ITenantFilter 豁免生效）' : '\nFAIL');
process.exit(exitCode);
