# 07 — 认证与权限架构扫描

> 扫描日期：2026-06-08
> 扫描范围：jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3

---

## 总览对比

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| 登录方式 | 密码/SSO/第三方/扫码 | 外部重定向 | 密码/微信/QQ/SSO/扫码 |
| Token 存储 | localStorage + AES加密 | sessionStorage 明文 | uni.storage 明文 |
| Token 刷新 | 无（600=登出） | 无 | 无（600/601/602=登出） |
| 路由权限 | v-auth 指令 + hasBtnP | 二元（有Token/无Token） | hasP/hasFormP/hasBtnP |
| 按钮权限 | ✅ permissionList[modelId].button | ❌ 无 | ✅ permissionList[modelId].button |
| 表单权限 | ✅ column/form 级别 | ❌ 无 | ✅ column/form 级别 |
| 多租户前端 | 租户选择弹窗（SSO多绑定） | 无 | 无 |
| 登出流程 | API + 清State + 路由跳转 | 无登出函数 | API + 清State + 清Storage |

---

## 1. jnpf-web-vue3 — 最完整的认证体系

### 1.1 登录流程

```
LoginForm → MD5(password) → AES-ECB(hex) → POST /api/oauth/Login
  ├── grant_type: 'password' (表单URL编码)
  ├── 成功: setToken → getCurrentUser → buildRoutesAction → redirect /home
  └── 失败: 刷新验证码
```

**其他登录方式:**
- SSO: iframe 加载 SSO URL → 轮询 `getTicketStatus()` → `updateToken`
- 第三方: popup 窗口 OAuth → 轮询 → `updateToken`
- 扫码: 获取二维码 → 轮询状态

### 1.2 Token 管理

```typescript
// 存储链
Pinia state.token ← Persistent (localStorage/sessionStorage + AES加密)

// 获取链 (getter降级)
get token(): state.token || Persistent.getLocal(TOKEN_KEY)

// 请求注入 (interceptor)
headers['Authorization'] = token  // 无 Bearer 前缀
headers['jnpf-origin'] = 'pc'
headers['vue-version'] = '3'
```

### 1.3 权限体系 (三层)

**路由权限:** `v-auth` 指令 — 无权限则 `removeChild()` 移除 DOM
**函数权限:** `hasBtnP(value)` / `hasColumnP(value)` / `hasFormP(value)`
**权限数据:** `permissionList: PermissionInfo[]`
```typescript
interface PermissionInfo {
  modelId: string;
  button: PermissionChildItem[];   // btn-edit, btn-delete
  column: PermissionChildItem[];   // 字段级
  form: PermissionChildItem[];     // 表单级
}
```

### 1.4 登出

`logout()` → `POST /api/oauth/Logout` → `resetToken()` → `router.push('/login')`
路由守卫 `createStateGuard`: `/login` 页面自动重置所有 Store

---

## 2. jnpf-web-datascreen — 极简认证

### 2.1 认证流程

```
页面加载 → initTokenListener()
  ├── postMessage 监听: { type: 'JNPF_TOKEN', token: '...' }
  └── URL参数(废弃): ?token=...

Token → sessionStorage['datascreen_token']
路由守卫 → hasToken()? → next() : window.location.href = loginUrl
```

### 2.2 风险评估

- **无登出功能:** `clearToken()` 定义了但从未调用
- **无权限体系:** 二元认证（有Token=全权限）
- **postMessage 注入:** 允许 `'*'` 通配符来源
- **Token 会话隔离:** sessionStorage → 关闭浏览器即失效

---

## 3. jnpf-app-vue3 — 多端适配

### 3.1 登录流程

```
密码登录: MD5(password) → AES-ECB → POST /api/oauth/Login (grant_type:password)
微信登录: uni.login({provider:'weixin'}) → uni.getUserProfile() → socials登录
QQ登录: uni.login({provider:'qq'}) → uni.getUserInfo({provider:'qq'})
SSO登录: getTicket() → redirect SSO URL
扫码登录: uni.scanCode()
```

### 3.2 权限体系

`libs/permission.js` (147 行):
```js
hasP(enCode, menuIds)     // 列权限
hasFormP(enCode, menuIds) // 表单权限
hasBtnP(enCode, menuIds)  // 按钮权限
```

全局暴露: `Vue.prototype.$permission` / `app.config.globalProperties.$permission`

### 3.3 WebSocket 强制登出

`libs/chat.js`: 收到服务端 `"logout"` 消息 → 清 token → `uni.reLaunch('/pages/login/index')`

---

## 关键发现 (认证层)

| # | 发现 | 严重度 | 项目 |
|---|---|---|---|
| 1 | 三项目均无 Token 自动刷新 | 中 | 全部 |
| 2 | datascreen 无登出、无权限体系 | 高 | datascreen |
| 3 | datascreen postMessage 域验证可绕 (`*`) | 高 | datascreen |
| 4 | 密码传输使用 ECB 模式（已知不安全） | 高 | web-vue3, app |
| 5 | 三项目加密密钥相同 (`EY8WePvjM5GGwQzn`) | 高 | 全部 |
| 6 | app-vue3 登录页有 `index - 副本.vue` 备份文件 | 低 | app-vue3 |
