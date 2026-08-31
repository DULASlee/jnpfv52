-- P8-C Batch 13 — ADD INDEX DDL (workflow-form-example wform_*)
-- Generated: 2026-08-30
-- Scope: 6 tables, ~18 indexes (3 indexes per table)
-- Pattern: dynamic-access workflow forms with F_FlowId, F_BillNo, F_ApplyUser

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 13 ADD INDEX START ===';

-- wform_applybanquet
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_BANQUET_FLOW' AND object_id = OBJECT_ID('wform_applybanquet'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_BANQUET_FLOW ON wform_applybanquet (f_tenant_id, F_FlowId)
    INCLUDE (F_Id, F_FlowTitle, F_BillNo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_BANQUET_BILLNO' AND object_id = OBJECT_ID('wform_applybanquet'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_BANQUET_BILLNO ON wform_applybanquet (f_tenant_id, F_BillNo)
    INCLUDE (F_Id, F_FlowTitle);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_BANQUET_USER' AND object_id = OBJECT_ID('wform_applybanquet'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_BANQUET_USER ON wform_applybanquet (f_tenant_id, F_ApplyUser)
    INCLUDE (F_Id, F_FlowTitle, F_ApplyDate);
PRINT '--- wform_applybanquet done ---';

-- wform_leaveapply
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_LEAVE_FLOW' AND object_id = OBJECT_ID('wform_leaveapply'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_LEAVE_FLOW ON wform_leaveapply (f_tenant_id, F_FlowId)
    INCLUDE (F_Id, F_FlowTitle, F_BillNo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_LEAVE_BILLNO' AND object_id = OBJECT_ID('wform_leaveapply'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_LEAVE_BILLNO ON wform_leaveapply (f_tenant_id, F_BillNo)
    INCLUDE (F_Id, F_FlowTitle, F_ApplyUser);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_LEAVE_USER' AND object_id = OBJECT_ID('wform_leaveapply'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_LEAVE_USER ON wform_leaveapply (f_tenant_id, F_ApplyUser)
    INCLUDE (F_Id, F_FlowTitle, F_LeaveStartTime, F_LeaveEndTime);
PRINT '--- wform_leaveapply done ---';

-- wform_contractapproval
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_CONTRACT_FLOW' AND object_id = OBJECT_ID('wform_contractapproval'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_CONTRACT_FLOW ON wform_contractapproval (f_tenant_id, F_FlowId)
    INCLUDE (F_Id, F_FlowTitle, F_BillNo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_CONTRACT_BILLNO' AND object_id = OBJECT_ID('wform_contractapproval'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_CONTRACT_BILLNO ON wform_contractapproval (f_tenant_id, F_BillNo)
    INCLUDE (F_Id, F_FlowTitle);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_CONTRACT_USER' AND object_id = OBJECT_ID('wform_contractapproval'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_CONTRACT_USER ON wform_contractapproval (f_tenant_id, F_InputPerson)
    INCLUDE (F_Id, F_FlowTitle, F_SigningDate);
PRINT '--- wform_contractapproval done ---';

-- wform_salesorder
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_SALESORDER_FLOW' AND object_id = OBJECT_ID('wform_salesorder'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_SALESORDER_FLOW ON wform_salesorder (f_tenant_id, F_FlowId)
    INCLUDE (F_Id, F_FlowTitle, F_BillNo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_SALESORDER_BILLNO' AND object_id = OBJECT_ID('wform_salesorder'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_SALESORDER_BILLNO ON wform_salesorder (f_tenant_id, F_BillNo)
    INCLUDE (F_Id, F_FlowTitle);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_SALESORDER_USER' AND object_id = OBJECT_ID('wform_salesorder'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_SALESORDER_USER ON wform_salesorder (f_tenant_id, F_Salesman)
    INCLUDE (F_Id, F_FlowTitle, F_SalesDate);
PRINT '--- wform_salesorder done ---';

-- wform_purchaselist
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_PURCHASE_FLOW' AND object_id = OBJECT_ID('wform_purchaselist'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_PURCHASE_FLOW ON wform_purchaselist (f_tenant_id, F_FlowId)
    INCLUDE (F_Id, F_FlowTitle, F_BillNo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_PURCHASE_BILLNO' AND object_id = OBJECT_ID('wform_purchaselist'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_PURCHASE_BILLNO ON wform_purchaselist (f_tenant_id, F_BillNo)
    INCLUDE (F_Id, F_FlowTitle);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_PURCHASE_USER' AND object_id = OBJECT_ID('wform_purchaselist'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_PURCHASE_USER ON wform_purchaselist (f_tenant_id, F_ApplyUser)
    INCLUDE (F_Id, F_FlowTitle, F_PurchaseDate);
PRINT '--- wform_purchaselist done ---';

-- wform_travelapply
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_TRAVEL_FLOW' AND object_id = OBJECT_ID('wform_travelapply'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_TRAVEL_FLOW ON wform_travelapply (f_tenant_id, F_FlowId)
    INCLUDE (F_Id, F_FlowTitle, F_BillNo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_TRAVEL_BILLNO' AND object_id = OBJECT_ID('wform_travelapply'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_TRAVEL_BILLNO ON wform_travelapply (f_tenant_id, F_BillNo)
    INCLUDE (F_Id, F_FlowTitle);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WFORM_TRAVEL_USER' AND object_id = OBJECT_ID('wform_travelapply'))
    CREATE NONCLUSTERED INDEX IDX_WFORM_TRAVEL_USER ON wform_travelapply (f_tenant_id, F_TravelMan)
    INCLUDE (F_Id, F_FlowTitle, F_ApplyDate);
PRINT '--- wform_travelapply done ---';

PRINT '=== Batch 13 ADD INDEX COMPLETE ===';
COMMIT TRANSACTION;
GO
