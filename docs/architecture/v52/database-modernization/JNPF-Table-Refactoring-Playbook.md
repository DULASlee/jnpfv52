# JNPF Table Refactoring — Engineering Playbook

> **作用**: 以后 AI 再遇到 JNPF 表级重构的标准施工手册
> **Audience**: AI Agent（JNPF 表级变更第一步必读）
> **基于**: Skill v2.0 + Final Closure Sprint 经验沉淀

---

## Part 1: 标准流程（9 步）

### Step 1: 读取 Artifact Registry

**必读**: `docs/architecture/v52/database-modernization/JNPF-Table-Refactoring-Registry.yaml`

确认权威文档位置，不要从历史 batch 开始。

---

### Step 2: 读取 Target Schema Contract

**必读**: `docs/superpowers/specs/2026-08-30-JNPF-Target-Schema-Contract.md`

理解 8 维度目标契约：
- Column Naming
- Data Type
- Nullable Contract
- Tenant Model
- Audit Model
- Index Contract
- Constraint Contract
- Security Boundary

---

### Step 3: Classify Table Type

| 类型 | 标记 | 规则 |
|:---|:---|:---|
| SYSTEM_CORE | `base_*` P0-Security | 严格重构（IRON-TABLE-04）|
| BUSINESS_ENTITY | `flow_*`, `visualdev_*` 等 | 标准迁移治理 |
| DYNAMIC_FORM | `wform_*`, `lowcode_*` | **禁止自动改名** |
| USER_EXTENDED | `ext_*` | **禁止自动改名** |

---

### Step 4: Gap Analysis（6 维度）

对每张表执行 6 维度差距分析：

1. **Column Gap** — 列名/类型不匹配
2. **Type Gap** — 数据类型偏差
3. **Constraint Gap** — 主键/外键/唯一约束缺失
4. **Index Gap** — 索引缺失或冗余
5. **Security Gap** — 安全边界不合规
6. **Performance Gap** — 性能问题（需 Before/After 数据）

---

### Step 5: Migration Decision（Type A/B/C）

| 类型 | 触发条件 | 处理方式 |
|:---|:---|:---|
| **A** | 纯技术命名错误（拼写错误、大小写不一致）| `sp_rename` + Entity 同步 |
| **B** | 语义变更（字段含义变化）| 双写 6 个月 + Entity 双字段 `[Obsolete]` |
| **C** | 低代码动态表（`wform_*`, `ext_*`）| **SKIP** — 手动治理 |

**IRON-TABLE-02**: Mapping ≠ Migration。三种路径有效，其他均禁止。

---

### Step 6: Human Gate（必须获批才执行）

**AI 自动授权**（无需人工）：
- Target Schema Contract 比较（只读）
- Gap Analysis 生成
- Migration Type 分类
- Forward/Rollback/Validation SQL 生成
- Evidence Bundle 收集
- Dry-run rollback 测试

**人类必须授权**：
- 生产环境 Forward Migration 执行
- Type C（低代码）字段变更
- P0-Security 表破坏性变更
- DROP COLUMN / TRUNCATE TABLE
- 单批次 DDL > 1 表

---

### Step 7: Migration 执行

生成 Migration Bundle（4 文件）：

```
V<YYYYMMDD>_<change_id>.sql           # Forward
V<YYYYMMDD>_<change_id>_down.sql      # Rollback
V<YYYYMMDD>_<change_id>_verify.sql    # Validation
V<YYYYMMDD>_<change_id>_evidence.json # Evidence Bundle
```

**IRON-TABLE-06**: Migration 是一等公民，必须包含 4 文件。

---

### Step 8: Runtime Validation（7 层）

Schema 变更完成前必须验证 7 层运行时链：

```
Database → ORM (SqlSugar Entity) → Repository (IRepository<T>)
        → Dynamic SQL (codegen) → Form Engine → Workflow Engine
        → Permission Engine
```

**IRON-TABLE-07**: Runtime 兼容性优先。

---

### Step 9: Final Gap Scan + Close

关闭前必须确认：

```
G0 = 0
G1 = 0
Unknown = 0
Unexplained = 0
Migration-induced Regression = 0
```

更新 Final Matrix（单一事实源），写入 Evidence Store。

---

## Part 2: 10 条经验规则（来自 Final Closure Sprint 血泪教训）

**这些规则比任何项目报告都有价值。**

---

### Lesson 01: NO_CHANGE ≠ 没有发现问题

NO_CHANGE 是有效最终状态，但**必须用 8 维度证据证明**与 Target Contract 一致。
禁止："我扫了，没问题" → 必须有 sys.columns / sys.indexes 证据。

---

### Lesson 02: Mapping ≠ Migration

列名别名（如 `SELECT F_InputPerson AS F_ApplyUser`）**不等于** Schema Migration。
三种有效路径：Type A（sp_rename）、Type B（双写）、Type C（低代码手动治理）。
其他方式均是违规。

---

### Lesson 03: row < 100 ≠ 不需要 Index

表行数少不等于不需要 Index。
正确判断：生产 workload 查询证据 + 选择性可测量。
空表应 Deferred，等待生产数据。

---

### Lesson 04: Dynamic Table ≠ Ordinary Table

`wform_*` / `lowcode_*` / `ext_*` 是低代码平台动态表。
**禁止自动改名**（IRON-TABLE-08）。这些表由运行时元数据驱动，手动改会破坏平台。

---

### Lesson 05: Missing PK ≠ 自动增加 PK

不是所有缺失 PK 的表都需要立即加 PK。
判断依据：ORM 兼容性（SqlSugar Insertable/Updateable 要求）+ 业务语义（关联表需要复合 PK）+ Chief Architect 授权。
没有授权不得自动添加。

---

### Lesson 06: READY_TO_EXECUTE ≠ FIXED

文档状态 "READY_TO_EXECUTE" 不等于实际已执行。
必须区分：
- READY_TO_EXECUTE：SQL 准备好了，还没跑
- ACTUALLY_FIXED：实际在数据库执行了

混淆这两个状态会导致误判项目完成度。

---

### Lesson 07: DESIGNED ≠ VALIDATED ≠ EXECUTED

Rollback 状态三严格区分：
- DESIGNED：SQL 写好了
- VALIDATED：Live DB 执行了验证（不实际 rollback）
- EXECUTED：真的 rollback 了

不能把 DESIGNED 当成 EXECUTED。环境策略禁止实际 rollback 时，必须明确记录 VALIDATED 而非 EXECUTED。

---

### Lesson 08: Test Failure ≠ Regression

测试失败不等于迁移引起的回归。
判断步骤：
1. 定位失败测试（test name / class / message）
2. 归因分类：A. Migration-induced / B. Pre-existing / C. Environment / D. Harness / E. False Failure
3. 只有 A 类才是真正的回归

不能把 Pre-existing 失败当作迁移问题，也不能为了形式上的 100% 通过而跳过真实问题。

---

### Lesson 09: Report ≠ Evidence

文档结论不等于证据。
证据必须：
- 可独立验证（JSON/SQL 可重跑）
- 有时间戳
- 有执行环境标记
- 引用具体 sys.indexes / sys.columns 行

没有 Evidence 的 Report 价值为零。

---

### Lesson 10: Deferred ≠ Unfinished

Deferred 是正式最终状态，不是"未完成"。
必须满足：
- 有明确的 Deferred 原因（空表 / 数据质量 / 架构决策）
- 有明确的触发条件（下次什么时候重新评估）
- 在 Registry 中标记为 DEFERRED，不是 NO_CHANGE

Deferred 项目不是被遗忘，而是当前证据不足而主动延期。

---

## Part 3: IRON-TABLE 铁律（完整版）

| ID | 铁律 | 违规动作 |
|:---|:---|:---|
| IRON-TABLE-01 | NO_CHANGE 必须 8 维度证据 | Decision Brief + STOP |
| IRON-TABLE-02 | Mapping ≠ Migration，3 种有效路径 | Decision Brief + STOP |
| IRON-TABLE-03 | 每张表必须有 Target Contract | 禁止调用 Skill |
| IRON-TABLE-04 | 安全边界优先（P0 表审计）| Decision Brief + STOP |
| IRON-TABLE-05 | 性能声称必须有 Before/After | Decision Brief + STOP |
| IRON-TABLE-06 | Migration 必须 4 文件 Bundle | Decision Brief + STOP |
| IRON-TABLE-07 | Runtime 兼容性 7 层验证 | 禁止声称完成 |
| IRON-TABLE-08 | Dynamic Table 禁止自动改名 | Decision Brief + STOP |
| IRON-TABLE-09 | Evidence Over Declaration | 禁止无证据声称 |
| IRON-TABLE-10 | Batch 完成必须有代表性证明 | Decision Brief + STOP |

---

## Part 4: Evidence Store 引用规范

所有 Markdown 文档只引用 Evidence ID，不内嵌 JSON：

```
Evidence:
EV-SCHEMA-20260831-0021
```

实际证据在：
```
backend/database/evidence/schema/EV-SCHEMA-20260831-0021.json
```

---

## Part 5: AI 必读清单（新 Session）

1. `JNPF-Table-Refactoring-Registry.yaml` — Artifact Registry
2. `JNPF-Table-Refactoring-Current-State.md` — Current State Snapshot
3. `JNPF-Final-Refactoring-Matrix-vFinal.json` — Final Matrix（机器可读事实源）
4. `JNPF-Table-Refactoring-Playbook.md` — 本文件

**不需要读取**: `batch-29/` `batch-30/` `batch-31/` `phase-32/`（已 ARCHIVED）