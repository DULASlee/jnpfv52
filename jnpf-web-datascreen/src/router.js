import { createRouter, createWebHistory } from 'vue-router';
import { hasToken } from '@/utils/auth';

const routes = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/page/login.vue'),
    meta: { public: true }
  },
  {
    path: '/',
    component: () => import('@/page/index.vue'),
    children: []
  },
  {
    path: '/view',
    component: () => import('@/page/view.vue'),
    meta: { public: true }
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/'
  }
];

const vueRouter = createRouter({
  history: createWebHistory(import.meta.env.VITE_APP_BASE),
  routes
});

vueRouter.beforeEach((to, from, next) => {
  if (to.meta && to.meta.public) {
    next();
    return;
  }
  if (!hasToken()) {
    const loginUrl = import.meta.env.VITE_LOGIN_REDIRECT_URL || '/login';
    window.location.href = loginUrl;
    return;
  }
  next();
});

export default vueRouter;