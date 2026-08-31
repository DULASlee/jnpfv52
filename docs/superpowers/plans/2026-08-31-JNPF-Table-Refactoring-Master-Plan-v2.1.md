# JNPF 后端表级重构

# Autonomous Implementation Master Plan v2.1

**状态：ACTIVE PLAN / 当前处于 STOP**
**当前 Gate：Batch 30+ Gap Review Gate**
**禁止事项：在 Gap Review Gate 结束前，不得执行任何 Schema、ORM、Entity 修改。**

---

# 0. 总体执行目标

本计划唯一目标：

> 将 JNPF 当前"治理已建立、Schema 尚未彻底修复"的状态，推进到真正的 **Table Schema Refactoring CLOSED**。

最终必须证明：

```text
Target Schema Contract
        ↓
Current Schema Evidence
        ↓
Gap Analysis
        ↓
Migration Decision
        ↓
Human Gate
        ↓
Executable Migration
        ↓
Runtime Compatibility
        ↓
Performance / Regression
        ↓
Evidence
        ↓
Gap Closure
        ↓
Final Acceptance
```

禁止把以下内容视为"完成"：

```text
登记完成
报告完成
索引完成
Mapping 完成
NO_CHANGE 写入完成
```

只有实际 Gap 被证明满足 Target Contract，或者被正式批准延期/排除，才可以 Closure。

---

# 1. 全局执行铁律

## IRON-TABLE-01

### No Change ≠ No Action

`NO_CHANGE` 必须有证据证明：

> 当前 Schema 已符合 Target Schema Contract。

不能使用：

> "暂时不改" = NO_CHANGE。

如果存在未解决 G1/G0 Gap：

必须使用：

```text
MIGRATION_REQUIRED
DEFERRED
EXCLUDED
BLOCKED
```

而不能伪装成 `NO_CHANGE`。

---

## IRON-TABLE-02

### Mapping Is Not Migration

任何：

```text
Alias
Mapping
DTO Adapter
ORM Property Mapping
Runtime Translation
```

都不能计为 Schema Migration。

如果 Target Contract 要求真实列名变化：

必须存在：

```text
ALTER / Data Migration / Compatibility Migration
+
Validation Evidence
```

---

## IRON-TABLE-03

### Every Table Needs Target Contract

每一张进入处理范围的表必须有：

```text
Project
Module
Table
Columns
Data Types
Nullability
Primary Key
Indexes
Tenant Semantics
Audit Semantics
Classification
Dynamic-Platform Status
```

没有 Target Contract：

**不得进入 Migration。**

---

## IRON-TABLE-04

### Security Boundary First

以下对象优先级最高：

```text
User
Tenant
Permission
Identity
Authentication
Authorization
Security-sensitive data
```

安全风险不得因为"表级重构范围有限"而被隐藏。

---

## IRON-TABLE-05

### Performance Claim Requires Measurement

禁止使用：

```text
Added Index
=
Performance Improved
```

任何"性能提升"必须有：

```text
Before
After
Workload
Execution Plan / Metrics
```

---

## IRON-TABLE-06

### Migration First-Class

真正 Schema Change 必须交付：

```text
Migration Script
Validation Script
Rollback Strategy
Execution Evidence
```

缺一不可。

---

## IRON-TABLE-07

### Runtime Compatibility First

任何 Schema Change 都必须检查：

```text
SQL
↓
SqlSugar / Dapper
↓
Repository / Service
↓
Dynamic SQL
↓
Low-code runtime
↓
Workflow
↓
Permission
```

---

## IRON-TABLE-08

### Dynamic Platform Exception

以下对象不得套用普通表迁移规则：

```text
wform_*
ext_*
动态表单
动态字段
动态实体
运行时生成对象
元数据驱动对象
```

必须先判断：

```text
STATIC
DYNAMIC
HYBRID
```

Dynamic 对象默认进入 Human Gate。

---

## IRON-TABLE-09

### Evidence Over Declaration

所有 PASS / CLOSED / NO_CHANGE / ACCEPT：

必须有 Evidence。

---

## IRON-TABLE-10

### Batch Completion Requires Representative Proof

Batch Closure 必须覆盖：

```text
Normal Table
Complex Table
Dynamic / Risky Table
```

不得仅凭数量和台账关闭 Batch。

---

# 2. 当前状态：严格 STOP

当前：

```text
Skill v2.0      VALIDATED (Pilot)
Skill v2.0      NOT YET FROZEN

Batch 29        ACCEPTED

Known Gaps      17 G1_MAJOR
                NOT FIXED

Production DDL  0

ORM Changes     0
Entity Changes  0

Phase 2         BLOCKED

ADR-024         ACCEPT_PENDING
```

因此：

## 当前唯一允许动作

**Gap Review / Analysis / Contract / Risk / Plan**

## 当前禁止动作

```text
ALTER TABLE
CREATE INDEX
DROP
Constraint Change
Column Change
ORM Mapping Change
Entity Change
Production Data Migration
```

---

# 3. Phase 30：Batch 30+ Gap Review Gate

## 目标

逐项审查当前 4 类 Gap：

```text
GAP-01 base_signature Missing PK
GAP-02 base_signature_user Missing PK
GAP-03 15 tables Missing Tenant Index
GAP-04 5 tables Missing Audit Fields
```

当前不执行修复。

---

# Task 30.1 — 建立 Gap Inventory

输入：

```text
batch-29-gap-analysis.json
batch-29-decisions.json
Target Schema Contract
Production Evidence
```

输出：

```text
batch-30-gap-inventory.json
```

每个 Gap 必须包含：

```text
Gap ID
Project
Module
Table
Dimension
Current State
Target State
Severity
Evidence
Dynamic Status
```

验收：

```text
17/17 G1_MAJOR 可追踪
0 orphan gap
0 duplicate gap
0 unsupported gap
```

---

# Task 30.2 — Gap 真实性复核

每一个 Gap 必须重新确认：

```text
Current Schema
VS
Target Schema
```

不得直接相信历史报告。

必须重新查询数据库元数据。

验收：

```text
17/17 Evidence-backed
```

---

# Task 30.3 — PK Gap 专项审查

对象：

```text
base_signature
base_signature_user
```

必须调查：

```text
是否真的无 PK
是否存在唯一候选键
是否存在重复数据
是否存在 NULL
是否存在外部引用
ORM 如何访问
SqlSugar 如何映射
Dapper 是否使用
是否属于动态表
```

必须生成：

```text
PK Candidate Analysis
Referential Dependency Analysis
Runtime Impact Analysis
```

禁止直接生成：

```sql
ALTER TABLE ... ADD PRIMARY KEY
```

---

# Task 30.4 — Tenant Index Gap 专项审查

对 15 张表逐表检查：

```text
tenant column
tenant nullability
tenant access pattern
existing indexes
composite indexes
common query predicates
unique constraints
cross-tenant query risk
```

特别注意：

> "缺 tenant index"不等于"必须单独增加 tenant index"。

必须根据真实查询模式决定：

```text
ADD_INDEX
REBUILD_EXISTING_INDEX
NO_CHANGE
DEFERRED
```

---

# Task 30.5 — Audit Field Gap 专项审查

5 张表逐表确定：

```text
CreateTime
CreatorUserId
LastModifyTime
LastModifyUserId
DeleteMark
DeleteTime
DeleteUserId
```

必须先确定：

```text
哪些字段是 Target Contract 强制要求
哪些只是历史习惯
哪些字段已经由现有模型表达
```

禁止因为"行业最佳实践"自行增加字段。

---

# Task 30.6 — Dynamic Classification

所有 17 Gap 必须重新确认：

```text
STATIC
DYNAMIC
HYBRID
```

对于：

```text
DYNAMIC / HYBRID
```

必须自动进入：

```text
Human Gate = REQUIRED
```

---

# Task 30.7 — Migration Decision Matrix

每个 Gap 必须得到唯一结论：

```text
MIGRATION_REQUIRED
NO_CHANGE
DEFERRED
EXCLUDED
BLOCKED
```

并记录：

```text
Why
Evidence
Risk
Owner
Prerequisite
```

禁止出现：

```text
TODO
TBD
Later
To be evaluated
```

等无操作性结果。

---

# Gate 30 — Gap Review Acceptance

只有满足：

```text
17/17 Gap reviewed
17/17 有 evidence
17/17 有 Target Contract
17/17 有 Risk
17/17 有 Migration Type
17/17 有 Runtime Impact
17/17 有 Rollback Strategy
```

才能离开 Gap Review。

否则：

```text
STOP
```

---

# 4. Phase 31：Migration Specification

仅对：

```text
MIGRATION_REQUIRED
```

项目进入 Phase 31。

每个 Migration 必须独立形成：

```text
Migration Spec
Migration SQL
Rollback
Validation SQL
Runtime Validation
```

---

# Task 31.1 — Migration Contract

每项变更必须明确：

```text
Current Schema
Target Schema
Exact SQL Change
Data Transformation
Precondition
Postcondition
Rollback
Failure Behavior
```

---

# Task 31.2 — Dependency Analysis

必须扫描：

```text
Table
View
Stored Procedure
Trigger
SqlSugar
Dapper SQL
Repository
Service
DTO
Entity
Dynamic Query
Workflow
Permission
```

输出：

```text
migration-dependencies.json
```

---

# Task 31.3 — Data Safety Analysis

涉及数据变化时必须检查：

```text
row count
null count
duplicate count
out-of-range values
orphan records
conversion loss
```

没有 Data Safety Evidence：

**不得 Migration。**

---

# Task 31.4 — Rollback Design

每一个 Migration：

必须回答：

```text
失败怎么办？
部分成功怎么办？
如何恢复？
数据是否可逆？
结构是否可逆？
```

不可逆变化：

必须升级 Human Gate。

---

# Gate 31

没有：

```text
Migration Spec
+
Rollback
+
Validation
+
Runtime Impact
```

不得执行 DDL。

---

# 5. Phase 32：Human Gate

需要人工批准的情况：

```text
Dynamic table
Security boundary
Destructive migration
Data transformation
PK change
Tenant semantics change
Runtime compatibility uncertainty
Irreversible migration
```

审批记录必须包含：

```text
Reviewer
Role
Scope
Decision
Timestamp
Approved Migration ID
Risk acknowledgement
```

禁止：

```text
--human-approved=true
```

作为唯一授权依据。

---

# Gate 32

Human Gate 未完成：

```text
NO DDL
NO ORM
NO Entity
```

---

# 6. Phase 33：Migration Execution

只有 Gate 32 PASS 后才能进入。

执行顺序：

```text
Backup / Snapshot
↓
Pre-flight Validation
↓
Migration
↓
Post-flight Validation
↓
Runtime Test
↓
Regression
```

---

# Task 33.1 — Pre-flight

必须保存：

```text
Schema Snapshot
Table Row Count
Column Metadata
Index Metadata
Constraint Metadata
Dependency Metadata
```

---

# Task 33.2 — Execute Migration

只允许执行：

> 已审批 Migration ID 对应的 SQL。

禁止工程师临时手改 SQL。

临时变化：

必须：

```text
STOP
Change Record
Re-approval
```

---

# Task 33.3 — Post Migration Validation

至少验证：

```text
Schema = Target Contract
Data Integrity
Constraint Integrity
Index Integrity
Tenant Integrity
Audit Integrity
```

---

# 7. Phase 34：Runtime Validation

必须实际运行相关后端测试。

至少覆盖：

```text
CRUD
Query
JOIN
Pagination
Tenant filtering
Workflow
Permission
Dynamic form
Existing APIs
```

任何 Runtime Regression：

```text
Migration = NOT ACCEPTED
```

---

# 8. Phase 35：Performance Validation

只有真正影响查询路径的变化才必须测量。

至少记录：

```text
Before Duration
After Duration

Before CPU
After CPU

Before Logical Reads
After Logical Reads

Before Execution Plan
After Execution Plan
```

不要为了"满足指标"人工制造性能测试。

---

# 9. Phase 36：Batch Closure

Batch 只有在：

```text
All Approved Migrations PASS
All Runtime Tests PASS
All Data Validation PASS
All Performance Claims Evidence-backed
All Evidence Stored
All Gap Status Updated
```

才能关闭。

Batch Closure Report 必须明确：

```text
Fixed
No Change
Deferred
Excluded
Blocked
```

这五类不能混为一谈。

---

# 10. Phase 37：全局 Gap Closure

重新扫描整个 JNPF Schema。

不是：

```text
只检查本 Batch
```

而是：

```text
Current Schema
VS
Target Contract
```

再次生成：

```text
final-gap-analysis.json
```

目标：

```text
G0 Critical = 0
Approved G1 = 0
Unauthorized Change = 0
Unexplained Gap = 0
```

允许存在：

```text
Deferred
Excluded
```

但每一项必须存在正式决策。

---

# 11. Phase 38：Final Acceptance

最终验收必须证明：

### Schema

```text
Target Contract satisfied
```

### Runtime

```text
No regression
```

### Security

```text
Security boundary validated
```

### Performance

```text
Claims evidence-backed
```

### Governance

```text
Every change auditable
```

### Documentation

```text
Single Source of Truth
```

---

# 12. 最终关闭条件

只有满足全部条件：

```text
Skill v2.0                    FROZEN
ADR-024                        ACCEPTED

All approved Schema Gaps      CLOSED
All Migration Evidence        COMPLETE
Runtime Regression             PASS
Performance Validation         PASS where applicable
Security Validation            PASS
Batch Records                  COMPLETE
No unexplained gaps             PASS
No unauthorized changes        PASS
```

最终才能：

```text
JNPF Table Refactoring
        = CLOSED
```

---

# 13. AI 工程师执行协议

以后每个 Task 必须按照：

```text
1. Read Contract
2. Inspect Current State
3. Collect Evidence
4. Analyze Gap
5. Decide
6. Validate Decision
7. Implement only if authorized
8. Test
9. Self-review
10. Produce Evidence
11. Update Status
```

禁止：

```text
发现问题
↓
立即修改
```

必须：

```text
发现问题
↓
证据
↓
Target
↓
Risk
↓
Decision
↓
Gate
↓
Migration
```

---

# 14. 汇报格式

每个 Task 完成后必须报告：

```text
TASK:
STATUS:

INPUT:
OUTPUT:

CURRENT:
TARGET:

GAP:
DECISION:

CHANGES:
  Schema:
  ORM:
  Entity:
  Data:

VALIDATION:

EVIDENCE:

RISKS:

ROLLBACK:

NEXT:
```

禁止只报告：

```text
✅ Done
✅ PASS
✅ Refactored
```

---

# 15. 当前唯一允许启动的 Task Bundle

## BATCH 30+ GAP REVIEW BUNDLE

允许：

```text
30.1 Gap Inventory
30.2 Gap Re-validation
30.3 PK Analysis
30.4 Tenant Index Analysis
30.5 Audit Field Analysis
30.6 Dynamic Classification
30.7 Migration Decision Matrix
```

完成后：

> **STOP → Human Gate / Batch 30+ Gap Review Acceptance**

没有明确批准：

```text
不得进入 Phase 31
不得生成可执行生产 DDL
不得修改 ORM
不得修改 Entity
```

---

# 16. 当前项目终点定义

不要把：

```text
Batch 29
```

当成最终完成。

也不要把：

```text
17 G1 gaps
```

全部默认修复。

真正目标是：

> **每一个 Gap 都得到有证据的最终状态。**

最终允许的结果只有：

```text
FIXED
NO_CHANGE (PROVEN)
DEFERRED (APPROVED)
EXCLUDED (APPROVED)
```

不存在：

```text
"报告里写过，所以算完成"
```

---

## 当前执行指令

**AI 工程师立即保持 STOP。**

在下一人工节点获得授权后：

> **仅执行 `BATCH 30+ GAP REVIEW BUNDLE`，完整完成 30.1 → 30.7。**

**不得执行任何 Schema Migration。**

Batch 30+ Gap Review 完成后再次进入 Gate，由 Chief Architect 审核每类 Gap 的最终处理方向。

这版相比前面的计划，最重要的改变是把 **"Gap → 修复"之间强制插入一个完整的 Decision Gate**，并且把 `NO_CHANGE / DEFERRED / EXCLUDED / BLOCKED / MIGRATION_REQUIRED` 五种状态彻底分开。这样后面即使发现 17 个 G1_MAJOR 中只有少数真正值得改，也不会再出现"没有改，所以当作完成"的问题。
