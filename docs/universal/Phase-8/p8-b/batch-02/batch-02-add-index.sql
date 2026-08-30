-- P8-B Batch 02 — ADD INDEX DDL
-- Generated: 2026-08-30
-- Mode: Controlled Production (DB writes allowed)
-- Scope: 5 tables, 12 indexes (all additive)

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 02 ADD INDEX START ===';

-- ============================================================
-- Table 01: base_authorize (3 indexes)
-- ============================================================
PRINT '--- base_authorize ---';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_AUTHORIZE_OBJECT' AND object_id = OBJECT_ID('base_authorize'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_AUTHORIZE_OBJECT
    ON base_authorize (f_tenant_id, f_object_type, f_object_id)
    INCLUDE (f_id, f_item_type, f_item_id);
    PRINT 'Created: IDX_AUTHORIZE_OBJECT';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_AUTHORIZE_ITEM' AND object_id = OBJECT_ID('base_authorize'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_AUTHORIZE_ITEM
    ON base_authorize (f_tenant_id, f_item_type, f_item_id)
    INCLUDE (f_id, f_object_type, f_object_id);
    PRINT 'Created: IDX_AUTHORIZE_ITEM';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_AUTHORIZE_OBJECT_ITEM' AND object_id = OBJECT_ID('base_authorize'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_AUTHORIZE_OBJECT_ITEM
    ON base_authorize (f_tenant_id, f_object_type, f_object_id, f_item_type, f_item_id)
    INCLUDE (f_id);
    PRINT 'Created: IDX_AUTHORIZE_OBJECT_ITEM';
END

-- ============================================================
-- Table 02: base_module (3 indexes)
-- ============================================================
PRINT '--- base_module ---';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MODULE_PARENT' AND object_id = OBJECT_ID('base_module'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_MODULE_PARENT
    ON base_module (f_tenant_id, f_parent_id)
    INCLUDE (f_id, f_full_name, f_type, f_enabled_mark);
    PRINT 'Created: IDX_MODULE_PARENT';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MODULE_TYPE' AND object_id = OBJECT_ID('base_module'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_MODULE_TYPE
    ON base_module (f_tenant_id, f_type)
    INCLUDE (f_id, f_full_name, f_parent_id);
    PRINT 'Created: IDX_MODULE_TYPE';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MODULE_CATEGORY' AND object_id = OBJECT_ID('base_module'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_MODULE_CATEGORY
    ON base_module (f_tenant_id, f_category)
    INCLUDE (f_id, f_full_name, f_type);
    PRINT 'Created: IDX_MODULE_CATEGORY';
END

-- ============================================================
-- Table 03: base_module_button (2 indexes)
-- ============================================================
PRINT '--- base_module_button ---';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_BUTTON_MODULE' AND object_id = OBJECT_ID('base_module_button'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_BUTTON_MODULE
    ON base_module_button (f_tenant_id, f_module_id)
    INCLUDE (f_id, f_full_name, f_en_code);
    PRINT 'Created: IDX_BUTTON_MODULE';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_BUTTON_PARENT' AND object_id = OBJECT_ID('base_module_button'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_BUTTON_PARENT
    ON base_module_button (f_tenant_id, f_parent_id)
    INCLUDE (f_id, f_full_name, f_module_id);
    PRINT 'Created: IDX_BUTTON_PARENT';
END

-- ============================================================
-- Table 04: base_module_column (2 indexes)
-- ============================================================
PRINT '--- base_module_column ---';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_COLUMN_MODULE' AND object_id = OBJECT_ID('base_module_column'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_COLUMN_MODULE
    ON base_module_column (f_tenant_id, f_module_id)
    INCLUDE (f_id, f_full_name, f_en_code, f_bind_table);
    PRINT 'Created: IDX_COLUMN_MODULE';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_COLUMN_BINDTABLE' AND object_id = OBJECT_ID('base_module_column'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_COLUMN_BINDTABLE
    ON base_module_column (f_tenant_id, f_bind_table)
    INCLUDE (f_id, f_full_name, f_module_id);
    PRINT 'Created: IDX_COLUMN_BINDTABLE';
END

-- ============================================================
-- Table 05: base_module_form (2 indexes)
-- ============================================================
PRINT '--- base_module_form ---';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_FORM_MODULE' AND object_id = OBJECT_ID('base_module_form'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_FORM_MODULE
    ON base_module_form (f_tenant_id, f_module_id)
    INCLUDE (f_id, f_full_name, f_en_code, f_bind_table);
    PRINT 'Created: IDX_FORM_MODULE';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_FORM_BINDTABLE' AND object_id = OBJECT_ID('base_module_form'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_FORM_BINDTABLE
    ON base_module_form (f_tenant_id, f_bind_table)
    INCLUDE (f_id, f_full_name, f_module_id);
    PRINT 'Created: IDX_FORM_BINDTABLE';
END

PRINT '=== Batch 02 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
