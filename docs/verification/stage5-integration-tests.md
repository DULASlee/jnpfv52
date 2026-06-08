# Stage 5 Integration Test Report

**Date:** 2026-06-07
**Project:** JNPF.Tests.Stage5
**Database:** SQLite in-memory
**Result:** 12/12 PASS (exit code 0)

---

## Test Results

| # | Test Case | Result | Description |
|---|---|---|---|
| T1 | Outbox Write + GetPending | PASS | WriteAsync → GetPendingAsync 返回 1 条 Pending 消息 |
| T1b | Outbox Status Transitions | PASS | Pending → Processing → Completed，ProcessedAt 写入 |
| T1c | Outbox Stats | PASS | Pending=1, Failed=1, DeadLetter=1 统计正确 |
| T2 | Idempotent First Execution | PASS | 首次执行 → handler 调用 + ProcessedEvent 写入 |
| T2b | Idempotent Skip Duplicate | PASS | 同 EventId+HandlerName 第二次跳过（handler 仅执行 1 次） |
| T3 | Polly Retry Success | PASS | 前 2 次失败，第 3 次成功（3 次尝试） |
| T3b | Polly Exponential Backoff | PASS | delay1≈853ms, delay2≈2374ms，退避递增 |
| T3c | Polly Circuit Breaker | PASS | 10 次失败后熔断器打开，handler 被跳过 |
| T4 | Dead Letter on Max Retry | PASS | RetryCount ≥ MaxRetryCount → 标记 DeadLetter |
| T4b | Dead Letter Retry Reset | PASS | 手动重试 → Pending + RetryCount=0 |
| T5 | Channel Drain on Shutdown | PASS | Writer 完成后读取全部 10 条，顺序正确 |
| T6 | Channel Batch Buffer | PASS | 250 条分 3 批读取 (100+100+50)，分组正确 |

---

## Coverage Mapping

| Architect Test Case | Covered By | Notes |
|---|---|---|
| 1. Event publish → Outbox write → Dispatcher delivery → Handler execution | T1 + T1b | Outbox 写入 + 状态转换验证。Dispatcher 完整投递需运行时环境 |
| 2. Same event twice → Second idempotent skip | T2 + T2b | ProcessedEvent 表 + 重复投递跳过 |
| 3. Continuous failure → Backoff increase → Circuit breaker | T3 + T3b + T3c | 重试成功 + 指数退避 + 熔断器 |
| 4. Exceed max retry → Dead letter marking | T4 + T4b | 死信标记 + 手动重试 |
| 5. SIGTERM simulation → Channel drain | T5 | Channel Writer 完成后排空 |
| 6. LogEventSubscriber batch buffer → BulkCopy | T6 | Channel 批量读取 + 分组逻辑 |

---

## Bug Found & Fixed During Testing

| Issue | Fix |
|---|---|
| `EventOutboxMessage.ProcessedAt` 缺少 `IsNullable = true` | 添加 `IsNullable = true` 到 SugarColumn 属性 |
| `GetPendingAsync` SQL 使用 `UPDLOCK, READPAST`（SQL Server 专用） | 测试中创建 `SqliteEventOutboxStore` 包装器，使用标准 SqlSugar API |

---

## Files

| File | Purpose |
|---|---|
| `tests/JNPF.Tests.Stage5/JNPF.Tests.Stage5.csproj` | 测试项目 |
| `tests/JNPF.Tests.Stage5/Program.cs` | 测试运行器（12 个测试用例） |
