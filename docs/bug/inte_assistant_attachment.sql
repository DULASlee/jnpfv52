-- ============================================================
-- SA 门控附件持久化表
-- 用途：存储 Pipeline 关联的附件信息和解析后的文本缓存
-- ============================================================

CREATE TABLE [dbo].[inte_assistant_attachment] (
    [F_Id]              NVARCHAR(50)    NOT NULL,     -- 主键
    [F_PipelineId]      NVARCHAR(50)    NOT NULL,     -- 关联的Pipeline
    [F_FileName]        NVARCHAR(200)   NOT NULL,     -- 文件名（需求文档.docx）
    [F_FileUrl]         NVARCHAR(500)   NOT NULL,     -- 文件存储地址（/api/File/xxx）
    [F_FileSize]        BIGINT          NOT NULL DEFAULT 0,  -- 文件大小（字节）
    [F_FileType]        NVARCHAR(20)    NOT NULL,     -- 文件类型（docx/xlsx/pdf/png）
    [F_ExtractedText]   NVARCHAR(MAX)   NULL,         -- 解析后的纯文本（缓存）
    [F_ProcessStatus]   INT             NOT NULL DEFAULT 0,  -- 0待处理 1处理中 2已完成 3失败
    [F_ProcessError]    NVARCHAR(500)   NULL,         -- 处理失败原因
    [F_CreatorTime]     DATETIME        NOT NULL DEFAULT GETDATE(),
    [F_TenantId]        NVARCHAR(50)    NULL,         -- 租户ID

    CONSTRAINT [PK_inte_assistant_attachment] PRIMARY KEY ([F_Id])
);

-- 索引：按Pipeline查询附件
CREATE INDEX [IX_attachment_pipeline] ON [dbo].[inte_assistant_attachment] ([F_PipelineId]);

-- 索引：按租户+Pipeline查询（多租户隔离）
CREATE INDEX [IX_attachment_tenant] ON [dbo].[inte_assistant_attachment] ([F_TenantId], [F_PipelineId]);
