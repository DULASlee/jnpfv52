/**
 * Studio 菜单 API (Sprint 1)
 */
import { defHttp } from '/@/utils/http/axios';
import type { StudioMenuItem, TotpVerifyRequest, TotpVerifyResponse } from '../types/menu';

/**
 * 获取当前用户可见的菜单树
 */
export function getUserMenus() {
  return defHttp.get<StudioMenuItem[]>({ url: '/api/studio/menu/user-menus' });
}

/**
 * 标记菜单已读（清除红点）
 */
export function markBadgeRead(menuId: number) {
  return defHttp.post({ url: '/api/studio/menu/badge/read', data: { menuId } });
}

/**
 * 验证 TOTP 码
 */
export function verifyTotp(data: TotpVerifyRequest) {
  return defHttp.post<TotpVerifyResponse>({ url: '/api/studio/founder/auth/verify', data });
}

/**
 * 获取 TOTP 认证状态
 */
export function getTotpStatus() {
  return defHttp.get({ url: '/api/studio/founder/auth/status' });
}
