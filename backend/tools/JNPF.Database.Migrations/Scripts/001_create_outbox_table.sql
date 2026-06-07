IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SYS_EVENT_OUTBOX_MESSAGE')
BEGIN
    CREATE TABLE SYS_EVENT_OUTBOX_MESSAGE (
        Id              UNIQUEIDENTIFIER    NOT NULL    DEFAULT NEWID(),
        EventName       NVARCHAR(200)       NOT NULL,
        EventPayload    NVARCHAR(MAX)       NOT NULL,
        CreatedAt       DATETIME            NOT NULL    DEFAULT GETUTCDATE(),
        ProcessedAt     DATETIME            NULL,
        RetryCount      INT                 NOT NULL    DEFAULT 0,
        MaxRetryCount   INT                 NOT NULL    DEFAULT 3,
        Status          INT                 NOT NULL    DEFAULT 0,
        Error           NVARCHAR(MAX)       NULL,
        CONSTRAINT PK_SYS_EVENT_OUTBOX_MESSAGE PRIMARY KEY (Id)
    );

    CREATE INDEX IX_OUTBOX_STATUS_CREATED
        ON SYS_EVENT_OUTBOX_MESSAGE (Status, CreatedAt);

    CREATE INDEX IX_OUTBOX_EVENT_NAME
        ON SYS_EVENT_OUTBOX_MESSAGE (EventName);
END
GO
