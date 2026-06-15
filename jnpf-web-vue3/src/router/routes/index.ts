import type { AppRouteRecordRaw } from '/@/router/types';

import { PAGE_NOT_FOUND_ROUTE, REDIRECT_ROUTE, COMMON_ROUTE } from '/@/router/routes/basic';

import { mainOutRoutes } from './mainOut';
import { PageEnum } from '/@/enums/pageEnum';
import { t } from '/@/hooks/web/useI18n';

// 根路由
export const RootRoute: AppRouteRecordRaw = {
  path: '/',
  name: 'Root',
  redirect: PageEnum.BASE_HOME,
  meta: {
    title: 'Root',
  },
};

export const LoginRoute: AppRouteRecordRaw = {
  path: '/login',
  name: 'Login',
  component: () => import('/@/views/basic/login/Login.vue'),
  meta: {
    title: t('routes.basic.login'),
  },
};
// 表单外链
export const FormShortLinkRoute: AppRouteRecordRaw = {
  path: '/formShortLink',
  name: 'FormShortLink',
  component: () => import('/@/views/common/formShortLink/index.vue'),
  meta: {
    title: '',
  },
};

// Studio AI routes (D-1: 2026-06-20 — QuickAppEntry + ProjectDashboard 拆分)
export const StudioQuickAppRoute: AppRouteRecordRaw = {
  path: '/studio/expert/quick-app-entry',
  name: 'QuickAppEntry',
  component: () => import('/@/views/expert/QuickAppEntry.vue'),
  meta: {
    title: '快速创建',
    hideMenu: true,
  },
};

export const StudioProjectsRoute: AppRouteRecordRaw = {
  path: '/studio/expert/my-projects',
  name: 'ProjectDashboard',
  component: () => import('/@/views/expert/ProjectDashboard.vue'),
  meta: {
    title: '我的项目',
  },
  children: [
    {
      path: ':id',
      name: 'PipelineDetail',
      component: () => import('/@/views/expert/PipelineManager.vue'),
      meta: {
        title: '流水线详情',
        hideMenu: true,
      },
    },
  ],
};

// Founder TOTP verify page (D-4: 2026-06-20)
export const FounderTotpVerifyRoute: AppRouteRecordRaw = {
  path: '/studio/founder/totp-verify',
  name: 'FounderTotpVerify',
  component: () => import('/@/views/founder/FounderLogin.vue'),
  meta: {
    title: '创始人二次认证',
    hideMenu: true,
    ignoreAuth: true,
  },
};

// ModelPlayground (D-2: 2026-06-20 — P1)
export const ModelPlaygroundRoute: AppRouteRecordRaw = {
  path: '/studio/dev/model-playground',
  name: 'ModelPlayground',
  component: () => import('/@/views/expert/ModelPlayground.vue'),
  meta: {
    title: '模型测试场',
  },
};

// ArchitectReview (D-3: 2026-06-20 — P1)
export const ArchitectReviewRoute: AppRouteRecordRaw = {
  path: '/studio/dev/ai-review',
  name: 'ArchitectReview',
  component: () => import('/@/views/expert/ArchitectReview.vue'),
  meta: {
    title: 'AI 架构评审',
  },
};

// Basic routing without permission
// 未经许可的基本路由
export const basicRoutes = [
  LoginRoute,
  FormShortLinkRoute,
  RootRoute,
  StudioQuickAppRoute,
  StudioProjectsRoute,
  ModelPlaygroundRoute,
  ArchitectReviewRoute,
  FounderTotpVerifyRoute,
  ...mainOutRoutes,
  REDIRECT_ROUTE,
  PAGE_NOT_FOUND_ROUTE,
  COMMON_ROUTE,
];
