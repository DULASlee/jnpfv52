# water 模块路径清单（从 web/dist_v1.1 提取）

> **适用源码**：JNPF v5.2  
> **源码仓库**：`d:\JNPF-v52\backend`  
> **文档编号**：v52-arch-W  
> **文档版本**：v1.0  
> **文档状态**：维护中  
> **批准日期**：2026-05-24  

> **提取时间**：2026-05-22  
> **来源**：`web/dist_v1.1/static/js/*.js` 中 `import.meta.glob` / 动态 import 路径  
> **处置结论**（2026-05-22 · LOG-20260522-015）：**不补回源码**；执行 [`scripts/sql/disable-water-menus.sql`](../../scripts/sql/disable-water-menus.sql) 将 **BASE_MODULE** 中 water 菜单 `F_ENABLED_MARK=0`  
> **原因**：本仓库 `modularity/` **无** `ZX_Water` Service；补前端亦无法调用 `/api/ZX_Water/*`  
> **机器可读清单**：[`water-module-from-dist.json`](water-module-from-dist.json)  
> **OpenSpec**：[`frontend-align-dist-v1`](../../openspec/specs/frontend-align-dist-v1/spec.md) GAP-01

---

## 1. 子模块总览

| 子模块 | 页面数 | 业务含义（据路径推断） |
|--------|--------|------------------------|
| `views/water/baseinfo/area/` | 3 | 区域基础信息（列表/表单/详情） |
| `views/water/customer/` | 2 | 客户管理 |
| `views/water/payment/` | 4 | 缴费/账单（含 index/index1/index2 多视图） |

**合计**：9 个 `.vue` 页面

---

## 2. 完整路径清单（建议在源码中的落点）

| # | dist 中的 views 路径 | 建议补回至 |
|---|----------------------|------------|
| 1 | `views/water/baseinfo/area/index.vue` | `jnpf-web-vue3/src/views/water/baseinfo/area/index.vue` |
| 2 | `views/water/baseinfo/area/Form.vue` | `jnpf-web-vue3/src/views/water/baseinfo/area/Form.vue` |
| 3 | `views/water/baseinfo/area/Detail.vue` | `jnpf-web-vue3/src/views/water/baseinfo/area/Detail.vue` |
| 4 | `views/water/customer/index.vue` | `jnpf-web-vue3/src/views/water/customer/index.vue` |
| 5 | `views/water/customer/Form.vue` | `jnpf-web-vue3/src/views/water/customer/Form.vue` |
| 6 | `views/water/payment/index.vue` | `jnpf-web-vue3/src/views/water/payment/index.vue` |
| 7 | `views/water/payment/index1.vue` | `jnpf-web-vue3/src/views/water/payment/index1.vue` |
| 8 | `views/water/payment/index2.vue` | `jnpf-web-vue3/src/views/water/payment/index2.vue` |
| 9 | `views/water/payment/Form.vue` | `jnpf-web-vue3/src/views/water/payment/Form.vue` |

---

## 3. dist 中关联的独立 chunk（节选）

以下 chunk 文件名含 water 相关逻辑，可用于反编译参考（完整映射见 JSON）：

- `web/dist_v1.1/static/js/Bill-e217bf5e.js` — 水电费用清单（`Bill.vue` 编译产物）
- 主 bundle `index-f8698ae9.js` — 路由表与 `views/water/*` 懒加载映射

---

## 4. 处置记录（2026-05-22，取代原「补回步骤」）

| 步骤 | 动作 | 证据 |
|------|------|------|
| 1 | 确认后端无 `ZX_Water` Service | `modularity/` 检索无匹配 |
| 2 | 执行 `disable-water-menus.sql` @ DevTest 库 | water 启用菜单 **0** 条 |
| 3 | 浏览器登录验收 | 侧栏无 water/水务入口 |
| 4 | 源码 | **不创建** `jnpf-web-vue3/src/views/water/` |

若未来业务恢复水务模块，须 **先后端 Service + 表结构**，再按 §2 路径补前端并重新启用 **BASE_MODULE** 菜单。

---

## 5. 原「补回步骤建议」（已废弃，仅作历史参考）

<details>
<summary>展开：2026-05-22 前的补回草案</summary>

1. 在 `jnpf-web-vue3/src/views/water/` 按 §2 重建目录。
2. 从 dist chunk 反推 SFC，或从原构建机/备份恢复 `.vue` 源文件。
3. 在 **BASE_MODULE** 确认菜单 `urlAddress` 指向 `water/...` 路径。
4. 执行 `pnpm build`，确认 `dist/static/js/` 重新出现 `views/water` 相关 chunk。
5. 与 `web/dist_v1.1` 对比路由与页面功能。

</details>

## 本节关键代码路径索引

| 路径 | 说明 |
|------|------|
| `web/dist_v1.1/static/js/index-f8698ae9.js` | 含 `../../views/water/` 动态 import |
| `jnpf-web-vue3/src/views/water/` | **不创建**（GAP-01 菜单禁用） |
| `docs/architecture/water-module-from-dist.json` | 路径 + chunk 映射 |
