# JNPF 后端数据库现代化 — 权威文档入口

> **最后更新**: 2026-08-31T19:45:00
> **状态**: CLOSED — FINAL ACCEPTANCE APPROVED
> **这是 AI 处理 JNPF 表级变更的唯一入口，不要从其他文档开始**

---

## 快速导航（强制顺序）

```
1. [JNPF-Table-Refactoring-Registry.yaml](./JNPF-Table-Refactoring-Registry.yaml)
   ↓  （了解所有权威文档的位置）
2. [JNPF-Table-Refactoring-Charter.md](./JNPF-Table-Refactoring-Charter.md)
   ↓  （了解项目范围：Why / What / Not What / Objectives / Done Criteria）
3. [JNPF-Table-Refactoring-Current-State.md](./JNPF-Table-Refactoring-Current-State.md)
   ↓  （了解当前状态：2 Fixed / 7 Deferred / 17 False Positive / G0=0 / G1=0）
4. [JNPF-Table-Refactoring-Playbook.md](./JNPF-Table-Refactoring-Playbook.md)
   ↓  （了解施工手册：9 步流程 + 10 经验规则 + IRON-TABLE 铁律）
5. [backend/database/final-refactor/JNPF-Final-Refactoring-Matrix-vFinal.json](../../backend/database/final-refactor/JNPF-Final-Refactoring-Matrix-vFinal.json)
   ↓  （机器可读事实源：每张表的当前状态、决策、证据）
6. [backend/database/evidence/](../../backend/database/evidence/)
   ↓  （证据存储：schema / migration / runtime / regression / rollback）
```

---

## 6 个权威文档

| # | 文档 | 作用 | 位置 |
|:---|:---|:---|:---|
| 1 | **Registry** | 权威文档索引 | `JNPF-Table-Refactoring-Registry.yaml` |
| 2 | **Charter** | 项目地图（给人看）| `JNPF-Table-Refactoring-Charter.md` |
| 3 | **Current State** | 状态快照（1-2 页）| `JNPF-Table-Refactoring-Current-State.md` |
| 4 | **Final Matrix** | 机器可读事实源 | `backend/database/final-refactor/JNPF-Final-Refactoring-Matrix-vFinal.json` |
| 5 | **Playbook** | AI 施工手册 | `JNPF-Table-Refactoring-Playbook.md` |
| 6 | **Final Acceptance** | 最终签字证书 | `backend/database/final-refactor/JNPF-Table-Refactoring-Final-Acceptance.md` |

---

## 工程铁律（核心原则）

```
Documents explain.        → Charter / Playbook / Current State
Matrix tells the truth.   → Final Matrix JSON (SSoT)
Evidence proves it.       → Evidence Store (JSON/SQL 可重跑)
ADR explains why.         → docs/adr/ADR-024/025/026
Skill explains how.       → .claude/skills/table-refactor-expert/SKILL.md
Archive preserves history.→ batch-29~31 / phase-32 (已归档)
```

---

## Evidence Store

证据只引用 ID，不内嵌：

```
backend/database/evidence/
├── schema/      EV-SCHEMA-*.json
├── migration/   EV-MIGRATION-*.json
├── runtime/     EV-RUNTIME-*.json
├── regression/  EV-TEST-*.json / EV-REGRESSION-*.json
└── rollback/    EV-ROLLBACK-*.json
```

---

## ADR Register（关键决策）

| ADR | 标题 |
|:---|:---|
| ADR-024 | Skill v2.0 是当前表级重构治理标准 |
| ADR-025 | base_signature_user 使用复合主键 |
| ADR-026 | Tenant Index 在本轮 Deferred |

---

## 历史归档

以下目录已归档，**不是**权威状态来源：

```
backend/database/batch-29/   ✅ ARCHIVED
backend/database/batch-30/   ✅ ARCHIVED
backend/database/batch-31/   ✅ ARCHIVED
backend/database/phase-32/   ✅ ARCHIVED
docs/universal/Phase-8/p8-c/batch-07~28/  ✅ ARCHIVED
```

---

## Skill v2.0 10 Iron Laws（摘要）

| ID | 规则 |
|:---|:---|
| IRON-TABLE-01 | NO_CHANGE 必须 8 维度证据 |
| IRON-TABLE-02 | Mapping ≠ Migration |
| IRON-TABLE-03 | 每张表必须有 Target Contract |
| IRON-TABLE-04 | 安全边界优先 |
| IRON-TABLE-05 | 性能声称必须有 Before/After |
| IRON-TABLE-06 | Migration 必须 4 文件 Bundle |
| IRON-TABLE-07 | Runtime 兼容性 7 层验证 |
| IRON-TABLE-08 | Dynamic Table 禁止自动改名 |
| IRON-TABLE-09 | Evidence Over Declaration |
| IRON-TABLE-10 | Batch 完成必须有代表性证明 |

---

## 10 条核心经验规则

| Lesson | 规则 |
|:---|:---|
| 01 | NO_CHANGE ≠ 没有发现问题 |
| 02 | Mapping ≠ Migration |
| 03 | row < 100 ≠ 不需要 Index |
| 04 | Dynamic Table ≠ Ordinary Table |
| 05 | Missing PK ≠ 自动增加 PK |
| 06 | READY_TO_EXECUTE ≠ FIXED |
| 07 | DESIGNED ≠ VALIDATED ≠ EXECUTED |
| 08 | Test Failure ≠ Regression |
| 09 | Report ≠ Evidence |
| 10 | Deferred ≠ Unfinished |

---

## 当前项目状态

```
JNPF BACKEND TABLE REFACTORING
= FINAL ACCEPTANCE APPROVED
= CLOSED
= 2026-08-31T19:20:00

M32-01: PK_base_signature ON f_id               ✅ ACTUALLY_FIXED
M32-02: PK_base_signature_user ON (f_id, f_uid) ✅ ACTUALLY_FIXED

Build: 0 errors
Tests: 728/729 (1 PRE_EXISTING)
```