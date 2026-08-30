# JNPF 数据库完整表清单（289 张）

> **版本**: v1.0
> **日期**: 2026-08-30
> **源数据库**: `ZXAF_V1_DevTest1` (SQL Server 2022)
> **关联文档**: `JNPF-Database-Architecture-Manual.md`

## 说明

本清单按 **P8-C.1 分类** 排序（DEMO → TEST → UNKNOWN → TEMPLATE → PRODUCT_CORE）。
每行格式：`[编号] 表名 (列数, 行数, 是否有租户) [分类]`

- **A** = PRODUCT_CORE（生产核心，IN_SCOPE）
- **B** = SYSTEM_TEMPLATE（系统模板，CONDITIONAL）
- **C** = DEMO_SAMPLE（演示，OUT_OF_SCOPE）
- **D** = TEST_FIXTURE（测试，OUT_OF_SCOPE）
- **U** = UNKNOWN（待人工判定，HUMAN_DECISION）
- **Y/N** (Tenant) = 是否包含 tenant 列

---

## TEST_FIXTURE（6 张 — OUT_OF_SCOPE）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 1 | BASE_STUDIO_MENU_BAK_20260617 | 19 | 54 | N |
| 2 | mt543406707183714245 | 7 | 2 | N |
| 3 | mt543408365615710149 | 7 | 64 | N |
| 4 | mt543552698159464389 | 7 | 32 | N |
| 5 | mt543668771097673669 | 7 | 2 | N |
| 6 | mt543971603646513093 | 3 | 0 | N |

---

## DEMO_SAMPLE（5 张 — OUT_OF_SCOPE）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 7 | Demo_ExcelTest | 14 | 3 | N |
| 8 | Demo_Order | 15 | 151 | N |
| 9 | Demo_OrderDetail | 9 | 60 | N |
| 10 | ext_table_example | 28 | 33 | Y |
| 11 | student | 7 | 4 | N |

---

## UNKNOWN（3 张 — HUMAN_DECISION）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 12 | zx_sys_config | 17 | 2 | N |
| 13 | zx_sys_db | 8 | 5 | N |
| 14 | zx_system_db | 8 | 0 | N |

---

## SYSTEM_TEMPLATE（69 张 — CONDITIONAL）

### ext_* 业务扩展示例（18 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 15 | ext_big_data | 12 | 0 | Y |
| 16 | ext_customer | 16 | 7 | Y |
| 17 | ext_document | 22 | 4 | Y |
| 18 | ext_document_share | 13 | 0 | Y |
| 19 | ext_email_config | 21 | 1 | Y |
| 20 | ext_email_receive | 23 | 0 | Y |
| 21 | ext_email_send | 22 | 0 | Y |
| 22 | ext_employee | 26 | 0 | Y |
| 23 | ext_order | 30 | 9 | Y |
| 24 | ext_order_entry | 24 | 6 | Y |
| 25 | ext_order_receivable | 19 | 1 | Y |
| 26 | ext_product | 38 | 3 | Y |
| 27 | ext_product_classify | 12 | 6 | Y |
| 28 | ext_product_entry | 23 | 12 | Y |
| 29 | ext_product_goods | 18 | 10 | Y |
| 30 | ext_project_gantt | 24 | 0 | Y |
| 31 | ext_work_log | 17 | 0 | Y |
| 32 | ext_work_log_share | 13 | 0 | Y |

### wform_* 工作流表单模板（51 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 33 | wform_applybanquet | 16 | 1 | Y |
| 34 | wform_applydelivergoods | 20 | 0 | Y |
| 35 | wform_applydelivergoodsentry | 11 | 0 | Y |
| 36 | wform_applymeeting | 25 | 0 | Y |
| 37 | wform_archivalborrow | 18 | 2 | Y |
| 38 | wform_articleswarehous | 16 | 0 | Y |
| 39 | wform_batchpack | 18 | 0 | Y |
| 40 | wform_batchtable | 16 | 0 | Y |
| 41 | wform_conbilling | 20 | 0 | Y |
| 42 | wform_contractapproval | 26 | 0 | Y |
| 43 | wform_contractapprovalsheet | 27 | 0 | Y |
| 44 | wform_debitbill | 19 | 0 | Y |
| 45 | wform_documentapproval | 15 | 0 | Y |
| 46 | wform_documentsigning | 17 | 0 | Y |
| 47 | wform_expenseexpenditure | 19 | 0 | Y |
| 48 | wform_finishedproduct | 11 | 0 | Y |
| 49 | wform_finishedproductentry | 11 | 0 | Y |
| 50 | wform_incomerecognition | 20 | 0 | Y |
| 51 | wform_leaveapply | 18 | 0 | Y |
| 52 | wform_letterservice | 15 | 0 | Y |
| 53 | wform_materialrequisition | 12 | 0 | Y |
| 54 | wform_materialrequisitionentry | 12 | 0 | Y |
| 55 | wform_monthlyreport | 17 | 0 | Y |
| 56 | wform_officesupplies | 16 | 0 | Y |
| 57 | wform_outboundorder | 14 | 0 | Y |
| 58 | wform_outboundorderentry | 11 | 0 | Y |
| 59 | wform_outgoingapply | 16 | 0 | Y |
| 60 | wform_paydistribution | 21 | 0 | Y |
| 61 | wform_paymentapply | 22 | 0 | Y |
| 62 | wform_postbatchtab | 15 | 0 | Y |
| 63 | wform_procurementmaterial | 17 | 0 | Y |
| 64 | wform_procurementmaterialentry | 11 | 0 | Y |
| 65 | wform_purchaselist | 18 | 0 | Y |
| 66 | wform_purchaselistentry | 11 | 0 | Y |
| 67 | wform_quotationapproval | 16 | 0 | Y |
| 68 | wform_receiptprocessing | 12 | 0 | Y |
| 69 | wform_receiptsign | 13 | 0 | Y |
| 70 | wform_rewardpunishment | 13 | 0 | Y |
| 71 | wform_salesorder | 19 | 1 | Y |
| 72 | wform_salesorderentry | 11 | 1 | Y |
| 73 | wform_salessupport | 24 | 0 | Y |
| 74 | wform_staffovertime | 15 | 0 | Y |
| 75 | wform_supplementcard | 16 | 0 | Y |
| 76 | wform_travelapply | 17 | 0 | Y |
| 77 | wform_travelreimbursement | 34 | 0 | Y |
| 78 | wform_vehicleapply | 17 | 0 | Y |
| 79 | wform_violationhandling | 18 | 0 | Y |
| 80 | wform_warehousereceipt | 15 | 0 | Y |
| 81 | wform_warehousereceiptentry | 11 | 0 | Y |
| 82 | wform_workcontactsheet | 15 | 0 | Y |
| 83 | wform_zjf_wikxqi | 3 | 0 | Y |

---

## PRODUCT_CORE（206 张 — IN_SCOPE）

### ai_* / inte_* AI 基础（8 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 84 | ai_entity_field | 26 | 824 | Y |
| 85 | ai_ir_events | 14 | 3780 | Y |
| 86 | ai_ir_fragment_snapshots | 13 | 782 | Y |
| 87 | ai_projects | 19 | 329 | Y |
| 88 | ai_route_table | 10 | 328 | Y |
| 89 | ai_seed_templates | 9 | 40 | N |
| 90 | ai_skill_llm_policy | 8 | 9 | N |
| 91 | ai_skill_runs | 11 | 373 | Y |

### BASE_AI_* AI 配置（16 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 92 | base_advanced_query_scheme | 18 | 2 | Y |
| 93 | BASE_AI_AGENT_CONFIG | 19 | 5 | N |
| 94 | BASE_AI_AGENT_SKILL | 13 | 0 | N |
| 95 | BASE_AI_CALL_LOG | 25 | 1502 | Y |
| 96 | BASE_AI_EVAL_CASE | 13 | 4 | N |
| 97 | BASE_AI_EVAL_GOLDEN_SET | 11 | 1 | N |
| 98 | BASE_AI_EVAL_RUN | 20 | 0 | Y |
| 99 | BASE_AI_GENERATED_PROJECT | 25 | 328 | Y |
| 100 | BASE_AI_MCP_CONFIG | 15 | 0 | N |
| 101 | BASE_AI_MODEL_PROVIDER | 20 | 5 | N |
| 102 | BASE_AI_MODEL_ROUTING | 16 | 5 | N |
| 103 | BASE_AI_PIPELINE | 38 | 409 | Y |
| 104 | BASE_AI_PIPELINE_MESSAGE | 20 | 678 | Y |
| 105 | BASE_AI_PIPELINE_S2_PROGRESS | 20 | 3 | Y |
| 106 | BASE_AI_PIPELINE_STAGE_CONFIG | 15 | 5 | N |
| 107 | BASE_AI_PROMPT_TEMPLATE | 12 | 0 | Y |
| 108 | BASE_AI_SKILL_REVIEW | 14 | 0 | Y |
| 109 | BASE_AI_UI_TEMPLATE | 18 | 0 | Y |

### base_* 系统核心（约 100 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 110 | base_api_log | 38 | 39 | Y |
| 111 | base_app_data | 20 | 0 | Y |
| 112 | base_authorize | 16 | 2553 | Y |
| 113 | base_bill_rule | 27 | 61 | Y |
| 114 | base_columns_purview | 14 | 1 | Y |
| 115 | base_common_fields | 18 | 10 | Y |
| 116 | base_common_words | 15 | 0 | Y |
| 117 | base_data_interface | 27 | 146 | Y |
| 118 | base_data_interface_log | 19 | 0 | Y |
| 119 | base_data_interface_oauth | 21 | 1 | Y |
| 120 | base_data_interface_user | 14 | 1 | Y |
| 121 | base_data_interface_variate | 15 | 1 | Y |
| 122 | base_db_link | 23 | 1 | Y |
| 123 | base_dictionary_data | 20 | 897 | Y |
| 124 | base_dictionary_type | 19 | 145 | Y |
| 125 | base_file | 16 | 0 | Y |
| 126 | BASE_FOUNDER_AUTH_LOG | 16 | 13 | Y |
| 127 | base_group | 15 | 1 | Y |
| 128 | base_im_content | 18 | 9 | Y |
| 129 | base_im_reply | 15 | 2 | Y |
| 130 | base_integrate | 20 | 3 | Y |
| 131 | base_integrate_node | 26 | 0 | Y |
| 132 | base_integrate_queue | 17 | 0 | Y |
| 133 | base_integrate_task | 23 | 0 | Y |
| 134 | BASE_IR_EDIT_PATCH | 17 | 0 | Y |
| 135 | BASE_IR_VERSION | 21 | 0 | Y |
| 136 | BASE_KNOWLEDGE_EDGE | 16 | 0 | Y |
| 137 | BASE_KNOWLEDGE_NODE | 15 | 0 | Y |
| 138 | BASE_KNOWLEDGE_RULE | 16 | 0 | Y |
| 139 | BASE_MENU_BADGE | 8 | 0 | Y |
| 140 | base_message | 20 | 1229 | Y |
| 141 | base_module | 28 | 210 | Y |
| 142 | base_module_authorize | 24 | 8 | Y |
| 143 | base_module_button | 20 | 34 | Y |
| 144 | base_module_column | 22 | 6 | Y |
| 145 | base_module_form | 21 | 6 | Y |
| 146 | base_module_link | 15 | 2 | Y |
| 147 | base_module_scheme | 20 | 8 | Y |
| 148 | base_msg_account | 39 | 4 | Y |
| 149 | base_msg_monitor | 21 | 147 | Y |
| 150 | base_msg_send | 17 | 24 | Y |
| 151 | base_msg_send_template | 17 | 23 | Y |
| 152 | base_msg_short_link | 21 | 0 | Y |
| 153 | base_msg_sms_field | 17 | 0 | Y |
| 154 | base_msg_template | 23 | 26 | Y |
| 155 | base_msg_template_param | 15 | 78 | Y |
| 156 | base_msg_wechat_user | 16 | 0 | Y |
| 157 | base_notice | 22 | 3 | Y |
| 158 | base_organize | 20 | 6 | Y |
| 159 | base_organize_administrator | 25 | 5 | Y |
| 160 | base_organize_relation | 15 | 0 | Y |
| 161 | base_permission_group | 16 | 5 | Y |
| 162 | base_portal | 24 | 2 | Y |
| 163 | base_portal_data | 16 | 9 | Y |
| 164 | base_portal_manage | 16 | 2 | Y |
| 165 | base_position | 18 | 2 | Y |
| 166 | base_print_log | 14 | 21 | Y |
| 167 | base_print_template | 25 | 5 | Y |
| 168 | base_province | 18 | 47512 | Y |
| 169 | base_province_atlas | 20 | 3210 | Y |
| 170 | BASE_REPORT | 14 | 5 | N |
| 171 | base_role | 18 | 9 | Y |
| 172 | BASE_SANDBOX | 22 | 0 | Y |
| 173 | base_schedule | 33 | 0 | Y |
| 174 | base_schedule_log | 35 | 0 | Y |
| 175 | base_schedule_user | 16 | 0 | Y |
| 176 | base_sign_img | 15 | 0 | Y |
| 177 | base_signature | 16 | 0 | Y |
| 178 | base_signature_user | 15 | 0 | Y |
| 179 | base_socials_users | 15 | 0 | Y |
| 180 | BASE_STUDIO_MENU | 19 | 54 | N |
| 181 | base_syn_third_info | 17 | 0 | Y |
| 182 | base_sys_config | 17 | 74 | Y |
| 183 | base_sys_log | 32 | 12615 | Y |
| 184 | base_system | 23 | 7 | Y |
| 185 | BASE_TENANT_GLOSSARY | 13 | 0 | Y |
| 186 | BASE_TENANT_INDUSTRY | 12 | 0 | Y |
| 187 | base_time_task | 21 | 0 | Y |
| 188 | base_time_task_log | 16 | 22 | Y |
| 189 | base_user | 68 | 45 | Y |
| 190 | base_user_device | 14 | 0 | Y |
| 191 | base_user_old_password | 15 | 0 | Y |
| 192 | base_user_relation | 15 | 82 | Y |
| 193 | base_visual_dev | 30 | 48 | Y |
| 194 | base_visual_filter | 14 | 0 | Y |
| 195 | base_visual_link | 26 | 0 | Y |
| 196 | base_visual_release | 29 | 25 | Y |

### blade_visual* + report* + data_report 可视化（12 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 197 | blade_visual | 14 | 77 | Y |
| 198 | blade_visual_category | 6 | 2 | Y |
| 199 | blade_visual_component | 7 | 42 | Y |
| 200 | blade_visual_config | 6 | 77 | Y |
| 201 | blade_visual_db | 16 | 4 | Y |
| 202 | blade_visual_glob | 6 | 0 | Y |
| 203 | blade_visual_map | 5 | 3 | Y |
| 204 | blade_visual_record | 19 | 3 | Y |
| 205 | data_report | 16 | 15 | Y |
| 231 | report_charts | 16 | 21 | Y |
| 232 | report_department | 5 | 12 | Y |
| 233 | report_user | 10 | 283 | Y |

### domain_model + framework 基础（4 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 206 | domain_model | 15 | 0 | N |
| 207 | EVAL_METRIC | 13 | 0 | Y |
| 230 | PROCESSED_EVENT | 3 | 0 | N |
| 247 | SchemaVersions | 3 | 2 | N |
| 248 | SYS_EVENT_OUTBOX_MESSAGE | 9 | 0 | N |
| 249 | SYS_PROCESSED_EVENT | 3 | 0 | N |
| 250 | undo_log | 10 | 0 | Y |

### flow_* 工作流引擎（18 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 208 | flow_candidates | 18 | 0 | Y |
| 209 | flow_comment | 16 | 0 | Y |
| 210 | flow_delegate | 23 | 0 | Y |
| 211 | flow_event_log | 15 | 24 | Y |
| 212 | flow_form | 27 | 4 | Y |
| 213 | flow_form_authorize | 14 | 0 | Y |
| 214 | flow_form_relation | 13 | 1 | Y |
| 215 | flow_launch_user | 18 | 16 | Y |
| 216 | flow_reject_data | 14 | 0 | Y |
| 217 | flow_task | 41 | 16 | Y |
| 218 | flow_task_circulate | 17 | 0 | Y |
| 219 | flow_task_node | 24 | 45 | Y |
| 220 | flow_task_operator | 28 | 555 | Y |
| 221 | flow_task_operator_record | 26 | 15 | Y |
| 222 | flow_task_operator_user | 28 | 0 | Y |
| 223 | flow_template | 19 | 6 | Y |
| 224 | flow_template_json | 19 | 3 | Y |
| 225 | flow_visible | 15 | 41 | Y |

### inte_assistant_* AI 交付物（2 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 226 | inte_assistant_attachment | 18 | 55 | Y |
| 227 | inte_assistant_deliverable | 11 | 269 | Y |

### kg_* 知识图谱（2 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 228 | kg_pattern | 18 | 0 | N |
| 229 | kg_pattern_usage | 6 | 0 | N |

### sa_* SA 智能体输出（12 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 234 | sa_assumptions | 12 | 14 | Y |
| 235 | sa_business_process | 30 | 19 | N |
| 236 | sa_consistency | 11 | 15 | Y |
| 237 | sa_data_dictionary | 35 | 19 | N |
| 238 | sa_decision_table | 30 | 172 | N |
| 239 | sa_dfd | 31 | 19 | N |
| 240 | sa_er | 29 | 19 | N |
| 241 | sa_pspec | 25 | 172 | N |
| 242 | sa_quality_score | 12 | 14 | Y |
| 243 | sa_scope | 25 | 19 | N |
| 244 | sa_state_machine | 30 | 19 | N |
| 245 | sa_ui | 28 | 210 | N |
| 246 | sa_validation_log | 13 | 0 | N |

### WH_* / WM_* 仓库管理（39 张）

| # | 表名 | 列数 | 行数 | Tenant |
|--:|---|--:|--:|:-:|
| 251 | WH_BasicData | 3 | 208 | N |
| 252 | WH_Bill | 16 | 2 | N |
| 253 | WH_BillAutoID | 3 | 4 | N |
| 254 | WH_BillDetail | 15 | 4 | N |
| 255 | WH_CheckBillDetail | 12 | 19 | N |
| 256 | WH_Customer | 13 | 1 | N |
| 257 | WH_CustomerClass | 4 | 2 | N |
| 258 | WH_Depot | 5 | 2 | N |
| 259 | WH_DepotMaterial | 12 | 2 | N |
| 260 | WH_Dept | 4 | 2 | N |
| 261 | WH_Material | 14 | 4 | N |
| 262 | WH_MaterialClass | 4 | 1 | N |
| 263 | WH_Project | 4 | 1 | N |
| 264 | WH_RemoveBill | 10 | 8 | N |
| 265 | WH_RemoveBillDetail | 12 | 13 | N |
| 266 | WH_StorageType | 3 | 4 | N |
| 267 | WH_Supplier | 10 | 1 | N |
| 268 | WH_SupplierClass | 3 | 3 | N |
| 269 | WM_BasicData | 3 | 29 | N |
| 270 | WM_Bill | 20 | 151 | N |
| 271 | WM_BillAutoID | 3 | 4 | N |
| 272 | WM_BillDetail | 16 | 1629 | N |
| 273 | WM_CheckBill | 11 | 1 | N |
| 274 | WM_CheckBillDetail | 14 | 1613 | N |
| 275 | WM_Client | 16 | 1 | N |
| 276 | WM_ClientClass | 4 | 0 | N |
| 277 | WM_Depot | 5 | 1 | N |
| 278 | WM_DepotMaterial | 15 | 0 | N |
| 279 | WM_Dept | 7 | 1 | N |
| 280 | WM_Employee | 8 | 3 | N |
| 281 | WM_Material | 17 | 739 | N |
| 282 | WM_Project | 4 | 0 | N |
| 283 | WM_RemoveBill | 12 | 5 | N |
| 284 | WM_RemoveBillDetail | 13 | 7 | N |
| 285 | WM_StorageClass | 7 | 9 | N |
| 286 | WM_StorageType | 3 | 8 | N |
| 287 | WM_Supplier | 12 | 1 | N |
| 288 | WM_SupplierClass | 14 | 0 | N |
| 289 | WM_TaxRate | 4 | 0 | N |

---

## 统计汇总

| 分类 | 表数 | Tenant Y | Tenant N |
|---|---:|---:|---:|
| **TEST_FIXTURE** | 6 | 0 | 6 |
| **DEMO_SAMPLE** | 5 | 1 | 4 |
| **UNKNOWN** | 3 | 0 | 3 |
| **SYSTEM_TEMPLATE** | 69 | 69 | 0 |
| **PRODUCT_CORE** | 206 | 167 | 39 |
| **总计** | **289** | **237** | **52** |

## 治理状态

| 分类 | 数量 | Phase 8 状态 |
|---|---:|---|
| OUT_OF_SCOPE | 11 | 永久跳过 |
| CONDITIONAL | 69 | 待用户决策 |
| HUMAN_DECISION | 3 | 待用户分类 |
| IN_SCOPE | 206 | 可进入生产重构 |
