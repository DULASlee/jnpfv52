# Check 5: Database Schema Management

## 5.1 Migration Mechanisms

- **Primary approach:** **Hybrid** — CodeFirst auto-create (SqlSugar) + manual SQL scripts.
- **CodeFirst usage:** Found primarily in test/verification code. Main application does NOT use `CodeFirst.InitTables()` in startup.
  - **127 entity files** across modules carry `[SugarTable]` attributes
  - CodeFirst is used extensively in tests:
    - `tests/verifications/SqlSugarVerification/Program.cs` (20+ usages)
    - `tests/JNPF.Tests.Stage5/Program.cs` (3 usages — EventOutboxMessage/ProcessedEvent)
    - `tests/JNPF.Tests.ADR012/Program.cs` (1 usage)

- **SQL scripts found:** 7 scripts:

| Script | Location | Purpose |
|---|---|---|
| jnpf_sundial_init.sql | `backend/web/` | Oracle DDL for Sundial job scheduling tables |
| jnpf_sundial_init_sqlserver.sql | `backend/web/` | SQL Server DDL for Sundial job scheduling |
| 主库脚本.sql | `backend/web/` | Full SQL Server DB creation (ZXAF_V1_DevTest1) — dump |
| jnpf事件库脚本.sql | `backend/web/` | SQL Server DB creation for jnpf_sundial — dump |
| logging_migration.sql | `backend/web/` | Adds F_TRACE_ID and F_TENANT_ID to BASE_SYS_LOG |
| outbox_migration.sql | `backend/web/` | Creates SYS_EVENT_OUTBOX_MESSAGE + SYS_PROCESSED_EVENT |
| rebrand-base_sys_config.sql | `scripts/` | Demo rebranding — updates BASE_SYS_CONFIG values |

- **Version tracking:** **None.** No SchemaVersions table, no MigrationHistory table, no `__EFMigrationsHistory` equivalent. Scripts named by purpose, not sequential version numbers, and run manually.

## 5.2 Schema Management Summary

- **Database type:** SQL Server (primary, `mcr.microsoft.com/mssql/server:2022-latest` in docker-compose). Oracle support exists for Sundial.
- **Database count:** 2 databases:
  1. **Main business DB** (`ZXAF_V1_DevTest1`): All business tables (BASE_SYS_*, flow tables, entity tables)
  2. **Sundial/event DB** (`jnpf_sundial`): Job scheduling tables (JOBCLUSTER, JOBDETAILS, JOBS, etc.)
  - Docker-compose references single SQL Server container for both
  - Multi-tenant support exists (T_TENANT_ID columns, TenantService)

- **Schema change history:** **Ad-hoc.** Changes tracked via:
  - Named SQL migration scripts in `backend/web/`
  - Git history for SQL scripts
  - No automated migration tooling (no EF Core migrations, no FluentMigrator, no DbUp)

- **Entity class patterns:** All use SqlSugar attributes:
  - `[SugarTable("TABLE_NAME")]` for table mapping
  - `[SugarColumn(IsPrimaryKey = true, ColumnName = "F_COLUMN")]` for column mapping
  - Column naming convention: `F_` prefix (F_ID, F_EVENT_NAME, F_CREATED_AT)
  - Organized by module (System, Permission, WorkFlow, Extend, Message, VisualDev, VisualData)

## 5.3 Outbox Tables Status

- **SYS_EVENT_OUTBOX_MESSAGE:** Created via `backend/web/outbox_migration.sql` (IF NOT EXISTS guard)
  - Entity: `infrastructure/.../EventOutboxMessage.cs` — `[SugarTable("SYS_EVENT_OUTBOX_MESSAGE")]`
  - Columns: F_ID, F_EVENT_NAME, F_EVENT_PAYLOAD, F_CREATED_AT, F_PROCESSED_AT, F_RETRY_COUNT, F_MAX_RETRY_COUNT, F_STATUS, F_ERROR
  - Indices: IX_OUTBOX_STATUS_CREATED, IX_OUTBOX_EVENT_NAME

- **SYS_PROCESSED_EVENT:** Created via `backend/web/outbox_migration.sql` (IF NOT EXISTS guard)
  - Entity: `infrastructure/.../ProcessedEvent.cs` — `[SugarTable("SYS_PROCESSED_EVENT")]`
  - Columns: F_EVENT_ID, F_HANDLER_NAME, F_PROCESSED_AT
  - Composite PK: (F_EVENT_ID, F_HANDLER_NAME)

- **Application code:**
  - `infrastructure/JNPF.Extras.EventBus.Outbox/` — dedicated Outbox infrastructure package
  - `application/.../Services/DeadLetterService.cs` — Dead Letter management API

### Impact on Stage 7.5 (Database Migration)

- **New managed migration project needed.** Current approach lacks:
  1. Execution history tracking (no migrations table)
  2. Ordering guarantees (script execution order is implicit)
  3. Rollback support
  4. Idempotent migration execution guarantee (some scripts have IF NOT EXISTS, others don't)
  5. Automated schema diff/validation

- **CodeFirst + manual migration coexistence:**
  - Keep CodeFirst for **new module development** (clean when you control schema)
  - Use manual SQL scripts for **production-compatible schema changes** to match `F_` prefix convention
  - Introduce migration tool (DbUp, FluentMigrator, or custom SqlSugar-based runner) with execution tracking
  - Standardize on sequential numbering for post-initialization schema changes

- **What needs unification:**
  1. **Database conflation:** Main + sundial databases need separate migration contexts
  2. **Migration order:** Scripts need sequential prefixes (e.g., 0001_, 0002_)
  3. **Migration table:** Create `SYS_SCHEMA_VERSION` table for tracking
  4. **Seed data:** Separate DDL migrations from seed/branding data

## Summary for Stage 7.5

Current schema management is functional but un-governed. 127 entity classes with SqlSugar attributes served by hand-written SQL scripts with no execution history. Stage 7.5 should introduce a managed migration system (recommend: DbUp or custom SqlSugar-based runner) with execution history table, sequential script numbering, and separate contexts for main and sundial databases.
