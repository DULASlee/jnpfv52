# JNPF Table Refactoring — Project Charter

> **Document**: JNPF-Table-Refactoring-Charter.md
> **Version**: v1.0
> **Date**: 2026-08-31
> **Status**: `FINAL ACCEPTED`
> **Scope**: JNPF v5.2 Backend Table-Level Schema Refactoring

---

## 1. 为什么做（Why）

JNPF v5.2 后端存在两张表（`BASE_SIGNATURE`、`BASE_SIGNATURE_USER`）缺少主键约束，违反以下架构原则：

1. **ORM 兼容性**：SqlSugar Insertable/Updateable 要求非自增实体必须有明确 PK
2. **关联表语义**：`BASE_SIGNATURE_USER` 是典型关联表，复合主键（f_signature_id, f_user_id）正确反映业务关系
3. **Schema 完整性**：无 PK 的表无法建立有效 FK 关系，限制未来扩展

---

## 2. 做什么（What）

### 本次完成范围

| 迁移ID | 表 | 操作 | 状态 |
|:---|:---|:---|:---|
| M32-01 | BASE_SIGNATURE | ADD PRIMARY KEY (f_id) | ACTUALLY_FIXED |
| M32-02 | BASE_SIGNATURE_USER | ADD PRIMARY KEY (f_signature_id, f_user_id) | ACTUALLY_FIXED |

###  corrective Step（M32-02 前置条件）

`f_signature_id` 和 `f_user_id` 在 DB 层为 NULLABLE，SQL Server 要求 PK 列必须 NOT NULL。执行 M32-02 前需：

```sql
ALTER TABLE base_signature_user
    ALTER COLUMN f_signature_id NVARCHAR(50) NOT NULL;
ALTER TABLE base_signature_user
    ALTER COLUMN f_user_id NVARCHAR(50) NOT NULL;
```

**授权来源**：Chief Architect 2026-08-31 即时授权
**前提条件**：两表均为空（0行），零数据风险

---

## 3. 不做什么（What NOT）

以下内容**明确不在本次范围**内，禁止借本次重构之名实施：

| 排除项 | 原因 |
|:---|:---|
| 15 个 Tenant Index | 生产数据不足，无法测量选择性收益 |
| FK 现代化 | 超出范围 |
| 命名规范变更 | 超出范围 |
| Datetime 迁移 | 超出范围 |
| 密码现代化 | 超出范围 |
| BASE_USER 重新设计 | 超出范围 |
| CQRS / Outbox / RLS | 超出范围 |

---

## 4. 目标（Objectives）

| 目标 | 达成标准 |
|:---|:---|
| M32-01 执行成功 | PK_base_signature 存在于 sys.indexes |
| M32-02 执行成功 | PK_base_signature_user 存在于 sys.indexes |
| 无迁移回归 | Build = 0 errors，Relevant tests = PASS |
| Schema = Target | Actual = Approved Target |
| Rollback 可行 | rollback.sql 经 VALIDATED |

---

## 5. 完成标准（Done Criteria）

| 标准 | 目标 | 实际 |
|:---|:---|:---|
| M32-01 = ACTUALLY_FIXED | 1 | 1 ✅ |
| M32-02 = ACTUALLY_FIXED | 1 | 1 ✅ |
| G0 Critical | 0 | 0 ✅ |
| G1 Major | 0 | 0 ✅ |
| Migration-induced Regression | 0 | 0 ✅ |
| Build | 0 errors | 0 ✅ |
| Test (relevant) | 100% | 92/92 ✅ |
| 唯一失败项 | Pre-existing | SugarTable_Mappings ✅ |
| Rollback | DESIGNED + VALIDATED | ✅ |

---

## 6. 当前最终状态（Current Status）

```
JNPF BACKEND TABLE REFACTORING
= FINAL ACCEPTANCE APPROVED
= CLOSED
```

| 维度 | 数值 |
|:---|:---|
| ACTUALLY_FIXED | 2 |
| NO_CHANGE | 10 |
| DEFERRED | 7 |
| FALSE_POSITIVE | 17 |
| G0_CRITICAL | 0 |
| G1_MAJOR | 0 |
| **总计** | **19** |

---

## 7. 权威文档索引

| 文档 | 用途 | 位置 |
|:---|:---|:---|
| 本 Charter | 项目地图（给人看）| `docs/architecture/v52/database-modernization/` |
| Final Matrix (机器可读) | 单一事实源 | `backend/database/final-refactor/JNPF-Final-Refactoring-Matrix-vFinal.json` |
| Final Acceptance Report | 验收报告 | `backend/database/final-refactor/JNPF-Table-Refactoring-Final-Acceptance.md` |
| Final Validation Package | 验证证据 | `backend/database/final-refactor/final-validation/` |

---

## 8. 过程记录归档

历史过程文档已归档，禁止继续增加：

```
docs/universal/Phase-8/
  p8-c/batch-29 ~ batch-31/   ✅ 已归档
  Phase-8-最终关闭报告.md       ✅ 已归档

backend/database/phase-32/    ✅ M32-01 + M32-02 SQL
backend/database/final-refactor/  ✅ 最终交付物
```

**规则**：后续表级变更不得新建 batch-* 目录，应直接使用 `final-refactor/` 下的最终文档。