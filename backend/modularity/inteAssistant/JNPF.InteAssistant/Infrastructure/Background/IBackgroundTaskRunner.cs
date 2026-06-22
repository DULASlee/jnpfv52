// 文件：Infrastructure/Background/IBackgroundTaskRunner.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Background
// 职责：后台任务执行器接口——任何模块都可以注入使用

namespace JNPF.InteAssistant.Infrastructure.Background;

/// <summary>
/// 后台任务执行器接口
///
/// 用法：
///   _taskRunner.Run("任务名", async (ctx, ct) => { ... });
///
/// 架构约束：
///   1. 自动捕获 HTTP 上下文到 RequestContext（后台线程无 HttpContext）
///   2. 自动管理 CancellationTokenSource 生命周期
///   3. 自动追踪活跃任务（支持优雅关闭）
/// </summary>
public interface IBackgroundTaskRunner
{
    /// <summary>
    /// 启动后台任务
    /// </summary>
    /// <param name="taskName">任务唯一名称（相同名称的任务不会重复启动）</param>
    /// <param name="action">任务逻辑（接收已捕获的请求上下文和取消令牌）</param>
    /// <param name="requestCt">请求级取消令牌（用户断开连接时触发）</param>
    /// <param name="timeout">超时时间（默认10分钟）</param>
    void Run(
        string taskName,
        Func<RequestContext, CancellationToken, Task> action,
        CancellationToken requestCt = default,
        TimeSpan? timeout = null);

    /// <summary>获取所有活跃任务</summary>
    IReadOnlyDictionary<string, TaskState> GetAllActive();

    /// <summary>取消指定任务</summary>
    void CancelTask(string taskName);
}
