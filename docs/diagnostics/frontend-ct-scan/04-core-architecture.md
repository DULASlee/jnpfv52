# CT Scan 2: 核心架构扫描报告

> 扫描日期: 2026-06-08
> 扫描范围: jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3
> 扫描维度: 路由系统 / 状态管理 / API层 / 认证权限

---

## 一、路由系统对比

### 1.1 三项目路由方案完全不同

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| **路由库** | Vue Router 4.2.1 | Vue Router 4.1.5 | UniApp pages.json |
| **模式** | createWebHistory | createWebHistory | hash (H5) / 原生 (APP/MP) |
| **路由注册** | 静态7条 + 动态addRoute | 全部静态 (registerRouters内) | 全量声明式 |
| **路由守卫** | 8 层守卫管道 | **无** | App.vue onLaunch + HTTP拦截器 |
| **404处理** | catch-all `/:path(.*)*` → Exception.vue | **无** | UniApp 框架默认 |
| **路由总数** | 7 静态 + N 动态 | 12 条固定 | 12 主包 + 6 分包 (~55页) |
| **动态路由** | 后端 menuList → transformObjToRoute → addRoute | 无 | 无 (pages.json 编译时确定) |

### 1.2 jnpf-web-vue3: 8层守卫管道 (最复杂)

```
beforeEach:
  1. createPageGuard        → 追踪已加载页面，emit routeChange
  2. createPageLoadingGuard  → 控制全局加载状态
  3. createHttpGuard         → 路由切换时取消所有pending HTTP请求
  4. createMessageGuard      → 关闭所有弹窗/通知
  5. createProgressGuard     → nProgress 进度条
  6. createPermissionGuard   → ★ 核心: token检查/动态路由注入/白名单/重定向

afterEach:
  7. createScrollGuard       → hash变化时滚动到顶部
  8. createStateGuard        → 导航到/login时重置所有Store
```

**权限守卫核心逻辑 (permissionGuard.ts):**
- 白名单路径: `/login`, `/sso`, `/formShortLink` — 直接放行
- 无token + 非ignoreAuth → 重定向 `/login?redirect=原路径`
- 有token + 动态路由未添加 → `buildRoutesAction()` → `router.addRoute()` 逐条注入
- 从login来 + 目标404 + 非首页 → 重定向到首页 (防止登录后闪现404)

### 1.3 jnpf-web-datascreen: 零守卫 (最大风险)

路由表在 `registerConfig.js:51-107` 硬编码，12条路由一次性 `router.addRoute()` 注入。**无任何导航守卫。** 任何人可直接访问 `/build` 编辑器路由。认证完全依赖外部宿主系统传递 token URL 参数。

### 1.4 jnpf-app-vue3: 条件编译的多平台路由

- 主包 + 6 个分包，通过 `preloadRule` 预加载4个核心分包
- APP 独有启动序列: launch/index → policy → guide
- MP 禁用 formShortLink (外链表单)
- TabBar 5项，标签使用 i18n 占位符

---

## 二、状态管理对比

### 2.1 方案差异

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| **方案** | Pinia 2.1.3 (9 stores) | **window.$glob + provide/inject** | Pinia (4 stores) |
| **持久化** | 加密 localStorage + Memory 双层 | localStorage (仅token) | uni.setStorageSync (手动) |
| **Store间通信** | 直接调用其他store action | Object.defineProperty setter | uni.$emit/$on 事件总线 |
| **注销清理** | createStateGuard 统一 resetState | **无** | logout() + resetToken() |
| **类型安全** | TypeScript 全覆盖 | 无 (纯JS window globals) | 无 |

### 2.2 jnpf-web-vue3: 9个Store依赖关系

```
userStore ←→ permissionStore (menuList → buildRoutes)
userStore  → baseStore (登录时reset)
userStore  → organizeStore (登录时reset)
userStore  → appStore (sysConfig注入)
appStore   → Persistent (PROJ_CFG_KEY)
localeStore → localStorage
lockStore   → localStorage + userStore(API)
multipleTabStore → localStorage (可选)
errorLogStore (独立，axios异常写入)
generatorStore (独立，代码生成状态)
```

**反模式:**
- `backMenuList` 和 `backRouterList` 存储相同数据 (user.ts:85-89)
- `getUserInfo` getter 回退到 localStorage (不纯的getter)
- Store 直接调用 `router.push()` (耦合)
- `generatorStore` 无 resetState

### 2.3 jnpf-web-datascreen: 全局window对象 + mixin

```
window.$website  ← public/config.js (~2400行, 应用配置)
window.$glob     ← axios.js (运行时全局状态)
  ├── .url       ← 屏体数据API地址
  ├── .group     ← 当前分组 (defineProperty setter → 组件过滤)
  ├── .themeId   ← 当前主题 (defineProperty setter → 容器刷新)
  ├── .theme     ← 主题色板对象
  ├── .params    ← URL查询参数
  ├── .query     ← 全局请求参数 (合并到所有API调用)
  ├── .header    ← 全局请求头 (合并到所有API调用)
  └── .*         ← 服务端全局变量 (动态注入)

mixin (provide/inject):
  main/contain → 页面实例 (build.vue 或 view.vue)
     └── nav[]  ← ★ 核心状态: 组件树数组
          └── active[] ← 多选中的组件索引
```

**严重问题:**
- 全局可变状态，无任何封装
- `Object.defineProperty` 实现 setter 副作用，隐式耦合
- `nav[]` 数组深度监听触发历史记录 (300ms 防抖)
- 撤销/重做仅内存存储，无持久化

### 2.4 jnpf-app-vue3: 手动同步 Pinia ↔ Storage

每次 setter 同时写 Pinia state 和 `uni.setStorageSync`:
```javascript
setToken(token) {
    this.token = token
    uni.setStorageSync('token', token)  // 手动双写
}
```

---

## 三、API层对比

### 3.1 HTTP客户端差异

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| **客户端** | VAxios (Axios封装类) | Axios 0.19.0 单例 | uni.request 封装 |
| **超时** | 1,000,000ms | 100,000ms | define.timeout |
| **Token注入** | Authorization header (无Bearer前缀) | Authorization header | Authorization header |
| **重试** | GET专用, 5次, 100ms间隔 | **无** | **无** |
| **取消** | CancelToken + pendingMap | **无** | **无** (uni.request不支持) |
| **代理** | Vite proxy /dev → localhost:5000 | `/visual/proxy` 服务端代理 | Vite proxy + 条件编译 |
| **错误码** | 600/601/602 → 强制登出 | code≠200 → ElMessage + reject | 600/601/602 → 1.5s后reLaunch |
| **401处理** | setToken(undefined) → logout | **无** | **无** (仅检查body code) |

### 3.2 jnpf-web-vue3 VAxios 完整管道

```
请求:
  beforeRequestHook (URL前缀/日期格式化/参数重组)
  → supportFormData (FormData编码)
  → axiosInstance.request()
  → requestInterceptors (jnpf-origin:pc / vue-version:3 / Authorization)
  → AxiosCanceler.addPending() (取消重复请求)

响应:
  AxiosCanceler.removePending()
  → responseInterceptors (透传)
  → transformResponseHook (code检查, 600系列强制登出, 错误展示)
  → .catch: 取消/超时/网络错误/HTTP状态码 → checkStatus → retry/errorLog
```

### 3.3 共同问题

1. **Token 格式不统一**: 三项目都直接发送原始 token (无 "Bearer " 前缀)
2. **无 Token 刷新机制**: 三个项目都没有 refresh token 逻辑, 过期直接踢出
3. **错误处理不一致**: web-vue3 有三级错误处理, datascreen 仅检查 code≠200, app 仅处理 600 系列
4. **加密密钥相同**: 三项目使用相同的 AES 密钥 `'EY8WePvjM5GGwQzn'`

---

## 四、认证权限对比

### 4.1 认证模型

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| **登录页** | ✅ Login.vue + LoginForm.vue | **无** | ✅ pages/login/index.vue |
| **Token来源** | POST /api/oauth/Login | URL 参数 `?token=xxx` | POST /api/oauth/Login |
| **Token存储** | 加密localStorage (AES) | localStorage (明文) | uni.setStorageSync |
| **密码加密** | MD5 → AES-ECB | — | MD5 → AES-ECB |
| **SSO** | ✅ 3种 (全量/社交/redirect) | — | ✅ 3种 (微信/QQ/SSO ticket) |
| **扫码登录** | ✅ | — | ✅ scanLogin |
| **刷新Token** | **无** | **无** | **无** |
| **登出清理** | 7步完整清理 | **无** | logout API + removeStorageSync |
| **多窗口同步** | beforeunload 同步 | — | — |
| **会话过期处理** | ROUTE_JUMP / PAGE_COVERAGE | **无** | toast + 1.5s后reLaunch |

### 4.2 权限模型

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| **模型** | RBAC + 资源级 (modelId) | **无** | RBAC + 资源级 (modelId) |
| **按钮权限** | v-auth 指令 + hasBtnPermission | — | hasBtnP (eval-based) |
| **列权限** | hasColumnPermission | — | hasP |
| **表单权限** | hasFormPermission | — | hasFormP |
| **数据权限** | authorize.ts API | — | — |
| **菜单权限** | 后端过滤 menuList | — | 后端过滤 menuList |
| **路由权限** | 动态路由注入 | — | pages.json 编译时 |

### 4.3 jnpf-web-vue3 权限数据结构

```typescript
PermissionInfo {
  modelId: string;     // 绑定到 route.meta.modelId
  button: [{ enCode: "btn_edit", ... }];
  column: [{ enCode: "column_name", ... }];
  form:   [{ enCode: "field_name", ... }];
  resource: [{ ... }];  // 数据级权限
}
```

### 4.4 jnpf-web-datascreen 权限: 仅有屏幕密码

唯一访问控制: `page/group/container.vue:322-342` — 发布屏幕可设密码。但这是**客户端校验**, 数据在密码校验前已加载。

---

## 五、架构差异矩阵 (热力图)

| 能力 | web-vue3 | datascreen | app-vue3 |
|---|---|---|---|
| 路由守卫 | 🟢 8层管道 | 🔴 零 | 🟡 onLaunch + HTTP拦截 |
| 动态路由 | 🟢 后端驱动 | 🔴 硬编码 | 🔴 编译时 pages.json |
| 状态管理 | 🟢 Pinia 9 stores | 🔴 window globals | 🟡 Pinia 4 stores |
| 持久化 | 🟢 加密双层 | 🔴 明文localStorage | 🟡 手动双写 |
| HTTP封装 | 🟢 VAxios 完整 | 🔴 Axios裸单例 | 🟡 uni.request薄封装 |
| 错误处理 | 🟢 三级 | 🔴 一级 | 🟡 二级 |
| 请求取消 | 🟢 CancelToken | 🔴 无 | 🔴 无 |
| 重试 | 🟢 GET 5次 | 🔴 无 | 🔴 无 |
| 认证 | 🟢 完整 | 🔴 外部依赖 | 🟢 完整 |
| 权限 | 🟢 4级 (按钮/列/表单/数据) | 🔴 无 | 🟡 3级 (按钮/列/表单) |
| SSO | 🟢 3种 | 🔴 无 | 🟢 3种 |
| 多平台 | 🔴 PC only | 🟡 UMD嵌入 | 🟢 8平台 |

---

## 六、关键发现 (P0)

1. **datascreen 零认证零权限**: 任何人知道 URL 即可访问编辑器。Token 通过查询参数明文传递。
2. **datascreen 全局可变状态**: `window.$glob` 和 `window.$website` 随时可被任何代码修改，无任何保护。
3. **三项目均无 Token 刷新**: 过期直接踢出，用户体验差。
4. **加密密钥三项目相同且硬编码**: `'EY8WePvjM5GGwQzn'` 出现在所有三个项目的源码中。
5. **datascreen 屏幕密码形同虚设**: 客户端校验，数据已加载。
