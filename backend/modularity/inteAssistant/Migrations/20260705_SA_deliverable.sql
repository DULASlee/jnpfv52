-- 流水线阶段交付物索引表（S0 门控报告等）
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'inte_assistant_deliverable')
BEGIN
    CREATE TABLE [dbo].[inte_assistant_deliverable] (
        [F_Id]           NVARCHAR(50)   NOT NULL,
        [F_PipelineId]   NVARCHAR(50)   NOT NULL,
        [F_StageCode]    NVARCHAR(10)   NOT NULL,
        [F_FileName]     NVARCHAR(260)  NOT NULL,
        [F_RelativePath] NVARCHAR(500)  NOT NULL,
        [F_ContentType]  NVARCHAR(100)  NOT NULL DEFAULT 'application/octet-stream',
        [F_FileSize]     BIGINT         NOT NULL DEFAULT 0,
        [F_CreatorTime]  DATETIME       NOT NULL DEFAULT GETDATE(),
        [F_TenantId]     NVARCHAR(50)   NOT NULL DEFAULT '',
        [F_DeleteMark]   BIT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_inte_assistant_deliverable] PRIMARY KEY ([F_Id])
    );

    CREATE INDEX [IX_deliverable_pipeline]
        ON [dbo].[inte_assistant_deliverable] ([F_PipelineId], [F_StageCode])
        WHERE [F_DeleteMark] = 0;
END
