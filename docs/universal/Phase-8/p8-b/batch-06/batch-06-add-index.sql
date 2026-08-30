-- P8-B Batch 06 — ADD INDEX DDL (system-extension)
-- Generated: 2026-08-30
-- Scope: 6 tables, ~14 indexes
-- Purpose: Reach 30 Table Units threshold for P8-C transition

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 06 ADD INDEX START ===';

-- ext_table_example (P8-A deferred index execution)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EXTEXAMPLE_TYPE' AND object_id = OBJECT_ID('ext_table_example'))
    CREATE NONCLUSTERED INDEX IDX_EXTEXAMPLE_TYPE ON ext_table_example (f_tenant_id, f_project_type)
    INCLUDE (f_id, f_project_code, f_project_name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EXTEXAMPLE_REGISTRANT' AND object_id = OBJECT_ID('ext_table_example'))
    CREATE NONCLUSTERED INDEX IDX_EXTEXAMPLE_REGISTRANT ON ext_table_example (f_tenant_id, f_registrant)
    INCLUDE (f_id, f_project_code, f_project_name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EXTEXAMPLE_CUSTOMER' AND object_id = OBJECT_ID('ext_table_example'))
    CREATE NONCLUSTERED INDEX IDX_EXTEXAMPLE_CUSTOMER ON ext_table_example (f_tenant_id, f_customer_name)
    INCLUDE (f_id, f_project_code);
PRINT '--- ext_table_example done ---';

-- ext_product (38 cols)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PRODUCT_TYPE' AND object_id = OBJECT_ID('ext_product'))
    CREATE NONCLUSTERED INDEX IDX_PRODUCT_TYPE ON ext_product (f_tenant_id, f_type)
    INCLUDE (f_id, f_en_code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PRODUCT_CUSTOMER' AND object_id = OBJECT_ID('ext_product'))
    CREATE NONCLUSTERED INDEX IDX_PRODUCT_CUSTOMER ON ext_product (f_tenant_id, f_customer_id)
    INCLUDE (f_id, f_en_code, f_customer_name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_PRODUCT_AUDIT_STATE' AND object_id = OBJECT_ID('ext_product'))
    CREATE NONCLUSTERED INDEX IDX_PRODUCT_AUDIT_STATE ON ext_product (f_tenant_id, f_audit_state)
    INCLUDE (f_id, f_en_code, f_audit_name);
PRINT '--- ext_product done ---';

-- ext_customer
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_CUSTOMER_ENCODE' AND object_id = OBJECT_ID('ext_customer'))
    CREATE NONCLUSTERED INDEX IDX_CUSTOMER_ENCODE ON ext_customer (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_customer_name, f_full_name, f_contact_tel);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_CUSTOMER_NAME' AND object_id = OBJECT_ID('ext_customer'))
    CREATE NONCLUSTERED INDEX IDX_CUSTOMER_NAME ON ext_customer (f_tenant_id, f_customer_name)
    INCLUDE (f_id, f_en_code);
PRINT '--- ext_customer done ---';

-- ext_order
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ORDER_CODE' AND object_id = OBJECT_ID('ext_order'))
    CREATE NONCLUSTERED INDEX IDX_ORDER_CODE ON ext_order (f_tenant_id, f_order_code)
    INCLUDE (f_id, f_customer_id, f_current_state, f_order_date);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ORDER_CUSTOMER' AND object_id = OBJECT_ID('ext_order'))
    CREATE NONCLUSTERED INDEX IDX_ORDER_CUSTOMER ON ext_order (f_tenant_id, f_customer_id)
    INCLUDE (f_id, f_order_code, f_current_state);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ORDER_STATE' AND object_id = OBJECT_ID('ext_order'))
    CREATE NONCLUSTERED INDEX IDX_ORDER_STATE ON ext_order (f_tenant_id, f_current_state)
    INCLUDE (f_id, f_order_code, f_customer_id);
PRINT '--- ext_order done ---';

-- ext_order_entry (line items)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ORDERENTRY_ORDER' AND object_id = OBJECT_ID('ext_order_entry'))
    CREATE NONCLUSTERED INDEX IDX_ORDERENTRY_ORDER ON ext_order_entry (f_tenant_id, f_order_id)
    INCLUDE (f_id, f_goods_id, f_goods_name, f_qty, f_actual_amount);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_ORDERENTRY_GOODS' AND object_id = OBJECT_ID('ext_order_entry'))
    CREATE NONCLUSTERED INDEX IDX_ORDERENTRY_GOODS ON ext_order_entry (f_tenant_id, f_goods_id)
    INCLUDE (f_id, f_order_id, f_qty);
PRINT '--- ext_order_entry done ---';

-- ext_email_config
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_EMAILCONFIG_ACCOUNT' AND object_id = OBJECT_ID('ext_email_config'))
    CREATE NONCLUSTERED INDEX IDX_EMAILCONFIG_ACCOUNT ON ext_email_config (f_account)
    INCLUDE (f_id, f_pop3_host, f_smtp_host);
PRINT '--- ext_email_config done ---';

PRINT '=== Batch 06 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
