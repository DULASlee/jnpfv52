-- P8-B Batch 05 — ADD INDEX DDL (行政区划与数据接口)
-- Generated: 2026-08-30
-- Mode: Controlled Production
-- Scope: 5 tables, 11 indexes

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 05 ADD INDEX START ===';

-- base_province (3 indexes - HIGH VOLUME 47512 rows, includes quick_query for full-text-style search)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PROVINCE_PARENT' AND object_id = OBJECT_ID('base_province'))
    CREATE NONCLUSTERED INDEX IDX_PROVINCE_PARENT ON base_province (f_parent_id)
    INCLUDE (f_id, f_full_name, f_en_code, f_type);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PROVINCE_ENCODE' AND object_id = OBJECT_ID('base_province'))
    CREATE NONCLUSTERED INDEX IDX_PROVINCE_ENCODE ON base_province (f_en_code)
    INCLUDE (f_id, f_full_name, f_parent_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PROVINCE_QUICKQUERY' AND object_id = OBJECT_ID('base_province'))
    CREATE NONCLUSTERED INDEX IDX_PROVINCE_QUICKQUERY ON base_province (f_quick_query)
    INCLUDE (f_id, f_full_name, f_en_code, f_type);
PRINT '--- base_province done ---';

-- base_province_atlas (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PROVATLAS_PARENT' AND object_id = OBJECT_ID('base_province_atlas'))
    CREATE NONCLUSTERED INDEX IDX_PROVATLAS_PARENT ON base_province_atlas (f_parent_id)
    INCLUDE (f_id, f_full_name, f_en_code, f_division_code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PROVATLAS_DIVCODE' AND object_id = OBJECT_ID('base_province_atlas'))
    CREATE NONCLUSTERED INDEX IDX_PROVATLAS_DIVCODE ON base_province_atlas (f_division_code)
    INCLUDE (f_id, f_full_name, f_parent_id);
PRINT '--- base_province_atlas done ---';

-- base_data_interface (3 indexes - JSON-heavy, focus on category/type/en_code)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DATAINTERFACE_CATEGORY' AND object_id = OBJECT_ID('base_data_interface'))
    CREATE NONCLUSTERED INDEX IDX_DATAINTERFACE_CATEGORY ON base_data_interface (f_tenant_id, f_category)
    INCLUDE (f_id, f_full_name, f_en_code, f_type);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DATAINTERFACE_TYPE' AND object_id = OBJECT_ID('base_data_interface'))
    CREATE NONCLUSTERED INDEX IDX_DATAINTERFACE_TYPE ON base_data_interface (f_tenant_id, f_type)
    INCLUDE (f_id, f_full_name, f_en_code, f_category);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DATAINTERFACE_ENCODE' AND object_id = OBJECT_ID('base_data_interface'))
    CREATE NONCLUSTERED INDEX IDX_DATAINTERFACE_ENCODE ON base_data_interface (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_category, f_type);
PRINT '--- base_data_interface done ---';

-- base_data_interface_log (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_INTERFACELOG_INVOK' AND object_id = OBJECT_ID('base_data_interface_log'))
    CREATE NONCLUSTERED INDEX IDX_INTERFACELOG_INVOK ON base_data_interface_log (f_tenant_id, f_invok_id)
    INCLUDE (f_id, f_invok_time, f_user_id, f_invok_waste_time);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_INTERFACELOG_USER' AND object_id = OBJECT_ID('base_data_interface_log'))
    CREATE NONCLUSTERED INDEX IDX_INTERFACELOG_USER ON base_data_interface_log (f_tenant_id, f_user_id)
    INCLUDE (f_id, f_invok_id, f_invok_time);
PRINT '--- base_data_interface_log done ---';

-- base_data_interface_oauth (1 index - f_sys_obj_id doesn't exist; use f_app_id)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_INTERFACEOAUTH_APPID' AND object_id = OBJECT_ID('base_data_interface_oauth'))
    CREATE NONCLUSTERED INDEX IDX_INTERFACEOAUTH_APPID ON base_data_interface_oauth (f_app_id)
    INCLUDE (f_id, f_app_name, f_useful_life);
PRINT '--- base_data_interface_oauth done ---';

PRINT '=== Batch 05 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
