# 10 — 工程化水平扫描

> 扫描日期：2026-06-08

---

## 总览对比

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| TypeScript | ✅ strict (部分放松) | ❌ JavaScript only | ❌ JavaScript only |
| ESLint | ✅ vue3-commended + TS | ❌ 无 | ❌ 无 |
| Prettier | ✅ (lint-staged) | ❌ 无 | ❌ 无 |
| Git Hooks | lint-staged + commitlint + cz-git | ❌ 无 | ❌ 无 |
| 构建工具 | Vite 4.3 | Vite 4.4 | Vite (via uni-app) |
| CSS 方案 | Less + WindiCSS | SCSS | uni.scss + SCSS |
| 测试框架 | ❌ 无 | ❌ 无 | ❌ 无 |
| 包管理器 | pnpm | (未锁定) | (未锁定) |
| Node 版本 | (Docker: 16.20.2) | (Docker: 20-alpine) | (N/A) |

---

## jnpf-web-vue3 — 工程化成熟度最高

### TypeScript 配置

- `strict: true` (但 `strictFunctionTypes: false`, `noImplicitAny: false`)
- 路径别名: `/@/*` → `src/*`, `/#/*` → `types/*`
- **974 处 `any` 使用** — TypeScript 的价值被系统性削弱

### ESLint 规则

- `@typescript-eslint/no-unused-vars: error` (允许 `_` 前缀)
- `vue/multi-word-component-names: off`
- `vue/max-attributes-per-line: off`

### Vite 构建优化

| 优化 | 状态 |
|---|---|
| 代码分割 (5 chunks) | ✅ |
| Gzip + Brotli | ✅ |
| Imagemin | ✅ |
| Legacy 浏览器 | 可用(默认关闭) |
| CDN 加载 | 可用(默认关闭) |
| PWA | 可用(默认关闭) |
| Bundle 分析 | ✅ (`stats.json` + `REPORT=true`) |
| Monaco Workers | ✅ pre-optimized |
| 运行时配置 (_app.config.js) | ✅ |

### 质量工具

- commitizen + cz-git (规范提交)
- commitlint (conventional commits)
- lint-staged (pre-commit)
- nprogress (路由过渡)

---

## jnpf-web-datascreen — 工程化薄弱

### 缺失项

| 缺失 | 影响 |
|---|---|
| 无 TypeScript | 无类型安全、IDE 提示弱 |
| 无 ESLint | 代码风格不统一 |
| 无测试 | 回归风险 |
| 无 Git Hooks | 低质量提交可能进入仓库 |
| 双锁文件 (`package-lock.json` + `yarn.lock`) | 包管理器冲突 |

### 风险点

- `package-lock.json` 和 `yarn.lock` 同时存在 → 依赖解析冲突
- CDN 加载 jQuery/ECharts/Vue/VueRouter — 与 npm 版本重复
- 构建配置极简 — 无代码分割、无压缩优化

---

## jnpf-app-vue3 — 工程化最弱

### 致命缺失

| 缺失 | 影响 |
|---|---|
| **package.json 仅声明 2 个依赖** | 实际使用 50+ 依赖未记录 |
| 无 TypeScript | — |
| 无 ESLint | — |
| 无 build scripts | uni-app CLI 隐式处理 |
| 无 `.env` 文件 | 配置硬编码在 `utils/define.js` |
| 双锁文件 | 同 datascreen |

### `package.json` 异常

```json
{ "dependencies": { "crypto-js": "^4.2.0", "sass": "^1.77.2" } }
// 仅声明 2 个依赖！实际依赖由 uni-app 框架隐式管理
```

---

## 关键发现

| # | 发现 | 严重度 | 项目 |
|---|---|---|---|
| 1 | web-vue3 974 处 `any` — TS 被削弱 | 高 | web-vue3 |
| 2 | datascreen 无 lint/测试/Git Hooks | 高 | datascreen |
| 3 | app-vue3 package.json 未声明真实依赖 | 高 | app-vue3 |
| 4 | datascreen 和 app-vue3 双锁文件冲突 | 中 | datascreen, app |
| 5 | 三项目零测试覆盖 | 高 | 全部 |
| 6 | datascreen CDN 和 npm 加载相同库 | 中 | datascreen |
