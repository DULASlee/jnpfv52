-- ════════════════════════════════════════════════════════════════
-- 阶段七 P7-E01 — Eval Pipeline 四层基础设施
--
-- 文档：15、全链条第七阶段开发计划.md §6.2
-- 内容：
--   1. BASE_AI_EVAL_RUN 扩展三元组（R12）+ CaseId + 分层结果 + 校准 + 一致性
--   2. 索引：按 case 查最近 k 次 run（pass^k 计算用）
--
-- 设计要点：
--   - 三元组 (tenantId, projectId, pipelineId) 强隔离（宪法 R12）
--   - F_LayerResults 存 JSON {l1,l2,l3,l4}，避免拆表（务实）
--   - F_Consistency 为 pass^k 预留（首版 k=1，退化为 pass@1）
-- ════════════════════════════════════════════════════════════════

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. 三元组：F_TenantId（R12 隔离）
IF COL_LENGTH('BASE_AI_EVAL_RUN', 'F_TenantId') IS NULL
BEGIN
    ALTER TABLE BASE_AI_EVAL_RUN ADD F_TenantId NVARCHAR(50) NOT NULL CONSTRAINT DF_EVAL_RUN_Tenant DEFAULT '';
    PRINT '[OK] BASE_AI_EVAL_RUN.F_TenantId added';
END
ELSE
    PRINT '[SKIP] F_TenantId exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 2. 三元组：F_ProjectId（R12 隔离）
IF COL_LENGTH('BASE_AI_EVAL_RUN', 'F_ProjectId') IS NULL
BEGIN
    ALTER TABLE BASE_AI_EVAL_RUN ADD F_ProjectId NVARCHAR(50) NOT NULL CONSTRAINT DF_EVAL_RUN_Project DEFAULT '';
    PRINT '[OK] BASE_AI_EVAL_RUN.F_ProjectId added';
END
ELSE
    PRINT '[SKIP] F_ProjectId exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 3. 三元组：F_PipelineId（R12 隔离）
IF COL_LENGTH('BASE_AI_EVAL_RUN', 'F_PipelineId') IS NULL
BEGIN
    ALTER TABLE BASE_AI_EVAL_RUN ADD F_PipelineId NVARCHAR(50) NOT NULL CONSTRAINT DF_EVAL_RUN_Pipeline DEFAULT '';
    PRINT '[OK] BASE_AI_EVAL_RUN.F_PipelineId added';
END
ELSE
    PRINT '[SKIP] F_PipelineId exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 4. F_CaseId — 关联具体测试用例（pass^k 一致性按 case 聚合）
IF COL_LENGTH('BASE_AI_EVAL_RUN', 'F_CaseId') IS NULL
BEGIN
    ALTER TABLE BASE_AI_EVAL_RUN ADD F_CaseId BIGINT NULL;
    PRINT '[OK] BASE_AI_EVAL_RUN.F_CaseId added';
END
ELSE
    PRINT '[SKIP] F_CaseId exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 5. F_LayerResults — 四层评估结果 JSON {l1,l2,l3,l4}
IF COL_LENGTH('BASE_AI_EVAL_RUN', 'F_LayerResults') IS NULL
BEGIN
    ALTER TABLE BASE_AI_EVAL_RUN ADD F_LayerResults NVARCHAR(MAX) NULL;
    PRINT '[OK] BASE_AI_EVAL_RUN.F_LayerResults added';
END
ELSE
    PRINT '[SKIP] F_LayerResults exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 6. F_OverallPassed — L1-L3 综合通过（fail-fast 后的整体结论）
IF COL_LENGTH('BASE_AI_EVAL_RUN', 'F_OverallPassed') IS NULL
BEGIN
    ALTER TABLE BASE_AI_EVAL_RUN ADD F_OverallPassed BIT NULL;
    PRINT '[OK] BASE_AI_EVAL_RUN.F_OverallPassed added';
END
ELSE
    PRINT '[SKIP] F_OverallPassed exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 7. F_JudgeKappa — L4 Judge 与人工的 Cohen's kappa（P7-E02 校准写入，P7-E01 预留）
IF COL_LENGTH('BASE_AI_EVAL_RUN', 'F_JudgeKappa') IS NULL
BEGIN
    ALTER TABLE BASE_AI_EVAL_RUN ADD F_JudgeKappa DECIMAL(5,3) NULL;
    PRINT '[OK] BASE_AI_EVAL_RUN.F_JudgeKappa added';
END
ELSE
    PRINT '[SKIP] F_JudgeKappa exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 8. F_Consistency — pass^k 一致性（首版 k=1，预留扩展点）
IF COL_LENGTH('BASE_AI_EVAL_RUN', 'F_Consistency') IS NULL
BEGIN
    ALTER TABLE BASE_AI_EVAL_RUN ADD F_Consistency DECIMAL(5,3) NULL;
    PRINT '[OK] BASE_AI_EVAL_RUN.F_Consistency added';
END
ELSE
    PRINT '[SKIP] F_Consistency exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 9. F_Status — eval run 状态（pending/running/completed/failed，原表无此列）
IF COL_LENGTH('BASE_AI_EVAL_RUN', 'F_Status') IS NULL
BEGIN
    ALTER TABLE BASE_AI_EVAL_RUN ADD F_Status NVARCHAR(20) NOT NULL CONSTRAINT DF_EVAL_RUN_Status DEFAULT 'pending';
    PRINT '[OK] BASE_AI_EVAL_RUN.F_Status added';
END
ELSE
    PRINT '[SKIP] F_Status exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 10. 索引：按 case 查最近 k 次 run（pass^k 一致性计算）
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EVAL_RUN_CASE_RUNAT')
BEGIN
    CREATE INDEX IX_EVAL_RUN_CASE_RUNAT ON BASE_AI_EVAL_RUN (F_CaseId, F_RunAt DESC)
        INCLUDE (F_TenantId, F_OverallPassed, F_Consistency);
    PRINT '[OK] IX_EVAL_RUN_CASE_RUNAT created';
END
ELSE
    PRINT '[SKIP] IX_EVAL_RUN_CASE_RUNAT exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 11. 索引：按三元组过滤 eval run（质量榜/列表查询）
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EVAL_RUN_TENANT_PROJECT')
BEGIN
    CREATE INDEX IX_EVAL_RUN_TENANT_PROJECT ON BASE_AI_EVAL_RUN (F_TenantId, F_ProjectId, F_RunAt DESC);
    PRINT '[OK] IX_EVAL_RUN_TENANT_PROJECT created';
END
ELSE
    PRINT '[SKIP] IX_EVAL_RUN_TENANT_PROJECT exists';
GO
