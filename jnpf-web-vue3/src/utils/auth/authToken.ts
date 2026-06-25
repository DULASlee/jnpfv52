import { getToken } from '/@/utils/auth';

const BEARER_PREFIX = 'Bearer ';
const BEARER_RE = /^Bearer\s+\S/i;

/**
 * 从 JWT Token 中提取指定 claim 值（不验证签名，仅解码 payload）
 */
function getJwtClaim(key: string): string {
  try {
    const token = getToken();
    if (!token) return '';
    const raw = BEARER_RE.test(token) ? token.slice(BEARER_PREFIX.length) : token;
    const payload = JSON.parse(atob(raw.split('.')[1]));
    return payload[key] ?? '';
  } catch {
    return '';
  }
}

/**
 * 获取当前租户 ID（从 JWT 的 TenantId claim 解析）
 */
export function getTenantId(): string {
  return getJwtClaim('TenantId');
}

/**
 * 获取纯 Token（去除可能存在的 Bearer 前缀）
 * 用于：URL 参数、WebSocket 查询参数、直接字符串拼接
 */
export function getRawToken(): string {
  const token = getToken();
  if (!token) return '';
  return BEARER_RE.test(token) ? token.slice(BEARER_PREFIX.length) : token;
}

/**
 * 获取 Authorization 头值（"Bearer xxx" 或 undefined）
 * undefined 会被 axios/fetch 自动过滤，避免发送空 Authorization 头
 * 用于：手动组装 Authorization 请求头
 */
export function getAuthHeader(): string | undefined {
  const token = getToken();
  if (!token) return undefined;
  return BEARER_RE.test(token) ? token : `${BEARER_PREFIX}${token}`;
}

/**
 * 生成 Authorization 请求头对象（推荐 HTTP 调用使用）
 * 无 token 时返回空对象，安全合并到 headers
 * 用于：computed 式上传组件 headers、fetch headers
 */
export function getAuthHeaders(): Record<string, string> {
  const header = getAuthHeader();
  return header ? { Authorization: header } : {};
}
