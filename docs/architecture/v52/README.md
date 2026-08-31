# JNPF v5.2 架构内参索引

> **适用源码**：JNPF v5.2  
> **源码仓库**：`d:\JNPF-v52\backend`  
> **文档状态**：**01–11 v2.0-final 全系列闭合**（2026-05-24）  
> **编写规范**：[`../ARCHITECTURE_DOC_RULES.md`](../ARCHITECTURE_DOC_RULES.md) · [`../_template.md`](../_template.md)  

---

## ⚠ 重要说明

2026-05-24 前「迁入 v52/」的正文已判定为 **v3.6 内容 + v5.2 标签**，存在端口/拓扑错误（如 `localhost:5000`）。  
已全部移至 [`../archive/pre-v52-rewrite/`](../archive/pre-v52-rewrite/) — **只读参考，禁止复制回 v52/**。

**本目录仅维护经 v5.2 源码核验后的新文档。**

---

## 文档清单

| 批次 | 文档 | 状态 |
|------|------|------|
| 规划 | [`CATALOG.md`](CATALOG.md) | ✅ 三批编写大纲 |
| 指南 | [`V5.2版本架构文档编写指南第一部分.md`](V5.2版本架构文档编写指南第一部分.md) | ✅ v1.1 源码对照修订 |
| 总纲 | [`00-outline-core-framework.md`](00-outline-core-framework.md) | ✅ 专项01 任务书 |
| **第一批** | [`01-core-framework.md`](01-core-framework.md) | ✅ v2.0-final（2026-05-24 审核通过） |
| 第二批 | [`02-application-services.md`](02-application-services.md) | ✅ v2.0-final（2026-05-24 审核通过） |
| 第二批 | [`03-application-modules-deep-dive.md`](03-application-modules-deep-dive.md) | ✅ v2.0-final（2026-05-24 审核通过） |
| 第二批 | `02-R` | 待编写 |
| **第三批** | [`04-application-frontend-deep-dive.md`](04-application-frontend-deep-dive.md) | ✅ v2.0-final（2026-05-24 审核通过） |
| 第三批 | [`05-visual-data-deep-dive.md`](05-visual-data-deep-dive.md) | ✅ v2.0-final（2026-05-24 审核通过） |
| 第三批 | [`06-mobile-uniapp-deep-dive.md`](06-mobile-uniapp-deep-dive.md) | ✅ v2.0-final（2026-05-24 审核通过） |
| 第三批 | `water` | 待评估 |
| **第四批** | [`07-cache-middleware-deep-dive.md`](07-cache-middleware-deep-dive.md) | ✅ v2.0-final（2026-05-24 审核通过） |
| 第四批 | [`08-mq-and-events-deep-dive.md`](08-mq-and-events-deep-dive.md) | ✅ v2.0-final（2026-05-24 审核通过） |
| 第五批 | [`09-frontend-runtime-deep-dive.md`](09-frontend-runtime-deep-dive.md) | ✅ v2.0-final（2026-05-24 审核闭合） |
| 第五批 | [`10-workflow-engine-deep-dive.md`](10-workflow-engine-deep-dive.md) | ✅ v2.0-final（2026-05-24 审核通过；编写指南原 11§3） |
| 第五批 | [`11-plugins-integration-deep-dive.md`](11-plugins-integration-deep-dive.md) | ✅ v2.0-final（2026-05-24 审核闭合） |
| **运行态第一期** | [`runtime-phase1-detailed-design-OUTLINE.md`](runtime-phase1-detailed-design-OUTLINE.md) | 🔴 v0.1-OUTLINE（2026-06-15 起草 · 待各章填充） |
| **数据库现代化治理** | [`database-modernization/`](database-modernization/) | ✅ v1.1（2026-08-30）Phase 8 工作成果归档 |

#### 数据库现代化治理 · 详细清单

| 文档 | 受众 | 内容定位 |
|------|------|---------|
| [`database-modernization/README.md`](database-modernization/README.md) | 索引 | 子目录导航与阅读路径 |
| [`database-modernization/JNPF-数据库现代化治理-架构设计与工作成果报告.md`](database-modernization/JNPF-数据库现代化治理-架构设计与工作成果报告.md) | 客户/管理层/团队工程师 | **架构设计与工作成果合并版**（中文为主） |
| [`database-modernization/JNPF-AI-数据库治理-转型报告.md`](database-modernization/JNPF-AI-数据库治理-转型报告.md) | 管理层/技术委员会 | 战略叙事 + 跨项目复用 |
| [`database-modernization/JNPF-表级重构-管理层报告.md`](database-modernization/JNPF-表级重构-管理层报告.md) | 管理层/产品 | 业务价值翻译 + ROI |
| [`database-modernization/JNPF-表级重构-技术变更目录.md`](database-modernization/JNPF-表级重构-技术变更目录.md) | 架构师/DBA/研发 | 单表详细记录（90+ 表 × 7 维度） |
| [`database-modernization/JNPF-表级重构-登记表.csv`](database-modernization/JNPF-表级重构-登记表.csv) | AI/Excel/工具 | 244 行机器可读清单 |
| [`database-modernization/Phase-8-最终关闭报告.md`](database-modernization/Phase-8-最终关闭报告.md) | 项目历史归档 | Phase 8 阶段关闭报告 |

**关键数据**：治理 248/274 张表（90.5%），190 索引优化，0 事故。  
**关联 ADR**：`docs/adr/ADR-019~023.md`（5 个架构决策记录）。

| **质量诊断** | [`design-quality-diagnostics.md`](design-quality-diagnostics.md) | ✅ v1.0（2026-08-06）八类方法手册 |
| 质量诊断 | [`design-quality-hotspot-top20.md`](design-quality-hotspot-top20.md) | ✅ Hotspot Top20 快照 |
| 质量诊断 | [`design-quality-frontend-index-summary.md`](design-quality-frontend-index-summary.md) | ✅ 三前端索引摘要 |
| 质量诊断 | [`design-quality-frontend-ct-report.md`](design-quality-frontend-ct-report.md) | ✅ 前端 CT 实测（vue-mess-detector） |
| 质量诊断 | [`design-quality-baseline-gates.md`](design-quality-baseline-gates.md) | ✅ 基线门禁设计（未实现） |

---

## v5.2 环境锚点（编写时强制）

| 服务 | 地址 |
|------|------|
| 后端 API | `http://localhost:30000` |
| 主 WEB | `http://localhost:3100`（proxy `/dev` → `:30000`） |
| 数字大屏 | `http://localhost:3102/DataV/` |
| UniApp H5 | `http://localhost:3800` |
| Univer 报表 API | `/reportDev` → `:32000`（见 [11 §4](11-plugins-integration-deep-dive.md)） |
| Univer 报表静态 | `:8200`；旧 ReportServer `:30007` |

❌ 禁止在 v52 文档中出现 **`localhost:5000`**（v3.6 旧拓扑）。

---

## 参考材料（不得迁入 v52）

| 路径 | 用途 |
|------|------|
| [`../archive/pre-v52-rewrite/`](../archive/pre-v52-rewrite/) | hybrid 快照：章节 checklist |
| [`../archive/v36/`](../archive/v36/) | v3.6 历史（待外部入库） |
| [`../../架构迭代/6、培训与操作手册/5、JNPF-v5.2操作手册使用指引.md`](../../架构迭代/6、培训与操作手册/5、JNPF-v5.2操作手册使用指引.md) | 环境 + 操作 |

---

## PR 审查检查项

- [ ] 正文位于 `v52/` 且状态非「从 archive 复制」
- [ ] 端口/URL 与上表或操作手册一致
- [ ] API 写 `*Service` + DynamicApi，表名 **BASE_***
- [ ] 文档头 + 每章核心表清单 + 代码路径索引
- [ ] 无未标注的【待源码验证】以外的臆造路径

---

## 施工包（非架构内参）

[`../phase2/README.md`](../phase2/README.md)


