-- ════════════════════════════════════════════════════════════════
-- 三元组隔离补全：inte_assistant_deliverable 加 F_ProjectId 列
--
-- 背景：交付物表原只有 (PipelineId, TenantId) 二元组，缺 ProjectId。
-- 多租户审查发现查询漏 TenantId + 缺 ProjectId，违反三元组隔离原则。
--
-- 本迁移：
--   1. 加 F_ProjectId 列（DEFAULT ''）
--   2. 存量回填 F_ProjectId = F_PipelineId（历史数据 projectId≡pipelineId）
--   3. 建三元组复合索引 IX_inte_assistant_deliverable_triple
-- ════════════════════════════════════════════════════════════════

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF COL_LENGTH('inte_assistant_deliverable', 'F_ProjectId') IS NULL
BEGIN
    ALTER TABLE inte_assistant_deliverable ADD F_ProjectId NVARCHAR(50) NOT NULL DEFAULT '';
    PRINT '[OK] F_ProjectId column added';
END
ELSE
BEGIN
    PRINT '[SKIP] F_ProjectId column already exists';
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

UPDATE inte_assistant_deliverable SET F_ProjectId = F_PipelineId WHERE F_ProjectId = '';
PRINT '[OK] backfill F_ProjectId done';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_inte_assistant_deliverable_triple' AND object_id = OBJECT_ID('inte_assistant_deliverable'))
BEGIN
    CREATE INDEX IX_inte_assistant_deliverable_triple ON inte_assistant_deliverable (F_TenantId, F_ProjectId, F_PipelineId);
    PRINT '[OK] index IX_inte_assistant_deliverable_triple created';
END
GO
