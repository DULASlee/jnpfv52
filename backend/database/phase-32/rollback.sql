-- M32-Rollback: Remove PK from base_signature + base_signature_user
-- READ-ONLY phase file — DO NOT EXECUTE without explicit rollback authorization
-- Mirror of migration.sql but in reverse order

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRANSACTION;

BEGIN TRY
    -- ============================================================
    -- Rollback M32-02 first (composite PK on base_signature_user)
    -- ============================================================
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.base_signature_user')
          AND is_primary_key = 1
          AND name = 'PK_base_signature_user'
    )
    BEGIN
        ALTER TABLE dbo.base_signature_user
            DROP CONSTRAINT PK_base_signature_user;
        PRINT 'PK_base_signature_user dropped';
    END
    ELSE
        PRINT 'PK_base_signature_user not present; nothing to drop';

    -- ============================================================
    -- Rollback M32-01 (PK on base_signature)
    -- ============================================================
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.base_signature')
          AND is_primary_key = 1
          AND name = 'PK_base_signature'
    )
    BEGIN
        ALTER TABLE dbo.base_signature
            DROP CONSTRAINT PK_base_signature;
        PRINT 'PK_base_signature dropped';
    END
    ELSE
        PRINT 'PK_base_signature not present; nothing to drop';

    COMMIT TRANSACTION;
    PRINT '=== M32-Rollback: ALL REVERSALS COMMITTED ===';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @err_msg NVARCHAR(4000) = ERROR_MESSAGE();
    PRINT '=== M32-Rollback: ROLLED BACK DUE TO ERROR ===';
    PRINT 'Error: ' + @err_msg;
    RAISERROR(@err_msg, 16, 1);
END CATCH;
