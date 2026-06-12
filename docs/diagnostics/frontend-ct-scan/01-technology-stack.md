# CT Scan 1.1: 技术栈识别报告

> 扫描日期: 2026-06-08
> 扫描范围: jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3
> 扫描深度: 10 维度全量分析

---

## 一、三项目技术栈总览

| 维度 | jnpf-web-vue3 (PC管理后台) | jnpf-web-datascreen (数字大屏) | jnpf-app-vue3 (移动端) |
|---|---|---|---|
| **框架** | Vue 3.3.4 | Vue 3.4.27 | UniApp (Vue 3) |
| **语言** | TypeScript 5.0.4 | JavaScript (纯JS) | JavaScript (0.17% TS) |
| **构建工具** | Vite 4.3.8 | Vite 4.4.6 | Vite + @dcloudio/vite-plugin-uni |
| **UI框架** | Ant Design Vue 3.2.20 | Element Plus 2.7.5 + Avue 3.4.8 | vk-uview-ui + uni-ui |
| **状态管理** | Pinia 2.1.3 | store2 (localStorage) | Pinia (4 stores) |
| **路由** | Vue Router 4.2.1 (history) | Vue Router 4.1.5 (history, 动态路由) | UniApp pages.json (hash) |
| **CSS方案** | Less + WindiCSS | SCSS | SCSS (uni.scss) |
| **HTTP客户端** | Axios 1.4.0 (VAxios封装) | Axios 0.19.0 (2019年版本!) | uni.request (自定义封装) |
| **国际化** | vue-i18n 9.2.2 | vue-i18n 9.1.9 | vue-i18n (691 keys × 3 locales) |
| **图表** | ECharts 5.4.2 + Highcharts 11.0.1 | ECharts 5.4.0 (CDN加载) | qiun-data-charts |
| **代码编辑器** | Monaco 0.38.0 + CodeMirror 5.65.12 | Monaco 0.34.1 | — |
| **包管理器** | pnpm 8.1.0 | pnpm + yarn (双锁文件) | pnpm + npm (双锁文件) |
| **测试** | — | — | — |
| **lint** | ESLint + Prettier + Stylelint | — | — |
| **TypeScript** | tsconfig.json (strict) | — | — |

---

## 二、各项目详细技术栈

### 2.1 jnpf-web-vue3 (PC管理后台)

**版本锁定策略:** Vue 3.3.4 / @vue/runtime-core 3.3.4 / @vue/shared 3.3.4 均为精确版本 (无 ^/~)，属于刻意锁定。

**核心依赖 (54个):**
- UI: ant-design-vue ^3.2.20, @ant-design/icons-vue ^6.1.0, @ant-design/colors ^7.0.0
- 状态: pinia ^2.1.3
- 路由: vue-router ^4.2.1
- 工具: @vueuse/core ^10.1.2, lodash-es ^4.17.21, dayjs ^1.11.7
- 图表: echarts ^5.4.2, echarts-stat ^1.2.0, highcharts ^11.0.1
- 流程: @logicflow/core ^1.2.1, @logicflow/extension ^1.2.1
- 日历: @fullcalendar/core ^6.1.8 (+ daygrid, interaction, timegrid, vue3)
- 富文本: tinymce ^5.10.7, vditor ^3.9.1
- 加密: crypto-js ^4.1.1
- WebSocket: reconnecting-websocket ^4.4.0
- 其他: qrcode, jsbarcode, cropperjs, sortablejs, print-js, vue-i18n, xlsx, spark-md5, intro.js

**开发依赖 (66个):**
- 编译: typescript ^5.0.4, vue-tsc ^1.6.5, @vue/compiler-sfc ^3.2.47
- Vite插件 (14个): legacy, vue, vue-jsx, theme, cdn-import, compression, html, imagemin, mkcert, purge-icons, pwa, style-import, svg-icons, windicss
- 代码质量: eslint ^8.37.0 + 4个插件, prettier ^2.8.8, stylelint ^15.4.0 + 5个插件
- Git: @commitlint/cli ^17.6.3, cz-git ^1.6.1, lint-staged 13.2.0

**浏览器兼容:** esbuild target es2015, cssTarget chrome80, @vitejs/plugin-legacy 可用

**缺失项:**
- 无单元测试框架 ( vitest / jest 均未安装)
- 无 E2E 测试框架 (playwright / cypress 均未安装)
- 无 Git hooks 管理器 (husky 未安装, lint-staged 已配置但无触发机制)

---

### 2.2 jnpf-web-datascreen (数字大屏)

**严重问题:** 零 TypeScript、零 lint、零格式化配置。纯 JavaScript 项目。

**核心依赖 (22个):**
- UI: element-plus ^2.7.5, @element-plus/icons-vue ^2.0.9
- 低代码: @smallwei/avue ^3.4.8 (Avue CRUD框架)
- 可视化: @kjgl77/datav-vue3 ^1.5.0 (DataV 大屏组件库)
- ECharts: **不在npm依赖中!** 通过 `/public/cdn/echarts/5.4.0/echarts.min.js` 全局脚本加载
- HTTP: **axios 0.19.0 (2019年版本, 5年未更新!)**
- 加密: crypto-js ^4.1.1
- MQTT: mqtt ^4.3.7
- 拖拽: vuedraggable ^4.1.0
- Mock: mockjs ^1.1.0
- 其他: dayjs ^1.10.6, js-cookie ^3.0.0, nprogress, highlight.js, store2, vue-json-viewer

**开发依赖 (8个，极少):**
- @vitejs/plugin-vue ^4.2.3, @vue/compiler-sfc ^3.0.5, sass ^1.37.5
- unplugin-auto-import ^0.11.2, vite-plugin-compression, vite-plugin-svg-icons, vite-plugin-vue-setup-extend

**CDN全局脚本 (通过 index.html `<script>` 加载):**
- ECharts 5.4.0 (echarts.min.js, echarts-wordcloud.min.js, echarts-gl.min.js)
- jQuery (jquery.min.js) — **jQuery 在现代 Vue3 项目中!**
- html2canvas (html2canvas.min.js)
- FileSaver.min.js, xlsx.full.min.js, jszip.min.js
- qrious.min.js (二维码生成)
- 自定义: components.js, config.js

**缺失项 (严重):**
- 无 TypeScript
- 无 ESLint / Prettier / Stylelint / EditorConfig
- 无 .gitignore
- 无测试框架
- 无 Git hooks
- 无 browserslist
- **硬编码加密密钥** (src/utils/crypto.js: aesKey `"EY8WePvjM5GGwQzn"`, desKey `"jMVCBsFGDQr1USHo"`)

---

### 2.3 jnpf-app-vue3 (移动端)

**严重问题:** package.json 仅声明 2 个依赖，所有核心依赖通过 UniApp 工具链隐式加载或 hoisted。

**package.json 显式依赖 (仅2个!):**
- crypto-js ^4.2.0
- sass ^1.77.2

**实际使用但未在 package.json 声明的关键依赖:**
- Vue 3 (通过 UniApp 框架)
- Pinia (store/)
- vue-i18n (locale/)
- vk-uview-ui (uni_modules/)
- uni-ui 47个模块 (uni_modules/)
- @dcloudio/vite-plugin-uni (vite.config.js)
- mescroll-uni, mp-html, qiun-data-charts (uni_modules/)

**UniApp 特有技术:**
- 条件编译: `#ifdef VUE3` / `#ifdef H5` / `#ifdef MP-WEIXIN` / `#ifdef APP-PLUS`
- easycom 自动组件注册: `^Jnpf(.*)` → `@/components/Jnpf/$1/index.vue`
- pages.json 路由 (替代 Vue Router)
- manifest.json 多平台配置
- uts (TypeScript原生扩展, 5个文件)
- uvue (原生渲染, 4个文件)
- wxs (微信小程序脚本, 3个文件)

**平台支持 (8个):**
H5, Android, iOS, 微信小程序, 支付宝小程序, 百度小程序, 抖音小程序, 鸿蒙OS

**缺失项 (严重):**
- 无 TypeScript 配置 (仅2个.ts文件, 0.17%)
- 无 ESLint / Prettier
- 无测试框架
- 无 Git hooks
- package.json 严重不完整 (缺少90%+的依赖声明)
- 双锁文件 (package-lock.json + pnpm-lock.yaml)

---

## 三、技术栈一致性分析

### 3.1 三项目均使用 (一致)

| 技术 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| Vue 3 | 3.3.4 | 3.4.27 | UniApp Vue 3 |
| Vite | 4.3.8 | 4.4.6 | ✅ |
| crypto-js | 4.1.1 | 4.1.1 | 4.2.0 |

### 3.2 各项目互不一致 (碎片化)

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 | 差异等级 |
|---|---|---|---|---|
| UI框架 | Ant Design Vue | Element Plus | vk-uview-ui | **严重** |
| CSS预处理 | Less + WindiCSS | SCSS | SCSS | **严重** |
| 状态管理 | Pinia | store2 (localStorage) | Pinia | 中等 |
| 路由 | Vue Router history | Vue Router history (动态) | pages.json hash | 中等 |
| HTTP | Axios 1.4 + VAxios | Axios 0.19 | uni.request | **严重** |
| 类型系统 | TypeScript strict | JavaScript | JavaScript | **严重** |
| 图表 | ECharts npm | ECharts CDN | qiun-data-charts | 中等 |
| 代码编辑器 | Monaco+CodeMirror | Monaco | — | 低 |
| 代码质量 | ESLint+Prettier+Stylelint | — | — | **严重** |

### 3.3 跨项目代码共享: 零

三个项目之间没有共享任何代码、组件、工具函数或配置。每个项目独立维护自己的 HTTP 封装、加密工具、权限检查、国际化文件。

---

## 四、风险矩阵

| 风险 | 严重度 | 项目 | 描述 |
|---|---|---|---|
| 纯JS无类型安全 | **高** | datascreen, app | 无 TypeScript，运行时才能发现类型错误 |
| 零代码规范 | **高** | datascreen, app | 无 ESLint/Prettier，代码风格完全不可控 |
| 过时依赖 | **高** | datascreen | axios 0.19.0 (2019, 5年未更新) |
| 硬编码密钥 | **严重** | datascreen | aesKey/desKey 硬编码在源码中 |
| CDN全局脚本 | **高** | datascreen | ECharts/jQuery 等通过 `<script>` 加载，不可 tree-shake |
| 双锁文件 | 中 | datascreen, app | 包管理器不统一，可能导致依赖不一致 |
| 依赖声明缺失 | **高** | app | package.json 仅声明2个依赖，实际使用50+ |
| jQuery + Vue3 混用 | 中 | datascreen | index.html 加载 jQuery，可能与 Vue 响应式冲突 |
| 无测试 | **高** | 三个项目 | 零单元测试、零E2E测试 |
| Vue 版本不一致 | 中 | 跨项目 | 3.3.4 vs 3.4.27, 行为可能有差异 |
| `import.meta.globEager` 已废弃 | 低 | datascreen | Vite 3+ 已废弃此API |

---

## 五、结论

**整体评分:** jnpf-web-vue3 (B+), jnpf-web-datascreen (D), jnpf-app-vue3 (C-)

**最紧急修复:**
1. datascreen: 移除硬编码加密密钥
2. datascreen: 升级 axios 到 1.x
3. app: 补全 package.json 依赖声明
4. datascreen + app: 引入 TypeScript + ESLint + Prettier

**下一步扫描:** Day 2 — 路由系统、状态管理、API层、权限认证深度分析
