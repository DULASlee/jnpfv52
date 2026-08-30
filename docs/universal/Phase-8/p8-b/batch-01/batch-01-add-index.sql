-- P8-B Batch 01 — ADD INDEX DDL
-- Generated: 2026-08-30
-- Mode: Controlled Production (DB writes allowed)
-- Scope: 4 tables, 10 indexes (all additive)

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 01 ADD INDEX START ===';

-- ============================================================
-- Table 01: BASE_ORGANIZE (3 indexes)
-- ============================================================
PRINT '--- BASE_ORGANIZE ---';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ORGANIZE_PARENT' AND object_id = OBJECT_ID('BASE_ORGANIZE'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_ORGANIZE_PARENT
    ON BASE_ORGANIZE (f_tenant_id, f_parent_id)
    INCLUDE (f_id, f_full_name, f_en_code, f_enabled_mark);
    PRINT 'Created: IDX_ORGANIZE_PARENT';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ORGANIZE_ENCODE' AND object_id = OBJECT_ID('BASE_ORGANIZE'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_ORGANIZE_ENCODE
    ON BASE_ORGANIZE (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_parent_id, f_enabled_mark);
    PRINT 'Created: IDX_ORGANIZE_ENCODE';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ORGANIZE_CATEGORY' AND object_id = OBJECT_ID('BASE_ORGANIZE'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_ORGANIZE_CATEGORY
    ON BASE_ORGANIZE (f_tenant_id, f_category)
    INCLUDE (f_id, f_full_name);
    PRINT 'Created: IDX_ORGANIZE_CATEGORY';
END

-- ============================================================
-- Table 02: BASE_ROLE (2 indexes)
-- ============================================================
PRINT '--- BASE_ROLE ---';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ROLE_ENCODE' AND object_id = OBJECT_ID('BASE_ROLE'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_ROLE_ENCODE
    ON BASE_ROLE (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_type, f_enabled_mark);
    PRINT 'Created: IDX_ROLE_ENCODE';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ROLE_TYPE' AND object_id = OBJECT_ID('BASE_ROLE'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_ROLE_TYPE
    ON BASE_ROLE (f_tenant_id, f_type)
    INCLUDE (f_id, f_full_name, f_en_code);
    PRINT 'Created: IDX_ROLE_TYPE';
END

-- ============================================================
-- Table 03: BASE_POSITION (2 indexes)
-- ============================================================
PRINT '--- BASE_POSITION ---';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_POSITION_ORG' AND object_id = OBJECT_ID('BASE_POSITION'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_POSITION_ORG
    ON BASE_POSITION (f_tenant_id, f_organize_id)
    INCLUDE (f_id, f_full_name, f_en_code, f_type);
    PRINT 'Created: IDX_POSITION_ORG';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_POSITION_ENCODE' AND object_id = OBJECT_ID('BASE_POSITION'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_POSITION_ENCODE
    ON BASE_POSITION (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_organize_id, f_type);
    PRINT 'Created: IDX_POSITION_ENCODE';
END

-- ============================================================
-- Table 04: BASE_USER_RELATION (3 indexes)
-- ============================================================
PRINT '--- BASE_USER_RELATION ---';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_USERRELATION_USER' AND object_id = OBJECT_ID('BASE_USER_RELATION'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_USERRELATION_USER
    ON BASE_USER_RELATION (f_tenant_id, f_user_id)
    INCLUDE (f_id, f_object_type, f_object_id, f_enabled_mark);
    PRINT 'Created: IDX_USERRELATION_USER';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_USERRELATION_OBJECT' AND object_id = OBJECT_ID('BASE_USER_RELATION'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_USERRELATION_OBJECT
    ON BASE_USER_RELATION (f_tenant_id, f_object_type, f_object_id)
    INCLUDE (f_id, f_user_id, f_enabled_mark);
    PRINT 'Created: IDX_USERRELATION_OBJECT';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_USERRELATION_USER_OBJECT' AND object_id = OBJECT_ID('BASE_USER_RELATION'))
BEGIN
    CREATE NONCLUSTERED INDEX IDX_USERRELATION_USER_OBJECT
    ON BASE_USER_RELATION (f_tenant_id, f_user_id, f_object_type)
    INCLUDE (f_id, f_object_id, f_enabled_mark);
    PRINT 'Created: IDX_USERRELATION_USER_OBJECT';
END

PRINT '=== Batch 01 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
