# 04 — 路由架构扫描

> 扫描日期：2026-06-08
> 扫描范围：jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3

---

## 总览对比

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| 路由库 | Vue Router 4 | Vue Router 4 | pages.json (UniApp 内置) |
| 路由模式 | History | History | 无（原生导航） |
| 路由类型 | 静态 + 动态（混合） | 静态 + 动态 addRoute | 静态 pages + subpackages |
| 路由数量 | 6 静态根 + ~200 动态 | 4 静态 + 12 动态子 | 14 主包 + 45 分包 = 69 |
| 守卫层数 | 8 层（含 1 禁用） | 1 层（简单 token 检查） | 0 层（无导航守卫） |
| 懒加载 | ✅ 全量 | ✅ 全量 | ✅ UniApp 自动分包 |
| KeepAlive | ❌ 关闭 | ❌ 无 | ❌ 无 |
| Tab 缓存 | ✅ 多标签页管理（未持久化） | ❌ 无 | ✅ 原生 TabBar |

---

## 1. jnpf-web-vue3 — 详细分析

### 1.1 路由体系架构

```
RouteConfig
├── 静态路由 (routes/)
│   ├── index.ts          — RootRoute (/ → /home), LoginRoute, FormShortLink
│   ├── basic.ts          — 404, Redirect, ErrorLog, Common (7 children: home/msg/profile/workflow/email/preview)
│   └── mainOut.ts        — /sso (ignoreAuth)
│
├── 动态路由 (permissionStore.buildRoutesAction)
│   ├── 数据源: userStore.getBackRouterList (后端 API 返回)
│   ├── 转换器: routeHelper.transformObjToRoute()
│   ├── 加载器: asyncImportRoute() — Vite import.meta.glob('../../views/**')
│   └── 扁平化: flatMultiLevelRoutes() — 超过2层压平
│
├── 守卫链 (guard/index.ts, 按序执行)
│   ├── createPageGuard         — loaded 状态追踪
│   ├── createPageLoadingGuard  — UI 加载状态
│   ├── createHttpGuard         — 取消待处理请求 (关闭)
│   ├── createScrollGuard       — 滚动重置
│   ├── createMessageGuard      — 销毁弹窗/通知
│   ├── createProgressGuard     — NProgress 进度条
│   ├── createPermissionGuard   — ★ 核心: 认证+动态路由加载
│   ├── createParamMenuGuard    — 禁用
│   └── createStateGuard        — /login 时重置所有 Store
│
└── 帮助器
    ├── routeHelper.ts (307行) — 8 种菜单类型转换
    └── menuHelper.ts (107行)  — 菜单结构构建
```

### 1.2 动态路由流程

```
1. permissionGuard → hasToken? → isDynamicAddedRoute?
2. permissionStore.buildRoutesAction()
   → userStore.getBackRouterList (原始后端菜单数据)
   → transformObjToRoute()
      ├── type=0 (Category): 递归 children
      ├── type=1 (Menu): 递归 children
      ├── type=2 (功能页面): import.meta.glob 动态匹配
      ├── type=3 (在线功能): ONLINE_MODEL
      ├── type=4 (在线字典): ONLINE_DICT
      ├── type=5 (在线报表): ONLINE_REPORT
      ├── type=6 (大屏): 外部 URL
      ├── type=7 (外链): IFRAME / 外部 URL
      └── type=8 (门户): ONLINE_PORTAL
   → flatMultiLevelRoutes() (压平 3+ 层级)
   → patchHomeAffix() (首页标记不可关闭)
   → router.addRoute() 逐个注册
   → router.addRoute(PAGE_NOT_FOUND_ROUTE) 兜底
```

### 1.3 路由 Meta 字段（19 个）

`title`, `defaultTitle`, `icon`, `hideMenu`, `hideBreadcrumb`, `hideChildrenInMenu`, `affix`, `ignoreAuth`, `modelId`, `relationId`, `isTree`, `frameSrc`, `dynamicLevel`, `realPath`, `loaded`, `hidePathForChildren`

### 1.4 守卫机制详解

| 守卫 | 触发时机 | 功能 |
|---|---|---|
| PageGuard | beforeEach/afterEach | loadedPageMap 追踪；mitt 事件发射 |
| PageLoadingGuard | beforeEach/afterEach | 220ms 延迟防闪烁 |
| HttpGuard | beforeEach | AbortController 取消待处理请求 (当前关闭) |
| ScrollGuard | afterEach | hash 锚点滚动 |
| MessageGuard | beforeEach | 销毁所有 Modal/notification |
| ProgressGuard | beforeEach/afterEach | NProgress 进度条 |
| **PermissionGuard** | **beforeEach** | ★ 认证检查 → 动态路由加载 → 重定向解析 |
| StateGuard | afterEach | /login 路由: 重置全部 Store |

**PermissionGuard 核心逻辑：**
1. 白名单 (`/login`, `/sso`, `/formShortLink`) → 直接放行
2. 无 Token → `/login?redirect=xxx`
3. 有 Token 但未加载动态路由 → `buildRoutesAction()` → `router.addRoute()` 批量添加
4. 从 `/login` 跳转到非首页 404 → 重定向 `/home`

### 1.5 Tab 标签页管理

- **Store:** `multipleTabStore` (353 行, 17 个 Action)
- **显示:** 开启 (`multiTabsSetting.show = true`)
- **KeepAlive:** 关闭 (`openKeepAlive = false`)
- **持久化:** 关闭 (`cache = false` — 刷新丢失所有标签)
- **功能:** 新增/关闭/左关/右关/其他/全部/排序/刷新/标题更新

---

## 2. jnpf-web-datascreen — 详细分析

### 2.1 路由架构

**模式:** History (`createWebHistory`), base = `/DataV/`

**静态路由 (4 条):**
| Path | Name | Component | Meta |
|---|---|---|---|
| `/login` | login | `@/page/login.vue` | `{ public: true }` |
| `/` | — | `@/page/index.vue` | (空) |
| `/view` | — | `@/page/view.vue` | `{ public: true }` |
| `/:pathMatch(.*)*` | — | redirect `/` | — |

**动态路由:** `registerConfig.js` 通过 `router.addRoute()` 添加：
- 9 个子路由（list/category/db/map/document/glob/components/file/record）
- 3 个顶层路由（build, build/:id, view/:id）

### 2.2 路由守卫

**仅 1 层 beforeeach：**
```js
beforeEach((to, from, next) => {
  if (to.meta?.public) next();
  else if (!hasToken()) window.location.href = loginUrl; // 硬跳转
  else next();
});
```

**关键问题：** 无 Token 时使用 `window.location.href` 硬跳转（会丢失 SPA 状态），而非 `router.push`。

### 2.3 独立 Token 检查

`index.vue` 和 `view.vue` 在 `created()` 钩子中各自独立检查 Token，与全局守卫逻辑重复。

---

## 3. jnpf-app-vue3 — 详细分析

### 3.1 路由机制

**不使用 Vue Router。** UniApp 通过 `pages.json` (604 行) 管理全量路由：

```
pages (主包 14 条)
├── launch/        — APP 启动/隐私协议/引导页
├── login/         — 登录/SSO/扫码/第三方
├── index/         — TabBar (首页/消息/流程/应用/我的)
└── formShortLink/ — 外链表单

subPackages (6 分包)
├── pages/portal       — 门户 (3 页)
├── pages/message      — IM/通讯录 (5 页)
├── pages/workFlow     — 流程引擎 (13 页)
├── pages/commonPage   — 通用页面 (2 页)
├── pages/apply        — 应用/动态模型 (11 页)
└── pages/my           — 个人中心/委托 (12 页)
```

### 3.2 导航方式

| API | 用途 |
|---|---|
| `uni.navigateTo` | 页面跳转（最常用） |
| `uni.switchTab` | TabBar 切换 |
| `uni.reLaunch` | 登录/登出/刷新 |
| `uni.navigateBack` | 返回（封装为 `jnpf.goBack()`） |
| `$u.route` | vk-uview-ui 路由封装 |

### 3.3 分包预加载

每个 Tab 预加载对应分包 (`network: "all"`)：
- Message Tab → `pages/message`
- WorkFlow Tab → `pages/workFlow`
- Apply Tab → `pages/apply`
- My Tab → `pages/my`

### 3.4 导航守卫

**无导航拦截器。** `uni.addInterceptor` 仅用于 promisify 适配（Promise 风格回调适配），不拦截导航。

### 3.5 条件编译路由

约 15% 的路由条目使用平台条件编译：
- `#ifdef APP` — APP 启动/引导页
- `#ifndef MP` — 外链表单（小程序限制外链）
- 无 `#ifdef` 的路由全平台通用

---

## 关键发现 (路由层面)

| # | 发现 | 严重度 | 影响范围 |
|---|---|---|---|
| 1 | datascreen 无 Token 时硬跳转丢失 SPA 状态 | 中 | datascreen |
| 2 | datascreen Token 检查逻辑重复（守卫+组件） | 低 | datascreen |
| 3 | uniapp 无导航守卫 — 任何页面可通过 URL 直接访问 | 中 | app-vue3 |
| 4 | web-vue3 KeepAlive 关闭 — 每次切标签页重新渲染 | 中 | web-vue3 |
| 5 | web-vue3 Tab 标签页刷新后全部丢失（未持久化） | 低 | web-vue3 |
| 6 | web-vue3 动态路由使用 `import.meta.glob` 全量匹配，未匹配的显示异常页 | 低 | web-vue3 |

---

## 扫描统计

| 项目 | 路由文件 | 守卫文件 | 帮助器文件 | 总路由条目 |
|---|---|---|---|---|
| web-vue3 | 4 | 5 | 2 | ~206 (6 静态 + ~200 动态) |
| datascreen | 1 | 1 | 0 | 16 |
| app-vue3 | 1 (pages.json) | 0 | 0 | 69 |
