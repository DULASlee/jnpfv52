-- ========================================
-- S2 流水线进度表（L2 唯一真相）
-- CR-20260718-01 P4 阶段 2
-- 日期：2026-07-18
-- ========================================

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'BASE_AI_PIPELINE_S2_PROGRESS' AND xtype = 'U')
BEGIN
    CREATE TABLE [dbo].[BASE_AI_PIPELINE_S2_PROGRESS] (
        [F_Id]                  NVARCHAR(50)    NOT NULL,
        [F_TENANT_ID]           NVARCHAR(50)    NOT NULL,
        [F_PROJECT_ID]          NVARCHAR(50)    NOT NULL,
        [F_PIPELINE_ID]         NVARCHAR(50)    NOT NULL,
        [F_PIPELINE_STAGE]      INT             NOT NULL DEFAULT 0,
        [F_SPEC_PHASE]          INT             NOT NULL DEFAULT 0,
        [F_CLAR_ROUND]          INT             NOT NULL DEFAULT 0,
        [F_SPEC_VERSION]        INT             NOT NULL DEFAULT 1,
        [F_CONTENT_HASH]        NVARCHAR(128)   NULL,
        [F_CONTENT_LENGTH]      INT             NULL,
        [F_AWAITING_USER]       BIT             NOT NULL DEFAULT 0,
        [F_CREATOR_TIME]        DATETIME2(7)    NULL,
        [F_CREATOR_USER_ID]     NVARCHAR(50)    NULL,
        [F_LAST_MODIFY_TIME]    DATETIME2(7)    NULL,
        [F_LAST_MODIFY_USER_ID] NVARCHAR(50)    NULL,
        [F_DELETE_MARK]         INT             NOT NULL DEFAULT 0,
        [F_DELETE_TIME]         DATETIME2(7)    NULL,
        [F_DELETE_USER_ID]      NVARCHAR(50)    NULL,
        [F_SORT_CODE]           BIGINT          NULL,
        [F_ENABLED_MARK]        INT             NOT NULL DEFAULT 1,
        CONSTRAINT [PK_BASE_AI_PIPELINE_S2_PROGRESS] PRIMARY KEY ([F_Id])
    );

    CREATE UNIQUE INDEX [UX_s2_progress_triple]
        ON [dbo].[BASE_AI_PIPELINE_S2_PROGRESS] ([F_TENANT_ID], [F_PROJECT_ID], [F_PIPELINE_ID])
        WHERE [F_DELETE_MARK] = 0;
END;
