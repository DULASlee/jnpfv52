# dist_v1.1 与 jnpf-web-vue3 源码对照矩阵

> **文档版本**：v1.1（架构师审核后修订）  
> **状态**：F0 完成；water=移除；真实待补 2 项  
> **数据来源**：`docs/architecture/dist-src-audit.json` + 2026-05-22 源码复核（含 `.tsx`）  
> **真理之源**：`web/dist_v1.1/`  
> **工程基座**：`jnpf-web-vue3/`（v3.6.0）

---

## 1. 四象限处置规则

| dist | 源码 | 处置 | 本仓库数量 |
|------|------|------|------------|
| ✓ | ✓ | **保留** | 325 页 + 2 tsx |
| ✓ | ✗ | **待补 / 移除 / 关闭** | 见 §2 |
| ✗ | ✓ | **候选隐藏菜单** | **0** |
| ✗ | ✗ | 不相关 | — |

---

## 2. dist 独有路径清单（修正后）

| GAP ID | dist views 路径 | 源码 | 最终状态 | 说明 |
|--------|-----------------|------|----------|------|
| GAP-01 | `water/**`（9 页） | ✗ | **移除** | 后端无 ZX_Water；禁用 BASE_MODULE 菜单 |
| GAP-02 | `system/printDevH5/*` | ✗ | **暂缓** | 待测试环境确认 |
| GAP-03 | `CustomBatchForm.vue` | ✗ | **已补** | `jnpf-web-vue3/src/views/common/dynamicModel/list/` |
| GAP-03 | `ExtendForm.vue` | ✗ | **已补** | 同上 |
| GAP-03 | `ChildrenList.vue` | ✗ | **关闭** | 源码 `ChildTableColumn.vue` 功能等价 |
| GAP-04 | `VersionHistory.vue` | ✗ | **关闭** | 源码 `VersionManage.vue` 功能等价 |
| GAP-05 | `LogDetail.vue` / `log.vue` | ✗ | **待 diff** | 暂不补 |
| GAP-06 | `schemaData.tsx` | ✓ | **关闭** | 假阳性（审计漏扫 tsx） |
| GAP-07 | `error-log/data.tsx` | ✓ | **关闭** | 假阳性 |

**补回目标路径**（仅 2 项已实施）：`jnpf-web-vue3/src/views/common/dynamicModel/list/CustomBatchForm.vue`、`ExtendForm.vue`

---

## 3. 模块级对照（一级目录）

| 模块 | dist 页数 | 源码页数 | 处置 |
|------|-----------|----------|------|
| extend | 80 | 79 | **保留** |
| system | 50 | 48 | **保留** + printDevH5 暂缓 |
| workFlow | 44 | 43 | **保留** + VersionHistory 关闭 |
| basic | 33 | 32 | **保留** |
| permission | 31 | 31 | **保留** |
| systemData | 30 | 28 | **保留** + Log 待 diff |
| common | 22 | 19 | **保留** + GAP-03 已补 |
| msgCenter | 21 | 21 | **保留** |
| onlineDev | 18 | 18 | **保留** |
| generator | 6 | 6 | **保留** |
| **water** | **9** | **0** | **移除（清菜单）** |

---

## 4. dist 依赖与路由标记（静态扫描）

| 类别 | 项 | dist 命中 | 源码 package.json | 处置 |
|------|-----|-----------|-------------------|------|
| 图表 | echarts / highcharts | ✓ | ✓ | **保留** |
| 表格 | xlsx | ✓ | ✓ | **保留** |
| 富文本 | tinymce | ✓ | ✓ | **保留** |
| 流程 | logicflow / monaco / vditor | ✓ | ✓ | **保留** |
| 白名单 | printDevH5 | ✓ | 源码无 | **暂缓** |

---

## 5. dist API 抽样（与 GAP 相关）

| API 前缀 | 说明 | 后端本仓库 |
|----------|------|------------|
| `/api/ZX_Water/*` | 水务 | **不存在** → 菜单移除 |
| `/api/system/printDev` | 打印模板 | 有 PrintDevService |
| `/api/oauth/CurrentUser` | 菜单+权限 | OAuthService.GetCurrentUser |

---

## 6. 运行时配置对照

| 配置项 | dist_v1.1 | 新 build（2026-05-22 已验证） |
|--------|-----------|------------------------------|
| 标题 | 智轩云 | ✅ `dist/_app.config.js` |
| API | `http://localhost:5000` | ✅ 已对齐 |
| WebSocket | `ws://localhost:5000` | ✅ 已对齐 |
| CDN | 本地 chunk（`VITE_CDN=false`） | ✅ 架构师裁定；勿用 bootcdn（vue-router/axios/dayjs 等已 404） |

---

## 本节核心表清单

| 表名 | 用途 |
|------|------|
| **BASE_MODULE** | water 菜单禁用（`scripts/sql/disable-water-menus.sql`） |

## 本节关键代码路径索引

| 路径 | 说明 |
|------|------|
| `scripts/sql/disable-water-menus.sql` | water 菜单禁用 SQL |
| `jnpf-web-vue3/src/views/common/dynamicModel/list/CustomBatchForm.vue` | GAP-03 已补 |
| `jnpf-web-vue3/src/views/common/dynamicModel/list/ExtendForm.vue` | GAP-03 已补 |
| `jnpf-web-vue3/.env.production.dist-v1.1.template` | 生产 env 基准 |

---

**F2 完成后**：填写 [`03-GAP待补清单与功能对等验收表.md`](03-GAP待补清单与功能对等验收表.md) 冒烟列。
