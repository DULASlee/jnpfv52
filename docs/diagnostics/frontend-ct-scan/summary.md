# JNPF V5.2 前端架构 CT 扫描 — 总报告

> 扫描日期：2026-06-08
> 扫描范围：jnpf-web-vue3 (PC) / jnpf-web-datascreen (大屏) / jnpf-app-vue3 (移动端)
> 报告版本：v2.0 (完整版)
> 编制：工程师 (Claude Code)

---

## 执行摘要

在 5 天计划内完成了对 JNPF V5.2 三个前端项目的**全量 CT 扫描**。产出 **15 份诊断报告** + **1 份安全红线记录** + **1 份总报告**，覆盖技术栈、目录结构、依赖分析、路由架构、状态管理、API 层、认证权限、组件架构、代码生成集成、工程化水平、跨项目共享、大屏专项、移动端专项、反模式清单、构建部署。

### 核心结论

三个项目呈现**显著的架构成熟度梯度**，且**完全独立演进、零代码共享**：

| 项目 | 评级 | 定位 |
|---|---|---|
| jnpf-web-vue3 | **B-** | 相对最成熟，有完整工程化配置，但 TypeScript 严格度不足 |
| jnpf-web-datascreen | **D+** | 严重安全与架构问题，但有核心差异化能力（大屏设计器） |
| jnpf-app-vue3 | **D** | 多平台复杂度 + 包管理异常，完全依赖 IDE 构建 |

**核心矛盾：** 同一平台（JNPF）的三个前端项目使用三种不同 UI 框架、两种 CSS 预处理器、两种状态管理模式，且同一功能（加密/HTTP/权限/Token）被重复实现 3-4 次。

---

## 关键数字

| 指标 | 数值 |
|---|---|
| 扫描项目数 | 3 |
| 报告总数 | 17 份 (15 诊断 + 1 安全红线 + 1 总报告) |
| 总源文件数 | 3,239 |
| Vue 组件总数 | 1,268 |
| 安全红线 (P0) | 4 |
| 高优先级问题 | 15+ |
| 反模式识别 | 33 |
| 量化技术债务 | ~67 人天 |
| 零测试项目 | 3/3 |
| TypeScript 覆盖率 | web-vue3: 42% / datascreen: 0% / app: 0.17% |

---

## 三项目架构对比

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| **UI 框架** | Ant Design Vue 3 | Element Plus 2 | vk-uview-ui + uni-ui |
| **状态管理** | Pinia 9 stores | window.$glob (全局对象) | Pinia 4 stores |
| **路由** | Vue Router + 8 层守卫 | Vue Router + 0 守卫 | pages.json 声明式 |
| **HTTP** | VAxios (500行, 5层管道) | Axios 0.19 (96行) | uni.request (116行) |
| **认证** | 密码/SSO/第三方/扫码 | URL 参数 Token (零认证) | 密码/微信/QQ/SSO/扫码 |
| **权限** | v-auth + hasBtnP/hasColumnP/hasFormP | 无 | hasP/hasFormP/hasBtnP |
| **构建** | Vite 4.3 + 13 插件 | Vite 4.4 + 5 插件 | Vite + uni 插件 |
| **CSS** | Less + WindiCSS | SCSS | uni.scss + SCSS |
| **TypeScript** | ✅ strict (noImplicitAny: false) | ❌ 纯 JS | ❌ 纯 JS |
| **ESLint/Prettier** | ✅ (19 条规则被关闭) | ❌ | ❌ |
| **Git Hooks** | lint-staged + commitlint + cz-git | ❌ | ❌ |
| **测试** | ❌ 0% | ❌ 0% | ❌ 0% |
| **CI/CD** | 🟡 Lint 门禁断裂 | ❌ | ❌ |
| **综合评分** | **B-** | **D+** | **D** |

---

## 安全红线 (4 项 P0)

| # | 红线 | 项目 | 影响 |
|---|---|---|---|
| 1 | **AES 密钥硬编码** `'EY8WePvjM5GGwQzn'` (三项目相同) | 全部 | 前端加密形同虚设 |
| 2 | **axios 0.19.0** (CVE-2023-45857 SSRF, CVE-2020-28168 ReDoS) | datascreen | 已知漏洞，5 年未更新 |
| 3 | **CDN 脚本无 SRI integrity** (10+ 全局脚本) | datascreen | 供应链攻击 |
| 4 | **零认证零授权** (无登录/守卫/权限) | datascreen | 未授权访问编辑器/屏幕 |

---

## 严重架构问题 (P1 — 15 项)

### 跨项目

1. **三项目零代码共享** — 同一功能独立实现 3-4 次 (~2,000 行重复)
2. **技术栈碎片化** — 3 种 UI 框架、2 种 CSS 方案、2 种状态管理
3. **均无 Token 刷新** — 过期直接踢出
4. **三项目零测试** — 0% 覆盖率
5. **双锁文件** — datascreen + app 包管理器冲突

### datascreen 专项

6. **全局可变状态** — `window.$glob` + `window.$website`
7. **巨型配置** — `public/config.js` ~2,400 行
8. **CDN 目录冗余** — Vue 2 生态库 + 多版本并存
9. **jQuery + Vue 3 混用** — 完全冗余
10. **废弃 API** — `import.meta.globEager`

### app-vue3 专项

11. **package.json 仅声明 2 个依赖** — 实际 50+ 未记录
12. **无法 CLI 构建** — 完全依赖 HBuilder X IDE
13. **双 UI 框架冗余** — uView + uni-ui 功能重叠
14. **Vue 2 死代码** — `#ifndef VUE3` 分支保留
15. **硬编码 URL/密钥** — `define.js` 中 localhost:5000 写死

### web-vue3 专项

16. **TypeScript 假严格** — `strictFunctionTypes: false` + `noImplicitAny: false` → 974 处 `any`
17. **四编辑器并存** — Monaco+CodeMirror+TinyMCE+Vditor (~6.7MB)
18. **双图表库** — ECharts+Highcharts (~1.8MB)

---

## 反模式 Top 10

| # | 反模式 | 项目 | 严重度 |
|---|---|---|---|
| 1 | God Config 2,400 行 | datascreen | P1 |
| 2 | 全局可变状态 (window.$glob) | datascreen | P1 |
| 3 | 隐式依赖 (package.json 不完整) | app | P0 |
| 4 | Store 耦合 Router | web-vue3 | P2 |
| 5 | Getter 副作用 (回退 localStorage) | web-vue3 | P3 |
| 6 | 974 处 `any` 类型 | web-vue3 | P1 |
| 7 | Options API 100% (无 Composition API) | datascreen | P2 |
| 8 | eval() / new Function() 动态执行 | app | P2 |
| 9 | watch() 无 onCleanup (237 个调用中仅 2 个) | web-vue3 | P3 |
| 10 | 客户端密码校验 (数据已加载) | datascreen | P1 |

---

## 代码生成器 (codegen) 集成

- Velocity 模板 (`backend/wwwroot/Template/vue3/`) — 7 个核心模板
- 生成输出无显式标记 — 无法区分生成代码与手写代码
- 重新生成会覆盖手动修改 — 无合并/保护机制
- Vue3 和 UniApp 模板独立维护 — 功能不对等

---

## 技术债务量化

| 类别 | 估计人天 | 优先级 |
|---|---|---|
| 安全红线修复 (P0) | 5 | 本周 |
| CI/CD 修复 + 补齐 | 5 | 本月 |
| TypeScript 迁移 (datascreen + app) | 25 | 长期 |
| 测试基线建设 (三项目) | 13 | 本月 |
| 共享包抽取 (@jnpf/shared) | 15 | 本月 |
| Lint/Format 补齐 (datascreen + app) | 3 | 本月 |
| 代码清理 (死代码/废弃API/拼写) | 1 | 随 Sprint |
| **合计** | **67** | |

---

## 分阶段重构计划

### Phase 1: 止血 (1 周) — 消除 P0 安全红线

```
├── 移除/外移硬编码加密密钥
├── 升级 axios 0.19.0 → 1.7.x
├── CDN 脚本添加 SRI integrity
├── datascreen 添加基础认证守卫 + 路由守卫
├── datascreen postMessage 添加 origin 白名单
├── 删除双锁文件 (保留 pnpm-lock.yaml)
└── 补全 app package.json 依赖声明
```

### Phase 2: 统一基线 (2-3 周)

```
├── 建立 pnpm workspace + @jnpf/shared 共享包
│   ├── @jnpf/shared-cipher (加密)
│   ├── @jnpf/shared-permission (权限)
│   ├── @jnpf/shared-token (Token 管理)
│   └── @jnpf/shared-constants (常量)
├── datascreen + app: 添加 ESLint + Prettier
├── 三项目: 添加 husky + lint-staged
├── 三项目: 添加前端 CI (lint + typecheck + build)
├── 修复 web-vue3 CI Lint 门禁
├── web-vue3: 开启 noImplicitAny
├── web-vue3: 升级 Dockerfile Node 16 → 20
└── app: 添加标准 CLI 构建 scripts
```

### Phase 3: 架构升级 (3-4 周)

```
├── datascreen: TypeScript 迁移 (206 文件)
├── datascreen: 拆分 public/config.js
├── datascreen: ECharts CDN → npm (tree-shaking)
├── datascreen: 移除 jQuery + Vue 2 CDN 残留
├── datascreen: import.meta.globEager → import.meta.glob
├── app: TypeScript 迁移
├── app: 删除 #ifndef VUE3 死代码
├── app: 替换 eval() 动态执行为安全方案
├── web-vue3: 编辑器统一 (评估保留 Monaco + TinyMCE)
├── web-vue3: 图表统一 (评估保留 ECharts)
└── 建立测试基线 (核心工具函数 + 关键业务流程)
```

### Phase 4: 持续演进 (长期)

```
├── pnpm workspace monorepo 完整化
├── datascreen Options API → Composition API
├── app UI 库去重 (uView vs uni-ui)
├── 前后端 API 类型生成 (OpenAPI → TypeScript)
├── renovate/dependabot 依赖自动更新
├── 自定义 ESLint 规则 (等价 Roslyn Analyzer)
└── 测试覆盖率目标: 框架 80% / 业务 60%
```

---

## 架构师决策要点

以下问题需要架构师决策：

1. **共享包策略**：monorepo (pnpm workspace) vs 独立 npm 包 (@jnpf/shared)？
2. **UI 框架统一**：datascreen 是否迁移到 Ant Design Vue（统一三项目）vs 保持 Element Plus（迁移成本高）？
3. **datascreen 认证模式**：嵌入 web-vue3 的 iframe（共享 token）vs 独立登录页？
4. **编辑器保留策略**：Monaco / CodeMirror / TinyMCE / Vditor 保留哪些？建议保留 Monaco（代码）+ TinyMCE（富文本）
5. **图表库保留策略**：ECharts vs Highcharts？建议保留 ECharts（与 datascreen 统一）
6. **TypeScript 迁移优先级**：datascreen 先（206 文件，体量小）vs app 先（价值高，用户多）？
7. **app 构建工具链**：摆脱 HBuilder X IDE 依赖 vs 保持现状？

---

## 报告清单

| # | 文件 | 内容 |
|---|---|---|
| 1 | `01-technology-stack.md` | 技术栈识别 (10 维度全量) |
| 2 | `02-directory-structure.md` | 目录结构与文件分布 |
| 3 | `03-dependency-analysis.md` | 依赖版本/安全/体积/共享 |
| 4 | `04-core-architecture.md` | 核心架构 (路由/状态/API/认证) |
| 5 | `04-routing-architecture.md` | 路由架构深度分析 |
| 6 | `05-state-management.md` | 状态管理深度分析 |
| 7 | `05-build-deploy-analysis.md` | 构建部署分析 (另一版本) |
| 8 | `06-api-layer.md` | API 请求层深度分析 |
| 9 | `06-component-architecture.md` | 组件架构 |
| 10 | `07-auth-permission.md` | 认证与权限深度分析 |
| 11 | `07-engineering-maturity.md` | 工程化成熟度 |
| 12 | `08-component-architecture.md` | 组件架构深度分析 |
| 13 | `08-cross-project-analysis.md` | 跨项目分析 + 反模式 (另一版本) |
| 14 | `09-codegen-integration.md` | 代码生成器前端对接 |
| 15 | `09-final-summary-report.md` | 总报告 (v1.0) |
| 16 | `10-engineering.md` | 工程化水平扫描 |
| 17 | `11-cross-project-sharing.md` | 跨项目代码共享分析 |
| 18 | `12-datascreen-special.md` | 大屏项目专项深度分析 |
| 19 | `13-uniapp-special.md` | 移动端项目专项深度分析 |
| 20 | `14-anti-patterns.md` | 反模式与代码坏味清单 (33 项) |
| 21 | `15-build-deploy.md` | 构建与部署分析 |
| 22 | `security-redlines.md` | 安全红线记录 (10 项) |
| 23 | `summary.md` | **本文件 — 总报告 v2.0** |

---

## 结语

JNPF V5.2 的三个前端项目呈现**显著的架构成熟度梯度**：

- **jnpf-web-vue3** (B-) 是相对最成熟的项目，有完整的工程化配置，但 TypeScript 严格度不足（974 处 any），测试为零，CI 有断裂点，4 个编辑器 + 2 个图表库并存
- **jnpf-web-datascreen** (D+) 存在严重的安全和架构问题（零认证、过时依赖、全局状态、巨型配置），但其大屏设计器是核心差异化能力，值得投入重构
- **jnpf-app-vue3** (D) 面临多平台复杂度和包管理异常的双重挑战（package.json 仅声明 2/50+ 依赖），几乎完全依赖 IDE 构建，但多端覆盖（H5/APP/小程序）是业务刚需

**核心矛盾：** 三项目独立演进，无代码共享，技术栈碎片化，导致同样的功能重复实现 3-4 次，安全漏洞复制 3 份。

**最大机会：** 建立 `@jnpf/shared` 共享包，统一 HTTP/加密/权限/Token 管理，可消除 ~2,000 行重复代码，同时修复 3 个项目的安全问题。

**扫描承诺兑现：** 未放过任何一个角落，未预设任何结论 — 让数据说话。

---

**扫描完成。** 等待架构师系统性分析后制定分阶段前端架构迭代计划。
