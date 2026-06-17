-- ============================================================
-- Logging System Overhaul: Add TraceId and TenantId to BASE_SYS_LOG
-- 源文件: logging_migration.sql
-- PostgreSQL 版本
-- ============================================================

ALTER TABLE base_sys_log ADD COLUMN f_trace_id varchar(64) NULL;
ALTER TABLE base_sys_log ADD COLUMN f_tenant_id varchar(64) NULL;

-- Index for TraceId queries
CREATE INDEX ix_sys_log_trace_id ON base_sys_log(f_trace_id);

-- Index for tenant + time range queries
CREATE INDEX ix_sys_log_tenant_time ON base_sys_log(f_tenant_id, f_creator_time);
