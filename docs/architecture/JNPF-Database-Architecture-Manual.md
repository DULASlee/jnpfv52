# JNPF 数据库架构手册

> **版本**: v1.0
> **日期**: 2026-08-30
> **作用域**: JNPF 低代码平台（.NET 8 + SqlSugar + SQL Server 2022）
> **适用读者**: 架构师、研发工程师、AI 大模型、数据库管理员
> **状态**: 已冻结 (Production Universe Frozen)
> **关联文档**:
> - Phase 8 Master Plan: `docs/universal/Phase-8/Phase-8-JNPF-Table-Refactoring-Master-Execution-Plan.md`
> - P8-C.1 Scope Classification: `docs/universal/Phase-8/p8-c/p8-c1-production-scope-registry.md`

---

## 文档目的

本手册面向 JNPF 平台数据库（SQL Server `ZXAF_V1_DevTest1`）的 **289 张物理表**，提供：

1. **架构全貌**: 模块划分、命名约定、依赖关系
2. **业务域解释**: 每个核心业务表的功能、用途、关键字段
3. **设计模式**: 多租户、生命周期、多态外键、SCD2 等通用模式
4. **生产对象边界**: 已冻结的 206 张生产表（PRODUCT_CORE）
5. **AI 可参考的语义层**: 为 AI 大模型提供领域语义、命名语义、关系语义

**这不是过程资料，而是架构基线 (Architecture Baseline)。**

---

## 第一部分：数据库总览

### 1.1 数据库基本信息

| 项目 | 值 |
|---|---|
| 数据库 | `ZXAF_V1_DevTest1` |
| SQL Server 版本 | Microsoft SQL Server 2022 (RTM-GDR) (KB5102334) - 16.0.1190.2 |
| 架构 (Schema) | `dbo`（单一 Schema） |
| 物理表总数 | **289 张** |
| 视图 (View) | 若干（含 `sa_entity_fields`） |
| 外键 (FK) 数量 | **14 个**（全部在 SA/KG 模块内） |
| 多租户支持 | 是（约 187 / 289 = 64.7% 表含 `f_tenant_id`） |

### 1.2 表数量按模块分布

| 模块 | 表数量 | 当前行数 | 说明 |
|---|---:|---:|---|
| **system-core**（系统核心 `base_*`） | 106 | 72,458 | 平台主干，含身份、权限、字典、日志、消息、接口 |
| **workflow-form-example**（`wform_*`） | 51 | 5 | 工作流表单模板（业务示例） |
| **system-warehouse-legacy**（`WH_*`/`WM_*`） | 39 | 4,483 | 老版仓库管理模块（无 tenant） |
| **system-extension**（`ext_*`） | 19 | 92 | 业务扩展示例（CRM/ERP/HR） |
| **workflow-engine**（`flow_*`） | 18 | 726 | 工作流引擎运行时 |
| **inteAssistant-SA**（`sa_*`） | 13 | 711 | SA 智能体输出表 |
| **visualdata**（可视化设计） | 12 | 539 | 可视化大屏、报表 |
| **inteAssistant-other**（`ai_*`/`inte_*`） | 10 | 6,789 | AI 基础（IR Events、Attachment 等） |
| **framework-infrastructure**（框架基础） | 5 | 2 | 框架级（事件、Undo Log、SchemaVersions） |
| **system-legacy-snowflake**（`mt*`） | 5 | 100 | 历史遗留 Snowflake ID 表 |
| **system-demo**（`Demo_*`） | 4 | 218 | 演示/教学数据 |
| **unknown**（`zx_*`） | 3 | 7 | 客户定制（来源待确认） |
| **inteAssistant-KG**（知识图谱） | 2 | 0 | 模式库、模式使用 |
| **other**（其他） | 2 | 0 | 其他未分类 |
| **合计** | **289** | **86,128** | |

### 1.3 按 JNPF 业务模块（namespace）汇总

| 业务模块 | 包含前缀 | 表数 |
|---|---|---:|
| **system** | `base_*`、`BASE_*`（不含 AI/STUDIO/IR）、`ext_*`、`WH_*`、`WM_*`、`Demo_*`、`student` | 153 |
| **workflow** | `flow_*`、`wform_*` | 69 |
| **inteAssistant** | `ai_*`、`BASE_AI_*`、`sa_*`、`BASE_KNOWLEDGE_*`、`kg_*`、`BASE_IR_*`、`BASE_STUDIO_*`、`inte_*`、`EVAL_*` | 50 |
| **visualdata** | `blade_*`、`BASE_REPORT`、`report_*`、`data_report` | 12 |
| **framework** | `SYS_*`、`PROCESSED_EVENT`、`SchemaVersions`、`undo_log` | 5 |
| **合计** | | **289** |

### 1.4 生产对象边界（已冻结）

按 Phase 8 P8-C.1 分类，289 张表分为 5 类：

| 分类 | 数量 | 比例 | 治理策略 |
|---|---:|---:|---|
| **A — PRODUCT_CORE** | 206 | 71.3% | IN_SCOPE：进入生产重构 |
| **B — SYSTEM_TEMPLATE** | 69 | 23.9% | CONDITIONAL：需用户决定（模板是否纳入） |
| **C — DEMO_SAMPLE** | 5 | 1.7% | OUT_OF_SCOPE：跳过 |
| **D — TEST_FIXTURE** | 6 | 2.1% | OUT_OF_SCOPE：跳过 |
| **U — UNKNOWN** | 3 | 1.0% | HUMAN_DECISION：待分类 |

> **生产宇宙（Production Universe）= 206 张 PRODUCT_CORE 表**。
> 物理表 289 张 ≠ 生产表 206 张。这是 Phase 8 最重要的概念修正。

---

## 第二部分：命名规范（Naming Conventions）

### 2.1 表命名规范

JNPF 表名遵循 **「前缀 + 模块/语义名」** 模式，前缀即模块标识。

#### 2.1.1 前缀分类

| 前缀 | 模块 | 命名空间 | 说明 |
|---|---|---|---|
| `base_` / `BASE_` | system | `JNPF.Systems.*` | JNPF 系统核心（小写 `base_` 占多数，大写 `BASE_` 多用于新模块） |
| `ext_` | system-extension | `JNPF.Extend.*` | 业务扩展示例（CRM/ERP/HR 等场景） |
| `WH_` / `WM_` | system-warehouse | 内部 | 仓库管理（Warehouse），无 tenant |
| `Demo_` | system-demo | 测试数据 | 演示/示例数据 |
| `flow_` | workflow | `JNPF.WorkFlow.*` | 工作流引擎运行时 |
| `wform_` | workflow | 模板 | 工作流表单模板示例 |
| `sa_` | inteAssistant-SA | `JNPF.InteAssistant.SA` | SA 智能体输出 |
| `ai_` / `inte_` | inteAssistant | `JNPF.InteAssistant.*` | AI 基础表 |
| `BASE_AI_` | inteAssistant-AI | `JNPF.InteAssistant.AI` | AI 配置（Pipeline、Agent、Skill、Model） |
| `BASE_KNOWLEDGE_` | inteAssistant-KG | `JNPF.InteAssistant.KG` | 知识图谱（Node、Edge、Rule） |
| `kg_` | inteAssistant-KG | `JNPF.InteAssistant.KG` | 知识图谱（Pattern） |
| `BASE_IR_` | inteAssistant-IR | `JNPF.InteAssistant.IR` | Intermediate Representation |
| `BASE_STUDIO_` | inteAssistant-Studio | `JNPF.InteAssistant.Studio` | Studio 设计器 |
| `blade_` | visualdata | 兼容 Avue | 兼容旧版 Avue 可视化 |
| `BASE_REPORT` / `report_*` / `data_report` | visualdata | `JNPF.VisualData.*` | 报表 |
| `SYS_` | framework | `JNPF.Framework.*` | 框架基础设施 |
| `mt<数字>` | legacy | 历史遗留 | 早期 Snowflake ID 自动生成表 |
| `zx_` | unknown | 客户定制 | 来源待确认 |

#### 2.1.2 命名风格一致性

**⚠️ JNPF 内部命名风格不一致**：

- **大小写混用**：
  - `base_user`（小写 `f_`）vs `BASE_REPORT`（大写 `F_`）
  - `flow_task_node`（小写 `f_`）vs `FLOW_TASK`（实际是小写）
  - `blade_visual`（无 `f_` 前缀）vs `BASE_AI_*`（大写 `F_`）

- **大小写规则基本规律**：
  - 老模块（`base_*`、`flow_*`、`ext_*`、`sa_*`、`kg_*`、`WH_*`、`WM_*`、`wform_*`）：**列名小写 `f_`**
  - 新模块（`BASE_AI_*`、`BASE_KNOWLEDGE_*`、`BASE_IR_*`、`BASE_STUDIO_*`）：**列名大写 `F_`**
  - 兼容旧版（`blade_*`、`BASE_REPORT`）：**无 `f_` 前缀** 或大写
  - 报告类（`report_charts`）：**混合命名**（`ID`、`QYBM`、`PGRQ` 等中文拼音大写）

**实践建议**：编写 SQL 时**先查 INFORMATION_SCHEMA** 确认列名实际拼写，不要假设。

### 2.2 列命名规范

#### 2.2.1 标准字段约定

| 字段 | 类型 | 含义 | 说明 |
|---|---|---|---|
| `f_id` / `F_Id` | `nvarchar(50)` | 主键 | Snowflake ID，分布式唯一 |
| `f_tenant_id` / `F_TenantId` / `F_TENANT_ID` | `nvarchar(50)` | 租户 ID | 多租户隔离；**多数表必备** |
| `f_delete_mark` / `F_DeleteMark` | `int` (0/1) 或 `bit` | 软删除标记 | `0` = 正常，`1` = 已删除 |
| `f_enabled_mark` / `F_Enabled` / `F_EnabledMark` | `int` (0/1) | 启用标记 | `0` = 禁用，`1` = 启用 |
| `f_creator_time` / `F_CreatorTime` | `datetime` | 创建时间 | CLDS 字段 |
| `f_creator_user_id` / `F_CreatorUserId` | `nvarchar(50)` | 创建者 | CLDS 字段 |
| `f_last_modify_time` / `F_ModifyTime` | `datetime` | 最后修改时间 | CLDS 字段 |
| `f_last_modify_user_id` / `F_ModifyUserId` | `nvarchar(50)` | 最后修改者 | CLDS 字段 |
| `f_delete_time` / `F_DeleteTime` | `datetime` | 删除时间 | CLDS 字段 |
| `f_delete_user_id` / `F_DeleteUserId` | `nvarchar(50)` | 删除者 | CLDS 字段 |
| `f_sort_code` / `F_SortCode` / `F_Sort` | `bigint` | 排序号 | 通用排序字段 |
| `f_description` / `F_Description` | `nvarchar(500)` | 描述 | 通用备注字段 |

**CLDS 字段**（Create/Last/Delete/Soft）是 JNPF 的核心审计模式，几乎所有业务表都包含。

#### 2.2.2 业务字段命名习惯

| 习惯 | 示例 | 说明 |
|---|---|---|
| 中文拼音首字母 | `QYBM`（区域编码）、`PGRQ`（评估日期）、`FXDMC`（分项名称） | 老模块（`report_*`）使用 |
| 业务语义英文 | `order_code`、`customer_name`、`audit_state` | 新模块使用 |
| JSON 字符串 | `f_property_json`、`f_form_data` | 表单/属性动态数据 |
| 时间戳字段名带 `_time` | `f_create_time`、`f_modify_time` | 注意与 CLDS 区别 |
| 状态字段名带 `_state` | `f_audit_state`、`f_current_state` | 业务状态机 |

### 2.3 索引命名约定（Phase 8 标准）

P8-B / P8-C 新建索引统一遵循：

```
IDX_<TABLE_ABBREV>_<COLUMN_PATTERN>
```

示例：
- `IDX_ORGANIZE_PARENT (f_tenant_id, f_parent_id)` — 组织树查询
- `IDX_AUTHORIZE_OBJECT (f_tenant_id, f_object_type, f_object_id)` — 授权多态查询
- `IDX_TASKOPERATOR_TASK (f_tenant_id, f_task_id)` — 工作流任务查询

---

## 第三部分：业务模块详解

### 3.1 system-core（系统核心）

**模块定位**：JNPF 平台的"心脏"，提供身份、权限、字典、消息、接口等核心能力。

**表数量**：106 张（含 `base_*` 和 `BASE_*` 大写模块）

#### 3.1.1 身份认证域（Identity & Authentication）

| 表名 | 行数 | 列数 | 用途 |
|---|---:|---:|---|
| `base_user` | 45 | **68** | **核心**：平台用户主表，最大列数 |
| `base_user_relation` | 82 | 15 | 用户-角色/组织 多对多关联（多态外键） |
| `base_user_device` | 0 | 14 | 用户设备绑定 |
| `base_user_old_password` | 0 | 15 | 历史密码记录（防复用） |
| `base_old_password` | 0 | - | 同上别名（待清理） |

**关键字段**（`base_user`）：
- `f_id` — Snowflake
- `f_account` — 登录账号
- `f_password` — 密码哈希
- `f_real_name` / `f_nick_name` — 姓名
- `f_mobile` / `f_email` — 联系方式
- `f_organize_id` / `f_position_id` / `f_role_id` — 主岗位/角色/组织（1:N）
- `f_is_administrator` — 超管标记
- `f_enabled_mark` / `f_lock_mark` — 状态
- `f_change_password_date` — 密码修改时间

**⚠️ 治理重点**：`base_user` 是最关键表，68 列、参考所有模块，**任何重构必须先发 Decision Brief**。

#### 3.1.2 组织架构域（Organization）

| 表名 | 行数 | 列数 | 用途 |
|---|---:|---:|---|
| `base_organize` | 6 | 20 | **核心**：组织树（公司/部门/小组） |
| `base_organize_administrator` | 5 | 25 | 组织管理员授权 |
| `base_organize_relation` | 0 | 15 | 组织-角色关联 |
| `base_position` | 2 | 18 | 岗位 |
| `base_group` | 1 | 15 | 用户分组 |

**`base_organize` 关键设计**：
- `f_parent_id` 自引用，构成组织树
- `f_organize_id_tree` 物化路径（denormalized path）加速子树查询
- `f_manager_id` 组织负责人
- `f_property_json` 扩展属性

#### 3.1.3 权限管理域（Authorization）

| 表名 | 行数 | 列数 | 用途 |
|---|---:|---:|---|
| `base_authorize` | 2,553 | 16 | **核心**：权限授权（多态外键） |
| `base_module` | 210 | 28 | 功能模块/菜单 |
| `base_module_button` | 34 | 20 | 按钮权限 |
| `base_module_column` | 6 | 22 | 列权限（字段级） |
| `base_module_form` | 6 | 21 | 表单权限 |
| `base_module_authorize` | 8 | 24 | 模块-用户授权 |
| `base_module_scheme` | 8 | 20 | 模块方案 |
| `base_module_link` | 2 | 15 | 模块链接 |
| `base_permission_group` | 5 | 16 | 权限分组 |

**`base_authorize` 关键设计**：
- `f_item_type` + `f_item_id`：被授权对象（模块/按钮/列/表单）
- `f_object_type` + `f_object_id`：被授权主体（用户/角色/岗位）
- 这是 JNPF 权限系统的核心，使用多态外键（polymorphic FK）模式

#### 3.1.4 数据字典域（Dictionary）

| 表名 | 行数 | 列数 | 用途 |
|---|---:|---:|---|
| `base_dictionary_type` | 145 | 19 | 字典类型（业务类别） |
| `base_dictionary_data` | 897 | 20 | 字典数据（业务枚举值） |
| `base_bill_rule` | 61 | 27 | 单据编号规则 |
| `base_common_fields` | 10 | 18 | 通用字段定义 |
| `base_common_words` | 0 | 15 | 常用词库（敏感词过滤） |

**字典设计**：
- `base_dictionary_type.f_en_code` 业务编码（如 `NATION`、`POSITION_TYPE`）
- `base_dictionary_data.f_dictionary_type_id` 关联类型
- `base_dictionary_data.f_simple_spelling` 拼音首字母（搜索辅助）
- `base_dictionary_data.f_is_default` 默认值标记

#### 3.1.5 系统配置域（System Config）

| 表名 | 行数 | 列数 | 用途 |
|---|---:|---:|---|
| `base_sys_config` | 74 | 17 | **键值对配置** |
| `base_sys_log` | 12,615 | 32 | **核心**：系统日志（最大数据量表） |
| `base_api_log` | 39 | 38 | API 调用日志（混合大小写） |
| `base_sign_img` | 0 | 15 | 签章图片 |
| `base_syn_third_info` | 0 | 17 | 第三方系统同步映射 |

**`base_sys_config` 是关键配置表**：
- `f_key` — 配置键（如 `sys.account.passwordStrength`）
- `f_value` — 配置值（任意字符串/JSON）
- `f_category` — 配置分类
- **每次业务调用都会查此表**，必须有索引

#### 3.1.6 数据接口域（Data Interface）

| 表名 | 行数 | 列数 | 用途 |
|---|---:|---:|---|
| `base_data_interface` | 146 | 27 | **核心**：数据接口定义 |
| `base_data_interface_log` | 0 | 19 | 接口调用日志 |
| `base_data_interface_oauth` | 1 | 21 | 接口 OAuth 凭证 |
| `base_data_interface_user` | 1 | 14 | 接口用户授权 |
| `base_data_interface_variate` | 1 | 15 | 接口变量 |
| `base_db_link` | 1 | 23 | 数据库连接 |

**`base_data_interface` 关键字段**：
- `f_en_code` 接口编码
- `f_type` (1=SQL, 2=API)
- `f_data_config_json` 接口参数配置
- `f_field_json` 返回字段映射
- `f_parameter_json` 参数定义

#### 3.1.7 消息通讯域（Messaging）

| 表名 | 列数 | 用途 |
|---|---:|---|
| `base_message` | 20 | 站内消息 |
| `base_msg_account` | 39 | 第三方消息账号（微信/钉钉/邮件） |
| `base_msg_template` | 23 | 消息模板 |
| `base_msg_send` | 17 | 发送记录 |
| `base_msg_send_template` | 17 | 发送模板 |
| `base_msg_template_param` | 15 | 模板参数 |
| `base_msg_sms_field` | 17 | 短信字段 |
| `base_msg_short_link` | 21 | 短链接 |
| `base_msg_wechat_user` | 16 | 微信用户 |
| `base_msg_monitor` | 21 | 消息监控 |
| `base_im_content` | 18 | IM 内容 |
| `base_im_reply` | 15 | IM 回复 |

#### 3.1.8 行政区划域（Region）

| 表名 | 行数 | 列数 | 用途 |
|---|---:|---:|---|
| `base_province` | 47,512 | 18 | **数据量最大表**：行政区划 |
| `base_province_atlas` | 3,210 | 20 | 行政区划地图 |

**`base_province` 设计**：
- `f_parent_id` 自引用（省/市/县/乡/村 五级）
- `f_quick_query` 拼音/首字母快速查询字段
- `f_en_code` 行政区划编码
- `f_type` 行政级别

#### 3.1.9 其它系统表

| 表名 | 用途 |
|---|---|
| `base_visual_dev` | 可视化开发（表单/列表/流程） |
| `base_visual_release` | 可视化发布版本 |
| `base_visual_link` | 可视化关联 |
| `base_visual_filter` | 可视化过滤器 |
| `base_schedule` / `base_schedule_user` / `base_schedule_log` | 调度任务 |
| `base_time_task` / `base_time_task_log` | 定时任务 |
| `base_print_template` / `base_print_log` | 打印模板/日志 |
| `base_signature` / `base_signature_user` | 电子签名 |
| `base_socials_users` | 社交账号 |
| `base_notice` / `base_portal` / `base_portal_data` | 通知/门户 |
| `base_app_data` | 应用数据 |
| `base_file` | 文件 |
| `base_advanced_query_scheme` | 高级查询方案 |
| `base_columns_purview` | 列权限 |
| `base_integrate` / `base_integrate_node` / `base_integrate_queue` / `base_integrate_task` | 集成任务 |

---

### 3.2 workflow-engine（工作流引擎）

**模块定位**：JNPF 的工作流运行时引擎（flowable-style 状态机）。

**表数量**：18 张

| 表名 | 列数 | 用途 |
|---|---:|---|
| `flow_task` | 41 | **核心**：流程任务实例 |
| `flow_task_node` | 24 | 任务节点 |
| `flow_task_operator` | 28 | 节点操作人 |
| `flow_task_operator_record` | 26 | 操作历史 |
| `flow_task_operator_user` | 28 | 操作人映射 |
| `flow_task_circulate` | 17 | 传阅 |
| `flow_template` | 19 | 流程模板 |
| `flow_template_json` | 19 | 模板 JSON |
| `flow_form` | 27 | 流程表单 |
| `flow_form_authorize` | 14 | 表单授权 |
| `flow_form_relation` | 13 | 表单关联 |
| `flow_candidates` | 18 | 候选审批人 |
| `flow_comment` | 16 | 审批意见 |
| `flow_event_log` | 15 | 事件日志 |
| `flow_delegate` | 23 | 委托授权 |
| `flow_visible` | 15 | 可见性配置 |
| `flow_launch_user` | 18 | 发起人 |
| `flow_reject_data` | 14 | 驳回数据 |

**`flow_task` 关键字段**：
- `f_en_code` — 业务单据号
- `f_flow_id` / `f_flow_code` / `f_flow_name` — 流程模板引用
- `f_current_node_code` / `f_current_node_name` — 当前节点
- `f_status` — 任务状态（0=草稿, 1=流转, 2=完成, 3=挂起）
- `f_flow_form_data_json` / `f_flow_template_json` — 表单/模板数据
- `f_parent_id` — 子流程父任务

**核心关系**：
```
flow_template (1) ──< (N) flow_task ──< (N) flow_task_node ──< (N) flow_task_operator
                                       │                       │
                                       │                       └─< flow_task_operator_record
                                       └─< flow_comment
                                       └─< flow_candidates
```

### 3.3 workflow-form-example（工作流表单模板）

**模块定位**：JNPF 内置的 51 张工作流业务表单模板（`wform_*`），覆盖 HR/财务/采购/销售/合同等场景。

**治理状态**：**SYSTEM_TEMPLATE / CONDITIONAL** — 是否纳入生产重构待用户决策。

**典型表**（部分）：
- `wform_leaveapply` — 请假申请
- `wform_applybanquet` — 宴会申请
- `wform_contractapproval` — 合同审批
- `wform_salesorder` — 销售订单
- `wform_purchaselist` — 采购清单
- `wform_travelapply` — 出差申请
- `wform_applydelivergoods` — 提货申请
- `wform_supplementcard` — 补卡申请
- 等等...

**统一模式**：
- `F_FlowId` — 关联 `flow_task.f_flow_id`
- `F_BillNo` — 单据号
- `F_ApplyUser` / `F_InputPerson` — 申请人（不同模板字段名不同）
- `F_ApplyDate` / `F_ApplyTime` — 申请时间

### 3.4 visualdata（可视化大屏与报表）

**模块定位**：大屏设计器、报表系统（兼容 Avue Blade）。

**表数量**：12 张

| 表名 | 列数 | 用途 |
|---|---:|---|
| `blade_visual` | 14 | 大屏设计 |
| `blade_visual_category` | 6 | 大屏分类 |
| `blade_visual_component` | 7 | 组件库 |
| `blade_visual_config` | 6 | 组件配置 |
| `blade_visual_db` | 16 | 数据源 |
| `blade_visual_glob` | 6 | 全局变量 |
| `blade_visual_map` | 5 | 地图配置 |
| `blade_visual_record` | 19 | 设计记录 |
| `BASE_REPORT` | 14 | **报表主表**（UPPERCASE） |
| `data_report` | 16 | 报表分类 |
| `report_charts` | 16 | 报表图表（中文拼音命名） |
| `report_user` / `report_department` | 10 / 5 | 报表授权 |

**`report_charts` 命名特殊性**：
- 使用中文拼音大写：`QYBM`（区域编码）、`FXDMC`（分项名称）、`PGRQ`（评估日期）
- 这是历史遗留命名，新代码应避免

### 3.5 inteAssistant-AI（AI 配置与运行）

**模块定位**：JNPF 的智能助手模块（inteAssistant），含 AI Pipeline、Agent、Skill、Model、Evaluation 等。

**表数量**：约 25 张（`BASE_AI_*` + `ai_*`）

#### 3.5.1 AI Pipeline（核心）

| 表名 | 列数 | 用途 |
|---|---:|---|
| `BASE_AI_PIPELINE` | 38 | **核心**：AI 流水线实例（Pilot 1 已完成） |
| `BASE_AI_PIPELINE_MESSAGE` | 20 | 流水线消息（IR Events 来源） |
| `BASE_AI_PIPELINE_S2_PROGRESS` | 20 | S2 阶段进度 |
| `BASE_AI_PIPELINE_STAGE_CONFIG` | 15 | 阶段配置 |
| `BASE_AI_GENERATED_PROJECT` | 25 | 生成的项目 |

**`BASE_AI_PIPELINE` 关键字段**：
- `F_PROJECT_ID` / `F_PIPELINE_ID` — 关联项目（Triple-Key 模式）
- `F_CURRENT_STAGE` / `F_STAGE_STATUS` — 当前阶段与状态
- `F_WORK_MODE` — `greenfield` / `bugfix` / `enhancement`
- `F_CHECKPOINT` — 断点（恢复用）
- `F_SOURCE_PIPELINE_ID` — Fork 来源
- `F_FROZEN` / `F_RESUME_COUNT` — 冻结与恢复

#### 3.5.2 Agent / Skill / Model

| 表名 | 列数 | 用途 |
|---|---:|---|
| `BASE_AI_AGENT_CONFIG` | 19 | Agent 配置（Prompt、Model、Temperature） |
| `BASE_AI_AGENT_SKILL` | 13 | Agent-Skill 关联 |
| `BASE_AI_SKILL_REVIEW` | 14 | Skill 评审 |
| `BASE_AI_PROMPT_TEMPLATE` | 12 | Prompt 模板 |
| `BASE_AI_UI_TEMPLATE` | 18 | UI 模板 |
| `BASE_AI_MODEL_PROVIDER` | 20 | 模型 Provider（OpenAI、DeepSeek 等） |
| `BASE_AI_MODEL_ROUTING` | 16 | 模型路由（按阶段路由） |
| `BASE_AI_MCP_CONFIG` | 15 | MCP 服务配置 |
| `BASE_AI_CALL_LOG` | 25 | LLM 调用日志 |
| `BASE_AI_EVAL_RUN` | 20 | Eval 运行记录 |
| `BASE_AI_EVAL_CASE` | 13 | Eval 用例 |
| `BASE_AI_EVAL_GOLDEN_SET` | 11 | Eval 黄金集 |

#### 3.5.3 AI 基础设施

| 表名 | 列数 | 用途 |
|---|---:|---|
| `ai_ir_events` | 14 | **核心**：IR 事件溯源（event sourcing） |
| `ai_ir_fragment_snapshots` | 13 | IR Fragment 快照 |
| `ai_entity_field` | 26 | AI 实体字段 |
| `ai_projects` | 19 | AI 项目 |
| `ai_route_table` | 10 | 路由表 |
| `ai_seed_templates` | 9 | 种子模板 |
| `ai_skill_llm_policy` | 8 | Skill LLM 策略 |
| `ai_skill_runs` | 11 | Skill 运行 |
| `inte_assistant_attachment` | 18 | 附件 |
| `inte_assistant_deliverable` | 11 | 交付物 |

**`ai_ir_events` 关键字段**：
- `F_TenantId` / `F_ProjectId` / `F_PIPELINE_ID` — Triple-Key（Triple-Key Iron Law R12）
- `F_EventType` — 事件类型（`fragment.created`/`stage.completed` 等）
- `F_FragmentType` / `F_FragmentId` — IR Fragment 引用
- `F_Sequence` — 序列号（事件溯源顺序保证）
- `F_IsRollback` — 回滚标记

### 3.6 inteAssistant-SA（SA 智能体输出）

**模块定位**：Studio Architecture (SA) 智能体的物化输出表。

**表数量**：13 张

| 表名 | 列数 | 用途 |
|---|---:|---|
| `sa_data_dictionary` | 35 | **核心**：SA 数据字典（5 个 incoming FK，最多被引用） |
| `sa_business_process` | 30 | 业务流程 |
| `sa_dfd` | 31 | 数据流图 |
| `sa_scope` | 25 | 范围定义 |
| `sa_pspec` | 25 | Process Spec |
| `sa_decision_table` | 30 | 决策表 |
| `sa_state_machine` | 30 | 状态机 |
| `sa_er` | 29 | ER 图 |
| `sa_ui` | 28 | UI 设计 |
| `sa_quality_score` | 12 | 质量评分 |
| `sa_consistency` | 11 | 一致性检查 |
| `sa_assumptions` | 12 | 假设 |
| `sa_validation_log` | 13 | 验证日志 |

**`sa_data_dictionary` 关键设计**：
- 使用 Triple-Key（tenant_id, project_id, pipeline_id）
- 使用 `is_current` / `valid_from` / `valid_to`（**SCD Type 2** 模式）
- 使用 `is_deleted`（bit 类型，与 JNPF 主库 `int` 模式不同 — **模式分歧**）
- 5 个 incoming FK（最高被引用）
- 命名风格：无 `F_` 前缀（小写）

**SA 中心节点关系图**：
```
                   sa_data_dictionary
                   ┌──────┼──────┐
                   │      │      │
            sa_decision_table  sa_er  sa_state_machine
                   │             │      │
                   └── sa_pspec ─┘      │
                         │             │
                    sa_business_process
                         │
                       sa_dfd
                         │
                       sa_scope
```

### 3.7 inteAssistant-KG（知识图谱）

**表数量**：2 张主要表

| 表名 | 列数 | 用途 |
|---|---:|---|
| `kg_pattern` | 18 | **核心**：模式库（pattern mining） |
| `kg_pattern_usage` | 6 | 模式使用记录 |

**`kg_pattern` 关键字段**：
- `pattern_type` / `industry` — 模式分类与行业
- `pattern_content` — 模式内容
- `pattern_tags` — 模式标签
- `score` — 评分
- `usage_count` / `success_count` — 使用统计
- `is_active` / `is_locked` — 状态

### 3.8 inteAssistant-IR / Studio

| 表名 | 列数 | 用途 |
|---|---:|---|
| `BASE_IR_EDIT_PATCH` | 17 | IR 编辑 Patch |
| `BASE_IR_VERSION` | 21 | IR 版本 |
| `BASE_STUDIO_MENU` | 19 | Studio 菜单 |
| `BASE_STUDIO_MENU_BAK_20260617` | 19 | Studio 菜单备份（**OUT_OF_SCOPE**） |

### 3.9 system-warehouse-legacy（仓库管理旧模块）

**表数量**：39 张（`WH_*` 19 + `WM_*` 20）

**特征**：
- **无 tenant_id 列** — 不支持多租户（早期模块遗留）
- 使用 **大写列名**（`ID`、`BillCode` 等）
- 数据量大（部分表 1500+ 行）

**核心表**：
| 表名 | 列数 | 用途 |
|---|---:|---|
| `WH_Bill` | 16 | 入库单 |
| `WH_BillDetail` | 15 | 入库单明细 |
| `WH_BillAutoID` | 3 | 单据自动编号 |
| `WH_Customer` | 13 | 客户 |
| `WH_CustomerClass` | 4 | 客户分类 |
| `WH_Depot` | 5 | 仓库 |
| `WH_DepotMaterial` | 12 | 仓库物料 |
| `WH_Dept` | 4 | 部门 |
| `WH_Material` | 14 | 物料 |
| `WH_MaterialClass` | 4 | 物料分类 |
| `WH_Project` | 4 | 项目 |
| `WH_RemoveBill` / `WH_RemoveBillDetail` | 10 / 12 | 出库单/明细 |
| `WH_StorageType` | 3 | 存储类型 |
| `WH_Supplier` / `WH_SupplierClass` | 10 / 3 | 供应商 |

### 3.10 system-extension（业务扩展示例）

**表数量**：19 张（`ext_*`）

**特征**：JNPF 内置业务场景示例（CRM/ERP/HR/Document/Email）

**主要表**：
| 表名 | 列数 | 用途 |
|---|---:|---|
| `ext_product` | 38 | 产品 |
| `ext_customer` | 16 | 客户 |
| `ext_order` | 30 | 订单 |
| `ext_order_entry` | 24 | 订单明细 |
| `ext_order_receivable` | 19 | 应收账款 |
| `ext_product_classify` | 12 | 产品分类 |
| `ext_product_entry` | 23 | 产品入库 |
| `ext_product_goods` | 18 | 产品库存 |
| `ext_employee` | 26 | 员工 |
| `ext_work_log` | 17 | 工作日志 |
| `ext_work_log_share` | 13 | 工作日志分享 |
| `ext_document` | 22 | 文档 |
| `ext_document_share` | 13 | 文档分享 |
| `ext_big_data` | 12 | 大数据示例 |
| `ext_email_config` | 21 | 邮件配置 |
| `ext_email_send` | 22 | 发件记录 |
| `ext_email_receive` | 23 | 收件记录 |
| `ext_project_gantt` | 24 | 项目甘特图 |
| `ext_table_example` | 28 | **示例表**（"Example" 后缀明确表示演示） |

**治理状态**：除 `ext_table_example` 外的 18 张为 **SYSTEM_TEMPLATE / CONDITIONAL**。

### 3.11 framework-infrastructure（框架基础设施）

**表数量**：5 张

| 表名 | 用途 |
|---|---|
| `SYS_PROCESSED_EVENT` | 框架事件溯源（用于消息总线） |
| `PROCESSED_EVENT` | 同上（小写别名） |
| `SYS_EVENT_OUTBOX_MESSAGE` | 事件 Outbox 模式（事务消息保证） |
| `undo_log` | Seata/RM 分布式事务 Undo Log |
| `SchemaVersions` | 数据库迁移版本记录（DbUp/Flyway 风格） |

### 3.12 system-demo（演示数据）

**OUT_OF_SCOPE** — 不进入生产重构。

| 表名 | 行数 | 列数 | 用途 |
|---|---:|---:|---|
| `Demo_Order` | 151 | 15 | 演示订单 |
| `Demo_OrderDetail` | 60 | 9 | 演示订单明细 |
| `Demo_ExcelTest` | 3 | 14 | Excel 演示 |
| `student` | 4 | 7 | 学生（教学示例） |

### 3.13 system-legacy-snowflake（Snowflake ID 遗留）

**OUT_OF_SCOPE** — 早期自动生成的测试/历史表。

| 表名 | 列数 | 行数 | 说明 |
|---|---:|---:|---|
| `mt543406707183714245` | 7 | 2 | Snowflake ID 命名 |
| `mt543408365615710149` | 7 | 64 | 同上 |
| `mt543552698159464389` | 7 | 32 | 同上 |
| `mt543668771097673669` | 7 | 2 | 同上 |
| `mt543971603646513093` | 3 | 0 | 同上 |

### 3.14 unknown（`zx_*` 客户定制）

**HUMAN_DECISION 待定**。

| 表名 | 列数 | 行数 | 说明 |
|---|---:|---:|---|
| `zx_sys_config` | 17 | 2 | 推测：客户（"ZXAF" 项目）系统配置 |
| `zx_sys_db` | 8 | 5 | 客户系统数据库连接 |
| `zx_system_db` | 8 | 0 | 客户系统数据库（同上？） |

---

## 第四部分：通用设计模式

### 4.1 CLDS 模式（Create/Last/Delete/Soft）

几乎所有 JNPF 业务表都包含：

```
f_creator_time      -- 创建时间
f_creator_user_id   -- 创建者
f_last_modify_time  -- 最后修改时间
f_last_modify_user_id -- 最后修改者
f_delete_time       -- 删除时间
f_delete_user_id    -- 删除者
f_delete_mark       -- 软删除标记 (0=正常, 1=已删除)
f_sort_code         -- 排序号
f_description       -- 备注
```

**新模块（`BASE_AI_*`）改用大写 `F_*`**：`F_CreatorTime`、`F_DeleteMark` 等。

### 4.2 多租户模式（`f_tenant_id`）

- **约 187 / 289 = 64.7%** 表包含 `f_tenant_id`
- 命名变体：`f_tenant_id` / `F_TenantId` / `F_TENANT_ID`
- 应用层通过 `ITenantFilter` 自动注入租户过滤
- DB 层无 FK，使用应用层管理租户隔离

**无 tenant 表**（约 102 张）：
- `WH_*` / `WM_*` 全部无 tenant
- `sa_*` 全部无 tenant（SA 输出表用 Triple-Key）
- `blade_visual*` 无 tenant（设计器元数据全局共享）
- `Demo_*` 无 tenant
- 部分 `BASE_AI_*` 无 tenant（AI 配置全局）

### 4.3 多态外键模式（Polymorphic Foreign Key）

JNPF 的核心设计模式 — 用 **类型+ID** 模拟 M:N 关系：

```
base_user_relation:
  f_user_id        -- 主体
  f_object_type    -- 'Role' / 'Organize' / 'Position'
  f_object_id      -- 目标对象 ID（无 FK 约束）

base_authorize:
  f_item_type      -- 'module' / 'button' / 'column' / 'form'
  f_item_id
  f_object_type    -- 'user' / 'role'
  f_object_id
```

**优点**：单表覆盖多种关系
**缺点**：DB 层无法强制参照完整性（需要应用层保证）

### 4.4 自引用层级模式（Self-Referential Hierarchy）

`base_organize`、`base_province`、`base_visual_dev` 等使用 `f_parent_id` 自引用：

```
base_organize:
  f_id
  f_parent_id  ───┐
  f_full_name    │
                 ▼ (refers to f_id of same table)
  base_organize (parent)
```

**配套字段**（如 `base_organize`）：`f_organize_id_tree` 物化路径，加速子树查询。

### 4.5 SCD Type 2 模式（Slowly Changing Dimension）

仅 SA 输出表（`sa_*`）使用：

```
sa_data_dictionary:
  is_current  bit           -- 是否当前版本
  valid_from  datetime2      -- 生效开始
  valid_to    datetime2      -- 生效结束（NULL = 当前）
  version     int            -- 版本号
```

**应用层应维护**：
- 新版本：INSERT（valid_from=now, valid_to=NULL, is_current=1）
- 旧版本：UPDATE（valid_to=now, is_current=0）

### 4.6 JSON 字符串模式

大量业务表使用 `nvarchar(MAX)` 存储 JSON 字符串：

```
base_visual_dev.f_form_data          -- 表单 JSON
base_visual_dev.f_column_data        -- 列定义 JSON
flow_task.f_flow_form_data_json      -- 流程表单数据
base_data_interface.f_data_config_json -- 接口配置
ext_table_example.f_postil_json      -- 批注 JSON
ext_table_example.f_sign             -- 签名 JSON
```

**注意事项**：
- DB 层无 JSON 校验（依赖应用层）
- **不能作为索引键列**（超过 900 字节）
- 如需查询 JSON 内容，需在应用层解析或用 `JSON_VALUE` 函数

### 4.7 外键（FK）的特殊模式

**14 个 FK 全部在 inteAssistant 模块内**（SA/KG）：

| FromTable | ToTable |
|---|---|
| `sa_business_process` | `sa_dfd` |
| `sa_data_dictionary` | `sa_dfd` |
| `sa_data_dictionary` | `sa_business_process` |
| `sa_decision_table` | `sa_pspec` |
| `sa_decision_table` | `sa_data_dictionary` |
| `sa_dfd` | `sa_scope` |
| `sa_er` | `sa_data_dictionary` |
| `sa_pspec` | `sa_data_dictionary` |
| `sa_pspec` | `sa_business_process` |
| `sa_state_machine` | `sa_data_dictionary` |
| `sa_state_machine` | `sa_business_process` |
| `sa_ui` | `sa_business_process` |
| `sa_ui` | `sa_data_dictionary` |
| `kg_pattern_usage` | `kg_pattern` |

**业务表（base_*/flow_*/ext_*）0 FK** — 完全应用层管理参照完整性。

---

## 第五部分：生产对象治理（已冻结）

### 5.1 Phase 8 P8-C.1 分类结果

| 分类 | 数量 | 资格 | 处置 |
|---|---:|---|---|
| **A. PRODUCT_CORE** | 206 | IN_SCOPE | 进入生产重构 |
| **B. SYSTEM_TEMPLATE** | 69 | CONDITIONAL | 待用户决定 |
| **C. DEMO_SAMPLE** | 5 | OUT_OF_SCOPE | 跳过 |
| **D. TEST_FIXTURE** | 6 | OUT_OF_SCOPE | 跳过 |
| **U. UNKNOWN** | 3 | HUMAN_DECISION | 待分类 |

### 5.2 治理规则

1. **未来批次只接受 A 类表**（必要时 + B 类经批准）
2. **C/D 类表永久跳过**，不进入生产重构
3. **U 类表阻塞批次**，直到用户分类完成
4. **物理表 ≠ 生产表**：289 ≠ 206

### 5.3 进度度量标准

**已废弃指标**（误导性）：
```
94 / 289 = 32.53%   ← 物理库存进度
```

**新指标**（正确）：
```
79 / 206 = 38.35%   ← 生产核心进度
```

---

## 第六部分：附录

### 附录 A：完整表清单（按分类）

#### A.1 PRODUCT_CORE（206 张 IN_SCOPE）

| # | 表名 | 列数 | 行数 | 模块 |
|--:|---|--:|--:|---|
| 1 | ai_entity_field | 26 | 824 | inteAssistant-AI |
| 2 | ai_ir_events | 14 | 3780 | inteAssistant-AI |
| 3 | ai_ir_fragment_snapshots | 13 | 782 | inteAssistant-AI |
| ... | (199 more) | | | |

#### A.2 SYSTEM_TEMPLATE（69 张 CONDITIONAL）

| # | 表名 | 列数 | 行数 |
|--:|---|--:|--:|
| 1 | ext_big_data | 12 | 0 |
| 2 | ext_customer | 16 | 7 |
| 3 | ext_document | 22 | 4 |
| ... | (66 more, incl. 51 wform_*) | | |

#### A.3 DEMO_SAMPLE（5 张 OUT_OF_SCOPE）

| # | 表名 | 列数 | 行数 |
|--:|---|--:|--:|
| 1 | Demo_ExcelTest | 14 | 3 |
| 2 | Demo_Order | 15 | 151 |
| 3 | Demo_OrderDetail | 9 | 60 |
| 4 | ext_table_example | 28 | 33 |
| 5 | student | 7 | 4 |

#### A.4 TEST_FIXTURE（6 张 OUT_OF_SCOPE）

| # | 表名 | 列数 | 行数 |
|--:|---|--:|--:|
| 1 | BASE_STUDIO_MENU_BAK_20260617 | 19 | 54 |
| 2 | mt543406707183714245 | 7 | 2 |
| 3 | mt543408365615710149 | 7 | 64 |
| 4 | mt543552698159464389 | 7 | 32 |
| 5 | mt543668771097673669 | 7 | 2 |
| 6 | mt543971603646513093 | 3 | 0 |

#### A.5 UNKNOWN（3 张 HUMAN_DECISION）

| # | 表名 | 列数 | 行数 |
|--:|---|--:|--:|
| 1 | zx_sys_config | 17 | 2 |
| 2 | zx_sys_db | 8 | 5 |
| 3 | zx_system_db | 8 | 0 |

### 附录 B：术语表（Glossary）

| 术语 | 英文 | 说明 |
|---|---|---|
| **Snowflake ID** | Snowflake ID | 分布式唯一 ID，18-19 位数字字符串 |
| **CLDS** | Create/Last/Delete/Soft | JNPF 标准审计字段集 |
| **SCD Type 2** | Slowly Changing Dimension Type 2 | 保留历史版本的时序数据 |
| **Triple-Key** | (tenant_id, project_id, pipeline_id) | AI 模块的复合主键模式 |
| **多态外键** | Polymorphic Foreign Key | 用 (type, id) 模拟多关系 |
| **IR** | Intermediate Representation | AI 中间表示（设计文档的 JSON 结构） |
| **SA** | Studio Architecture | JNPF 智能体（Studio Architecture） |
| **KG** | Knowledge Graph | 知识图谱 |
| **MCP** | Model Context Protocol | LLM 上下文协议 |
| **Tenant** | Tenant | 多租户隔离单位 |
| **Soft Delete** | Soft Delete | 用标记列代替物理删除 |
| **Master Spec** | Master Specification | Universal Core 主规范 |
| **JNPF Extension** | JNPF Extension | JNPF 平台特有逻辑（与 Universal Core 分开） |

### 附录 C：常见错误避免

| 错误 | 影响 | 正确做法 |
|---|---|---|
| 假设列名是 `f_xxx` | SQL 报错 | 先查 `INFORMATION_SCHEMA.COLUMNS` |
| 假设大小写一致 | WHERE 条件不匹配 | 使用 `COLLATE` 或实际大小写 |
| 给 nvarchar(MAX) 建索引 | 失败 | 用应用层或 `JSON_VALUE` 查询 |
| 在 JSON 列上做 `LIKE '%xxx%'` | 全表扫描 | 应用层解析后查询 |
| 跨租户查询 | 数据泄露 | 必须使用 `ITenantFilter` |
| 修改 `f_delete_mark=1` 的数据 | 软删除失效 | 先恢复或永久删除 |
| 使用 SA 表的 `F_DELETE_MARK int` 模式 | 模式分歧 | SA 表用 `is_deleted bit` |

### 附录 D：版本历史

| 版本 | 日期 | 变更 |
|---|---|---|
| v1.0 | 2026-08-30 | 初版（基于 Phase 8 P8-C.1 分类） |
