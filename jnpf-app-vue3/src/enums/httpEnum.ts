/**
 * @description: UniApp 请求枚举 — 与 PC 端 src/enums/httpEnum.ts 完全对齐
 * @jnpf-generated v5.2.0 type=httpEnum platform=uniapp
 *
 * 对齐规则：
 * - SUCCESS / TOKEN_TIMEOUT / TOKEN_LOGGED / TOKEN_ERROR 与 PC 端一致
 * - SUCCESS_ALT (0) 为 UniApp 额外支持的后端备用成功码
 */

/** 响应结果码 — 与 PC 端 ResultEnum 对齐 */
export const ResultEnum = {
  /** 成功 */
  SUCCESS: 200,
  /** 成功（备用码，部分后端接口使用 0 表示成功） */
  SUCCESS_ALT: 0,
  /** Token 超时（需刷新） */
  TOKEN_TIMEOUT: 600,
  /** Token 有效但无此接口权限 */
  TOKEN_LOGGED: 601,
  /** Token 错误（无效/被篡改） */
  TOKEN_ERROR: 602,
} as const;

export type ResultCode = (typeof ResultEnum)[keyof typeof ResultEnum];

/** 请求方法 */
export const RequestEnum = {
  GET: 'GET',
  POST: 'POST',
  PUT: 'PUT',
  DELETE: 'DELETE',
} as const;

export type RequestMethod = (typeof RequestEnum)[keyof typeof RequestEnum];

/** Content-Type */
export const ContentTypeEnum = {
  /** JSON */
  JSON: 'application/json;charset=UTF-8',
  /** form-data qs */
  FORM_URLENCODED: 'application/x-www-form-urlencoded;charset=UTF-8',
  /** form-data upload */
  FORM_DATA: 'multipart/form-data;charset=UTF-8',
} as const;

export type ContentType = (typeof ContentTypeEnum)[keyof typeof ContentTypeEnum];

/**
 * RESTfulResult 统一响应结构
 * 对应后端 RESTfulResult<T> 包装
 */
export interface ApiResponse<T = unknown> {
  code: ResultCode | number;
  msg: string;
  data: T;
  extras?: Record<string, unknown>;
  timestamp?: number;
}
