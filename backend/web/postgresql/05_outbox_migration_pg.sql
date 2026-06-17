-- ============================================================
-- Outbox Pipeline Tables Migration
-- 源文件: outbox_migration.sql
-- PostgreSQL 版本
-- Stage: 5.3 - Event Reliability Pipeline
-- ============================================================

-- Table 1: SYS_EVENT_OUTBOX_MESSAGE (Outbox pattern for reliable event delivery)
CREATE TABLE IF NOT EXISTS sys_event_outbox_message (
    f_id              uuid         NOT NULL,
    f_event_name      varchar(200) NOT NULL,
    f_event_payload   text         NOT NULL,
    f_created_at      timestamp    NOT NULL,
    f_processed_at    timestamp    NULL,
    f_retry_count     integer      NOT NULL DEFAULT 0,
    f_max_retry_count integer      NOT NULL DEFAULT 3,
    f_status          integer      NOT NULL DEFAULT 0,
    f_error           text         NULL,
    CONSTRAINT pk_sys_event_outbox_message PRIMARY KEY (f_id)
);

-- Index for Dispatcher polling: status + created_at
CREATE INDEX IF NOT EXISTS ix_outbox_status_created
    ON sys_event_outbox_message(f_status, f_created_at);

-- Index for dead letter management queries
CREATE INDEX IF NOT EXISTS ix_outbox_event_name
    ON sys_event_outbox_message(f_event_name);

-- Table 2: SYS_PROCESSED_EVENT (Idempotency records)
CREATE TABLE IF NOT EXISTS sys_processed_event (
    f_event_id      varchar(200) NOT NULL,
    f_handler_name  varchar(200) NOT NULL,
    f_processed_at  timestamp    NOT NULL DEFAULT now(),
    CONSTRAINT pk_sys_processed_event PRIMARY KEY (f_event_id, f_handler_name)
);
