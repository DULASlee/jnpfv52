import { useGlobSetting } from '/@/hooks/setting';
import { getToken } from '/@/utils/auth';

/**
 * 构建 SSE / fetch 流式请求 URL。
 * - 开发环境自动附加 apiUrl 前缀（/dev）使请求经 Vite 代理到达后端
 * - 附加 ?token= 以支持 EventSource（无法自定义 Authorization 头）
 * - fetch 流式请求建议使用 buildFetchSseUrl()，可直接在 Header 传 Authorization
 */
export function buildEventSourceUrl(relativeUrl: string): string {
  const { apiUrl } = useGlobSetting();
  let url = relativeUrl.startsWith('/') ? relativeUrl : `/${relativeUrl}`;

  if (apiUrl && !/^https?:\/\//.test(url)) {
    url = `${apiUrl}${url}`;
  }

  const fullUrl = /^https?:\/\//.test(url) ? new URL(url) : new URL(url, window.location.origin);

  const token = getToken();
  if (token) {
    fullUrl.searchParams.set('token', String(token));
  }

  return fullUrl.toString();
}

/** 架构师设计稿别名，与 buildEventSourceUrl 等效 */
export const buildSSEUrl = buildEventSourceUrl;

/**
 * 构建用于 fetch ReadableStream 的 SSE URL（不附加 ?token=，通过 Authorization 头传）。
 */
export function buildFetchSseUrl(relativeUrl: string): string {
  const { apiUrl } = useGlobSetting();
  let url = relativeUrl.startsWith('/') ? relativeUrl : `/${relativeUrl}`;
  if (apiUrl && !/^https?:\/\//.test(url)) {
    url = `${apiUrl}${url}`;
  }
  if (/^https?:\/\//.test(url)) return url;
  return new URL(url, window.location.origin).toString();
}
