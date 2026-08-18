-- ========================================
-- P1 修复：ai_ir_fragment_snapshots 唯一约束补全三元组
-- 正式列名：F_PIPELINE_ID（与 AiIrFragmentSnapshotEntity 一致）
-- 禁止再建第二列 F_PipelineId
-- 日期：2026-07-11
-- ========================================

IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = N'UQ_fragment_current'
      AND parent_object_id = OBJECT_ID(N'dbo.ai_ir_fragment_snapshots')
)
BEGIN
    ALTER TABLE [dbo].[ai_ir_fragment_snapshots] DROP CONSTRAINT [UQ_fragment_current];
END
GO

-- 若误创建了第二列 F_PipelineId，合并后删除
IF COL_LENGTH('ai_ir_fragment_snapshots', 'F_PipelineId') IS NOT NULL
   AND COL_LENGTH('ai_ir_fragment_snapshots', 'F_PIPELINE_ID') IS NOT NULL
BEGIN
    EXEC(N'UPDATE [dbo].[ai_ir_fragment_snapshots]
          SET [F_PIPELINE_ID] = COALESCE(NULLIF([F_PIPELINE_ID], ''''), NULLIF([F_PipelineId], ''''), [F_ProjectId])
          WHERE [F_PIPELINE_ID] IS NULL OR [F_PIPELINE_ID] = ''''');
    ALTER TABLE [dbo].[ai_ir_fragment_snapshots] DROP COLUMN [F_PipelineId];
END
GO

IF COL_LENGTH('ai_ir_fragment_snapshots', 'F_PIPELINE_ID') IS NULL
BEGIN
    ALTER TABLE [dbo].[ai_ir_fragment_snapshots] ADD [F_PIPELINE_ID] NVARCHAR(50) NULL;
END
GO

UPDATE [dbo].[ai_ir_fragment_snapshots]
SET [F_PIPELINE_ID] = [F_ProjectId]
WHERE [F_PIPELINE_ID] IS NULL OR LTRIM(RTRIM([F_PIPELINE_ID])) = '';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = N'UQ_fragment_current'
      AND parent_object_id = OBJECT_ID(N'dbo.ai_ir_fragment_snapshots')
)
BEGIN
    ALTER TABLE [dbo].[ai_ir_fragment_snapshots]
        ADD CONSTRAINT [UQ_fragment_current]
        UNIQUE ([F_TenantId], [F_ProjectId], [F_PIPELINE_ID], [F_FragmentId]);
END
GO
