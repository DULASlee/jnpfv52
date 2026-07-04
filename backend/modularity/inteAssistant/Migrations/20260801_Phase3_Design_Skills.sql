-- ========================================
-- 阶段三 设计 Skill + LLM Budget Guard（P3-B07）
-- BASE_AI_CALL_LOG 扩展 / ai_skill_llm_policy / ai_projects 扩展
-- 日期：2026-08-01
-- ========================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_CALL_LOG') AND name = 'F_RunId')
    ALTER TABLE [dbo].[BASE_AI_CALL_LOG] ADD [F_RunId] NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_CALL_LOG') AND name = 'F_SkillId')
    ALTER TABLE [dbo].[BASE_AI_CALL_LOG] ADD [F_SkillId] NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_CALL_LOG') AND name = 'F_ProjectId')
    ALTER TABLE [dbo].[BASE_AI_CALL_LOG] ADD [F_ProjectId] NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ai_call_log_project' AND object_id = OBJECT_ID('BASE_AI_CALL_LOG'))
    CREATE INDEX [IX_ai_call_log_project]
        ON [dbo].[BASE_AI_CALL_LOG] ([F_TENANT_ID], [F_ProjectId], [F_CREATOR_TIME] DESC);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ai_projects') AND name = 'F_DesignCompletedAt')
    ALTER TABLE [dbo].[ai_projects] ADD [F_DesignCompletedAt] DATETIME2(7) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ai_projects') AND name = 'F_LlmBudgetStatus')
    ALTER TABLE [dbo].[ai_projects] ADD [F_LlmBudgetStatus] NVARCHAR(20) NOT NULL DEFAULT 'green';

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'ai_skill_llm_policy' AND xtype = 'U')
BEGIN
    CREATE TABLE [dbo].[ai_skill_llm_policy] (
        [F_Id]               NVARCHAR(50)    NOT NULL,
        [F_SkillId]          NVARCHAR(100)   NOT NULL,
        [F_MaxLlmCalls]      INT             NOT NULL DEFAULT 3,
        [F_MaxTokensPerCall] INT            NOT NULL DEFAULT 8192,
        [F_MaxTotalTokens]   INT             NOT NULL DEFAULT 50000,
        [F_ModelTier]        NVARCHAR(20)    NOT NULL DEFAULT 'strong',
        [F_TimeoutMs]        INT             NOT NULL DEFAULT 120000,
        [F_CreatedAt]        DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ai_skill_llm_policy] PRIMARY KEY ([F_Id]),
        CONSTRAINT [UQ_skill_llm_policy] UNIQUE ([F_SkillId])
    );
END;

-- 种子策略（幂等）
IF NOT EXISTS (SELECT 1 FROM [dbo].[ai_skill_llm_policy] WHERE [F_SkillId] = 'architect-skill')
    INSERT INTO [dbo].[ai_skill_llm_policy] ([F_Id],[F_SkillId],[F_MaxLlmCalls],[F_MaxTokensPerCall],[F_MaxTotalTokens],[F_ModelTier],[F_TimeoutMs])
    VALUES (NEWID(), 'architect-skill', 3, 8192, 80000, 'strong', 120000);

IF NOT EXISTS (SELECT 1 FROM [dbo].[ai_skill_llm_policy] WHERE [F_SkillId] = 'db-design-skill')
    INSERT INTO [dbo].[ai_skill_llm_policy] ([F_Id],[F_SkillId],[F_MaxLlmCalls],[F_MaxTokensPerCall],[F_MaxTotalTokens],[F_ModelTier],[F_TimeoutMs])
    VALUES (NEWID(), 'db-design-skill', 2, 8192, 60000, 'strong', 120000);

IF NOT EXISTS (SELECT 1 FROM [dbo].[ai_skill_llm_policy] WHERE [F_SkillId] = 'ui-design-skill')
    INSERT INTO [dbo].[ai_skill_llm_policy] ([F_Id],[F_SkillId],[F_MaxLlmCalls],[F_MaxTokensPerCall],[F_MaxTotalTokens],[F_ModelTier],[F_TimeoutMs])
    VALUES (NEWID(), 'ui-design-skill', 2, 4096, 40000, 'strong', 120000);

IF NOT EXISTS (SELECT 1 FROM [dbo].[ai_skill_llm_policy] WHERE [F_SkillId] = 'system-design-skill')
    INSERT INTO [dbo].[ai_skill_llm_policy] ([F_Id],[F_SkillId],[F_MaxLlmCalls],[F_MaxTokensPerCall],[F_MaxTotalTokens],[F_ModelTier],[F_TimeoutMs])
    VALUES (NEWID(), 'system-design-skill', 1, 4096, 20000, 'strong', 120000);

IF NOT EXISTS (SELECT 1 FROM [dbo].[ai_skill_llm_policy] WHERE [F_SkillId] = 'pm-skill')
    INSERT INTO [dbo].[ai_skill_llm_policy] ([F_Id],[F_SkillId],[F_MaxLlmCalls],[F_MaxTokensPerCall],[F_MaxTotalTokens],[F_ModelTier],[F_TimeoutMs])
    VALUES (NEWID(), 'pm-skill', 3, 8192, 40000, 'strong', 120000);

IF NOT EXISTS (SELECT 1 FROM [dbo].[ai_skill_llm_policy] WHERE [F_SkillId] = 'analyst-skill')
    INSERT INTO [dbo].[ai_skill_llm_policy] ([F_Id],[F_SkillId],[F_MaxLlmCalls],[F_MaxTokensPerCall],[F_MaxTotalTokens],[F_ModelTier],[F_TimeoutMs])
    VALUES (NEWID(), 'analyst-skill', 0, 0, 0, 'strong', 0);
