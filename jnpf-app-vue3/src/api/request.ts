/**
 * UniApp Alova 请求封装
 *
 * 与 PC 端 src/utils/http/axios/index.ts 行为完全对齐：
 * - Token 注入 (Authorization: Bearer)
 * - RESTfulResult 统一响应处理 (200/0 → data)
 * - Token 过期处理 (600/601/602 → 清登 → 登录页)
 * - HTTP 401 处理
 *
 * @jnpf-generated v5.2.0 type=request platform=uniapp
 */

import { createAlova } from 'alova';
import adapterUniapp from '@alova/adapter-uniapp';
import { ResultEnum } from '../enums/httpEnum';
import type { ApiResponse } from '../enums/httpEnum';

// ============================================================
// 配置常量
// ============================================================

/** API 基础地址 — 由 vite.config.js 代理 /api → 后端 */
const BASE_URL = '/api';

/** 请求超时 (ms) */
const TIMEOUT = 30_000;

// ============================================================
// 认证工具函数
// ============================================================

/** 获取存储的 token */
function getToken(): string {
  return uni.getStorageSync('token') || '';
}

/** 清除所有认证数据并跳转登录页 */
function clearToken(): void {
  const keysToRemove = [
    'token',
    'cid',
    'userInfo',
    'permissionList',
    'sysVersion',
    'dynamicModelExtra',
  ];
  keysToRemove.forEach(key => {
    try {
      uni.removeStorageSync(key);
    } catch {
      // 静默处理移除失败（如 key 不存在）
    }
  });

  // 跳转登录页
  uni.reLaunch({ url: '/pages/login/index' });
}

// ============================================================
// Alova 实例创建
// ============================================================

const alovaInstance = createAlova({
  // 基础 URL
  baseURL: BASE_URL,

  // UniApp 适配器
  requestAdapter: adapterUniapp(),

  // 请求超时
  timeout: TIMEOUT,

  // ==========================================================
  // 请求前拦截：注入 Token 与平台标识
  // ==========================================================
  beforeRequest(method) {
    const token = getToken();
    if (token) {
      method.config.headers.Authorization = `Bearer ${token}`;
    }
    // 平台标识（与 PC 端 jnpf-origin: pc 对齐）
    method.config.headers['jnpf-origin'] = 'app';
    method.config.headers['vue-version'] = '3';
  },

  // ==========================================================
  // 响应处理
  // ==========================================================
  responded: {
    /**
     * 响应成功拦截
     *
     * 对齐 PC 端 transformResponseHook：
     * - code === 200 || code === 0 → 返回 data
     * - code === 600/601/602 → 清除 token + 跳转登录页
     * - 其他 code → 显示错误提示并 reject
     */
    onSuccess(response: { data: ApiResponse }, _method: unknown) {
      const { code, msg, data } = response.data;

      // 成功码：200 (标准) 或 0 (备用)
      if (code === ResultEnum.SUCCESS || code === ResultEnum.SUCCESS_ALT) {
        return data;
      }

      // Token 异常：600 / 601 / 602
      if (
        code === ResultEnum.TOKEN_TIMEOUT ||
        code === ResultEnum.TOKEN_LOGGED ||
        code === ResultEnum.TOKEN_ERROR
      ) {
        uni.showToast({ title: msg || '登录已过期，请重新登录', icon: 'none' });
        clearToken();
        return Promise.reject(new Error(msg || 'Token 异常'));
      }

      // 业务错误
      uni.showToast({ title: msg || '请求失败', icon: 'none' });
      return Promise.reject(new Error(msg || '请求失败'));
    },

    /**
     * 响应错误拦截
     *
     * 对齐 PC 端 responseInterceptorsCatch：
     * - HTTP 401 → 清除 token + 跳转登录页
     * - 其他错误 → 提示网络异常
     */
    onError(error: { status?: number; message?: string }, _method: unknown) {
      // HTTP 401 未授权
      if (error.status === 401) {
        uni.showToast({ title: '未登录或登录已过期', icon: 'none' });
        clearToken();
        return Promise.reject(new Error('未登录'));
      }

      // 网络/超时错误
      const errMsg = error.message || '网络异常，请检查网络连接';
      uni.showToast({ title: errMsg, icon: 'none' });
      return Promise.reject(error);
    },
  },
});

export default alovaInstance;

// ============================================================
// 通用实体 CRUD API 工厂
// ============================================================

/**
 * 为指定实体创建标准 RESTful API 方法集合
 *
 * 生成的代码（编译器输出）调用此函数获取类型安全的 API 对象：
 * ```ts
 * const userApi = createEntityApi<UserEntity>('/api/System/User');
 * const list = await userApi.list({ page: 1 }).send();
 * ```
 *
 * @param basePath — API 基础路径，如 '/api/System/User'
 * @returns CRUD 方法集合，每个方法返回 Alova Method 实例
 */
export function createEntityApi<T>(basePath: string) {
  const path = (suffix = '') =>
    `${basePath}${suffix}`;

  return {
    /** 列表查询（带 SWR 缓存，30s 过期） */
    list(params?: Record<string, unknown>) {
      return alovaInstance.Get<T[]>(path(), {
        params,
        cacheFor: {
          mode: 'restore',
          expire: 30_000,
        },
      });
    },

    /** 详情查询 */
    detail(id: string) {
      return alovaInstance.Get<T>(path(`/${id}`));
    },

    /** 新增 */
    create(data: Partial<T>) {
      return alovaInstance.Post<T>(path(), data);
    },

    /** 更新 */
    update(id: string, data: Partial<T>) {
      return alovaInstance.Put(path(`/${id}`), data);
    },

    /** 单条删除 */
    delete(id: string) {
      return alovaInstance.Delete(path(`/${id}`));
    },

    /** 批量删除 */
    batchDelete(ids: string[]) {
      return alovaInstance.Delete(path('/batch'), { ids });
    },
  };
}

export type EntityApi<T> = ReturnType<typeof createEntityApi<T>>;
