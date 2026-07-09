-- ════════════════════════════════════════════════════════════════
-- P9 Domain 1 — Entity field read model
--
-- Purpose:
--   ai_ir_events / IR fragments remain the immutable Write Model.
--   ai_entity_field is the deterministic CQRS Read Model consumed by
--   DbDesign, Developer/codegen, and later SA materialization.
--
-- R12:
--   Every row is isolated by F_TenantId + F_ProjectId + F_PIPELINE_ID.
-- ════════════════════════════════════════════════════════════════

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ai_entity_field')
BEGIN
    CREATE TABLE ai_entity_field (
        F_Id                   NVARCHAR(50)   NOT NULL CONSTRAINT PK_ai_entity_field PRIMARY KEY,
        F_TenantId             NVARCHAR(50)   NOT NULL CONSTRAINT DF_ai_entity_field_Tenant DEFAULT '',
        F_ProjectId            NVARCHAR(50)   NOT NULL CONSTRAINT DF_ai_entity_field_Project DEFAULT '',
        F_PIPELINE_ID          NVARCHAR(50)   NOT NULL CONSTRAINT DF_ai_entity_field_Pipeline DEFAULT '',
        F_SchemaVersion        NVARCHAR(50)   NOT NULL CONSTRAINT DF_ai_entity_field_Schema DEFAULT 'entity-field.v1',
        F_ProjectionHash       NVARCHAR(64)   NOT NULL CONSTRAINT DF_ai_entity_field_Hash DEFAULT '',
        F_SourceFragmentId     NVARCHAR(100)  NOT NULL CONSTRAINT DF_ai_entity_field_Source DEFAULT '',
        F_SourceDdlFragmentId  NVARCHAR(100)  NULL,

        F_EntityName           NVARCHAR(100)  NOT NULL,
        F_EntityDisplayName    NVARCHAR(200)  NULL,
        F_TableName            NVARCHAR(100)  NOT NULL,
        F_FieldName            NVARCHAR(100)  NOT NULL,
        F_PropertyName         NVARCHAR(100)  NOT NULL,
        F_DbColumnName         NVARCHAR(100)  NOT NULL,
        F_CSharpType           NVARCHAR(50)   NOT NULL CONSTRAINT DF_ai_entity_field_CSharpType DEFAULT 'string',
        F_SqlType              NVARCHAR(100)  NOT NULL CONSTRAINT DF_ai_entity_field_SqlType DEFAULT 'NVARCHAR(255)',
        F_IsRequired           BIT            NOT NULL CONSTRAINT DF_ai_entity_field_Required DEFAULT 0,
        F_IsPrimaryKey         BIT            NOT NULL CONSTRAINT DF_ai_entity_field_PK DEFAULT 0,
        F_IsNullable           BIT            NOT NULL CONSTRAINT DF_ai_entity_field_Nullable DEFAULT 1,
        F_IsIdentity           BIT            NOT NULL CONSTRAINT DF_ai_entity_field_Identity DEFAULT 0,
        F_References           NVARCHAR(200)  NULL,
        F_ReferencesTable      NVARCHAR(100)  NULL,
        F_ReferencesColumn     NVARCHAR(100)  NULL,

        F_CreatorTime          DATETIME       NOT NULL CONSTRAINT DF_ai_entity_field_CreateTime DEFAULT GETUTCDATE(),
        F_LastModifyTime       DATETIME       NULL,
        F_DeleteMark           BIT            NOT NULL CONSTRAINT DF_ai_entity_field_Delete DEFAULT 0
    );
    PRINT '[OK] ai_entity_field created';
END
ELSE
    PRINT '[SKIP] ai_entity_field exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ai_entity_field_triple_field')
BEGIN
    CREATE UNIQUE INDEX UX_ai_entity_field_triple_field
        ON ai_entity_field (F_TenantId, F_ProjectId, F_PIPELINE_ID, F_EntityName, F_FieldName)
        WHERE F_DeleteMark = 0;
    PRINT '[OK] UX_ai_entity_field_triple_field created';
END
ELSE
    PRINT '[SKIP] UX_ai_entity_field_triple_field exists';
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ai_entity_field_triple_table')
BEGIN
    CREATE INDEX IX_ai_entity_field_triple_table
        ON ai_entity_field (F_TenantId, F_ProjectId, F_PIPELINE_ID, F_TableName)
        INCLUDE (F_EntityName, F_FieldName, F_DbColumnName, F_SqlType, F_IsPrimaryKey);
    PRINT '[OK] IX_ai_entity_field_triple_table created';
END
ELSE
    PRINT '[SKIP] IX_ai_entity_field_triple_table exists';
GO
