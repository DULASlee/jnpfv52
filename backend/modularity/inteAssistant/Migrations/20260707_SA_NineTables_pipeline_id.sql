-- SA 九表补 pipeline_id（三元组 tenant_id + project_id + pipeline_id）
-- 对齐 20260705_SA_三元组与冻结恢复.sql
-- 历史数据：pipeline_id 回填为 project_id（greenfield 时二者相等）

SET QUOTED_IDENTIFIER ON;
GO

DECLARE @tables TABLE (name SYSNAME);
INSERT INTO @tables (name) VALUES
    (N'sa_scope'), (N'sa_dfd'), (N'sa_business_process'), (N'sa_data_dictionary'),
    (N'sa_pspec'), (N'sa_decision_table'), (N'sa_er'), (N'sa_state_machine'),
    (N'sa_ui'), (N'sa_validate_log');

DECLARE @t SYSNAME;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM @tables;
OPEN cur;
FETCH NEXT FROM cur INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = @t)
       AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(@t) AND name = 'pipeline_id')
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[' + @t + N'] ADD [pipeline_id] BIGINT NOT NULL CONSTRAINT [DF_' + @t + N'_pipeline_id] DEFAULT 0;');
    END
    FETCH NEXT FROM cur INTO @t;
END
CLOSE cur;
DEALLOCATE cur;
GO

-- 存量回填：pipeline_id = project_id（九表均有版本触发器，回填时临时禁用 ALL）
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @tables TABLE (name SYSNAME);
INSERT INTO @tables (name) VALUES
    (N'sa_scope'), (N'sa_dfd'), (N'sa_business_process'), (N'sa_data_dictionary'),
    (N'sa_pspec'), (N'sa_decision_table'), (N'sa_er'), (N'sa_state_machine'),
    (N'sa_ui'), (N'sa_validate_log');

DECLARE @t SYSNAME;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM @tables;
OPEN cur;
FETCH NEXT FROM cur INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = @t)
       AND EXISTS (SELECT 1 FROM sys.triggers WHERE parent_id = OBJECT_ID(@t))
        EXEC(N'ALTER TABLE [dbo].[' + @t + N'] DISABLE TRIGGER ALL;');
    FETCH NEXT FROM cur INTO @t;
END
CLOSE cur;
DEALLOCATE cur;
GO

IF OBJECT_ID(N'dbo.sa_scope', N'U') IS NOT NULL
    UPDATE [dbo].[sa_scope] SET [pipeline_id] = [project_id] WHERE [pipeline_id] = 0;
IF OBJECT_ID(N'dbo.sa_dfd', N'U') IS NOT NULL
    UPDATE [dbo].[sa_dfd] SET [pipeline_id] = [project_id] WHERE [pipeline_id] = 0;
IF OBJECT_ID(N'dbo.sa_business_process', N'U') IS NOT NULL
    UPDATE [dbo].[sa_business_process] SET [pipeline_id] = [project_id] WHERE [pipeline_id] = 0;
IF OBJECT_ID(N'dbo.sa_data_dictionary', N'U') IS NOT NULL
    UPDATE [dbo].[sa_data_dictionary] SET [pipeline_id] = [project_id] WHERE [pipeline_id] = 0;
IF OBJECT_ID(N'dbo.sa_pspec', N'U') IS NOT NULL
    UPDATE [dbo].[sa_pspec] SET [pipeline_id] = [project_id] WHERE [pipeline_id] = 0;
IF OBJECT_ID(N'dbo.sa_decision_table', N'U') IS NOT NULL
    UPDATE [dbo].[sa_decision_table] SET [pipeline_id] = [project_id] WHERE [pipeline_id] = 0;
IF OBJECT_ID(N'dbo.sa_er', N'U') IS NOT NULL
    UPDATE [dbo].[sa_er] SET [pipeline_id] = [project_id] WHERE [pipeline_id] = 0;
IF OBJECT_ID(N'dbo.sa_state_machine', N'U') IS NOT NULL
    UPDATE [dbo].[sa_state_machine] SET [pipeline_id] = [project_id] WHERE [pipeline_id] = 0;
IF OBJECT_ID(N'dbo.sa_ui', N'U') IS NOT NULL
    UPDATE [dbo].[sa_ui] SET [pipeline_id] = [project_id] WHERE [pipeline_id] = 0;
IF OBJECT_ID(N'dbo.sa_validate_log', N'U') IS NOT NULL
    UPDATE [dbo].[sa_validate_log] SET [pipeline_id] = [project_id] WHERE [pipeline_id] = 0;
GO

DECLARE @tables2 TABLE (name SYSNAME);
INSERT INTO @tables2 (name) VALUES
    (N'sa_scope'), (N'sa_dfd'), (N'sa_business_process'), (N'sa_data_dictionary'),
    (N'sa_pspec'), (N'sa_decision_table'), (N'sa_er'), (N'sa_state_machine'),
    (N'sa_ui'), (N'sa_validate_log');

DECLARE @t2 SYSNAME;
DECLARE cur2 CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM @tables2;
OPEN cur2;
FETCH NEXT FROM cur2 INTO @t2;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = @t2)
       AND EXISTS (SELECT 1 FROM sys.triggers WHERE parent_id = OBJECT_ID(@t2))
        EXEC(N'ALTER TABLE [dbo].[' + @t2 + N'] ENABLE TRIGGER ALL;');
    FETCH NEXT FROM cur2 INTO @t2;
END
CLOSE cur2;
DEALLOCATE cur2;
GO

-- 三元组索引（表不存在则跳过）
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sa_scope', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sa_scope_triple')
    CREATE INDEX [IX_sa_scope_triple] ON [dbo].[sa_scope]([tenant_id], [project_id], [pipeline_id]);

IF OBJECT_ID(N'dbo.sa_dfd', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sa_dfd_triple')
    CREATE INDEX [IX_sa_dfd_triple] ON [dbo].[sa_dfd]([tenant_id], [project_id], [pipeline_id]);

IF OBJECT_ID(N'dbo.sa_business_process', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sa_bpm_triple')
    CREATE INDEX [IX_sa_bpm_triple] ON [dbo].[sa_business_process]([tenant_id], [project_id], [pipeline_id]);

IF OBJECT_ID(N'dbo.sa_data_dictionary', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sa_dict_triple')
    CREATE INDEX [IX_sa_dict_triple] ON [dbo].[sa_data_dictionary]([tenant_id], [project_id], [pipeline_id]);

IF OBJECT_ID(N'dbo.sa_pspec', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sa_pspec_triple')
    CREATE INDEX [IX_sa_pspec_triple] ON [dbo].[sa_pspec]([tenant_id], [project_id], [pipeline_id]);

IF OBJECT_ID(N'dbo.sa_decision_table', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sa_dt_triple')
    CREATE INDEX [IX_sa_dt_triple] ON [dbo].[sa_decision_table]([tenant_id], [project_id], [pipeline_id]);

IF OBJECT_ID(N'dbo.sa_er', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sa_er_triple')
    CREATE INDEX [IX_sa_er_triple] ON [dbo].[sa_er]([tenant_id], [project_id], [pipeline_id]);

IF OBJECT_ID(N'dbo.sa_state_machine', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sa_std_triple')
    CREATE INDEX [IX_sa_std_triple] ON [dbo].[sa_state_machine]([tenant_id], [project_id], [pipeline_id]);

IF OBJECT_ID(N'dbo.sa_ui', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sa_ui_triple')
    CREATE INDEX [IX_sa_ui_triple] ON [dbo].[sa_ui]([tenant_id], [project_id], [pipeline_id]);

IF OBJECT_ID(N'dbo.sa_validate_log', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sa_validate_log_triple')
    CREATE INDEX [IX_sa_validate_log_triple] ON [dbo].[sa_validate_log]([tenant_id], [project_id], [pipeline_id]);
GO
