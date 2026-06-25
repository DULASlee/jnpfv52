# CT Scan 3.2: 工程化成熟度报告

> 扫描日期: 2026-06-08
> 扫描范围: jnpf-web-vue3 / jnpf-web-datascreen / jnpf-app-vue3
> 扫描维度: Lint / Test / CI / Git Hooks / TypeScript / Docs

---

## 一、工程化成熟度总览

| 维度 | jnpf-web-vue3 | jnpf-web-datascreen | jnpf-app-vue3 |
|---|---|---|---|
| **ESLint** | ✅ 配置完整 (19规则关闭) | ❌ 无 | ❌ 无 |
| **Prettier** | ✅ (有废弃配置项) | ❌ 无 | ❌ 无 |
| **Stylelint** | ✅ | ❌ 无 | ❌ 无 |
| **EditorConfig** | ✅ | ❌ 无 | ❌ 无 |
| **TypeScript** | 🟡 strict 但有关键放宽 | ❌ 纯 JS | ❌ 纯 JS (0.17% TS) |
| **测试** | ❌ 0 测试 | ❌ 0 测试 | ❌ 0 测试 |
| **CI/CD** | 🟡 有但 Lint 门禁断裂 | 🟡 有但无 Lint | ❌ 完全无 CI |
| **Git Hooks** | ❌ 无 pre-commit | ❌ 无 | ❌ 无 |
| **依赖更新** | ❌ 无 renovate/dependabot | ❌ 无 | ❌ 无 |
| **锁文件** | ❌ 双锁文件 | ❌ 双锁文件 | ❌ 双锁文件 |
| **文档** | ✅ README 完整 | ✅ README 完整 | 🟡 README 过于简略 |
| **综合评分** | **C+** | **D-** | **F** |

---

## 二、Lint/Format 详细分析

### 2.1 jnpf-web-vue3: 配置存在但有关键问题

**ESLint 19 条规则被关闭 (off):**
```
@typescript-eslint/no-explicit-any: off        ← 允许 any
@typescript-eslint/no-non-null-assertion: off   ← 允许 !
@typescript-eslint/ban-ts-comment: off          ← 允许 @ts-ignore
@typescript-eslint/no-empty-function: off       ← 允许空函数
vue/require-default-prop: off                   ← Vue最佳实践
vue/multi-word-component-names: off             ← Vue最佳实践
vue/attribute-hyphenation: off                  ← Vue最佳实践
vue/require-explicit-emits: off                 ← Vue3最佳实践
... 及更多
```

**Prettier 废弃配置:**
```javascript
jsxBracketSameLine: true  // ← Prettier 2.4 已废弃，应改为 bracketSameLine
```

**CI Lint 门禁断裂:**
```yaml
# ci.yml:102 — 调用了不存在的脚本!
- run: pnpm lint
  continue-on-error: true  # 即使失败也继续
```
`package.json` 中无 `lint` 脚本，只有 `lint:eslint`、`lint:prettier`、`lint:stylelint`。CI 的 lint 步骤永远失败但被 `continue-on-error: true` 掩盖。

### 2.2 jnpf-web-datascreen & jnpf-app-vue3: 零配置

无 ESLint、Prettier、Stylelint、EditorConfig。代码风格完全不可控。不同开发者的代码风格完全取决于个人习惯。

---

## 三、测试覆盖率: **0%**

**三个前端项目，零测试文件。**

| 项目 | .test.* | .spec.* | __tests__/ | 测试配置 | 测试运行器 |
|---|---|---|---|---|---|
| jnpf-web-vue3 | 0 | 0 | 0 | 无 vitest/jest 配置 | 无 |
| jnpf-web-datascreen | 0 | 0 | 0 | 无 | 无 |
| jnpf-app-vue3 | 0 | 0 | 0 | 无 | 无 |

`@vue/test-utils ^2.3.2` 已安装 (web-vue3 devDependencies) 但从未使用。

---

## 四、CI/CD 管线分析

### 4.1 三条 GitHub Actions 流水线

```
ci.yml          → PR / push to main, develop
cd-staging.yml  → push to develop / 手动
cd-production.yml → release / 手动
```

### 4.2 前端 CI Job 详情

| Job | 项目 | Lint | Build | 问题 |
|---|---|---|---|---|
| `frontend-web` | jnpf-web-vue3 | `pnpm lint` (脚本不存在!) | `pnpm build` | Lint 永远跳过 |
| `frontend-datascreen` | jnpf-web-datascreen | **无** | `pnpm build` | 无 Lint 步骤 |
| — | jnpf-app-vue3 | **无 CI Job** | **无 CI Job** | 完全无 CI |

### 4.3 Docker 构建

| 项目 | Node 版本 | Nginx 版本 | 问题 |
|---|---|---|---|
| jnpf-web-vue3 | **node:16.20.2** | nginx:1.25.2-alpine | Node 16 EOL (2023-09) |
| jnpf-web-datascreen | node:20-alpine | nginx:stable-alpine | ✅ 最新 |

---

## 五、Git Hooks: 几乎为零

- **无 `.husky/`** — 无 pre-commit 钩子
- **无 commitlint 配置** — 无 commit-msg 校验
- **lint-staged 已配置但未触发** — 配置存在于 package.json 但无 hook 激活
- **仅有的 hook**: `.githooks/post-commit` — 知识库刷新脚本 (非质量相关)

commitizen/cz-git 已安装但为选择性使用 (`pnpm commit`)，无强制。

---

## 六、TypeScript 严格度

### jnpf-web-vue3: "假严格模式"

```json
{
  "strict": true,                    // ✅ 开启
  "strictFunctionTypes": false,      // ❌ 显式关闭 — 破坏 strict
  "noImplicitAny": false,            // ❌ 显式关闭 — 最大安全缺口
  "skipLibCheck": true,              // 跳过 .d.ts 检查
}
```

`noImplicitAny: false` 意味着未标注类型的参数/返回值隐式为 `any`，大量类型错误被隐藏。

### jnpf-web-datascreen & jnpf-app-vue3: 纯 JavaScript

无 TypeScript 配置。迁移到 TS 的障碍:
- 现有代码量 (datascreen: 206 文件, app: 654 文件)
- 无类型基础设施
- 团队 JS 惯性

---

## 七、依赖管理

### 7.1 三项目全部双锁文件

| 项目 | 锁文件 1 | 锁文件 2 | packageManager 字段 |
|---|---|---|---|
| jnpf-web-vue3 | package-lock.json (908KB) | pnpm-lock.yaml (456KB) | `^pnpm@8.1.0` |
| jnpf-web-datascreen | pnpm-lock.yaml (134KB) | yarn.lock (51KB) | 无 |
| jnpf-app-vue3 | package-lock.json (14KB) | pnpm-lock.yaml (7.5KB) | 无 |

### 7.2 无依赖更新自动化

无 renovate.json、dependabot.yml、pnpm-workspace.yaml。

---

## 八、jnpf-app-vue3 特殊情况

**package.json 严重不完整:**
```json
{
  "name": "jnpf-app-vue3",
  "version": "5.2.0",
  "private": true,
  "dependencies": {
    "crypto-js": "^4.2.0",
    "sass": "^1.77.2"
  }
}
```
- **无 `scripts` 字段** — 完全依赖 HBuilder X IDE 构建
- **仅声明 2 个依赖** — 实际使用 50+
- **无 devDependencies** — vue/vite/pinia/vue-i18n 全未声明
- **无 engines 字段** — Node 版本未约束

---

## 九、工程化改进优先级

### P0 (本周)
1. **修复 CI Lint 门禁**: 将 `pnpm lint` 改为 `pnpm lint:eslint && pnpm lint:prettier && pnpm lint:stylelint` 或添加 `lint` 脚本别名
2. **删除多余锁文件**: 统一使用 pnpm-lock.yaml

### P1 (本月)
3. **为 datascreen + app 添加 ESLint + Prettier**: 最低基线
4. **启用 TypeScript strictFunctionTypes + noImplicitAny**: 渐进式
5. **添加 pre-commit hooks (husky + lint-staged)**: 自动化质量门禁
6. **升级 web-vue3 Dockerfile Node 16 → 20**: EOL 修复
7. **为 app 补全 package.json**: scripts + 所有依赖

### P2 (长期)
8. **建立测试基线**: vitest + 核心工具函数测试先行
9. **添加 renovate/dependabot**: 依赖自动更新
10. **为 app 添加 CI**: 至少 H5 构建验证
11. **开启 ESLint 被关闭的 Vue 最佳实践规则**
