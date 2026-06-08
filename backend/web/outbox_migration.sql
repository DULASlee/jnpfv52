-- ============================================================
-- Outbox Pipeline Tables Migration
-- Date: 2026-06-07
-- Stage: 5.3 - Event Reliability Pipeline
-- Description: Creates SYS_EVENT_OUTBOX_MESSAGE and SYS_PROCESSED_EVENT tables
-- ============================================================

-- Table 1: SYS_EVENT_OUTBOX_MESSAGE (Outbox pattern for reliable event delivery)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SYS_EVENT_OUTBOX_MESSAGE')
BEGIN
    CREATE TABLE [dbo].[SYS_EVENT_OUTBOX_MESSAGE](
        [F_ID] [uniqueidentifier] NOT NULL,
        [F_EVENT_NAME] [nvarchar](200) NOT NULL,
        [F_EVENT_PAYLOAD] [text] NOT NULL,
        [F_CREATED_AT] [datetime] NOT NULL,
        [F_PROCESSED_AT] [datetime] NULL,
        [F_RETRY_COUNT] [int] NOT NULL DEFAULT(0),
        [F_MAX_RETRY_COUNT] [int] NOT NULL DEFAULT(3),
        [F_STATUS] [int] NOT NULL DEFAULT(0),
        [F_ERROR] [text] NULL,
        CONSTRAINT [PK_SYS_EVENT_OUTBOX_MESSAGE] PRIMARY KEY CLUSTERED ([F_ID])
    );

    -- Index for Dispatcher polling: status + created_at
    CREATE NONCLUSTERED INDEX [IX_OUTBOX_STATUS_CREATED]
        ON [dbo].[SYS_EVENT_OUTBOX_MESSAGE]([F_STATUS], [F_CREATED_AT]);

    -- Index for dead letter management queries
    CREATE NONCLUSTERED INDEX [IX_OUTBOX_EVENT_NAME]
        ON [dbo].[SYS_EVENT_OUTBOX_MESSAGE]([F_EVENT_NAME]);

    PRINT 'Table SYS_EVENT_OUTBOX_MESSAGE created.';
END
ELSE
BEGIN
    PRINT 'Table SYS_EVENT_OUTBOX_MESSAGE already exists, skipping.';
END
GO

-- Table 2: SYS_PROCESSED_EVENT (Idempotency records)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SYS_PROCESSED_EVENT')
BEGIN
    CREATE TABLE [dbo].[SYS_PROCESSED_EVENT](
        [F_EVENT_ID] [nvarchar](200) NOT NULL,
        [F_HANDLER_NAME] [nvarchar](200) NOT NULL,
        [F_PROCESSED_AT] [datetime] NOT NULL DEFAULT(GETUTCDATE()),
        CONSTRAINT [PK_SYS_PROCESSED_EVENT] PRIMARY KEY CLUSTERED ([F_EVENT_ID], [F_HANDLER_NAME])
    );

    PRINT 'Table SYS_PROCESSED_EVENT created.';
END
ELSE
BEGIN
    PRINT 'Table SYS_PROCESSED_EVENT already exists, skipping.';
END
GO
