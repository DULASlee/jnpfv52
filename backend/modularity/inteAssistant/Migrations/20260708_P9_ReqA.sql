-- ════════════════════════════════════════════════════════════════
-- P9 需求分析子链重构 — 迁移 SQL（26 号第一阶段）
--
-- 2 张物理表 + 1 张视图 = 3 个 DDL 对象
--
-- 关键约束（23 号决策清单）：
--   - 三元组 (F_TenantId, F_ProjectId, F_PIPELINE_ID) 作为隔离键
--   - snake_case 表名 + F_ 列名前缀
--   - sa_entity_fields 是 VIEW（物理源 = ai_entity_field）
-- ════════════════════════════════════════════════════════════════

-- ─── 表 1：sa_assumptions（假设项追踪） ───
-- Round 1/2 内存传递，Round 3 落库。
-- 三个来源：C# 编译器推导 / LLM_SUGGESTED / DDD 增强

CREATE TABLE [dbo].[sa_assumptions] (
    [F_Id]              NVARCHAR(50)   NOT NULL,
    [F_TenantId]        NVARCHAR(50)   NOT NULL DEFAULT '',
    [F_ProjectId]       NVARCHAR(50)   NOT NULL DEFAULT '',
    [F_PIPELINE_ID]     NVARCHAR(50)   NOT NULL DEFAULT '',

    -- 关联事件（NULL = 全局假设）
    [F_EventId]         NVARCHAR(50)   NULL,

    -- 产生假设的 SA 步骤或来源
    -- C# 编译器: "Scope"/"DFD"/"Dict"/"ER"/"StateMachine"/"UI"
    -- LLM 精化: "PSpec"/"DecisionTable"
    -- DDD 增强: "DDD_DomainModel"/"DDD_Aggregate"/"DDD_EventCatalog"/"DDD_Cqrs"/"DDD_Integration"
    [F_SourceStep]      NVARCHAR(50)   NOT NULL,

    -- 假设内容描述
    [F_AssumptionText]  NVARCHAR(MAX)  NOT NULL,

    -- 置信度 0.00-1.00
    -- C# 编译器推导默认 0.50
    -- LLM_SUGGESTED 默认 0.50
    -- R2 门禁降级的幻觉字段 = 0.00
    -- DDD 增强 confidence < 1.0 的推导结果
    [F_Confidence]      DECIMAL(3,2)   NOT NULL DEFAULT 0.50,

    -- 用户是否已确认
    [F_IsUserConfirmed] BIT            NOT NULL DEFAULT 0,

    -- 用户裁决：correct / incorrect（NULL = 未裁决）
    [F_UserVerdict]     NVARCHAR(10)   NULL,

    -- 创建轮次（1/2/3）
    [F_RoundCreated]    INT            NOT NULL DEFAULT 1,

    [F_CreatedAt]       DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_sa_assumptions] PRIMARY KEY ([F_Id])
);

-- 三元组 + 未确认项查询索引（第 3 轮 Q8 用）
CREATE INDEX [IX_sa_assumptions_triple]
    ON [dbo].[sa_assumptions] ([F_TenantId], [F_ProjectId], [F_PIPELINE_ID], [F_IsUserConfirmed])
    WHERE [F_IsUserConfirmed] = 0;


-- ─── 表 2：sa_consistency（一致性报告） ───
-- 28 号一致性检查器写入。增量/全量双模式。

CREATE TABLE [dbo].[sa_consistency] (
    [F_Id]              NVARCHAR(50)   NOT NULL,
    [F_TenantId]        NVARCHAR(50)   NOT NULL DEFAULT '',
    [F_ProjectId]       NVARCHAR(50)   NOT NULL DEFAULT '',
    [F_PIPELINE_ID]     NVARCHAR(50)   NOT NULL DEFAULT '',

    -- 第几轮（1/2/3）
    [F_RoundNumber]     INT            NOT NULL,

    -- 检查类型：DATA_ENTITY / ROLE / FLOW_CLOSURE / ASSUMPTION
    [F_CheckType]       NVARCHAR(30)   NOT NULL,

    -- 冲突列表 JSON: [{type, entityA, entityB, field, description}]
    [F_ConflictsJson]   NVARCHAR(MAX)  NULL,

    -- 全局假设项汇总 JSON
    [F_AssumptionsJson] NVARCHAR(MAX)  NULL,

    -- 识别出的遗漏 JSON
    [F_GapsJson]        NVARCHAR(MAX)  NULL,

    -- 严重度：INFO / WARNING / CRITICAL
    [F_Severity]        NVARCHAR(10)   NOT NULL,

    [F_CreatedAt]       DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_sa_consistency] PRIMARY KEY ([F_Id])
);

CREATE INDEX [IX_sa_consistency_triple]
    ON [dbo].[sa_consistency] ([F_TenantId], [F_ProjectId], [F_PIPELINE_ID], [F_RoundNumber]);


-- ─── 视图 3：sa_entity_fields（VIEW on ai_entity_field） ───
-- 一致性检查规则 1（数据实体一致性）的查询源。
-- 物理数据只有 ai_entity_field 一份，避免两份数据不一致（声明 1）。

CREATE VIEW [dbo].[sa_entity_fields] AS
SELECT
    [F_TenantId]        AS TenantId,
    [F_ProjectId]       AS ProjectId,
    [F_PIPELINE_ID]     AS PipelineId,
    [F_EntityName]      AS EntityName,
    [F_FieldName]       AS FieldName,
    [F_PropertyName]    AS PropertyName,
    [F_DbColumnName]    AS DbColumnName,
    [F_CSharpType]      AS CSharpType,
    [F_SqlType]         AS SqlType,
    [F_IsPrimaryKey]    AS IsPrimaryKey,
    [F_IsRequired]      AS IsRequired,
    [F_IsNullable]      AS IsNullable,
    [F_References]      AS References,
    [F_ReferencesTable]  AS ReferencesTable,
    [F_ReferencesColumn] AS ReferencesColumn
FROM [dbo].[ai_entity_field]
WHERE [F_DeleteMark] = 0;


-- ─────────────────────────────────────────────────────────────────────────────
-- 28 号第三阶段：sa_quality_score（质量评分表，5 维度含 DDD）
-- 由 28 号 QualityScoreCalculator 在 Round 3 工程保障后写入。
-- ─────────────────────────────────────────────────────────────────────────────

CREATE TABLE [dbo].[sa_quality_score] (
    [F_Id]                NVARCHAR(50)  NOT NULL PRIMARY KEY,
    [F_TenantId]          NVARCHAR(50)  NOT NULL DEFAULT '',
    [F_ProjectId]         NVARCHAR(50)  NOT NULL DEFAULT '',
    [F_PIPELINE_ID]       NVARCHAR(50)  NOT NULL DEFAULT '',
    [F_RoundNumber]       INT           NOT NULL,
    [F_StructureScore]    DECIMAL(5,2)  NOT NULL,   -- 结构完整度 25%
    [F_CoverageScore]     DECIMAL(5,2)  NOT NULL,   -- 决策覆盖率 25%
    [F_ConsistencyScore]  DECIMAL(5,2)  NOT NULL,   -- 一致性 20%
    [F_DepthScore]        DECIMAL(5,2)  NOT NULL,   -- 深度 15%
    [F_DddScore]          DECIMAL(5,2)  NOT NULL,   -- DDD 增强 15%
    [F_TotalScore]        DECIMAL(5,2)  NOT NULL,   -- 综合评分
    [F_CreatedAt]         DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE()
);

-- 三元组 + 轮次索引
CREATE NONCLUSTERED INDEX [IX_sa_quality_score_triple]
    ON [dbo].[sa_quality_score] ([F_TenantId], [F_ProjectId], [F_PIPELINE_ID], [F_RoundNumber]);

