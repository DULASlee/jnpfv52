IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PROCESSED_EVENT')
BEGIN
    CREATE TABLE PROCESSED_EVENT (
        EventId         NVARCHAR(200)       NOT NULL,
        HandlerName     NVARCHAR(200)       NOT NULL,
        ProcessedAt     DATETIME            NOT NULL    DEFAULT GETUTCDATE(),
        CONSTRAINT PK_PROCESSED_EVENT PRIMARY KEY (EventId, HandlerName)
    );
END
GO
