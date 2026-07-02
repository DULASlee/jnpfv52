# VisualDevEntity Temporary Record Lifecycle Design

> **日期**：2026-07-02
> **方案**：A — 临时记录生命周期
> **关联**：`2026-07-02-c2-ir-adapter-design.md`（适配方案主文档）
> **状态**：待确认

---

## 1. Overview

Plan A adapter creates temporary `VisualDevEntity` records in the database so that `CodeGenService.DownloadCode()` can operate without any code-path changes. These records must be reliably cleaned up to avoid database bloat.

### Flow

```
IR JSON
  │
  ▼
IrToVisualDevAdapter.Convert(irJson)
  → INSERT temp VisualDevEntity (Id, FullName, FormData, Tables, ...)
  │
  ▼
CodeGenService.DownloadCode(tempEntityId, input)
  → read temp record by Id → generate files → zip
  │
  ▼
Cleanup (sync or async)
  → soft-delete / hard-delete temp record
```

### Key Non-Goals

- No changes to `CodeGenService.DownloadCode()` — it reads by Id and is agnostic to whether the record is temporary or permanent.
- No new table or schema — temp records live in the existing `VisualDevEntity` table, distinguished by a naming convention.

---

## 2. Creation Phase

### 2.1 When

C2 pipeline stage 4 (code generation), immediately before calling `CodeGenService.DownloadCode()`.

### 2.2 How

`IrToVisualDevAdapter.Convert(irJson)` constructs and persists a `VisualDevEntity`:

| Field | Value | Source |
|-------|-------|--------|
| `Id` | `Guid.NewGuid().ToString("N")` | Generated — 32 hex chars, no hyphens |
| `FullName` | `"[AI-TEMP] {moduleName} {timestamp}"` | IR → module name |
| `FormData` | JSON string | IR → formData |
| `Tables` | JSON string | IR → tables |
| `ColumnData` | JSON string or null | IR → columnData (optional) |
| `AppColumnData` | JSON string or null | IR → appColumnData (optional) |
| `Category` | Dictionary category ID | IR → category or default |
| `WebType` | `1` (PC) | Fixed |
| `Type` | `1` (standard) | Fixed |
| `DeleteMark` | `null` | Not deleted |
| `TenantId` | Current tenant | Injected by framework |
| `CreatorTime` | `DateTime.UtcNow` | Injected by framework |

### 2.3 Temp Record Marker Convention

Temp records are identified by `FullName` starting with `"[AI-TEMP]"` (case-insensitive). This prefix is used by:

- **Cleanup logic** — stale record deletion queries `FullName LIKE '[AI-TEMP]%'`
- **Diagnostics** — operators can visually distinguish auto-generated temp records from user-created ones
- **Metrics** — monitors can count `[AI-TEMP]` records to track code-generation volume

The prefix is defined as a single constant:

```csharp
public const string AiTempPrefix = "[AI-TEMP]";
```

### 2.4 Repository Method

```csharp
public async Task<VisualDevEntity> CreateTempRecordAsync(
    string formData, string tables, string? columnData,
    string? appColumnData, string moduleName, string category,
    string tenantId)
{
    var entity = new VisualDevEntity
    {
        Id = Guid.NewGuid().ToString("N"),
        FullName = $"{AiTempPrefix} {moduleName} {DateTime.UtcNow:yyyyMMdd-HHmmss}",
        FormData = formData,
        Tables = tables,
        ColumnData = columnData,
        AppColumnData = appColumnData,
        Category = category,
        WebType = 1,
        Type = 1,
        DeleteMark = null,
        TenantId = tenantId
    };

    await _repository.InsertAsync(entity);
    return entity;
}
```

### 2.5 Tenant Safety

`TenantId` is inherited from the base entity. The framework's `ITenantFilter` global query filter automatically scopes all reads and writes. No additional tenant-handling code is needed.

---

## 3. Usage Phase

### 3.1 Consumption

`CodeGenService.DownloadCode(tempEntityId, input)` performs a standard `IRepository<VisualDevEntity>.GetFirstAsync()` lookup by `Id`. Since `ITenantFilter` is active, the record is visible only to the creating tenant.

### 3.2 Generated File Path

Generated files land in:
```
StudioWorkspace/{tenantId}/{pipelineId}/generated/
```

This path is already handled by the code-generation pipeline. The temp record's `Id` is not used in the file path — the pipeline's `pipelineId` determines the output directory.

### 3.3 Behavior Gap

CodeGenService treats temp records identically to permanent records. There is no code-path branching based on the `FullName` prefix or any other marker. This is intentional — zero risk of regression in the existing code generator.

---

## 4. Cleanup Phase

### 4.1 Success Path

After zip creation and delivery to the client, the caller cleans up:

```csharp
// After zip delivery succeeds
await _repository.DeleteAsync(tempEntity);
```

**Method choice:** `IRepository.DeleteAsync()` performs a hard delete. Soft-delete (`DeleteMark = true`) is an alternative, but since temp records have no business value after delivery, hard delete is preferred to keep the table lean.

### 4.2 Failure Path

If code generation throws, the catch block must clean up the temp record before re-throwing or returning the error response:

```csharp
try
{
    var tempEntity = await adapter.CreateTempRecordAsync(...);
    var zip = await codeGenService.DownloadCode(tempEntity.Id, input);
    // ... deliver zip ...
    await _repository.DeleteAsync(tempEntity);
}
catch (Exception ex)
{
    // Attempt cleanup; never let cleanup failure mask the original error
    if (tempEntity != null)
    {
        try { await _repository.DeleteAsync(tempEntity); }
        catch (Exception cleanupEx)
        {
            _logger.Warn(cleanupEx,
                "Failed to clean up temp VisualDevEntity {Id}", tempEntity.Id);
        }
    }
    throw; // Preserve original exception
}
```

**Rule:** The cleanup attempt in the catch block must never throw or replace the original exception. Use `ExceptionDispatchInfo.Capture(ex).Throw()` or simply `throw;` after the cleanup attempt.

### 4.3 Stale Record Cleanup (Background Job)

A hosted service runs daily to purge records that were not cleaned up at runtime (crashes, timeouts, process kills):

```csharp
public class TempRecordCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);
    private readonly TimeSpan _staleThreshold = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);
            await CleanupStaleTempRecordsAsync(stoppingToken);
        }
    }

    private async Task CleanupStaleTempRecordsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<IRepository<VisualDevEntity>>();

        var cutoff = DateTime.UtcNow.Add(-_staleThreshold);
        var staleRecords = await repo
            .GetFirstAsync(r =>
                r.FullName.StartsWith(AiTempPrefix) &&
                r.CreatorTime < cutoff
            );

        foreach (var record in staleRecords)
        {
            await repo.DeleteAsync(record);
        }
    }
}
```

**Configuration:** The stale threshold (default 24h) and check interval (default 24h) should be configurable via `appsettings.json`:

```json
{
  "TempRecordCleanup": {
    "Enabled": true,
    "CheckIntervalHours": 24,
    "StaleThresholdHours": 24
  }
}
```

### 4.4 What Happens Without Cleanup

If cleanup were skipped entirely, a typical C2 session creating 10-50 temp records per pipeline run would accumulate ~1,500-7,500 records per month per tenant. Given that each `VisualDevEntity` record stores JSON blobs (`FormData`, `Tables`, `ColumnData`) that can reach tens of KB, this results in meaningful storage growth. The stale cleanup job provides a safety net even if the synchronous cleanup is missed.

---

## 5. Idempotency

If the same pipeline calls code generation twice (e.g., retry after transient failure):

1. Create a **new** temp record (new `Id`, new `CreatorTime`).
2. The previous temp record from the first attempt remains in the table.
3. The stale cleanup job (Section 4.3) removes the orphaned record within 24 hours.

No deduplication logic is needed — reusing the same temp record would risk concurrent-read conflicts on a record that may already be in the cleanup window.

---

## 6. Concurrency

- Each pipeline invocation creates its own temp record (1:1 relationship).
- No sharing — no two pipelines read or write the same temp record.
- No lock needed.
- The `IRepository.InsertAsync` call is a single-row insert; no transaction scope is required beyond the default.

**Edge case:** The stale cleanup job could theoretically delete a temp record that a concurrent pipeline is actively using. In practice, this is prevented by the 24-hour stale threshold — no pipeline takes 24+ hours from record creation to code generation.

---

## 7. Error Handling Matrix

| Failure Point | Effect | Action |
|---------------|--------|--------|
| Temp record `INSERT` fails | No record created | Propagate error upstream. No cleanup needed. |
| Code generation throws | Temp record exists, zip not delivered | Catch → hard-delete temp record → re-throw original exception |
| Zip delivery fails | Temp record exists, client did not receive zip | Treat as success-path failure: delete temp record, return error to client |
| Cleanup `DELETE` throws | Orphaned temp record in DB | Log warning. Stale cleanup (24h) handles it. |
| Stale cleanup delete fails | Record survives another 24h cycle | Log error. One orphan has negligible impact. |
| Stale cleanup config `Enabled: false` | Orphans accumulate indefinitely | Log warning at startup. Monitor `[AI-TEMP]` record count. |

### Recovery

| Scenario | Recovery |
|----------|----------|
| Code gen fails after temp record creation | Synchronous deletion in catch block |
| Process crashes before cleanup | Stale cleanup job (worst case: 24h orphan window) |
| Stale cleanup job itself crashes | Next cycle handles it; missing cycles logged |

---

## 8. Migration / Schema Impact

- No new table.
- No new column on `VisualDevEntity`.
- No index change (the `FullName LIKE '[AI-TEMP]%'` query in stale cleanup will perform a full scan, but the volume of temp records is negligible compared to the main table).
- If perf becomes a concern, add a filtered index: `CREATE INDEX IX_VisualDevEntity_TempRecords ON VisualDevEntity(CreatorTime) WHERE FullName LIKE '[AI-TEMP]%'`.

---

## 9. Testing Strategy

| Test | Type | What to Verify |
|------|------|----------------|
| Create + cleanup | Integration | Record is inserted, `DownloadCode()` reads it, record is deleted after zip delivery |
| Failure cleanup | Integration | Exception during code gen → record is deleted |
| Stale cleanup | Integration | Record older than threshold is deleted by background job |
| Stale cleanup skip | Integration | Record newer than threshold is NOT deleted by background job |
| Tenancy | Integration | Tenant A's temp record is invisible to Tenant B |
| Concurrent pipeline | Stress | Two parallel pipelines with distinct temp records do not interfere |

---

## 10. Appendix: Code Change Summary

| File | Change | Priority |
|------|--------|----------|
| `IrToVisualDevAdapter.cs` | Add `CreateTempRecordAsync()` method | Required |
| CodeGen caller | Add try-catch-cleanup around `DownloadCode()` | Required |
| `TempRecordCleanupService.cs` | New `BackgroundService` | Recommended |
| `appsettings.json` | Add `TempRecordCleanup` section | Recommended |
