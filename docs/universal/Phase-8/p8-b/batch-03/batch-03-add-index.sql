-- P8-B Batch 03 — ADD INDEX DDL (system-core dictionary)
-- Generated: 2026-08-30
-- Mode: Controlled Production
-- Scope: 5 tables, 12 indexes (all additive)

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Batch 03 ADD INDEX START ===';

-- base_dictionary_type (3 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DICTTYPE_PARENT' AND object_id = OBJECT_ID('base_dictionary_type'))
    CREATE NONCLUSTERED INDEX IDX_DICTTYPE_PARENT ON base_dictionary_type (f_tenant_id, f_parent_id)
    INCLUDE (f_id, f_full_name, f_en_code, f_enabled_mark);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DICTTYPE_ENCODE' AND object_id = OBJECT_ID('base_dictionary_type'))
    CREATE NONCLUSTERED INDEX IDX_DICTTYPE_ENCODE ON base_dictionary_type (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_type);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DICTTYPE_TYPE' AND object_id = OBJECT_ID('base_dictionary_type'))
    CREATE NONCLUSTERED INDEX IDX_DICTTYPE_TYPE ON base_dictionary_type (f_tenant_id, f_type)
    INCLUDE (f_id, f_full_name);
PRINT '--- base_dictionary_type done ---';

-- base_dictionary_data (3 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DICTDATA_TYPEID' AND object_id = OBJECT_ID('base_dictionary_data'))
    CREATE NONCLUSTERED INDEX IDX_DICTDATA_TYPEID ON base_dictionary_data (f_tenant_id, f_dictionary_type_id)
    INCLUDE (f_id, f_full_name, f_en_code, f_sort_code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DICTDATA_PARENT' AND object_id = OBJECT_ID('base_dictionary_data'))
    CREATE NONCLUSTERED INDEX IDX_DICTDATA_PARENT ON base_dictionary_data (f_tenant_id, f_parent_id)
    INCLUDE (f_id, f_full_name, f_dictionary_type_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_DICTDATA_ENCODE' AND object_id = OBJECT_ID('base_dictionary_data'))
    CREATE NONCLUSTERED INDEX IDX_DICTDATA_ENCODE ON base_dictionary_data (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_dictionary_type_id);
PRINT '--- base_dictionary_data done ---';

-- base_bill_rule (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_BILLRULE_ENCODE' AND object_id = OBJECT_ID('base_bill_rule'))
    CREATE NONCLUSTERED INDEX IDX_BILLRULE_ENCODE ON base_bill_rule (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_prefix, f_output_number);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_BILLRULE_CATEGORY' AND object_id = OBJECT_ID('base_bill_rule'))
    CREATE NONCLUSTERED INDEX IDX_BILLRULE_CATEGORY ON base_bill_rule (f_tenant_id, f_category)
    INCLUDE (f_id, f_full_name, f_en_code);
PRINT '--- base_bill_rule done ---';

-- base_common_fields (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_COMMONFIELDS_NAME' AND object_id = OBJECT_ID('base_common_fields'))
    CREATE NONCLUSTERED INDEX IDX_COMMONFIELDS_NAME ON base_common_fields (f_tenant_id, f_field_name)
    INCLUDE (f_id, f_data_type, f_data_length);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_COMMONFIELDS_DATATYPE' AND object_id = OBJECT_ID('base_common_fields'))
    CREATE NONCLUSTERED INDEX IDX_COMMONFIELDS_DATATYPE ON base_common_fields (f_tenant_id, f_data_type)
    INCLUDE (f_id, f_field_name);
PRINT '--- base_common_fields done ---';

-- base_common_words (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_COMMONWORDS_TYPE' AND object_id = OBJECT_ID('base_common_words'))
    CREATE NONCLUSTERED INDEX IDX_COMMONWORDS_TYPE ON base_common_words (f_tenant_id, f_common_words_type)
    INCLUDE (f_id, f_common_words_text);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_COMMONWORDS_SYSTEMIDS' AND object_id = OBJECT_ID('base_common_words'))
    CREATE NONCLUSTERED INDEX IDX_COMMONWORDS_SYSTEMIDS ON base_common_words (f_tenant_id, f_system_ids)
    INCLUDE (f_id, f_common_words_type);
PRINT '--- base_common_words done ---';

PRINT '=== Batch 03 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
