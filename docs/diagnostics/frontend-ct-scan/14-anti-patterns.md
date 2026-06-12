# 14 — 反模式与代码坏味清单

> 扫描日期：2026-06-08
> 扫描范围：jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3

---

## 一、架构反模式

### AP1: God Config — 巨型单体配置

| 位置 | 行数 | 说明 |
|---|---|---|
| datascreen: `public/config.js` | ~2,400 | 所有组件的属性/样式/数据配置集中在一个文件中 |

**问题：** 修改一个图表类型需要在一个 2,400 行的文件中定位，合并冲突频繁，无法按需加载。

**建议：** 拆分为 `config/charts/bar.js`, `config/charts/line.js` 等，由工厂函数聚合。

### AP2: 全局可变状态

| 位置 | 说明 |
|---|---|
| datascreen: `window.$glob` | 全局配置/用户信息 |
| datascreen: `window.$website` | 站点信息/主题 |

**问题：** 任何代码可以任意修改全局状态，无类型约束，无变更追踪，调试困难。

**建议：** 迁移到 Pinia store，利用 DevTools 可追踪、类型安全。

### AP3: 隐式依赖

| 位置 | 说明 |
|---|---|
| app-vue3: `package.json` | 仅声明 2 个依赖，实际 50+ 由 IDE 隐式注入 |

**问题：** 标准 `pnpm install` 无法构建项目，CI 不可行，安全审计盲区。

**建议：** 补全所有实际依赖到 package.json，添加 lockfile。

### AP4: 双锁文件冲突

| 位置 | 冲突 |
|---|---|
| datascreen | `pnpm-lock.yaml` + `yarn.lock` |
| app-vue3 | `pnpm-lock.yaml` + `package-lock.json` |

**问题：** 不同开发者使用不同包管理器导致 `node_modules` 不一致，产生"我机器上能跑"问题。

**建议：** 删除多余锁文件，在 package.json 设置 `"packageManager": "pnpm@8.x"`。

### AP5: Vue 2 死代码

| 位置 | 说明 |
|---|---|
| app-vue3: `main.js` `#ifndef VUE3` 分支 | 完整 Vue 2 初始化代码 (~50 行) |

**问题：** `manifest.json` 已声明 `vueVersion: "3"`，Vue 2 分支永远不会编译。增加认知负担。

**建议：** 直接删除，Git 历史可恢复。

### AP6: Store 耦合 Router

| 位置 | 说明 |
|---|---|
| web-vue3: `userStore` 内部调用 `router.push()` | 状态管理层直接触发路由跳转 |

**问题：** 违反单向数据流。Store 不应知道路由的存在。单元测试 store 时需要 mock router。

**建议：** Store 只返回状态/抛出事件，由组件层（路由守卫）决定是否跳转。

### AP7: Getter 副作用

| 位置 | 说明 |
|---|---|
| web-vue3: `userStore.get token` | getter 中回退读取 localStorage |

**问题：** Pinia getter 应该无副作用。当前实现在 state 为空时静默回退 localStorage，行为不透明。

**建议：** 在 store 初始化时同步 localStorage → state，getter 只返回 state。

### AP8: 重复状态存储

| 位置 | 说明 |
|---|---|
| web-vue3: `backMenuList` 与 `backRouterList` | 存储相同数据两次 |

**问题：** 两个状态数组从同一 API 响应 (`getMenu`) 拆分而来，但内容高度重叠。一者变更另一者可能不同步。

**建议：** 单一数据源 + 派生（computed/getter）。

### AP9: 废弃 API 使用

| 位置 | 说明 |
|---|---|
| datascreen: `import.meta.globEager` | Vite 2.x API，Vite 3+ 废弃 |

**问题：** 当前 Vite 4.4.6 仍兼容，但未来版本可能移除。

**建议：** 迁移到 `import.meta.glob('**/*.vue', { eager: true })`。

### AP10: CDN 与 npm 重复加载

| 位置 | 重复的库 |
|---|---|
| datascreen: index.html + package.json | ECharts, Vue, VueRouter, ElementPlus 同时通过 CDN 和 npm 加载 |

**问题：** 同一库被加载两次（不同版本可能），浪费带宽，可能引起运行时冲突。

**建议：** 统一为 npm 依赖（tree-shakeable），删除 CDN 引用。

---

## 二、代码坏味

### CS1: TypeScript `any` 滥用

| 位置 | 数量 | 说明 |
|---|---|---|
| web-vue3 | **974 处** | `any` 类型使用 |

**问题：** `strict: true` 但 `noImplicitAny: false` 允许隐式 any，TypeScript 的类型安全价值被系统性削弱。

**高频文件：**
- `utils/http/axios/VAxios.ts`: any 用于泛型响应处理
- `components/Form/src/BasicForm.vue`: any 用于动态表单值
- `hooks/web/useForm.ts`: any 用于 Schema 泛型

**建议：** 开启 `noImplicitAny: true`，分批次消除 any。优先修复 utils 和 hooks 层。

### CS2: Options API (Vue 2 风格) 100%

| 位置 | 说明 |
|---|---|
| datascreen: 全部 131 个组件 | `export default { data(), methods: {}, mounted() }` |

**问题：** 无 `<script setup>`，无 Composition API，逻辑复用困难（mixins 而非 composables）。

**建议：** 渐进迁移，新组件优先使用 `<script setup>` + Composition API。

### CS3: 巨型文件

| 项目 | 文件 | 行数 |
|---|---|---|
| datascreen | `public/config.js` | 2,400 |
| datascreen | `views/builder/container.vue` | 1,400 |
| app-vue3 | `api/common.js` | 700 |
| web-vue3 | `views/generator/...` | 多处 500+ |

**问题：** 单文件过长难以理解、测试、审查。合并冲突频繁。

**建议：** 拆分为职责单一的模块。每个文件不超过 300 行。

### CS4: `console.log` 残留

| 位置 | 数量 |
|---|---|
| datascreen: `views/code-tip.vue` | 18 处 |
| app-vue3: 各处 | 待扫描 |

**问题：** 生产构建中可能泄露调试信息。

**建议：** 配置 ESLint `no-console` 规则 (warn)，CI 中检查。

### CS5: `v-if` 与 `v-for` 同时使用

| 位置 | 数量 (app-vue3) |
|---|---|
| app-vue3: 多处 | 14 处 |

**问题：** Vue 3 中 `v-if` 和 `v-for` 在同一元素上时，`v-if` 优先级更高（Vue 2 相反），可能导致 `v-for` 变量未定义错误。

**建议：** 用 `<template>` 包裹 `v-for`，`v-if` 放在子元素上。

### CS6: `v-html` 使用

| 位置 | 数量 (web-vue3) |
|---|---|
| web-vue3: 各处 | 12 处 |

**问题：** `v-html` 直接渲染 HTML 字符串，如内容来自用户输入则有 XSS 风险。

**建议：** 使用 DOMPurify 消毒，或替换为安全的渲染方式。

### CS7: 拼写错误

| 位置 | 错误 | 正确 |
|---|---|---|
| app-vue3: `libs/base.js:96` | `data.dictionaryList.fliter()` | `filter()` |

**问题：** 代码可以运行说明 `fliter` 被意外定义了，或者是 `filter` 的原型污染。无论哪种都是 bug。

### CS8: `watch()` 无清理

| 位置 | 数量 |
|---|---|
| web-vue3 | 237 个 `watch()` 调用，仅 2 个有 `onCleanup` |

**问题：** watch 中创建的定时器/订阅/事件监听在组件卸载时不会自动清理，可能导致内存泄漏。

**建议：** 有副作用的 watch 回调必须使用 `onCleanup` 注册清理函数。

### CS9: `this.$set` 残留 (Vue 2 API)

| 位置 | 数量 |
|---|---|
| app-vue3 | 60+ 处 `this.$set()` |

**问题：** `this.$set` 是 Vue 2 的响应式 API，Vue 3 中不需要。虽然是兼容层，但表明代码未针对 Vue 3 优化。

### CS10: `eval()` / `new Function()` 动态执行

| 位置 | 说明 |
|---|---|
| app-vue3: `utils/jnpf.js` `getScriptFunc()` | `new Function('return ' + str)()` 执行动态脚本 |

**问题：** 如果后端返回的按钮启用/禁用脚本被篡改，可执行任意代码。CSP 也无法防御。

**建议：** 使用预定义函数映射（如 `{ 'isAdmin': () => user.role === 'admin' }`），或沙箱解释器。

---

## 三、性能反模式

### PF1: 全量 CDN 加载

| 位置 | 加载内容 | 估计大小 |
|---|---|---|
| datascreen: `index.html` | ECharts + jQuery + html2canvas + XLSX + FileSaver + JSZip + qrious | ~3MB+ |

**问题：** 即使用户只访问简单页面，也加载全部图表/工具库。

**建议：** ECharts 按需引入 (`import * as echarts from 'echarts/core'`)，其他库动态导入。

### PF2: Monaco Editor 全量

| 位置 | 大小 | 用途 |
|---|---|---|
| datascreen | ~5MB | 仅数据源 SQL 编辑一处使用 |

**建议：** 考虑用 CodeMirror 6 (lighter) 替代，或将 Monaco 改为动态 `import()`。

### PF3: 四编辑器并存

| 位置 | 编辑器 | 估计大小 |
|---|---|---|
| web-vue3 | Monaco | ~5MB |
| web-vue3 | CodeMirror 5 | ~200KB |
| web-vue3 | TinyMCE | ~1MB |
| web-vue3 | Vditor | ~500KB |

**合计 ~6.7MB 编辑器代码。建议评估保留 1-2 个：Monaco (代码) + TinyMCE (富文本)。**

### PF4: 双图表库

| 位置 | 图表库 | 大小 |
|---|---|---|
| web-vue3 | ECharts 5.x | ~1MB |
| web-vue3 | Highcharts | ~800KB |

**合计 ~1.8MB。建议统一为 ECharts（与 datascreen 一致）。**

### PF5: 全局组件注册无 Tree-shaking

| 位置 | 注册量 |
|---|---|
| web-vue3 | 618 个全局组件 |

**问题：** 所有组件在 `app.component()` 中注册，Vite 无法 tree-shake 未使用的组件。

### PF6: 无路由懒加载

| 位置 | 说明 |
|---|---|
| datascreen | 所有路由组件使用同步 `import` |

**建议：** 改为 `() => import('./views/...')` 动态导入。

### PF7: emoji GIF 打包

| 位置 | 内容 |
|---|---|
| app-vue3 | 200 个 GIF 表情内嵌在代码包中 |

**建议：** 远程加载或使用 Unicode emoji / SVG 替代。

---

## 四、安全反模式

### SF1: 客户端加密

| 位置 | 说明 |
|---|---|
| 三项目 | AES-ECB 密钥 `'EY8WePvjM5GGwQzn'` 硬编码在源码中 |

**问题：** 前端加密无法保密。密钥在源码/bundle 中可见，任何可访问前端的人都能解密传输数据。**这不是加密，是混淆。**

**建议：** 密码传输依赖 HTTPS（传输层安全），敏感数据加密由后端处理。前端"加密"是浪费时间。

### SF2: Token 明文存储

| 位置 | 说明 |
|---|---|
| datascreen: `sessionStorage['datascreen_token']` | 无加密 |
| app-vue3: `uni.setStorageSync('token', ...)` | 无加密 |

**建议：** 至少使用 httpOnly cookie（防 XSS），或 Web Crypto API 加密存储。

### SF3: Token 在 URL 中传递

| 位置 | 说明 |
|---|---|
| datascreen: `?token=xxx` | web-vue3 跳转大屏时拼接 URL |
| datascreen: `index.html` URL 参数 | 备用 token 获取方式 |

**问题：** Token 出现在浏览器历史、服务器日志、Referer header 中。

### SF4: postMessage 域验证缺失

| 位置 | 说明 |
|---|---|
| datascreen: `window.addEventListener('message', ...)` | 接受 `'*'` 来源 |

**建议：** 检查 `event.origin` 是否在白名单中。

### SF5: 客户端密码校验

| 位置 | 说明 |
|---|---|
| datascreen: 屏幕密码 | 密码仅在 UI 层校验，API 数据已返回浏览器 |

**建议：** 屏幕密码应在后端校验，未授权请求不返回数据。

---

## 五、反模式严重度汇总

### P0 — 立即修复

| # | 反模式 | 项目 |
|---|---|---|
| SF1 | 客户端加密形同虚设 | 全部 |
| SF2 | Token 明文存储 | datascreen, app |
| SF3 | Token 在 URL 中 | datascreen |
| SF4 | postMessage 无域验证 | datascreen |

### P1 — 本迭代修复

| # | 反模式 | 项目 |
|---|---|---|
| AP3 | 隐式依赖 (package.json) | app |
| AP4 | 双锁文件 | datascreen, app |
| AP5 | Vue 2 死代码 | app |
| AP10 | CDN/npm 重复加载 | datascreen |
| CS1 | 974 处 any | web-vue3 |
| CS10 | eval() 动态执行 | app |
| PF3 | 四编辑器并存 | web-vue3 |
| PF4 | 双图表库 | web-vue3 |

### P2 — 计划修复

| # | 反模式 | 项目 |
|---|---|---|
| AP1 | God Config 2,400 行 | datascreen |
| AP2 | 全局可变状态 | datascreen |
| AP6 | Store 耦合 Router | web-vue3 |
| AP9 | globEager 废弃 API | datascreen |
| CS2 | 100% Options API | datascreen |
| CS3 | 巨型文件 (多处 500+ 行) | 全部 |
| PF1 | 全量 CDN 加载 | datascreen |
| PF6 | 无路由懒加载 | datascreen |

### P3 — 随 Sprint

| # | 反模式 | 项目 |
|---|---|---|
| AP7 | Getter 副作用 | web-vue3 |
| AP8 | 重复状态存储 | web-vue3 |
| CS4 | console.log 残留 | datascreen, app |
| CS5 | v-if + v-for 混用 | app |
| CS7 | 拼写错误 (fliter) | app |
| CS8 | watch 无清理 | web-vue3 |
| CS9 | this.$set 残留 | app |
| PF7 | emoji GIF 内嵌 | app |
