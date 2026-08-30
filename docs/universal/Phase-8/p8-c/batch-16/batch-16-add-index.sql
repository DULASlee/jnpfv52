-- P8-C Batch 16 — ADD INDEX DDL (inteAssistant-KG + remaining)
-- Generated: 2026-08-30
-- Scope: 3 tables, 6 indexes
-- Note: BASE_KNOWLEDGE_EDGE already has Pilot 2 indexes

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 16 ADD INDEX START ===';

-- BASE_KNOWLEDGE_RULE (16 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_KNOWLEDGE_RULE_TENANT' AND object_id = OBJECT_ID('BASE_KNOWLEDGE_RULE'))
    CREATE NONCLUSTERED INDEX IDX_KNOWLEDGE_RULE_TENANT ON BASE_KNOWLEDGE_RULE (F_TenantId, F_Type)
    INCLUDE (F_Id, F_Name, F_Enabled);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_KNOWLEDGE_RULE_ENTITY' AND object_id = OBJECT_ID('BASE_KNOWLEDGE_RULE'))
    CREATE NONCLUSTERED INDEX IDX_KNOWLEDGE_RULE_ENTITY ON BASE_KNOWLEDGE_RULE (F_Entity)
    INCLUDE (F_Id, F_Name, F_Type);
PRINT '--- BASE_KNOWLEDGE_RULE done ---';

-- kg_pattern (18 cols, no tenant, lowercase id)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_KGPATTERN_TYPE' AND object_id = OBJECT_ID('kg_pattern'))
    CREATE NONCLUSTERED INDEX IDX_KGPATTERN_TYPE ON kg_pattern (pattern_type, industry)
    INCLUDE (id, score, is_active);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_KGPATTERN_ACTIVE' AND object_id = OBJECT_ID('kg_pattern'))
    CREATE NONCLUSTERED INDEX IDX_KGPATTERN_ACTIVE ON kg_pattern (is_active, is_locked)
    INCLUDE (id, pattern_type, score);
PRINT '--- kg_pattern done ---';

-- kg_pattern_usage (6 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_KGPATTERNUSAGE_PATTERN' AND object_id = OBJECT_ID('kg_pattern_usage'))
    CREATE NONCLUSTERED INDEX IDX_KGPATTERNUSAGE_PATTERN ON kg_pattern_usage (pattern_id)
    INCLUDE (target_type, target_id, used_at);
PRINT '--- kg_pattern_usage done ---';

PRINT '=== Batch 16 ADD INDEX COMPLETE ===';
COMMIT TRANSACTION;
GO
