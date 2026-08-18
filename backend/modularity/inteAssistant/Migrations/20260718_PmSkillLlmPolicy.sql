-- PM 主链 LLM 策略对齐（步骤①→③ 多轮调用 + 15 事件 PSpec 增强）
-- 修复：ai_skill_llm_policy 中 pm-skill 仍为 40000/3 次导致步骤② PSpec 触发 LLM_SKILL_TOKEN_LIMIT

IF EXISTS (SELECT 1 FROM sysobjects WHERE name = 'ai_skill_llm_policy' AND xtype = 'U')
BEGIN
    UPDATE [dbo].[ai_skill_llm_policy]
    SET [F_MaxLlmCalls] = 12,
        [F_MaxTokensPerCall] = 16384,
        [F_MaxTotalTokens] = 160000
    WHERE [F_SkillId] = N'pm-skill';
END;
