/**
 * JNPF 无浏览器登录 — MD5 + AES-ECB（与 PC/App 前端一致）
 *
 * 用法（模块）：
 *   import { login, encryptPassword, apiRequest } from './lib/jnpf-auth.mjs';
 *
 * 用法（CLI 取 token）：
 *   node scripts/lib/jnpf-auth.mjs
 *   node scripts/lib/jnpf-auth.mjs --json
 */

import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '../..');
const SESSION_FILE = path.join(REPO_ROOT, 'scripts', '.jnpf-session.json');

const DEFAULTS = {
  apiUrl: process.env.JNPF_API_URL || 'http://localhost:5000',
  account: process.env.JNPF_ACCOUNT || 'admin',
  password: process.env.JNPF_PASSWORD || '123456',
  cipherKey: process.env.JNPF_CIPHER_KEY || 'EY8WePvjM5GGwQzn',
  origin: process.env.JNPF_ORIGIN || 'pc', // axios 默认 jnpf-origin: pc
};

/** 与 LoginForm.vue：encryptByMd5 → encryptByAES(useHex) */
export function encryptPassword(plainPassword, cipherKey = DEFAULTS.cipherKey) {
  const md5hex = crypto.createHash('md5').update(plainPassword, 'utf8').digest('hex');
  const key = Buffer.from(cipherKey, 'utf8');
  if (key.length !== 16) throw new Error(`AES key must be 16 bytes, got ${key.length}`);
  const cipher = crypto.createCipheriv('aes-128-ecb', key, null);
  const encrypted = Buffer.concat([cipher.update(md5hex, 'utf8'), cipher.final()]);
  return encrypted.toString('hex');
}

/** JNPF 返回的 token 可能已含 "Bearer " 前缀 */
export function normalizeToken(token) {
  if (!token) return '';
  return token.startsWith('Bearer ') ? token.slice(7).trim() : token.trim();
}

export function authHeader(token) {
  const t = normalizeToken(token);
  return t ? `Bearer ${t}` : '';
}

function decodeJwtExp(token) {
  try {
    const payload = token.split('.')[1];
    const json = JSON.parse(Buffer.from(payload, 'base64url').toString('utf8'));
    return typeof json.exp === 'number' ? json.exp * 1000 : null;
  } catch {
    return null;
  }
}

export function loadCachedSession() {
  try {
    if (!fs.existsSync(SESSION_FILE)) return null;
    const data = JSON.parse(fs.readFileSync(SESSION_FILE, 'utf8'));
    if (!data.token) return null;
    const expMs = data.expiresAt ?? decodeJwtExp(data.token);
    if (expMs && Date.now() > expMs - 60_000) return null;
    return data;
  } catch {
    return null;
  }
}

export function saveSession(session) {
  fs.mkdirSync(path.dirname(SESSION_FILE), { recursive: true });
  fs.writeFileSync(SESSION_FILE, JSON.stringify(session, null, 2), 'utf8');
}

/**
 * POST /api/oauth/Login → { token, ... }
 */
export async function login(options = {}) {
  const cfg = { ...DEFAULTS, ...options };
  const cached = options.force ? null : loadCachedSession();
  if (cached?.token && cached.apiUrl === cfg.apiUrl && cached.account === cfg.account) {
    return cached;
  }

  const password = encryptPassword(cfg.password, cfg.cipherKey);
  const body = new URLSearchParams({
    account: cfg.account,
    password,
    code: '',
    timestamp: '',
    origin: 'password',
    grant_type: 'password',
  });

  const res = await fetch(`${cfg.apiUrl.replace(/\/$/, '')}/api/oauth/Login`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
      'jnpf-origin': cfg.origin,
    },
    body: body.toString(),
  });

  const json = await res.json().catch(async () => ({ raw: await res.text() }));
  if (!res.ok || json.code !== 200) {
    throw new Error(`Login failed HTTP ${res.status} code=${json.code} msg=${json.msg || JSON.stringify(json)}`);
  }

  const token = normalizeToken(json.data?.token);
  if (!token) throw new Error('Login OK but no token in response');

  const session = {
    apiUrl: cfg.apiUrl,
    account: cfg.account,
    token,
    expiresAt: decodeJwtExp(token),
    loginAt: new Date().toISOString(),
  };
  saveSession(session);
  return session;
}

/** 带 Bearer 的 fetch；401 时可选重登一次；timeoutMs 默认 60s */
export async function apiRequest(method, urlPath, { body, token, session, retry = true, timeoutMs = 60_000 } = {}) {
  const cfg = { ...DEFAULTS };
  let sess = session || loadCachedSession();
  if (!sess?.token) sess = await login(cfg);
  if (!token) token = sess.token;

  const base = (sess.apiUrl || cfg.apiUrl).replace(/\/$/, '');
  const url = urlPath.startsWith('http') ? urlPath : `${base}${urlPath.startsWith('/') ? '' : '/'}${urlPath}`;

  const headers = {
    Authorization: authHeader(token),
    'jnpf-origin': cfg.origin,
  };
  let payload;
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
    payload = typeof body === 'string' ? body : JSON.stringify(body);
  }

  const res = await fetch(url, {
    method,
    headers,
    body: payload,
    signal: timeoutMs ? AbortSignal.timeout(timeoutMs) : undefined,
  });
  if ((res.status === 401 || res.status === 600) && retry) {
    const fresh = await login({ ...cfg, force: true });
    return apiRequest(method, urlPath, {
      body,
      token: fresh.token,
      session: fresh,
      retry: false,
      timeoutMs,
    });
  }

  const text = await res.text();
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  return { ok: res.ok, status: res.status, json, text, headers: Object.fromEntries(res.headers.entries()) };
}

/** JNPF RESTfulResult：HTTP 200 但 code 可能非 200 */
export function isJnpfOk(result) {
  if (!result?.ok) return false;
  const j = result.json;
  if (j && typeof j === 'object' && 'code' in j) return j.code === 200;
  return true;
}

export function jnpfData(result) {
  const j = result?.json;
  if (j && typeof j === 'object' && 'data' in j) return j.data;
  return j;
}

/** 兼容 PascalCase / camelCase 字段 */
export function pick(obj, ...keys) {
  if (!obj || typeof obj !== 'object') return undefined;
  for (const k of keys) {
    if (obj[k] !== undefined) return obj[k];
  }
  return undefined;
}

// CLI: node jnpf-auth.mjs [--json] [--force]
const isMain = process.argv[1] && path.resolve(process.argv[1]) === path.resolve(fileURLToPath(import.meta.url));
if (isMain) {
  const force = process.argv.includes('--force');
  const asJson = process.argv.includes('--json');
  login({ force })
    .then(s => {
      if (asJson) console.log(JSON.stringify(s, null, 2));
      else console.log(s.token);
    })
    .catch(err => {
      console.error(err.message || err);
      process.exit(1);
    });
}
