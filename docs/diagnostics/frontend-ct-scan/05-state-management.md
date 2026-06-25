# 05 — 状态管理架构扫描

> 扫描日期：2026-06-08
> 扫描范围：jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3

---

## 总览对比

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| 状态管理库 | Pinia | 无（window 全局对象） | Pinia |
| Store 模块数 | 10 | 0（1 个全局对象） | 4（含 1 测试） |
| 持久化方式 | Persistent + localStorage 加密 | sessionStorage (明文) | uni.setStorageSync (明文) |
| 持久化插件 | 自研 Persistent 类 | 无 | 无 (手动) |
| Token 存储 | localStorage + AES 加密 | sessionStorage (明文) | uni.setStorageSync (明文) |
| 跨标签同步 | `storage` 事件 | postMessage | 无 |

---

## 1. jnpf-web-vue3 — 10 个 Pinia Store

### 1.1 全量 Store 清单

| # | Store ID | 文件 | 行数 | 核心职责 |
|---|---|---|---|---|
| 1 | `app-user` | `store/modules/user.ts` | 223 | 认证、Token、用户信息、权限列表、后端菜单 |
| 2 | `app` | `store/modules/app.ts` | 97 | 暗色模式、页面加载、项目配置、多标签设置 |
| 3 | `app-permission` | `store/modules/permission.ts` | 118 | 动态路由构建、菜单列表、`isDynamicAddedRoute` |
| 4 | `app-base` | `store/modules/base.ts` | 123 | 数据字典缓存、字典数据查询 |
| 5 | `app-multiple-tab` | `store/modules/multipleTab.ts` | 353 | 多标签页管理、Keep-Alive 缓存 |
| 6 | `app-locale` | `store/modules/locale.ts` | 61 | 多语言状态 |
| 7 | `app-lock` | `store/modules/lock.ts` | 53 | 锁屏状态、解锁验证 |
| 8 | `app-error-log` | `store/modules/errorLog.ts` | 78 | Ajax 错误日志收集 |
| 9 | `app-generator` | `store/modules/generator.ts` | 63 | 代码生成器表单状态 |
| 10 | `app-organize` | `store/modules/organize.ts` | 117 | 组织/岗位/角色树缓存 |

### 1.2 关键 Store 详解

#### `app-user` — 认证核心

```typescript
state: {
  token?: string;                     // 认证 Token
  userInfo: Nullable<UserInfo>;       // 用户对象
  permissionList: PermissionInfo[];   // 权限授权
  backMenuList: BackMenu[];           // 原始后端菜单
  backRouterList: BackMenu[];         // 路由用菜单
  sessionTimeout: boolean;            // 会话过期标志
  lastUpdateTime: number;             // 最后获取时间戳
}
```

- **getter 降级读取:** 所有 getter 都有回退逻辑 `this.token || getAuthCache(TOKEN_KEY)`
- **持久化:** Token 写入 `Persistent`（localStorage/sessionStorage + AES 加密）
- **关联:** `login()` → `baseStore.resetState()` → `organizeStore.resetState()` → `buildRoutesAction()`
- **退出:** `logout()` → API 调用 → `resetToken()` → 路由 `/login`

#### `app-permission` — 路由构建

```typescript
buildRoutesAction():
  1. userStore.getBackRouterList (原始数据)
  2. transformObjToRoute() (后端菜单 → Vue Router 路由)
  3. flatMultiLevelRoutes() (>2 层压平)
  4. patchHomeAffix() (首页标记不可关闭)
  5. router.addRoute() 逐个注册
```

#### `app-multiple-tab` — 多标签管理

- 17 个 Action 方法
- `cacheTabList: Set<string>` — 缓存的 route name
- `tabList: RouteLocationNormalized[]` — 打开标签列表
- 当前: **不持久化** (`cache: false`)，**无 KeepAlive** (`openKeepAlive: false`)
- 支持: 关闭左/右/其他/全部、拖拽排序、刷新、标题更新

### 1.3 持久化架构

```
Store state → Persistent.setLocal(KEY, value, encryption?)
                    ↓
              Memory 缓存层
                    ↓
            beforeunload → localStorage/sessionStorage
                    ↓
              AES 加密（非 dev 模式下）

读取: Store getter → state || Persistent.getLocal(KEY)
```

### 1.4 Store 间交互图

```
login() → userStore
  ├── baseStore.resetState()
  ├── organizeStore.resetState()
  └── userStore.afterLoginAction()
        └── userStore.getUserInfoAction()
              ├── userStore.setUserInfo()
              ├── userStore.setPermissionList()
              ├── userStore.setBackMenuList/RouterList()
              └── appStore.setProjectConfig({ sysConfigInfo })

/logout 路由守卫 (createStateGuard):
  ├── appStore.resetAllState()
  ├── permissionStore.resetState()
  ├── multipleTabStore.resetState()
  └── userStore.resetState()
```

---

## 2. jnpf-web-datascreen — 无 Store 架构

### 2.1 全局状态机制

**无 Pinia/Vuex。** 使用两个 `window` 级全局对象：

**`window.$glob`:**
```js
{ url, group, themeId, theme, params, query, header }
```
- 通过 URL 查询参数 `?key=value` 自动填充
- 通过 API `/visual-global/list` 全局配置更新

**`window.$website`:**
- 2,411 行的 `public/config.js`
- 包含: 站点元信息、路由配置、OAuth 设置、全部组件类型定义

### 2.2 风险评估

| 问题 | 严重度 |
|---|---|
| 全局对象无响应式 — 修改不触发 UI 更新 | 高 |
| 无类型定义 — 运行时属性拼写错误静默失败 | 高 |
| 跨组件状态同步困难 | 中 |
| 2,411 行单文件 — 难以维护 | 中 |

---

## 3. jnpf-app-vue3 — Pinia (手动持久化)

### 3.1 Store 清单

| # | ID | 文件 | 行数 | 核心职责 |
|---|---|---|---|---|
| 1 | `user` | `store/modules/user.js` | 102 | Token、用户信息、菜单、推送 CID |
| 2 | `app-base` | `store/modules/base.js` | 253 | 字典/组织/岗位/角色/用户树缓存 |
| 3 | `chat` | `store/modules/chat.js` | 112 | WebSocket、IM 未读数、消息列表 |
| 4 | `test` | `store/modules/test.js` | 18 | 模板/示例（可删除） |

### 3.2 关键特征

- **无持久化插件:** 每个 Action 手动调用 `uni.setStorageSync(key, value)`
- **懒加载缓存:** `base.js` 中所有树数据首次访问时从 API 拉取，`if (this.xxx.length) return` 模式
- **chat store:** 使用 `uni.$emit` 跨组件通信（`addMsg`, `updateList`, `getMessageList`）
- **双版本兼容:** `main.js` 中 `#ifndef VUE3` / `#ifdef VUE3` 分别支持 Vue 2 和 Vue 3

### 3.3 持久化键名汇总

| 键名 | 用途 | 读写方式 |
|---|---|---|
| `token` | 认证 Token | uni.setStorageSync / uni.getStorageSync |
| `userInfo` | 用户信息 | 同上 |
| `menuList` | 导航菜单 | 同上 |
| `permissionList` | 权限码 | 同上 |
| `sysConfigInfo` | 系统配置 | 同上 |
| `cid` | Push 客户端 ID | 同上 |
| `rememberAccount` | 记住登录 | 同上 |

---

## 关键发现 (状态管理层)

| # | 发现 | 严重度 | 项目 |
|---|---|---|---|
| 1 | datascreen 无响应式状态管理 — 全局对象修改不触发 UI 更新 | 高 | datascreen |
| 2 | datascreen 2,411 行单文件 `config.js` 极难维护 | 高 | datascreen |
| 3 | app-vue3 `test store` 是模板代码，应清理 | 低 | app-vue3 |
| 4 | web-vue3 KeepAlive 和多标签持久化均关闭 — 用户体验降低 | 中 | web-vue3 |
| 5 | app-vue3 手动持久化模式易出错（遗漏 sync → 刷新丢失状态） | 中 | app-vue3 |
