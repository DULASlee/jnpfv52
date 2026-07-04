// 文件：Infrastructure/Background/RequestContext.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Background
// 职责：HTTP 请求上下文快照——主线程捕获，后台线程只读

using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using JNPF.Common.Core.MultiTenancy;

namespace JNPF.InteAssistant.Infrastructure.Background;

/// <summary>
/// HTTP 请求上下文快照
///
/// 生命周期：主线程创建 → 后台线程只读 → 任务完成后 GC 回收
///
/// 设计决策：
///   - sealed class + init 属性（不用 record，因为 DownloadCache 是引用类型）
///   - 所有属性 init-only，创建后不可变（除了内部缓存）
/// </summary>
public sealed class RequestContext
{
    public string Scheme { get; init; } = "";
    public string Host { get; init; } = "";
    public string TenantId { get; init; } = "";
    public string UserId { get; init; } = "";
    public string UserName { get; init; } = "";

    /// <summary>原请求 Authorization，供后台任务下载 annex 附件</summary>
    public string Authorization { get; init; } = "";

    /// <summary>
    /// 步骤间共享的下载缓存（internal，仅供 Infrastructure 和 Gates 内部使用）
    /// key: 文件URL, value: 文件内容
    /// </summary>
    internal ConcurrentDictionary<string, byte[]> DownloadCache { get; } = new();

    /// <summary>附件文本缓存（避免重复提取）</summary>
    internal string? ExtractedAttachmentText { get; set; }

    /// <summary>获取完整基础 URL（后台自调用时 0.0.0.0 不可达，归一为 localhost）</summary>
    public string GetBaseUrl()
    {
        if (string.IsNullOrEmpty(Scheme) || string.IsNullOrEmpty(Host))
            return "";
        var host = Host.StartsWith("0.0.0.0", StringComparison.OrdinalIgnoreCase)
            ? Host.Replace("0.0.0.0", "localhost", StringComparison.OrdinalIgnoreCase)
            : Host;
        return $"{Scheme}://{host}";
    }

    /// <summary>
    /// 从 IHttpContextAccessor 捕获当前请求上下文
    /// 必须在主线程调用（HttpContext 存在时）
    /// </summary>
    public static RequestContext Capture(IHttpContextAccessor accessor)
    {
        var http = accessor?.HttpContext;
        var authorization = http?.Request?.Headers["Authorization"].FirstOrDefault() ?? "";
        if (string.IsNullOrWhiteSpace(authorization))
        {
            var queryToken = http?.Request?.Query["token"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(queryToken))
            {
                authorization = queryToken.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase)
                    ? queryToken
                    : $"Bearer {queryToken}";
            }
        }

        return new RequestContext
        {
            Scheme = http?.Request?.Scheme ?? "",
            Host = http?.Request?.Host.ToString() ?? "",
            TenantId = ResolveTenantId(accessor),
            UserId = ResolveClaim(accessor, "UserId"),
            UserName = ResolveClaim(accessor, "UserName"),
            Authorization = authorization,
        };
    }

    /// <summary>
    /// 从 JWT Claims 安全提取值
    /// 仅捕获 HttpContext == null 的场景，不吞 OOM 等致命异常
    /// </summary>
    private static string ResolveTenantId(IHttpContextAccessor accessor)
    {
        var claim = ResolveClaim(accessor, "TenantId");
        if (!string.IsNullOrWhiteSpace(claim))
            return claim;

        var resolved = TenantResolver.Resolve();
        return resolved >= 0 ? resolved.ToString() : "";
    }

    private static string ResolveClaim(IHttpContextAccessor accessor, string claimType)
    {
        try
        {
            return accessor?.HttpContext?.User?.FindFirst(claimType)?.Value ?? "";
        }
        catch (NullReferenceException)
        {
            // HttpContext 为 null（非 HTTP 入口，正常场景）
            return "";
        }
    }
}
