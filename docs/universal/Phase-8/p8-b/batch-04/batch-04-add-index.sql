-- P8-B Batch 04 — ADD INDEX DDL (system-core config)
-- Generated: 2026-08-30
-- Mode: Controlled Production
-- Scope: 5 tables, 11 indexes

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 04 ADD INDEX START ===';

-- base_sys_config (2 indexes - executes P8-A deferred recommendation)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SYSCONFIG_KEY' AND object_id = OBJECT_ID('base_sys_config'))
    CREATE NONCLUSTERED INDEX IDX_SYSCONFIG_KEY ON base_sys_config (f_tenant_id, f_key)
    INCLUDE (f_id, f_full_name, f_value, f_enabled_mark);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SYSCONFIG_CATEGORY' AND object_id = OBJECT_ID('base_sys_config'))
    CREATE NONCLUSTERED INDEX IDX_SYSCONFIG_CATEGORY ON base_sys_config (f_tenant_id, f_category)
    INCLUDE (f_id, f_full_name, f_key, f_value);
PRINT '--- base_sys_config done ---';

-- base_sys_log (3 indexes - high volume 12615 rows)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SYSLOG_USER' AND object_id = OBJECT_ID('base_sys_log'))
    CREATE NONCLUSTERED INDEX IDX_SYSLOG_USER ON base_sys_log (f_tenant_id, f_user_id, f_creator_time DESC)
    INCLUDE (f_id, f_type, f_level, f_ip_address, f_description);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SYSLOG_TYPE' AND object_id = OBJECT_ID('base_sys_log'))
    CREATE NONCLUSTERED INDEX IDX_SYSLOG_TYPE ON base_sys_log (f_tenant_id, f_type, f_creator_time DESC)
    INCLUDE (f_id, f_user_id, f_level);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SYSLOG_MODULE' AND object_id = OBJECT_ID('base_sys_log'))
    CREATE NONCLUSTERED INDEX IDX_SYSLOG_MODULE ON base_sys_log (f_tenant_id, f_module_id, f_creator_time DESC)
    INCLUDE (f_id, f_user_id, f_type);
PRINT '--- base_sys_log done ---';

-- base_api_log (3 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_APILOG_USER' AND object_id = OBJECT_ID('base_api_log'))
    CREATE NONCLUSTERED INDEX IDX_APILOG_USER ON base_api_log (f_tenant_id, f_user_id, f_creator_time DESC)
    INCLUDE (f_id, f_type, f_level, f_request_url, f_request_duration);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_APILOG_TYPE' AND object_id = OBJECT_ID('base_api_log'))
    CREATE NONCLUSTERED INDEX IDX_APILOG_TYPE ON base_api_log (f_tenant_id, f_type, f_creator_time DESC)
    INCLUDE (f_id, f_user_id, f_request_url);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_APILOG_MODULE' AND object_id = OBJECT_ID('base_api_log'))
    CREATE NONCLUSTERED INDEX IDX_APILOG_MODULE ON base_api_log (f_tenant_id, f_module_id, f_creator_time DESC)
    INCLUDE (f_id, f_user_id, f_type);
PRINT '--- base_api_log done ---';

-- base_sign_img (1 index - low volume, but pattern consistency)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SIGNIMG_DEFAULT' AND object_id = OBJECT_ID('base_sign_img'))
    CREATE NONCLUSTERED INDEX IDX_SIGNIMG_DEFAULT ON base_sign_img (f_tenant_id, f_is_default)
    INCLUDE (f_id, f_sign_img, f_description);
PRINT '--- base_sign_img done ---';

-- base_syn_third_info (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SYNTHIRD_TYPE' AND object_id = OBJECT_ID('base_syn_third_info'))
    CREATE NONCLUSTERED INDEX IDX_SYNTHIRD_TYPE ON base_syn_third_info (f_tenant_id, f_third_type)
    INCLUDE (f_id, f_sys_obj_id, f_third_obj_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SYNTHIRD_SYSOBJ' AND object_id = OBJECT_ID('base_syn_third_info'))
    CREATE NONCLUSTERED INDEX IDX_SYNTHIRD_SYSOBJ ON base_syn_third_info (f_tenant_id, f_sys_obj_id)
    INCLUDE (f_id, f_third_type, f_third_obj_id);
PRINT '--- base_syn_third_info done ---';

PRINT '=== Batch 04 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
