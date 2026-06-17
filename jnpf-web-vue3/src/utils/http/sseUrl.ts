import { useGlobSetting } from '/@/hooks/setting';

/**
 * 构建 EventSource URL，与 defHttp/axios 的 apiUrl 前缀规则保持一致。
 * 开发环境 VITE_GLOB_API_URL=/dev 时，axios 走 /dev/api/...，SSE 也必须走同一路径。
 */
export function buildEventSourceUrl(relativeUrl: string): string {
  const { apiUrl } = useGlobSetting();
  let url = relativeUrl.startsWith('/') ? relativeUrl : `/${relativeUrl}`;

  if (apiUrl && !/^https?:\/\//.test(url)) {
    url = `${apiUrl}${url}`;
  }

  if (/^https?:\/\//.test(url)) return url;
  return new URL(url, window.location.origin).toString();
}
