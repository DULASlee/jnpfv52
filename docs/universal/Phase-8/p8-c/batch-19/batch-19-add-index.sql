-- P8-C Batch 19 — ADD INDEX DDL (system-core-schedule + print)
-- Generated: 2026-08-30
-- Skill v1.0 (FROZEN): schema-drift detected + auto-fixed
-- Scope: 7 tables, ~16 indexes

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

PRINT '=== Batch 19 ADD INDEX START ===';

-- base_schedule (schedule / calendar; f_content/f_files are nvarchar(MAX))
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SCHEDULE_CATEGORY' AND object_id = OBJECT_ID('base_schedule'))
    CREATE NONCLUSTERED INDEX IDX_SCHEDULE_CATEGORY ON base_schedule (f_tenant_id, f_category)
    INCLUDE (f_id, f_title, f_start_day, f_creator_user_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SCHEDULE_GROUP' AND object_id = OBJECT_ID('base_schedule'))
    CREATE NONCLUSTERED INDEX IDX_SCHEDULE_GROUP ON base_schedule (f_tenant_id, f_group_id)
    INCLUDE (f_id, f_title, f_start_day);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SCHEDULE_CREATOR' AND object_id = OBJECT_ID('base_schedule'))
    CREATE NONCLUSTERED INDEX IDX_SCHEDULE_CREATOR ON base_schedule (f_tenant_id, f_creator_user_id, f_start_day)
    INCLUDE (f_id, f_title, f_category);
PRINT '--- base_schedule done ---';

-- base_schedule_log (schedule operation log; f_user_id/f_content are nvarchar(MAX))
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SCHEDULELOG_SCHEDULE' AND object_id = OBJECT_ID('base_schedule_log'))
    CREATE NONCLUSTERED INDEX IDX_SCHEDULELOG_SCHEDULE ON base_schedule_log (f_tenant_id, f_schedule_id)
    INCLUDE (f_id, f_operation_type, f_creator_time);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SCHEDULELOG_CATEGORY' AND object_id = OBJECT_ID('base_schedule_log'))
    CREATE NONCLUSTERED INDEX IDX_SCHEDULELOG_CATEGORY ON base_schedule_log (f_tenant_id, f_category, f_creator_time)
    INCLUDE (f_id, f_schedule_id, f_operation_type);
PRINT '--- base_schedule_log done ---';

-- base_schedule_user (schedule recipients)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SCHEDULEUSER_SCHEDULE' AND object_id = OBJECT_ID('base_schedule_user'))
    CREATE NONCLUSTERED INDEX IDX_SCHEDULEUSER_SCHEDULE ON base_schedule_user (f_tenant_id, f_schedule_id)
    INCLUDE (f_id, f_to_user_id, f_type);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SCHEDULEUSER_USER' AND object_id = OBJECT_ID('base_schedule_user'))
    CREATE NONCLUSTERED INDEX IDX_SCHEDULEUSER_USER ON base_schedule_user (f_tenant_id, f_to_user_id)
    INCLUDE (f_id, f_schedule_id, f_type);
PRINT '--- base_schedule_user done ---';

-- base_time_task (timed tasks; f_execute_content/f_execute_cycle_json are nvarchar(MAX))
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TIMETASK_CODE' AND object_id = OBJECT_ID('base_time_task'))
    CREATE NONCLUSTERED INDEX IDX_TIMETASK_CODE ON base_time_task (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_enabled_mark);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TIMETASK_ENABLED' AND object_id = OBJECT_ID('base_time_task'))
    CREATE NONCLUSTERED INDEX IDX_TIMETASK_ENABLED ON base_time_task (f_tenant_id, f_enabled_mark, f_next_run_time)
    INCLUDE (f_id, f_full_name, f_execute_type);
PRINT '--- base_time_task done ---';

-- base_time_task_log
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TIMETASKLOG_TASK' AND object_id = OBJECT_ID('base_time_task_log'))
    CREATE NONCLUSTERED INDEX IDX_TIMETASKLOG_TASK ON base_time_task_log (f_tenant_id, f_task_id, f_run_time DESC)
    INCLUDE (f_id, f_run_result);
PRINT '--- base_time_task_log done ---';

-- base_print_log (print history)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PRINTLOG_PRINT' AND object_id = OBJECT_ID('base_print_log'))
    CREATE NONCLUSTERED INDEX IDX_PRINTLOG_PRINT ON base_print_log (f_tenant_id, f_print_id, f_creator_time DESC)
    INCLUDE (f_id, f_print_title, f_print_num);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PRINTLOG_USER' AND object_id = OBJECT_ID('base_print_log'))
    CREATE NONCLUSTERED INDEX IDX_PRINTLOG_USER ON base_print_log (f_tenant_id, f_creator_user_id, f_creator_time DESC)
    INCLUDE (f_id, f_print_title);
PRINT '--- base_print_log done ---';

-- base_print_template (print template; f_sql_template/f_print_template/f_page_param/f_parameter_json are nvarchar(MAX))
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PRINTTPL_CODE' AND object_id = OBJECT_ID('base_print_template'))
    CREATE NONCLUSTERED INDEX IDX_PRINTTPL_CODE ON base_print_template (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_category, f_type);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PRINTTPL_CATEGORY' AND object_id = OBJECT_ID('base_print_template'))
    CREATE NONCLUSTERED INDEX IDX_PRINTTPL_CATEGORY ON base_print_template (f_tenant_id, f_category)
    INCLUDE (f_id, f_full_name, f_type, f_enabled_mark);
PRINT '--- base_print_template done ---';

PRINT '=== Batch 19 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
