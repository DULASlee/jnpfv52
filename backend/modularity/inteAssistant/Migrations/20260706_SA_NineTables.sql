-- SA 九步专用表 + 校验日志（sa-service SqlServerSADatabase 持久化）
-- 执行：run-inte-migration.mjs 或 sqlcmd

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sa_scope')
BEGIN
    CREATE TABLE [dbo].[sa_scope] (
        [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tenant_id] NVARCHAR(50) NOT NULL,
        [project_id] BIGINT NOT NULL,
        [asset_level] NVARCHAR(20) NOT NULL DEFAULT 'PROJECT',
        [event_id] BIGINT NULL,
        [system_boundary] NVARCHAR(MAX) NOT NULL,
        [external_entities] NVARCHAR(MAX) NOT NULL,
        [business_events] NVARCHAR(MAX) NOT NULL,
        [event_count] INT NOT NULL DEFAULT 0,
        [validation_status] NVARCHAR(20) NOT NULL DEFAULT 'PASS',
        [created_at] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [created_by] NVARCHAR(50) NOT NULL DEFAULT 'sa-service',
        [is_deleted] BIT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_sa_scope_project] ON [dbo].[sa_scope]([tenant_id], [project_id]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sa_dfd')
BEGIN
    CREATE TABLE [dbo].[sa_dfd] (
        [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tenant_id] NVARCHAR(50) NOT NULL,
        [project_id] BIGINT NOT NULL,
        [scope_id] BIGINT NOT NULL,
        [payload_json] NVARCHAR(MAX) NOT NULL,
        [validation_status] NVARCHAR(20) NOT NULL DEFAULT 'PASS',
        [created_at] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [created_by] NVARCHAR(50) NOT NULL DEFAULT 'sa-service',
        CONSTRAINT [FK_sa_dfd_scope] FOREIGN KEY ([scope_id]) REFERENCES [dbo].[sa_scope]([id])
    );
    CREATE INDEX [IX_sa_dfd_scope] ON [dbo].[sa_dfd]([scope_id]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sa_business_process')
BEGIN
    CREATE TABLE [dbo].[sa_business_process] (
        [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tenant_id] NVARCHAR(50) NOT NULL,
        [project_id] BIGINT NOT NULL,
        [dfd_id] BIGINT NOT NULL,
        [payload_json] NVARCHAR(MAX) NOT NULL,
        [validation_status] NVARCHAR(20) NOT NULL DEFAULT 'PASS',
        [created_at] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [created_by] NVARCHAR(50) NOT NULL DEFAULT 'sa-service',
        CONSTRAINT [FK_sa_bpm_dfd] FOREIGN KEY ([dfd_id]) REFERENCES [dbo].[sa_dfd]([id])
    );
    CREATE INDEX [IX_sa_bpm_dfd] ON [dbo].[sa_business_process]([dfd_id]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sa_data_dictionary')
BEGIN
    CREATE TABLE [dbo].[sa_data_dictionary] (
        [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tenant_id] NVARCHAR(50) NOT NULL,
        [project_id] BIGINT NOT NULL,
        [dfd_id] BIGINT NOT NULL,
        [bpm_id] BIGINT NOT NULL,
        [payload_json] NVARCHAR(MAX) NOT NULL,
        [validation_status] NVARCHAR(20) NOT NULL DEFAULT 'PASS',
        [created_at] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [created_by] NVARCHAR(50) NOT NULL DEFAULT 'sa-service',
        CONSTRAINT [FK_sa_dict_dfd] FOREIGN KEY ([dfd_id]) REFERENCES [dbo].[sa_dfd]([id]),
        CONSTRAINT [FK_sa_dict_bpm] FOREIGN KEY ([bpm_id]) REFERENCES [dbo].[sa_business_process]([id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sa_pspec')
BEGIN
    CREATE TABLE [dbo].[sa_pspec] (
        [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tenant_id] NVARCHAR(50) NOT NULL,
        [project_id] BIGINT NOT NULL,
        [event_id] BIGINT NULL,
        [dict_id] BIGINT NOT NULL,
        [bpm_id] BIGINT NOT NULL,
        [payload_json] NVARCHAR(MAX) NOT NULL,
        [validation_status] NVARCHAR(20) NOT NULL DEFAULT 'PASS',
        [created_at] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [created_by] NVARCHAR(50) NOT NULL DEFAULT 'sa-service',
        CONSTRAINT [FK_sa_pspec_dict] FOREIGN KEY ([dict_id]) REFERENCES [dbo].[sa_data_dictionary]([id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sa_decision_table')
BEGIN
    CREATE TABLE [dbo].[sa_decision_table] (
        [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tenant_id] NVARCHAR(50) NOT NULL,
        [project_id] BIGINT NOT NULL,
        [event_id] BIGINT NULL,
        [pspec_id] BIGINT NULL,
        [dict_id] BIGINT NOT NULL,
        [payload_json] NVARCHAR(MAX) NOT NULL,
        [validation_status] NVARCHAR(20) NOT NULL DEFAULT 'PASS',
        [created_at] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [created_by] NVARCHAR(50) NOT NULL DEFAULT 'sa-service',
        CONSTRAINT [FK_sa_dt_dict] FOREIGN KEY ([dict_id]) REFERENCES [dbo].[sa_data_dictionary]([id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sa_er')
BEGIN
    CREATE TABLE [dbo].[sa_er] (
        [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tenant_id] NVARCHAR(50) NOT NULL,
        [project_id] BIGINT NOT NULL,
        [dict_id] BIGINT NOT NULL,
        [payload_json] NVARCHAR(MAX) NOT NULL,
        [validation_status] NVARCHAR(20) NOT NULL DEFAULT 'PASS',
        [created_at] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [created_by] NVARCHAR(50) NOT NULL DEFAULT 'sa-service',
        CONSTRAINT [FK_sa_er_dict] FOREIGN KEY ([dict_id]) REFERENCES [dbo].[sa_data_dictionary]([id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sa_state_machine')
BEGIN
    CREATE TABLE [dbo].[sa_state_machine] (
        [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tenant_id] NVARCHAR(50) NOT NULL,
        [project_id] BIGINT NOT NULL,
        [event_id] BIGINT NULL,
        [dict_id] BIGINT NOT NULL,
        [bpm_id] BIGINT NOT NULL,
        [payload_json] NVARCHAR(MAX) NOT NULL,
        [validation_status] NVARCHAR(20) NOT NULL DEFAULT 'PASS',
        [created_at] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [created_by] NVARCHAR(50) NOT NULL DEFAULT 'sa-service',
        CONSTRAINT [FK_sa_std_dict] FOREIGN KEY ([dict_id]) REFERENCES [dbo].[sa_data_dictionary]([id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sa_ui')
BEGIN
    CREATE TABLE [dbo].[sa_ui] (
        [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tenant_id] NVARCHAR(50) NOT NULL,
        [project_id] BIGINT NOT NULL,
        [event_id] BIGINT NULL,
        [bpm_id] BIGINT NOT NULL,
        [dict_id] BIGINT NOT NULL,
        [payload_json] NVARCHAR(MAX) NOT NULL,
        [validation_status] NVARCHAR(20) NOT NULL DEFAULT 'PASS',
        [created_at] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [created_by] NVARCHAR(50) NOT NULL DEFAULT 'sa-service',
        CONSTRAINT [FK_sa_ui_bpm] FOREIGN KEY ([bpm_id]) REFERENCES [dbo].[sa_business_process]([id]),
        CONSTRAINT [FK_sa_ui_dict] FOREIGN KEY ([dict_id]) REFERENCES [dbo].[sa_data_dictionary]([id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sa_validate_log')
BEGIN
    CREATE TABLE [dbo].[sa_validate_log] (
        [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tenant_id] NVARCHAR(50) NOT NULL,
        [project_id] BIGINT NOT NULL,
        [sa_table_name] NVARCHAR(100) NOT NULL,
        [sa_record_id] BIGINT NULL,
        [validator_name] NVARCHAR(100) NOT NULL,
        [retry_count] INT NOT NULL DEFAULT 0,
        [validation_status] NVARCHAR(20) NOT NULL,
        [errors_json] NVARCHAR(MAX) NULL,
        [duration_ms] INT NOT NULL DEFAULT 0,
        [created_at] DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX [IX_sa_validate_log_project] ON [dbo].[sa_validate_log]([tenant_id], [project_id]);
END
GO
