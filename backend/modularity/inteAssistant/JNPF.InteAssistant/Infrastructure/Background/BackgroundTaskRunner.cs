// 文件：Infrastructure/Background/BackgroundTaskRunner.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Background
// 职责：后台任务执行器实现

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace JNPF.InteAssistant.Infrastructure.Background;

/// <summary>
/// 后台任务执行器
///
/// 架构约束（写代码前必须理解）：
///   1. TryAdd 原子占位——消除 TOCTOU 竞态
///   2. 不把 token 传给 Task.Run——防止调度前取消导致 finally 不执行
///   3. CTS 在 finally 中手动 Dispose——不用 using（方法返回后 using 会提前回收）
///   4. 异常过滤器——OOM/StackOverflow 不进入 catch 块（catch 内做日志会二次 OOM）
/// </summary>
public sealed class BackgroundTaskRunner : IBackgroundTaskRunner, IDisposable
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<BackgroundTaskRunner> _logger;
    private readonly ConcurrentDictionary<string, TaskState> _activeTasks = new();

    public BackgroundTaskRunner(IHttpContextAccessor accessor, ILogger<BackgroundTaskRunner> logger)
    {
        _accessor = accessor;
        _logger = logger;
    }

    public void Run(
        string taskName,
        Func<RequestContext, CancellationToken, Task> action,
        CancellationToken requestCt = default,
        TimeSpan? timeout = null)
    {
        var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(10));
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, requestCt);

        var state = new TaskState { Cts = cts, LinkedCts = linkedCts };

        // 原子占位——TryAdd 保证并发安全，失败立即释放资源
        if (!_activeTasks.TryAdd(taskName, state))
        {
            _logger.LogWarning("后台任务已存在: {Name}", taskName);
            linkedCts.Dispose();
            cts.Dispose();
            return;
        }

        // 主线程捕获上下文（此时 HttpContext 还活着）
        var context = RequestContext.Capture(_accessor);

        // 不传 token 给 Task.Run——调度前取消不会阻止 finally 执行
        var task = Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("后台任务开始: {Name}, TenantId={TenantId}", taskName, context.TenantId);
                await action(context, linkedCts.Token);
                _logger.LogInformation("后台任务完成: {Name}, {Ms}ms", taskName, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                _logger.LogInformation("后台任务超时取消: {Name}, {Ms}ms", taskName, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("后台任务外部取消: {Name}, {Ms}ms", taskName, sw.ElapsedMilliseconds);
            }
            // OOM/StackOverflow/AccessViolation 不进入 catch——catch 内做日志会二次 OOM
            catch (Exception ex) when (ex is not OutOfMemoryException
                                    && ex is not StackOverflowException
                                    && ex is not AccessViolationException)
            {
                _logger.LogError(ex, "后台任务异常: {Name}, {Ms}ms", taskName, sw.ElapsedMilliseconds);
            }
            finally
            {
                _activeTasks.TryRemove(taskName, out _);
                linkedCts.Dispose();  // 释放链接关系
                cts.Dispose();        // 释放源定时器
            }
        });  // 注意：不传 linkedCts.Token

        // 防止未观察到的 Task 异常
        task.ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                _logger.LogError(t.Exception, "后台任务未处理异常: {Name}", taskName);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public IReadOnlyDictionary<string, TaskState> GetAllActive() => _activeTasks;

    public void CancelTask(string taskName)
    {
        if (_activeTasks.TryGetValue(taskName, out var state))
            state.LinkedCts.Cancel();
    }

    public void Dispose()
    {
        foreach (var kvp in _activeTasks)
        {
            try { kvp.Value.LinkedCts.Cancel(); } catch { /* 已释放 */ }
        }
    }
}
