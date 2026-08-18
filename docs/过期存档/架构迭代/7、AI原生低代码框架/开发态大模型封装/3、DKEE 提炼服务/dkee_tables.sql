-- =====================================================
-- DKEE 存储表 - kg_pattern + domain_model
-- 数据库: SQL Server 2016+ / PostgreSQL 12+
-- 配合 sa_schema.sql 一起执行
-- =====================================================

-- ============================================================
-- 表: kg_pattern (知识图谱 - 跨项目 Pattern 存储)
-- ============================================================
IF OBJECT_ID('kg_pattern', 'U') IS NOT NULL DROP TABLE kg_pattern;
GO

CREATE TABLE kg_pattern (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    -- 模式分类
    pattern_type NVARCHAR(50) NOT NULL,    -- field_naming / decision_rule / state_machine / process_pattern
    industry     NVARCHAR(50) NOT NULL,    -- manufacturing / ecommerce / optical / ...

    -- 模式内容(JSON)
    pattern_content NVARCHAR(MAX) NOT NULL,  -- 不同 type 不同结构
    pattern_tags    NVARCHAR(MAX) NULL,      -- ["MES-机加工", "标准字段"]

    -- 评分(运行时算)
    score         DECIMAL(5,2) NOT NULL DEFAULT 0,
    usage_count   INT          NOT NULL DEFAULT 0,
    success_count INT          NOT NULL DEFAULT 0,
    last_score_at DATETIME2    NULL,

    -- 溯源(可追溯到 SA 表的哪条记录)
    source_projects NVARCHAR(MAX) NULL,  -- JSON: [1001, 1002, 1003, ...]
    source_records  NVARCHAR(MAX) NULL,  -- JSON: [{sa_table, record_id, version}]
    source          NVARCHAR(20)  NOT NULL DEFAULT 'human-created',
    -- 枚举: human-created / ai-discovered / self-play

    -- 时效管理
    created_at     DATETIME2 NOT NULL DEFAULT GETDATE(),
    last_used_at   DATETIME2 NULL,
    deprecated_at  DATETIME2 NULL,
    half_life_days INT       NOT NULL DEFAULT 180,

    -- 软删除 + 备注
    is_active BIT       NOT NULL DEFAULT 1,
    is_locked BIT       NOT NULL DEFAULT 0,  -- 锁定的 Pattern 不会被覆盖
    notes     NVARCHAR(MAX) NULL,

    CONSTRAINT CK_kg_pattern_type CHECK (pattern_type IN ('field_naming','decision_rule','state_machine','process_pattern')),
    CONSTRAINT CK_kg_pattern_source CHECK (source IN ('human-created','ai-discovered','self-play'))
);
GO

CREATE INDEX idx_kg_pattern_industry_type ON kg_pattern(industry, pattern_type);
CREATE INDEX idx_kg_pattern_score         ON kg_pattern(score DESC);
CREATE INDEX idx_kg_pattern_active        ON kg_pattern(is_active, deprecated_at);
CREATE INDEX idx_kg_pattern_usage         ON kg_pattern(usage_count DESC);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'知识图谱 Pattern 表 - 存储跨项目提炼的业务模式', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'kg_pattern';
GO


-- ============================================================
-- 表: domain_model (领域模型 - 行业标准知识)
-- ============================================================
IF OBJECT_ID('domain_model', 'U') IS NOT NULL DROP TABLE domain_model;
GO

CREATE TABLE domain_model (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,

    -- 模型分类
    industry   NVARCHAR(50)  NOT NULL,    -- manufacturing / ecommerce / ...
    model_type NVARCHAR(50)  NOT NULL,    -- standard_field_set / standard_state_machine / standard_process
    model_name NVARCHAR(100) NOT NULL,    -- 如 "MES-机加工-标准字段集"
    version    INT           NOT NULL DEFAULT 1,

    -- 模型内容(JSON)
    model_content NVARCHAR(MAX) NOT NULL,  -- 标准字段集 / 标准状态机 / 标准流程
    model_tags    NVARCHAR(MAX) NULL,

    -- 评分
    score       DECIMAL(5,2) NOT NULL DEFAULT 0,
    usage_count INT          NOT NULL DEFAULT 0,

    -- 溯源
    source_projects NVARCHAR(MAX) NULL,

    -- 时效管理
    created_at    DATETIME2 NOT NULL DEFAULT GETDATE(),
    last_used_at  DATETIME2 NULL,
    deprecated_at DATETIME2 NULL,
    is_active     BIT       NOT NULL DEFAULT 1,
    notes         NVARCHAR(MAX) NULL,

    CONSTRAINT CK_domain_model_type CHECK (model_type IN ('standard_field_set','standard_state_machine','standard_process'))
);
GO

CREATE INDEX idx_domain_model_industry_type ON domain_model(industry, model_type);
CREATE INDEX idx_domain_model_score          ON domain_model(score DESC);
CREATE INDEX idx_domain_model_active         ON domain_model(is_active, deprecated_at);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'领域模型表 - 存储行业标准字段集、状态机、流程', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'domain_model';
GO


-- ============================================================
-- 表: kg_pattern_usage (Pattern 使用日志 - 用于评分更新)
-- ============================================================
IF OBJECT_ID('kg_pattern_usage', 'U') IS NOT NULL DROP TABLE kg_pattern_usage;
GO

CREATE TABLE kg_pattern_usage (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    pattern_id   BIGINT NOT NULL,
    project_id   BIGINT NOT NULL,
    is_success   BIT    NOT NULL,           -- 使用后 Validator 通过/失败
    used_at      DATETIME2 NOT NULL DEFAULT GETDATE(),
    context_info NVARCHAR(MAX) NULL,        -- 当时用的 SA step / event

    CONSTRAINT FK_kg_pattern_usage FOREIGN KEY (pattern_id) REFERENCES kg_pattern(id)
);
GO

CREATE INDEX idx_kg_pattern_usage_pattern ON kg_pattern_usage(pattern_id);
CREATE INDEX idx_kg_pattern_usage_project ON kg_pattern_usage(project_id);
CREATE INDEX idx_kg_pattern_usage_time    ON kg_pattern_usage(used_at DESC);
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Pattern 使用日志 - 每次使用都记,用于动态更新评分', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'kg_pattern_usage';
GO


-- ============================================================
-- 视图: 高质量活跃 Pattern(给 LLM context 注入用)
-- ============================================================
CREATE OR ALTER VIEW v_active_kg_patterns AS
SELECT
    id,
    pattern_type,
    industry,
    pattern_content,
    pattern_tags,
    score,
    usage_count,
    success_count,
    source,
    source_projects,
    last_used_at,
    created_at
FROM kg_pattern
WHERE is_active = 1
  AND deprecated_at IS NULL
  AND score >= 0.6          -- 评分门禁
  AND usage_count >= 1      -- 至少被用过 1 次
ORDER BY score DESC, usage_count DESC;
GO


-- ============================================================
-- 视图: Pattern 评分统计(给监控用)
-- ============================================================
CREATE OR ALTER VIEW v_kg_pattern_stats AS
SELECT
    pattern_type,
    industry,
    COUNT(*) AS total_patterns,
    AVG(score) AS avg_score,
    MAX(score) AS max_score,
    SUM(usage_count) AS total_usage,
    SUM(CASE WHEN is_active = 1 THEN 1 ELSE 0 END) AS active_patterns,
    SUM(CASE WHEN deprecated_at IS NOT NULL THEN 1 ELSE 0 END) AS deprecated_patterns
FROM kg_pattern
GROUP BY pattern_type, industry;
GO


-- ============================================================
-- 验证
-- ============================================================
PRINT '✓ DKEE 表创建成功:kg_pattern (3 张) + domain_model + kg_pattern_usage';
PRINT '✓ 视图:v_active_kg_patterns / v_kg_pattern_stats';
GO
