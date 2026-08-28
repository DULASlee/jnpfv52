# Platform Asset Inventory v1.0（平台资产清单）

**版本**：v1.0 定稿 ｜ **日期**：2026-08-26
**任务**：jnpf-v52-goal / T1.1 ｜ **政策依据**：MASTER 总体设计规格 §产品资产分层；ng1a 产品边界 + ng1b 溯源矩阵
**数据 CSV**：[platform-asset-inventory.v1.csv](./platform-asset-inventory.v1.csv)（289 行 × 15 列）

## 来源与合并口径

| 输入 | 路径 | 角色 |
|---|---|---|
| ng1a 分类矩阵 | `.claude/evidence/jnpf-next-architecture/ng1a-product-boundary/platform-asset-classification.csv` | product_asset_class / asset_lifecycle / tenant_style / evidence |
| ng1b 溯源矩阵 | `.claude/evidence/jnpf-next-architecture/ng1b-provenance/provenance-matrix.csv` | write_owner / read_consumers / api_exposed / code_owner / creation_source |

合并键 `table_name` 大小写不敏感：两矩阵各 289 唯一键，交集 289、差集 0。

## 处置判定（disposition）

- **ENTER（进入重构范围）**：`asset_lifecycle = MANDATORY` — 平台运行必需，行为受考卷保护；
- **FREEZE（处置冻结）**：其余生命周期 — 不进入重构波次，禁止修改其结构与行为，仅允许只读访问或按处置策略退役。

## 复算验证（T1.1 验收数字）

```
total   = 289
ENTER   = 157  （P0_PLATFORM_CORE 146 ＋ P1_LOWCODE_RUNTIME 11）
FREEZE  = 132  （TEMPLATE 48 ＋ LEGACY 47 ＋ DEMO 25 ＋ UNKNOWN 6 ＋ CUSTOMER_GENERATED 5 ＋ ORPHAN 1）
校验    : 157 ＋ 132 = 289 ✅
键一致性 : classification 289 ∩ provenance 289 = 289，差集 0 ✅
```

## 分层分布

| product_asset_class | lifecycle | 数量 | disposition |
|---|---|---:|---|
| P0_PLATFORM_CORE | MANDATORY | 146 | ENTER |
| P1_LOWCODE_RUNTIME | MANDATORY | 11 | ENTER |
| P2_PRODUCT_TEMPLATE | TEMPLATE | 48 | FREEZE |
| P6_LEGACY | LEGACY | 47 | FREEZE |
| P3_DEMO_APPLICATION | DEMO | 25 | FREEZE |
| PX_UNKNOWN | UNKNOWN | 6 | FREEZE |
| P4_CUSTOMER_APPLICATION | CUSTOMER_GENERATED | 5 | FREEZE |
| P7_ORPHAN | ORPHAN | 1 | FREEZE |

## 与 L1 表级螺旋的相容性

`.claude/evidence/backend-refactor/l1/l1-batch-order.csv` 入围 142 张表经本清单复核：**142/142 全部落于 ENTER 层**（L1 已排除 sa_\* 13 张与框架表 2 张）。两工件自洽，L1 螺旋不会触碰任何 FREEZE 资产。

## 冻结层后续动作（不在 T2.0 波次内）

| 生命周期 | 数量 | 后续 |
|---|---:|---|
| TEMPLATE | 48 | 产品模板内容，随发布流程管理，不参与架构迁移 |
| LEGACY | 47 | 对照 Legacy Compatibility Registry（T0.4）逐项 REMOVE/REDEFINE/DEPRECATE/KEEP |
| DEMO | 25 | 演示数据，可在客户交付前清理 |
| UNKNOWN | 6 | 待补充溯源证据后重新分类（不阻塞主链） |
| CUSTOMER_GENERATED | 5 | 客户自建表，交付时随租户数据导出 |
| ORPHAN | 1 | 无任何代码引用，列入退役候选 |

## 变更纪律

- 本清单为 S1 家底定稿工件；此后任何表级操作必须先对照 disposition；
- ENTER 层结构变更 → 编号迁移脚本 + 考卷绿（见《L1-表级螺旋执行手册》）；
- FREEZE 层一律冻结；重分类需走 C 级裁决会并回写本清单。
