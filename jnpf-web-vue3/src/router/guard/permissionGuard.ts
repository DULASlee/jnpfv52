import type { Router, RouteRecordRaw } from 'vue-router';

import { usePermissionStoreWithOut } from '/@/store/modules/permission';

import { PageEnum } from '/@/enums/pageEnum';
import { useUserStoreWithOut } from '/@/store/modules/user';

import { PAGE_NOT_FOUND_ROUTE } from '/@/router/routes/basic';

const LOGIN_PATH = PageEnum.BASE_LOGIN;
const SSO_PATH = PageEnum.BASE_SSO;
const BASE_FORM_SHORT_LINK_PATH = PageEnum.BASE_FORM_SHORT_LINK;

const whitePathList: PageEnum[] = [LOGIN_PATH, SSO_PATH, BASE_FORM_SHORT_LINK_PATH];

// Founder path prefix (D-4: 2026-06-20 — TOTP session binding)
const FOUNDER_PATH_PREFIX = '/studio/founder';
const FOUNDER_TOTP_PATH = '/studio/founder/totp-verify';
const FOUNDER_SESSION_KEY = 'founder_totp_session';

function isFounderSessionValid(): boolean {
  try {
    const raw = localStorage.getItem(FOUNDER_SESSION_KEY);
    if (!raw) return false;
    const session = JSON.parse(raw);
    return session?.expiresAt && Date.now() < session.expiresAt;
  } catch {
    return false;
  }
}

export function createPermissionGuard(router: Router) {
  const userStore = useUserStoreWithOut();
  const permissionStore = usePermissionStoreWithOut();
  router.beforeEach(async (to, from, next) => {
    const token = userStore.getToken;

    // Founder TOTP guard (D-4): redirect to TOTP verify if session expired/missing
    if (to.path.startsWith(FOUNDER_PATH_PREFIX) && to.path !== FOUNDER_TOTP_PATH && !isFounderSessionValid()) {
      next({
        path: FOUNDER_TOTP_PATH,
        query: { redirect: to.fullPath },
        replace: true,
      });
      return;
    }

    // Founder session is valid and navigating to TOTP page → skip to target
    if (to.path === FOUNDER_TOTP_PATH && isFounderSessionValid()) {
      const redirect = (to.query.redirect as string) || '/studio/founder/console';
      next({ path: redirect, replace: true });
      return;
    }

    if (to.path == '/workFlowDetail' && to.query.token && token != to.query.token) {
      userStore.updateToken(to.query.token as string);
      next({ ...to, replace: true });
      return;
    }

    // Whitelist can be directly entered
    if (whitePathList.includes(to.path as PageEnum)) {
      if (to.path === LOGIN_PATH && token) {
        const isSessionTimeout = userStore.getSessionTimeout;
        try {
          await userStore.afterLoginAction();
          if (!isSessionTimeout) {
            next((to.query?.redirect as string) || '/');
            return;
          }
        } catch {}
      }
      next();
      return;
    }

    // token does not exist
    if (!token) {
      // You can access without permission. You need to set the routing meta.ignoreAuth to true
      if (to.meta.ignoreAuth) {
        next();
        return;
      }

      // redirect login page
      const redirectData: { path: string; replace: boolean; query?: Recordable<string> } = {
        path: LOGIN_PATH,
        replace: true,
      };
      if (to.path) {
        redirectData.query = {
          ...redirectData.query,
          redirect: to.path,
        };
      }
      next(redirectData);
      return;
    }

    // Jump to the 404 page after processing the login
    if (from.path === LOGIN_PATH && to.name === PAGE_NOT_FOUND_ROUTE.name && to.fullPath !== PageEnum.BASE_HOME) {
      next(PageEnum.BASE_HOME);
      return;
    }

    // get userinfo while last fetch time is empty
    if (userStore.getLastUpdateTime === 0) {
      try {
        await userStore.getUserInfoAction();
      } catch (err) {
        next();
        return;
      }
    }

    if (permissionStore.getIsDynamicAddedRoute) {
      next();
      return;
    }

    const routes = await permissionStore.buildRoutesAction();

    routes.forEach(route => {
      router.addRoute(route as unknown as RouteRecordRaw);
    });

    router.addRoute(PAGE_NOT_FOUND_ROUTE as unknown as RouteRecordRaw);

    permissionStore.setDynamicAddedRoute(true);

    if (to.name === PAGE_NOT_FOUND_ROUTE.name) {
      // 动态添加路由后，此处应当重定向到fullPath，否则会加载404页面内容
      next({ path: to.fullPath, replace: true, query: to.query });
    } else {
      const redirectPath = (from.query.redirect || to.path) as string;
      const redirect = decodeURIComponent(redirectPath);
      const nextData = to.path === redirect ? { ...to, replace: true } : { path: redirect };
      next(nextData);
    }
  });
}
