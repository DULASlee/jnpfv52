using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Entitys.Entity;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// AI 用量统计服务 (Sprint 3 - S3-2)
/// 聚合查询 AI 调用日志，提供 summary 和 call-log 接口
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioUsage", Order = 195)]
[Route("api/studio/ai/usage")]
public class StudioUsageService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;

    public StudioUsageService(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 用量汇总
    /// </summary>
    [HttpGet("summary")]
    public async Task<object> GetSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = (endDate ?? DateTime.Today).AddDays(1);

        var dt = await _db.Ado.GetDataTableAsync(
            @"SELECT F_PROVIDER AS Provider, F_MODEL AS Model,
                     SUM(F_PROMPT_TOKENS) AS PromptTokens,
                     SUM(F_COMPLETION_TOKENS) AS CompletionTokens,
                     AVG(F_LATENCY_MS) AS AvgLatencyMs,
                     COUNT(*) AS CallCount
              FROM BASE_AI_CALL_LOG
              WHERE F_CREATOR_TIME >= @start AND F_CREATOR_TIME < @end
              GROUP BY F_PROVIDER, F_MODEL
              ORDER BY F_PROVIDER",
            new SugarParameter("@start", start),
            new SugarParameter("@end", end));

        long totalTokens = 0;
        long totalCalls = 0;
        double totalLatency = 0;
        var providers = new Dictionary<string, object>();

        foreach (System.Data.DataRow row in dt.Rows)
        {
            var provider = row["Provider"]?.ToString() ?? "";
            var pTokens = Convert.ToInt64(row["PromptTokens"] ?? 0);
            var cTokens = Convert.ToInt64(row["CompletionTokens"] ?? 0);
            var calls = Convert.ToInt32(row["CallCount"] ?? 0);
            var avgLat = Convert.ToDouble(row["AvgLatencyMs"] ?? 0);

            totalTokens += pTokens + cTokens;
            totalCalls += calls;
            totalLatency += avgLat * calls;

            if (!providers.ContainsKey(provider))
                providers[provider] = new { provider, tokens = 0L, cost = 0m, count = 0 };
        }

        return new
        {
            totalTokens,
            totalCalls,
            totalCost = totalTokens / 1_000_000m * 0.002m, // $0.002/1K tokens
            avgLatency = totalCalls > 0 ? (int)(totalLatency / totalCalls) : 0,
            providers = providers.Values,
            period = new { start, end },
        };
    }

    /// <summary>
    /// 调用明细分页查询
    /// </summary>
    [HttpGet("call-log")]
    public async Task<object> GetCallLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = (endDate ?? DateTime.Today).AddDays(1);

        var query = _db.Queryable<AiCallLogEntity>()
            .Where(x => x.CreatorTime >= start && x.CreatorTime < end)
            .OrderByDescending(x => x.CreatorTime)
            .Select(x => new
            {
                x.Id,
                Provider = (string?)null,
                Model = (string?)null,
                Stage = (int?)null,
                PromptTokens = x.PromptTokens ?? 0,
                CompletionTokens = x.CompletionTokens ?? 0,
                Latency = (int)(x.LatencyMs ?? 0),
                Status = (x.StatusCode == 200 ? "success" : "failed"),
                EstimatedCost = (x.PromptTokens + x.CompletionTokens ?? 0) / 1_000_000m * 0.002m,
                CreateTime = x.CreatorTime,
            });

        RefAsync<int> total = 0;
        var items = await query.ToPageListAsync(page, pageSize, total);

        return new { total = total.Value, items };
    }
}
