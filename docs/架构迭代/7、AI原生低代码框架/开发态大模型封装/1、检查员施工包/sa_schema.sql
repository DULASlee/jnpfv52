-- =====================================================
-- SA 流水线数据库 Schema (10 张表完整版)
-- 数据库: SQL Server 2016+  (PostgreSQL 兼容注释在每段末尾)
-- 编码: UTF-8
-- 创建日期: 2026-06-19
-- 描述: SA 流水线 9 步 + validation_log 持久化,含三级分层、版本管理、外键链
-- =====================================================

-- ============================================================
-- 准备工作: 创建 schema (避免与业务表冲突)
-- ============================================================
-- CREATE SCHEMA sa;  -- SQL Server 可选,PostgreSQL 推荐
-- USE sa;             -- SQL Server

-- ============================================================
-- 表 1: sa_scope (Step 1 - 边界与事件提取)
-- ============================================================
IF OBJECT_ID('sa_scope', 'U') IS NOT NULL DROP TABLE sa_scope;
GO

CREATE TABLE sa_scope (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    -- ① 三级分层字段
    tenant_id      NVARCHAR(50) NOT NULL,
    project_id     BIGINT       NOT NULL,
    asset_level    NVARCHAR(20) NOT NULL,
    event_id       BIGINT       NULL,
    CONSTRAINT CK_sa_scope_level CHECK (asset_level IN ('PROJECT','EVENT','PROCESS')),

    -- ② 版本管理 (SCD Type 2)
    version        INT          NOT NULL DEFAULT 1,
    is_current     BIT          NOT NULL DEFAULT 1,
    valid_from     DATETIME2    NOT NULL DEFAULT GETDATE(),
    valid_to       DATETIME2    NULL,

    -- ③ 核心产出 (JSON)
    system_boundary    NVARCHAR(MAX) NOT NULL,  -- {inScope:[...], outOfScope:[...]}
    external_entities  NVARCHAR(MAX) NOT NULL,  -- [{name, type, description}]
    business_events    NVARCHAR(MAX) NOT NULL,  -- [{id, name, complexity, description}]
    event_count        INT           NOT NULL,

    -- ④ 校验状态
    validation_status  NVARCHAR(20)  NOT NULL DEFAULT 'PENDING',
    validation_errors  NVARCHAR(MAX) NULL,
    CONSTRAINT CK_sa_scope_status CHECK (validation_status IN ('PASS','FAIL','PENDING')),

    -- ⑤ 质量信号 (用于 KG/DM 提取)
    human_confirmed    BIT           NOT NULL DEFAULT 0,
    llm_confidence     DECIMAL(3,2)  NULL,
    tags               NVARCHAR(MAX) NULL,   -- ["MES","机加工"]

    -- ⑥ 审计字段
    created_at   DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by   NVARCHAR(50) NOT NULL,
    updated_at   DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_by   NVARCHAR(50) NOT NULL,
    is_deleted   BIT       NOT NULL DEFAULT 0,
    deleted_at   DATETIME2 NULL
);
GO

CREATE INDEX idx_sa_scope_tenant        ON sa_scope(tenant_id);
CREATE INDEX idx_sa_scope_project       ON sa_scope(project_id);
CREATE INDEX idx_sa_scope_event         ON sa_scope(event_id) WHERE event_id IS NOT NULL;
CREATE INDEX idx_sa_scope_level         ON sa_scope(asset_level);
CREATE INDEX idx_sa_scope_validation    ON sa_scope(validation_status);
CREATE INDEX idx_sa_scope_current       ON sa_scope(project_id, is_current);
CREATE INDEX idx_sa_scope_composite     ON sa_scope(tenant_id, project_id, asset_level, event_id, is_current);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Step 1: 边界与事件提取 - 整个 SA 流水线的入口', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'sa_scope';
GO


-- ============================================================
-- 表 2: sa_dfd (Step 2 - DFD 分层)
-- ============================================================
IF OBJECT_ID('sa_dfd', 'U') IS NOT NULL DROP TABLE sa_dfd;
GO

CREATE TABLE sa_dfd (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    tenant_id      NVARCHAR(50) NOT NULL,
    project_id     BIGINT       NOT NULL,
    asset_level    NVARCHAR(20) NOT NULL,
    event_id       BIGINT       NULL,
    CONSTRAINT CK_sa_dfd_level CHECK (asset_level IN ('PROJECT','EVENT','PROCESS')),

    version        INT          NOT NULL DEFAULT 1,
    is_current     BIT          NOT NULL DEFAULT 1,
    valid_from     DATETIME2    NOT NULL DEFAULT GETDATE(),
    valid_to       DATETIME2    NULL,

    -- 强外键:Step 2 必须引用 Step 1
    scope_id       BIGINT       NOT NULL,

    -- 核心产出
    context_diagram   NVARCHAR(MAX) NOT NULL,   -- 顶层 Context 图
    dfd_levels        NVARCHAR(MAX) NOT NULL,   -- {level_0:{...}, level_1:{...}}
    processes         NVARCHAR(MAX) NOT NULL,   -- [{id, name, inputFlows, outputFlows, parentId}]
    data_flows        NVARCHAR(MAX) NOT NULL,   -- [{name, fields:[...]}]
    data_stores       NVARCHAR(MAX) NOT NULL,   -- [{name, fields:[...]}]

    -- 校验结果 (来自 DFDValidator)
    balance_check        BIT          NULL,
    conservation_check   BIT          NULL,
    validation_status    NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    validation_errors    NVARCHAR(MAX) NULL,
    CONSTRAINT CK_sa_dfd_status CHECK (validation_status IN ('PASS','FAIL','PENDING')),

    human_confirmed BIT NOT NULL DEFAULT 0,
    llm_confidence  DECIMAL(3,2) NULL,
    tags            NVARCHAR(MAX) NULL,
    is_pattern_source BIT NOT NULL DEFAULT 0,
    pattern_tags    NVARCHAR(MAX) NULL,

    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by NVARCHAR(50) NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_by NVARCHAR(50) NOT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2 NULL,

    CONSTRAINT FK_sa_dfd_scope FOREIGN KEY (scope_id) REFERENCES sa_scope(id)
);
GO

CREATE INDEX idx_sa_dfd_scope        ON sa_dfd(scope_id);
CREATE INDEX idx_sa_dfd_tenant       ON sa_dfd(tenant_id);
CREATE INDEX idx_sa_dfd_project      ON sa_dfd(project_id);
CREATE INDEX idx_sa_dfd_validation   ON sa_dfd(validation_status);
CREATE INDEX idx_sa_dfd_composite    ON sa_dfd(tenant_id, project_id, asset_level, event_id, is_current);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Step 2: DFD 分层 - 强外键到 sa_scope', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'sa_dfd';
GO


-- ============================================================
-- 表 3: sa_business_process (Step 3 - 业务流程图)
-- ============================================================
IF OBJECT_ID('sa_business_process', 'U') IS NOT NULL DROP TABLE sa_business_process;
GO

CREATE TABLE sa_business_process (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    tenant_id      NVARCHAR(50) NOT NULL,
    project_id     BIGINT       NOT NULL,
    asset_level    NVARCHAR(20) NOT NULL,
    event_id       BIGINT       NULL,
    CONSTRAINT CK_sa_bpm_level CHECK (asset_level IN ('PROJECT','EVENT','PROCESS')),

    version        INT          NOT NULL DEFAULT 1,
    is_current     BIT          NOT NULL DEFAULT 1,
    valid_from     DATETIME2    NOT NULL DEFAULT GETDATE(),
    valid_to       DATETIME2    NULL,

    -- 强外键
    dfd_id         BIGINT       NOT NULL,

    -- 核心产出
    swim_lanes           NVARCHAR(MAX) NOT NULL,  -- [{laneId, role, name}]
    activity_nodes       NVARCHAR(MAX) NOT NULL,  -- [{id, name, laneId, dfdProcessId, type}]
    edges                NVARCHAR(MAX) NOT NULL,  -- [{from, to, label}]
    exception_paths      NVARCHAR(MAX) NULL,      -- [{from, to, condition}]

    -- BPM 节点 → DFD 过程映射(必填,Validator 检查)
    dfd_process_mappings NVARCHAR(MAX) NOT NULL,  -- {bpmNodeId: dfdProcessId}
    mapping_validation    BIT          NULL,

    validation_status    NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    validation_errors    NVARCHAR(MAX) NULL,
    CONSTRAINT CK_sa_bpm_status CHECK (validation_status IN ('PASS','FAIL','PENDING')),

    human_confirmed BIT NOT NULL DEFAULT 0,
    llm_confidence  DECIMAL(3,2) NULL,
    tags            NVARCHAR(MAX) NULL,
    is_pattern_source BIT NOT NULL DEFAULT 0,
    pattern_tags    NVARCHAR(MAX) NULL,

    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by NVARCHAR(50) NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_by NVARCHAR(50) NOT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2 NULL,

    CONSTRAINT FK_sa_bpm_dfd FOREIGN KEY (dfd_id) REFERENCES sa_dfd(id)
);
GO

CREATE INDEX idx_sa_bpm_dfd          ON sa_business_process(dfd_id);
CREATE INDEX idx_sa_bpm_tenant       ON sa_business_process(tenant_id);
CREATE INDEX idx_sa_bpm_project      ON sa_business_process(project_id);
CREATE INDEX idx_sa_bpm_validation   ON sa_business_process(validation_status);
CREATE INDEX idx_sa_bpm_composite    ON sa_business_process(tenant_id, project_id, asset_level, event_id, is_current);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Step 3: 业务流程图(泳道) - 强外键到 sa_dfd', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'sa_business_process';
GO


-- ============================================================
-- 表 4: sa_data_dictionary (Step 4 - 数据字典,★ 最关键)
-- ============================================================
IF OBJECT_ID('sa_data_dictionary', 'U') IS NOT NULL DROP TABLE sa_data_dictionary;
GO

CREATE TABLE sa_data_dictionary (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    tenant_id      NVARCHAR(50) NOT NULL,
    project_id     BIGINT       NOT NULL,
    asset_level    NVARCHAR(20) NOT NULL,
    event_id       BIGINT       NULL,
    CONSTRAINT CK_sa_dict_level CHECK (asset_level IN ('PROJECT','EVENT','PROCESS')),

    version        INT          NOT NULL DEFAULT 1,
    is_current     BIT          NOT NULL DEFAULT 1,
    valid_from     DATETIME2    NOT NULL DEFAULT GETDATE(),
    valid_to       DATETIME2    NULL,

    -- 强外键(同时引用 DFD 和 BPM)
    dfd_id         BIGINT       NOT NULL,
    bpm_id         BIGINT       NOT NULL,

    -- 核心产出
    elements          NVARCHAR(MAX) NOT NULL,  -- 字段级 [{name, type, length, isFK, refEntity, isRequired, scope}]
    data_structures   NVARCHAR(MAX) NOT NULL,  -- 记录级 [{name, fields:[...]}]
    data_flows        NVARCHAR(MAX) NOT NULL,  -- [{name, fields:[...]}]
    data_stores       NVARCHAR(MAX) NOT NULL,  -- [{name, fields:[...]}]

    -- 字段级校验(来自 DictValidator)
    type_check           BIT          NULL,
    length_check         BIT          NULL,
    constraint_check     BIT          NULL,
    fk_reference_check   BIT          NULL,
    has_tenant_id        BIT          NULL,
    has_audit_fields     BIT          NULL,

    validation_status    NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    validation_errors    NVARCHAR(MAX) NULL,
    CONSTRAINT CK_sa_dict_status CHECK (validation_status IN ('PASS','FAIL','PENDING')),

    -- KG 提取关键字段
    human_confirmed  BIT NOT NULL DEFAULT 0,
    llm_confidence   DECIMAL(3,2) NULL,
    tags             NVARCHAR(MAX) NULL,
    is_pattern_source BIT NOT NULL DEFAULT 0,
    pattern_tags     NVARCHAR(MAX) NULL,

    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by NVARCHAR(50) NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_by NVARCHAR(50) NOT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2 NULL,

    CONSTRAINT FK_sa_dict_dfd FOREIGN KEY (dfd_id) REFERENCES sa_dfd(id),
    CONSTRAINT FK_sa_dict_bpm FOREIGN KEY (bpm_id) REFERENCES sa_business_process(id)
);
GO

CREATE INDEX idx_sa_dict_dfd         ON sa_data_dictionary(dfd_id);
CREATE INDEX idx_sa_dict_bpm         ON sa_data_dictionary(bpm_id);
CREATE INDEX idx_sa_dict_tenant      ON sa_data_dictionary(tenant_id);
CREATE INDEX idx_sa_dict_project     ON sa_data_dictionary(project_id);
CREATE INDEX idx_sa_dict_validation  ON sa_data_dictionary(validation_status);
CREATE INDEX idx_sa_dict_pattern_src  ON sa_data_dictionary(is_pattern_source) WHERE is_pattern_source = 1;
CREATE INDEX idx_sa_dict_composite   ON sa_data_dictionary(tenant_id, project_id, asset_level, event_id, is_current);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Step 4: 数据字典(最关键) - KG/DM 提取核心原料,强外键到 sa_dfd + sa_business_process', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'sa_data_dictionary';
GO


-- ============================================================
-- 表 5: sa_pspec (Step 5 - PSPEC 原子过程伪代码)
-- ============================================================
IF OBJECT_ID('sa_pspec', 'U') IS NOT NULL DROP TABLE sa_pspec;
GO

CREATE TABLE sa_pspec (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    tenant_id      NVARCHAR(50) NOT NULL,
    project_id     BIGINT       NOT NULL,
    asset_level    NVARCHAR(20) NOT NULL,
    event_id       BIGINT       NULL,
    CONSTRAINT CK_sa_pspec_level CHECK (asset_level IN ('PROJECT','EVENT','PROCESS')),

    version        INT          NOT NULL DEFAULT 1,
    is_current     BIT          NOT NULL DEFAULT 1,
    valid_from     DATETIME2    NOT NULL DEFAULT GETDATE(),
    valid_to       DATETIME2    NULL,

    dict_id        BIGINT       NOT NULL,
    bpm_id         BIGINT       NOT NULL,

    process_specs      NVARCHAR(MAX) NOT NULL,  -- [{id, name, input, output, validation, algorithm}]
    field_reference_check BIT        NULL,     -- 字段引用是否都在 dict 里

    validation_status  NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    validation_errors  NVARCHAR(MAX) NULL,
    CONSTRAINT CK_sa_pspec_status CHECK (validation_status IN ('PASS','FAIL','PENDING')),

    human_confirmed BIT NOT NULL DEFAULT 0,
    llm_confidence  DECIMAL(3,2) NULL,
    tags            NVARCHAR(MAX) NULL,

    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by NVARCHAR(50) NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_by NVARCHAR(50) NOT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2 NULL,

    CONSTRAINT FK_sa_pspec_dict FOREIGN KEY (dict_id) REFERENCES sa_data_dictionary(id),
    CONSTRAINT FK_sa_pspec_bpm  FOREIGN KEY (bpm_id)  REFERENCES sa_business_process(id)
);
GO

CREATE INDEX idx_sa_pspec_dict        ON sa_pspec(dict_id);
CREATE INDEX idx_sa_pspec_bpm         ON sa_pspec(bpm_id);
CREATE INDEX idx_sa_pspec_tenant      ON sa_pspec(tenant_id);
CREATE INDEX idx_sa_pspec_project     ON sa_pspec(project_id);
CREATE INDEX idx_sa_pspec_validation  ON sa_pspec(validation_status);
CREATE INDEX idx_sa_pspec_composite   ON sa_pspec(tenant_id, project_id, asset_level, event_id, is_current);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Step 5: PSPEC 原子过程伪代码 - 强外键到 sa_data_dictionary + sa_business_process', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'sa_pspec';
GO


-- ============================================================
-- 表 6: sa_decision_table (Step 6 - 判定表,★★ 跨事件一致)
-- ============================================================
IF OBJECT_ID('sa_decision_table', 'U') IS NOT NULL DROP TABLE sa_decision_table;
GO

CREATE TABLE sa_decision_table (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    tenant_id      NVARCHAR(50) NOT NULL,
    project_id     BIGINT       NOT NULL,
    asset_level    NVARCHAR(20) NOT NULL,
    event_id       BIGINT       NULL,
    CONSTRAINT CK_sa_dt_level CHECK (asset_level IN ('PROJECT','EVENT','PROCESS')),

    version        INT          NOT NULL DEFAULT 1,
    is_current     BIT          NOT NULL DEFAULT 1,
    valid_from     DATETIME2    NOT NULL DEFAULT GETDATE(),
    valid_to       DATETIME2    NULL,

    pspec_id       BIGINT       NOT NULL,
    dict_id        BIGINT       NOT NULL,

    -- 判定表结构 (JSON)
    tables  NVARCHAR(MAX) NOT NULL,
    -- tables: [
    --   {
    --     "id": "P2.2-报工校验",
    --     "conditions": [{"name":"报废率>5%","operator":">","value":0.05}],
    --     "actions": [{"name":"合格接收"},{"name":"驳回"}],
    --     "rules": [{"conditionMask":[true],"actionIndex":0}]
    --   }
    -- ]

    -- 跨事件一致性(★★)
    cross_event_consistency     BIT          NULL,
    condition_whitelist_check   BIT          NULL,
    completeness_check          BIT          NULL,
    has_default_rule            BIT          NULL,

    validation_status    NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    validation_errors    NVARCHAR(MAX) NULL,
    CONSTRAINT CK_sa_dt_status CHECK (validation_status IN ('PASS','FAIL','PENDING')),

    -- KG 提取关键(判定表是最丰富的 Pattern 来源)
    human_confirmed  BIT NOT NULL DEFAULT 0,
    llm_confidence   DECIMAL(3,2) NULL,
    tags             NVARCHAR(MAX) NULL,
    is_pattern_source BIT NOT NULL DEFAULT 0,
    pattern_tags     NVARCHAR(MAX) NULL,

    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by NVARCHAR(50) NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_by NVARCHAR(50) NOT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2 NULL,

    CONSTRAINT FK_sa_dt_pspec FOREIGN KEY (pspec_id) REFERENCES sa_pspec(id),
    CONSTRAINT FK_sa_dt_dict  FOREIGN KEY (dict_id)  REFERENCES sa_data_dictionary(id)
);
GO

CREATE INDEX idx_sa_dt_pspec        ON sa_decision_table(pspec_id);
CREATE INDEX idx_sa_dt_dict         ON sa_decision_table(dict_id);
CREATE INDEX idx_sa_dt_tenant       ON sa_decision_table(tenant_id);
CREATE INDEX idx_sa_dt_project      ON sa_decision_table(project_id);
CREATE INDEX idx_sa_dt_validation   ON sa_decision_table(validation_status);
CREATE INDEX idx_sa_dt_pattern_src  ON sa_decision_table(is_pattern_source) WHERE is_pattern_source = 1;
CREATE INDEX idx_sa_dt_composite    ON sa_decision_table(tenant_id, project_id, asset_level, event_id, is_current);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Step 6: 判定表(★★ 跨事件一致性) - 强外键到 sa_pspec + sa_data_dictionary,KG Pattern 核心来源', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'sa_decision_table';
GO


-- ============================================================
-- 表 7: sa_er (Step 7 - ER 图 + 3NF)
-- ============================================================
IF OBJECT_ID('sa_er', 'U') IS NOT NULL DROP TABLE sa_er;
GO

CREATE TABLE sa_er (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    tenant_id      NVARCHAR(50) NOT NULL,
    project_id     BIGINT       NOT NULL,
    asset_level    NVARCHAR(20) NOT NULL,
    event_id       BIGINT       NULL,
    CONSTRAINT CK_sa_er_level CHECK (asset_level IN ('PROJECT','EVENT','PROCESS')),

    version        INT          NOT NULL DEFAULT 1,
    is_current     BIT          NOT NULL DEFAULT 1,
    valid_from     DATETIME2    NOT NULL DEFAULT GETDATE(),
    valid_to       DATETIME2    NULL,

    dict_id        BIGINT       NOT NULL,

    entities          NVARCHAR(MAX) NOT NULL,  -- [{name, columns:[...]}]
    relationships     NVARCHAR(MAX) NOT NULL,  -- [{from, to, type, fkColumn}]

    third_normal_form     BIT          NULL,
    fk_in_dict            BIT          NULL,
    no_calculated_columns  BIT          NULL,

    validation_status    NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    validation_errors    NVARCHAR(MAX) NULL,
    CONSTRAINT CK_sa_er_status CHECK (validation_status IN ('PASS','FAIL','PENDING')),

    human_confirmed BIT NOT NULL DEFAULT 0,
    llm_confidence  DECIMAL(3,2) NULL,
    tags            NVARCHAR(MAX) NULL,
    is_pattern_source BIT NOT NULL DEFAULT 0,
    pattern_tags    NVARCHAR(MAX) NULL,

    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by NVARCHAR(50) NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_by NVARCHAR(50) NOT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2 NULL,

    CONSTRAINT FK_sa_er_dict FOREIGN KEY (dict_id) REFERENCES sa_data_dictionary(id)
);
GO

CREATE INDEX idx_sa_er_dict         ON sa_er(dict_id);
CREATE INDEX idx_sa_er_tenant       ON sa_er(tenant_id);
CREATE INDEX idx_sa_er_project      ON sa_er(project_id);
CREATE INDEX idx_sa_er_validation   ON sa_er(validation_status);
CREATE INDEX idx_sa_er_composite    ON sa_er(tenant_id, project_id, asset_level, event_id, is_current);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Step 7: ER 图 + 3NF - 强外键到 sa_data_dictionary', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'sa_er';
GO


-- ============================================================
-- 表 8: sa_state_machine (Step 8 - 状态机 STD)
-- ============================================================
IF OBJECT_ID('sa_state_machine', 'U') IS NOT NULL DROP TABLE sa_state_machine;
GO

CREATE TABLE sa_state_machine (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    tenant_id      NVARCHAR(50) NOT NULL,
    project_id     BIGINT       NOT NULL,
    asset_level    NVARCHAR(20) NOT NULL,
    event_id       BIGINT       NULL,
    CONSTRAINT CK_sa_std_level CHECK (asset_level IN ('PROJECT','EVENT','PROCESS')),

    version        INT          NOT NULL DEFAULT 1,
    is_current     BIT          NOT NULL DEFAULT 1,
    valid_from     DATETIME2    NOT NULL DEFAULT GETDATE(),
    valid_to       DATETIME2    NULL,

    dict_id        BIGINT       NOT NULL,
    bpm_id         BIGINT       NOT NULL,

    state_machines  NVARCHAR(MAX) NOT NULL,
    -- state_machines: [
    --   {
    --     "entity": "ProductionReport",
    --     "states": ["待校验","待终核","已归档","已驳回"],
    --     "transitions": [{"from":"待校验","to":"待终核","trigger":"初核通过"}]
    --   }
    -- ]

    states_in_dict             BIT          NULL,
    bpm_state_change_match     BIT          NULL,
    reachability_check         BIT          NULL,
    dead_end_check             BIT          NULL,

    validation_status    NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    validation_errors    NVARCHAR(MAX) NULL,
    CONSTRAINT CK_sa_std_status CHECK (validation_status IN ('PASS','FAIL','PENDING')),

    human_confirmed  BIT NOT NULL DEFAULT 0,
    llm_confidence   DECIMAL(3,2) NULL,
    tags             NVARCHAR(MAX) NULL,
    is_pattern_source BIT NOT NULL DEFAULT 0,
    pattern_tags     NVARCHAR(MAX) NULL,

    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by NVARCHAR(50) NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_by NVARCHAR(50) NOT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2 NULL,

    CONSTRAINT FK_sa_std_dict FOREIGN KEY (dict_id) REFERENCES sa_data_dictionary(id),
    CONSTRAINT FK_sa_std_bpm  FOREIGN KEY (bpm_id)  REFERENCES sa_business_process(id)
);
GO

CREATE INDEX idx_sa_std_dict        ON sa_state_machine(dict_id);
CREATE INDEX idx_sa_std_bpm         ON sa_state_machine(bpm_id);
CREATE INDEX idx_sa_std_tenant      ON sa_state_machine(tenant_id);
CREATE INDEX idx_sa_std_project     ON sa_state_machine(project_id);
CREATE INDEX idx_sa_std_validation  ON sa_state_machine(validation_status);
CREATE INDEX idx_sa_std_composite   ON sa_state_machine(tenant_id, project_id, asset_level, event_id, is_current);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Step 8: 状态机 STD - 强外键到 sa_data_dictionary + sa_business_process', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'sa_state_machine';
GO


-- ============================================================
-- 表 9: sa_ui (Step 9 - UI 原型)
-- ============================================================
IF OBJECT_ID('sa_ui', 'U') IS NOT NULL DROP TABLE sa_ui;
GO

CREATE TABLE sa_ui (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    tenant_id      NVARCHAR(50) NOT NULL,
    project_id     BIGINT       NOT NULL,
    asset_level    NVARCHAR(20) NOT NULL,
    event_id       BIGINT       NULL,
    CONSTRAINT CK_sa_ui_level CHECK (asset_level IN ('PROJECT','EVENT','PROCESS')),

    version        INT          NOT NULL DEFAULT 1,
    is_current     BIT          NOT NULL DEFAULT 1,
    valid_from     DATETIME2    NOT NULL DEFAULT GETDATE(),
    valid_to       DATETIME2    NULL,

    bpm_id         BIGINT       NOT NULL,
    dict_id        BIGINT       NOT NULL,

    screens                NVARCHAR(MAX) NOT NULL,  -- [{id, name, dataFlow, bpmNodeId, fields:[...]}]
    field_to_dict_mapping  NVARCHAR(MAX) NOT NULL,  -- {uiFieldName: dictFieldName}

    ui_fields_in_dict          BIT          NULL,
    no_extra_fields            BIT          NULL,
    event_to_screen_mapping    BIT          NULL,

    validation_status    NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    validation_errors    NVARCHAR(MAX) NULL,
    CONSTRAINT CK_sa_ui_status CHECK (validation_status IN ('PASS','FAIL','PENDING')),

    human_confirmed BIT NOT NULL DEFAULT 0,
    llm_confidence  DECIMAL(3,2) NULL,
    tags            NVARCHAR(MAX) NULL,

    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    created_by NVARCHAR(50) NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_by NVARCHAR(50) NOT NULL,
    is_deleted BIT NOT NULL DEFAULT 0,
    deleted_at DATETIME2 NULL,

    CONSTRAINT FK_sa_ui_bpm  FOREIGN KEY (bpm_id)  REFERENCES sa_business_process(id),
    CONSTRAINT FK_sa_ui_dict FOREIGN KEY (dict_id) REFERENCES sa_data_dictionary(id)
);
GO

CREATE INDEX idx_sa_ui_bpm          ON sa_ui(bpm_id);
CREATE INDEX idx_sa_ui_dict         ON sa_ui(dict_id);
CREATE INDEX idx_sa_ui_tenant       ON sa_ui(tenant_id);
CREATE INDEX idx_sa_ui_project      ON sa_ui(project_id);
CREATE INDEX idx_sa_ui_validation   ON sa_ui(validation_status);
CREATE INDEX idx_sa_ui_composite    ON sa_ui(tenant_id, project_id, asset_level, event_id, is_current);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Step 9: UI 原型 - 强外键到 sa_business_process + sa_data_dictionary', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'sa_ui';
GO


-- ============================================================
-- 表 10: sa_validation_log (校验日志,★ 含重试闭环)
-- ============================================================
IF OBJECT_ID('sa_validation_log', 'U') IS NOT NULL DROP TABLE sa_validation_log;
GO

CREATE TABLE sa_validation_log (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    tenant_id   NVARCHAR(50) NOT NULL,
    project_id  BIGINT       NOT NULL,

    sa_table_name  NVARCHAR(50) NOT NULL,  -- sa_scope / sa_dfd / sa_data_dictionary / ...
    sa_record_id   BIGINT       NOT NULL,
    validator_name NVARCHAR(100) NOT NULL, -- DFDValidator / DictValidator / UIValidator / ...

    -- ★ 重试闭环
    retry_count     INT           NOT NULL DEFAULT 0,
    previous_errors NVARCHAR(MAX) NULL,   -- JSON: 上次错误列表
    is_converged    BIT           NOT NULL DEFAULT 0,

    validation_status NVARCHAR(20) NOT NULL,
    errors            NVARCHAR(MAX) NULL,  -- JSON: 本次错误
    duration_ms       INT          NULL,   -- 校验耗时

    created_at DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

CREATE INDEX idx_sa_vlog_tenant      ON sa_validation_log(tenant_id);
CREATE INDEX idx_sa_vlog_project     ON sa_validation_log(project_id);
CREATE INDEX idx_sa_vlog_table       ON sa_validation_log(sa_table_name, sa_record_id);
CREATE INDEX idx_sa_vlog_converged   ON sa_validation_log(is_converged);
CREATE INDEX idx_sa_vlog_created_at  ON sa_validation_log(created_at DESC);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'校验日志表 - 记录每次 Validator 执行结果,含重试闭环,用于 DKEE 分析 LLM 错误模式', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'sa_validation_log';
GO


-- ============================================================
-- 版本管理触发器(SCD Type 2):同一 key 只能有一条 is_current=1
-- 以 sa_scope 为例,其他 8 张表照搬即可
-- ============================================================

-- sa_scope 版本触发器
IF OBJECT_ID('trg_sa_scope_version', 'TR') IS NOT NULL DROP TRIGGER trg_sa_scope_version;
GO

CREATE TRIGGER trg_sa_scope_version
ON sa_scope
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- 把同 key 的旧版本标记为非当前
    UPDATE s
    SET s.is_current = 0,
        s.valid_to = GETDATE()
    FROM sa_scope s
    INNER JOIN inserted i ON
        s.tenant_id = i.tenant_id
        AND s.project_id = i.project_id
        AND s.asset_level = i.asset_level
        AND ISNULL(s.event_id, 0) = ISNULL(i.event_id, 0)
        AND s.id != i.id
        AND s.is_current = 1
        AND i.is_current = 1;
END;
GO


-- sa_dfd 版本触发器
IF OBJECT_ID('trg_sa_dfd_version', 'TR') IS NOT NULL DROP TRIGGER trg_sa_dfd_version;
GO

CREATE TRIGGER trg_sa_dfd_version
ON sa_dfd
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE s
    SET s.is_current = 0,
        s.valid_to = GETDATE()
    FROM sa_dfd s
    INNER JOIN inserted i ON
        s.tenant_id = i.tenant_id
        AND s.project_id = i.project_id
        AND s.asset_level = i.asset_level
        AND ISNULL(s.event_id, 0) = ISNULL(i.event_id, 0)
        AND s.id != i.id
        AND s.is_current = 1
        AND i.is_current = 1;
END;
GO


-- sa_business_process 版本触发器
IF OBJECT_ID('trg_sa_bpm_version', 'TR') IS NOT NULL DROP TRIGGER trg_sa_bpm_version;
GO

CREATE TRIGGER trg_sa_bpm_version
ON sa_business_process
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE s
    SET s.is_current = 0, s.valid_to = GETDATE()
    FROM sa_business_process s
    INNER JOIN inserted i ON
        s.tenant_id = i.tenant_id
        AND s.project_id = i.project_id
        AND s.asset_level = i.asset_level
        AND ISNULL(s.event_id, 0) = ISNULL(i.event_id, 0)
        AND s.id != i.id AND s.is_current = 1 AND i.is_current = 1;
END;
GO


-- sa_data_dictionary 版本触发器
IF OBJECT_ID('trg_sa_dict_version', 'TR') IS NOT NULL DROP TRIGGER trg_sa_dict_version;
GO

CREATE TRIGGER trg_sa_dict_version
ON sa_data_dictionary
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE s
    SET s.is_current = 0, s.valid_to = GETDATE()
    FROM sa_data_dictionary s
    INNER JOIN inserted i ON
        s.tenant_id = i.tenant_id
        AND s.project_id = i.project_id
        AND s.asset_level = i.asset_level
        AND ISNULL(s.event_id, 0) = ISNULL(i.event_id, 0)
        AND s.id != i.id AND s.is_current = 1 AND i.is_current = 1;
END;
GO


-- sa_pspec 版本触发器
IF OBJECT_ID('trg_sa_pspec_version', 'TR') IS NOT NULL DROP TRIGGER trg_sa_pspec_version;
GO

CREATE TRIGGER trg_sa_pspec_version
ON sa_pspec
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE s
    SET s.is_current = 0, s.valid_to = GETDATE()
    FROM sa_pspec s
    INNER JOIN inserted i ON
        s.tenant_id = i.tenant_id
        AND s.project_id = i.project_id
        AND s.asset_level = i.asset_level
        AND ISNULL(s.event_id, 0) = ISNULL(i.event_id, 0)
        AND s.id != i.id AND s.is_current = 1 AND i.is_current = 1;
END;
GO


-- sa_decision_table 版本触发器
IF OBJECT_ID('trg_sa_dt_version', 'TR') IS NOT NULL DROP TRIGGER trg_sa_dt_version;
GO

CREATE TRIGGER trg_sa_dt_version
ON sa_decision_table
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE s
    SET s.is_current = 0, s.valid_to = GETDATE()
    FROM sa_decision_table s
    INNER JOIN inserted i ON
        s.tenant_id = i.tenant_id
        AND s.project_id = i.project_id
        AND s.asset_level = i.asset_level
        AND ISNULL(s.event_id, 0) = ISNULL(i.event_id, 0)
        AND s.id != i.id AND s.is_current = 1 AND i.is_current = 1;
END;
GO


-- sa_er 版本触发器
IF OBJECT_ID('trg_sa_er_version', 'TR') IS NOT NULL DROP TRIGGER trg_sa_er_version;
GO

CREATE TRIGGER trg_sa_er_version
ON sa_er
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE s
    SET s.is_current = 0, s.valid_to = GETDATE()
    FROM sa_er s
    INNER JOIN inserted i ON
        s.tenant_id = i.tenant_id
        AND s.project_id = i.project_id
        AND s.asset_level = i.asset_level
        AND ISNULL(s.event_id, 0) = ISNULL(i.event_id, 0)
        AND s.id != i.id AND s.is_current = 1 AND i.is_current = 1;
END;
GO


-- sa_state_machine 版本触发器
IF OBJECT_ID('trg_sa_std_version', 'TR') IS NOT NULL DROP TRIGGER trg_sa_std_version;
GO

CREATE TRIGGER trg_sa_std_version
ON sa_state_machine
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE s
    SET s.is_current = 0, s.valid_to = GETDATE()
    FROM sa_state_machine s
    INNER JOIN inserted i ON
        s.tenant_id = i.tenant_id
        AND s.project_id = i.project_id
        AND s.asset_level = i.asset_level
        AND ISNULL(s.event_id, 0) = ISNULL(i.event_id, 0)
        AND s.id != i.id AND s.is_current = 1 AND i.is_current = 1;
END;
GO


-- sa_ui 版本触发器
IF OBJECT_ID('trg_sa_ui_version', 'TR') IS NOT NULL DROP TRIGGER trg_sa_ui_version;
GO

CREATE TRIGGER trg_sa_ui_version
ON sa_ui
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE s
    SET s.is_current = 0, s.valid_to = GETDATE()
    FROM sa_ui s
    INNER JOIN inserted i ON
        s.tenant_id = i.tenant_id
        AND s.project_id = i.project_id
        AND s.asset_level = i.asset_level
        AND ISNULL(s.event_id, 0) = ISNULL(i.event_id, 0)
        AND s.id != i.id AND s.is_current = 1 AND i.is_current = 1;
END;
GO


-- ============================================================
-- KG/DM 视图:从 9 张 SA 表抽 Pattern(只读视图,不存数据)
-- ============================================================

-- 视图 1: 数据字典 Pattern 候选
CREATE OR ALTER VIEW v_kg_dict_pattern_candidates AS
SELECT
    id,
    project_id,
    asset_level,
    event_id,
    elements,
    data_flows,
    data_stores,
    tags,
    pattern_tags,
    llm_confidence,
    version,
    created_at
FROM sa_data_dictionary
WHERE validation_status = 'PASS'
  AND human_confirmed = 1
  AND is_deleted = 0
  AND is_current = 1;
GO

-- 视图 2: 判定表 Pattern 候选
CREATE OR ALTER VIEW v_kg_decision_table_pattern_candidates AS
SELECT
    id,
    project_id,
    asset_level,
    event_id,
    tables,
    cross_event_consistency,
    pattern_tags,
    llm_confidence,
    version,
    created_at
FROM sa_decision_table
WHERE validation_status = 'PASS'
  AND human_confirmed = 1
  AND is_pattern_source = 1
  AND cross_event_consistency = 1   -- ★ 跨事件一致才进 KG
  AND is_deleted = 0
  AND is_current = 1;
GO

-- 视图 3: 状态机 Pattern 候选
CREATE OR ALTER VIEW v_kg_state_machine_pattern_candidates AS
SELECT
    id,
    project_id,
    asset_level,
    event_id,
    state_machines,
    states_in_dict,
    pattern_tags,
    llm_confidence,
    version,
    created_at
FROM sa_state_machine
WHERE validation_status = 'PASS'
  AND human_confirmed = 1
  AND is_pattern_source = 1
  AND is_deleted = 0
  AND is_current = 1;
GO

-- 视图 4: 重试失败统计(DKEE 训练数据)
CREATE OR ALTER VIEW v_dkee_failure_stats AS
SELECT
    sa_table_name,
    validator_name,
    COUNT(*) AS total_runs,
    SUM(CASE WHEN is_converged = 1 THEN 1 ELSE 0 END) AS converged_runs,
    AVG(retry_count) AS avg_retry_count,
    AVG(CAST(duration_ms AS FLOAT)) AS avg_duration_ms
FROM sa_validation_log
WHERE created_at > DATEADD(DAY, -30, GETDATE())
GROUP BY sa_table_name, validator_name;
GO


-- ============================================================
-- 验证查询:确认所有表创建成功
-- ============================================================
SELECT
    t.TABLE_NAME AS [表名],
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME) AS [字段数],
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k WHERE k.TABLE_NAME = t.TABLE_NAME) AS [外键数]
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_NAME LIKE 'sa_%'
ORDER BY t.TABLE_NAME;
GO

-- 期望输出: 10 行(sa_scope / sa_dfd / sa_business_process / sa_data_dictionary / sa_pspec / sa_decision_table / sa_er / sa_state_machine / sa_ui / sa_validation_log)


-- ============================================================
-- 完成
-- ============================================================
PRINT '✓ 10 张 SA 表创建成功(含强外键链 + 三级分层 + 版本管理 + KG 视图)';
GO
