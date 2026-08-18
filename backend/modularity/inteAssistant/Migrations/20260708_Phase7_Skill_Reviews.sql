-- ════════════════════════════════════════════════════════════════
-- 阶段七 P7-E02/E03 — 人工抽检评审表 + LLM Judge policy 种子
--
-- 文档：15、全链条第七阶段开发计划.md §6.3 §6.4
-- 内容：
--   1. BASE_AI_SKILL_REVIEW 新表（人工抽检评分，供 Judge Cohen's kappa 校准 join）
--   2. ai_skill_llm_policy 种子：eval-judge（maxCalls=1, fast tier → mimo 跨家族）
--
-- 设计要点：
--   - 2026 实践：跨家族 Judge（生成 deepseek / Judge mimo）避免自偏好偏差（+10-25% 虚高）
--   - pass/fail 二元（Score>=60 → PASS），而非 1-5 分制
--   - 三元组 R12 隔离：F_TenantId + F_ProjectId + F_PipelineId
--   - 同一 run 支持多人独立评分（校准 inter-rater agreement）
-- ════════════════════════════════════════════════════════════════

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. BASE_AI_SKILL_REVIEW 人工抽检评审表
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BASE_AI_SKILL_REVIEW')
BEGIN
    CREATE TABLE BASE_AI_SKILL_REVIEW (
        F_Id              BIGINT         NOT NULL CONSTRAINT PK_SKILL_REVIEW PRIMARY KEY,
        F_SkillRunId      NVARCHAR(50)   NOT NULL,
        F_EvalRunId       BIGINT         NULL,
        F_SkillId         NVARCHAR(100)  NOT NULL,
        F_Score           INT            NOT NULL,          -- 0-100（>=60 视为 PASS）
        F_Verdict         NVARCHAR(20)   NOT NULL,          -- PASS / FAIL
        F_Comment         NVARCHAR(2000) NULL,
        F_ReviewerId      BIGINT         NULL,
        F_ReviewerName    NVARCHAR(100)  NULL,

        -- 三元组 R12 隔离
        F_TenantId        NVARCHAR(50)   NOT NULL CONSTRAINT DF_SKILL_REVIEW_Tenant DEFAULT '',
        F_ProjectId       NVARCHAR(50)   NOT NULL CONSTRAINT DF_SKILL_REVIEW_Project DEFAULT '',
        F_PipelineId      NVARCHAR(50)   NOT NULL CONSTRAINT DF_SKILL_REVIEW_Pipeline DEFAULT '',

        F_CreatorTime     DATETIME       NOT NULL CONSTRAINT DF_SKILL_REVIEW_Time DEFAULT GETDATE(),
        F_ModifyTime      DATETIME       NULL
    );
    PRINT '[OK] BASE_AI_SKILL_REVIEW created';
END
ELSE
    PRINT '[SKIP] BASE_AI_SKILL_REVIEW exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 2. 索引：按 skill_run 查所有 review（多人独立评分）
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SKILL_REVIEW_RUN')
BEGIN
    CREATE INDEX IX_SKILL_REVIEW_RUN ON BASE_AI_SKILL_REVIEW (F_SkillRunId, F_TenantId, F_CreatorTime DESC);
    PRINT '[OK] IX_SKILL_REVIEW_RUN created';
END
ELSE
    PRINT '[SKIP] IX_SKILL_REVIEW_RUN exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 3. 索引：按租户 + 时间查 review（Judge 校准 join 用）
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SKILL_REVIEW_TENANT_TIME')
BEGIN
    CREATE INDEX IX_SKILL_REVIEW_TENANT_TIME ON BASE_AI_SKILL_REVIEW (F_TenantId, F_CreatorTime DESC)
        INCLUDE (F_SkillRunId, F_EvalRunId, F_Verdict);
    PRINT '[OK] IX_SKILL_REVIEW_TENANT_TIME created';
END
ELSE
    PRINT '[SKIP] IX_SKILL_REVIEW_TENANT_TIME exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 4. ai_skill_llm_policy 种子：eval-judge（跨家族 mimo，fast tier，maxCalls=1）
IF NOT EXISTS (SELECT 1 FROM ai_skill_llm_policy WHERE F_SkillId = 'eval-judge')
BEGIN
    INSERT INTO ai_skill_llm_policy (F_Id, F_SkillId, F_MaxLlmCalls, F_MaxTokensPerCall, F_MaxTotalTokens, F_ModelTier, F_TimeoutMs, F_CreatedAt)
    VALUES (NEWID(), 'eval-judge', 1, 500, 1000, 'fast', 30000, GETUTCDATE());
    PRINT '[OK] eval-judge LLM policy seeded (maxCalls=1, fast→mimo, maxTokens=500)';
END
ELSE
    PRINT '[SKIP] eval-judge policy exists';
GO
