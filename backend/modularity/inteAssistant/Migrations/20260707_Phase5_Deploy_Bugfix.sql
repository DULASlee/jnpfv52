-- ════════════════════════════════════════════════════════════════
-- 阶段五 P5 — deploy-skill + bugfix-skill DDL 补全
--
-- 文档：13、全链条第五阶段开发计划.md §5 DDL
-- 内容：
--   1. ai_projects 扩展：F_DeploymentVerifiedAt、F_LastBugfixAt
--   2. ai_skill_llm_policy 种子：deploy-skill(maxCalls=1)、bugfix-skill(maxCalls=3, fast)
-- ════════════════════════════════════════════════════════════════

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. ai_projects 加 F_DeploymentVerifiedAt
IF COL_LENGTH('ai_projects', 'F_DeploymentVerifiedAt') IS NULL
BEGIN
    ALTER TABLE ai_projects ADD F_DeploymentVerifiedAt DATETIME2 NULL;
    PRINT '[OK] ai_projects.F_DeploymentVerifiedAt added';
END
ELSE
    PRINT '[SKIP] F_DeploymentVerifiedAt exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 2. ai_projects 加 F_LastBugfixAt
IF COL_LENGTH('ai_projects', 'F_LastBugfixAt') IS NULL
BEGIN
    ALTER TABLE ai_projects ADD F_LastBugfixAt DATETIME2 NULL;
    PRINT '[OK] ai_projects.F_LastBugfixAt added';
END
ELSE
    PRINT '[SKIP] F_LastBugfixAt exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 3. ai_skill_llm_policy 种子：deploy-skill（零 LLM，maxCalls=1 仅为占位）
IF NOT EXISTS (SELECT 1 FROM ai_skill_llm_policy WHERE F_SkillId = 'deploy-skill')
BEGIN
    INSERT INTO ai_skill_llm_policy (F_Id, F_SkillId, F_MaxLlmCalls, F_MaxTokensPerCall, F_MaxTotalTokens, F_ModelTier, F_TimeoutMs, F_CreatedAt)
    VALUES (NEWID(), 'deploy-skill', 1, 0, 0, 'fast', 600000, GETUTCDATE());
    PRINT '[OK] deploy-skill LLM policy seeded (maxCalls=1, zero LLM)';
END
ELSE
    PRINT '[SKIP] deploy-skill policy exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 4. ai_skill_llm_policy 种子：bugfix-skill（maxCalls=3, fast tier）
IF NOT EXISTS (SELECT 1 FROM ai_skill_llm_policy WHERE F_SkillId = 'bugfix-skill')
BEGIN
    INSERT INTO ai_skill_llm_policy (F_Id, F_SkillId, F_MaxLlmCalls, F_MaxTokensPerCall, F_MaxTotalTokens, F_ModelTier, F_TimeoutMs, F_CreatedAt)
    VALUES (NEWID(), 'bugfix-skill', 3, 4000, 12000, 'fast', 60000, GETUTCDATE());
    PRINT '[OK] bugfix-skill LLM policy seeded (maxCalls=3, fast)';
END
ELSE
    PRINT '[SKIP] bugfix-skill policy exists';
GO
