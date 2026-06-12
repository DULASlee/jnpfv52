# 13 — jnpf-app-vue3 专项深度分析

> 扫描日期：2026-06-08
> 扫描范围：jnpf-app-vue3 (UniApp 移动端项目)

---

## 项目定位

基于 UniApp 的多平台移动端应用，一次编写、多端运行：

| 平台 | 构建产物 | 状态 |
|---|---|---|
| H5 | `dist/build/h5/` | 主要开发调试平台 |
| APP (Android/iOS) | `dist/build/app/` | 生产发布 |
| 微信小程序 | `dist/build/mp-weixin/` | 生产发布 |
| 支付宝/百度/抖音/鸿蒙 | 对应 dist 目录 | 配置支持但未验证 |

---

## 一、架构全景

### 1.1 技术栈

| 维度 | 详情 |
|---|---|
| 框架 | Vue 3 + UniApp |
| UI 库 | **双框架**: vk-uview-ui (90+ 组件) + @dcloudio/uni-ui (47 modules) |
| 自定义组件 | JNPF 自研 ~30 个 (通过 easycom 自动注册) |
| 状态管理 | Pinia 4 stores (user/base/chat/test) |
| 路由 | `pages.json` 声明式路由 (604 行)，14 主包 + 6 分包 |
| HTTP | `uni.request` 薄封装 (~116 行) |
| 构建 | Vite + `@dcloudio/vite-plugin-uni` |
| CSS | uni.scss + SCSS |
| 语言 | JavaScript (无 TypeScript，仅 2 个 .ts 文件) |

### 1.2 多平台复杂度

UniApp 的"一次编写多端运行"引入了显著的复杂度：

```
条件编译指令散落:
├── #ifdef VUE3 / #ifndef VUE3     — Vue 版本分支
├── #ifdef H5                       — H5 专用代码
├── #ifdef APP-PLUS                 — APP 专用代码
├── #ifdef MP-WEIXIN                — 微信小程序专用
├── #ifdef MP-ALIPAY                — 支付宝小程序
├── #ifdef MP-BAIDU                 — 百度小程序
├── #ifdef MP-TOUTIAO               — 抖音小程序
└── #ifdef APP-HARMONY              — 鸿蒙
```

**统计：** 1,452 处条件编译出现于 248 个文件中。

---

## 二、包管理异常 — 最严重问题

### 2.1 package.json 仅声明 2 个依赖

```json
{
  "dependencies": {
    "crypto-js": "^4.2.0",
    "sass": "^1.77.2"
  }
}
```

**实际使用的 50+ 依赖全部未声明：**

| 类别 | 未声明依赖 (部分) |
|---|---|
| 核心框架 | vue, pinia, vue-i18n, @dcloudio/uni-app |
| UI 组件 | vk-uview-ui, @dcloudio/uni-ui (47 modules) |
| 工具库 | dayjs, md5, qs, core-js |
| 平台相关 | @dcloudio/uni-mp-weixin, @dcloudio/uni-h5 |
| 构建 | @dcloudio/vite-plugin-uni, vite |

### 2.2 根因分析

UniApp 项目的依赖管理高度依赖 HBuilder X IDE：
- IDE 自动注入 `@dcloudio/uni-app` 等核心依赖
- `node_modules` 由 IDE 预装，不通过 `package.json` 解析
- 标准 `pnpm install` 无法构建此项目

### 2.3 影响

| 影响 | 详情 |
|---|---|
| **CI 不可构建** | `pnpm install && pnpm build` 失败 — 依赖缺失 |
| **可复现性为零** | 不同开发者环境的 `node_modules` 来自 IDE，版本不确定 |
| **安全审计盲区** | `pnpm audit` 只能检查 2 个声明的依赖 |
| **依赖升级困难** | 不知道哪些依赖是项目实际需要的 |

---

## 三、UI 组件三层架构

```
vk-uview-ui (90+ components)     ← 第三方社区库 (uView Vue3 fork)
    +
@dcloudio/uni-ui (47 modules)    ← DCloud 官方组件库
    +
Jnpf custom (~30 components)     ← JNPF 自研 (easycom 自动注册)
    =
超过 167 个 UI 组件共存
```

### 3.1 功能重叠

| 功能 | uView | uni-ui | Jnpf |
|---|---|---|---|
| 按钮 | u-button | — | JnpfButton |
| 输入框 | u-input | uni-easyinput | JnpfInput |
| 选择器 | u-picker | uni-data-picker | JnpfSelect |
| 日期 | u-calendar | uni-datetime-picker | JnpfDatePicker |
| 上传 | u-upload | uni-file-picker | JnpfUpload |
| 弹窗 | u-popup | uni-popup | — |
| 表单 | u-form | uni-forms | — |
| 列表 | u-list | uni-list | — |

uView 和 uni-ui 在至少 8 类组件上功能重叠。包体积难以控制。

### 3.2 easycom 自动注册

```json
// pages.json
{
  "easycom": {
    "autoscan": true,
    "custom": {
      "^Jnpf(.*)": "@/components/Jnpf/$1/index.vue"
    }
  }
}
```

`Jnpf*` 前缀组件自动解析到 `@/components/Jnpf/` 目录，无需手动 import。

---

## 四、路由架构

### 4.1 pages.json 声明式路由

```json
{
  "pages": [ /* 14 个主包页面 */ ],
  "subPackages": [
    { "root": "subPages/app", "pages": [/* 6 */] },
    { "root": "subPages/apply", "pages": [/* 10 */] },
    { "root": "subPages/workFlow", "pages": [/* 12 */] },
    { "root": "subPages/portal", "pages": [/* 6 */] },
    { "root": "subPages/msgCenter", "pages": [/* 5 */] },
    { "root": "subPages/onlineDev", "pages": [/* 6 */] }
  ]
}
```

- **主包 14 页**：登录、工作台、消息、我的、门户等
- **6 个分包 45 页**：应用、审批、工作流、门户、消息中心、在线开发

### 4.2 无导航守卫

与 web-vue3 的 8 层守卫链不同，UniApp 没有 `router.beforeEach`。权限检查在每个页面的 `onLoad` 中分散实现。

---

## 五、存储抽象泄露

UniApp 的 `uni.setStorageSync` / `uni.getStorageSync` 在不同平台表现不同：

| 平台 | 底层实现 | 容量 | 持久性 |
|---|---|---|---|
| H5 | `localStorage` | 5-10MB | 永久 |
| APP | SQLite | 无限制 | 永久 |
| 微信小程序 | `wx.setStorageSync` | 10MB | 永久 |
| 其他小程序 | 平台特定 | 各不相同 | 各不相同 |

代码中对存储容量的假设在不同平台上可能不成立。

---

## 六、Vue 2 兼容包袱

`main.js` 中保留了完整的 Vue 2 分支：

```javascript
// #ifndef VUE3
import Vue from 'vue';
import App from './App';
// ... Vue 2 初始化代码 (~50 行)
// #endif

// #ifdef VUE3
import { createApp } from 'vue';
import App from './App';
// ... Vue 3 初始化代码
// #endif
```

由于 `manifest.json` 已声明 `"vueVersion": "3"`，Vue 2 分支是**死代码**，增加认知负担和维护成本。

---

## 七、Native API 使用

5+App (APP-PLUS) 使用了大量 HTML5+ Runtime API：

| API | 用途 |
|---|---|
| `plus.push` | 推送通知 |
| `plus.runtime` | 应用版本/安装/重启 |
| `plus.device` | 设备信息 |
| `plus.storage` | 本地存储 |
| `plus.geolocation` | 定位 |
| `plus.camera` | 拍照 |
| `plus.gallery` | 相册选择 |
| `plus.uploader` | 文件上传 |
| `plus.downloader` | 文件下载 |
| `plus.nativeUI` | 原生 UI 组件 |

这些 API 在 H5 和小程序上不可用，需条件编译处理。

---

## 八、开发辅助脚本

```
scripts/
├── proxy_server.py       — H5 开发代理 (绕过 CORS，将 /api 转发到后端)
├── start-h5-demo.ps1     — PowerShell 启动脚本
└── verify-login-api.mjs  — 登录 API 验证
```

### proxy_server.py

```python
# 简单 HTTP 代理，解决 H5 开发的跨域问题
# 将浏览器请求转发到后端 localhost:5000
class ProxyHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        # 转发到 http://localhost:5000 + self.path
```

这是临时解决方案。生产环境中跨域应由 Nginx 或后端 CORS 配置处理。

---

## 九、关键文件分析

### 9.1 `utils/define.js` — 配置中心 (~100 行)

```javascript
// 硬编码配置
const baseURL = 'http://localhost:5000';  // ⚠️ 硬编码 URL
const cipherKey = 'EY8WePvjM5GGwQzn';     // ⚠️ 硬编码 AES 密钥
const scanCodeUrl = '...';                  // 扫码地址
```

### 9.2 `api/common.js` — 巨型 API 文件 (~700 行)

包含 65+ API 端点定义，是所有业务 API 的单体聚合。

### 9.3 `libs/permission.js` — 权限工具 (147 行)

```javascript
hasP(enCode, menuIds)     // 列权限
hasFormP(enCode, menuIds) // 表单权限
hasBtnP(enCode, menuIds)  // 按钮权限
```

通过 `Vue.prototype.$permission` / `app.config.globalProperties.$permission` 全局暴露。

---

## 十、重构路线图建议

### Phase 1: 止血 (1 周)
- **补全 package.json** — 声明所有实际依赖（最高优先级）
- 添加 `build:h5` / `dev:h5` scripts
- 删除 `#ifndef VUE3` 死代码
- 添加 `.npmrc` 强制包管理器

### Phase 2: 工程化 (2 周)
- 添加 ESLint + Prettier
- 添加 CI：H5 构建验证
- 拆分 `api/common.js` → 领域模块
- 配置管理：`define.js` 硬编码 → `.env` 文件

### Phase 3: 架构升级 (3-4 周)
- TypeScript 迁移
- UI 库统一（评估 uView 与 uni-ui 去重）
- 引入 `@jnpf/shared` 替换内联加密/权限
- 添加单元测试

---

## 关键发现

| # | 发现 | 严重度 |
|---|---|---|
| 1 | package.json 仅声明 2 个依赖，实际 50+ 未记录 | 高 |
| 2 | 无法通过标准 CLI 构建 (`pnpm install` 失败) | 高 |
| 3 | 双 UI 框架冗余 (uView + uni-ui 功能重叠) | 中 |
| 4 | Vue 2 死代码保留在 main.js | 低 |
| 5 | 硬编码 URL 和密钥在 `define.js` | 高 |
| 6 | `common.js` 700 行单体文件 | 低 |
| 7 | 1,452 处条件编译散落 248 文件 | 中 |
| 8 | 存储抽象泄露 (不同平台不同行为) | 中 |
| 9 | eval() 通过 `new Function()` 动态执行权限脚本 | 中 |
| 10 | 开发依赖 Python 代理脚本绕过 CORS | 低 |
