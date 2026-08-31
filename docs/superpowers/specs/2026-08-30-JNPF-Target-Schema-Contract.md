# JNPF v5.2 后端 Target Schema Contract

> **Status**: 🚧 DRAFT（2026-08-30，配合 Skill v2.0 升级同步建立）
> **作用**: Skill v2.0 必备输入（IRON-TABLE-03）— 没有 Contract 就不能调用 Skill
> **配套**: [`2026-08-30-表级重构专家Skill-v2.0设计规格.md`](./2026-08-30-表级重构专家Skill-v2.0设计规格.md)
> **覆盖范围**: JNPF v5.2 后端 289 张表（生产范围 274 张 + OUT_OF_SCOPE 14 张 + Demo 1 张）
> **本文件包含**:
>   - §1 Project Default Contract（项目级默认契约，所有表继承）
>   - §2 Module Override Contract（模块级覆盖）
>   - §3 Table-Specific Contract（表级特化 — 仅列代表性 5 张，其余用 v2.0 Skill 自动生成）

---

## 1. Project Default Contract（项目级默认）

```yaml
# jnpf-project-default-contract.yaml
# 适用范围：JNPF v5.2 后端所有生产表（继承此 Contract 后再做表级覆盖）

project: JNPF v5.2
orm: SqlSugar 5.x
db: SQL Server 2019+
low_code_platform: true    # ⚠️ 关键标记：JNPF 是低代码平台

# === 维度 1: 列命名规范 ===
column_naming:
  rule: lowercase_with_f_prefix
  pattern: '^f_[a-z][a-z0_0_]*$'    # 小写字母 + 数字 + 下划线，f_ 前缀
  allowed_examples: [f_id, f_account, f_tenant_id, f_creator_time]
  forbidden_examples: [F_USER_ID, F_UserId, f_user_id, all_caps, no_prefix]
  notes: |
    Phase 8 沉淀的现实：JNPF 实际命名混杂（f_* / F_* / F_* 三种并存）
    Target 目标：统一为 lowercase_with_f_prefix（小写 + f_ 前缀）
    但是：因为 JNPF 是低代码平台，禁止自动改名（IRON-TABLE-02 类型 C）
    → 此 Contract 仅作为**新建表**标准，旧表通过 ALTER TABLE 逐步迁移

# === 维度 2: 数据类型规范 ===
data_type:
  id: nvarchar(50)               # GUID 字符串（JNPF 当前约定，保留）
  code: nvarchar(50)
  name: nvarchar(200)
  description: nvarchar(500)
  short_text: nvarchar(200)
  long_text: nvarchar(max)
  rich_text: nvarchar(max)
  json: nvarchar(max)            # ⚠️ JNPF 暂用 nvarchar(max) 存 JSON，未来可改 jsonb
  amount: decimal(18,4)
  count: int
  flag: int                      # 0/1 布尔
  ratio: decimal(5,4)
  created_time: datetime2(7)     # ⚠️ 新表必须 datetime2(7)，旧表通过 P1 迁移
  date: date
  year_month: nvarchar(7)        # 'YYYY-MM' 格式
  notes: |
    JNPF 当前大量使用 datetime（旧），Target 升级到 datetime2(7)
    但 v2.0 阶段仅规范新表，旧表迁移属 P1 阶段

# === 维度 3: 可空契约 ===
nullable_contract:
  id: NOT NULL
  f_creator_time: NOT NULL DEFAULT GETDATE()
  f_creator_user_id: NOT NULL DEFAULT 'SYSTEM'
  f_last_modify_time: NOT NULL DEFAULT GETDATE()
  f_last_modify_user_id: NOT NULL DEFAULT 'SYSTEM'
  f_delete_mark: NOT NULL DEFAULT 0
  f_enabled_mark: NOT NULL DEFAULT 1
  f_tenant_id: NOT NULL DEFAULT 'DEFAULT_TENANT'   # P0 阶段硬保证
  f_account: NOT NULL
  notes: |
    当前 JNPF 大部分字段可空，违反 SaaS 基本规范
    Target：核心业务字段 NOT NULL，低代码动态字段允许 NULL

# === 维度 4: 租户模型 ===
tenant_model:
  type: SHARED_COLUMN            # 共享列式（不是 SHARED_SCHEMA / SHARED_DB）
  column: f_tenant_id
  isolation_level: STRICT        # 不允许 NULL tenant_id
  filter_required: true          # ITenantFilter 必须启用
  notes: |
    JNPF 当前多租户隔离依赖 ITenantFilter（应用层）+ f_tenant_id 字段
    Target：DB 层 NOT NULL 硬保证 + 应用层 ITenantFilter 冗余
    Phase 1 P0 修复：f_tenant_id NOT NULL
    RLS（Row-Level Security）P3 暂不做（避免过度现代化）

# === 维度 5: 审计模型 ===
audit_model:
  soft_delete: true
  fields:
    created_at: f_creator_time
    created_by: f_creator_user_id
    updated_at: f_last_modify_time
    updated_by: f_last_modify_user_id
    deleted_at: f_delete_time
    deleted_by: f_delete_user_id
    delete_flag: f_delete_mark
  notes: |
    JNPF 已实现软删除（3 字段：f_delete_mark + f_delete_time + f_delete_user_id）
    Target：保留手写软删除，**不引入 Temporal Tables**（P2 暂不做）
    原因：Temporal Tables 与现有 SqlSugar f_delete_mark 写入路径冲突
    + JNPF 软删除已稳定运行多年，不应替换为新方案

# === 维度 6: 索引契约（项目级默认）===
index_contract:
  primary_key: f_id
  default_indexes:
    - name: PK_table_name
      columns: [f_id]
      type: PRIMARY KEY
    - name: IDX_table_tenant_creator
      columns: [f_tenant_id, f_creator_time DESC]
      type: NONCLUSTERED
    - name: IDX_table_tenant_modifier
      columns: [f_tenant_id, f_last_modify_time DESC]
      type: NONCLUSTERED
  notes: |
    JNPF 89 张业务表必备索引：(f_tenant_id, f_creator_time)
    用于：审计列表 + 多租户隔离
    Phase 8 沉淀保留

# === 维度 7: 约束契约（项目级默认）===
constraint_contract:
  foreign_keys: null             # ⚠️ JNPF 不强制 FK，由应用层维护
  check_constraints:
    - name: CK_delete_mark
      expression: f_delete_mark IN (0, 1)
    - name: CK_enabled_mark
      expression: f_enabled_mark IN (0, 1)
  notes: |
    FK 暂不做（P2 延后），原因：
    1. JNPF 业务表之间大量 f_*_id 软引用，硬 FK 会破坏动态配置
    2. 已稳定运行多年，应用层维护完整
    CHECK 约束：仅加最低限度（f_delete_mark / f_enabled_mark）

# === 维度 8: 安全边界（项目级默认）===
security_boundary:
  sensitive_columns:
    - f_password_hash
    - f_secretkey
    - f_mobile_phone
    - f_email
    - f_certificates_number
  encryption:
    at_rest: TDE                  # SQL Server TDE 全库加密（已启用）
    in_transit: TLS               # 连接加密
  notes: |
    JNPF 全库 TDE 已启用
    PII（手机/邮箱/身份证）哈希化暂不做（业务需要明文查询）
    密码字段 P0 必须升级到 PBKDF2

# === Schema Migration Governance（项目级）===
migration_governance:
  type_a_pure_technical: DIRECT_RENAME        # 直接 sp_rename
  type_b_semantic_change: DUAL_WRITE_6_MONTHS  # 双写兼容 6 个月
  type_c_low_code_dynamic: MANUAL_GOVERNANCE  # 禁止自动改名
  low_code_table_patterns:
    - '^wform_'                # 内置流程表单
    - '^ext_'                  # 扩展业务表（部分）
    - '^lowcode_'              # 低代码动态表
  notes: |
    JNPF 是低代码平台，Type C 比例很高
    v2.0 Skill 必须能识别并跳过 Type C 表

# === 范围控制（OUT_OF_SCOPE / LOW_CODE_DYNAMIC）===
out_of_scope:
  - name: ext_table_example
    reason: DEMO_SAMPLE - SVR-001 处置（Phase 8 已识别）
    status: RETAIN_AS_EXCEPTION
```

---

## 2. Module Override Contract（模块级覆盖）

### 2.1 system-core 模块（身份/权限/字典/配置）

```yaml
# system-core-override.yaml
module: system-core
applies_to:
  - base_user / base_organize / base_role
  - base_authorize / base_module / base_module_button
  - base_dictionary_type / base_dictionary_data
  - base_sys_config / base_sys_log

overrides:
  security_boundary:
    priority: P0                 # 最高优先级
    audit_required: true         # 必须完整审计

  index_contract:
    additional_required_indexes:
      base_user:
        - name: UK_tenant_account
          columns: [f_tenant_id, f_account]
          type: UNIQUE
        - name: IDX_user_tenant_organize
          columns: [f_tenant_id, f_organize_id]
          type: NONCLUSTERED
      base_module:
        - name: IDX_module_tenant_parent
          columns: [f_tenant_id, f_parent_id]
          type: NONCLUSTERED

  constraint_contract:
    additional_check_constraints:
      base_user:
        - name: CK_user_enabled_mark
          expression: f_enabled_mark IN (0, 1)
        - name: CK_user_delete_mark
          expression: f_delete_mark IN (0, 1)
        - name: CK_user_is_administrator
          expression: f_is_administrator IN (0, 1)

  notes: |
    system-core 模块是身份/权限基础设施，必须 P0 优先级审计
    Phase 8 已对 system-core 完成索引治理
    P0 修复：base_user 密码升级 + 租户隔离硬保证
```

### 2.2 workflow-engine 模块

```yaml
module: workflow-engine
applies_to:
  - flow_task / flow_task_node / flow_task_operator
  - flow_template / flow_form / flow_delegate
  - flow_candidates

overrides:
  data_type:
    f_flow_form_data_json: nvarchar(max)
    f_flow_template_json: nvarchar(max)
  notes: |
    workflow-engine 大量 JSON 字段，保留 nvarchar(max)
    Phase 8 已对 flow_task_node 等完成索引治理
    flow_task 主表保持 NO-CHANGE（核心表，暂不改）
```

### 2.3 inteAssistant-AI 模块

```yaml
module: inteAssistant-AI
applies_to:
  - BASE_AI_* 系列
  - ai_ir_events / ai_ir_fragment_snapshots / ai_projects
  - sa_assumptions / sa_consistency / sa_quality_score

overrides:
  tenant_model:
    type: TRIPLE_KEY             # (tenant, project, pipeline) 三元组
    columns: [f_tenant_id, F_ProjectId, F_PIPELINE_ID]
    notes: |
      Triple-Key Iron Law（ADR-021）：AI 模块必须三键隔离
      Phase 8 已实施
      v2.0 阶段验证 + 文档化

  column_naming:
    rule: PascalCase_no_prefix    # AI 模块使用 PascalCase（与系统不一致）
    notes: |
      AI 模块历史已使用 PascalCase（F_TenantId / F_ProjectId）
      Target：保留 PascalCase（Type A 拼写错误除外）
      禁止强制改为 lowercase（破坏 Entity 层）
```

### 2.4 warehouse-legacy 模块（高风险 legacy）

```yaml
module: warehouse-legacy
applies_to:
  - WH_Bill / WH_BillDetail / WH_Customer / WH_Material 等 33 张

overrides:
  risk_level: R3+                # 强制高风险
  action: NO-CHANGE              # 默认 NO-CHANGE
  reason: |
    warehouse-legacy 是历史遗留模块（数十年前设计）
    Phase 8 已对 6 张完成索引治理
    剩余 33 张全部 NO-CHANGE（Phase 8 已确认）
    v2.0 P4 阶段建议：迁移到独立归档库 JNPF_Archive_Legacy
  notes: |
    不在 v2.0 阶段强改造
    P3 阶段：归档决策
```

### 2.5 低代码动态表（wform_* / ext_*）

```yaml
module: low-code-dynamic
applies_to:
  - '^wform_'           # 51 张内置流程表单
  - '^ext_'             # 部分扩展业务表（18 张）

overrides:
  migration_governance:
    type: C              # ⚠️ 低代码动态字段，禁止自动改名
    action: MANUAL_GOVERNANCE_REQUIRED
  notes: |
    wform_* 是低代码设计器生成的动态表单字段
    直接 sp_rename 会破坏：
      1. 动态表单配置（field_name）
      2. 动态权限（authorize 关联）
      3. 流程引擎节点（flow_form_data_json 引用）
      4. SQL 生成器（codegen 表）
    v2.0 Skill 必须识别并跳过
```

---

## 3. Table-Specific Contract（表级特化 - 代表性 5 张）

### 3.1 base_user（核心用户表）

```yaml
# base-user-contract.yaml
table_name: base_user
priority: P0_SECURITY
risk_level: R3+
classification: PRODUCT_CORE

target_schema_contract:
  # === 维度 1: 列命名 ===
  column_naming:
    rule: lowercase_with_f_prefix
    exceptions: [f_openId]       # Type A 拼写错误，待 P1 修复

  # === 维度 2: 数据类型 ===
  data_type:
    f_id: nvarchar(50)
    f_account: nvarchar(50)
    f_password: nvarchar(50)     # ⚠️ 当前，过短（暗示 MD5）
    f_secretkey: nvarchar(50)
    f_creator_time: datetime     # ⚠️ 当前，P1 升级到 datetime2(7)

  # === 维度 3: 可空契约 ===
  nullable_contract:
    f_tenant_id: NOT NULL        # ⚠️ P0 必须修复
    f_account: NOT NULL
    f_password: NOT NULL

  # === 维度 4: 租户模型 ===
  tenant_model:
    type: SHARED_COLUMN
    column: f_tenant_id
    isolation_level: STRICT

  # === 维度 5: 审计模型 ===
  audit_model:
    soft_delete: true
    delete_flag_field: f_delete_mark

  # === 维度 6: 索引契约 ===
  index_contract:
    primary_key: f_id
    required_indexes:
      - name: PK_base_user
        columns: [f_id]
        type: PRIMARY KEY
      - name: UK_base_user_tenant_account    # ⚠️ P0 必须新增
        columns: [f_tenant_id, f_account]
        type: UNIQUE

  # === 维度 7: 约束契约 ===
  constraint_contract:
    check_constraints:
      - name: CK_base_user_enabled_mark
        expression: f_enabled_mark IN (0, 1)
      - name: CK_base_user_delete_mark
        expression: f_delete_mark IN (0, 1)

  # === 维度 8: 安全边界 ===
  security_boundary:
    priority: P0_SECURITY
    sensitive_columns:
      - f_password               # ⚠️ P0 升级到 f_password_hash + f_password_algo
      - f_secretkey
    password_storage:
      current: PLAIN_OR_WEAK_HASH   # ⚠️ 推断（P0 验证）
      target: PBKDF2_SHA256         # ⚠️ P0 升级
      target_field: f_password_hash
      target_algo_field: f_password_algo
      forbidden: [MD5, SHA1]

migration_plan:
  type_b_changes:
    - field: f_password
      target: [f_password_hash, f_password_algo, f_password_updated_at, f_password_version]
      strategy: DUAL_WRITE_6_MONTHS
      reason: 业务字段语义变化（加密哈希 + 算法标识 + 时间戳）
    - field: f_openId
      target: f_open_id
      strategy: DIRECT_RENAME    # Type A 拼写错误
      reason: 纯技术命名错误

gap_analysis:
  schema_change_count_estimated: 6    # 4 字段 + 1 拼写 + 1 唯一约束
  no_change_evidence_required:
    - column_naming: PASS（除 f_openId）
    - data_type: PARTIAL（datetime 待 P1）
    - nullable_contract: FAIL（f_tenant_id NULL）
    - tenant_model: FAIL（缺 NOT NULL）
    - audit_model: PASS
    - index_contract: FAIL（缺 UK_tenant_account）
    - constraint_contract: PARTIAL
    - security_boundary: FAIL（密码字段过短）

current_state: PARTIAL_COMPLIANT
target_state: FULL_COMPLIANT
verdict: REFACTORED                  # 必须实际重构
```

---

### 3.2 base_message（消息表）

```yaml
table_name: base_message
priority: P1_BUSINESS
risk_level: R2
classification: PRODUCT_CORE

target_schema_contract:
  column_naming:
    rule: lowercase_with_f_prefix
    pass: true
  data_type:
    f_id: nvarchar(50)
    f_user_id: nvarchar(50)
    f_body_text: nvarchar(max)        # ⚠️ nvarchar(max) 不能索引
    f_creator_time: datetime          # ⚠️ P1 升级
  nullable_contract:
    f_tenant_id: NOT NULL             # ⚠️ P0
    f_user_id: NOT NULL
    f_is_read: NOT NULL DEFAULT 0
  tenant_model:
    type: SHARED_COLUMN
    column: f_tenant_id
  index_contract:
    primary_key: f_id
    required_indexes:
      - name: IDX_message_tenant_user_readtime
        columns: [f_tenant_id, f_user_id, f_is_read, f_creator_time DESC]
        type: NONCLUSTERED
        note: Phase 8 已添加（IDX_MESSAGE_USER_READ = batch-18）
      - name: IDX_message_tenant_creator
        columns: [f_tenant_id, f_creator_time DESC]
        type: NONCLUSTERED

migration_plan:
  type_a_changes: []
  type_b_changes: []
  type_c_changes: []

gap_analysis:
  schema_change_count_estimated: 0
  no_change_evidence_required:
    - column_naming: PASS
    - data_type: PARTIAL（datetime 待 P1）
    - nullable_contract: FAIL（f_tenant_id NULL）
    - tenant_model: FAIL
    - audit_model: PASS
    - index_contract: PASS（Phase 8 已完成）
    - constraint_contract: PARTIAL
    - security_boundary: N/A

current_state: PARTIAL_COMPLIANT
target_state: FULL_COMPLIANT
verdict: REFACTORED                  # 需要补 f_tenant_id NOT NULL
```

---

### 3.3 flow_task（流程任务主表）

```yaml
table_name: flow_task
priority: P1_BUSINESS
risk_level: R3+
classification: PRODUCT_CORE

target_schema_contract:
  column_naming:
    rule: lowercase_with_f_prefix
    pass: true
  data_type:
    f_id: nvarchar(50)
    f_flow_form_data_json: nvarchar(max)
    f_flow_template_json: nvarchar(max)
    f_current_node_code: nvarchar(2000)
    f_current_node_name: nvarchar(2000)
    f_creator_time: datetime
  nullable_contract:
    f_tenant_id: NOT NULL
    f_flow_id: NOT NULL
    f_status: NOT NULL
  tenant_model:
    type: SHARED_COLUMN
    column: f_tenant_id

migration_plan:
  type_a_changes: []
  type_b_changes: []
  type_c_changes:
    - field: ALL
      reason: 流程任务主表，索引已覆盖，暂不重构
      strategy: NO_CHANGE_PROTECTED

gap_analysis:
  schema_change_count_estimated: 0
  no_change_evidence:
    column_naming: PASS
    data_type: PARTIAL（datetime 待 P1）
    nullable_contract: PARTIAL（f_tenant_id NULL）
    tenant_model: PARTIAL
    audit_model: PASS
    index_contract: PASS（Phase 8 已 NO-CHANGE 确认）
    constraint_contract: PARTIAL
    security_boundary: N/A

current_state: PARTIAL_COMPLIANT
verdict: NO-CHANGE                   # ⚠️ IRON-TABLE-01 必须 8 维度证据
evidence: |
  Phase 8 Batch 10 已确认：
    - 已有 4 个索引覆盖 (f_tenant_id, f_flow_id), (f_status), (f_creator_time), (f_last_modify_time)
    - 当前查询性能可接受
  但 NO-CHANGE 必须同时证明：
    - f_tenant_id NOT NULL 后仍可接受（依赖 P0 修复后验证）
    - datetime → datetime2 升级后仍可接受（依赖 P1 迁移后验证）
```

---

### 3.4 wform_contractapproval（低代码动态表）

```yaml
table_name: wform_contractapproval
priority: P3_LOW_CODE
classification: LOW_CODE_DYNAMIC

migration_governance:
  type: C                              # ⚠️ 低代码动态字段
  action: MANUAL_GOVERNANCE_REQUIRED
  reason: |
    wform_* 是低代码设计器生成的动态表单
    字段由配置驱动，DB 仅存储
    直接 sp_rename 会破坏：
      1. 动态表单 field_name 配置
      2. 动态权限 authorize 关联
      3. 流程引擎 flow_form_data_json
      4. SQL 生成器 codegen
  notes: |
    Phase 8 Batch 13 部分治理（F_ApplyUser → F_InputPerson 实际是 Type B 业务字段语义变化）
    v2.0 Skill 必须能识别并跳过

target_schema_contract:
  classification: LOW_CODE_DYNAMIC
  migration_strategy: MANUAL_GOVERNANCE_REQUIRED

gap_analysis:
  schema_change_count_estimated: 0
  verdict: SKIP_LOW_CODE_DYNAMIC
  evidence: |
    Type C 判定：表名以 wform_ 开头
    字段名由低代码配置管理
    不在 v2.0 Skill 自动处理范围
```

---

### 3.5 base_message（性能基准示例）

```yaml
table_name: base_message
priority: P1_BUSINESS
performance_measurement:
  before:
    query: |
      SELECT TOP 100 *
      FROM base_message
      WHERE f_user_id = 'X'
        AND f_is_read = 0
        AND f_delete_mark = 0
      ORDER BY f_creator_time DESC
    logical_reads: 1280
    cpu_ms: 42
    duration_ms: 68
    execution_plan_hash: "..."
  after_phase8:
    timestamp: "2026-08-30 (Phase 8 Batch 18)"
    query: 同上
    logical_reads: 14       # Phase 8 索引生效
    cpu_ms: 3
    duration_ms: 5
    improvement:
      logical_reads_reduction: 98.9%
      duration_reduction: 92.6%
  target:
    logical_reads: <= 14    # 维持当前水平
    duration_ms: <= 5
  notes: |
    Phase 8 已完成性能优化
    v2.0 阶段验证 + 归档 BEFORE/AFTER measurement
```

---

## 4. Contract 自动生成机制（Skill v2.0）

### 4.1 三级 Contract 继承

```
Project Default Contract（§1）
    ↓ 继承
Module Override Contract（§2）
    ↓ 继承
Table-Specific Contract（§3）
```

### 4.2 Skill v2.0 调用流程

```
输入：
  - 当前表 schema（sys.columns, sys.indexes, sys.foreign_keys）
  - Project Default Contract
  - Module Override Contract
  - Table-Specific Contract（如有）

处理：
  - 合并 8 维度 Target Contract
  - 对比 Current Schema vs Target Contract
  - 输出 Gap Analysis Report

Gap 分类：
  - G0 Critical：安全/身份/租户（如 f_tenant_id NULL）
  - G1 Major：结构/类型（如 datetime → datetime2）
  - G2 Minor：性能/索引
  - G3 OK：完全合规
```

### 4.3 当前 JNPF 状态统计（Phase 8 后）

| 维度 | 合规表数 | 部分合规表数 | 不合规表数 |
|------|---------|------------|-----------|
| column_naming | 32 | 16 | 0 |
| data_type | 0 | 89 | 0 |
| nullable_contract | 0 | 89 | 0 |
| tenant_model | 0 | 89 | 0 |
| audit_model | 89 | 0 | 0 |
| index_contract | 89 | 0 | 0 |
| constraint_contract | 0 | 89 | 0 |
| security_boundary | 0 | 5 | 0 |

**结论**：
- audit_model + index_contract：Phase 8 已完成（89 张合规）
- column_naming：32 张完全合规，16 张有 Type A/B 漂移
- 其余维度：全部需要 P0/P1 阶段处理

---

## 5. 后续动作

1. **本 Contract 审批**：用户拍板"接受 / 调整 / 拒绝"
2. **Skill v2.0 集成 Contract 模板**：将本文件接入 v2.0 SKILL.md
3. **全表 Contract 自动生成**：v2.0 Skill 启动时扫描 289 张表，自动生成 Contract
4. **v2.0 R2-COMP 验证**：用 10 张代表性表验证 Gap Analysis 准确性
5. **v2.0 FROZEN**：ADR-024 + Skill v2.0 成为默认

---

**版本**：v0.1（初稿，2026-08-30）
**作者**：AI Engineer + Chief Architect
**配套**：[`2026-08-30-表级重构专家Skill-v2.0设计规格.md`](./2026-08-30-表级重构专家Skill-v2.0设计规格.md)
**下一步**：用户审批 → Skill v2.0 集成 → 全表 Contract 自动生成