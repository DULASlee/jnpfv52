-- P8-C Batch 07 — ADD INDEX DDL (workflow-engine)
-- Generated: 2026-08-30
-- Phase: 8 — P8-C Autonomous Batch Production
-- Scope: 6 tables, 16 indexes

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 07 ADD INDEX START ===';

-- flow_task_node (24 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASKNODE_TASK' AND object_id = OBJECT_ID('flow_task_node'))
    CREATE NONCLUSTERED INDEX IDX_TASKNODE_TASK ON flow_task_node (f_tenant_id, f_task_id)
    INCLUDE (f_id, f_node_code, f_node_name, f_state);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASKNODE_STATE' AND object_id = OBJECT_ID('flow_task_node'))
    CREATE NONCLUSTERED INDEX IDX_TASKNODE_STATE ON flow_task_node (f_tenant_id, f_state)
    INCLUDE (f_id, f_task_id, f_node_code, f_completion);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASKNODE_NODECODE' AND object_id = OBJECT_ID('flow_task_node'))
    CREATE NONCLUSTERED INDEX IDX_TASKNODE_NODECODE ON flow_task_node (f_tenant_id, f_node_code)
    INCLUDE (f_id, f_task_id, f_state);
PRINT '--- flow_task_node done ---';

-- flow_task_operator (28 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASKOPERATOR_TASK' AND object_id = OBJECT_ID('flow_task_operator'))
    CREATE NONCLUSTERED INDEX IDX_TASKOPERATOR_TASK ON flow_task_operator (f_tenant_id, f_task_id)
    INCLUDE (f_id, f_handle_id, f_state, f_completion);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASKOPERATOR_NODE' AND object_id = OBJECT_ID('flow_task_operator'))
    CREATE NONCLUSTERED INDEX IDX_TASKOPERATOR_NODE ON flow_task_operator (f_tenant_id, f_task_node_id)
    INCLUDE (f_id, f_handle_id, f_state);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASKOPERATOR_HANDLE' AND object_id = OBJECT_ID('flow_task_operator'))
    CREATE NONCLUSTERED INDEX IDX_TASKOPERATOR_HANDLE ON flow_task_operator (f_tenant_id, f_handle_id)
    INCLUDE (f_id, f_task_id, f_state);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TASKOPERATOR_STATE' AND object_id = OBJECT_ID('flow_task_operator'))
    CREATE NONCLUSTERED INDEX IDX_TASKOPERATOR_STATE ON flow_task_operator (f_tenant_id, f_state)
    INCLUDE (f_id, f_task_id, f_handle_id, f_completion);
PRINT '--- flow_task_operator done ---';

-- flow_template (19 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TEMPLATE_ENCODE' AND object_id = OBJECT_ID('flow_template'))
    CREATE NONCLUSTERED INDEX IDX_TEMPLATE_ENCODE ON flow_template (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_type);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_TEMPLATE_CATEGORY' AND object_id = OBJECT_ID('flow_template'))
    CREATE NONCLUSTERED INDEX IDX_TEMPLATE_CATEGORY ON flow_template (f_tenant_id, f_category)
    INCLUDE (f_id, f_full_name, f_en_code, f_type);
PRINT '--- flow_template done ---';

-- flow_form (27 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_FLOWFORM_ENCODE' AND object_id = OBJECT_ID('flow_form'))
    CREATE NONCLUSTERED INDEX IDX_FLOWFORM_ENCODE ON flow_form (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_form_type, f_flow_type);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_FLOWFORM_CATEGORY' AND object_id = OBJECT_ID('flow_form'))
    CREATE NONCLUSTERED INDEX IDX_FLOWFORM_CATEGORY ON flow_form (f_tenant_id, f_category)
    INCLUDE (f_id, f_full_name, f_en_code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_FLOWFORM_FLOWID' AND object_id = OBJECT_ID('flow_form'))
    CREATE NONCLUSTERED INDEX IDX_FLOWFORM_FLOWID ON flow_form (f_tenant_id, f_flow_id)
    INCLUDE (f_id, f_en_code, f_full_name);
PRINT '--- flow_form done ---';

-- flow_delegate (23 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DELEGATE_USER' AND object_id = OBJECT_ID('flow_delegate'))
    CREATE NONCLUSTERED INDEX IDX_DELEGATE_USER ON flow_delegate (f_tenant_id, f_user_id)
    INCLUDE (f_id, f_to_user_id, f_flow_id, f_start_time, f_end_time);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DELEGATE_TOUSER' AND object_id = OBJECT_ID('flow_delegate'))
    CREATE NONCLUSTERED INDEX IDX_DELEGATE_TOUSER ON flow_delegate (f_tenant_id, f_to_user_id)
    INCLUDE (f_id, f_user_id, f_flow_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DELEGATE_FLOW' AND object_id = OBJECT_ID('flow_delegate'))
    CREATE NONCLUSTERED INDEX IDX_DELEGATE_FLOW ON flow_delegate (f_tenant_id, f_flow_id)
    INCLUDE (f_id, f_user_id, f_to_user_id);
PRINT '--- flow_delegate done ---';

-- flow_candidates (18 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_CANDIDATES_TASK' AND object_id = OBJECT_ID('flow_candidates'))
    CREATE NONCLUSTERED INDEX IDX_CANDIDATES_TASK ON flow_candidates (f_tenant_id, f_task_id)
    INCLUDE (f_id, f_task_node_id, f_handle_id, f_account);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_CANDIDATES_HANDLE' AND object_id = OBJECT_ID('flow_candidates'))
    CREATE NONCLUSTERED INDEX IDX_CANDIDATES_HANDLE ON flow_candidates (f_tenant_id, f_handle_id)
    INCLUDE (f_id, f_task_id, f_account);
PRINT '--- flow_candidates done ---';

PRINT '=== Batch 07 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
