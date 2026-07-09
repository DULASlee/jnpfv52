SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BASE_AI_SKILL_REVIEW')
BEGIN
    CREATE TABLE BASE_AI_SKILL_REVIEW (
        F_Id              BIGINT         NOT NULL CONSTRAINT PK_SKILL_REVIEW PRIMARY KEY,
        F_SkillRunId      NVARCHAR(50)   NOT NULL,
        F_EvalRunId       BIGINT         NULL,
        F_SkillId         NVARCHAR(100)  NOT NULL,
        F_Score           INT            NOT NULL,
        F_Verdict         NVARCHAR(20)   NOT NULL,
        F_Comment         NVARCHAR(2000) NULL,
        F_ReviewerId      BIGINT         NULL,
        F_ReviewerName    NVARCHAR(100)  NULL,
        F_TenantId        NVARCHAR(50)   NOT NULL CONSTRAINT DF_SKILL_REVIEW_Tenant DEFAULT '',
        F_ProjectId       NVARCHAR(50)   NOT NULL CONSTRAINT DF_SKILL_REVIEW_Project DEFAULT '',
        F_PipelineId      NVARCHAR(50)   NOT NULL CONSTRAINT DF_SKILL_REVIEW_Pipeline DEFAULT '',
        F_CreatorTime     DATETIME       NOT NULL CONSTRAINT DF_SKILL_REVIEW_Time DEFAULT GETDATE(),
        F_ModifyTime      DATETIME       NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SKILL_REVIEW_RUN' AND object_id = OBJECT_ID('BASE_AI_SKILL_REVIEW'))
CREATE INDEX IX_SKILL_REVIEW_RUN ON BASE_AI_SKILL_REVIEW (F_SkillRunId, F_TenantId, F_CreatorTime DESC);
GO

-- eval-judge LLM policy 种子
IF NOT EXISTS (SELECT 1 FROM ai_skill_llm_policy WHERE F_SkillId = 'eval-judge')
BEGIN
    DECLARE @policyId NVARCHAR(50) = REPLACE(NEWID(),'-','');
    INSERT INTO ai_skill_llm_policy (F_Id, F_SkillId, F_MaxLlmCalls, F_MaxTokensPerCall, F_MaxTotalTokens, F_ModelTier, F_TimeoutMs, F_CreatedAt)
    VALUES (@policyId, 'eval-judge', 1, 500, 1000, 'fast', 30000, GETUTCDATE());
END
GO
