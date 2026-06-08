using JNPF.EventBus;
using JNPF.Extras.EventBus.Idempotency;
using JNPF.Extras.EventBus.Outbox;
using JNPF.EventHandler;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;

namespace JNPF.Tests.Stage5;

/// <summary>
/// Stage 5 集成测试 — Outbox 管道 + 幂等 + Polly + 死信 + Channel 排空.
/// 使用 SQLite 内存数据库。退出码：0=全部通过，1=有失败.
/// </summary>
class Program
{
    static int _passed = 0;
    static int _failed = 0;

    static async Task<int> Main()
    {
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("  Stage 5 Integration Tests");
        Console.WriteLine("  数据库: SQLite 内存数据库");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();

        using var client = CreateSqliteClient();

        try
        {
            await T1_OutboxWriteAndPoll(client);
            await T1b_OutboxStatusTransitions(client);
            await T1c_OutboxStats(client);
            await T2_IdempotentFirstExecution(client);
            await T2b_IdempotentSkipDuplicate(client);
            await T3_PollyRetrySuccess(client);
            await T3b_PollyRetryExponentialBackoff(client);
            await T3c_PollyCircuitBreaker(client);
            await T4_DeadLetterOnMaxRetry(client);
            await T4b_DeadLetterRetryReset(client);
            await T5_ChannelDrainOnShutdown();
            await T6_ChannelBatchBuffer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL] 未捕获异常: {ex}");
            _failed++;
        }

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine($"  结果: {_passed} 通过, {_failed} 失败");
        Console.WriteLine("═══════════════════════════════════════════════");

        return _failed > 0 ? 1 : 0;
    }

    static SqlSugarClient CreateSqliteClient()
    {
        var client = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = "DataSource=:memory:",
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute
        });
        client.Open();
        client.CodeFirst.InitTables<EventOutboxMessage>();
        client.CodeFirst.InitTables<ProcessedEvent>();
        return client;
    }

    static void Pass(string name)
    {
        Console.WriteLine($"  [PASS] {name}");
        _passed++;
    }

    static void Fail(string name, string reason)
    {
        Console.WriteLine($"  [FAIL] {name}: {reason}");
        _failed++;
    }

    // ═══════════════════════════════════════════
    // T1: Outbox Write + GetPending + MarkCompleted
    // ═══════════════════════════════════════════

    static async Task T1_OutboxWriteAndPoll(SqlSugarClient client)
    {
        client.Deleteable<EventOutboxMessage>().ExecuteCommand();
        var store = new SqliteEventOutboxStore(client);

        // Write
        await store.WriteAsync("Test:Event", new { Data = "hello" });

        // GetPending
        var pending = await store.GetPendingAsync(10);
        if (pending.Count != 1)
        {
            Fail("T1", $"GetPendingAsync 返回 {pending.Count} 条，预期 1");
            return;
        }
        if (pending[0].EventName != "Test:Event")
        {
            Fail("T1", $"EventName 不匹配: '{pending[0].EventName}'");
            return;
        }
        if (pending[0].Status != OutboxStatus.Pending)
        {
            Fail("T1", $"Status 不是 Pending: {pending[0].Status}");
            return;
        }
        Pass("T1: Outbox 写入 → GetPending 返回 1 条 Pending 消息");
    }

    static async Task T1b_OutboxStatusTransitions(SqlSugarClient client)
    {
        client.Deleteable<EventOutboxMessage>().ExecuteCommand();
        var store = new SqliteEventOutboxStore(client);

        await store.WriteAsync("Test:Transition", new { });
        var pending = await store.GetPendingAsync(10);
        var id = pending[0].Id;

        // Pending → Processing → Completed
        await store.MarkProcessingAsync(id);
        var msg = await client.Queryable<EventOutboxMessage>().InSingleAsync(id);
        if (msg?.Status != OutboxStatus.Processing)
        {
            Fail("T1b", $"MarkProcessing 后状态不是 Processing: {msg?.Status}");
            return;
        }

        await store.MarkCompletedAsync(id);
        msg = await client.Queryable<EventOutboxMessage>().InSingleAsync(id);
        if (msg?.Status != OutboxStatus.Completed)
        {
            Fail("T1b", $"MarkCompleted 后状态不是 Completed: {msg?.Status}");
            return;
        }
        if (msg?.ProcessedAt == null)
        {
            Fail("T1b", "MarkCompleted 后 ProcessedAt 为 null");
            return;
        }

        // 已 Completed 的消息不应出现在 GetPending 中
        var stillPending = await store.GetPendingAsync(10);
        if (stillPending.Count > 0)
        {
            Fail("T1b", $"Completed 消息仍出现在 GetPending 中: {stillPending.Count}");
            return;
        }
        Pass("T1b: Outbox 状态转换 Pending → Processing → Completed");
    }

    static async Task T1c_OutboxStats(SqlSugarClient client)
    {
        client.Deleteable<EventOutboxMessage>().ExecuteCommand();
        var store = new SqliteEventOutboxStore(client);

        // 插入不同状态的消息
        await store.WriteAsync("S1", new { });
        await store.WriteAsync("S2", new { });
        await store.WriteAsync("S3", new { });

        var pending = await store.GetPendingAsync(10);
        await store.MarkFailedAsync(pending[0].Id, "err1");
        // MarkFailed 会检查 RetryCount >= MaxRetryCount，但 RetryCount=0, MaxRetryCount=3，不会变成 DeadLetter
        // 手动设为 DeadLetter
        await store.MarkDeadLetterAsync(pending[2].Id);

        var stats = await store.GetStatsAsync();
        if (stats.PendingCount != 1)
        {
            Fail("T1c", $"PendingCount 预期 1，实际 {stats.PendingCount}");
            return;
        }
        if (stats.FailedCount != 1)
        {
            Fail("T1c", $"FailedCount 预期 1，实际 {stats.FailedCount}");
            return;
        }
        if (stats.DeadLetterCount != 1)
        {
            Fail("T1c", $"DeadLetterCount 预期 1，实际 {stats.DeadLetterCount}");
            return;
        }
        Pass("T1c: Outbox 统计 — Pending=1, Failed=1, DeadLetter=1");
    }

    // ═══════════════════════════════════════════
    // T2: 幂等处理
    // ═══════════════════════════════════════════

    static async Task T2_IdempotentFirstExecution(SqlSugarClient client)
    {
        client.Deleteable<ProcessedEvent>().ExecuteCommand();
        var logger = new ConsoleLogger<IdempotentEventHandler>();
        var handler = new IdempotentEventHandler(client, logger);

        var executed = false;
        // HandlerMethod.Name will be "Main" from the reflection lookup
        var context = CreateContext("idempotent-event-001", "Main");
        var expectedHandlerName = context.HandlerMethod?.Name ?? "Main";

        await handler.ExecuteAsync(context, _ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        if (!executed)
        {
            Fail("T2", "首次执行未调用 handler");
            return;
        }

        // 验证 ProcessedEvent 记录（HandlerName = context.HandlerMethod.Name = "Main"）
        var record = await client.Queryable<ProcessedEvent>()
            .Where(it => it.EventId == "idempotent-event-001" && it.HandlerName == expectedHandlerName)
            .FirstAsync();
        if (record == null)
        {
            Fail("T2", $"ProcessedEvent 记录未写入 (HandlerName='{expectedHandlerName}')");
            return;
        }
        Pass($"T2: 幂等首次执行 → handler 调用 + ProcessedEvent 写入 (HandlerName='{expectedHandlerName}')");
    }

    static async Task T2b_IdempotentSkipDuplicate(SqlSugarClient client)
    {
        client.Deleteable<ProcessedEvent>().ExecuteCommand();
        var logger = new ConsoleLogger<IdempotentEventHandler>();
        var handler = new IdempotentEventHandler(client, logger);

        int executionCount = 0;
        var context = CreateContext("idempotent-event-002", "Main");
        Func<EventHandlerExecutingContext, Task> increment = _ =>
        {
            executionCount++;
            return Task.CompletedTask;
        };

        // 第一次执行
        await handler.ExecuteAsync(context, increment);

        // 第二次执行（同 EventId + HandlerName）
        await handler.ExecuteAsync(context, increment);

        if (executionCount != 1)
        {
            Fail("T2b", $"handler 执行了 {executionCount} 次，预期 1 次（第二次应跳过）");
            return;
        }
        Pass("T2b: 幂等重复投递 → 第二次跳过（handler 仅执行 1 次）");
    }

    // ═══════════════════════════════════════════
    // T3: Polly 重试 + 熔断
    // ═══════════════════════════════════════════

    static async Task T3_PollyRetrySuccess(SqlSugarClient client)
    {
        var logger = new ConsoleLogger<PollyRetryHandlerExecutor>();
        var executor = new PollyRetryHandlerExecutor(logger);

        int attempts = 0;
        var context = CreateContext("polly-ok", "TestHandler");

        await executor.ExecuteAsync(context, _ =>
        {
            attempts++;
            if (attempts < 3)
                throw new Exception($"Simulated failure #{attempts}");
            return Task.CompletedTask;
        });

        if (attempts != 3)
        {
            Fail("T3", $"预期 3 次尝试，实际 {attempts}");
            return;
        }
        Pass("T3: Polly 重试 — 前 2 次失败，第 3 次成功");
    }

    static async Task T3b_PollyRetryExponentialBackoff(SqlSugarClient client)
    {
        var logger = new ConsoleLogger<PollyRetryHandlerExecutor>();
        var executor = new PollyRetryHandlerExecutor(logger);

        var timestamps = new List<DateTime>();
        var context = CreateContext("polly-backoff", "TestHandler");

        try
        {
            await executor.ExecuteAsync(context, _ =>
            {
                timestamps.Add(DateTime.UtcNow);
                throw new Exception("Always fail");
            });
        }
        catch
        {
            // 预期抛出异常（超过 MaxRetries）
        }

        if (timestamps.Count < 3)
        {
            Fail("T3b", $"尝试次数不足: {timestamps.Count}");
            return;
        }

        // 验证退避递增（第 2 次和第 3 次之间的延迟应大于第 1 次和第 2 次之间）
        var delay1 = timestamps[1] - timestamps[0];
        var delay2 = timestamps[2] - timestamps[1];

        // 允许抖动范围：base delay 1s ± 20% vs 2s ± 20%
        // delay1 应约 1s，delay2 应约 2s
        if (delay1.TotalMilliseconds < 500 || delay2.TotalMilliseconds < 1000)
        {
            Fail("T3b", $"退避未递增: delay1={delay1.TotalMilliseconds}ms, delay2={delay2.TotalMilliseconds}ms");
            return;
        }
        Pass($"T3b: Polly 指数退避 — delay1≈{delay1.TotalMilliseconds:F0}ms, delay2≈{delay2.TotalMilliseconds:F0}ms");
    }

    static async Task T3c_PollyCircuitBreaker(SqlSugarClient client)
    {
        // 清除熔断器状态（静态字典）
        var failureField = typeof(PollyRetryHandlerExecutor).GetField("_failureCounts",
            BindingFlags.Static | BindingFlags.NonPublic);
        var breakerField = typeof(PollyRetryHandlerExecutor).GetField("_circuitBreakerUntil",
            BindingFlags.Static | BindingFlags.NonPublic);
        (failureField?.GetValue(null) as ConcurrentDictionary<string, int>)?.Clear();
        (breakerField?.GetValue(null) as ConcurrentDictionary<string, DateTime>)?.Clear();

        var logger = new ConsoleLogger<PollyRetryHandlerExecutor>();
        var executor = new PollyRetryHandlerExecutor(logger);
        var context = CreateContext("polly-circuit", "TestHandler");

        // 触发 10 次失败使熔断器打开
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await executor.ExecuteAsync(context, _ => throw new Exception("fail"));
            }
            catch { }
        }

        // 熔断器应已打开，下一次调用应立即返回（不执行 handler）
        bool handlerCalled = false;
        await executor.ExecuteAsync(context, _ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        if (handlerCalled)
        {
            Fail("T3c", "熔断器打开后 handler 仍被调用");
            return;
        }

        // 清理
        (failureField?.GetValue(null) as ConcurrentDictionary<string, int>)?.Clear();
        (breakerField?.GetValue(null) as ConcurrentDictionary<string, DateTime>)?.Clear();

        Pass("T3c: Polly 熔断 — 10 次失败后熔断器打开，handler 被跳过");
    }

    // ═══════════════════════════════════════════
    // T4: 死信标记
    // ═══════════════════════════════════════════

    static async Task T4_DeadLetterOnMaxRetry(SqlSugarClient client)
    {
        client.Deleteable<EventOutboxMessage>().ExecuteCommand();
        var store = new SqliteEventOutboxStore(client);

        await store.WriteAsync("Test:DeadLetter", new { });
        var pending = await store.GetPendingAsync(10);
        var id = pending[0].Id;

        // 模拟 3 次失败（MaxRetryCount=3）
        for (int i = 0; i < 3; i++)
        {
            await store.IncrementRetryAsync(id);
        }
        await store.MarkFailedAsync(id, "Max retries exceeded");

        var msg = await client.Queryable<EventOutboxMessage>().InSingleAsync(id);
        if (msg?.Status != OutboxStatus.DeadLetter)
        {
            Fail("T4", $"超过最大重试后状态不是 DeadLetter: {msg?.Status}");
            return;
        }

        // 验证死信查询
        var deadLetters = await store.GetDeadLettersAsync();
        if (deadLetters.Count != 1)
        {
            Fail("T4", $"GetDeadLettersAsync 返回 {deadLetters.Count} 条，预期 1");
            return;
        }
        Pass("T4: 超过 MaxRetryCount → 标记 DeadLetter → 可查询");
    }

    static async Task T4b_DeadLetterRetryReset(SqlSugarClient client)
    {
        client.Deleteable<EventOutboxMessage>().ExecuteCommand();
        var store = new SqliteEventOutboxStore(client);

        await store.WriteAsync("Test:RetryReset", new { });
        var pending = await store.GetPendingAsync(10);
        var id = pending[0].Id;

        // 标记死信
        await store.MarkDeadLetterAsync(id);

        // 手动重试
        await store.RetryDeadLetterAsync(id);

        var msg = await client.Queryable<EventOutboxMessage>().InSingleAsync(id);
        if (msg?.Status != OutboxStatus.Pending)
        {
            Fail("T4b", $"重试后状态不是 Pending: {msg?.Status}");
            return;
        }
        if (msg?.RetryCount != 0)
        {
            Fail("T4b", $"重试后 RetryCount 不是 0: {msg?.RetryCount}");
            return;
        }

        // 死信列表应为空
        var deadLetters = await store.GetDeadLettersAsync();
        if (deadLetters.Count != 0)
        {
            Fail("T4b", $"重试后死信列表不为空: {deadLetters.Count}");
            return;
        }
        Pass("T4b: 死信手动重试 → Pending + RetryCount=0");
    }

    // ═══════════════════════════════════════════
    // T5: SIGTERM Channel 排空
    // ═══════════════════════════════════════════

    static async Task T5_ChannelDrainOnShutdown()
    {
        var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        // 写入 10 条
        for (int i = 0; i < 10; i++)
        {
            await channel.Writer.WriteAsync(i);
        }

        // 模拟 SIGTERM：关闭 Writer
        channel.Writer.TryComplete();

        // 排空
        var drained = new List<int>();
        while (channel.Reader.TryRead(out var item))
        {
            drained.Add(item);
        }

        if (drained.Count != 10)
        {
            Fail("T5", $"排空后读取 {drained.Count} 条，预期 10");
            return;
        }

        // 验证顺序
        for (int i = 0; i < 10; i++)
        {
            if (drained[i] != i)
            {
                Fail("T5", $"顺序错误: drained[{i}]={drained[i]}, expected={i}");
                return;
            }
        }
        Pass("T5: Channel 排空 — Writer 完成后读取全部 10 条，顺序正确");
    }

    // ═══════════════════════════════════════════
    // T6: Channel 批量缓冲
    // ═══════════════════════════════════════════

    static async Task T6_ChannelBatchBuffer()
    {
        var channel = Channel.CreateBounded<TestItem>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

        // 写入 250 条
        for (int i = 0; i < 250; i++)
        {
            channel.Writer.TryWrite(new TestItem { Id = i, Group = i % 3 });
        }

        // 模拟批量读取（batch size = 100）
        var batch1 = new List<TestItem>();
        while (batch1.Count < 100 && channel.Reader.TryRead(out var item))
        {
            batch1.Add(item);
        }

        var batch2 = new List<TestItem>();
        while (batch2.Count < 100 && channel.Reader.TryRead(out var item))
        {
            batch2.Add(item);
        }

        var batch3 = new List<TestItem>();
        while (batch3.Count < 100 && channel.Reader.TryRead(out var item))
        {
            batch3.Add(item);
        }

        if (batch1.Count != 100 || batch2.Count != 100 || batch3.Count != 50)
        {
            Fail("T6", $"批量大小不正确: {batch1.Count}, {batch2.Count}, {batch3.Count}");
            return;
        }

        // 验证分组逻辑
        var allItems = batch1.Concat(batch2).Concat(batch3).ToList();
        var groups = allItems.GroupBy(x => x.Group).ToList();
        if (groups.Count != 3)
        {
            Fail("T6", $"分组数不正确: {groups.Count}");
            return;
        }

        // 验证所有 250 条都被读取
        if (allItems.Count != 250)
        {
            Fail("T6", $"总读取数不正确: {allItems.Count}");
            return;
        }

        // 验证 Channel 已空
        if (channel.Reader.TryRead(out _))
        {
            Fail("T6", "Channel 应已空");
            return;
        }
        Pass("T6: Channel 批量缓冲 — 250 条分 3 批读取 (100+100+50)，分组正确");
    }

    // ═══════════════════════════════════════════
    // 辅助方法
    // ═══════════════════════════════════════════

    /// <summary>
    /// 通过反射创建 EventHandlerExecutingContext（构造函数是 internal）.
    /// </summary>
    static EventHandlerExecutingContext CreateContext(string eventId, string handlerName)
    {
        var source = new TestEventSource(eventId);
        var properties = new Dictionary<object, object>();
        var handlerMethod = typeof(Program).GetMethod(nameof(Main), BindingFlags.Static | BindingFlags.NonPublic)
                            ?? typeof(Program).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).First();

        var ctor = typeof(EventHandlerExecutingContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Cannot find EventHandlerExecutingContext constructor");

        return (EventHandlerExecutingContext)ctor.Invoke(new object[] { source, properties, handlerMethod, null! });
    }
}

/// <summary>
/// SQLite 兼容的 Outbox 存储（GetPendingAsync 不使用 UPDLOCK+READPAST）.
/// </summary>
class SqliteEventOutboxStore : SqlSugarEventOutboxStore
{
    private readonly ISqlSugarClient _db;

    public SqliteEventOutboxStore(ISqlSugarClient db) : base(db)
    {
        _db = db;
    }

    public new async Task<IList<EventOutboxMessage>> GetPendingAsync(int batchSize)
    {
        return await _db.Queryable<EventOutboxMessage>()
            .Where(it => (it.Status == OutboxStatus.Pending || it.Status == OutboxStatus.Failed)
                         && it.RetryCount < it.MaxRetryCount)
            .OrderBy(it => it.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }
}

/// <summary>
/// 测试用事件源.
/// </summary>
class TestEventSource : IEventSource
{
    public string EventId { get; }
    public object? Payload => null;
    public CancellationToken CancellationToken => default;
    public DateTime CreatedTime => DateTime.UtcNow;
    public bool IsConsumOnce => false;

    public TestEventSource(string eventId)
    {
        EventId = eventId;
    }
}

/// <summary>
/// 测试用 Channel 条目.
/// </summary>
class TestItem
{
    public int Id { get; set; }
    public int Group { get; set; }
}

/// <summary>
/// 控制台日志实现（无 DI 依赖）.
/// </summary>
class ConsoleLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            Console.WriteLine($"    [{logLevel}] {formatter(state, exception)}");
        }
    }
}
