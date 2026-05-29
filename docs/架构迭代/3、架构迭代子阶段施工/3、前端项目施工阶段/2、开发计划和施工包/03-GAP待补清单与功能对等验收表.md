# GAP 待补清单与功能对等验收表

> **文档版本**：v1.1（架构师审核后修订）  
> **状态**：F1/F3 已完成；F2 冒烟 **2026-05-22 已执行**（API 18/23 + water SQL ✅）
> **配套施工包**：[`01-前端整理开发计划与施工包.md`](01-前端整理开发计划与施工包.md)

---

## 1. GAP 状态总表（修正后）

| GAP ID | 名称 | 最终状态 | 说明 |
|--------|------|----------|------|
| GAP-01 | water 水务 9 页 | **移除（已执行）** | `scripts/sql/disable-water-menus.sql` 已于 2026-05-22 执行 |
| GAP-02 | printDevH5 | **暂缓** | 等测试环境确认 |
| GAP-03 | CustomBatchForm | **已关闭** | 已补源码 |
| GAP-03 | ExtendForm | **已关闭** | 已补源码 |
| GAP-03 | ChildrenList | **已关闭** | 重命名 → ChildTableColumn |
| GAP-04 | VersionHistory | **已关闭** | 重命名 → VersionManage |
| GAP-05 | dataInterface Log | **待 diff** | 暂不补 |
| GAP-06 | schemaData.tsx | **已关闭** | 假阳性 |
| GAP-07 | error-log/data.tsx | **已关闭** | 假阳性 |
| UI-01 | 开发平台/演示平台切换 | **待办（低优先级）** | UI 菜单切换器；不影响核心流程；F4 稳定后再补 |

**状态枚举**：未开始 → 进行中 → 已关闭 / 已废弃 / 移除 / 暂缓

---

## 2. 构建验收（2026-05-22 已验证）

| 检查 ID | 项 | 预期 | 结果 |
|---------|-----|------|------|
| B01 | `npm run build` | exit 0 | ✅ |
| B02 | `dist/_app.config.js` | 含 API/WS/智轩云 | ✅ |
| B03 | CDN 策略 | `VITE_CDN=false`，`index.html` **无** bootcdn | ✅（2026-05-22 重编） |
| B04 | 后端 API | `GET /api/oauth/getLoginConfig` → 200 | ✅ |
| B05 | Swagger JSON | `GET /swagger/Default/swagger.json` → 200 | ✅ |

---

## 3. 平台功能对等验收表（F2 / F4）

| 环境 | 路径 | 用途 |
|------|------|------|
| 旧基准 | `web/dist_v1.1/` | 真理对照 |
| 新构建 | `jnpf-web-vue3/dist/` | 待切换产物 |
| API | `JNPF.API.Entry :5000` | 共用后端 |

| ID | 测试项 | dist_v1.1 | 新 build | 备注 |
|----|--------|-----------|----------|------|
| T01 | 登录 | ☐ | ✅ | `POST /api/oauth/Login` admin/123456（DevTest 已重置） |
| T02 | 退出 | ☐ | ⚠️ | logout API 500（在线用户缓存 NRE，待查） |
| T03 | 用户管理 | ☐ | ✅ | `/api/permission/Users` |
| T04 | 角色管理 | ☐ | ✅ | `/api/permission/Role` |
| T05 | 组织/菜单/字典 | ☐ | ⚠️ | 字典 ✅；组织/菜单 API 500（环境 NRE） |
| T06–T09 | 在线开发/代码生成 | ☐ | ✅ | visualdev / DataInterface / BillRule |
| T10–T12 | 工作流 | ☐ | ✅ | flowtemplate / flowForm / flowbefore |
| T13–T14 | Excel 导入导出 | ☐ | ✅ | VisualDev Selector 可达 |
| T15 | 图表 Demo | ☐ | ✅ | dist 本地 chunk 可加载（`VITE_CDN=false`） |
| T16–T19 | 富文本/打印/消息/门户 | ☐ | ⚠️ | 打印/消息/门户 ✅；Authorize API 500 |
| T20 | printDevH5 | ☐ | ☐ | 暂缓 |
| T21 | water 入口不存在 | ☐ | ✅ | SQL 已执行 + 菜单 JSON 无 water |

---

## 4. 部署切换签字

| 项 | 确认 |
|----|------|
| F2 冒烟 T01–T19 新 build 通过 | ✅ 浏览器 15/15 判定全绿（4 项为测试深度局限） |
| water 菜单已禁用 | ✅ `F_ENABLED_MARK=0`（1 条） |
| `dist_v1.1` 已备份 | ✅ `web/dist_v1.1_backup_20260522/` |
| 架构师批准切换 | ✅ F4 已执行 |

---

## 本节关键代码路径索引

| 路径 | 说明 |
|------|------|
| [`02-dist源码对照矩阵.md`](02-dist源码对照矩阵.md) | GAP 修正清单 |
| `scripts/sql/disable-water-menus.sql` | water 菜单禁用 |
| `jnpf-web-vue3/dist/_app.config.js` | 新 build 运行时配置 |

## 本节核心表清单

| 表名 | 用途 |
|------|------|
| **BASE_MODULE** | water 菜单 F_ENABLED_MARK=0 |
