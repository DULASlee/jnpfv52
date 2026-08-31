-- M32-Migration: Add PK to base_signature + composite PK to base_signature_user
-- READ-ONLY phase file — DO NOT EXECUTE without Phase 32 Acceptance Gate
-- Per Chief Architect 2026-08-31 decision (Batch 31 v2 decision matrix)

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRANSACTION;

BEGIN TRY
    -- ============================================================
    -- M32-01: Add PK on base_signature (single-column: f_id)
    -- Per Batch 31: empty table (0 rows) → safe migration
    -- Per Codebase analysis: CLDSEntityBase provides f_id as standard surrogate
    -- Per SqlSugar: PK on f_id is mandatory for Insertable/Updateable operations
    -- ============================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.base_signature') AND is_primary_key = 1
    )
    BEGIN
        -- Pre-check: confirm f_id is unique AND not null
        DECLARE @sig_dup_count INT = 0;
        DECLARE @sig_null_count INT = 0;
        SELECT @sig_dup_count = COUNT(*) - COUNT(DISTINCT f_id) FROM dbo.base_signature;
        SELECT @sig_null_count = COUNT(*) FROM dbo.base_signature WHERE f_id IS NULL;

        IF @sig_dup_count > 0
            THROW 50001, 'base_signature has duplicate f_id values; cannot add PK', 1;
        IF @sig_null_count > 0
            THROW 50002, 'base_signature has NULL f_id values; cannot add PK', 1;

        ALTER TABLE dbo.base_signature
            ADD CONSTRAINT PK_base_signature PRIMARY KEY CLUSTERED (f_id);

        PRINT 'PK_base_signature added successfully';
    END
    ELSE
        PRINT 'PK_base_signature already exists; skipping';

    -- ============================================================
    -- M32-02: Add composite PK on base_signature_user (f_signature_id, f_user_id)
    -- Per Chief Architect 2026-08-31 decision: composite (not surrogate f_id)
    -- Rationale: table is association table (Signature↔User), composite matches semantic
    -- Per SqlSugar: composite PK requires explicit Entity configuration (NOT f_id navigation)
    -- ============================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.base_signature_user') AND is_primary_key = 1
    )
    BEGIN
        -- Pre-check: confirm composite uniqueness AND no NULLs in either column
        DECLARE @siguser_dup_count INT = 0;
        DECLARE @siguser_null_sig INT = 0;
        DECLARE @siguser_null_user INT = 0;
        SELECT @siguser_dup_count = COUNT(*) - COUNT(DISTINCT (f_signature_id + '|' + f_user_id))
        FROM dbo.base_signature_user;
        SELECT @siguser_null_sig = COUNT(*) FROM dbo.base_signature_user WHERE f_signature_id IS NULL;
        SELECT @siguser_null_user = COUNT(*) FROM dbo.base_signature_user WHERE f_user_id IS NULL;

        IF @siguser_dup_count > 0
            THROW 50003, 'base_signature_user has duplicate (signature_id, user_id) pairs', 1;
        IF @siguser_null_sig > 0
            THROW 50004, 'base_signature_user has NULL f_signature_id', 1;
        IF @siguser_null_user > 0
            THROW 50005, 'base_signature_user has NULL f_user_id', 1;

        ALTER TABLE dbo.base_signature_user
            ADD CONSTRAINT PK_base_signature_user PRIMARY KEY CLUSTERED (f_signature_id, f_user_id);

        PRINT 'PK_base_signature_user (composite) added successfully';
    END
    ELSE
        PRINT 'PK_base_signature_user already exists; skipping';

    COMMIT TRANSACTION;
    PRINT '=== M32-Migration: ALL CHANGES COMMITTED ===';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @err_msg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @err_severity INT = ERROR_SEVERITY();
    DECLARE @err_state INT = ERROR_STATE();
    PRINT '=== M32-Migration: ROLLED BACK DUE TO ERROR ===';
    PRINT 'Error: ' + @err_msg;
    RAISERROR(@err_msg, @err_severity, @err_state);
END CATCH;
