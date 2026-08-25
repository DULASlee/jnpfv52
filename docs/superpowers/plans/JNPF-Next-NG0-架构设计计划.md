# JNPF-Next NG-0 架构设计计划 v1.0

**日期**：2026-08-25 ｜ **阶段**：NG-0（只读设计——禁止业务代码实施）
**状态**：设计产物完成，待人工裁决（APPROVE → NG-1 / REFINE / REJECT）

---

## 1. 本阶段产出（已完成）

| 类 | 文件 |
|----|------|
| 规格 ×5 | JNPF-Next-数据库架构设计规格 / 领域与模块边界规格 / 数据访问架构规格 / 租户权限架构规格 / Aspire与运行架构规格 |
| 证据 ×10 | database-inventory / database-relationship-map / query-hotspots / data-ownership-map / domain-candidates / legacy-compatibility-map / modular-monolith-vs-microservices / aspire-evaluation / migration-strategy / cost-and-risk-estimate |
| 数据底账 | db-tables.tsv（289 表）/ db-index-stats / db-audit-cols / db-type-dist / db-prefix-clusters / db-nopk / db-noidx / db-fks |

## 2. NG-0 核心结论（一页）

1. **数据库**：289 表/6134 列；FK 14（275 表零外键=隐式关系单体）；租户列三风格（219 表）；字符串 ID 主键 77%；JSON 大字段 161 表；
2. **领域**：12 候选域（D1-D12）不沿用项目目录；全部事务边界单库 → **Modular Monolith 首选**（形态 A）；
3. **数据访问**：Producer→Adapter→Consumer 形态；权限评估 API（33+43 特征为等价基线）；显式关系注册表替代零 FK；
4. **租户权限**：单一 tenant_id 契约 + 显式 TenantContext；怪异逐项裁决（Q1-Q6 KEEP/Q7-Q11 REDEFINE·REMOVE/E 系列 REMOVE·REDEFINE）；
5. **Aspire**：采用为开发编排+遥测底座；**非微服务前提**；Broker 不引入（出箱表+进程内总线先行）；
6. **迁移**：8 波次（W1 字典/File → W2 沙盘 → W3 权限快照 → W4 Identity API 化 → W5 工作流事件化 → W6 Form/LC → W7 AI 自治 → W8 租户统一）；总量 65-100 人周；每波独立回滚；
7. **不做**：微服务设计（冻结）/Legacy 重构（P0-C DEFERRED/S2 BLOCKED）/索引调优（NG-1 DMV）/修怪异。

## 3. NG-1 范围（待批准后启动——本计划不含实施）

- 数据库反向工程深化（DMV/慢查询/TransactionScope 扫描）；
- 权限评估 API 原型（33+43 特征等价）；
- Identity 读取 API 原型；
- 动态表注册表裁决（mt* 归属）；
- Aspire AppHost 骨架 + 日志独立存储选型；
- File 独立进程原型（零依赖验证）；
- 跨租户泄漏回归套件设计。

## 4. 闸门

```text
NG-0 ──► REJECT（回 Legacy 路线）
    ├──► REFINE（补充设计——人工指定范围）
    └──► APPROVE（进入 NG-1 原型）
NG-1 完成后重新人工裁决；不自动进入 NG-2。
```

## 5. 提交边界

- 本阶段仅文档/证据/数据底账（零代码）；
- 不修改 Legacy 业务代码/数据库/API/UniApp；
- 独立提交，可整体 revert。
