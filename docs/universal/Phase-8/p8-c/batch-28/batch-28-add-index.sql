-- P8-C Batch 28 — ADD INDEX DDL (final mixed batch)
-- Generated: 2026-08-30
-- Skill v1.0 (FROZEN): schema-drift detected + Triple-Key Iron Law applied
-- Scope: 6 tables, 2 REFACTORED + 4 NO-CHANGE

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

PRINT '=== Batch 28 ADD INDEX START ===';

-- inte_assistant_deliverable (269 rows; Triple-Key available)
-- Triple-Key Iron Law: (F_TenantId, F_ProjectId, F_PipelineId)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_INTEASSIST_TRIPLEKEY' AND object_id = OBJECT_ID('inte_assistant_deliverable'))
    CREATE NONCLUSTERED INDEX IDX_INTEASSIST_TRIPLEKEY ON inte_assistant_deliverable (F_TenantId, F_ProjectId, F_PipelineId)
    INCLUDE (F_Id, F_FileName, F_StageCode, F_FileSize, F_CreatorTime);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_INTEASSIST_FILE' AND object_id = OBJECT_ID('inte_assistant_deliverable'))
    CREATE NONCLUSTERED INDEX IDX_INTEASSIST_FILE ON inte_assistant_deliverable (F_TenantId, F_StageCode)
    INCLUDE (F_Id, F_FileName, F_ContentType, F_CreatorTime);
PRINT '--- inte_assistant_deliverable done ---';

-- report_user (283 rows; lowercase f_tenant_id, mixed column naming)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_REPORTUSER_TENANT' AND object_id = OBJECT_ID('report_user'))
    CREATE NONCLUSTERED INDEX IDX_REPORTUSER_TENANT ON report_user (f_tenant_id)
    INCLUDE (id, username, departmentnum, year, month);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_REPORTUSER_DEPT' AND object_id = OBJECT_ID('report_user'))
    CREATE NONCLUSTERED INDEX IDX_REPORTUSER_DEPT ON report_user (f_tenant_id, departmentnum, year)
    INCLUDE (id, username, salary);
PRINT '--- report_user done ---';

-- BASE_STUDIO_MENU (54 rows; PascalCase, nvarchar(MAX) F_TenantViewConfig)
-- REFACTORED: studio menu config is actively used; 54 rows is significant
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_STUDIOMENU_PARENT' AND object_id = OBJECT_ID('BASE_STUDIO_MENU'))
    CREATE NONCLUSTERED INDEX IDX_STUDIOMENU_PARENT ON BASE_STUDIO_MENU (F_ParentId, F_Sort)
    INCLUDE (F_Id, F_Name, F_Enabled, F_IsVisible);
PRINT '--- BASE_STUDIO_MENU done ---';

-- BASE_FOUNDER_AUTH_LOG (13 rows), data_report (15 rows), report_department (12 rows) — NO-CHANGE per Skill v1.0
PRINT '--- BASE_FOUNDER_AUTH_LOG/data_report/report_department NO-CHANGE ---';

PRINT '=== Batch 28 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
