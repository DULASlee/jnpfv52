-- ========================================
-- SA 流水线数据表完善:三元组(tenantId+projectId+pipelineId) + 冻结/恢复(checkpoint)
--
-- 背景:
--   1. 历史 pipeline≡project 隐式绑定,导致 BUG 修复/二次开发无法区分迭代分支
--   2. 缺少冻结/恢复语义,无法支持"开发任务对话冻结与重新拉起"
--   3. 部分 IR/Skill 表缺少 pipelineId,BASE_IR_VERSION 缺少 projectId
--   4. inte_assistant_attachment 的 TenantId 可空(fail-open 安全隐患)
--
-- 决策:
--   - 所有 SA 表强制持有三元组(tenantId + projectId + pipelineId),NOT NULL
--   - 全量 checkpoint(流水线状态 + 对话 + IR 版本快照)
--   - 解除 pipeline≡project 绑定,支持 Project 1:N Pipeline
--
-- 日期:2026-07-05
-- 作者:SA 流水线深度审查
-- ========================================

-- ════════════════════════════════════════════════════════
-- 1. BASE_AI_PIPELINE:加 ProjectId + 冻结/恢复字段
-- ════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE') AND name = 'F_PROJECT_ID')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE] ADD [F_PROJECT_ID] NVARCHAR(50) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE') AND name = 'F_FROZEN')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE] ADD [F_FROZEN] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE') AND name = 'F_FROZEN_AT')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE] ADD [F_FROZEN_AT] DATETIME2(7) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE') AND name = 'F_FROZEN_BY')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE] ADD [F_FROZEN_BY] NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE') AND name = 'F_FROZEN_REASON')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE] ADD [F_FROZEN_REASON] NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE') AND name = 'F_RESUME_COUNT')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE] ADD [F_RESUME_COUNT] INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE') AND name = 'F_LAST_RESUMED_AT')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE] ADD [F_LAST_RESUMED_AT] DATETIME2(7) NULL;

-- 全量 checkpoint:序列化 {currentStage, stages[], lastMessageIds[], irVersion, irSnapshot}
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE') AND name = 'F_CHECKPOINT')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE] ADD [F_CHECKPOINT] NVARCHAR(MAX) NULL;

-- ════════════════════════════════════════════════════════
-- 2. BASE_AI_PIPELINE_MESSAGE:加 ProjectId + 会话级冻结
-- ════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE_MESSAGE') AND name = 'F_PROJECT_ID')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE_MESSAGE] ADD [F_PROJECT_ID] NVARCHAR(50) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE_MESSAGE') AND name = 'F_SESSION_ID')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE_MESSAGE] ADD [F_SESSION_ID] NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_PIPELINE_MESSAGE') AND name = 'F_IS_FROZEN')
    ALTER TABLE [dbo].[BASE_AI_PIPELINE_MESSAGE] ADD [F_IS_FROZEN] BIT NOT NULL DEFAULT 0;

-- ════════════════════════════════════════════════════════
-- 3. BASE_AI_CALL_LOG:加 PipelineId(已有 ProjectId 可空)
-- ════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_AI_CALL_LOG') AND name = 'F_PIPELINE_ID')
    ALTER TABLE [dbo].[BASE_AI_CALL_LOG] ADD [F_PIPELINE_ID] NVARCHAR(50) NOT NULL DEFAULT '';

-- ════════════════════════════════════════════════════════
-- 4. ai_skill_runs:加 PipelineId(已有 ProjectId)
-- ════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ai_skill_runs') AND name = 'F_PIPELINE_ID')
    ALTER TABLE [dbo].[ai_skill_runs] ADD [F_PIPELINE_ID] NVARCHAR(50) NOT NULL DEFAULT '';

-- ════════════════════════════════════════════════════════
-- 5. ai_ir_events:加 PipelineId(已有 ProjectId)
-- ════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ai_ir_events') AND name = 'F_PIPELINE_ID')
    ALTER TABLE [dbo].[ai_ir_events] ADD [F_PIPELINE_ID] NVARCHAR(50) NOT NULL DEFAULT '';

-- ════════════════════════════════════════════════════════
-- 6. ai_ir_fragment_snapshots:加 PipelineId(已有 ProjectId)
-- ════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ai_ir_fragment_snapshots') AND name = 'F_PIPELINE_ID')
    ALTER TABLE [dbo].[ai_ir_fragment_snapshots] ADD [F_PIPELINE_ID] NVARCHAR(50) NOT NULL DEFAULT '';

-- ════════════════════════════════════════════════════════
-- 7. BASE_IR_VERSION:加 ProjectId(已有 PipelineId)
-- ════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_IR_VERSION') AND name = 'F_PROJECT_ID')
    ALTER TABLE [dbo].[BASE_IR_VERSION] ADD [F_PROJECT_ID] NVARCHAR(50) NOT NULL DEFAULT '';

-- ════════════════════════════════════════════════════════
-- 8. BASE_IR_EDIT_PATCH:加 ProjectId(已有 PipelineId)
-- ════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BASE_IR_EDIT_PATCH') AND name = 'F_PROJECT_ID')
    ALTER TABLE [dbo].[BASE_IR_EDIT_PATCH] ADD [F_PROJECT_ID] NVARCHAR(50) NOT NULL DEFAULT '';

-- ════════════════════════════════════════════════════════
-- 9. inte_assistant_attachment:加 ProjectId + 收紧 TenantId 为 NOT NULL
-- ════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inte_assistant_attachment') AND name = 'F_PROJECT_ID')
    ALTER TABLE [dbo].[inte_assistant_attachment] ADD [F_PROJECT_ID] NVARCHAR(50) NOT NULL DEFAULT '';

-- 收紧 TenantId 为 NOT NULL(fail-closed 安全铁律)
-- 先把 NULL 兜底为空串,再改列定义
-- 注意:inte_assistant_attachment 表的列名是驼峰风格(F_TenantId),非 F_TENANT_ID
UPDATE [dbo].[inte_assistant_attachment] SET [F_TenantId] = '' WHERE [F_TenantId] IS NULL;

-- ⚠ ALTER COLUMN 不允许在被索引依赖的列上直接修改(SQL Server 5074 错误)
-- F_TenantId 上有多个索引(老的 IX_attachment_tenant + 新建的 IDX_ATTACHMENT_TRIPLE),
-- 必须全部 DROP → ALTER → CREATE。用游标按列查找所有依赖索引,最稳。
DECLARE @drop_sql NVARCHAR(MAX);
DECLARE drop_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT 'DROP INDEX [' + i.name + '] ON [dbo].[inte_assistant_attachment]'
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID('inte_assistant_attachment')
      AND i.is_primary_key = 0
      AND i.is_unique_constraint = 0
      AND i.name IS NOT NULL
      AND EXISTS (
          SELECT 1 FROM sys.index_columns ic
          JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND c.name = 'F_TenantId'
      );
OPEN drop_cur;
FETCH NEXT FROM drop_cur INTO @drop_sql;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sp_executesql @drop_sql;
    FETCH NEXT FROM drop_cur INTO @drop_sql;
END
CLOSE drop_cur;
DEALLOCATE drop_cur;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inte_assistant_attachment') AND name = 'F_TenantId'
          AND is_nullable = 1)
BEGIN
    ALTER TABLE [dbo].[inte_assistant_attachment] ALTER COLUMN [F_TenantId] NVARCHAR(50) NOT NULL;
END;

-- 重建索引(三元组 IDX_ATTACHMENT_TRIPLE 会在第 11 段建,这里恢复原索引保持向后兼容)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_attachment_tenant'
               AND object_id = OBJECT_ID('inte_assistant_attachment'))
    CREATE INDEX [IX_attachment_tenant] ON [dbo].[inte_assistant_attachment]([F_TenantId], [F_PipelineId]);

-- 关键:GO 分隔批处理。SQL Server 同一批内编译时,ALTER TABLE 新增的列
-- 在后续 UPDATE/CREATE INDEX 语句解析时尚未进入元数据缓存,会报 207
-- 「列名无效」。必须用 GO 切断批次,让 DDL 先落库,再编译 DML。
GO

-- ════════════════════════════════════════════════════════
-- 10. 存量数据回填:pipelineId ≡ projectId(历史数据保持兼容)
-- ════════════════════════════════════════════════════════
-- 说明:历史数据中 pipelineId 和 projectId 是同一个值,回填保持兼容
-- 后续新建 pipeline 时,projectId 由 CreateAsync 设置(首次创建 = pipelineId;
-- 二次开发场景由 create-iteration API 继承原始 pipeline 的 ID)

UPDATE [dbo].[BASE_AI_PIPELINE]           SET [F_PROJECT_ID] = [F_ID]                 WHERE [F_PROJECT_ID] = '';
UPDATE [dbo].[BASE_AI_PIPELINE_MESSAGE]   SET [F_PROJECT_ID] = [F_PIPELINE_ID]       WHERE [F_PROJECT_ID] = '';
UPDATE [dbo].[BASE_IR_VERSION]            SET [F_PROJECT_ID] = [F_PIPELINE_ID]       WHERE [F_PROJECT_ID] = '';
UPDATE [dbo].[BASE_IR_EDIT_PATCH]         SET [F_PROJECT_ID] = CAST([F_PIPELINE_ID] AS NVARCHAR(50)) WHERE [F_PROJECT_ID] = '';

-- 这四张表的 ProjectId 列原为驼峰 F_ProjectId(早期建表),按其回填新加的 F_PIPELINE_ID
UPDATE [dbo].[BASE_AI_CALL_LOG]           SET [F_PIPELINE_ID] = [F_ProjectId]        WHERE [F_PIPELINE_ID] = '' AND [F_ProjectId] IS NOT NULL AND [F_ProjectId] <> '';
UPDATE [dbo].[ai_skill_runs]              SET [F_PIPELINE_ID] = [F_ProjectId]        WHERE [F_PIPELINE_ID] = '';
UPDATE [dbo].[ai_ir_events]               SET [F_PIPELINE_ID] = [F_ProjectId]        WHERE [F_PIPELINE_ID] = '';
UPDATE [dbo].[ai_ir_fragment_snapshots]   SET [F_PIPELINE_ID] = [F_ProjectId]        WHERE [F_PIPELINE_ID] = '';

-- attachment 表 PipelineId 已有,ProjectId 按 PipelineId 回填
UPDATE [dbo].[inte_assistant_attachment]  SET [F_PROJECT_ID] = [F_PipelineId]        WHERE [F_PROJECT_ID] = '';

-- ════════════════════════════════════════════════════════
-- 11. 索引(三元组查询 + 冻结状态查询 + 会话查询)
-- ════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PIPELINE_PROJECT')
    CREATE INDEX [IDX_PIPELINE_PROJECT] ON [dbo].[BASE_AI_PIPELINE]([F_TENANT_ID], [F_PROJECT_ID], [F_CURRENT_STAGE]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PIPELINE_FROZEN')
    CREATE INDEX [IDX_PIPELINE_FROZEN] ON [dbo].[BASE_AI_PIPELINE]([F_TENANT_ID], [F_FROZEN], [F_LAST_RESUMED_AT] DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PIPELINE_MSG_SESSION')
    CREATE INDEX [IDX_PIPELINE_MSG_SESSION] ON [dbo].[BASE_AI_PIPELINE_MESSAGE]([F_PIPELINE_ID], [F_SESSION_ID], [F_SEQUENCE]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_AI_CALL_LOG_TRIPLE')
    CREATE INDEX [IDX_AI_CALL_LOG_TRIPLE] ON [dbo].[BASE_AI_CALL_LOG]([F_TENANT_ID], [F_ProjectId], [F_PIPELINE_ID]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_IR_EVENT_TRIPLE')
    CREATE INDEX [IDX_IR_EVENT_TRIPLE] ON [dbo].[ai_ir_events]([F_TenantId], [F_ProjectId], [F_PIPELINE_ID], [F_Sequence]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_IR_VERSION_TRIPLE')
    CREATE INDEX [IDX_IR_VERSION_TRIPLE] ON [dbo].[BASE_IR_VERSION]([F_TENANT_ID], [F_PROJECT_ID], [F_PIPELINE_ID], [F_VERSION] DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ATTACHMENT_TRIPLE')
    CREATE INDEX [IDX_ATTACHMENT_TRIPLE] ON [dbo].[inte_assistant_attachment]([F_TenantId], [F_PROJECT_ID], [F_PipelineId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SKILL_RUNS_TRIPLE')
    CREATE INDEX [IDX_SKILL_RUNS_TRIPLE] ON [dbo].[ai_skill_runs]([F_TenantId], [F_ProjectId], [F_PIPELINE_ID], [F_StartedAt] DESC);
