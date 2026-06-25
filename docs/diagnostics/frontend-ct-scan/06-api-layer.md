# 06 — API 请求层架构扫描

> 扫描日期：2026-06-08
> 扫描范围：jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3

---

## 总览对比

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| HTTP 客户端 | Axios (VAxios 封装) | Axios | uni.request |
| TypeScript 类型 | ✅ 完整泛型 | ❌ JavaScript | ❌ JavaScript |
| 请求拦截器 | 5 层管道 | 全局拦截器 | 内联逻辑 |
| 响应拦截器 | Transform + checkStatus | 简单 code 检查 | code 检查 + 自动登出 |
| Token 刷新 | 无（600 登出） | 无（600 登出） | 无（600 登出） |
| 请求取消 | CancelToken Map | 无 | 无 |
| 请求重试 | GET 仅（默认关闭） | 无 | 无 |
| 数据解包 | `res.data.data` | 直接 `res.data` | 直接 `res.data` |
| API 文件数 | ~68 (TypeScript) | 9 (JavaScript) | ~20 (JavaScript) |
| 超时时间 | 1,000,000ms (~16.7min) | 100,000ms | 1,000,000ms |

---

## 1. jnpf-web-vue3 — 最成熟

### 1.1 架构层次

```
defHttp (单例)
  └─ VAxios 类
       ├─ beforeRequestHook — URL 拼接、日期格式化、防缓存 _t
       ├─ requestInterceptors — jnpf-origin:pc, vue-version:3, Authorization
       ├─ transformResponseHook — code=200 成功, code=600/601/602 Token 过期
       ├─ responseInterceptorsCatch — HTTP 错误 + GET 重试
       ├─ AxiosCanceler — 重复请求取消 (CancelToken Map)
       └─ AxiosRetry — GET 请求重试 (默认关闭)
```

### 1.2 请求拦截器

```typescript
headers['jnpf-origin'] = 'pc';
headers['vue-version'] = '3';
headers['Authorization'] = token;  // 无 Bearer 前缀 (authenticationScheme: '')
```

### 1.3 响应处理

**业务码处理:**
| code | 含义 | 行为 |
|---|---|---|
| 200 | 成功 | 返回 `res.data` |
| 600 | Token 超时 | 清除 Token → 强制登出 |
| 601 | 异地登录 | 清除 Token → 强制登出 |
| 602 | Token 错误 | 清除 Token → 强制登出 |
| 其他 | 业务错误 | 显示错误消息 (modal/message/none) |

**HTTP 状态码:**
| 状态码 | 行为 |
|---|---|
| 401 | `PAGE_COVERAGE` 模式: 覆盖登录弹窗 / `ROUTE_JUMP` 模式: 跳转登录页 |
| 403/404/405/408 | 显示错误消息 |
| 500-505 | 显示错误消息 |

### 1.4 错误显示模式

`errorMessageMode` 三档:
- `'none'` — 不自动显示（catch 中的默认值）
- `'message'` — `createMessage.error()` toast
- `'modal'` — `createErrorModal()` 弹窗

---

## 2. jnpf-web-datascreen — 轻量但灵活

### 2.1 架构

```js
axios (单例, timeout=100000ms)
  ├─ 请求拦截器: URL 变量替换(${name}), Base URL 拼接, Header/Query 合并, 代理转发
  ├─ Token 注入: headers['Authorization'] = getToken()
  └─ 响应拦截器: code!=200 → ElMessage + reject
```

### 2.2 特殊功能

- **URL 变量替换:** `${varName}` → `window.$glob[varName]`
- **代理转发:** `config.headers.proxy` → 重写为 POST `/visual/proxy`
- **Token 注入:** 从 `sessionStorage['datascreen_token']` 获取，无 `Bearer` 前缀

### 2.3 风险

- `validateStatus: 200 <= status <= 500` — 接受所有 500 以下的响应为合法
- 无请求取消 — 页面切换时未完成请求可能更新错误的组件状态

---

## 3. jnpf-app-vue3 — 原生适配

### 3.1 架构

```js
function request(config) {
  const token = uni.getStorageSync('token') || '';
  uni.request({
    header: {
      'Content-Type': 'application/json',
      'jnpf-origin': 'app',
      'vue-version': '3',
      'Accept-Language': locale,
      'Authorization': token
    },
    success: (res) => {
      if (statusCode === 200 && res.data.code == 200) resolve(res.data);
      else { toast(data.msg); if (600/601/602) uni.reLaunch('/pages/login/index'); }
    },
    fail: () => { toast('连接服务器失败'); reject(); }
  });
}
```

### 3.2 平台适配

| 平台 | baseURL |
|---|---|
| H5 (开发) | `http://localhost:5000` (在 main.js#H5 覆写) |
| APP-PLUS (生产) | `''` (相对路径) |
| MP (小程序) | `http://localhost:5000` (需配置域名白名单) |

### 3.3 风险

- 无请求取消 — 所有请求执行到完成
- 600/601/602 硬编码 1.5s 延迟后跳转（不优雅）
- 非标准 200 的 HTTP 状态码被静默吞没

---

## 4. API 文件组织对比

### web-vue3 — 领域模块化 (68 文件)

```
src/api/
├── basic/      (common.ts, user.ts)
├── system/     (19 文件: 区域/缓存/日志/菜单/系统...)
├── permission/ (12 文件: 组织/角色/用户/岗位...)
├── onlineDev/  (6 文件: 在线开发/门户/大屏)
├── workFlow/   (7 文件: 流程引擎/表单设计...)
├── extend/     (10 文件: 表格/文档/邮件...)
├── msgCenter/  (4 文件: 消息模板/监控...)
└── systemData/ (7 文件: 数据模型/字典/接口...)
```

### datascreen — 扁平式 (9 文件)

```
src/api/
├── glob.js, visual.js, category.js, components.js
├── db.js, file.js, map.js, record.js, task.js
```

### app-vue3 — 业务分组 (~20 文件)

```
api/
├── common.js (大型文件 ~700 行, 65+ 端点)
├── home.js, message.js, commonWords.js, signature.js
├── apply/ (apply.js, order.js, reportLog.js, visualDev.js, webDesign.js)
├── workFlow/ (flowEngine.js, flowBefore.js, flowLaunch.js, document.js, schedule.js, ...)
└── portal/ (portal.js)
```

---

## 关键发现 (API 层)

| # | 发现 | 严重度 | 项目 |
|---|---|---|---|
| 1 | 三项目 API 定义无共享 — 同一后端接口被实现 3 次 | 高 | 全部 |
| 2 | 超时均为 16.7 分钟 — 不适合大部分请求 | 中 | web-vue3, app |
| 3 | 均无 Token 自动刷新 — 过期即登出 | 中 | 全部 |
| 4 | datascreen validateStatus 过宽 (200-500 均接受) | 中 | datascreen |
| 5 | app-vue3 `common.js` 700 行单体文件 | 低 | app-vue3 |
