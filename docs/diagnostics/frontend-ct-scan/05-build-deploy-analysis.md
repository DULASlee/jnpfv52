# CT Scan 5.1: 构建与部署分析报告

> 扫描日期: 2026-06-08
> 扫描范围: jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3

---

## 一、构建工具链对比

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| **构建工具** | Vite 4.3.8 | Vite 4.4.6 | Vite + @dcloudio/vite-plugin-uni |
| **语言** | TypeScript 5.0.4 | JavaScript (vite.config.js) | JavaScript (vite.config.js) |
| **目标** | ES2015 + Chrome 80 | 默认 | UniApp 多平台 |
| **压缩** | Terser + Gzip/Brotli | Gzip (可选) | UniApp 内置 |
| **CSS** | Less (Ant Design 主题) | SCSS (sass 1.37.5) | SCSS (sass 1.77.2) |
| **HMR** | ✅ (port 3100 strict) | ✅ (port 3102 strict) | ✅ (port 3800 strict) |
| **构建产物** | dist/ (~1654 files) | dist/ | 多平台输出 |
| **构建时间** | 未测量 (需 benchmark) | 未测量 | 未测量 |

---

## 二、jnpf-web-vue3 构建深度分析

### 2.1 插件管道 (13个 Vite 插件)

```
createVitePlugins():
  ├── vue()                          — SFC 编译
  ├── vueJsx()                       — JSX/TSX 支持
  ├── windiCSS()                     — WindiCSS 工具类
  ├── mkcert()                       — 本地 HTTPS (dev only)
  ├── configHtmlPlugin()             — EJS 模板变量注入
  ├── configSvgIconsPlugin()        — SVG 雪碧图
  ├── PurgeIcons()                   — 图标 tree-shaking
  ├── configStyleImportPlugin()     — Ant Design 按需加载样式
  ├── configVisualizerPlugin()      — Bundle 分析 (仅 report 模式)
  ├── configThemePlugin()           — 动态主题切换 (CSS 变量)
  ├── configCdnPlugin()             — CDN 外部化 (仅 build)
  ├── configImageminPlugin()        — 图片压缩 (仅 build)
  ├── configCompressPlugin()        — Gzip/Brotli (仅 build)
  └── configLegacyPlugin()          — @vitejs/plugin-legacy (可选)
```

### 2.2 Code Splitting 策略

```typescript
// vite.config.ts — manualChunks
vendor-vue:     [vue, vue-router, pinia, @vue/shared, @vue/runtime-core]
vendor-antd:    [ant-design-vue, @ant-design/icons-vue]
vendor-tinymce: [tinymce]
vendor-monaco:  [monaco-editor]
vendor-codemirror: [codemirror]
```

5个独立 vendor chunk，2000KB chunkSizeWarningLimit。

### 2.3 环境变量矩阵

| 变量 | Development | Production | Test |
|---|---|---|---|
| VITE_PORT | 3100 | — | — |
| VITE_PUBLIC_PATH | / | / | / |
| VITE_DROP_CONSOLE | false | true | true |
| VITE_BUILD_COMPRESS | — | gzip | gzip |
| VITE_CDN | — | false | false |
| VITE_USE_IMAGEMIN | — | true | true |
| VITE_USE_PWA | — | false | false |
| VITE_LEGACY | — | false | false |
| VITE_PROXY | [["/dev","http://localhost:5000"]] | — | — |

### 2.4 构建后处理 (postBuild.ts)

构建完成后生成 `_app.config.js` 运行时配置文件，包含:
- 应用标题/短名称
- API URL
- 版本号 + 构建时间戳
- 通过 `<script>` 在 index.html 中加载

---

## 三、jnpf-web-datascreen 构建深度分析

### 3.1 双模式构建

| 模式 | 命令 | 入口 | 输出 | 用途 |
|---|---|---|---|---|
| **SPA** | `vite build` | index.html → src/main.js | dist/ | 独立部署 |
| **UMD库** | `vite build --mode lib` | src/page/index.js | public/lib/index.umd.js | 嵌入第三方 |

UMD 库构建 (lib.config.js):
- 外部化: vue → `Vue`, axios → `axios`, AVUE → `AVUE`
- 输出格式: UMD only
- 库名: `AvueData`
- 注册全局组件: `<avue-data>`

### 3.2 插件 (5个)

```
createVitePlugins():
  ├── @vitejs/plugin-vue
  ├── unplugin-auto-import (Vue/VueRouter API 自动导入)
  ├── vite-plugin-svg-icons (src/icons/svg)
  ├── vite-plugin-vue-setup-extend (组件名扩展)
  └── vite-plugin-compression (Gzip, build only)
```

### 3.3 CDN 依赖 (手动管理)

所有外部依赖存放在 `public/cdn/`:
```
public/cdn/
├── animate/3.5.1/
├── avue/3.2.16/
├── axios/1.0.0/, 1.3.6/     ← 两个版本!
├── echarts/5.4.0/
├── element-plus/2.3.3/
├── element-ui/2.15.0/       ← Element UI (Vue2!)
├── html2canvas/
├── iconfont/
├── staticfile/              ← FileSaver/XLSX/JSZip
├── vue/3.2.47/
├── vue-router/3.0.1/        ← Vue Router 3!
├── vuex/2.4.1/, 3.1.1/     ← Vuex 双子版本!
```

**严重问题:** CDN 目录包含 Vue2 生态库 (element-ui, vue-router 3.x, vuex 2.x/3.x), 当前项目是 Vue 3 — 这些库完全多余。

---

## 四、jnpf-app-vue3 构建深度分析

### 4.1 UniApp 多平台构建

构建由 `@dcloudio/vite-plugin-uni` 处理, 输出到:
- **H5**: `dist/build/h5/`
- **微信小程序**: `dist/build/mp-weixin/`
- **APP**: `dist/build/app/`
- 其他平台: 支付宝/百度/抖音/鸿蒙

### 4.2 条件编译

UniApp 的 `#ifdef` / `#ifndef` 预编译指令在构建时移除不需要的代码:
- `#ifdef VUE3` / `#ifndef VUE3` — Vue 版本分支
- `#ifdef H5` / `#ifdef APP-PLUS` / `#ifdef MP-WEIXIN` — 平台分支
- `#ifdef APP-HARMONY` — 鸿蒙分支

这创建了隐式的多代码路径，难以静态分析。

### 4.3 开发辅助脚本

```
scripts/
├── proxy_server.py       — H5 开发代理 (绕过 CORS)
├── start-h5-demo.ps1     — PowerShell 启动脚本
└── verify-login-api.mjs  — 登录 API 验证
```

---

## 五、Docker 部署对比

### jnpf-web-vue3
```dockerfile
# 构建阶段: node:16.20.2 + pnpm (npmmirror)
# 运行阶段: nginx:1.25.2-alpine
# 端口: 80
# SPA 模式: try_files $uri $uri/ /index.html
# Gzip: gzip_static on
# 反向代理: /api/ → jnpf-gateway
```

### jnpf-web-datascreen
```dockerfile
# 构建阶段: node:20-alpine + pnpm 9.9.0
# 运行阶段: nginx:stable-alpine
# 端口: 80
# 子路径部署: /DataV/
# 反向代理: /api/ → jnpf-java-boot-external:30000
```

### jnpf-app-vue3
无 Dockerfile — UniApp 构建产物部署到各平台 (H5 部署到 Web 服务器, APP 打包为 APK/IPA, MP 上传到小程序平台)。

---

## 六、CI/CD 覆盖率

| 项目 | CI 文件 | 状态 |
|---|---|---|
| jnpf-web-vue3 | — (后端有 ci.yml/cd-staging.yml/cd-production.yml) | ❌ 无前端 CI |
| jnpf-web-datascreen | — | ❌ 无 |
| jnpf-app-vue3 | — | ❌ 无 |

**所有三个前端项目均无专属 CI/CD 流水线。** 后端的 GitHub Actions 流水线 (`.github/workflows/`) 不包含前端构建/测试步骤。

---

## 七、部署前检查清单完成率

基于 `docs/deployment/guide.md` 的 10 项清单:

| 检查项 | web-vue3 | datascreen | app-vue3 |
|---|---|---|---|
| appsettings.json 配置 | ✅ | ✅ | N/A |
| ConnectionStrings.json | ✅ (gitignored) | ✅ | N/A |
| Redis 可连接 | ✅ | ✅ | N/A |
| RabbitMQ (如启用) | ✅ | — | — |
| Jaeger (如启用可观测) | ✅ | — | — |
| 数据库迁移 | ✅ (DbUp) | — | — |
| SSL 证书 | ⚠️ 未记录 | ❌ | ❌ |
| /health 返回 200 | ✅ | ❌ 无端点 | ❌ 无端点 |
| /health/live | ✅ | ❌ | ❌ |
| /health/ready | ✅ | ❌ | ❌ |

---

## 八、构建优化建议

1. **统一 Vite 版本**: web-vue3 (4.3.8) → 4.4.x 或更高
2. **datascreen CDN 清理**: 删除 Vue2 生态库, 删除重复 axios 版本
3. **datascreen ECharts npm 化**: 从 CDN 迁移到 npm, 实现 tree-shaking (~节省 60% 体积)
4. **web-vue3 编辑器评估**: Monaco + CodeMirror + TinyMCE + Vditor → 考虑统一为一个
5. **web-vue3 图表评估**: ECharts + Highcharts → 考虑统一为一个
6. **添加前端 CI**: 至少包含 `vue-tsc --noEmit` + `vite build` + bundle 体积对比
7. **app package.json 补全**: 声明所有依赖, 添加 scripts
