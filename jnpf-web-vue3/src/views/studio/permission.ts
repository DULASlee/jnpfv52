/**
 * Studio 路由守卫 (Sprint 1)
 */
import type { Router } from 'vue-router';
import { useUserStore } from '/@/store/modules/user';
import { useStudioMenuStore } from './store/studio-menu';
import NProgress from 'nprogress';

const TOTP_VERIFY_ROUTE = '/studio/founder/totp-verify';
const LOGIN_ROUTE = '/login';
const FORBIDDEN_ROUTE = '/403';

/**
 * 注册 Studio 路由守卫
 */
export function setupStudioPermissionGuards(router: Router) {
  router.beforeEach(async (to, _from, next) => {
    NProgress.start();

    const userStore = useUserStore();

    // 1. 未登录 → 跳登录页
    if (!userStore.token) {
      if (to.path !== LOGIN_ROUTE) {
        return next({ path: LOGIN_ROUTE, query: { redirect: to.fullPath } });
      }
      return next();
    }

    // 2. 加载菜单（首次）
    const menuStore = useStudioMenuStore();
    if (!menuStore.loaded) {
      await menuStore.loadMenus();
    }

    // 3. Studio 路由权限检查
    if (to.path.startsWith('/studio/') && to.path !== TOTP_VERIFY_ROUTE) {
      const isMenuRoute = isRouteInMenus(to.path, menuStore.menus);

      if ((to.meta as any)?.public) return next();
      if (!isMenuRoute && (to.meta as any)?.requiresMenu !== false) {
        return next(FORBIDDEN_ROUTE);
      }
    }

    // 4. Foundry TOTP 门禁
    if (to.path.startsWith('/studio/foundry/') && to.path !== TOTP_VERIFY_ROUTE) {
      const totpVerified = sessionStorage.getItem('founder_totp_verified');
      if (totpVerified !== 'true') {
        return next({ path: TOTP_VERIFY_ROUTE, query: { redirect: to.fullPath } });
      }
    }

    next();
  });

  router.afterEach(() => {
    NProgress.done();
  });
}

/**
 * 检查路由路径是否存在于菜单树中
 */
function isRouteInMenus(path: string, menus: any[]): boolean {
  for (const menu of menus) {
    if (menu.url === path) return true;
    if (menu.children?.length && isRouteInMenus(path, menu.children)) return true;
  }
  return false;
}
