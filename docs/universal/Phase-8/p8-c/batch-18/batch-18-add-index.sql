-- P8-C Batch 18 — ADD INDEX DDL (system-core-message)
-- Generated: 2026-08-30
-- Skill v1.0 (FROZEN): schema-drift detected + auto-fixed
-- Scope: 10 tables, ~25 indexes

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

PRINT '=== Batch 18 ADD INDEX START ===';

-- base_msg_monitor (message dispatch monitoring)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MSGMONITOR_TEMPLATE' AND object_id = OBJECT_ID('base_msg_monitor'))
    CREATE NONCLUSTERED INDEX IDX_MSGMONITOR_TEMPLATE ON base_msg_monitor (f_tenant_id, f_message_template_id)
    INCLUDE (f_id, f_account_id, f_title, f_send_time, f_message_type);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MSGMONITOR_TYPE' AND object_id = OBJECT_ID('base_msg_monitor'))
    CREATE NONCLUSTERED INDEX IDX_MSGMONITOR_TYPE ON base_msg_monitor (f_tenant_id, f_message_type)
    INCLUDE (f_id, f_account_id, f_send_time);
PRINT '--- base_msg_monitor done ---';

-- base_msg_send (message send config)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MSGSEND_CODE' AND object_id = OBJECT_ID('base_msg_send'))
    CREATE NONCLUSTERED INDEX IDX_MSGSEND_CODE ON base_msg_send (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_template_type, f_enabled_mark);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MSGSEND_TYPE' AND object_id = OBJECT_ID('base_msg_send'))
    CREATE NONCLUSTERED INDEX IDX_MSGSEND_TYPE ON base_msg_send (f_tenant_id, f_template_type)
    INCLUDE (f_id, f_full_name, f_enabled_mark);
PRINT '--- base_msg_send done ---';

-- base_msg_send_template (send-template binding)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MSGSENDTPL_CONFIG' AND object_id = OBJECT_ID('base_msg_send_template'))
    CREATE NONCLUSTERED INDEX IDX_MSGSENDTPL_CONFIG ON base_msg_send_template (f_tenant_id, f_send_config_id)
    INCLUDE (f_id, f_message_type, f_template_id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MSGSENDTPL_TEMPLATE' AND object_id = OBJECT_ID('base_msg_send_template'))
    CREATE NONCLUSTERED INDEX IDX_MSGSENDTPL_TEMPLATE ON base_msg_send_template (f_tenant_id, f_template_id)
    INCLUDE (f_id, f_message_type);
PRINT '--- base_msg_send_template done ---';

-- base_msg_template (message template definition)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MSGTPL_CODE' AND object_id = OBJECT_ID('base_msg_template'))
    CREATE NONCLUSTERED INDEX IDX_MSGTPL_CODE ON base_msg_template (f_tenant_id, f_en_code)
    INCLUDE (f_id, f_full_name, f_template_type, f_enabled_mark);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MSGTPL_TYPE' AND object_id = OBJECT_ID('base_msg_template'))
    CREATE NONCLUSTERED INDEX IDX_MSGTPL_TYPE ON base_msg_template (f_tenant_id, f_template_type)
    INCLUDE (f_id, f_full_name, f_template_code);
PRINT '--- base_msg_template done ---';

-- base_msg_sms_field (SMS field mapping)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SMSFIELD_TEMPLATE' AND object_id = OBJECT_ID('base_msg_sms_field'))
    CREATE NONCLUSTERED INDEX IDX_SMSFIELD_TEMPLATE ON base_msg_sms_field (f_tenant_id, f_template_id)
    INCLUDE (f_id, f_field, f_sms_field);
PRINT '--- base_msg_sms_field done ---';

-- base_notice (announcement / notice; f_to_user_ids is nvarchar(MAX) — cannot index)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_NOTICE_CATEGORY' AND object_id = OBJECT_ID('base_notice'))
    CREATE NONCLUSTERED INDEX IDX_NOTICE_CATEGORY ON base_notice (f_tenant_id, f_category)
    INCLUDE (f_id, f_title, f_type, f_creator_time);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_NOTICE_TYPE' AND object_id = OBJECT_ID('base_notice'))
    CREATE NONCLUSTERED INDEX IDX_NOTICE_TYPE ON base_notice (f_tenant_id, f_type)
    INCLUDE (f_id, f_title, f_creator_time);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_NOTICE_SEND_CONFIG' AND object_id = OBJECT_ID('base_notice'))
    CREATE NONCLUSTERED INDEX IDX_NOTICE_SEND_CONFIG ON base_notice (f_tenant_id, f_send_config_id)
    INCLUDE (f_id, f_title);
PRINT '--- base_notice done ---';

-- base_message (user message inbox; f_body_text is nvarchar(MAX))
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MESSAGE_USER' AND object_id = OBJECT_ID('base_message'))
    CREATE NONCLUSTERED INDEX IDX_MESSAGE_USER ON base_message (f_tenant_id, f_user_id, f_is_read)
    INCLUDE (f_id, f_type, f_title, f_creator_time);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MESSAGE_TYPE' AND object_id = OBJECT_ID('base_message'))
    CREATE NONCLUSTERED INDEX IDX_MESSAGE_TYPE ON base_message (f_tenant_id, f_type, f_is_read)
    INCLUDE (f_id, f_title, f_user_id, f_creator_time);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MESSAGE_READ' AND object_id = OBJECT_ID('base_message'))
    CREATE NONCLUSTERED INDEX IDX_MESSAGE_READ ON base_message (f_tenant_id, f_is_read, f_creator_time DESC)
    INCLUDE (f_id, f_user_id, f_title);
PRINT '--- base_message done ---';

-- base_msg_wechat_user (wechat user mapping)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WXUSER_GZH' AND object_id = OBJECT_ID('base_msg_wechat_user'))
    CREATE NONCLUSTERED INDEX IDX_WXUSER_GZH ON base_msg_wechat_user (f_tenant_id, f_gzh_id)
    INCLUDE (f_id, f_user_id, f_open_id, f_close_mark);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_WXUSER_USER' AND object_id = OBJECT_ID('base_msg_wechat_user'))
    CREATE NONCLUSTERED INDEX IDX_WXUSER_USER ON base_msg_wechat_user (f_tenant_id, f_user_id)
    INCLUDE (f_id, f_gzh_id, f_open_id);
PRINT '--- base_msg_wechat_user done ---';

-- base_msg_short_link (short URL tracking)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SHORTLINK_CODE' AND object_id = OBJECT_ID('base_msg_short_link'))
    CREATE NONCLUSTERED INDEX IDX_SHORTLINK_CODE ON base_msg_short_link (f_tenant_id, f_short_link)
    INCLUDE (f_id, f_real_pc_link, f_click_num, f_is_used);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_SHORTLINK_USER' AND object_id = OBJECT_ID('base_msg_short_link'))
    CREATE NONCLUSTERED INDEX IDX_SHORTLINK_USER ON base_msg_short_link (f_tenant_id, f_user_id)
    INCLUDE (f_id, f_short_link, f_click_num);
PRINT '--- base_msg_short_link done ---';

-- base_msg_template_param (template parameters)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IDX_MSGTPLPARAM_TEMPLATE' AND object_id = OBJECT_ID('base_msg_template_param'))
    CREATE NONCLUSTERED INDEX IDX_MSGTPLPARAM_TEMPLATE ON base_msg_template_param (f_tenant_id, f_template_id)
    INCLUDE (f_id, f_field, f_field_name);
PRINT '--- base_msg_template_param done ---';

PRINT '=== Batch 18 ADD INDEX COMPLETE ===';

COMMIT TRANSACTION;
GO
