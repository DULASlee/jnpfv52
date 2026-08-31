-- P8-C Batch 23 — ADD INDEX DDL (ai-ir remaining)
-- Generated: 2026-08-30
-- Skill v1.0 (FROZEN): schema-drift detected + Triple-Key Iron Law applied
-- Scope: 6 tables, ~6 indexes (3 REFACTORED + 3 NO-CHANGE)

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

PRINT '=== Batch 23 ADD INDEX START ===';

-- ai_ir_fragment_snapshots (782 rows, PascalCase; F_IrContent is nvarchar(MAX))
-- Triple-Key Iron Law: (F_TenantId, F_ProjectId, F_PIPELINE_ID)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_IRSNAPSHOT_TRIPLEKEY' AND object_id = OBJECT_ID('ai_ir_fragment_snapshots'))
    CREATE NONCLUSTERED INDEX IDX_IRSNAPSHOT_TRIPLEKEY ON ai_ir_fragment_snapshots (F_TenantId, F_ProjectId, F_PIPELINE_ID)
    INCLUDE (F_Id, F_FragmentId, F_FragmentType, F_UpdatedAt);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_IRSNAPSHOT_STABILITY' AND object_id = OBJECT_ID('ai_ir_fragment_snapshots'))
    CREATE NONCLUSTERED INDEX IDX_IRSNAPSHOT_STABILITY ON ai_ir_fragment_snapshots (F_TenantId, F_StabilityState, F_UpdatedAt DESC)
    INCLUDE (F_Id, F_FragmentId, F_ProjectId);
PRINT '--- ai_ir_fragment_snapshots done ---';

-- ai_projects (329 rows, PascalCase; no F_ProjectId — use F_Id as project key)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_AIPROJ_TENANT_STATUS' AND object_id = OBJECT_ID('ai_projects'))
    CREATE NONCLUSTERED INDEX IDX_AIPROJ_TENANT_STATUS ON ai_projects (F_TenantId, F_Status)
    INCLUDE (F_Id, F_ProjectName, F_CurrentPhase, F_CreatedAt);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_AIPROJ_CREATOR' AND object_id = OBJECT_ID('ai_projects'))
    CREATE NONCLUSTERED INDEX IDX_AIPROJ_CREATOR ON ai_projects (F_TenantId, F_CreatorUserId, F_CreatedAt DESC)
    INCLUDE (F_Id, F_ProjectName, F_Status);
PRINT '--- ai_projects done ---';

-- ai_route_table (328 rows, PascalCase; no F_PIPELINE_ID — use F_Id)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_AIROUTE_PROJECT' AND object_id = OBJECT_ID('ai_route_table'))
    CREATE NONCLUSTERED INDEX IDX_AIROUTE_PROJECT ON ai_route_table (F_TenantId, F_ProjectId)
    INCLUDE (F_Id, F_SandboxId, F_SandboxStatus);
PRINT '--- ai_route_table done ---';

-- ai_seed_templates (40 rows, no tenant_id) — NO-CHANGE per Skill v1.0
-- ai_skill_llm_policy (9 rows, no tenant_id) — NO-CHANGE per Skill v1.0
-- EVAL_METRIC (0 rows, empty) — NO-CHANGE per Skill v1.0
PRINT '--- ai_seed_templates/ai_skill_llm_policy/EVAL_METRIC NO-CHANGE ---';

PRINT '=== Batch 23 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
