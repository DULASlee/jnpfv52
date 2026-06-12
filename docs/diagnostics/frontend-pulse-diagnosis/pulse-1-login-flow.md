# Pulse 1: 登录 → 首页 → 权限加载（web-vue3）

> 诊断日期: 2026-06-08
> 诊断方法: 逐文件追踪数据流，标注每个环节的数据格式和转换方式
> 诊断范围: jnpf-web-vue3 登录全链路

---

## 一、完整调用链路图

```
[LoginForm.vue]                   [userStore]                  [API Layer]              [Backend]
     │                                │                            │                       │
     │ 1. 用户输入账号密码             │                            │                       │
     │ 2. MD5(password)               │                            │                       │
     │ 3. AES-ECB(MD5, cipherKey)     │                            │                       │
     │ 4. loginApi({account,          │                            │                       │
     │      password: encrypted}) ────┤                            │                       │
     │                                │ 5. POST /api/oauth/Login ──┤                       │
     │                                │                            │ ──────────────────────→│
     │                                │                            │ ←── { token }          │
     │                                │ 6. setToken(token)        │                       │
     │                                │    ├─ Pinia state          │                       │
     │                                │    └─ Persistent(cache)   │                       │
     │                                │       └─ Memory(7d TTL)   │                       │
     │                                │          └─ AES-ECB       │                       │
     │                                │             └─ localStorage│                       │
     │                                │                            │                       │
     │                                │ 7. afterLoginAction()     │                       │
     │                                │    ├─ getUserInfoAction()─┤ GET /api/oauth/        │
     │                                │    │                      │   CurrentUser ─────────→│
     │                                │    │                      │ ←── { userInfo,        │
     │                                │    │                      │      sysConfigInfo,    │
     │                                │    │                      │      menuList[],       │
     │                                │    │                      │      permissionList[] }│
     │                                │    │                      │                       │
     │                                │    ├─ setUserInfo()       │                       │
     │                                │    ├─ setPermissionList() │                       │
     │                                │    ├─ setBackMenuList()   │                       │
     │                                │    ├─ setBackRouterList() │  ← SAME DATA (bug)    │
     │                                │    │                      │                       │
     │                                │    ├─ buildRoutesAction() │                       │
     │                                │    │   └─ transformObjToRoute(menuList)           │
     │                                │    │      └─ asyncImportRoute() → import.meta.glob│
     │                                │    │      └─ flatMultiLevelRoutes()               │
     │                                │    │                      │                       │
     │                                │    ├─ router.addRoute() × N                       │
     │                                │    ├─ router.addRoute(PAGE_NOT_FOUND)             │
     │                                │    └─ router.replace('/home')                     │
     │                                │                            │                       │
     │ ←── redirect to /home          │                            │                       │
```

---

## 二、各环节详细分析

### 2.1 密码加密（LoginForm.vue:173-174）

```typescript
const password = encryptByMd5(data.password);        // MD5 hash
const encryptPassword = aesEncryption.encryptByAES(password); // AES-ECB
```

**数据变换:**
```
用户输入 "123456"
  → MD5 → "e10adc3949ba59abbe56e057f20f883e"
  → AES-ECB (key from VITE_CIPHER_KEY env, ECB mode, PKCS7)
  → Base64 → "xyzABC..."
  → POST /api/oauth/Login { account, password: "xyzABC...", grant_type: "password" }
```

**安全发现:**
- 使用 MD5（已破解，碰撞攻击可行）— 应升级为 SHA-256
- AES-ECB 模式（同明文=同密文）— 应使用 CBC/GCM
- `VITE_CIPHER_KEY` 默认值需确认是否有硬编码回退

### 2.2 Token 存储（user.ts:73-76）

```typescript
setToken(info: string | undefined) {
  this.token = info ? info : '';
  setAuthCache(TOKEN_KEY, info);  // → Persistent.setLocal(key, value, true)
}
```

**存储层级:**
```
Token 值
  ├── Pinia state (内存，刷新丢失)
  └── Persistent Cache
       ├── Memory (内存，7天 TTL，多tab不共享)
       └── localStorage [AES-ECB 加密]
            key: APP_LOCAL_CACHE_KEY
            value: { TOKEN_KEY: "<encrypted_token>", ... }
            加密密钥: cacheCipher.key = '_11111000001111@'
```

**安全发现:**
- Token 存储使用另一个密钥 `_11111000001111@`（区别于密码加密的 `VITE_CIPHER_KEY`）
- Token 本身无 Bearer 前缀 — 发送时作为 `Authorization: <raw_token>`
- localStorage 加密仅在非开发模式下启用（`enableStorageEncryption = !isDevMode()`）

### 2.3 获取用户信息（user.ts:157-180）

```typescript
// GET /api/oauth/CurrentUser
const res = await getUserInfo();
const { userInfo, sysConfigInfo, menuList = [], permissionList = [] } = res.data;
```

**后端返回数据结构（推断）:**
```typescript
{
  userInfo: { id, account, realName, organizeId, ... },
  sysConfigInfo: { appName, logo, theme, ... },
  menuList: [{ id, fullName, enCode, type, urlAddress, icon, hasChildren, children[], propertyJson }],
  permissionList: [{ modelId, moduleId, enCode, fullName }]
}
```

**关键校验:** 如果 menuList 为空 → 清除 Token → reject("您的权限不足，请联系管理员")

**数据存储问题（user.ts:175-176）:**
```typescript
this.setBackMenuList(menuList);     // ← backMenuList
this.setBackRouterList(menuList);   // ← backRouterList = 完全相同的数据!
```
两个 store 属性存储完全相同的后端菜单数据，没有任何差异。这是冗余存储。

### 2.4 动态路由构建（permission.ts:62-109 + routeHelper.ts:99-226）

**transformObjToRoute — 6种菜单类型 → 路由:**

| type | 名称 | 路由处理 |
|---|---|---|
| 0 | 分类目录 | 递归处理 children，不生成路由 |
| 1 | 模块 | 递归处理 children，不生成路由 |
| 2 | 页面 | `component: path` → `asyncImportRoute()` 用 `import.meta.glob('../../views/**/*.{vue,tsx}')` 匹配 |
| 3 | 在线模型 | `component: 'ONLINE_MODEL'` → `views/common/dynamicModel/index.vue` |
| 4 | 在线字典 | `component: 'ONLINE_DICT'` → `views/common/dynamicDictionary/index.vue` |
| 5 | 在线报表 | `component: 'ONLINE_REPORT'` → `views/common/dynamicDataReport/index.vue` |
| 6 | 数据大屏 | **不生成路由!** 只设置 `e.path = datavUrl + view/{id}?token={TOKEN}` |
| 7 | 外链 | `_self`→IFRAME 路由；其他→直接外链路径 |
| 8 | 门户 | `component: 'ONLINE_PORTAL'` → `views/common/dynamicPortal/index.vue` |

**严重安全发现 (type=6):**
```typescript
// routeHelper.ts:173
e.path = `${globSetting.dataVUrl}view/${moduleId}?token=${getToken()}`;
```
Token 作为 URL 参数嵌入数据大屏链接! 浏览器历史/服务器日志/Referer 头均可泄露 Token。

**严重安全发现 (type=7):**
```typescript
// routeHelper.ts:177
const path = e.urlAddress.replace(/\${jnpfToken}/g, getToken());
```
外链中通过 `${jnpfToken}` 占位符将 Token 传入第三方 URL — Token 泄露给外部系统!

### 2.5 路由守卫（permissionGuard.ts）

**8层守卫执行顺序:**
```
createPageGuard         → 页面加载状态
createPageLoadingGuard  → 页面加载动画
createHttpGuard         → HTTP 请求管理
createScrollGuard       → 滚动位置恢复
createMessageGuard      → 消息清理
createProgressGuard     → 进度条
createPermissionGuard   → ← 权限核心 (本次分析重点)
createStateGuard        → 状态恢复
```

**permissionGuard 决策树:**
```
beforeEach(to, from, next)
│
├─ [workFlowDetail + query.token] → updateToken → 重试
│
├─ [白名单路径: /login, /sso, /formShortLink]
│   ├─ [/login + 有token] → afterLoginAction() → redirect to /或home
│   └─ [其他白名单] → next()
│
├─ [无token]
│   ├─ [meta.ignoreAuth] → next()
│   └─ [需要认证] → redirect /login?redirect=<原路径>
│
├─ [来自login + 目标404 + 非Home] → next(/home)
│
├─ [lastUpdateTime=0] → getUserInfoAction() (刷新用户数据)
│
├─ [动态路由已添加] → next()
│
└─ [动态路由未添加]
    ├─ buildRoutesAction()
    ├─ router.addRoute() × N
    ├─ router.addRoute(PAGE_NOT_FOUND)
    └─ [目标404] → { path: to.fullPath, replace } (重定向)
        [其他] → { path: redirect, replace }
```

**异常路径分析:**

| 异常场景 | 处理方式 | 是否安全 |
|---|---|---|
| Token 过期 (HTTP 600) | VAxios 拦截器 → resetToken → redirect /login | ✅ 但无主动检测 |
| 网络断开 | createHttpGuard 显示加载，超时后无专门处理 | 🟡 无离线提示 |
| 后端返回空 menuList | `resetToken()` + reject("权限不足") | ✅ |
| 后端返回畸形 menuList | `transformObjToRoute` 可能抛异常，无 try-catch | ❌ 未处理 |
| import.meta.glob 找不到组件 | 返回 EXCEPTION_COMPONENT (404 组件) | 🟡 用户看到404而非友好提示 |
| Token 多Tab不同步 | `beforeunload` 回写 + `storage` 事件监听 | 🟡 有竞争条件 |
| SSO 票据过期 | ssoTicket 传后端，后端校验 | ✅ |

---

## 三、数据格式与转换矩阵

### 3.1 菜单数据 → 路由 → 组件

```
BackMenu (后端 JSON)
  { id, fullName, enCode, type, urlAddress, icon, hasChildren, children[], propertyJson }
    │
    ▼ transformObjToRoute()
AppRouteModule (前端路由对象)
  { path, component, name, meta: { title, defaultTitle, icon, modelId, ... } }
    │
    ▼ asyncImportRoute()
    │ import.meta.glob('../../views/**/*.{vue,tsx}')
    │ 字符串匹配: component path → 实际文件路径
    │
    ▼ flatMultiLevelRoutes()
    │ 3+ 级路由 → 2 级 (创建临时 Router 实例解析)
    │
    ▼ router.addRoute()
Vue Router (运行时路由)
```

### 3.2 Token 生命周期

```
创建: loginApi 响应 → setToken() → Pinia + Persistent + localStorage
读取: getToken() → Pinia getter → Persistent.getLocal(TOKEN_KEY) → Memory.get() → localStorage (fallback)
使用: VAxios interceptor → config.headers.Authorization = token
清除: logout() → setToken(undefined) → resetState() → router.push(/login)
刷新: ❌ 无 refresh token 机制
过期检测: ❌ 无主动检测 (依赖后端返回 600)
跨Tab: beforeunload 回写 + storage 事件监听 (有竞争条件)
```

---

## 四、发现汇总

### P0 安全红线 (继承自 CT 扫描)

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| S-1 | Token 嵌入 URL 参数传给 DataV | routeHelper.ts:173 | Token 泄露 |
| S-2 | Token 通过 `${jnpfToken}` 传入外链 | routeHelper.ts:177 | Token 泄露给第三方 |
| S-3 | localStorage 加密密钥硬编码 `_11111000001111@` | encryptionSetting.ts:8 | 可离线解密 |
| S-4 | 密码使用 MD5 (已破解) | cipher.ts:58 | 彩虹表攻击 |

### P1 架构问题 (本次诊断新发现)

| # | 发现 | 位置 | 影响 |
|---|---|---|---|
| A-1 | backMenuList === backRouterList (冗余存储) | user.ts:175-176 | 维护负担 |
| A-2 | getUserInfo getter 有副作用 (回退 localStorage) | user.ts:51 | 不可预测 |
| A-3 | userStore 直接调用 router (违反单向数据流) | user.ts:12,199 | 耦合 |
| A-4 | transformObjToRoute 无异常处理 | routeHelper.ts:99 | 后端畸形数据→前端崩溃 |
| A-5 | permissionGuard 吞异常 catch {} | permissionGuard.ts:38 | 静默失败 |
| A-6 | `as unknown as` 类型断言泛滥 | routeHelper/guards | 零类型安全 |
| A-7 | 无 Token 主动过期检测 | 全局 | 过期 Token 一直用到 600 错误 |

### P2 技术债务

| # | 发现 | 位置 |
|---|---|---|
| E-1 | 双加密系统 (cipher.ts + encryptionSetting.ts 两套密钥) | 全局 |
| E-2 | AES-ECB 模式不安全 (同明文=同密文) | cipher.ts:33 |
| E-3 | 页面组件匹配依赖字符串拼接 + 文件系统扫描 | routeHelper.ts:72-95 |
| E-4 | `import.meta.glob` 静态分析不可追踪动态组件 | routeHelper.ts:49 |

---

## 五、性能观察

| 环节 | 估算耗时 | 说明 |
|---|---|---|
| MD5+AES 加密 | <1ms | 客户端，无影响 |
| loginApi 网络请求 | 50-500ms | 取决于后端 |
| getUserInfo 网络请求 | 50-300ms | 单次请求，含 menuList |
| buildRoutesAction | 10-50ms | import.meta.glob 已预扫描 |
| flatMultiLevelRoutes | 1-5ms | 仅展平路由层级 |

**首屏关键路径:** Login API → 获取 menuList → buildRoutes → addRoute → router.replace → 首页组件加载。网络延迟是主要瓶颈。

---

## 六、改进建议 (未纳入本阶段范围)

1. **Token 不要嵌入 URL** — 改为 postMessage (已在前端扫描报告中提出，将在 F-2 阶段实现)
2. **添加 refresh token 机制** — Token 过期前主动刷新而非被动等 600
3. **统一加密密钥管理** — cipher.ts 和 encryptionSetting.ts 合并为单一加密服务
4. **transformObjToRoute 添加异常保护** — try-catch + 降级为 404
5. **消除 backMenuList/backRouterList 冗余** — 合并为单一数据源
