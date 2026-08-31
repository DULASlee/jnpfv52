-- P8-C Batch 12 — ADD INDEX DDL (system-extension + visualdata remaining)
-- Generated: 2026-08-30
-- Scope: 6 tables, 14 indexes

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 12 ADD INDEX START ===';

-- ext_document (22 cols, hierarchical, share tracking)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DOCUMENT_PARENT' AND object_id = OBJECT_ID('ext_document'))
    CREATE NONCLUSTERED INDEX IDX_DOCUMENT_PARENT ON ext_document (f_tenant_id, f_parent_id)
    INCLUDE (f_id, f_full_name, f_file_extension, f_is_share);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DOCUMENT_TYPE' AND object_id = OBJECT_ID('ext_document'))
    CREATE NONCLUSTERED INDEX IDX_DOCUMENT_TYPE ON ext_document (f_tenant_id, f_type)
    INCLUDE (f_id, f_full_name, f_file_path);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DOCUMENT_SHARE' AND object_id = OBJECT_ID('ext_document'))
    CREATE NONCLUSTERED INDEX IDX_DOCUMENT_SHARE ON ext_document (f_tenant_id, f_is_share, f_share_time DESC)
    INCLUDE (f_id, f_full_name);
PRINT '--- ext_document done ---';

-- ext_employee (26 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EMPLOYEE_ENCODE' AND object_id = OBJECT_ID('ext_employee'))
    CREATE NONCLUSTERED INDEX IDX_EMPLOYEE_ENCODE ON ext_employee (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_department_name, f_position_name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EMPLOYEE_DEPT' AND object_id = OBJECT_ID('ext_employee'))
    CREATE NONCLUSTERED INDEX IDX_EMPLOYEE_DEPT ON ext_employee (f_tenant_id, f_department_name)
    INCLUDE (f_id, f_full_name, f_position_name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EMPLOYEE_IDNUMBER' AND object_id = OBJECT_ID('ext_employee'))
    CREATE NONCLUSTERED INDEX IDX_EMPLOYEE_IDNUMBER ON ext_employee (f_tenant_id, f_ID_number)
    INCLUDE (f_id, f_full_name);
PRINT '--- ext_employee done ---';

-- ext_work_log (17 cols; f_to_user_id is nvarchar(MAX) so cannot be indexed; skip IDX_WORKLOG_TOUSER)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WORKLOG_CREATOR' AND object_id = OBJECT_ID('ext_work_log'))
    CREATE NONCLUSTERED INDEX IDX_WORKLOG_CREATOR ON ext_work_log (f_tenant_id, f_creator_user_id, f_creator_time DESC)
    INCLUDE (f_id, f_title);
PRINT '--- ext_work_log done ---';

-- ext_product_classify (12 cols, hierarchical)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PRODUCTCLASS_PARENT' AND object_id = OBJECT_ID('ext_product_classify'))
    CREATE NONCLUSTERED INDEX IDX_PRODUCTCLASS_PARENT ON ext_product_classify (f_tenant_id, f_parent_id)
    INCLUDE (f_id, f_full_name, f_sort_code);
PRINT '--- ext_product_classify done ---';

-- ext_email_send (22 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EMAILSEND_CREATOR' AND object_id = OBJECT_ID('ext_email_send'))
    CREATE NONCLUSTERED INDEX IDX_EMAILSEND_CREATOR ON ext_email_send (f_tenant_id, f_creator_user_id, f_creator_time DESC)
    INCLUDE (f_id, f_subject, f_state);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EMAILSEND_STATE' AND object_id = OBJECT_ID('ext_email_send'))
    CREATE NONCLUSTERED INDEX IDX_EMAILSEND_STATE ON ext_email_send (f_tenant_id, f_state)
    INCLUDE (f_id, f_subject, f_creator_time);
PRINT '--- ext_email_send done ---';

-- ext_project_gantt (24 cols — f_manager_ids is nvarchar(MAX); index by f_type as proxy for grouping)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_GANTT_PROJECT' AND object_id = OBJECT_ID('ext_project_gantt'))
    CREATE NONCLUSTERED INDEX IDX_GANTT_PROJECT ON ext_project_gantt (f_tenant_id, f_project_id)
    INCLUDE (f_id, f_full_name, f_start_time, f_end_time);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_GANTT_ASSIGNEE' AND object_id = OBJECT_ID('ext_project_gantt'))
    CREATE NONCLUSTERED INDEX IDX_GANTT_ASSIGNEE ON ext_project_gantt (f_tenant_id, f_type)
    INCLUDE (f_id, f_project_id, f_schedule);
PRINT '--- ext_project_gantt done ---';

PRINT '=== Batch 12 ADD INDEX COMPLETE ===';
COMMIT TRANSACTION;
GO
