# 11 — 跨项目代码共享分析

> 扫描日期：2026-06-08
> 扫描范围：jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3

---

## 核心结论：共享程度为零

三个项目之间**没有任何形式的代码共享机制**：

- ❌ 无 monorepo tooling (pnpm workspace / turborepo / nx / lerna)
- ❌ 无共享 npm 包 (内部 registry 或 workspace package)
- ❌ 无 Git submodule
- ❌ 无 npm workspace
- ❌ 无符号链接共享
- ❌ 无共享 ESLint / Prettier / TypeScript 配置

---

## 重复实现清单

### 加密工具 — 三份独立实现

| 项目 | 文件 | 行数 | 算法 |
|---|---|---|---|
| web-vue3 | `src/utils/cipher.ts` | ~60 | AES-ECB + MD5 |
| datascreen | `src/utils/crypto.js` | ~30 | AES-ECB + DES |
| app-vue3 | `utils/define.js` (内联) | ~20 | AES-ECB + MD5 |

**共享可行性：极高。** 纯算法，无运行时依赖差异。统一为一个 `@jnpf/shared` 包可消除 ~110 行重复。

### HTTP 请求封装 — 三份独立实现

| 项目 | 实现 | 行数 | 特性差异 |
|---|---|---|---|
| web-vue3 | VAxios 类 `utils/http/axios/` | ~500 | 5层管道、CancelToken、重试、Transform |
| datascreen | `utils/request.js` | ~96 | URL变量替换、代理转发 |
| app-vue3 | `libs/request.js` | ~116 | uni.request 封装 |

**共享可行性：中。** 平台 API 不同（Axios vs uni.request），但拦截器链、错误处理、Token 注入逻辑可抽象。

### Token 管理 — 三份独立实现

| 项目 | 实现 | 存储方式 | 加密 |
|---|---|---|---|
| web-vue3 | Pinia + Persistent 双层 | localStorage (AES) | ✅ (硬编码密钥) |
| datascreen | 直接 localStorage.getItem | sessionStorage | ❌ 明文 |
| app-vue3 | uni.getStorageSync | uni.storage | ❌ 明文 |

**共享可行性：高。** Token 获取/设置/清除/过期判断可统一接口。

### 权限检查 — 两份独立实现

| 项目 | 实现 | 粒度 |
|---|---|---|
| web-vue3 | `v-auth` 指令 + `hasBtnP/hasColumnP/hasFormP` | 按钮/列/表单 |
| app-vue3 | `$permission.hasP/hasFormP/hasBtnP` + `libs/permission.js` (147行) | 按钮/列/表单 |
| datascreen | 无 | — |

**共享可行性：极高。** 纯逻辑，输入 `permissionList + modelId + enCode`，输出 boolean。接口完全一致。

### 日期处理 — 两份独立实现，版本不同

| 项目 | 库 | 版本 |
|---|---|---|
| web-vue3 | dayjs | 1.11.7 |
| datascreen | dayjs | 1.10.6 |
| app-vue3 | 手动处理 | — |

---

## 技术栈碎片化

| 维度 | web-vue3 | datascreen | app-vue3 |
|---|---|---|---|
| UI 框架 | Ant Design Vue 3 | Element Plus 2 | vk-uview-ui + uni-ui |
| CSS 预处理 | Less + WindiCSS | SCSS | uni.scss + SCSS |
| 状态管理 | Pinia 9 stores | window globals | Pinia 4 stores |
| 构建 | Vite 4.3 | Vite 4.4 | Vite (uni插件) |
| 包管理器 | pnpm | (混乱: npm+yarn+pnpm) | (混乱: npm+pnpm) |
| Node 版本 | 16.20.2 | 20-alpine | N/A |

同一平台（JNPF）三个前端项目使用三种不同 UI 框架、两种 CSS 预处理器、两种状态管理模式。

---

## 共享可行性矩阵

| 候选模块 | 技术可行性 | 预估工作量 | 价值 | 优先级 |
|---|---|---|---|---|
| 加密工具 (cipher) | **极高** | 1 天 | 高 | P1 |
| 权限检查 (permission) | **极高** | 1 天 | 高 | P1 |
| 常量/枚举 (enums) | **极高** | 0.5 天 | 中 | P2 |
| 日期工具 (date) | **极高** | 0.5 天 | 中 | P2 |
| Token 管理 (token) | 高 (接口抽象) | 2 天 | 高 | P1 |
| HTTP 封装 (http) | 中 (平台API差异) | 3 天 | 高 | P2 |
| 表单校验规则 | 高 | 1 天 | 中 | P2 |
| 通用组件 | 低 (UI框架不同) | 10+ 天 | 中 | P3 |
| 主题/样式 | 低 (预处理不同) | 5+ 天 | 低 | P3 |

---

## 推荐共享架构

```
@jnpf/shared/
├── packages/
│   ├── cipher/          # 加密工具 (纯函数，零依赖)
│   │   ├── src/
│   │   │   ├── aes.ts       # AES-ECB 加密/解密
│   │   │   ├── md5.ts       # MD5 哈希
│   │   │   └── index.ts
│   │   └── package.json
│   ├── permission/      # 权限检查 (纯函数)
│   │   ├── src/
│   │   │   ├── hasPermission.ts
│   │   │   ├── types.ts     # PermissionInfo, PermissionChildItem
│   │   │   └── index.ts
│   │   └── package.json
│   ├── token/           # Token 管理 (抽象存储层)
│   │   ├── src/
│   │   │   ├── token-manager.ts  # 接口: get/set/clear/isExpired
│   │   │   ├── adapters/
│   │   │   │   ├── localStorage.ts
│   │   │   │   ├── uni-storage.ts
│   │   │   │   └── session-storage.ts
│   │   │   └── index.ts
│   │   └── package.json
│   ├── constants/       # 共享常量
│   │   └── src/
│   │       ├── business-codes.ts  # 600/601/602 等
│   │       ├── enums.ts           # 状态枚举
│   │       └── index.ts
│   └── shared/          # 一键安装的聚合包
│       └── package.json  # depends on all above
├── pnpm-workspace.yaml
└── package.json
```

### 使用方式

```typescript
// web-vue3: workspace protocol
import { encryptByAES, decryptByAES } from '@jnpf/shared-cipher';
import { hasBtnP, hasColumnP } from '@jnpf/shared-permission';

// datascreen: workspace protocol (需先迁移到 TypeScript)
import { encryptByAES } from '@jnpf/shared-cipher';

// app-vue3: workspace protocol (需先迁移到 TypeScript)
import { hasBtnP } from '@jnpf/shared-permission';
```

---

## 实施路径

### Phase 1: 建立 monorepo 骨架 (2 天)
1. 在项目根目录创建 `packages/shared/` 目录
2. 配置 `pnpm-workspace.yaml` 包含 `packages/shared/*`
3. 创建 `@jnpf/shared-cipher` 包，从 web-vue3 提取 cipher.ts
4. 在 web-vue3 中验证：替换导入路径，功能不变

### Phase 2: 迁移其余模块 (5 天)
5. 提取 `@jnpf/shared-permission`（从 web-vue3 提取，app-vue3 适配）
6. 提取 `@jnpf/shared-token`
7. 提取 `@jnpf/shared-constants`
8. datascreen 和 app-vue3 逐步替换内联实现

### Phase 3: 统一工程配置 (3 天)
9. 抽取共享 ESLint 配置 (`@jnpf/eslint-config`)
10. 抽取共享 Prettier 配置 (`@jnpf/prettier-config`)
11. 抽取共享 TypeScript 配置 (`@jnpf/tsconfig`)

---

## 关键发现

| # | 发现 | 严重度 |
|---|---|---|
| 1 | 三项目零代码共享 — 同一功能实现 3 次 | 高 |
| 2 | AES 加密在 3 个项目中独立实现且密钥相同 | 高 |
| 3 | 权限检查逻辑在 web-vue3 和 app-vue3 中 100% 重复 | 高 |
| 4 | 三项目使用不同 UI 框架 → 组件无法共享 | 中 |
| 5 | CSS 方案分裂 (Less vs SCSS) → 样式无法共享 | 中 |
| 6 | 无共享工程配置 (ESLint/Prettier/TS) → 代码风格不统一 | 中 |
