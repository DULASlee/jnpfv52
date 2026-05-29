# Capability: frontend-align-dist-v1

> **状态**：✅ **已收口**（2026-05-22 · 架构师验收 · LOG-20260522-016）  
> **历史真理对照**：`web/dist_v1.1/`（只读基准，已备份至 `web/dist_v1.1_backup_20260522/`）  
> **当前生产静态资源**：`web/dist/`（自 `jnpf-web-vue3` 新 build，1600 文件）  
> **工程基座**：`jnpf-web-vue3/`（v3.6.0）  
> **推进记录**：[`progress-registry.yaml`](../../../docs/架构迭代/4、项目工作推进日程清单/progress-registry.yaml) LOG-007/015/016  
> **架构内参**：[`docs/architecture/04-application-frontend-deep-dive.md`](../../../docs/architecture/04-application-frontend-deep-dive.md) v1.2 · [`05-frontend-source-merge-completion.md`](../../../docs/architecture/05-frontend-source-merge-completion.md)

## Purpose

将正式前端工程 `jnpf-web-vue3` 的可构建产物与运行时能力与历史生产基准 `web/dist_v1.1` 对齐，并完成 F4 部署切换。**源码为工程基座，dist_v1.1 为对照基准，web/dist 为当前运行产物。**

## Outcome summary（2026-05-22）

| 项 | 结果 |
|----|------|
| 源码引入 | GitHub `jnpf-web-vue3` v3.6.0，与 dist_v1.1 约 85% 匹配 |
| GAP 审计 | 19 项 → 真实缺失 2–3 项；其余假阳性/重命名/移除 |
| GAP-01 water | **菜单禁用**（`BASE_MODULE.F_ENABLED_MARK=0`），不补源码（后端无 `ZX_Water` Service） |
| GAP-03 | `CustomBatchForm.vue` / `ExtendForm.vue` 已补 |
| 构建 | `VITE_CDN=false`（消除 bootcdn 404）；`npm run build` 零 ERROR |
| 联调 | CORS 白名单含 `localhost:4173`；登录 `jnpf_ticket`/UA 空值已修复 |
| 部署 | `web/dist/` 已覆盖；`:4173` preview + `:5000` API 验证通过 |
| Backlog | UI-01 演示平台切换 · GAP-02 printDevH5 |

## Constraints（仍有效）

- `views/extend/` dist **80** 页 — 禁止物理删除
- `package.json` 中 echarts/xlsx/tinymce/logicflow/monaco 等 dist 已用依赖 — 禁止移除
- `web/dist_v1.1_backup_20260522/` 保留至下一 major 前端升级前

## Requirements

### Requirement: Dist baseline preserved and superseded

The project SHALL retain `web/dist_v1.1_backup_20260522/` as rollback reference and SHALL deploy new builds to `web/dist/` after acceptance.

#### Scenario: F4 production switch completed

- **WHEN** architecture sign-off occurs (LOG-20260522-016)
- **THEN** `web/dist/` contains the new build from `jnpf-web-vue3`
- **AND** `web/dist_v1.1_backup_20260522/` remains unchanged

### Requirement: GAP closure recorded

Each dist-only GAP item SHALL be closed as **implement**, **disable menu**, **rename equivalent**, or **backlog** with evidence in construction matrix.

#### Scenario: Water module disabled

- **WHEN** GAP-01 is evaluated and modularity has no `ZX_Water` Service
- **THEN** `scripts/sql/disable-water-menus.sql` sets **BASE_MODULE** water entries `F_ENABLED_MARK=0`
- **AND** water menu is absent in browser sidebar after login

#### Scenario: GAP-03 implemented

- **WHEN** `CustomBatchForm.vue` / `ExtendForm.vue` missing in source
- **THEN** files exist under `jnpf-web-vue3/src/views/common/dynamicModel/list/`
- **AND** `list/index.vue` imports both components

### Requirement: Production build without CDN external deps

Production build SHALL set `VITE_CDN=false` in `jnpf-web-vue3/.env.production` so `index.html` bundles Vue/Router/Pinia locally (no bootcdn.net).

#### Scenario: Build index.html has no bootcdn

- **WHEN** `npm run build` completes
- **THEN** `web/dist/index.html` contains no `bootcdn.net` references
- **AND** main entry is `/static/js/index-*.js`

### Requirement: Login path operational

Account/password login SHALL succeed via `POST /api/oauth/Login` without `Value cannot be null` from empty `jnpf_ticket` or empty User-Agent in login log.

#### Scenario: Empty SSO ticket does not crash login

- **WHEN** frontend submits login without `jnpf_ticket` (normal password login)
- **THEN** `OAuthService.Login()` skips social ticket cache lookup when `input.jnpf_ticket` is empty
- **AND** response `code=200` with token

### Requirement: Phased execution F0 through F4 complete

Frontend alignment phases F0–F4 SHALL be marked done in progress registry.

#### Scenario: All phases closed

- **WHEN** LOG-20260522-016 is recorded
- **THEN** F0 audit, F1 build, F2/F3 GAP+fixes, F4 deploy are all `status: done`

## Key code paths

| Path | Role |
|------|------|
| `jnpf-web-vue3/src/router/guard/permissionGuard.ts` | `createPermissionGuard()` |
| `jnpf-web-vue3/src/router/helper/routeHelper.ts` | `transformObjToRoute()` |
| `jnpf-web-vue3/src/views/basic/login/LoginForm.vue` | `handleLogin()` — conditional `jnpf_ticket` |
| `modularity/oauth/JNPF.OAuth/OAuthService.cs` | `Login()` L966 — ticket guard; `GetCurrentUser()` |
| `modularity/common/JNPF.Common/Net/UserAgent.cs` | `RawValue` — empty UA → `""` |
| `application/JNPF.API.Entry/Configurations/Cors.json` | dev preview origins |
| `scripts/sql/disable-water-menus.sql` | water menu disable |

## Core tables

| Table | Role |
|-------|------|
| **BASE_MODULE** | Menu `F_URL_ADDRESS`；water `F_ENABLED_MARK=0` |
| **BASE_AUTHORIZE** | Button auth aligned with `v-auth` |
| **BASE_USER** | admin 账号；DevTest 密码策略保持内部约定 |

## GAP final status

| GAP | Final status | Notes |
|-----|--------------|-------|
| GAP-01 water | **移除（菜单禁用）** | 后端无 Service |
| GAP-02 printDevH5 | **backlog** | 待确认是否在用 |
| GAP-03 CustomBatchForm/ExtendForm | **已关闭** | 源码已补 |
| GAP-03 ChildrenList | **已关闭** | → `ChildTableColumn.vue` |
| GAP-04 VersionHistory | **已关闭** | → `VersionManage.vue` |
| GAP-05 dataInterface Log | **待 diff** | 按需 |
| GAP-06/07 | **已关闭** | 假阳性 |
| UI-01 演示平台切换 | **backlog** | 低优先级 UI |

Full matrix: [`02-dist源码对照矩阵.md`](../../../docs/架构迭代/3、架构迭代子阶段施工/3、前端项目施工阶段/2、开发计划和施工包/02-dist源码对照矩阵.md)
