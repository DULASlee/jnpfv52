using System.Collections.Concurrent;
using System.Diagnostics;

namespace JNPF.Tests.Phase6;

/// <summary>
/// Phase 6 性能基线测试 — 补 Day 31.
/// 测量沙箱并发退化 + 图遍历复杂度，不优化，只知天花板.
/// </summary>
public static class PerformanceBaselineTests
{
    static int _passed;
    static int _failed;

    public static async Task<int> RunAll()
    {
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("  Phase 6 — Performance Baseline Tests");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();

        try
        {
            await P1_SandboxConcurrency_5Concurrent_Baseline();
            await P2_SandboxConcurrency_50Concurrent_Degradation();
            await P3_GraphBfsTraversal_100Nodes();
            await P4_GraphBfsTraversal_1000Nodes();
            await P5_KnowledgePatchSigning_1KBNodes();
            await P6_ConcurrentGraphWrites_Contention();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL] {ex.Message}");
            _failed++;
        }

        Console.WriteLine();
        Console.WriteLine($"  性能基线结果: {_passed} 通过, {_failed} 失败");
        return _failed > 0 ? 1 : 0;
    }

    /// <summary>
    /// P1: 5 并发 SemaphoreSlim — 基线 (门禁标准: 无退化).
    /// </summary>
    static async Task P1_SandboxConcurrency_5Concurrent_Baseline()
    {
        var semaphore = new SemaphoreSlim(5, 5);
        var sw = Stopwatch.StartNew();
        int maxConcurrent = 0, current = 0;

        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            await semaphore.WaitAsync();
            try
            {
                var c = Interlocked.Increment(ref current);
                int seen;
                do { seen = maxConcurrent; }
                while (Interlocked.CompareExchange(ref maxConcurrent, Math.Max(seen, c), seen) != seen);

                await Task.Delay(20); // 模拟 Docker 调用
                Interlocked.Decrement(ref current);
            }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        if (maxConcurrent > 5)
        { Fail("P1", $"最大并发 {maxConcurrent} > 5"); return; }
        if (sw.ElapsedMilliseconds > 1000)
        { Fail("P1", $"5 并发耗时 {sw.ElapsedMilliseconds}ms > 1000ms"); return; }

        Pass($"P1: 5 并发 SemaphoreSlim — 最大并发 {maxConcurrent}, 耗时 {sw.ElapsedMilliseconds}ms (基线)");
    }

    /// <summary>
    /// P2: 50 并发 SemaphoreSlim — 退化曲线 (接受排队，不崩溃).
    /// </summary>
    static async Task P2_SandboxConcurrency_50Concurrent_Degradation()
    {
        var semaphore = new SemaphoreSlim(5, 5);
        var sw = Stopwatch.StartNew();
        int completed = 0, errors = 0;

        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            try
            {
                await semaphore.WaitAsync();
                await Task.Delay(10);
                Interlocked.Increment(ref completed);
            }
            catch { Interlocked.Increment(ref errors); }
            finally
            {
                try { semaphore.Release(); } catch { /* already disposed */ }
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        if (errors > 0)
        { Fail("P2", $"50 并发中出现 {errors} 个异常"); return; }
        if (completed != 50)
        { Fail("P2", $"仅完成 {completed}/50"); return; }

        // 5 并发 × 10ms × 10 批 = ~100ms 理论，实际排队会更高
        // 门禁: < 5s (允许排队但不允许雪崩)
        if (sw.ElapsedMilliseconds > 5000)
        { Fail("P2", $"50 并发耗时 {sw.ElapsedMilliseconds}ms > 5000ms (雪崩风险)"); return; }

        Pass($"P2: 50 并发 SemaphoreSlim — {completed}/50 完成, 0 错误, 耗时 {sw.ElapsedMilliseconds}ms (退化可接受)");
    }

    /// <summary>
    /// P3: BFS 图遍历 100 节点 — 基线.
    /// </summary>
    static Task P3_GraphBfsTraversal_100Nodes()
    {
        // 生成 100 节点的随机图，模拟 BFS 遍历
        var nodes = Enumerable.Range(0, 100).Select(i => $"node-{i}").ToHashSet();
        var edges = new List<(string, string)>();
        var random = new Random(42);
        for (int i = 0; i < 99; i++)
            edges.Add(($"node-{i}", $"node-{i + 1}"));
        // 加一些随机边
        for (int i = 0; i < 50; i++)
            edges.Add(($"node-{random.Next(100)}", $"node-{random.Next(100)}"));

        var sw = Stopwatch.StartNew();

        // BFS depth=3 模拟
        var visited = new HashSet<string> { "node-0" };
        var frontier = new List<string> { "node-0" };
        for (int d = 0; d < 3; d++)
        {
            var next = new List<string>();
            foreach (var src in frontier)
            {
                var neighbors = edges
                    .Where(e => e.Item1 == src || e.Item2 == src)
                    .Select(e => e.Item1 == src ? e.Item2 : e.Item1)
                    .Where(n => !visited.Contains(n))
                    .Distinct();
                foreach (var n in neighbors)
                {
                    visited.Add(n);
                    next.Add(n);
                }
            }
            frontier = next;
        }
        sw.Stop();

        if (visited.Count < 3)
        { Fail("P3", $"BFS 仅访问 {visited.Count} 节点 (预期 >= 3)"); return Task.CompletedTask; }
        if (sw.ElapsedMilliseconds > 100)
        { Fail("P3", $"100 节点 BFS 耗时 {sw.ElapsedMilliseconds}ms > 100ms"); return Task.CompletedTask; }

        Pass($"P3: BFS 100 节点 depth=3 — 访问 {visited.Count} 节点, 耗时 {sw.Elapsed.TotalMilliseconds:F2}ms (基线)");
        return Task.CompletedTask;
    }

    /// <summary>
    /// P4: BFS 图遍历 1000 节点 — 复杂度验证.
    /// </summary>
    static Task P4_GraphBfsTraversal_1000Nodes()
    {
        var edges = new List<(string, string)>();
        var random = new Random(42);
        for (int i = 0; i < 999; i++)
            edges.Add(($"node-{i}", $"node-{i + 1}"));
        for (int i = 0; i < 200; i++)
            edges.Add(($"node-{random.Next(1000)}", $"node-{random.Next(1000)}"));

        var sw = Stopwatch.StartNew();
        var visited = new HashSet<string> { "node-0" };
        var frontier = new List<string> { "node-0" };
        for (int d = 0; d < 3; d++)
        {
            var next = new List<string>();
            foreach (var src in frontier)
            {
                foreach (var (s, t) in edges)
                {
                    if (s == src && visited.Add(t)) next.Add(t);
                    else if (t == src && visited.Add(s)) next.Add(s);
                }
            }
            frontier = next;
        }
        sw.Stop();

        if (sw.ElapsedMilliseconds > 5000)
        { Fail("P4", $"1000 节点 BFS 耗时 {sw.ElapsedMilliseconds}ms > 5000ms (需索引优化)"); return Task.CompletedTask; }

        Pass($"P4: BFS 1000 节点 depth=3 — 访问 {visited.Count} 节点, 耗时 {sw.ElapsedMilliseconds}ms (天花板)");
        return Task.CompletedTask;
    }

    /// <summary>
    /// P5: KnowledgePatch SHA256+HMAC 签名 — 1KB~64KB 内容.
    /// </summary>
    static Task P5_KnowledgePatchSigning_1KBNodes()
    {
        // 构造 ~64KB 的知识内容 (约 500 节点)
        var nodes = new List<object>();
        for (int i = 0; i < 500; i++)
            nodes.Add(new { label = "entity", name = $"Node-{i:D5}", properties = "{}" });
        var content = System.Text.Json.JsonSerializer.Serialize(new { nodes, edges = new object[0] });

        var sw = Stopwatch.StartNew();

        // SHA256
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(content));

        // HMAC-SHA256
        var key = System.Text.Encoding.UTF8.GetBytes("test-key-32-bytes-long-minimum!");
        var sig = System.Security.Cryptography.HMACSHA256.HashData(key, hash);

        sw.Stop();

        var contentSize = System.Text.Encoding.UTF8.GetByteCount(content);
        if (sw.ElapsedMilliseconds > 50)
        { Fail("P5", $"{contentSize / 1024}KB 签耗时 {sw.ElapsedMilliseconds}ms > 50ms"); return Task.CompletedTask; }

        Pass($"P5: KnowledgePatch 签名 {contentSize / 1024}KB — SHA256+HMAC 耗时 {sw.Elapsed.TotalMilliseconds:F2}ms");
        return Task.CompletedTask;
    }

    /// <summary>
    /// P6: 并发写入知识图谱 — 锁争用测量.
    /// </summary>
    static async Task P6_ConcurrentGraphWrites_Contention()
    {
        var dict = new ConcurrentDictionary<string, int>();
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            await Task.Yield(); // 模拟异步 I/O
            dict.AddOrUpdate($"key-{i % 10}", 1, (_, v) => v + 1);
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        var totalWrites = dict.Values.Sum();
        if (totalWrites != 100)
        { Fail("P6", $"总写入 {totalWrites} != 100 (数据丢失)"); return; }
        if (sw.ElapsedMilliseconds > 500)
        { Fail("P6", $"100 并发写入耗时 {sw.ElapsedMilliseconds}ms > 500ms"); return; }

        Pass($"P6: 100 并发写 ConcurrentDictionary — {dict.Count} 唯一键, {totalWrites} 总写入, 耗时 {sw.ElapsedMilliseconds}ms");
    }

    static void Pass(string n) { Console.WriteLine($"  [PASS] {n}"); _passed++; }
    static void Fail(string n, string r) { Console.WriteLine($"  [FAIL] {n}: {r}"); _failed++; }
}
