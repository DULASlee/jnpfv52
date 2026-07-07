using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using Microsoft.Extensions.Caching.Memory;
using SqlSugar;

namespace JNPF.InteAssistant.Llm;

/// <summary>
/// Skill 级 LLM 调用策略（P3-L02）。DB 优先，缺失时回退内置默认值。
/// </summary>
public interface ILlmCallPolicyService
{
    Task<LlmCallPolicy> GetPolicyAsync(string skillId, CancellationToken ct = default);
}

public sealed class LlmCallPolicy
{
    public string SkillId { get; init; } = string.Empty;
    public int MaxLlmCalls { get; init; } = 3;
    public int MaxTokensPerCall { get; init; } = 8192;
    public int MaxTotalTokens { get; init; } = 50_000;
    public string ModelTier { get; init; } = "strong";
    public int TimeoutMs { get; init; } = 120_000;
}

public sealed class LlmCallPolicyService : ILlmCallPolicyService, ITransient
{
    private static readonly Dictionary<string, LlmCallPolicy> Defaults = new(StringComparer.Ordinal)
    {
        [DesignSkillIds.Architect] = new() { SkillId = DesignSkillIds.Architect, MaxLlmCalls = 3, MaxTokensPerCall = 8192, MaxTotalTokens = 80_000, ModelTier = "strong" },
        [DesignSkillIds.DbDesign] = new() { SkillId = DesignSkillIds.DbDesign, MaxLlmCalls = 2, MaxTokensPerCall = 8192, MaxTotalTokens = 60_000, ModelTier = "strong" },
        [DesignSkillIds.UiDesign] = new() { SkillId = DesignSkillIds.UiDesign, MaxLlmCalls = 2, MaxTokensPerCall = 4096, MaxTotalTokens = 40_000, ModelTier = "strong" },
        [DesignSkillIds.SystemDesign] = new() { SkillId = DesignSkillIds.SystemDesign, MaxLlmCalls = 1, MaxTokensPerCall = 4096, MaxTotalTokens = 20_000, ModelTier = "strong" },
        ["pm-skill"] = new() { SkillId = "pm-skill", MaxLlmCalls = 3, MaxTokensPerCall = 8192, MaxTotalTokens = 40_000, ModelTier = "strong" },
        ["analyst-skill"] = new() { SkillId = "analyst-skill", MaxLlmCalls = 0, MaxTokensPerCall = 0, MaxTotalTokens = 0, ModelTier = "strong", TimeoutMs = 0 },
        [DeploySkillIds.Deploy] = new() { SkillId = DeploySkillIds.Deploy, MaxLlmCalls = 0, MaxTokensPerCall = 0, MaxTotalTokens = 0, ModelTier = "fast", TimeoutMs = 0 },
    };

    private readonly ISqlSugarClient _db;
    private readonly IMemoryCache _cache;

    public LlmCallPolicyService(ISqlSugarClient db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<LlmCallPolicy> GetPolicyAsync(string skillId, CancellationToken ct = default)
    {
        var cacheKey = $"llm-policy:{skillId}";
        if (_cache.TryGetValue(cacheKey, out LlmCallPolicy? cached) && cached != null)
            return cached;

        AiSkillLlmPolicyEntity? row = null;
        try
        {
            row = await _db.Queryable<AiSkillLlmPolicyEntity>()
                .FirstAsync(x => x.SkillId == skillId, ct);
        }
        catch
        {
            // 表未迁移时回退默认值
        }

        var policy = row == null
            ? Defaults.GetValueOrDefault(skillId) ?? new LlmCallPolicy { SkillId = skillId }
            : new LlmCallPolicy
            {
                SkillId = row.SkillId,
                MaxLlmCalls = row.MaxLlmCalls,
                MaxTokensPerCall = row.MaxTokensPerCall,
                MaxTotalTokens = row.MaxTotalTokens,
                ModelTier = row.ModelTier,
                TimeoutMs = row.TimeoutMs,
            };

        _cache.Set(cacheKey, policy, TimeSpan.FromMinutes(5));
        return policy;
    }
}
