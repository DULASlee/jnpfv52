# P8-A — Shadow Table Selection v1.0

> **Phase**: 8 — P8-A
> **Status**: SELECTED
> **Date**: 2026-08-30
> **Method**: Selection Matrix × Real Candidate Pool (289 tables)

---

## 1. Selection Method

按照 `Phase-8-Shadow-Mode-Table-Selection.md` §3-4：
- 来源：P8-0 Table Unit Registry (289 tables)
- 排除 Pilot 1-3 已覆盖表 (4): BASE_AI_PIPELINE / BASE_KNOWLEDGE_NODE / BASE_KNOWLEDGE_EDGE / FLOW_TASK
- 应用 Selection Matrix (9 维度评分)
- 目标自然风险分布：R0/R1 + R2 + R3+

---

## 2. Selection Matrix — Candidate Evaluation

从 285 候选表（289 - 4 Pilot）中，按 9 维度矩阵评估关键候选。

### 2.1 高优先级候选评分

| Table | Schema | Integrity | Index/Query | Lifecycle | CRUD | DDD | Target | Legacy | Dep | Risk (est) |
|---|---|---|---|---|---|---|---|---|---|---|
| base_sys_config | 2 | 1 | 1 | 1 | 1 | 5 | 5 | 1 | 2 | **R0/R1** |
| base_bill_rule | 2 | 1 | 1 | 1 | 1 | 5 | 5 | 1 | 3 | **R0/R1** |
| base_user | 5 | 4 | 4 | 3 | 4 | 4 | 4 | 4 | 5 | **R2** |
| base_role | 3 | 3 | 3 | 2 | 3 | 4 | 5 | 2 | 4 | **R1/R2** |
| base_organize | 3 | 3 | 2 | 2 | 3 | 4 | 5 | 2 | 4 | **R1/R2** |
| base_visual_dev | 4 | 3 | 4 | 3 | 3 | 3 | 3 | 4 | 3 | **R2** |
| ext_table_example | 3 | 2 | 2 | 2 | 2 | 4 | 4 | 3 | 2 | **R2** |
| ext_product | 5 | 4 | 3 | 3 | 4 | 3 | 3 | 4 | 4 | **R2/R3** |
| sa_data_dictionary | 3 | 5 | 3 | 2 | 3 | 2 | 2 | 3 | 5 | **R3+** |
| sa_business_process | 3 | 5 | 3 | 3 | 3 | 2 | 2 | 3 | 4 | **R3+** |
| base_authorize | 4 | 4 | 4 | 3 | 4 | 3 | 3 | 4 | 4 | **R2/R3** |
| base_module | 4 | 3 | 3 | 3 | 3 | 4 | 4 | 3 | 4 | **R2** |
| flow_template | 4 | 3 | 3 | 3 | 3 | 4 | 4 | 3 | 3 | **R2** |
| flow_task_operator | 4 | 3 | 3 | 3 | 4 | 3 | 3 | 3 | 3 | **R2** |
| wform_leaveapply | 4 | 2 | 3 | 3 | 3 | 3 | 2 | 4 | 2 | **R2** |
| wform_salesorder | 5 | 3 | 4 | 3 | 4 | 3 | 2 | 5 | 3 | **R3** |

评分说明：1（简单/低）→ 5（复杂/高）

### 2.2 关键决策因子

**必须满足**：
1. 至少 1 张 R0/R1
2. 至少 1 张 R2
3. 至少 1 张 R3+
4. 跨 module 覆盖（system / workflow / visualdata / inteAssistant）
5. Entity mapping 混合（有 + 无）
6. 不与 Pilot 1-3 重叠

---

## 3. 5 张 Shadow Tables 选定

| # | Table | Module | Entity | Risk | Why Selected |
|---|---|---|---|---|---|
| 1 | **base_sys_config** | system | YES (SysConfigEntity) | **R0/R1** | 简单 config 表；Dry-run 已验证 state machine；最 baseline |
| 2 | **base_user** | system | YES (UserEntity) | **R2** | 68 列最多；identity 核心；典型 R2 中风险 |
| 3 | **base_visual_dev** | visualdata | YES (VisualDevEntity) | **R2** | visualdev 元数据；中等复杂度；覆盖 visualdata module |
| 4 | **ext_table_example** | system-extension | YES (TableExampleEntity) | **R2** | Extension 示例；典型 R2 业务表 |
| 5 | **sa_data_dictionary** | inteAssistant-SA | NO (dynamic) | **R3+** | 5 incoming FKs (最高)；无 Entity；典型 R3+ 无 entity 案例 |

### 3.1 风险分布验证

| Risk | Tables | Count |
|---|---|---|
| R0/R1 | base_sys_config | 1 |
| R2 | base_user / base_visual_dev / ext_table_example | 3 |
| R3+ | sa_data_dictionary | 1 |
| **Total** | — | **5** |

满足 Master Plan §6 "Shadow 5 张表自然形成 R0/R1 + R2 + R3+" 要求。

### 3.2 Module 覆盖验证

| Module | Table |
|---|---|
| system | base_sys_config / base_user / ext_table_example |
| visualdata | base_visual_dev |
| inteAssistant (SA) | sa_data_dictionary |

满足 Master Plan §6 "跨 module 覆盖" 要求。

### 3.3 Entity Mapping 混合验证

| Status | Tables |
|---|---|
| YES (Entity) | base_sys_config / base_user / base_visual_dev / ext_table_example |
| NO (dynamic) | sa_data_dictionary |

满足 Master Plan §6 "entity mapped 混合" 要求（4 mapped + 1 dynamic）。

---

## 4. Selected Tables 详情

### 4.1 base_sys_config

- **Risk**: R0/R1
- **Schema**: 14 cols, simple
- **PK**: F_ID
- **Tenant**: YES (F_TENANT_ID)
- **SoftDelete**: YES (F_DELETE_MARK)
- **Entity**: `backend/modularity/system/JNPF.Systems.Entitys/Entity/System/SysConfigEntity.cs`
- **Module**: system
- **Row count**: 74
- **Dry-run**: ✓ Already validated in P8-0

### 4.2 base_user

- **Risk**: R2
- **Schema**: 68 cols (highest in system)
- **PK**: F_ID
- **Tenant**: YES
- **SoftDelete**: YES
- **Entity**: `backend/modularity/system/JNPF.Systems.Entitys/Entity/Permission/UserEntity.cs` (TenantCLDSEntityBase)
- **Module**: system
- **Row count**: 45
- **Critical**: identity core, extensively referenced in code

### 4.3 base_visual_dev

- **Risk**: R2
- **Schema**: 30 cols
- **PK**: F_ID
- **Tenant**: YES
- **SoftDelete**: YES
- **Entity**: `backend/modularity/visualdev/JNPF.VisualDev.Entitys/Entity/VisualDevEntity.cs`
- **Module**: visualdata
- **Row count**: 48
- **Critical**: visualdev metadata, low-code designer data

### 4.4 ext_table_example

- **Risk**: R2
- **Schema**: 28 cols
- **PK**: F_ID
- **Tenant**: YES
- **SoftDelete**: YES
- **Entity**: `backend/modularity/extend/JNPF.Extend.Entitys/Entity/TableExampleEntity.cs`
- **Module**: system (extension)
- **Row count**: 28
- **Critical**: extension example table, demonstrates JNPF extension pattern

### 4.5 sa_data_dictionary

- **Risk**: R3+
- **Schema**: 35 cols
- **PK**: id
- **Tenant**: NO (SA output, per-pipeline data)
- **SoftDelete**: NO (SA output)
- **Entity**: NONE (dynamically queried)
- **Module**: inteAssistant (SA output)
- **Row count**: 35
- **Critical**: 5 incoming FKs (highest in DB) — most referenced SA table
- **Special**: No Tenant + No SoftDelete is the SA output pattern (anomaly to check)

---

## 5. Pilot 排除验证

| Pilot | Table | Reused in P8-A? |
|---|---|---|
| Pilot 1 | BASE_AI_PIPELINE | NO ✓ |
| Pilot 2 | BASE_KNOWLEDGE_NODE | NO ✓ |
| Pilot 2 | BASE_KNOWLEDGE_EDGE | NO ✓ |
| Pilot 3 | FLOW_TASK | NO ✓ |

无 Pilot 重叠。

---

## 6. Selection Summary

| # | Table | Risk | Module | Entity | Special |
|---|---|---|---|---|---|
| 1 | base_sys_config | R0/R1 | system | YES | Dry-run validated |
| 2 | base_user | R2 | system | YES | Most columns |
| 3 | base_visual_dev | R2 | visualdata | YES | visualdev metadata |
| 4 | ext_table_example | R2 | system-ext | YES | Extension pattern |
| 5 | sa_data_dictionary | R3+ | inteAssistant-SA | NO | Most FKs, no entity |

**Selected for P8-A Shadow Production**: 5 tables

---

## 7. Next Action

进入 P8-A.2：AI Track A Execution
- 对 5 张表分别执行 Skill 评估
- 产出 Track A 文档（findings / risk / evidence / design / recommended action / hard gate / closure status）
- 不写库
