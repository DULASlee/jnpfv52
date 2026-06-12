# JNPF V5.2 前端架构 CT 扫描 — 总报告

> 扫描日期: 2026-06-08
> 扫描范围: jnpf-web-vue3 (PC管理后台) / jnpf-web-datascreen (数字大屏) / jnpf-app-vue3 (移动端)
> 报告版本: v1.0
> 编制: 工程师 (Claude Code)

---

## 一、扫描概览

### 1.1 执行摘要

在 5 天计划内完成了对 JNPF V5.2 三个前端项目的**全量 CT 扫描**。产出 **9 份诊断报告** + **1 份安全红线记录**，覆盖技术栈、目录结构、依赖分析、核心架构、构建部署、组件架构、工程化成熟度、跨项目分析、安全红线。

### 1.2 关键数字

| 指标 | 数值 |
|---|---|
| 扫描项目数 | 3 |
| 报告产出 | 9 份 + 1 安全红线 |
| 总源代码文件 | 3,239 |
| Vue 组件总数 | 1,268 |
| 发现安全问题 (P0) | 4 |
| 发现高优先级问题 | 10+ |
| 量化技术债务 | ~67 人天 |
| 零测试覆盖率 | 3/3 项目 |

### 1.3 项目规模

| 指标 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| 源文件数 | 1,585 | 474 | 1,180 |
| Vue 组件 | 725 | 131 | 412 |
| TypeScript 覆盖率 | 42.4% (.ts+.tsx) | **0%** | **0.17%** |
| 运行时依赖 | 54 | 22 | **2 (声明)** / 50+ (实际) |
| package.json 完整性 | ✅ | ✅ | ❌ 严重不完整 |

---

## 二、三项目架构对比

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| **UI 框架** | Ant Design Vue 3 | Element Plus 2 | vk-uview-ui + uni-ui |
| **状态管理** | Pinia 9 stores | window globals | Pinia 4 stores |
| **路由** | Vue Router + 8层守卫 | Vue Router + 零守卫 | pages.json 声明式 |
| **HTTP** | VAxios (500行封装) | axios 0.19.0 单例 | uni.request 薄封装 |
| **构建** | Vite + 13插件 | Vite + 5插件 | Vite + uni插件 |
| **CSS** | Less + WindiCSS | SCSS | SCSS |
| **认证** | 完整 (密码/SSO/扫码) | 外部依赖 (URL参数) | 完整 (密码/微信/QQ/SSO) |
| **权限** | 4级 (按钮/列/表单/数据) | 无 | 3级 (按钮/列/表单) |
| **TypeScript** | ✅ (放宽严格模式) | ❌ 纯 JS | ❌ 纯 JS |
| **Lint/Format** | ✅ (有断裂) | ❌ | ❌ |
| **测试** | ❌ 0% | ❌ 0% | ❌ 0% |
| **CI/CD** | 🟡 (Lint门禁断裂) | 🟡 (无Lint) | ❌ |
| **综合评分** | **B-** | **D+** | **D** |

---

## 三、安全红线 (4项 P0)

| # | 红线 | 项目 | 影响 |
|---|---|---|---|
| 1 | AES/DES 密钥硬编码 `'EY8WePvjM5GGwQzn'` | 三项目 | 前端加密形同虚设 |
| 2 | axios 0.19.0 (CVE-2023-45857, CVE-2020-28168) | datascreen | SSRF + ReDoS |
| 3 | CDN 脚本无 SRI integrity | datascreen | 供应链攻击 |
| 4 | 零认证零授权 | datascreen | 未授权访问编辑器/屏幕 |

---

## 四、架构问题分类汇总

### 4.1 严重 (P0) — 必须立即修复

1. **安全红线 ×4** (见上)
2. **CI Lint 门禁断裂**: `pnpm lint` 脚本不存在，被 `continue-on-error: true` 掩盖
3. **jnpf-app-vue3 零 CI**: 无构建验证，完全依赖 HBuilder X IDE

### 4.2 高 (P1) — 本迭代修复

4. **三项目零代码共享**: HTTP/加密/权限/Token 独立实现 4 次
5. **datascreen 全局可变状态**: `window.$glob` + `window.$website` 无封装
6. **datascreen 巨型配置文件**: `public/config.js` ~2400行
7. **web-vue3 TypeScript 假严格**: `strictFunctionTypes: false` + `noImplicitAny: false`
8. **三项目双锁文件**: 包管理器不统一
9. **app package.json 严重不完整**: 仅声明 2/50+ 依赖, 无 scripts
10. **web-vue3 四编辑器并存**: Monaco+CodeMirror+TinyMCE+Vditor (~30MB)
11. **web-vue3 双图表库**: ECharts+Highcharts (~8MB)

### 4.3 中 (P2) — 计划修复

12. **datascreen CDN 依赖**: ECharts/jQuery 通过全局脚本加载, 不可 tree-shake
13. **datascreen 废弃 API**: `import.meta.globEager` (Vite 2.x)
14. **app Vue 2 死代码**: `#ifndef VUE3` 分支保留
15. **app eval() 动态执行**: `getScriptFunc` 使用 new Function()
16. **datascreen 客户端密码校验**: 屏幕密码仅 UI 层拦截
17. **web-vue3 Dockerfile Node 16 EOL**
18. **19 条 ESLint 规则被关闭** (含 Vue 最佳实践)

### 4.4 低 (P3) — 随 Sprint 推进

19. **app 拼写错误**: `fliter` → `filter` (base.js:96)
20. **Prettier 废弃配置**: `jsxBracketSameLine`
21. **无 CHANGELOG / CONTRIBUTING 文档**
22. **web-vue3 Store 耦合 Router**

---

## 五、技术债务量化

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

## 六、建议分阶段重构计划

### Phase 1: 止血 (1 周)

```
目标: 消除 P0 安全红线 + 修复 CI 门禁
├── 移除/外移加密密钥
├── 升级 axios 0.19.0 → 1.x
├── CDN 脚本添加 SRI integrity
├── datascreen 添加基础认证守卫
├── 修复 CI Lint 门禁
├── 删除双锁文件
└── 补全 app package.json
```

### Phase 2: 统一基线 (2-3 周)

```
目标: 三项目达到统一工程化基线
├── datascreen + app: 添加 ESLint + Prettier + EditorConfig
├── web-vue3: 开启 strictFunctionTypes + noImplicitAny
├── 建立 @jnpf/shared 共享包
│   ├── HTTP 封装 (统一 Axios 版本)
│   ├── 加密工具 (密钥从环境变量注入)
│   ├── Token 管理
│   └── 权限检查
├── 三项目添加 pre-commit hooks (husky + lint-staged)
├── 为 app 添加 CI (H5 构建验证)
└── 升级 web-vue3 Dockerfile Node 16 → 20
```

### Phase 3: 架构升级 (3-4 周)

```
目标: 消除核心技术债务
├── datascreen: TypeScript 迁移 + 移除 jQuery
├── datascreen: 拆分 public/config.js
├── datascreen: ECharts CDN → npm (tree-shaking)
├── app: TypeScript 迁移 + 移除 Vue 2 死代码
├── web-vue3: 编辑器统一 (评估保留 1-2 个)
├── web-vue3: 图表统一 (评估保留1个)
└── 建立测试基线 (核心工具函数 + 关键业务流程)
```

### Phase 4: 持续演进 (长期)

```
目标: 追赶现代化前端工程标准
├── 开启 ESLint 被关闭的 Vue 最佳实践规则
├── 添加 renovate/dependabot 依赖自动更新
├── 提升测试覆盖率 (框架 80% / 业务 60%)
├── 建立 pnpm workspace monorepo
├── Roslyn Analyzer 等价物: 自定义 ESLint 规则
└── 前后端 API 类型生成 (OpenAPI → TypeScript)
```

---

## 七、架构师决策要点

以下问题需要架构师决策:

1. **共享包策略**: monorepo (pnpm workspace) vs 独立 npm 包 (@jnpf/shared)?
2. **UI 框架统一**: datascreen 是否迁移到 Ant Design Vue? (统一三项目) vs 保持 Element Plus? (迁移成本高)
3. **datascreen 认证模式**: 嵌入 web-vue3 的 iframe (共享 token) vs 独立登录页?
4. **编辑器保留策略**: Monaco / CodeMirror / TinyMCE / Vditor 保留哪些? 建议保留 Monaco (功能最全) + TinyMCE (富文本场景)
5. **图表库保留策略**: ECharts vs Highcharts? 建议保留 ECharts (与 datascreen 统一)
6. **TypeScript 迁移优先级**: datascreen 先 (206文件, 体量小) vs app 先 (价值高, 用户多)?
7. **app 构建工具链**: 摆脱 HBuilder X IDE 依赖 vs 保持现状? 建议添加 CLI 构建能力

---

## 八、报告清单

| # | 文件 | 内容 |
|---|---|---|
| 1 | `01-technology-stack.md` | 技术栈识别 (10维度全量) |
| 2 | `02-directory-structure.md` | 目录结构与文件分布 |
| 3 | `03-dependency-analysis.md` | 依赖版本/安全/体积/共享 |
| 4 | `04-core-architecture.md` | 路由/状态/API/认证权限 |
| 5 | `05-build-deploy-analysis.md` | 构建工具链/Docker/CI/CD |
| 6 | `06-component-architecture.md` | 组件生态/代码生成/对比矩阵 |
| 7 | `07-engineering-maturity.md` | Lint/Test/CI/GitHooks/TS/Docs |
| 8 | `08-cross-project-analysis.md` | 代码共享/反模式/技术债务量化 |
| 9 | `09-final-summary-report.md` | **本文件 — 总报告** |
| 10 | `security-redlines.md` | 10 项安全红线 (4 P0) |

---

## 九、结语

JNPF V5.2 的三个前端项目呈现**显著的架构成熟度梯度**:
- **jnpf-web-vue3** (B-) 是相对最成熟的项目, 有完整的工程化配置, 但 TypeScript 严格度不足, 测试为零, CI 有断裂点
- **jnpf-web-datascreen** (D+) 存在严重的安全和架构问题: 零认证, 全局可变状态, 过时依赖, 无 TypeScript, 零测试
- **jnpf-app-vue3** (D) 面临多平台复杂度和包管理异常的双重挑战, 几乎完全依赖 IDE 构建

**核心矛盾**: 三项目独立演进, 无代码共享, 技术栈碎片化, 导致同样的功能重复实现 3-4 次, 安全漏洞复制 3 份。

**最大机会**: 建立 `@jnpf/shared` 共享包, 统一 HTTP/加密/权限/Token 管理, 可消除 ~2,000 行重复代码, 同时修复 3 个项目的安全问题。

**扫描承诺兑现**: 未放过任何一个角落, 未预设任何结论 — 让数据说话。

---

**扫描完成。** 等待架构师系统性分析后制定分阶段前端架构迭代计划。
