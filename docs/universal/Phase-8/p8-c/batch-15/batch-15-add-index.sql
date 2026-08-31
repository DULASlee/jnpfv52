-- P8-C Batch 15 — ADD INDEX DDL (inteAssistant-SA-output remaining)
-- Generated: 2026-08-30
-- Scope: 4 tables, 9 indexes
-- Note: Most sa_* tables already have IDX_* from prior Pilot work; this batch fills gaps

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 15 ADD INDEX START ===';

-- sa_assumptions (12 cols, no prior indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SAASSUMPTIONS_TRIPLEKEY' AND object_id = OBJECT_ID('sa_assumptions'))
    CREATE NONCLUSTERED INDEX IDX_SAASSUMPTIONS_TRIPLEKEY ON sa_assumptions (F_TenantId, F_ProjectId, F_PIPELINE_ID)
    INCLUDE (F_Id, F_AssumptionText, F_Confidence);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SAASSUMPTIONS_EVENT' AND object_id = OBJECT_ID('sa_assumptions'))
    CREATE NONCLUSTERED INDEX IDX_SAASSUMPTIONS_EVENT ON sa_assumptions (F_EventId)
    INCLUDE (F_Id, F_AssumptionText);
PRINT '--- sa_assumptions done ---';

-- sa_consistency (11 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SACONSISTENCY_TRIPLEKEY' AND object_id = OBJECT_ID('sa_consistency'))
    CREATE NONCLUSTERED INDEX IDX_SACONSISTENCY_TRIPLEKEY ON sa_consistency (F_TenantId, F_ProjectId, F_PIPELINE_ID)
    INCLUDE (F_Id, F_CheckType, F_Severity);
PRINT '--- sa_consistency done ---';

-- sa_quality_score (12 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SAQUALITY_TRIPLEKEY' AND object_id = OBJECT_ID('sa_quality_score'))
    CREATE NONCLUSTERED INDEX IDX_SAQUALITY_TRIPLEKEY ON sa_quality_score (F_TenantId, F_ProjectId, F_PIPELINE_ID)
    INCLUDE (F_Id, F_RoundNumber, F_TotalScore);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SAQUALITY_ROUND' AND object_id = OBJECT_ID('sa_quality_score'))
    CREATE NONCLUSTERED INDEX IDX_SAQUALITY_ROUND ON sa_quality_score (F_RoundNumber DESC)
    INCLUDE (F_Id, F_TotalScore);
PRINT '--- sa_quality_score done ---';

-- sa_entity_fields is a VIEW (not schema-bound) over ai_entity_field; cannot create index on view.
-- Equivalent indexes already exist on ai_entity_field from Batch 09:
--   IDX_ENTITYFIELD_TENANT_PROJECT (F_TenantId, F_ProjectId, F_PIPELINE_ID)
--   IDX_ENTITYFIELD_TABLE (F_TableName, F_SchemaVersion)
-- sa_entity_fields query patterns are covered by ai_entity_field indexes.
PRINT '--- sa_entity_fields skipped (VIEW; covered by ai_entity_field indexes) ---';

PRINT '=== Batch 15 ADD INDEX COMPLETE ===';
COMMIT TRANSACTION;
GO
