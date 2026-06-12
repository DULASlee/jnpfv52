/**
 * UniApp 请求模板 (Alova)
 * 对齐 ResultEnum 状态码体系
 *
 * @jnpf-generated — UniApp request adapter for JNPF API
 */

export const ResultEnum = {
  /** 成功 */
  SUCCESS: 200,
  /** Token 超时（需刷新） */
  TOKEN_TIMEOUT: 600,
  /** 已登录（Token 有效但无此接口权限） */
  LOGGED_IN: 601,
  /** Token 错误（无效/被篡改） */
  TOKEN_ERROR: 602,
} as const;

export type ResultCode = (typeof ResultEnum)[keyof typeof ResultEnum];

export interface ApiResponse<T = unknown> {
  code: ResultCode | number;
  msg: string;
  data: T;
  extras?: Record<string, unknown>;
  timestamp?: number;
}

/**
 * 创建 Alova 请求实例
 * 自动处理 Token 注入、600/601/602 状态码、HTTP 401
 */
export function createRequest(baseURL: string) {
  const instance = {
    baseURL,
    timeout: 30000,

    /** 请求拦截器 */
    beforeRequest(config: Record<string, unknown>) {
      const token = uni.getStorageSync('token');
      if (token) {
        config.headers = {
          ...(config.headers as Record<string, unknown>),
          Authorization: `Bearer ${token}`,
        };
      }
      return config;
    },

    /** 响应拦截器 */
    afterResponse(response: { status: number; data: ApiResponse }) {
      // HTTP 401 → 未授权
      if (response.status === 401) {
        uni.removeStorageSync('token');
        uni.reLaunch({ url: '/pages/login/index' });
        return Promise.reject(new Error('未登录'));
      }

      const { code, msg } = response.data;

      switch (code) {
        case ResultEnum.SUCCESS:
          return response.data;

        case ResultEnum.TOKEN_TIMEOUT:
          // Token 超时，尝试刷新
          uni.removeStorageSync('token');
          uni.reLaunch({ url: '/pages/login/index' });
          return Promise.reject(new Error('Token 已过期'));

        case ResultEnum.LOGGED_IN:
          // 已登录但无权限
          uni.showToast({ title: msg || '无此权限', icon: 'none' });
          return Promise.reject(new Error('无权限'));

        case ResultEnum.TOKEN_ERROR:
          // Token 无效
          uni.removeStorageSync('token');
          uni.reLaunch({ url: '/pages/login/index' });
          return Promise.reject(new Error('Token 无效'));

        default:
          // 业务错误
          uni.showToast({ title: msg || '请求失败', icon: 'none' });
          return Promise.reject(new Error(msg || '请求失败'));
      }
    },
  };

  return instance;
}

/**
 * RESTful API 包装
 */
export function useApi<T>(baseURL: string) {
  const req = createRequest(baseURL);

  return {
    get: <R = T>(url: string, params?: Record<string, unknown>) =>
      uni.request({ url: `${baseURL}${url}`, method: 'GET', data: params }) as Promise<ApiResponse<R>>,

    post: <R = T>(url: string, data?: Record<string, unknown>) => uni.request({ url: `${baseURL}${url}`, method: 'POST', data }) as Promise<ApiResponse<R>>,

    put: <R = T>(url: string, data?: Record<string, unknown>) => uni.request({ url: `${baseURL}${url}`, method: 'PUT', data }) as Promise<ApiResponse<R>>,

    delete: <R = T>(url: string, data?: Record<string, unknown>) => uni.request({ url: `${baseURL}${url}`, method: 'DELETE', data }) as Promise<ApiResponse<R>>,
  };
}
