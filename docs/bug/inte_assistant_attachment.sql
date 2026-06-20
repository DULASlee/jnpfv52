-- ============================================================
-- SA 门控附件持久化表（企业级 v2）
-- 用途：存储 Pipeline 关联的附件信息和解析后的文本缓存
-- 版本：v2.0
-- ============================================================

CREATE TABLE [dbo].[inte_assistant_attachment] (
    [F_Id]                NVARCHAR(50)    NOT NULL,                                    -- 主键
    [F_PipelineId]        NVARCHAR(50)    NOT NULL,                                    -- 关联的Pipeline
    [F_FileName]          NVARCHAR(200)   NOT NULL,                                    -- 文件名（需求文档.docx）
    [F_FileUrl]           NVARCHAR(500)   NOT NULL,                                    -- 文件存储地址（/api/File/xxx）
    [F_FileSize]          BIGINT          NOT NULL DEFAULT 0,                          -- 文件大小（字节）
    [F_FileType]          NVARCHAR(20)    NOT NULL,                                    -- 文件类型（docx/xlsx/pdf/png）
    [F_FileHash]          NVARCHAR(64)    NULL,                                        -- 文件内容SHA256（去重用）
    [F_ExtractedText]     NVARCHAR(MAX)   NULL,                                        -- 解析后的纯文本（缓存）
    [F_ProcessStatus]     INT             NOT NULL DEFAULT 0,                          -- 0待处理 1处理中 2已完成 3失败
    [F_ProcessError]      NVARCHAR(2000)  NULL,                                        -- 处理失败原因（含堆栈）
    [F_CreatorUserId]     NVARCHAR(50)    NULL,                                        -- 上传人ID
    [F_CreatorUserName]   NVARCHAR(50)    NULL,                                        -- 上传人姓名（冗余，查询用）
    [F_CreatorTime]       DATETIME        NOT NULL DEFAULT GETDATE(),                  -- 创建时间
    [F_LastModifyUserId]  NVARCHAR(50)    NULL,                                        -- 最后修改人ID
    [F_LastModifyTime]    DATETIME        NULL,                                        -- 最后修改时间
    [F_DeleteMark]        BIT             NOT NULL DEFAULT 0,                          -- 软删除标记：0正常 1已删除
    [F_TenantId]          NVARCHAR(50)    NULL,                                        -- 租户ID

    CONSTRAINT [PK_inte_assistant_attachment] PRIMARY KEY ([F_Id]),
    CONSTRAINT [CK_attachment_status] CHECK ([F_ProcessStatus] IN (0, 1, 2, 3)),
    CONSTRAINT [UQ_attachment_pipeline_url] UNIQUE ([F_PipelineId], [F_FileUrl])        -- 同一Pipeline下同URL不重复
);

-- ═══ 索引 ═══

-- 按Pipeline查询附件（主查询路径）
CREATE INDEX [IX_attachment_pipeline]
    ON [dbo].[inte_assistant_attachment] ([F_PipelineId])
    INCLUDE ([F_FileName], [F_FileType], [F_FileSize], [F_ProcessStatus]);

-- 按租户+Pipeline查询（多租户隔离）
CREATE INDEX [IX_attachment_tenant]
    ON [dbo].[inte_assistant_attachment] ([F_TenantId], [F_PipelineId])
    WHERE [F_DeleteMark] = 0;

-- 按状态查询待处理附件（后台任务用）
CREATE INDEX [IX_attachment_pending]
    ON [dbo].[inte_assistant_attachment] ([F_ProcessStatus], [F_CreatorTime])
    WHERE [F_ProcessStatus] = 0 AND [F_DeleteMark] = 0;

-- 按文件Hash去重查询
CREATE INDEX [IX_attachment_hash]
    ON [dbo].[inte_assistant_attachment] ([F_FileHash])
    WHERE [F_FileHash] IS NOT NULL AND [F_DeleteMark] = 0;

-- ═══ 表注释 ═══

EXEC sp_addextendedproperty N'MS_Description', N'AI助手-附件持久化表（存储Pipeline关联附件及解析缓存）',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment';

EXEC sp_addextendedproperty N'MS_Description', N'主键（GUID）',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_Id';

EXEC sp_addextendedproperty N'MS_Description', N'关联Pipeline ID',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_PipelineId';

EXEC sp_addextendedproperty N'MS_Description', N'文件名',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_FileName';

EXEC sp_addextendedproperty N'MS_Description', N'文件存储地址',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_FileUrl';

EXEC sp_addextendedproperty N'MS_Description', N'文件大小（字节）',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_FileSize';

EXEC sp_addextendedproperty N'MS_Description', N'文件类型（docx/xlsx/pdf/png/jpg）',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_FileType';

EXEC sp_addextendedproperty N'MS_Description', N'文件内容SHA256哈希（去重用）',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_FileHash';

EXEC sp_addextendedproperty N'MS_Description', N'解析后的纯文本（缓存，避免重复解析）',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_ExtractedText';

EXEC sp_addextendedproperty N'MS_Description', N'处理状态：0待处理 1处理中 2已完成 3失败',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_ProcessStatus';

EXEC sp_addextendedproperty N'MS_Description', N'处理失败原因（含堆栈）',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_ProcessError';

EXEC sp_addextendedproperty N'MS_Description', N'上传人ID',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_CreatorUserId';

EXEC sp_addextendedproperty N'MS_Description', N'上传人姓名',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_CreatorUserName';

EXEC sp_addextendedproperty N'MS_Description', N'软删除标记：0正常 1已删除',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_DeleteMark';

EXEC sp_addextendedproperty N'MS_Description', N'租户ID',
    N'SCHEMA', N'dbo', N'TABLE', N'inte_assistant_attachment', N'COLUMN', N'F_TenantId';
