-- P8-C Batch 14 — ADD INDEX DDL (system-warehouse-legacy WH_*)
-- Generated: 2026-08-30
-- Scope: 6 tables, 18 indexes (3 per table)
-- Note: WH_* tables have no tenant column, use uppercase column names

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 14 ADD INDEX START ===';

-- WH_Bill
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHBILL_CODE' AND object_id = OBJECT_ID('WH_Bill'))
    CREATE NONCLUSTERED INDEX IDX_WHBILL_CODE ON WH_Bill (BillCode)
    INCLUDE (ID, DepotID, CustomerID, SupplierID, CreateDate);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHBILL_DEPOT' AND object_id = OBJECT_ID('WH_Bill'))
    CREATE NONCLUSTERED INDEX IDX_WHBILL_DEPOT ON WH_Bill (DepotID)
    INCLUDE (ID, BillCode, CustomerID, SupplierID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHBILL_CUSTOMER' AND object_id = OBJECT_ID('WH_Bill'))
    CREATE NONCLUSTERED INDEX IDX_WHBILL_CUSTOMER ON WH_Bill (CustomerID)
    INCLUDE (ID, BillCode, CreateDate);
PRINT '--- WH_Bill done ---';

-- WH_BillDetail
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHBILLDETAIL_BILL' AND object_id = OBJECT_ID('WH_BillDetail'))
    CREATE NONCLUSTERED INDEX IDX_WHBILLDETAIL_BILL ON WH_BillDetail (BillId)
    INCLUDE (ID, MaterialId, Qty, Price);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHBILLDETAIL_MATERIAL' AND object_id = OBJECT_ID('WH_BillDetail'))
    CREATE NONCLUSTERED INDEX IDX_WHBILLDETAIL_MATERIAL ON WH_BillDetail (MaterialId)
    INCLUDE (ID, BillId, Qty);
PRINT '--- WH_BillDetail done ---';

-- WH_Customer
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHCUSTOMER_NAME' AND object_id = OBJECT_ID('WH_Customer'))
    CREATE NONCLUSTERED INDEX IDX_WHCUSTOMER_NAME ON WH_Customer (Name)
    INCLUDE (ID, ClassID, LinkMan, Telephone);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHCUSTOMER_CLASS' AND object_id = OBJECT_ID('WH_Customer'))
    CREATE NONCLUSTERED INDEX IDX_WHCUSTOMER_CLASS ON WH_Customer (ClassID)
    INCLUDE (ID, Name, LinkMan);
PRINT '--- WH_Customer done ---';

-- WH_Material
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHMATERIAL_CODE' AND object_id = OBJECT_ID('WH_Material'))
    CREATE NONCLUSTERED INDEX IDX_WHMATERIAL_CODE ON WH_Material (MaterialCode)
    INCLUDE (ID, MaterialName, ClassId, DepotID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHMATERIAL_NAME' AND object_id = OBJECT_ID('WH_Material'))
    CREATE NONCLUSTERED INDEX IDX_WHMATERIAL_NAME ON WH_Material (MaterialName)
    INCLUDE (ID, MaterialCode, Spec);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHMATERIAL_BARNO' AND object_id = OBJECT_ID('WH_Material'))
    CREATE NONCLUSTERED INDEX IDX_WHMATERIAL_BARNO ON WH_Material (BarNo)
    INCLUDE (ID, MaterialCode);
PRINT '--- WH_Material done ---';

-- WH_Supplier
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHSUPPLIER_NAME' AND object_id = OBJECT_ID('WH_Supplier'))
    CREATE NONCLUSTERED INDEX IDX_WHSUPPLIER_NAME ON WH_Supplier (Name)
    INCLUDE (ID, ClassID, Telephone);
PRINT '--- WH_Supplier done ---';

-- WH_Depot
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WHDEPOT_NAME' AND object_id = OBJECT_ID('WH_Depot'))
    CREATE NONCLUSTERED INDEX IDX_WHDEPOT_NAME ON WH_Depot (Name)
    INCLUDE (ID);
PRINT '--- WH_Depot done ---';

PRINT '=== Batch 14 ADD INDEX COMPLETE ===';
COMMIT TRANSACTION;
GO
