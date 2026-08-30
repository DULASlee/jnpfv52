-- P8-C Batch 10 — ADD INDEX DDL (workflow-engine remaining)
-- Generated: 2026-08-30
-- Scope: 6 tables, 14 indexes

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 10 ADD INDEX START ===';

-- flow_task (41 cols, Pilot 3 R3+ — was READY pending HG#5; add runtime indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASK_FLOW' AND object_id = OBJECT_ID('flow_task'))
    CREATE NONCLUSTERED INDEX IDX_TASK_FLOW ON flow_task (f_tenant_id, f_flow_id)
    INCLUDE (f_id, f_full_name, f_status, f_current_node_code, f_start_time);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASK_STATUS' AND object_id = OBJECT_ID('flow_task'))
    CREATE NONCLUSTERED INDEX IDX_TASK_STATUS ON flow_task (f_tenant_id, f_status, f_start_time DESC)
    INCLUDE (f_id, f_flow_id, f_full_name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASK_ENCODE' AND object_id = OBJECT_ID('flow_task'))
    CREATE NONCLUSTERED INDEX IDX_TASK_ENCODE ON flow_task (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_flow_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASK_CREATOR' AND object_id = OBJECT_ID('flow_task'))
    CREATE NONCLUSTERED INDEX IDX_TASK_CREATOR ON flow_task (f_tenant_id, f_creator_user_id, f_creator_time DESC)
    INCLUDE (f_id, f_flow_id, f_status);
PRINT '--- flow_task done ---';

-- flow_comment
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_COMMENT_TASK' AND object_id = OBJECT_ID('flow_comment'))
    CREATE NONCLUSTERED INDEX IDX_COMMENT_TASK ON flow_comment (f_tenant_id, f_task_id, f_creator_time DESC)
    INCLUDE (f_id, f_text, f_image);
PRINT '--- flow_comment done ---';

-- flow_event_log
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EVENTLOG_TASKNODE' AND object_id = OBJECT_ID('flow_event_log'))
    CREATE NONCLUSTERED INDEX IDX_EVENTLOG_TASKNODE ON flow_event_log (f_tenant_id, f_task_node_id)
    INCLUDE (f_id, f_full_name, f_result, f_creator_time);
PRINT '--- flow_event_log done ---';

-- flow_task_operator_user
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_OPERATORUSER_TASK' AND object_id = OBJECT_ID('flow_task_operator_user'))
    CREATE NONCLUSTERED INDEX IDX_OPERATORUSER_TASK ON flow_task_operator_user (f_tenant_id, f_task_id)
    INCLUDE (f_id, f_handle_id, f_state);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_OPERATORUSER_HANDLE' AND object_id = OBJECT_ID('flow_task_operator_user'))
    CREATE NONCLUSTERED INDEX IDX_OPERATORUSER_HANDLE ON flow_task_operator_user (f_tenant_id, f_handle_id)
    INCLUDE (f_id, f_task_id, f_state);
PRINT '--- flow_task_operator_user done ---';

-- flow_task_circulate
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_CIRCULATE_TASK' AND object_id = OBJECT_ID('flow_task_circulate'))
    CREATE NONCLUSTERED INDEX IDX_CIRCULATE_TASK ON flow_task_circulate (f_tenant_id, f_task_id)
    INCLUDE (f_id, f_node_code, f_node_name);
PRINT '--- flow_task_circulate done ---';

-- flow_visible
PRINT '--- flow_visible ---';
DECLARE @fv_cols NVARCHAR(MAX);
SELECT @fv_cols = STRING_AGG(COLUMN_NAME, ',') FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'flow_visible';
PRINT 'flow_visible columns: ' + ISNULL(@fv_cols, 'none');

PRINT '=== Batch 10 ADD INDEX COMPLETE ===';
COMMIT TRANSACTION;
GO
