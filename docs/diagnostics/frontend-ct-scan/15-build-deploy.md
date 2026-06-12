# 15 — 构建与部署分析

> 扫描日期：2026-06-08
> 扫描范围：jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3

---

## 一、构建工具链对比

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| 构建工具 | Vite 4.3.8 | Vite 4.4.6 | Vite + @dcloudio/vite-plugin-uni |
| 配置语言 | TypeScript (`vite.config.ts`) | JavaScript (`vite.config.js`) | JavaScript (`vite.config.js`) |
| 构建目标 | ES2015 + Chrome 80 | 默认 | UniApp 多平台 |
| 开发端口 | 3100 | 3102 | 3800 (H5) |
| HMR | ✅ (strictPort) | ✅ (strictPort) | ✅ |
| CSS 方案 | Less + WindiCSS | SCSS (sass 1.37.5) | SCSS (sass 1.77.2) |
| 构建产物 | `dist/` (~1,654 files) | `dist/` | `dist/build/{platform}/` |
| 包管理器 | pnpm | (混乱: npm/yarn/pnpm) | (混乱: npm/pnpm) |

---

## 二、jnpf-web-vue3 构建深度分析

### 2.1 Vite 插件管道 (13 个)

```
createVitePlugins():
  ├── vue()                          — SFC 编译
  ├── vueJsx()                       — JSX/TSX 支持
  ├── windiCSS()                     — 原子化 CSS
  ├── mkcert()                       — 本地 HTTPS (dev only)
  ├── configHtmlPlugin()             — EJS 模板变量注入
  ├── configSvgIconsPlugin()        — SVG 雪碧图
  ├── PurgeIcons()                   — 图标 tree-shaking
  ├── configStyleImportPlugin()     — Ant Design 按需加载样式
  ├── configVisualizerPlugin()      — Bundle 分析 (REPORT=true)
  ├── configThemePlugin()           — 动态主题 (CSS 变量)
  ├── configCdnPlugin()             — CDN 外部化 (VITE_CDN=true)
  ├── configImageminPlugin()        — 图片压缩 (build only)
  ├── configCompressPlugin()        — Gzip + Brotli (build only)
  └── configLegacyPlugin()          — @vitejs/plugin-legacy (VITE_LEGACY)
```

### 2.2 Code Splitting

```typescript
// manualChunks 策略 — 5 个独立 vendor chunk
vendor-vue:       [vue, vue-router, pinia, @vue/shared, @vue/runtime-core]
vendor-antd:      [ant-design-vue, @ant-design/icons-vue]
vendor-tinymce:   [tinymce]
vendor-monaco:    [monaco-editor]
vendor-codemirror:[codemirror]

// chunkSizeWarningLimit: 2000 KB
```

### 2.3 环境变量矩阵

| 变量 | Development | Production | Test |
|---|---|---|---|
| `VITE_PORT` | 3100 | — | — |
| `VITE_PUBLIC_PATH` | / | / | / |
| `VITE_DROP_CONSOLE` | false | true | true |
| `VITE_BUILD_COMPRESS` | — | gzip | gzip |
| `VITE_CDN` | — | false | false |
| `VITE_USE_IMAGEMIN` | — | true | true |
| `VITE_USE_PWA` | — | false | false |
| `VITE_LEGACY` | — | false | false |
| `VITE_PROXY` | `[["/dev","http://localhost:5000"]]` | — | — |

### 2.4 构建后处理

`postBuild.ts` 生成运行时配置文件 `_app.config.js`：

```javascript
// dist/_app.config.js
window.__APP_CONFIG__ = {
  title: 'JNPF',
  shortName: 'JNPF',
  apiUrl: '',
  version: '5.2.0',
  buildTime: '2026-06-08T...'
};
```

通过 `<script src="/_app.config.js">` 在 `index.html` 中加载，支持部署后修改配置无需重新构建。

### 2.5 可选功能 (默认关闭)

| 功能 | 开启方式 | 说明 |
|---|---|---|
| CDN 外部化 | `VITE_CDN=true` | Vue/Antd/ECharts 从 CDN 加载 |
| PWA | `VITE_USE_PWA=true` | Service Worker + 离线缓存 |
| Legacy 浏览器 | `VITE_LEGACY=true` | IE11 兼容 (通过 @vitejs/plugin-legacy) |
| Bundle 分析 | `REPORT=true` | 生成 `stats.json` (rollup-plugin-visualizer) |

---

## 三、jnpf-web-datascreen 构建分析

### 3.1 双模式构建

| 模式 | 命令 | 入口 | 输出 | 用途 |
|---|---|---|---|---|
| **SPA** | `vite build` | `index.html` → `src/main.js` | `dist/` | 独立部署 |
| **UMD** | `vite build --mode lib` | `src/page/index.js` | `dist/lib/index.umd.js` | 嵌入第三方 |

UMD 构建 (`lib.config.js`):
```javascript
build: {
  lib: { entry: 'src/page/index.js', name: 'AvueData' },
  rollupOptions: {
    external: ['vue', 'axios', 'AVUE'],
    output: { globals: { vue: 'Vue', axios: 'axios', AVUE: 'AVUE' } }
  }
}
```

### 3.2 Vite 插件 (5 个)

```
createVitePlugins():
  ├── @vitejs/plugin-vue              — SFC 编译
  ├── unplugin-auto-import            — Vue/VueRouter API 自动导入
  ├── vite-plugin-svg-icons           — SVG 图标 (src/icons/svg)
  ├── vite-plugin-vue-setup-extend    — 组件名扩展
  └── vite-plugin-compression         — Gzip (build only)
```

### 3.3 构建配置极简

```javascript
// vite.config.js — 无代码分割、无环境变量、无构建优化
export default defineConfig({
  plugins: createVitePlugins(),
  resolve: { alias: { '@': resolve(__dirname, 'src') } }
  // 无 build.rollupOptions.manualChunks
  // 无 CSS 预处理配置
  // 无 Terser 配置
})
```

---

## 四、jnpf-app-vue3 构建分析

### 4.1 UniApp CLI 构建

构建完全由 `@dcloudio/vite-plugin-uni` 托管：

| 平台 | 命令 | 输出 |
|---|---|---|
| H5 | `uni build --platform h5` | `dist/build/h5/` |
| 微信小程序 | `uni build --platform mp-weixin` | `dist/build/mp-weixin/` |
| APP | `uni build --platform app` | `dist/build/app/` |

### 4.2 条件编译机制

`#ifdef` / `#ifndef` 预编译指令在构建时移除不需要的代码：

```
源码 ──→ @dcloudio/uni-cli-shared ──→ 平台特定输出
  │                    │
  │  #ifdef H5         │  仅 H5 保留
  │  #ifdef APP-PLUS   │  仅 APP 保留
  │  #ifdef MP-WEIXIN  │  仅小程序保留
```

### 4.3 无构建脚本

```json
// package.json — scripts 字段严重不足
{
  "scripts": {
    // 仅此一项！实际上 dev/build 由 HBuilder X IDE 触发
  }
}
```

**标准 CLI 构建需要补充：**
```json
{
  "scripts": {
    "dev:h5": "uni --platform h5",
    "build:h5": "uni build --platform h5",
    "dev:mp-weixin": "uni --platform mp-weixin",
    "build:mp-weixin": "uni build --platform mp-weixin",
    "build:app": "uni build --platform app"
  }
}
```

### 4.4 开发辅助工具

```
scripts/
├── proxy_server.py       — H5 CORS 代理 (开发期)
├── start-h5-demo.ps1     — PowerShell 启动脚本
└── verify-login-api.mjs  — 登录 API 验证
```

---

## 五、Docker 部署对比

### 5.1 jnpf-web-vue3

```dockerfile
# 构建阶段
FROM node:16.20.2-alpine        # ⚠️ Node 16 EOL (2023-09)
RUN npm install -g pnpm
COPY . /app
RUN pnpm install && pnpm build

# 运行阶段
FROM nginx:1.25.2-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Nginx 配置
# - SPA: try_files $uri $uri/ /index.html
# - Gzip: gzip_static on
# - API 代理: /api/ → jnpf-gateway
```

**问题：** Node 16 已于 2023-09-11 EOL。应升级到 Node 20 LTS。

### 5.2 jnpf-web-datascreen

```dockerfile
# 构建阶段
FROM node:20-alpine
RUN npm install -g pnpm@9.9.0
COPY . /app
RUN pnpm install && pnpm build

# 运行阶段
FROM nginx:stable-alpine
COPY --from=build /app/dist /DataV/    # 子路径部署

# Nginx 配置
# - 子路径: /DataV/
# - API 代理: /api/ → jnpf-java-boot-external:30000
```

### 5.3 jnpf-app-vue3

**无 Dockerfile。** UniApp 构建产物部署方式：
- **H5**：静态文件部署到 Nginx/CDN
- **APP**：通过 HBuilder X 云打包或本地离线打包生成 APK/IPA
- **小程序**：通过 HBuilder X 或 CI 插件上传到各小程序平台

---

## 六、CI/CD 覆盖

### 6.1 当前状态

| 项目 | CI 文件 | 状态 |
|---|---|---|
| web-vue3 | 无前端专属 CI | ❌ |
| datascreen | 无 | ❌ |
| app-vue3 | 无 | ❌ |

后端的 `.github/workflows/ci.yml` 仅编译后端和构建 Docker 镜像，**不包含前端构建/测试步骤**。

### 6.2 后端 CI 定义 (ci.yml)

```yaml
jobs:
  build-backend:    # dotnet build (后端 only)
  build-web-vue3:   # ⚠️ 存在但不完整
  build-datascreen: # ❌ 无
  build-app:        # ❌ 无
```

### 6.3 部署流水线

| 流水线 | 内容 |
|---|---|
| `cd-staging.yml` | Docker Compose 启动 4 个服务 (gateway/web/datascreen/otel) |
| `cd-production.yml` | Quality gate → Docker build → Deploy + health check retry |

---

## 七、构建健康检查

### 7.1 web-vue3

| 检查项 | 状态 | 说明 |
|---|---|---|
| `vue-tsc --noEmit` 通过 | ⚠️ | `strictFunctionTypes: false`, `noImplicitAny: false` |
| `vite build` 成功 | ✅ | 输出 ~1,654 文件 |
| ESLint 通过 | ⚠️ | 19 条规则被关闭 |
| Bundle 分析可用 | ✅ | `REPORT=true` 生成 stats.json |

### 7.2 datascreen

| 检查项 | 状态 | 说明 |
|---|---|---|
| TypeScript 检查 | ❌ | 纯 JavaScript |
| `vite build` 成功 | ✅ | SPA + UMD 双模式 |
| ESLint 通过 | ❌ | 无 ESLint |
| Gzip 压缩 | ✅ | vite-plugin-compression |

### 7.3 app-vue3

| 检查项 | 状态 | 说明 |
|---|---|---|
| TypeScript 检查 | ❌ | 纯 JavaScript |
| CLI 构建 | ❌ | `pnpm install` 失败（依赖未声明） |
| IDE 构建 | ✅ | HBuilder X 可构建 |
| Lint | ❌ | 无 ESLint |

---

## 八、构建优化建议

### 8.1 立即执行 (P1)

1. **app-vue3: 补全 package.json 依赖** — 最高优先级，否则 CI 完全不可行
2. **app-vue3: 添加构建 scripts** — `dev:h5`, `build:h5` 等
3. **web-vue3: 升级 Node 16 → 20** (Dockerfile)
4. **datascreen: 添加 `build.rollupOptions.manualChunks`** 代码分割

### 8.2 短期 (P2)

5. **三个项目: 统一 Vite 版本** (当前 4.3 / 4.4 分裂)
6. **datascreen: 删除 CDN 冗余文件** (Vue 2 生态、重复 axios 版本)
7. **datascreen: ECharts CDN → npm** (tree-shaking, ~60% 体积节省)
8. **web-vue3: 评估编辑器/图表去重**

### 8.3 中期 (P3)

9. **添加前端 CI**：至少 `lint` + `typecheck` + `build` + bundle 体积对比
10. **添加 pre-commit hooks**: husky + lint-staged
11. **统一包管理器**: 全部锁定 pnpm，添加 `.npmrc`
12. **app-vue3: 摆脱 HBuilder X 依赖**，实现纯 CLI 构建

---

## 关键发现

| # | 发现 | 严重度 | 项目 |
|---|---|---|---|
| 1 | app-vue3 无法通过 CLI 构建 (`pnpm install` 失败) | 高 | app |
| 2 | web-vue3 Dockerfile Node 16 EOL | 中 | web-vue3 |
| 3 | 三项目无前端专属 CI/CD | 中 | 全部 |
| 4 | datascreen CDN 目录冗余 ~5MB (Vue2 生态) | 中 | datascreen |
| 5 | app-vue3 构建完全依赖 HBuilder X IDE | 中 | app |
| 6 | web-vue3 构建配置最成熟 (13 插件、5 chunks、压缩/分析) | ✅ | web-vue3 |
| 7 | datascreen 构建配置极简 (无代码分割、无环境变量) | 中 | datascreen |
