# P8-0 — Table Unit Registry v1.0（Final）

> **Phase**: 8 — P8-0 Production Calibration
> **Status**: COMPLETE
> **Date**: 2026-08-30
> **Total Table Units**: 289
> **Source**: SQL Server `(local)\SQLEXPRESS` / `ZXAF_V1_DevTest1`

---

## 1. Executive Summary

| 指标 | 值 | 备注 |
|---|---|---|
| **Total User Tables** | **289** | 唯一候选 Table Unit |
| **Schemas** | 1 (dbo) | — |
| **显式 Entity 映射** | 164 | 有 `[SugarTable]` attribute |
| **无 Entity 映射** | 128 | 需代码级 / 动态访问 |
| **含 F_TENANT_ID** | 187 | 多租户隔离 |
| **含 F_DELETE_MARK 类列** | 150 | 软删除 |
| **Total FK** | 14 | 9 出 / 6 入 / 0 自引用 |

**结论**：289 = 289 Table Units，无重叠 / 拆分 / 合并需求。

---

## 2. Category Distribution

| Category | Count | Module 归属 | Entity Mapping |
|---|---|---|---|
| system-core | 85 | system | 85/85 ✅ |
| workflow-form-example | 51 | workflow | 0/51 ❌（动态）|
| system-warehouse-legacy | 39 | system (legacy) | 0/39 ❌ |
| inteAssistant-AI | 25 | inteAssistant | 25/25 ✅ |
| system-extension | 19 | system | 19/19 ✅ |
| workflow-engine | 18 | workflow | 18/18 ✅ |
| inteAssistant-SA-output | 13 | inteAssistant (SA) | 0/13 ❌（动态）|
| visualdata | 12 | visualdata | 5/12 ⚠️ |
| system-legacy-snowflake | 5 | system (legacy) | 0/5 ❌ |
| infrastructure | 5 | framework | 2/5 ⚠️ |
| inteAssistant-KG | 5 | inteAssistant (KG) | 3/5 ⚠️ |
| system-demo | 4 | system (demo) | 0/4 ❌ |
| inteAssistant-IR | 2 | inteAssistant (IR) | 2/2 ✅ |
| inteAssistant | 2 | inteAssistant | 2/2 ✅ |
| inteAssistant-studio | 2 | inteAssistant (studio) | 2/2 ✅ |
| inteAssistant-eval | 1 | inteAssistant (eval) | 1/1 ✅ |
| other | 1 | — | 0/1 ❌ |

**Entity 覆盖**：164/289 = 56.7% 显式映射
**动态访问**：125/289 = 43.3% 无显式映射（需运行时 SQL 探查）

---

## 3. Module Mapping（基于代码 namespace）

| JNPF Module | Table Count | Prefix(es) |
|---|---|---|
| system | 85 + 19 + 39 + 5 + 4 + 1 = 153 | base_*, BASE_*, ext_*, WH_*, WM_*, mt*, Demo_*, student, domain_model |
| workflow | 18 + 51 = 69 | flow_*, wform_* |
| inteAssistant | 25 + 13 + 5 + 2 + 2 + 2 + 1 = 50 | ai_*, BASE_AI_*, sa_*, BASE_KNOWLEDGE_*, kg_*, BASE_IR_*, BASE_STUDIO_MENU_*, inte_assistant_*, EVAL_* |
| visualdata | 12 | blade_visual_*, BASE_REPORT, report_*, data_report |
| framework | 5 | SYS_*, PROCESSED_EVENT, SchemaVersions, undo_log |

---

## 4. Dependency Graph（FK Relationships — 14 edges）

### 4.1 Incoming FK（被引用 — 6 表）

| Table | Incoming FK Count | Sources |
|---|---|---|
| sa_data_dictionary | 5 | sa_decision_table, sa_er, sa_pspec, sa_state_machine, sa_ui |
| sa_business_process | 4 | sa_dfd, sa_data_dictionary, sa_pspec, sa_state_machine, sa_ui |
| sa_dfd | 2 | sa_business_process, sa_data_dictionary |
| sa_pspec | 1 | sa_decision_table |
| sa_scope | 1 | sa_dfd |
| kg_pattern | 1 | kg_pattern_usage |

### 4.2 Outgoing FK（引用他人 — 9 表）

| Table | Outgoing FK Count | Targets |
|---|---|---|
| sa_data_dictionary | 2 | sa_business_process, sa_dfd |
| sa_decision_table | 2 | sa_data_dictionary, sa_pspec |
| sa_pspec | 2 | sa_business_process, sa_data_dictionary |
| sa_state_machine | 2 | sa_business_process, sa_data_dictionary |
| sa_ui | 2 | sa_business_process, sa_data_dictionary |
| kg_pattern_usage | 1 | kg_pattern |
| sa_business_process | 1 | sa_dfd |
| sa_dfd | 1 | sa_scope |
| sa_er | 1 | sa_data_dictionary |

### 4.3 Self-referencing FKs

**0** — 数据库中无自引用外键（这意味着 SKILL 不需要处理递归 FK 场景）

### 4.4 Observation

- **14 个 FK 全部在 inteAssistant 模块内**（sa_* 物化表之间 + kg_pattern/usage）
- **业务表（base_*, ext_*, flow_*, wform_*）几乎无 FK** — 这是 JNPF 架构特征
- 这意味着 Phase 8 重构时**数据库级 cascade 风险极低**
- 主要 refactor risk 来自 application 层（code-level FK）和 query pattern

---

## 5. Tenant / SoftDelete 分布

### 5.1 Tenant（187 / 289 = 64.7%）

| 类别 | 含 Tenant | 不含 Tenant |
|---|---|---|
| system-core | 80/85 | 5/85 |
| workflow-form-example | 51/51 | 0 |
| system-warehouse-legacy | 0/39 | 39/39 ⚠️ |
| inteAssistant-AI | 22/25 | 3/25 |
| system-extension | 19/19 | 0 |
| workflow-engine | 18/18 | 0 |
| inteAssistant-SA-output | 0/13 | 13/13 ⚠️ |
| visualdata | 0/12 | 12/12 ⚠️ |
| 其他 | 7/27 | 20/27 |

**关键发现**：
- WH_*/WM_* (39 张) **无 tenant 列** — 旧模块不参与多租户
- sa_* (13 张) **无 tenant 列** — SA 输出是 per-pipeline 数据
- visualdata (12 张) **无 tenant 列** — 设计器元数据全局共享

### 5.2 SoftDelete（150 / 289 = 51.9%）

类似分布，warehouse-legacy / sa_* / visualdata 通常也不使用 F_DELETE_MARK。

---

## 6. Row Count Distribution（活跃度）

| 范围 | 数量 | 占比 |
|---|---|---|
| 空表（0 rows） | ~80 | ~28% |
| 1-100 rows | ~100 | ~35% |
| 101-1000 rows | ~70 | ~24% |
| 1000-10000 rows | ~30 | ~10% |
| >10000 rows | 2 | <1% |
| 活跃业务表（>10 rows） | ~150 | ~52% |

**Top 5 最大表**：
1. base_province (47512 rows) — 行政区划数据
2. base_sys_log (12615 rows) — 系统日志
3. ai_ir_events (3780 rows) — IR 事件溯源
4. base_province_atlas (3210 rows) — 行政区划扩展
5. base_authorize (2553 rows) — 权限授权

---

## 7. State Distribution（初始 — P8-0 完成）

```
DISCOVERED:  289  (100%)
ASSESSED:    0
DESIGNED:    0
READY:       0
REFACTORED:  0
NO-CHANGE:   0
VERIFIED:    0
CLOSED:      0
```

---

## 8. Critical Tables Identified

### 8.1 Pilot-Covered Tables（Phase 6 已完成）

| Table | Module | Status |
|---|---|---|
| BASE_AI_PIPELINE | inteAssistant | Pilot 1 CLOSED |
| BASE_KNOWLEDGE_NODE | inteAssistant (KG) | Pilot 2 CLOSED |
| BASE_KNOWLEDGE_EDGE | inteAssistant (KG) | Pilot 2 CLOSED (indexes added) |
| FLOW_TASK | workflow | Pilot 3 READY (HG#5 pending) |

### 8.2 Backup / Special Tables

| Table | Special Status |
|---|---|
| BASE_STUDIO_MENU_BAK_20260617 | 备份表（2026-06-17）|
| mt543406707183714245 ~ mt543971603646513093 | Snowflake ID 命名 — 历史遗留 |

### 8.3 High-Risk Candidates

| Table | Risk Reason |
|---|---|
| base_user | 68 列, 核心表 |
| flow_task | 41 列, Pilot 3 已识别 R3+ |
| base_msg_account | 39 列, 第三方账号中心 |
| base_api_log | 38 列, 高频写入 |
| ext_product | 38 列, 业务核心 |
| sa_data_dictionary | 5 incoming FKs, 最高被引用 |
| sa_business_process | 4 incoming FKs, SA 中心 |

---

## 9. Batch 初始建议（基于 dependency + business coherence）

按 Master Plan §2.5 (Batch Registry) 规则初步分组：

| Batch | Group | Tables (count) | Reason |
|---|---|---|---|
| **Batch 01** | system-core identity | base_user, base_role, base_position, base_organize, base_user_relation (5) | 身份/组织基础，强耦合 |
| **Batch 02** | system-core permission | base_authorize, base_module, base_module_button, base_module_column, base_module_form (5) | 权限元数据 |
| **Batch 03** | system-core dictionary | base_dictionary_type, base_dictionary_data, base_bill_rule, base_common_fields, base_common_words (5) | 字典与编码规则 |
| **Batch 04** | system-core config | base_sys_config, base_sys_log, base_api_log, base_sign_img, base_syn_third_info (5) | 系统配置与日志 |
| **Batch 05** | system-core province | base_province, base_province_atlas, base_data_interface, base_data_interface_log, base_data_interface_oauth (5) | 行政区划与数据接口 |
| ... | ... | ... | （其余 batches 在 P8-B 时根据 P8-A 选定的 5 张表进一步组织）|

**注意**：完整 Batch 详细分组待 P8-A 选定 5 张表后，按 dependency 顺序确定。

---

## 10. 引用

- 物理 inventory: `inventory-raw.txt`
- 视图与类型: `views-and-types-raw.txt`
- Row counts: `row-counts-raw.txt`
- FK: `foreign-keys-raw.txt`
- Column metadata: `table-metadata-raw.txt`
- Entity 映射原始: `entity-mapping-raw.csv`
- 完整 registry (CSV): `table-unit-registry-full.csv`
- 无 Entity 映射表清单: `unmapped-tables.txt`
