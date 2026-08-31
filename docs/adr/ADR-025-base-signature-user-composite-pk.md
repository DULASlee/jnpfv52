# ADR-025: 为什么 base_signature_user 使用 Composite Primary Key

> **ADR**: ADR-025
> **Title**: Composite Primary Key for BASE_SIGNATURE_USER
> **Status**: ACCEPTED
> **Date**: 2026-08-31
> **Context**: M32-02 Migration Decision — BASE_SIGNATURE_USER has no PK

---

## Context

`BASE_SIGNATURE_USER` 是关联表（signature 与 user 的多对多关系），当前无主键约束。

SqlSugar ORM 要求非自增实体必须有明确 PK 才能进行 Insertable/Updateable 操作。

Chief Architect 需要在两种方案中选择：

- **Option A**: 复合主键 `(f_signature_id, f_user_id)` — 保留关联表业务语义
- **Option B**: 新增代理单列主键 `f_id` — 不反映业务关系

---

## Decision

**采用 Option A：复合主键 `(f_signature_id, f_user_id)`**

---

## Reason

1. **业务语义正确**: `BASE_SIGNATURE_USER` 表示"某个用户有权限使用某个签章"，`(signature, user)` 组合唯一识别一条记录，复合 PK 正确反映了这种关联关系。

2. **自然唯一性**: 在业务上，一个用户不能在同一个签章上有重复记录，复合主键直接映射这个约束，无需额外 UNIQUE 索引。

3. **SqlSugar `[Navigate]` 兼容**: `SignatureUserEntity` 使用 `[Navigate(NavigationType.OneToMany, nameof(SignatureId))]`，FK 列是 `SignatureId`（指向父表 `Id`），与子表 PK 结构无关。复合 PK 不影响 ORM 导航。

4. **无性能损失**: 复合主键在 SQL Server 中等价于在 `(f_signature_id, f_user_id)` 上建唯一聚集索引，查询性能与单列 PK 无差异。

---

## Alternatives

| 方案 | 缺点 |
|:---|:---|
| Option B: 代理主键 `f_id` | 不反映业务关系，增加无业务意义的列，关联表语义模糊 |
| 不加 PK | SqlSugar Insertable/Updateable 报错，ORM 兼容性问题 |
| 唯一索引而非 PK | 需要额外维护 UNIQUE 约束，语义不正确 |

---

## Consequences

- M32-02 实际执行使用复合主键
- `f_signature_id` 和 `f_user_id` 需要 NOT NULL（SQL Server PK 要求），执行中发现两列为 NULLABLE，Chief Architect 授权 `ALTER COLUMN NOT NULL`（表为空，零数据风险）
- 后续如果 BASE_SIGNATURE_USER 需要支持同一用户对同一签章有多个版本（如时间有效性），需要重新评估（此时应改用 `(signature_id, user_id, valid_from)` 三元组）

---

## Status

**ACCEPTED** — M32-02 执行完成，PK 已落库。