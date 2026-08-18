-- 附件解析分块存档说明（2026-07-12）
-- 权威全文不靠 F_ExtractedText 单字段截断，而落在 StudioWorkspace 文件分块：
--   {StudioWorkspace}/{tenantId}/[{projectId}/]{pipelineId}/attachments/{attachmentId}/
--     manifest.json
--     chunks/0000.txt … NNNN.txt
--
-- 本脚本不强制建表：分块存档以文件系统为准（增量写 / 按序合并读）。
-- F_ExtractedText 仍写合并结果作兼容缓存（nvarchar(max)）。
--
-- 若需 SQL 侧审计，可选用下列可选表（非门控主路径）：

IF OBJECT_ID(N'dbo.inte_assistant_attachment_chunk', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.inte_assistant_attachment_chunk
    (
        F_Id            NVARCHAR(50)  NOT NULL CONSTRAINT PK_att_chunk PRIMARY KEY,
        F_TenantId      NVARCHAR(50)  NOT NULL,
        F_PROJECT_ID    NVARCHAR(50)  NOT NULL CONSTRAINT DF_att_chunk_project DEFAULT (N''),
        F_PipelineId    NVARCHAR(50)  NOT NULL,
        F_AttachmentId  NVARCHAR(50)  NOT NULL,
        F_ChunkIndex    INT           NOT NULL,
        F_CharCount     INT           NOT NULL CONSTRAINT DF_att_chunk_chars DEFAULT (0),
        F_SourceHint    NVARCHAR(200) NULL,
        F_RelativePath  NVARCHAR(400) NOT NULL,
        F_CreatorTime   DATETIME      NOT NULL CONSTRAINT DF_att_chunk_ctime DEFAULT (GETDATE()),
        CONSTRAINT UQ_att_chunk_triple UNIQUE (F_TenantId, F_PROJECT_ID, F_PipelineId, F_AttachmentId, F_ChunkIndex)
    );
    CREATE INDEX IX_att_chunk_att ON dbo.inte_assistant_attachment_chunk (F_AttachmentId, F_ChunkIndex);
END
GO
