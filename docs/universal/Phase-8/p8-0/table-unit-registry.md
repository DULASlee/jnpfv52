# P8-0 — Table Unit Registry v1.0

> **Phase**: 8 — P8-0 Production Calibration
> **Status**: IN PROGRESS
> **Source**: SQL Server `(local)\SQLEXPRESS` / `ZXAF_V1_DevTest1`
> **Date**: 2026-08-30
> **Extraction Script**: `docs/universal/Phase-8/p8-0/00-inventory-extract.sql`

---

## 1. 真实 Inventory 数字

| 指标 | 值 | 来源 |
|---|---|---|
| **Total User Tables** | **289** | sys.tables where is_ms_shipped=0 |
| Schemas | 1 (dbo) | sys.schemas |
| Views (excluded) | 7 | sys.views |
| User-defined Table Types (excluded) | 1 (sa_entity_fields) | sys.types |
| **Candidate Table Units** | **289** | All USER_TABLE |
| Tables with F_TENANT_ID | 187 | sys.columns |
| Tables with F_DELETE_MARK / F_DELETEMARK / DELETE_MARK | 150 | sys.columns |
| Self-referencing FKs | 0 | sys.foreign_keys |
| Total FK relationships | 14 | sys.foreign_keys |
| Tables with outgoing FK | 9 | sys.foreign_keys |
| Tables with incoming FK | 6 | sys.foreign_keys |

**重要结论**：
- **289 张 physical tables = 289 candidate Table Units**（无系统表 / 临时表 / 视图）
- 唯一 view 是 7 张 `v_*` 已排除
- 唯一 user-defined table type 是 `sa_entity_fields`（不是物理表）
- 7 张 v_ 开头 view（KG 模块）已确认排除

---

## 2. Table Unit Categorization by Prefix（真实分类）

按表名前缀分类（基于 inventory 数据）。每个 prefix 对应 JNPF 的一个业务模块或子模块。

### 2.1 `base_*` / `BASE_*` 系列 — 系统核心（107 张）

包括用户、组织、角色、权限、字典、消息、菜单、模块、消息中心等核心系统表。

**JNPF 模块归属**：`system` module

详细清单见 `table-unit-registry-detail.md`。

### 2.2 `flow_*` — 工作流（17 张）

JNPF Flowable 工作流引擎表。

### 2.3 `wform_*` — 流程表单示例（53 张）

`wform_apply*`, `wform_contract*`, `wform_document*`, `wform_expense*`, `wform_finished*`, `wform_leaveapply`, `wform_material*`, `wform_monthly*`, `wform_officesupplies`, `wform_outbound*`, `wform_outgoing*`, `wform_pay*`, `wform_payment*`, `wform_post*`, `wform_procurement*`, `wform_purchase*`, `wform_quotation*`, `wform_receipt*`, `wform_reward*`, `wform_sales*`, `wform_staff*`, `wform_supplement*`, `wform_travel*`, `wform_vehicle*`, `wform_violation*`, `wform_warehouse*`, `wform_work*`, `wform_zjf_wikxqi`

### 2.4 `ext_*` — 业务扩展示例（22 张）

`ext_big_data`, `ext_customer`, `ext_document`, `ext_email_*`, `ext_employee`, `ext_order*`, `ext_product*`, `ext_project_gantt`, `ext_table_example`, `ext_work_log*`

### 2.5 `blade_visual_*` — 可视化大屏（10 张）

`blade_visual`, `blade_visual_category`, `blade_visual_component`, `blade_visual_config`, `blade_visual_db`, `blade_visual_glob`, `blade_visual_map`, `blade_visual_record`

### 2.6 `ai_*` / `BASE_AI_*` / `EVAL_*` — AI / Studio 模块（25 张）

- `ai_entity_field`, `ai_ir_events`, `ai_ir_fragment_snapshots`, `ai_projects`, `ai_route_table`, `ai_seed_templates`, `ai_skill_llm_policy`, `ai_skill_runs`
- `BASE_AI_AGENT_*`, `BASE_AI_CALL_LOG`, `BASE_AI_EVAL_*`, `BASE_AI_GENERATED_PROJECT`, `BASE_AI_MCP_CONFIG`, `BASE_AI_MODEL_*`, `BASE_AI_PIPELINE_*`, `BASE_AI_PROMPT_TEMPLATE`, `BASE_AI_SKILL_REVIEW`, `BASE_AI_UI_TEMPLATE`
- `EVAL_METRIC`
- `inte_assistant_attachment`, `inte_assistant_deliverable`

### 2.7 `sa_*` — Studio Architecture 输出（14 张）

SA (Studio Architecture) 物化表：`sa_scope`, `sa_dfd`, `sa_business_process`, `sa_data_dictionary`, `sa_pspec`, `sa_decision_table`, `sa_state_machine`, `sa_ui`, `sa_er`, `sa_validation_log`, `sa_assumptions`, `sa_consistency`, `sa_quality_score`

### 2.8 `BASE_KNOWLEDGE_*` / `kg_*` — Knowledge Graph（4 张）

- `BASE_KNOWLEDGE_NODE`, `BASE_KNOWLEDGE_EDGE`, `BASE_KNOWLEDGE_RULE`
- `kg_pattern`, `kg_pattern_usage`

### 2.9 `BASE_IR_*` / `BASE_STUDIO_MENU` / `BASE_REPORT` — IR & Studio Menus（4 张）

- `BASE_IR_VERSION`, `BASE_IR_EDIT_PATCH`
- `BASE_STUDIO_MENU`, `BASE_STUDIO_MENU_BAK_20260617`

### 2.10 `data_report` / `report_*` — 报表（4 张）

`data_report`, `report_charts`, `report_department`, `report_user`

### 2.11 `WH_*` / `WM_*` — 仓库管理（27 张）

WH_* 系列（15 张）+ WM_* 系列（12 张）

### 2.12 `Demo_*` / `student` / `mt_*` — 演示 / 历史遗留（8 张）

`Demo_ExcelTest`, `Demo_Order`, `Demo_OrderDetail`, `student`, `mt543406707183714245`, `mt543408365615710149`, `mt543552698159464389`, `mt543668771097673669`, `mt543971603646513093`

### 2.13 `WH_BillAutoID` / `WM_BillAutoID` / `SchemaVersions` / `Sys_Processed_Event` 等系统基础设施（剩余）

包括：
- `SchemaVersions`
- `SYS_EVENT_OUTBOX_MESSAGE`, `SYS_PROCESSED_EVENT`
- `PROCESSED_EVENT`, `undo_log`
- `zx_sys_config`, `zx_sys_db`, `zx_system_db`
- `BASE_FOUNDER_AUTH_LOG`, `BASE_SANDBOX`
- `BASE_TENANT_GLOSSARY`, `BASE_TENANT_INDUSTRY`
- `BASE_MENU_BADGE`
- `BASE_REPORT`

---

## 3. Module 映射（基于代码与命名约定）

| Prefix | Module | Table Count (approx) |
|---|---|---|
| base_* / BASE_* | system | 107 |
| flow_* | workflow | 17 |
| wform_* | workflow (form examples) | 53 |
| ext_* | system (extension examples) | 22 |
| blade_visual_* | visualdata | 10 |
| ai_* / BASE_AI_* / EVAL_* / inte_assistant_* | inteAssistant | 25 |
| sa_* | inteAssistant (SA output) | 14 |
| BASE_KNOWLEDGE_* / kg_* | inteAssistant (KG) | 4 |
| BASE_IR_* / BASE_STUDIO_MENU | inteAssistant | 4 |
| data_report / report_* | visualdata | 4 |
| WH_* / WM_* | system (warehouse) | 27 |
| Demo_* / student / mt_* | system (demo / legacy) | 8 |
| Others (system infrastructure) | framework / system | 8 |

**注**：详细 prefix → module 映射将基于 Entity 类所在命名空间确认（下一阶段任务）。

---

## 4. Table Unit Status（初始 — P8-0）

所有 289 张表初始状态：

```
DISCOVERED: 289
ASSESSED:    0
DESIGNED:    0
READY:       0
REFACTORED:  0
NO-CHANGE:   0
VERIFIED:    0
CLOSED:      0
```

---

## 5. 备注

### 5.1 已排除

- 7 张 view (`v_*`)
- 1 张 user-defined table type (`sa_entity_fields`)

### 5.2 已识别的备份表

- `BASE_STUDIO_MENU_BAK_20260617` — 备份表（2026-06-17 创建）。**特殊处理**。

### 5.3 已知历史遗留

- `mt*` 表名（Snowflake ID 风格）— 历史遗留表
- `Demo_*` 表 — 演示用表
- `WH_*` / `WM_*` 系列 — 仓库管理功能（可能为旧模块）

### 5.4 Entity ↔ Table 映射

下一阶段需要从 C# code 提取 Entity → Table 映射。P8-0 当前完成 DB 层 inventory，Entity mapping 标记为 P8-0.2 子任务。

---

## 6. 引用

- 原始 inventory: `inventory-raw.txt`
- Views & Types: `views-and-types-raw.txt`
- Row counts: `row-counts-raw.txt`
- FK relationships: `foreign-keys-raw.txt`
- Table metadata: `table-metadata-raw.txt`
