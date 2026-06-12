# JNPF V5.2 前端架构迭代计划

> 签发日期: 2026-06-08
> 版本: v1.0
> 签发人: 首席架构师
> 执行人: 工程师
> 前置: CT 扫描 (10 份报告) + 经络把脉 (3 条业务流) + 真气探索 (3 个核心组件) 全部完成

---

## 零、诊断数据汇总

### 0.1 三层诊断全景

```
CT 扫描 (骨骼)
  ├── 3 项目、3,239 文件、1,268 组件
  ├── 22 项问题 (4 P0 + 10 P1 + 6 P2 + 4 P3)
  └── 技术债务 ~67 人天

经络把脉 (经络 — 数据流)
  ├── Pulse 1: 登录 → 首页 (14 发现: 4 P0 + 7 P1 + 4 P2)
  ├── Pulse 2: CRUD 列表 (16 发现: 2 P0 + 7 P1 + 5 P2)
  └── Pulse 3: 工作流跨平台 (17 发现: 3 P0 + 8 P1 + 6 P2)

真气探索 (脏腑 — 核心组件)
  ├── Qi 1: 动态表单 (17 发现: 2 P0 + 8 P1 + 6 P2)
  ├── Qi 2: 代码生成器 (15 发现: 1 P0 + 7 P1 + 6 P2)
  └── Qi 3: 数据大屏 (19 发现: 5 P0 + 8 P1 + 6 P2)

合计: 120 项发现 (21 P0 + 55 P1 + 37 P2 + 7 P3)
```

### 0.2 安全红线 (全部 P0 — F-0 已修复)

| # | 红线 | 状态 |
|---|---|---|
| S-1 | AES/DES 密钥硬编码 | ✅ F-0.1 — 环境变量化 |
| S-2 | axios 0.19.0 CVE | ✅ F-0.2 — 升级 1.17.0 |
| S-3 | CDN 脚本无 SRI | ✅ F-0.3 — 添加 integrity |
| S-4 | datascreen 零认证 | ✅ F-0.4 — postMessage + sessionStorage + 路由守卫 |

### 0.3 新增 P0 (诊断过程中发现 — 待修复)

| # | 发现 | 来源 | 位置 |
|---|---|---|---|
| P0-1 | web-vue3 Token 嵌入 DataV URL 参数 | Pulse 1 | routeHelper.ts:173 |
| P0-2 | Token 通过 `${jnpfToken}` 泄露给外链 | Pulse 1 | routeHelper.ts:177 |
| P0-3 | eval() 执行用户脚本 (Parser.vue) | Qi 1 | Parser.vue buildListeners() |
| P0-4 | funEval() = `new Function()` 执行任意代码 | Qi 3 | container.vue setGlobParams() |
| P0-5 | eval() 注册组件 | Qi 3 | container.vue init() |
| P0-6 | datascreen config.style 直接注入 DOM | Qi 3 | mixins/index.js |
| P0-7 | `new Function()` 在 app 动态权限 | CT | utils/jnpf.js |
| P0-8 | transformObjToRoute 无异常处理 | Pulse 1 | routeHelper.ts:99 |
| P0-9 | permissionGuard 吞异常 `catch {}` | Pulse 1 | permissionGuard.ts:38 |
| P0-10 | datascreen `$refs` 链 (`$parent.$parent.$parent`) | Qi 3 | container.vue |

### 0.4 架构师七项决策 (已定)

| 决策 | 结论 |
|---|---|
| D1 | pnpm workspace monorepo — `@jnpf/shared` 共享包 |
| D2 | 三项目保持各自 UI 框架不变 |
| D3 | datascreen 短期 postMessage 共享 Token，长期独立鉴权 |
| D4 | 保留 Monaco + TinyMCE，移除 CodeMirror + Vditor |
| D5 | 标准化 ECharts，移除 Highcharts |
| D6 | TypeScript: datascreen 先行 → app 跟进 → web-vue3 收紧 strict |
| D7 | app 渐进脱离 HBuilder X IDE |

---

## 迭代总览

```
┌────────┬──────────────────┬──────────┬───────────────────────────────────────────────────┐
│  阶段  │       主题       │  周期    │                        目标                        │
├────────┼──────────────────┼──────────┼───────────────────────────────────────────────────┤
│ F-0    │ 安全止血          │ 2-3 天   │ 消除 datascreen 4 个 P0 安全红线                   │
│ F-1    │ 工程化基线统一     │ 2-3 周   │ 三项目统一包管理/Lint/构建/CI                      │
│ F-2    │ 共享基础层        │ 3-4 周   │ @jnpf/shared (HTTP/Token/权限/加密/工具)            │
│ F-3    │ 架构能力升级       │ 4-6 周   │ TS 迁移 + 编辑器/图表统一 + 体积优化 + 测试基线     │
│ F-4    │ 长期演进          │ 持续     │ Monorepo 深化 + API 类型生成 + 组件库提取            │
└────────┴──────────────────┴──────────┴───────────────────────────────────────────────────┘
```

### 验证体系 (L1-L4)

```
L1: 编译验证   → dotnet build / pnpm build (所有被修改项目)
L2: 启动验证   → pnpm run dev → 服务无 crash
L3: 功能验证   → Playwright 浏览器截图 / UniApp H5
L4: 浏览器验证 → pnpm run dev + 手动操作全链路
```

---

## F-0: 安全止血 (已完成)

> **状态: ✅ COMPLETE** — 详见 `F-0 完成报告`

| # | 任务 | 结果 |
|---|---|---|
| F-0.1 | 密钥环境变量化 | ✅ crypto.js → import.meta.env |
| F-0.2 | axios 升级 | ✅ 0.19.0 → 1.17.0 |
| F-0.3 | CDN SRI | ✅ 9 个脚本添加 SHA-384 |
| F-0.4 | 基础认证 | ✅ postMessage + sessionStorage + 路由守卫 |

---

## F-1: 工程化基线统一 (2-3 周)

> 目标: 三项目达到统一工程化基线。消除 "我机器上能跑" 问题。

### F-1.1: 包管理器统一 (0.5 天)

**问题:** datascreen 和 app 存在双锁文件 (pnpm-lock.yaml + yarn.lock/package-lock.json)

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-datascreen/yarn.lock` | 删除 | 统一使用 pnpm |
| `jnpf-app-vue3/package-lock.json` | 删除 | 统一使用 pnpm |
| `jnpf-web-datascreen/package.json` | 修改 | 添加 `"packageManager": "pnpm@8.x"` |
| `jnpf-app-vue3/package.json` | 修改 | 添加 `"packageManager": "pnpm@8.x"` |
| `jnpf-web-datascreen/.npmrc` | 新建 | `engine-strict=true` + `auto-install-peers=true` |
| `jnpf-app-vue3/.npmrc` | 新建 | 同上 |
| `pnpm-workspace.yaml` | 新建 | 根目录 workspace 配置 |

**验证:** 每个项目 `pnpm install` 无警告 + `pnpm build` 通过

### F-1.2: Lint/Format 补齐 (1 天)

**问题:** datascreen 和 app 零 ESLint/Prettier 配置。web-vue3 19 条规则被关闭。

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-datascreen/eslint.config.mjs` | 新建 | 从 web-vue3 拷贝基础配置，保留核心规则 |
| `jnpf-web-datascreen/.prettierrc.js` | 新建 | 统一 Prettier 配置 |
| `jnpf-app-vue3/eslint.config.mjs` | 新建 | UniApp 适配版本 |
| `jnpf-app-vue3/.prettierrc.js` | 新建 | 统一 Prettier 配置 |
| `jnpf-web-vue3/eslint.config.mjs` | 修改 | 逐步开启已关闭的规则: `vue/require-prop-types`、`vue/require-default-prop`、`@typescript-eslint/no-explicit-any` (warn)、`no-console` (warn) |
| `jnpf-web-vue3/package.json` | 修改 | 修复 `pnpm lint` 脚本 (当前指向不存在的 `eslint:lint`) |

**⚠️ 风险:** datascreen 的 `index.html` 通过 CDN 加载 jQuery/ECharts — ESLint 可能报全局变量未定义错误

**验证:** `pnpm lint` 在每个项目中可执行 (允许初始有 warning，禁止 error)

### F-1.3: app package.json 补全 (1 天)

**问题:** app 的 package.json 仅声明 2 个依赖，实际使用 50+ 依赖。pnpm 严格模式不可用。

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-app-vue3/package.json` | 修改 | 补全所有实际使用的依赖 (vue, pinia, vue-i18n, vk-uview-ui, uni-ui 等) |
| `jnpf-app-vue3/package.json` | 修改 | 添加 scripts: `dev:h5`, `build:h5`, `dev:mp-weixin`, `build:mp-weixin` |

**方法:** 遍历 `uni_modules/` 和 `src/` 中所有 import 语句，提取依赖列表。

**验证:** `pnpm install` 无缺失依赖警告 + `pnpm build:h5` 通过

### F-1.4: web-vue3 TypeScript 收紧 (0.5 天)

**问题:** `strictFunctionTypes: false` + `noImplicitAny: false` — 虚假的类型安全。

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-vue3/tsconfig.json` | 修改 | `strictFunctionTypes: true`, `noImplicitAny: true` |

**⚠️ 风险:** 开启后可能有大量编译错误。处理策略:
1. 先开启 → `vue-tsc --noEmit` → 列出错误数量
2. 如果 ≤30 个错误 → 逐文件修复
3. 如果 >30 个错误 → 降级为 `noImplicitAny: true` + `strictFunctionTypes: false`，剩余在 F-3 中修复

**验证:** `vue-tsc --noEmit` 0 errors

### F-1.5: CI 门禁修复 (1 天)

**问题:** 3 个项目 CI 门禁全线断裂。

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-vue3/.gitee/workflows/*.yml` | 修改 | 修复 `pnpm lint` 脚本引用；移除 `continue-on-error: true` |
| `jnpf-web-datascreen/.gitee/workflows/*.yml` | 修改 | 添加 lint + build step |
| `jnpf-app-vue3/.gitee/workflows/ci.yml` | 新建 | 添加 H5 构建验证 |

**验证:** CI 流水线可触发 + lint step 不跳过

### F-1.6: web-vue3 Dockerfile 升级 (0.5 天)

**问题:** Dockerfile 使用 Node 16 (EOL 2023-09)。

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-vue3/Dockerfile` | 修改 | `FROM node:16-alpine` → `FROM node:20-alpine` |

**验证:** Docker build 成功

### F-1 完成标准

```
✅ 三项目 pnpm install 无警告
✅ 三项目 pnpm lint 可执行 (0 errors)
✅ 三项目 pnpm build 通过
✅ web-vue3 vue-tsc --noEmit 0 errors
✅ app package.json 声明完整
✅ CI lint gate 不为 continue-on-error
✅ Docker build 使用 Node 20
```

---

## F-2: 共享基础层 (3-4 周)

> 目标: 消除三项目重复代码 (~2,000 行)，统一 HTTP/加密/Token/权限。

### F-2.1: pnpm workspace 搭建 (0.5 天)

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `pnpm-workspace.yaml` | 新建 | 根目录 workspace 定义 |
| `packages/shared/package.json` | 新建 | `@jnpf/shared` 入口 |
| `packages/shared/src/index.ts` | 新建 | 统一导出 |
| `packages/shared/tsconfig.json` | 新建 | TypeScript 严格模式 |

```yaml
# pnpm-workspace.yaml
packages:
  - 'packages/*'
  - 'jnpf-web-vue3'
  - 'jnpf-web-datascreen'
  - 'jnpf-app-vue3/*'
```

### F-2.2: HTTP 封装统一 (2 天)

**现状:** 三项目各自实现 HTTP 层:
- web-vue3: VAxios (500 行 TypeScript)
- datascreen: axios 0.19.0 单例 plain JS
- app: uni.request 薄封装

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `packages/shared/src/http/index.ts` | 新建 | 统一 Axios 封装 (基于 VAxios) |
| `packages/shared/src/http/interceptors.ts` | 新建 | Token 注入 / 错误处理 / 重试逻辑 |
| `packages/shared/src/http/types.ts` | 新建 | 请求/响应类型定义 |
| `jnpf-web-datascreen/src/axios.js` | 修改 | 替换为 `import { createHttp } from '@jnpf/shared'` |
| `jnpf-app-vue3/utils/request.js` | 修改 | 替换为 `@jnpf/shared` HTTP (适配 uni.request) |

**web-vue3 迁移:** 渐进式 — 先在 datascreen 验证，web-vue3 在 F-3 中迁移。

**验证:** datascreen `pnpm build` 通过 + 所有 API 调用正常

### F-2.3: Token 管理统一 (1.5 天)

**现状:**
- web-vue3: Pinia + AES-ECB 加密 localStorage + Memory LRU
- datascreen: sessionStorage (F-0.4 修复后)
- app: uni.setStorageSync 明文

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `packages/shared/src/auth/token.ts` | 新建 | 统一 Token 管理 (get/set/clear/isExpired/refresh) |
| `packages/shared/src/auth/storage.ts` | 新建 | 抽象存储层 (localStorage/sessionStorage/uni.storage) |
| `packages/shared/src/auth/postMessage.ts` | 新建 | 跨窗口 Token 传递 (含源验证) |
| `jnpf-web-datascreen/src/utils/auth.js` | 修改 | 迁移到 `@jnpf/shared` |
| `jnpf-app-vue3/utils/auth.js` | 修改 | 迁移到 `@jnpf/shared` |

**验证:** 三项目 Token 获取/存储/清除统一

### F-2.4: 加密工具统一 (1 天)

**现状:** 三项目各实现加密 (MD5 + AES-ECB)，密钥硬编码。

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `packages/shared/src/crypto/index.ts` | 新建 | 统一加密工具 (SHA-256 + AES-CBC + 环境变量密钥) |
| `jnpf-web-vue3/src/utils/cipher.ts` | 修改 | 迁移到 `@jnpf/shared` |
| `jnpf-web-datascreen/src/utils/crypto.js` | 修改 | 迁移到 `@jnpf/shared` |
| `jnpf-app-vue3/utils/define.js` | 修改 | 迁移到 `@jnpf/shared` |

**⚠️ 注意:** 加密算法变更 (MD5→SHA-256, ECB→CBC) 需要**后端配合**。此任务仅统一前端加密工具接口，不改变加密算法。算法升级在 F-3 中与后端协调。

**验证:** 登录流程通过 (密码加密结果与后端兼容)

### F-2.5: 权限检查统一 (1 天)

**现状:** 三项目各自实现 `hasPermission()` / `hasBtnP()` — 逻辑完全相同。

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `packages/shared/src/permission/index.ts` | 新建 | 统一权限检查: `hasPermission`, `hasButton`, `hasColumn`, `hasForm` |
| `packages/shared/src/permission/types.ts` | 新建 | PermissionInfo 类型 |
| `jnpf-web-vue3/src/utils/permission.ts` | 修改 | 迁移到 `@jnpf/shared` |
| `jnpf-web-datascreen/src/utils/permission.ts` | 新建 | 使用 `@jnpf/shared` |
| `jnpf-app-vue3/libs/permission.js` | 修改 | 迁移到 `@jnpf/shared` |

**验证:** 按钮/列/表单权限检查一致

### F-2.6: 安全加固 — web-vue3 Token URL 泄露 (1 天)

**问题 (P0-1, P0-2):** web-vue3 的 `routeHelper.ts` 将 Token 嵌入 DataV 和外链 URL 参数。

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-vue3/src/router/helper/routeHelper.ts` | 修改 | type=6 (DataV) 改为 postMessage 传递 Token；type=7 (外链) 移除 `${jnpfToken}` 占位符 |
| `jnpf-web-vue3/src/views/common/iframe/index.vue` | 修改 | 嵌入 datascreen 时使用 postMessage API |
| `jnpf-web-vue3/src/router/helper/routeHelper.ts` | 修改 | 添加 try-catch 处理 `transformObjToRoute` 异常 |

**修复方案:**
```typescript
// BEFORE (routeHelper.ts:173) — P0-1:
e.path = `${globSetting.dataVUrl}view/${moduleId}?token=${getToken()}`;

// AFTER:
e.path = `${globSetting.dataVUrl}view/${moduleId}`;
// Token via postMessage in iframe/index.vue:
iframe.contentWindow.postMessage({ type: 'JNPF_TOKEN', token: getToken() }, datavUrl);
```

```typescript
// BEFORE (routeHelper.ts:177) — P0-2:
const path = e.urlAddress.replace(/\${jnpfToken}/g, getToken());

// AFTER:
const path = e.urlAddress; // 外链不注入 Token
// 如需认证外链 → 使用独立的 SSO ticket
```

**验证:** DataV 大屏在 web-vue3 iframe 中正常显示 + 外链 URL 不含 Token

### F-2.7: eval/动态代码执行安全治理 (1 天)

**问题:** P0-3 ~ P0-7 涉及 5 处 eval/new Function 动态代码执行。

**变更文件 (优先修复高风险项):**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-vue3/src/components/FormGenerator/src/components/Parser.vue` | 修改 | `buildListeners()` — 将 eval 改为预定义函数映射表 |
| `jnpf-web-datascreen/src/mixins/index.js` | 修改 | `funEval()` — 限制可用函数白名单，禁止访问 window/global |
| `jnpf-web-datascreen/src/page/group/container.vue` | 修改 | `eval(cmp)` — 改为 componentMap 注册表查询 |
| `jnpf-app-vue3/utils/jnpf.js` | 修改 | `getScriptFunc()` — 改为预定义操作码映射 |

**eval 替换策略:**
```typescript
// BEFORE (Parser.vue):
const func = getScriptFunc(str); // eval(string)
func({ data, ...params });

// AFTER (predefined function registry):
const FUNCTIONS = {
  setDisabled: ({ data, field, value }) => { /* ... */ },
  setRequired: ({ data, field, value }) => { /* ... */ },
  setVisibility: ({ data, field, value }) => { /* ... */ },
  // ...
};
const func = FUNCTIONS[operationName];
```

**⚠️ 风险:** 在线设计器的用户自定义脚本功能将被限制。需要在 F-3 中建立沙箱方案。

**验证:** 所有使用动态脚本的表单/大屏正常渲染 + 恶意代码被拒绝

### F-2 完成标准

```
✅ @jnpf/shared 发布可用 (HTTP + Token + Crypto + Permission)
✅ datascreen 使用共享 HTTP 层 (all API calls work)
✅ datascreen 使用共享 Token 管理
✅ web-vue3 DataV 链接不含 Token URL 参数
✅ web-vue3 外链不含 Token 占位符
✅ eval/new Function 调用全部替换或沙箱化
✅ 三项目 pnpm build 通过
```

---

## F-3: 架构能力升级 (4-6 周)

> 目标: 消除核心技术债务 — TypeScript 迁移 + 编辑器/图表统一 + 体积优化 + 测试基线。

### F-3.1: datascreen TypeScript 迁移 (8-10 天)

**范围:** 131 个 .vue 文件 → `<script lang="ts">` + 普适 JS 文件 → .ts

**方法:**
1. `tsconfig.json` 先设 `strict: false` + `noImplicitAny: false` (宽松模式)
2. 逐文件改 `lang="js"` → `lang="ts"` + 补充类型注解
3. 所有文件迁移完成后，收紧 `strict: true`

**变更文件 (按优先级):**

| 批次 | 文件 | 说明 |
|---|---|---|
| 1 | `src/utils/*.js` | 工具函数（无 Vue 依赖） |
| 2 | `src/mixins/index.js` | 核心 mixin → composable |
| 3 | `src/page/group/container.vue` | 核心渲染引擎 |
| 4 | `src/page/build.vue` | 设计器 |
| 5 | `src/echart/` | 图表组件 (32 files) |
| 6 | `src/components/` | 其余组件 |

**⚠️ 风险:** datascreen 使用 Options API + window.$glob 全局状态，TS 迁移前需要先建立基础类型定义。

**验证:** `vue-tsc --noEmit` (渐进式降低错误数 → 0)

### F-3.2: app TypeScript 迁移 (10-12 天)

**范围:** 412 个 .vue 文件 + 所有 .js 文件

**前置条件:** F-1.3 (package.json 补全) 已完成

**方法:**
1. 同 datascreen — 先宽松模式，逐文件迁移
2. 优先迁移 `utils/` 和 `libs/` (无 Vue 依赖)
3. UniApp 条件编译 (`#ifdef`) 保留 JS，不迁移
4. uni_modules 下的第三方包不迁移

**验证:** `pnpm build:h5` 通过

### F-3.3: web-vue3 TypeScript 严格模式收紧 (1 天)

**前置条件:** F-1.4

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-vue3/tsconfig.json` | 修改 | `noImplicitAny: true`, `strictFunctionTypes: true`, `noImplicitReturns: true` |

**策略:**
1. 开启 → `vue-tsc --noEmit 2>&1 \| grep "error TS" \| wc -l`
2. 逐文件修复类型错误
3. 无法立即修复的 → `// @ts-expect-error` + TODO 注释

**验证:** `vue-tsc --noEmit` 0 errors

### F-3.4: 编辑器统一 — 移除 CodeMirror + Vditor (2 天)

**⚠️ 前置确认:** 阅读 CodeMirror 和 Vditor 的所有使用场景，确认 Monaco 可完全替代。

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-vue3/package.json` | 修改 | 移除 `codemirror`, `@codemirror/*`, `vditor` |
| `jnpf-web-vue3/src/components/Editor/` | 修改 | 确保 Monaco 配置覆盖所有场景 (代码 + Markdown) |
| 所有引用 CodeMirror 的组件 | 修改 | 改为 Monaco 组件 |
| 所有引用 Vditor 的组件 | 修改 | 改为 Monaco Markdown 模式 |

**验证:** 代码预览 / Markdown 编辑 / 自定义代码块 全部功能可用

### F-3.5: 图表统一 — 移除 Highcharts (1 天)

**⚠️ 前置确认:** 确认 web-vue3 中 Highcharts 的使用场景 (哪些页面? 什么图表类型?)

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-vue3/package.json` | 修改 | 移除 `highcharts`, `@highcharts/*` |
| 所有引用 Highcharts 的页面/组件 | 修改 | 改为 ECharts |

**验证:** 所有原 Highcharts 图表正常渲染

### F-3.6: datascreen CDN 迁移到 npm (1 天)

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-datascreen/package.json` | 修改 | 添加 `echarts`, `jquery` (如需保留), `html2canvas`, `file-saver`, `xlsx`, `jszip`, `qrious` |
| `jnpf-web-datascreen/index.html` | 修改 | 移除 CDN 脚本标签，改为 Vite 按需加载 |
| `jnpf-web-datascreen/src/echart/index.js` | 修改 | `import.meta.globEager` → `import.meta.glob({ eager: true })` |

**验证:** `pnpm build` 通过 + 大屏正常加载

### F-3.7: 代码重复消除 (1 天)

**问题:** webForm/Form.vue (528 lines) 和 flowForm/Form.vue (490 lines) 约 85% 代码重复。

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `jnpf-web-vue3/src/views/generator/BaseForm.vue` | 新建 | 提取公共逻辑 (2 步和 3 步模式的基类) |
| `jnpf-web-vue3/src/views/generator/webForm/Form.vue` | 修改 | 继承 BaseForm，仅保留差异逻辑 |
| `jnpf-web-vue3/src/views/generator/flowForm/Form.vue` | 修改 | 继承 BaseForm，仅保留差异逻辑 |

**验证:** 代码生成器 webForm 和 flowForm 功能不变

### F-3.8: 测试基线建设 (5 天)

**策略:** 不追求覆盖率数字，先建立"最重要的东西有测试"的安全网。

| 优先级 | 测试目标 | 项目 | 测试方式 |
|---|---|---|---|
| 1 | `@jnpf/shared` 加密/Token/权限工具函数 | shared | Vitest 单元测试 |
| 2 | BasicTable fetch() 参数拼接逻辑 | web-vue3 | Vitest + 模拟 |
| 3 | FormItem 动态规则生成 | web-vue3 | Vitest 单元测试 |
| 4 | datascreen auth 工具 | datascreen | Vitest 单元测试 |
| 5 | 登录流程 E2E | web-vue3 | Playwright |

**变更文件:**

| 文件 | 操作 | 说明 |
|---|---|---|
| `packages/shared/vitest.config.ts` | 新建 | 测试配置 |
| `packages/shared/src/**/*.test.ts` | 新建 | 加密/Token/权限 单元测试 |
| `jnpf-web-vue3/vitest.config.ts` | 新建 | 测试配置 |
| `jnpf-web-vue3/src/components/Table/__tests__/useDataSource.test.ts` | 新建 | BasicTable fetch 逻辑 |
| `jnpf-web-vue3/e2e/login.spec.ts` | 新建 | Playwright 登录 E2E |
| `jnpf-web-datascreen/vitest.config.ts` | 新建 | 测试配置 |

**验证:** `pnpm test` 通过 + 覆盖率报告可生成

### F-3 完成标准

```
✅ datascreen vue-tsc --noEmit 0 errors (或未迁移文件已标注)
✅ web-vue3 vue-tsc --noEmit 0 errors (strict mode)
✅ CodeMirror + Vditor 已移除，Monaco 覆盖全场景
✅ Highcharts 已移除，ECharts 统一图表
✅ datascreen CDN 依赖已迁移至 npm
✅ webForm/flowForm 代码重复率 < 20%
✅ @jnpf/shared 核心函数测试覆盖率 ≥ 80%
✅ 登录 E2E 测试通过
```

---

## F-4: 长期演进 (持续)

> 目标: 追赶现代化前端工程标准。F-4 的任务按需排入正常 Sprint。

### F-4.1: pnpm workspace monorepo 深化

- 引入 Turborepo 加速构建 (影响: 所有项目)
- 配置 `turbo.json` 定义任务依赖拓扑
- 共享 tsconfig / eslint / prettier 配置

### F-4.2: API 类型生成

- 从后端 OpenAPI/Swagger JSON → TypeScript 类型定义
- 消除前端手工维护的 API 类型 (user.ts 的 LoginParams 等)
- 实现: `openapi-typescript` + 自定义 codegen

### F-4.3: 自定义 ESLint 规则

- JNPF 专用规则: `jnpf/no-direct-localstorage` (禁止直接访问 localStorage)
- `jnpf/require-tenant-filter` (前端版: 禁止无租户参数的 API 调用)
- `jnpf/no-eval` (替代现有的 `no-eval`，增加预定义函数检测)

### F-4.4: 设计 Token 统一

- 提取三项目的颜色/间距/圆角/字号为共享 CSS 变量
- `@jnpf/design-tokens` 包
- 三项目各自 UI 框架引用同一套 Token → 视觉统一

### F-4.5: 依赖自动更新

- 添加 renovate.json 或 dependabot.yml
- 自动 PR: 小版本/补丁版本自动合并，大版本人工审核

### F-4.6: P2/P3 技术债清理

| # | 任务 | 人天 |
|---|---|---|
| 1 | app Vue 2 死代码 (`#ifndef VUE3`) 清理 | 0.5 |
| 2 | app 拼写错误 `fliter` → `filter` | 0.1 |
| 3 | Prettier 废弃配置 `jsxBracketSameLine` | 0.1 |
| 4 | web-vue3 Store-Router 耦合解耦 | 2 |
| 5 | datascreen `import.meta.globEager` → `glob({ eager: true })` | 0.5 |
| 6 | web-vue3 19 条 ESLint 规则全量开启 | 1 |
| 7 | CHANGELOG / CONTRIBUTING 文档 | 1 |
| 8 | datascreen 配置拆分 (2400行 config.js) | 3 |

---

## 附录 A: 封存文件清单

以下文件一旦在 F-1/F-2 阶段稳定后，标记为"封存"——需架构师审批才能修改:

| 文件 | 原因 | 封存阶段 |
|---|---|---|
| `packages/shared/src/http/index.ts` | 统一 HTTP 层 | F-2 |
| `packages/shared/src/auth/token.ts` | 统一 Token 管理 | F-2 |
| `packages/shared/src/crypto/index.ts` | 统一加密工具 | F-2 |
| `packages/shared/src/permission/index.ts` | 统一权限检查 | F-2 |
| `jnpf-web-vue3/src/components/Form/src/componentMap.ts` | 组件注册表 | F-3 |
| `jnpf-web-datascreen/src/utils/auth.js` | 大屏认证 | F-2 |

---

## 附录 B: 风险矩阵

| 风险 | 概率 | 影响 | 缓解措施 |
|---|---|---|---|
| TypeScript 迁移破坏运行时 | 中 | 高 | 宽松模式渐进迁移，每批次验证 |
| eval 替换破坏现有表单 | 中 | 高 | 先在测试环境验证，保留回退路径 |
| app 脱离 HBuilder X 失败 | 高 | 中 | 先验证 uni-app-cli 构建，不急于切换 IDE |
| editor/chart 移除后功能缺失 | 低 | 高 | 移除前完整确认使用场景 |
| 加密算法变更导致登录失败 | 低 | 极高 | F-2 仅统一接口，不改变算法 |
| pnpm 严格模式破坏 app 构建 | 高 | 中 | F-1.3 先补全依赖声明 |
| postMessage Token 传递在旧浏览器不兼容 | 低 | 低 | postMessage 支持 IE8+ |

---

## 附录 C: 与后端迭代对照

| 后端阶段 | 前端阶段 | 对应关系 |
|---|---|---|
| 阶段 0 (前置扫描) | CT 扫描 + 经络 + 真气 | 诊断 → 理解 |
| 阶段 1-3 (基础设施) | F-0 (止血) + F-1 (基线) | 安全 + 工程化 |
| 阶段 4-6 (核心功能) | F-2 (共享基础层) | 消除重复 + 统一 |
| 阶段 7 (架构升级) | F-3 (架构能力升级) | 核心技术债务 |
| 阶段 8 (长期演进) | F-4 (长期演进) | 持续改进 |

---

## 附录 D: 关键 ADR 记录

### ADR-FE-001: pnpm workspace monorepo

- **决策:** 采用 pnpm workspace monorepo 策略，创建 `@jnpf/shared` 共享包
- **替代方案:** 独立 npm 包发布
- **理由:** 三项目同仓库、共享代码变更即时可见、无需版本管理开销
- **风险:** pnpm 的 strict mode 可能暴露 app 的隐式依赖

### ADR-FE-002: 保持三项目 UI 框架不变

- **决策:** web-vue3 = Ant Design Vue / datascreen = Element Plus / app = vk-uview-ui
- **理由:** datascreen 是大屏场景（非管理后台），UI 框架统一收益低、成本极高
- **替代方案:** 全项目统一为 Ant Design Vue

### ADR-FE-003: ECharts 为唯图表库

- **决策:** 移除 Highcharts，保留 ECharts
- **理由:** Highcharts 需商业授权且 ECharts 可完全替代

### ADR-FE-004: Monaco + TinyMCE 双编辑器

- **决策:** 保留 Monaco（代码）+ TinyMCE（富文本），移除 CodeMirror + Vditor
- **理由:** Monaco = VS Code 内核，TinyMCE = 最成熟富文本编辑器

---

**计划状态: 正式签发。F-0 已完成，F-1 可立即开始。**
