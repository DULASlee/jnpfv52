using JNPF.API.Entry.Entities;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.API.Entry.Services;

/// <summary>
/// AI 调用审计日志服务。
/// 提供日志写入、分页查询、使用统计等 API。
/// </summary>
[ApiDescriptionSettings(Tag = "AI", Name = "AiCallLog")]
[AllowAnonymous]
public class AiCallLogService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;

    public AiCallLogService(ISqlSugarClient db)
    {
        _db = db;
    }

    // ============================================================
    // 写入
    // ============================================================

    /// <summary>
    /// 写入一条 AI 调用日志。
    /// </summary>
    [HttpPost("/api/ai/call-logs")]
    public async Task CreateLog([FromBody] CreateAiCallLogInput input, CancellationToken cancellationToken = default)
    {
        var entity = new AiCallLogEntity
        {
            Id = SnowFlakeSingle.Instance.NextId(),
            TenantId = input.TenantId ?? string.Empty,
            Provider = input.Provider,
            Model = input.Model,
            PromptTokens = input.PromptTokens,
            CompletionTokens = input.CompletionTokens,
            TotalTokens = input.TotalTokens,
            LatencyMs = input.LatencyMs,
            Success = input.Success,
            ErrorMessage = input.ErrorMessage,
            RequestSummary = Truncate(input.RequestSummary, 200),
            ResponseSummary = Truncate(input.ResponseSummary, 200),
            CreateTime = DateTime.UtcNow,
            CreateUserId = input.UserId,
        };

        await _db.Insertable(entity).ExecuteCommandAsync();
    }

    // ============================================================
    // 查询
    // ============================================================

    /// <summary>
    /// 分页查询 AI 调用日志。
    /// </summary>
    [HttpGet("/api/ai/call-logs")]
    public async Task<dynamic> GetList(
        [FromQuery] string? provider = null,
        [FromQuery] bool? success = null,
        [FromQuery] int currentPage = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<AiCallLogEntity>()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrEmpty(provider))
        {
            query = query.Where(x => x.Provider == provider);
        }

        if (success.HasValue)
        {
            query = query.Where(x => x.Success == success.Value);
        }

        var result = await query
            .OrderBy(x => x.CreateTime, OrderByType.Desc)
            .ToPagedListAsync(currentPage, pageSize);

        var list = result.list.Select(x => new AiCallLogOutput
        {
            Id = x.Id,
            Provider = x.Provider,
            Model = x.Model,
            PromptTokens = x.PromptTokens,
            CompletionTokens = x.CompletionTokens,
            TotalTokens = x.TotalTokens,
            LatencyMs = x.LatencyMs,
            Success = x.Success,
            ErrorMessage = x.ErrorMessage,
            RequestSummary = x.RequestSummary,
            ResponseSummary = x.ResponseSummary,
            CreateTime = x.CreateTime,
        }).ToList();

        return new { list, pagination = new { total = result.pagination.Total } };
    }

    // ============================================================
    // 统计
    // ============================================================

    /// <summary>
    /// AI 调用使用统计。
    /// </summary>
    [HttpGet("/api/ai/call-logs/stats")]
    public async Task<dynamic> GetStats(CancellationToken cancellationToken = default)
    {
        var entities = await _db.Queryable<AiCallLogEntity>()
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var byProvider = entities
            .GroupBy(x => x.Provider)
            .Select(g => new
            {
                provider = g.Key,
                totalCalls = g.Count(),
                successCalls = g.Count(x => x.Success),
                failureCalls = g.Count(x => !x.Success),
                totalPromptTokens = g.Sum(x => x.PromptTokens),
                totalCompletionTokens = g.Sum(x => x.CompletionTokens),
                totalTokens = g.Sum(x => x.TotalTokens),
                averageLatency = g.Count() > 0 ? g.Average(x => x.LatencyMs) : 0,
            })
            .ToList();

        return new
        {
            totalCalls = entities.Count,
            totalSuccess = entities.Count(x => x.Success),
            totalFailure = entities.Count(x => !x.Success),
            totalPromptTokens = entities.Sum(x => x.PromptTokens),
            totalCompletionTokens = entities.Sum(x => x.CompletionTokens),
            totalTokens = entities.Sum(x => x.TotalTokens),
            averageLatency = entities.Count > 0 ? entities.Average(x => x.LatencyMs) : 0,
            byProvider,
        };
    }

    // ============================================================
    // 辅助
    // ============================================================

    private static string? Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}

/// <summary>
/// AI 调用日志写入输入。
/// </summary>
public class CreateAiCallLogInput
{
    public string? TenantId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public long LatencyMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RequestSummary { get; set; }
    public string? ResponseSummary { get; set; }
    public string? UserId { get; set; }
}

/// <summary>
/// AI 调用日志列表输出。
/// </summary>
public class AiCallLogOutput
{
    public long Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public long LatencyMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RequestSummary { get; set; }
    public string? ResponseSummary { get; set; }
    public DateTime CreateTime { get; set; }
}
