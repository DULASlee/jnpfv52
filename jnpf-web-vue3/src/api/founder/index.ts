/**
 * Founder Console API (Phase 6 Day 16-20).
 * 所有 /api/founder/* 端点，注入 X-Founder-Token 认证头.
 */
import { defHttp } from '/@/utils/http/axios';

const FOUNDER_TOKEN_KEY = 'FOUNDER_TOKEN__';

export function getFounderToken(): string | null {
  try {
    return localStorage.getItem(FOUNDER_TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setFounderToken(token: string): void {
  localStorage.setItem(FOUNDER_TOKEN_KEY, token);
}

export function clearFounderToken(): void {
  localStorage.removeItem(FOUNDER_TOKEN_KEY);
}

/** 为请求注入 X-Founder-Token header */
function founderHeaders(): Record<string, string> {
  const token = getFounderToken();
  return token ? { 'X-Founder-Token': token } : {};
}

// ═══════════ Auth ═══════════

/** 设置 TOTP — 匿名端点 */
export function setupTotp(email: string) {
  return defHttp.post({
    url: '/api/founder/auth/setup-totp',
    data: { email },
    headers: { ...founderHeaders() },
  });
}

/** 验证 TOTP 码，签发 founder_token — 匿名端点 */
export function verifyTotp(email: string, code: number) {
  return defHttp.post({
    url: '/api/founder/auth/verify-totp',
    data: { email, code },
    headers: { ...founderHeaders() },
  });
}

/** 获取认证日志 */
export function getAuthLogs(params: { currentPage?: number; pageSize?: number; result?: string } = {}) {
  return defHttp.get({
    url: '/api/founder/auth/logs',
    params,
    headers: { ...founderHeaders() },
  });
}

// ═══════════ Model Config ═══════════

export function configureModel(data: { primaryModel: string; fallbackModel?: string; temperature?: number; maxTokens?: number }) {
  return defHttp.post({
    url: '/api/founder/config/model',
    data,
    headers: { ...founderHeaders() },
  });
}

// ═══════════ Prompt Config ═══════════

export function configurePrompt(data: { templateName: string; content?: string; category?: string }) {
  return defHttp.post({
    url: '/api/founder/config/prompt',
    data,
    headers: { ...founderHeaders() },
  });
}

// ═══════════ Self-Play ═══════════

export function toggleSelfPlay(enabled: boolean) {
  return defHttp.post({
    url: '/api/founder/selfplay/toggle',
    data: { enabled },
    headers: { ...founderHeaders() },
  });
}

export function getSelfPlayStatus() {
  return defHttp.get({
    url: '/api/founder/selfplay/status',
    headers: { ...founderHeaders() },
  });
}
