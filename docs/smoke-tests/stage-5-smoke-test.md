# Stage 5 Smoke Test Report

**Date:** 2026-06-07
**Stage:** 5 — TenantSafe Templates + DiffLog Publisher + Event Reliability Pipeline + Refresh Token + Dead Letter API

---

## L1: Build Verification

| Project | Result |
|---|---|
| JNPF.API.Entry | PASS (0 errors) |
| JNPF.Extras.EventBus.Outbox | PASS (0 errors) |
| JNPF.Common.Core (PollyRetryHandlerExecutor) | PASS (0 errors) |
| JNPF.OAuth (RefreshTokenService) | PASS (0 errors) |

---

## L2: Runtime Verification

| Check | Result |
|---|---|
| Port 5000 LISTENING | PASS |
| EventOutboxDispatcher started | PASS (log: "EventOutboxDispatcher started.") |
| Pre-existing MemoryCache NullRef | Known issue (not caused by Stage 5) |

---

## L3: Health Check

```json
{"status":"Healthy","checks":[{"name":"sqlserver","status":"Healthy"}]}
```

**Result: PASS**

---

## Task Summary

### 5.1: TenantSafe Template Update
**6 templates modified** (174 insertions, 20 deletions):
- 1-SingleTable/Service.cs.vm + InlineEditor — INSERT, DELETE, BATCH DELETE
- 2-MainBelt/Service.cs.vm + InlineEditor — DELETE, BATCH DELETE, UPDATE
- 5-PrimarySecondary/Service.cs.vm + InlineEditor — DELETE, BATCH DELETE
- 4-MainBeltVice — No changes (logical delete only, not compatible with Safe*)

**Replacement pattern:** Ternary `@(Model.DbLinkId != "0" ? ... : ...)` → `@if/@else if/@else` 3-way split:
1. Cross-DB (`DbLinkId != "0"`) → keeps `_sqlSugarClient`
2. Logical delete → keeps `AsDeleteable().IsLogic()`
3. Physical local → uses `SafeInsertAsync/SafeDeleteByIdAsync/SafeDeleteByIdsAsync`

**UPDATE handling:** Only 2-MainBelt replaced with `SafeUpdateAsync(entity)` for simple cases. ConcurrencyLock/UpdateColumns paths preserved.

### 5.2: DiffLog Publisher Replacement
**New files:**
- `DiffLogPublishModule.cs` — DI module, replaces NoOp when `EnableDiffLog=true`
- `OutboxDiffLogPublisher.cs` — Writes DiffLog to Outbox via `IEventOutboxStore`
- `IEventOutboxStore.cs` — Framework-layer interface
- `NoOpEventOutboxStore.cs` — Temporary until Task 5.3 store is active

**Sealed file constraint:** SqlSugarConfigureExtensions.cs NOT modified. DI override pattern used.

### 5.3: Event Reliability Pipeline
**New project:** `JNPF.Extras.EventBus.Outbox` (infrastructure layer)

**New files:**
- `EventOutboxMessage.cs` — Outbox entity (SYS_EVENT_OUTBOX_MESSAGE)
- `SqlSugarEventOutboxStore.cs` — Store with UPDLOCK+READPAST row locking
- `EventOutboxDispatcher.cs` — BackgroundService with Channel signaling + 30s poll
- `PollyRetryHandlerExecutor.cs` — Exponential backoff (1s→60s) + ±20% jitter + circuit breaker (10 failures → 30s)
- `ProcessedEvent.cs` — Idempotency entity (SYS_PROCESSED_EVENT)
- `IdempotentEventHandler.cs` — Check-before-execute decorator
- `BypassOutboxAttribute.cs` — For system heartbeat events
- `EventBusModule.cs` — Registers Polly executor + Outbox components

### 5.4: Refresh Token Mechanism
**New file:** `RefreshTokenService.cs` — `POST /api/oauth/refresh` endpoint
- Accepts expired accessToken + refreshToken
- Uses `JWTEncryption.Exchange()` for validation
- Returns new dual Token pair

**Modified:** `OAuthService.cs` — Login now returns `refreshToken` in response body

### 5.5: Dead Letter Management API
**New file:** `DeadLetterService.cs` (in API.Entry/Services for DynamicApiController discovery)
- `GET /api/eventbus/deadletters` — Paginated dead letter query
- `POST /api/eventbus/deadletters/{id}/retry` — Manual retry
- `POST /api/eventbus/deadletters/batch-retry` — Batch retry
- `GET /api/eventbus/outbox/stats` — Pending/Failed/DeadLetter counts

---

## Known Issues

| Issue | Impact | Root Cause |
|---|---|---|
| DeadLetter API returns 404 | Endpoint not registered | DynamicApiController assembly scanning — fixed by moving to API.Entry project |
| MemoryCache.GetAllKeys() NullRef | Background job errors | Pre-existing .NET 8 API change (not Stage 5) |

---

## Sealed File Compliance

| File | Status |
|---|---|
| SqlSugarConfigureExtensions.cs | NOT modified |
| Program.cs | NOT modified |
| JwtHandler.cs | NOT modified |
| AppServiceCollectionExtensions.cs | NOT modified |
| Startup.cs | NOT modified |
| SqlSugarDbContextProvider.cs | NOT modified |
| SqlSugarRepository.cs | NOT modified |

---

## Supplementary Verification (2026-06-07)

### Supplementary A: DeadLetter API Endpoint Registration — PASS

**Problem:** DeadLetter API returned 404 because `JNPF.Extras.EventBus.Outbox` assembly was not in `App.Assemblies` for DynamicApiController scanning.

**Fix:** Moved `DeadLetterService` from `JNPF.Extras.EventBus.Outbox/EventBus/DeadLetter/` to `JNPF.API.Entry/Services/DeadLetterService.cs` with `IDynamicApiController, ITransient` interfaces. Removed old duplicate from Outbox project.

**Verification:**
```
GET  /api/eventbus/outbox/stats  → HTTP 200 (body: code 500, "对象名 'SYS_EVENT_OUTBOX_MESSAGE' 无效" — expected, table not yet created)
GET  /api/eventbus/deadletters   → HTTP 200
POST /api/eventbus/deadletters/{id}/retry → HTTP 200
```

**Status:** Endpoints registered correctly.

**Migration (2026-06-07):** Created `outbox_migration.sql` and executed against ZXAF_V1_DevTest1. Tables SYS_EVENT_OUTBOX_MESSAGE and SYS_PROCESSED_EVENT now exist.

**Post-migration verification:**
```json
GET /api/eventbus/outbox/stats → {"code":200,"data":{"PendingCount":0,"FailedCount":0,"DeadLetterCount":0}}
```

### Supplementary B: L4 Browser Smoke Test — PASS

| Test Item | Result | Details |
|---|---|---|
| Login page render | PASS | Form fields, buttons, branding all visible |
| Login with admin/123456 | PASS | Redirected to /home after 3s |
| Home dashboard | PASS | KPI cards ($2,000/$20,000/$8,000/$5,000), charts, navigation |
| System config page | PASS | Tabbed form (基本设置/安全设置/同步设置), system title/version fields |
| User management CRUD | PASS | Department tree + user table (41 records, 3 pages), search filters, action buttons |
| Workflow menu expand | PASS | Sub-items: 流程表单, 流程设计, 流程监控 |
| Console errors | PASS | 0 errors across all pages |

**Screenshots:** `l4-login-page.png`, `l4-home-page.png`, `l4-sysconfig.png`, `l4-user-management.png`

### Supplementary C: LogEventSubscriber Refactoring — COMPLETE (2026-06-07)

**Status:** Sub-task 5.3.6 executed. `LogEventSubscriber.cs` refactored to Channel bulk buffer.

**Changes:**
- `Channel<LogEventSource>` buffer (capacity 1000, BoundedChannelFullMode.DropWrite)
- `CreateLog` handler writes to Channel (non-blocking TryWrite)
- Background flush loop: every 5 seconds, drains up to 100 records
- Groups by TenantId → `CopyNew()` per group → `Fastest<T>().BulkCopyAsync()`
- Graceful shutdown: `StopAsync` completes Channel writer, flushes remaining entries
- Implements `IHostedService` alongside existing `IEventSubscriber, ISingleton`

### Supplementary D: Integration Test Results — PASS (2026-06-07)

**Status:** Sub-task 5.3.7 executed. Test project `JNPF.Tests.Stage5` created with 12 test cases.

**Result:** 12/12 PASS (exit code 0)

| # | Test Case | Result |
|---|---|---|
| T1 | Outbox Write + GetPending | PASS |
| T1b | Outbox Status Transitions | PASS |
| T1c | Outbox Stats | PASS |
| T2 | Idempotent First Execution | PASS |
| T2b | Idempotent Skip Duplicate | PASS |
| T3 | Polly Retry Success | PASS |
| T3b | Polly Exponential Backoff | PASS |
| T3c | Polly Circuit Breaker | PASS |
| T4 | Dead Letter on Max Retry | PASS |
| T4b | Dead Letter Retry Reset | PASS |
| T5 | Channel Drain on Shutdown | PASS |
| T6 | Channel Batch Buffer | PASS |

**Bug fixed during testing:** `EventOutboxMessage.ProcessedAt` missing `IsNullable = true` (SQLite CodeFirst compatibility).

**Detailed report:** `docs/verification/stage5-integration-tests.md`

---

## Stage 5 Final Status

| Task | Status | Notes |
|---|---|---|
| 5.1 TenantSafe Templates | ✅ COMPLETE | 6 templates modified, 3-way @if/@else split |
| 5.2 DiffLog Publisher | ✅ COMPLETE | DI override pattern, no sealed file changes |
| 5.3 Event Pipeline | ✅ COMPLETE | Outbox + Dispatcher + Polly + Idempotency |
| 5.4 Refresh Token | ✅ COMPLETE | POST /api/oauth/refresh with JWTEncryption.Exchange() |
| 5.5 Dead Letter API | ✅ COMPLETE | Endpoints registered, 200 OK (table migration pending) |
| 5.3.6 LogEventSubscriber | ✅ COMPLETE | Channel bulk buffer (Capacity=1000, Batch=100, Flush=5s) |
| 5.3.7 Integration Tests | ✅ PASS | 12/12 tests, SQLite in-memory, exit code 0 |
| L4 Browser Smoke | ✅ PASS | Login, dashboard, system config, user CRUD, workflow menu |
