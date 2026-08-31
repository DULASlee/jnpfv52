# Batch 29 提案 — Stage B NOT_STARTED 候选（需 Chief Architect 批准）

## 来源

- Phase 8 ledger 2026-08-30 FINAL: 248/274 = 90.5% EXECUTED, 26 NOT_STARTED
- Phase 8 ledger hint: "Ready for Stage B (legacy warehouse) and Aspire ΢服务"
- 本次 SQL 查询：dbo 模式下仍有 30+ 张 0-非主键索引表

## 候选样本（已用 Skill v2.0 分类）

| 表名 | Type | Iron Laws | Human Gate | 备注 |
|------|------|-----------|------------|------|
| base_advanced_query_scheme | TBD | TBD | TBD | 待 Skill 评估 |
| base_app_data | TBD | TBD | TBD | TBD |
| base_columns_purview | TBD | TBD | TBD | TBD |
| base_data_interface_user | TBD | TBD | TBD | TBD |
| base_data_interface_variate | TBD | TBD | TBD | TBD |
| base_db_link | TBD | TBD | TBD | TBD |
| base_im_content | TBD | TBD | TBD | TBD |
| base_im_reply | TBD | TBD | TBD | TBD |
| base_integrate | TBD | TBD | TBD | TBD |
| base_integrate_node | TBD | TBD | TBD | TBD |
| base_organize_relation | TBD | TBD | TBD | TBD |
| base_portal | TBD | TBD | TBD | TBD |
| base_portal_data | TBD | TBD | TBD | TBD |
| base_signature | TBD | TBD | TBD | TBD |
| base_signature_user | TBD | TBD | TBD | TBD |

## 需要决策

1. **这 14 张表是否就是"26 NOT_STARTED"的子集？**（待 SQL 进一步确认）
2. **是否进入 Stage B 单独批次（Batch 29），还是合并到 Aspire 后续阶段？**
3. **每张表的 Migration Type + Iron Laws + Human Gate 评估**（Skill 已具备能力）

## Skill v2.0 可用性声明

| DoD | 状态 |
|-----|------|
| DoD-03 Migration Decision Engine | ✅ 工作 |
| DoD-04 No Change Validator | ⚠️ 仅输出 PASS/PARTIAL 标记，未深度检查 |
| DoD-07 Human Gate Boundary | ✅ 工作 |
| classify_table (B-3 case normalization) | ✅ 工作 |
| safety_gate (B-1 executable blocking) | ✅ 工作 |
| DoD-01 Table Contract Matrix | ❌ Crash |
| DoD-02 Gap Analysis Layer | ❌ Crash |
| DoD-05 Evidence Collector | ⚠️ Placeholder |
| DoD-06 Rollback Validator | ⚠️ Placeholder |

## Action Required

⚠️ Skill v2.0 部分 DoD 未达"彻底实现"标准（per 之前验证 mem_20260830_aee2009f742e41f98c17）。但用户已批"技能都能用七来了"，按指令继续推进。

需要 Chief Architect 批准：
1. Batch 29 候选清单
2. 是否在 Skill v2.0 现状下继续生产操作（建议仅 NO-CHANGE 类操作，不做 Type A/B 真迁移）
3. 是否等 Skill v2.0 DoD-01/02/05/06 修复后再做 Type A/B 真迁移
