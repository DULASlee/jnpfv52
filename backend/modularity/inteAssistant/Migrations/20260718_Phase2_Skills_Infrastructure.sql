-- ========================================
-- 阶段二 Skill 基础设施（P2-B11）
-- ai_skill_runs / ai_seed_templates / ai_projects 扩展
-- 日期：2026-07-18
-- ========================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ai_skill_runs' AND xtype = 'U')
BEGIN
    CREATE TABLE [dbo].[ai_skill_runs] (
        [F_Id]              NVARCHAR(50)    NOT NULL,
        [F_TenantId]        NVARCHAR(50)    NOT NULL,
        [F_ProjectId]       NVARCHAR(50)    NOT NULL,
        [F_SkillId]         NVARCHAR(100)   NOT NULL,
        [F_Status]          NVARCHAR(20)    NOT NULL DEFAULT 'running',
        [F_StartedAt]       DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [F_CompletedAt]     DATETIME2(7)    NULL,
        [F_TokenConsumed]   BIGINT          NOT NULL DEFAULT 0,
        [F_ErrorMessage]    NVARCHAR(2000)  NULL,
        [F_Metadata]        NVARCHAR(MAX)   NULL,
        CONSTRAINT [PK_ai_skill_runs] PRIMARY KEY ([F_Id])
    );

    CREATE INDEX [IX_skill_runs_project]
        ON [dbo].[ai_skill_runs] ([F_TenantId], [F_ProjectId], [F_StartedAt] DESC);
END;

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ai_seed_templates' AND xtype = 'U')
BEGIN
    CREATE TABLE [dbo].[ai_seed_templates] (
        [F_Id]               NVARCHAR(50)    NOT NULL,
        [F_TemplateId]       NVARCHAR(100)   NOT NULL,
        [F_Industry]         NVARCHAR(50)    NOT NULL,
        [F_EventNamePattern] NVARCHAR(500)   NOT NULL,
        [F_ComplexityHint]   NVARCHAR(20)    NOT NULL DEFAULT 'simple',
        [F_CoverageScore]    DECIMAL(4,2)    NOT NULL DEFAULT 0.80,
        [F_TemplateJson]     NVARCHAR(MAX)   NOT NULL,
        [F_CreatedAt]        DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        [F_DeleteMark]       BIT             NOT NULL DEFAULT 0,
        CONSTRAINT [PK_ai_seed_templates] PRIMARY KEY ([F_Id]),
        CONSTRAINT [UQ_seed_template_id] UNIQUE ([F_TemplateId])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ai_projects') AND name = 'F_AnalysisCompletedAt')
    ALTER TABLE [dbo].[ai_projects] ADD [F_AnalysisCompletedAt] DATETIME2(7) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ai_projects') AND name = 'F_Ir0ConfirmedAt')
    ALTER TABLE [dbo].[ai_projects] ADD [F_Ir0ConfirmedAt] DATETIME2(7) NULL;
