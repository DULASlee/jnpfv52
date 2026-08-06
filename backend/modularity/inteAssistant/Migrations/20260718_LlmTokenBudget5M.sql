-- 开发环境：项目 LLM Token 预算 50 万 → 500 万（已耗尽 red 的项目同步回 green）
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ai_projects') AND name = 'F_TokenBudget')
BEGIN
    UPDATE [dbo].[ai_projects]
    SET [F_TokenBudget] = 5000000,
        [F_LlmBudgetStatus] = CASE
            WHEN [F_TokenConsumed] >= [F_TokenBudget] THEN 'fuse'
            WHEN CAST([F_TokenConsumed] AS FLOAT) / 5000000.0 >= 0.95 THEN 'red'
            WHEN CAST([F_TokenConsumed] AS FLOAT) / 5000000.0 >= 0.70 THEN 'yellow'
            ELSE 'green'
        END
    WHERE [F_TokenBudget] <= 500000;
END
GO
