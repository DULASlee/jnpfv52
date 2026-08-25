# NG-1A Final Review — Platform Product Boundary Audit 执行总结

**日期**：2026-08-26 ｜ **计划**：`docs/superpowers/plans/NG-1A-平台产品资产边界审计计划-v1.1.md` ｜ **裁决依据**：NG-1 规格 §0A（纠偏裁决）+ §0A.6（资产模型升级裁决：P0-PX + 二维）

---

## 1. 执行摘要

NG-1A 回答「如果把所有 Demo、示例、模板、历史业务表拿掉，JNPF 平台本身还剩哪些数据」：

- **289 表全部获得 P0-PX 分类 + 二维标签（PlatformRole × AssetLifecycle）**，0 表无分类；
- **平台资产 159 张（55.0%）**：P0 PLATFORM_CORE 148 + P1 LOWCODE_RUNTIME 11；
- **非平台资产 130 张（45.0%）**：P2 模板 48 + P3 演示 25 + P4 客户 5 + P6 遗留 45 + P7 孤儿 1 + PX 未知 6；
- **7 项命题 P1-P7 全部有裁决**（4 证伪 + 1 方法修正 + 2 成立），见 `product-boundary-proof.md` §2；
- **D12 Candidate Slice 维持暂停**；ext_* 初步裁决为 P3 DEMO + P2-template-candidate（§0A.6.4 四种可能之②/③ 待 Provenance Matrix 细分）。

## 2. 五零约束遵守声明

| 约束 | 遵守情况 |
|------|---------|
| 零业务代码修改 | ✅ 本批仅改文档/脚本/CSV（evidence 目录 + specs/plans） |
| 零数据库修改 | ✅ 全部 sqlcmd 只读查询 |
| 零微服务实现 | ✅ 无服务/边界代码 |
| 零 Aspire 引入 | ✅ 未触碰任何部署配置 |
| 零迁移 | ✅ 无 migration 文件变更 |

## 3. 证据链清单（可复核）

| 证据 | 文件 |
|------|------|
| E1 DB 元数据 289 表 | `ng1-batch1/db-matrix-raw.tsv`（sqlcmd 实测） |
| E2 实体映射 172 张 | `_entity-tables.txt`（全仓 SugarTable 提取，含 TableDescription 修正） |
| E3 init 脚本 273 张 | `_init-sql-tables.txt`（ZXAFINIT.sql 515MB） |
| E4 无实体表引用计数 | `_no-entity-refs.tsv`（126 张全仓字符串扫描） |
| E5 sa-service 双端引用 | 本批扫描（sa_ 前缀 196 处，排除 node_modules 噪音） |
| E6 服务/API 可达性 | 12 个 ext 子域服务 Glob + IDynamicApiController 验证 |

## 4. 289 表分类结果（v2）

| 类 | 表数 | 占比 | 裁决 |
|----|-----|------|------|
| P0 PLATFORM_CORE | 148 | 51.2% | ✅ 进入 NG 设计 |
| P1 LOWCODE_RUNTIME | 11 | 3.8% | ✅ 进入 NG 设计 |
| P2 PRODUCT_TEMPLATE | 48 | 16.6% | ❌ 独立模板包 |
| P3 DEMO_APPLICATION | 25 | 8.7% | ❌ 可删除/隔离 |
| P4 CUSTOMER_APPLICATION | 5 | 1.7% | ❌ 不进平台核心 |
| P5 TEST_FIXTURE | 0 | 0% | ❌（本库无） |
| P6 LEGACY | 45 | 15.6% | ❌ 归档 |
| P7 ORPHAN | 1 | 0.3% | ❌ 隔离 |
| P8 EXTERNAL | 0 | 0% | ❌（本库无） |
| PX UNKNOWN | 6 | 2.1% | ⏸ BLOCKED |

## 5. G0 状态（2026-08-26 人工裁决，§0A.7）

```text
G0 = Product Boundary Proof
├── 289 表 P0-PX 全覆盖 ✅
├── 二维分类全表输出 ✅
├── 硬规则 1-8 全部遵守 ✅
├── 四证据链可追溯 ✅
└── 人工裁决：PASS-PENDING-PROVENANCE（不最终 PASS）
```

**裁决记录**：NG-1A 验收通过；G0 登记 `PASS-PENDING-PROVENANCE`。第一轮边界证明已完成，但 Provenance Proof 未闭合 → 不得进入 Domain Ownership Proof、不得恢复 D12。已批准启动 NG-1B Provenance Matrix（只读审计，289 表 × 14 维，每表 PROVEN/PARTIAL/UNKNOWN），完成后 G0 Final Review 仅允许 PASS/REFINE/BLOCK。

## 6. 下一步（§0A.7，已批准启动 NG-1B）

**Provenance Matrix**：289 表 × 14 维（Creation Source / Code Owner / Write Owner / Read Consumers / API / UI-Menu / Template / Demo / Runtime / Startup / Product / Lifecycle / Provenance）的创建来源追踪，优先 ext_* / WFORM_* / WM_* / WH_* / base_* / sa_*。每表获得 PROVEN/PARTIAL/UNKNOWN 三态。完成后 G0 Final Review（PASS/REFINE/BLOCK）+ STOP 等裁决。
