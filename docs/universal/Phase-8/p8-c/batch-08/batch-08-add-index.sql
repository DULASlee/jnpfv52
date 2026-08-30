-- P8-C Batch 08 — ADD INDEX DDL (visualdata)
-- Generated: 2026-08-30
-- Phase: 8 — P8-C Autonomous Batch
-- Scope: 6 tables, ~13 indexes
-- NOTE: visualdata has inconsistent column naming (f_*, F_*, id, ID)

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 08 ADD INDEX START ===';

-- blade_visual (lowercase id, no f_ prefix)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_BLADEVISUAL_CATEGORY' AND object_id = OBJECT_ID('blade_visual'))
    CREATE NONCLUSTERED INDEX IDX_BLADEVISUAL_CATEGORY ON blade_visual (f_tenant_id, category)
    INCLUDE (id, title, status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_BLADEVISUAL_USER' AND object_id = OBJECT_ID('blade_visual'))
    CREATE NONCLUSTERED INDEX IDX_BLADEVISUAL_USER ON blade_visual (f_tenant_id, create_user)
    INCLUDE (id, title, create_time);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_BLADEVISUAL_STATUS' AND object_id = OBJECT_ID('blade_visual'))
    CREATE NONCLUSTERED INDEX IDX_BLADEVISUAL_STATUS ON blade_visual (f_tenant_id, status)
    INCLUDE (id, title);
PRINT '--- blade_visual done ---';

-- blade_visual_category
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_BLADEVISUALCAT_KEY' AND object_id = OBJECT_ID('blade_visual_category'))
    CREATE NONCLUSTERED INDEX IDX_BLADEVISUALCAT_KEY ON blade_visual_category (f_tenant_id, category_key)
    INCLUDE (id, category_value);
PRINT '--- blade_visual_category done ---';

-- BASE_REPORT (UPPERCASE F_ prefix)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_REPORT_ENCODE' AND object_id = OBJECT_ID('BASE_REPORT'))
    CREATE NONCLUSTERED INDEX IDX_REPORT_ENCODE ON BASE_REPORT (F_EN_CODE)
    INCLUDE (F_ID, F_FULL_NAME, F_CATEGORY);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_REPORT_CATEGORY' AND object_id = OBJECT_ID('BASE_REPORT'))
    CREATE NONCLUSTERED INDEX IDX_REPORT_CATEGORY ON BASE_REPORT (F_CATEGORY)
    INCLUDE (F_ID, F_FULL_NAME, F_EN_CODE);
PRINT '--- BASE_REPORT done ---';

-- report_charts (mixed case)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_REPORTCHARTS_QYBM' AND object_id = OBJECT_ID('report_charts'))
    CREATE NONCLUSTERED INDEX IDX_REPORTCHARTS_QYBM ON report_charts (f_tenant_id, QYBM)
    INCLUDE (ID, FXDMC, PGRQ);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_REPORTCHARTS_STATUS' AND object_id = OBJECT_ID('report_charts'))
    CREATE NONCLUSTERED INDEX IDX_REPORTCHARTS_STATUS ON report_charts (f_tenant_id, STATUS)
    INCLUDE (ID, QYBM, FXDMC);
PRINT '--- report_charts done ---';

PRINT '=== Batch 08 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
