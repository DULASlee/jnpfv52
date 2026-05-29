# JNPF v5.2 架构文档目录大纲

> **适用源码**：JNPF v5.2  
> **文档状态**：**01–11 v2.0-final 全系列闭合** · 2026-05-24  
> **原则**：v5.2 目录下每一行均须对 v5.2 源码负责；v3.6 / hybrid 快照仅作参考  

---

## 第一批（优先 — 版本差异最大、最易踩坑）

| 编号 | 文件 | 状态 | 说明 |
|------|------|------|------|
| 01 | [`01-core-framework.md`](01-core-framework.md) | ✅ v2.0-final | Serve.Run、DynamicApi、SqlSugar、JWT、中间件、部署拓扑 |
| — | [`00-outline-core-framework.md`](00-outline-core-framework.md) | ✅ 总纲 | 专项01 章节与产出清单 |
| ENV | 内嵌 01 第一章 / 独立 `00-environment-topology.md`（可选） | 规划 | :30000 / :3100 / :8100 / :3800；与操作手册对齐 |

**第一批完成标准**：新人仅读 01 + 操作手册环境章，可正确启动 v5.2 全链路。

---

## 第二批（应用横切 + 业务模块）

| 编号 | 文件 | 状态 | 说明 |
|------|------|------|------|
| 02 | [`02-application-services.md`](02-application-services.md) | ✅ v2.0-final | DI、Filter、数据权限、事务、文件、API 规范 |
| 03 | [`03-application-modules-deep-dive.md`](03-application-modules-deep-dive.md) | ✅ v2.0-final | Systems 六大模块 BASE_* |
| 02-R | `02-application-services-review.md` | 待编写 | 02 完成后审查 |

---

## 第二批（05–08 · 2026-05-24）

| 编号 | 文件 | 状态 | 说明 |
|------|------|------|------|
| 05 | [`05-visual-data-deep-dive.md`](05-visual-data-deep-dive.md) | ✅ v2.0-final | `jnpf-web-datascreen-vue3`；`:8100`；`/api/blade-visual/`；BLADE_*；lazy-list 已知缺陷 |
| 06 | [`06-mobile-uniapp-deep-dive.md`](06-mobile-uniapp-deep-dive.md) | ✅ v2.0-final | `jnpf-app-vue3`；HBuilderX / `:3800`；`jnpf-origin: app` |
| 07 | [`07-cache-middleware-deep-dive.md`](07-cache-middleware-deep-dive.md) | ✅ v2.0-final | `ICacheManager`、Cache.json、28 项键清单、Cache-Aside |
| 08 | [`08-mq-and-events-deep-dive.md`](08-mq-and-events-deep-dive.md) | ✅ v2.0-final | EventBus Memory 默认；8 事件；TaskQueue 对比 |

**第二批进度（05–08）**：✅ 全部 v2.0-final（2026-05-24 审核通过）。

---

## 第三批（09–11 · 2026-05-24 起）

| 编号 | 文件 | 状态 | 编写指南原编号 | 说明 |
|------|------|------|----------------|------|
| 10 | [`10-workflow-engine-deep-dive.md`](10-workflow-engine-deep-dive.md) | ✅ v2.0-final | 原 11 §3 工作流 | 自研 JSON 状态机；FLOW_* ×18；API 不一致已知缺陷 |
| 09 | [`09-frontend-runtime-deep-dive.md`](09-frontend-runtime-deep-dive.md) | ✅ v2.0-final | 原 09+10 运行时+Codegen | Parser/Jnpf/columnData；OnlineDev API |
| 11 | [`11-plugins-integration-deep-dive.md`](11-plugins-integration-deep-dive.md) | ✅ v2.0-final | 原 11 汇总+补遗 | 拓扑/报表/文件/SSO/速查表 |


## 第三批（前端主 WEB — 已完成）

| 编号 | 文件 | 状态 | 说明 |
|------|------|------|------|
| 04 | [`04-application-frontend-deep-dive.md`](04-application-frontend-deep-dive.md) | ✅ v2.0-final | `jnpf-web-vue3`；**proxy → :30000**；`:3100` |
| W | `water-module-from-dist.md` | 待评估 | 菜单已禁用；可降为附录 |

---

## （原第四批已合并入第二批 07/08）

---

## 非架构内参（位置不变）

| 目录 | 内容 |
|------|------|
| [`../phase2/`](../phase2/) | 二期施工包 |
| [`../archive/v36/`](../archive/v36/) | v3.6 历史快照（待外部入库） |
| [`../archive/pre-v52-rewrite/`](../archive/pre-v52-rewrite/) | 迁入失败 hybrid 快照（只读参考） |

---

## 参考 vs 复用

| 来源 | 用法 |
|------|------|
| `archive/pre-v52-rewrite/` | 章节 checklist、曾覆盖主题 |
| `archive/v36/` | v3.6 业务背景、术语 |
| 三份操作手册 | 环境端口、操作流程（使用者视角） |
| **禁止** | 复制粘贴配置片段、端口、类名路径未验证段落 |

---

## 关联

- 编写铁律：[`../ARCHITECTURE_DOC_RULES.md`](../ARCHITECTURE_DOC_RULES.md)  
- 文档模板：[`../_template.md`](../_template.md)  
- 操作手册指引：[`../../架构迭代/6、培训与操作手册/5、JNPF-v5.2操作手册使用指引.md`](../../架构迭代/6、培训与操作手册/5、JNPF-v5.2操作手册使用指引.md)
