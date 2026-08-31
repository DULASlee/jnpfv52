-- P8-C Batch 09 — ADD INDEX DDL (inteAssistant-AI)
-- Generated: 2026-08-30
-- Phase: 8 — P8-C Autonomous Batch
-- Scope: 6 tables, 13 indexes
-- NOTE: mixed case column naming (F_, F_, ai_*)

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 09 ADD INDEX START ===';

-- BASE_AI_PIPELINE (Pilot 1 done, but more indexes for runtime queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PIPELINE_PROJECT' AND object_id = OBJECT_ID('BASE_AI_PIPELINE'))
    CREATE NONCLUSTERED INDEX IDX_PIPELINE_PROJECT ON BASE_AI_PIPELINE (F_TENANT_ID, F_PROJECT_ID)
    INCLUDE (F_ID, F_NAME, F_STATUS, F_CURRENT_STAGE, F_STARTED_TIME);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PIPELINE_STATUS' AND object_id = OBJECT_ID('BASE_AI_PIPELINE'))
    CREATE NONCLUSTERED INDEX IDX_PIPELINE_STATUS ON BASE_AI_PIPELINE (F_TENANT_ID, F_STATUS)
    INCLUDE (F_ID, F_NAME, F_PROJECT_ID);
PRINT '--- BASE_AI_PIPELINE done ---';

-- BASE_AI_AGENT_CONFIG
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_AGENT_CODE' AND object_id = OBJECT_ID('BASE_AI_AGENT_CONFIG'))
    CREATE NONCLUSTERED INDEX IDX_AGENT_CODE ON BASE_AI_AGENT_CONFIG (F_AgentCode)
    INCLUDE (F_Id, F_Name, F_AgentType);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_AGENT_TYPE' AND object_id = OBJECT_ID('BASE_AI_AGENT_CONFIG'))
    CREATE NONCLUSTERED INDEX IDX_AGENT_TYPE ON BASE_AI_AGENT_CONFIG (F_AgentType)
    INCLUDE (F_Id, F_AgentCode, F_Name);
PRINT '--- BASE_AI_AGENT_CONFIG done ---';

-- ai_ir_events (event sourcing)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_IREVENTS_PROJECT' AND object_id = OBJECT_ID('ai_ir_events'))
    CREATE NONCLUSTERED INDEX IDX_IREVENTS_PROJECT ON ai_ir_events (F_TenantId, F_ProjectId, F_PIPELINE_ID)
    INCLUDE (F_Id, F_EventType, F_Sequence, F_CreatedAt);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_IREVENTS_TYPE' AND object_id = OBJECT_ID('ai_ir_events'))
    CREATE NONCLUSTERED INDEX IDX_IREVENTS_TYPE ON ai_ir_events (F_EventType, F_CreatedAt)
    INCLUDE (F_Id, F_ProjectId, F_FragmentId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_IREVENTS_FRAGMENT' AND object_id = OBJECT_ID('ai_ir_events'))
    CREATE NONCLUSTERED INDEX IDX_IREVENTS_FRAGMENT ON ai_ir_events (F_FragmentId, F_FragmentVersion)
    INCLUDE (F_Id, F_EventType);
PRINT '--- ai_ir_events done ---';

-- ai_entity_field (projection, IR projection)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ENTITYFIELD_TENANT_PROJECT' AND object_id = OBJECT_ID('ai_entity_field'))
    CREATE NONCLUSTERED INDEX IDX_ENTITYFIELD_TENANT_PROJECT ON ai_entity_field (F_TenantId, F_ProjectId, F_PIPELINE_ID)
    INCLUDE (F_Id, F_EntityName, F_TableName);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ENTITYFIELD_TABLE' AND object_id = OBJECT_ID('ai_entity_field'))
    CREATE NONCLUSTERED INDEX IDX_ENTITYFIELD_TABLE ON ai_entity_field (F_TableName, F_SchemaVersion)
    INCLUDE (F_Id, F_EntityName);
PRINT '--- ai_entity_field done ---';

-- BASE_AI_SKILL_REVIEW (PascalCase F_TenantId/F_ProjectId per actual schema)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SKILLREVIEW_PROJECT' AND object_id = OBJECT_ID('BASE_AI_SKILL_REVIEW'))
    CREATE NONCLUSTERED INDEX IDX_SKILLREVIEW_PROJECT ON BASE_AI_SKILL_REVIEW (F_TenantId, F_ProjectId)
    INCLUDE (F_Id, F_SkillId, F_SkillRunId, F_Verdict, F_Score);
PRINT '--- BASE_AI_SKILL_REVIEW done ---';

-- BASE_AI_EVAL_RUN (PascalCase F_TenantId/F_ProjectId; uses F_RunAt not F_RUN_TIME; F_Status not F_RESULT)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EVALRUN_PROJECT' AND object_id = OBJECT_ID('BASE_AI_EVAL_RUN'))
    CREATE NONCLUSTERED INDEX IDX_EVALRUN_PROJECT ON BASE_AI_EVAL_RUN (F_TenantId, F_ProjectId)
    INCLUDE (F_Id, F_PipelineId, F_Status, F_RunAt);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EVALRUN_TIME' AND object_id = OBJECT_ID('BASE_AI_EVAL_RUN'))
    CREATE NONCLUSTERED INDEX IDX_EVALRUN_TIME ON BASE_AI_EVAL_RUN (F_RunAt)
    INCLUDE (F_Id, F_ProjectId, F_Status);
PRINT '--- BASE_AI_EVAL_RUN done ---';

PRINT '=== Batch 09 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
