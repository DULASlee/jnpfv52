using System.Collections.Concurrent;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Llm;

/// <summary>
/// Skill 级 LLM 预算门禁（P3-L01）。所有设计 Skill MUST 经此 Guard 调用 ILlmGatewayService。
/// </summary>
public interface ISkillLlmBudgetGuard
{
    Task ValidateProjectBudgetAsync(string projectId, string tenantId, double reserveRatio = 0.95, CancellationToken ct = default);
    Task<LlmCallSlot> AcquireAsync(string projectId, string skillId, string runId, string tenantId, long pipelineId, CancellationToken ct = default);
    Task<ChatCompletionResponse> ExecuteAsync(LlmCallSlot slot, ChatCompletionRequest request, CancellationToken ct = default);
    void ReleaseRun(string runId, string skillId);
}

public sealed class LlmCallSlot
{
    internal string RunId { get; init; } = string.Empty;
    internal string SkillId { get; init; } = string.Empty;
    internal string ProjectId { get; init; } = string.Empty;
    internal string TenantId { get; init; } = string.Empty;
    internal long PipelineId { get; init; }
    internal LlmCallPolicy Policy { get; init; } = new();
    internal string ProviderCode { get; init; } = string.Empty;
    internal int MaxTokens { get; init; }
    internal int TimeoutMs { get; init; }
}

internal sealed class SkillRunLlmUsage
{
    public int CallCount;
    public long TotalTokens;
}

public sealed class SkillLlmBudgetGuard : ISkillLlmBudgetGuard, ITransient
{
    public const string BudgetExhaustedCode = "LLM_BUDGET_EXHAUSTED";
    public const string CallLimitCode = "LLM_CALL_LIMIT_EXCEEDED";
    public const string SkillTokenLimitCode = "LLM_SKILL_TOKEN_LIMIT";

    private static readonly ConcurrentDictionary<string, SkillRunLlmUsage> RunUsage = new(StringComparer.Ordinal);

    private readonly ISqlSugarClient _db;
    private readonly ILlmGatewayService _gateway;
    private readonly ILlmCallPolicyService _policyService;
    private readonly IConfiguration _configuration;
    private readonly IPipelineSseChannelHub _sseHub;
    private readonly ILogger<SkillLlmBudgetGuard> _logger;

    public SkillLlmBudgetGuard(
        ISqlSugarClient db,
        ILlmGatewayService gateway,
        ILlmCallPolicyService policyService,
        IConfiguration configuration,
        IPipelineSseChannelHub sseHub,
        ILogger<SkillLlmBudgetGuard> logger)
    {
        _db = db;
        _gateway = gateway;
        _policyService = policyService;
        _configuration = configuration;
        _sseHub = sseHub;
        _logger = logger;
    }

    public async Task ValidateProjectBudgetAsync(
        string projectId, string tenantId, double reserveRatio = 0.95, CancellationToken ct = default)
    {
        var project = await LoadProjectAsync(projectId, tenantId, ct);
        var threshold = (long)(project.TokenBudget * reserveRatio);
        if (project.TokenConsumed >= threshold)
            ThrowBudgetExhausted(project, pipelineId: 0, skillId: null, reason: "pre-check");
    }

    public async Task<LlmCallSlot> AcquireAsync(
        string projectId, string skillId, string runId, string tenantId, long pipelineId, CancellationToken ct = default)
    {
        var policy = await _policyService.GetPolicyAsync(skillId, ct);
        if (policy.MaxLlmCalls <= 0)
            ThrowCallRejected(pipelineId, skillId, runId, CallLimitCode,
                $"Skill {skillId} 禁止直连 LLM Gateway（MaxLlmCalls=0）");

        var project = await LoadProjectAsync(projectId, tenantId, ct);
        if (project.TokenConsumed >= project.TokenBudget)
            ThrowBudgetExhausted(project, pipelineId, skillId, runId, "project budget exhausted");

        var usageKey = UsageKey(runId, skillId);
        var usage = RunUsage.GetOrAdd(usageKey, _ => new SkillRunLlmUsage());

        if (usage.CallCount >= policy.MaxLlmCalls)
            ThrowCallRejected(pipelineId, skillId, runId, CallLimitCode,
                $"Skill {skillId} 已达 maxCalls={policy.MaxLlmCalls}");

        if (usage.TotalTokens >= policy.MaxTotalTokens)
            ThrowCallRejected(pipelineId, skillId, runId, SkillTokenLimitCode,
                $"Skill {skillId} 已达 maxTotalTokens={policy.MaxTotalTokens}");

        var provider = ResolveProvider(policy.ModelTier, null);
        return new LlmCallSlot
        {
            RunId = runId,
            SkillId = skillId,
            ProjectId = projectId,
            TenantId = tenantId,
            PipelineId = pipelineId,
            Policy = policy,
            ProviderCode = provider,
            MaxTokens = policy.MaxTokensPerCall,
            TimeoutMs = policy.TimeoutMs,
        };
    }

    public async Task<ChatCompletionResponse> ExecuteAsync(
        LlmCallSlot slot, ChatCompletionRequest request, CancellationToken ct = default)
    {
        var usageKey = UsageKey(slot.RunId, slot.SkillId);
        var usage = RunUsage.GetOrAdd(usageKey, _ => new SkillRunLlmUsage());

        _logger.LogInformation(
            "LlmCallStart RunId={RunId} SkillId={SkillId} ProjectId={ProjectId} Call={Call}",
            slot.RunId, slot.SkillId, slot.ProjectId, usage.CallCount + 1);

        var adjusted = request with
        {
            ProviderCode = string.IsNullOrEmpty(request.ProviderCode) ? slot.ProviderCode : request.ProviderCode,
            MaxTokens = request.MaxTokens > 0
                ? Math.Min(request.MaxTokens, slot.MaxTokens)
                : slot.MaxTokens,
            TimeoutMs = request.TimeoutMs > 0
                ? Math.Min(request.TimeoutMs, slot.TimeoutMs)
                : slot.TimeoutMs,
        };

        ChatCompletionResponse response;
        using (LlmCallAuditContext.Begin(slot.RunId, slot.SkillId, slot.ProjectId, slot.TenantId))
        {
            response = await _gateway.ChatAsync(adjusted, ct);
        }

        usage.CallCount++;
        var tokensUsed = Math.Max(0, response.TokensIn) + Math.Max(0, response.TokensOut);
        usage.TotalTokens += tokensUsed;

        if (response.IsSuccess && tokensUsed > 0)
            await AccumulateProjectTokensAsync(slot.ProjectId, slot.TenantId, tokensUsed, ct);

        _logger.LogInformation(
            "LlmCallComplete RunId={RunId} SkillId={SkillId} Tokens={Tokens} Success={Success}",
            slot.RunId, slot.SkillId, tokensUsed, response.IsSuccess);

        return response;
    }

    public void ReleaseRun(string runId, string skillId)
        => RunUsage.TryRemove(UsageKey(runId, skillId), out _);

    private async Task<AiProjectEntity> LoadProjectAsync(string projectId, string tenantId, CancellationToken ct)
    {
        var project = await _db.Queryable<AiProjectEntity>()
            .FirstAsync(x => x.Id == projectId && x.TenantId == tenantId && !x.DeleteMark, ct);

        if (project == null)
            throw Oops.Oh($"项目不存在: {projectId}");

        return project;
    }

    private async Task AccumulateProjectTokensAsync(string projectId, string tenantId, long delta, CancellationToken ct)
    {
        var project = await LoadProjectAsync(projectId, tenantId, ct);
        var newConsumed = project.TokenConsumed + delta;
        var budgetStatus = newConsumed >= project.TokenBudget
            ? "exhausted"
            : newConsumed >= (long)(project.TokenBudget * 0.95)
                ? "yellow"
                : project.LlmBudgetStatus;

        await _db.Updateable<AiProjectEntity>()
            .SetColumns(x => new AiProjectEntity
            {
                TokenConsumed = x.TokenConsumed + delta,
                LlmBudgetStatus = budgetStatus,
                LastModifyTime = DateTime.UtcNow,
            })
            .Where(x => x.Id == projectId && x.TenantId == tenantId)
            .ExecuteCommandAsync(ct);
    }

    private string ResolveProvider(string modelTier, string? overrideProvider)
    {
        if (!string.IsNullOrWhiteSpace(overrideProvider))
            return overrideProvider;

        if (string.Equals(modelTier, "fast", StringComparison.OrdinalIgnoreCase))
            return _configuration.GetValue("LlmRouting:FastProvider", "mimo")!;

        var strong = _configuration.GetValue<string>("LlmRouting:StrongProvider");
        return string.IsNullOrWhiteSpace(strong) ? string.Empty : strong;
    }

    private void ThrowBudgetExhausted(
        AiProjectEntity project, long pipelineId, string? skillId, string? runId = null, string? reason = null)
    {
        PushSseError(pipelineId, skillId, runId, BudgetExhaustedCode, reason ?? BudgetExhaustedCode);
        throw Oops.Oh($"LLM 预算已耗尽: consumed={project.TokenConsumed}, budget={project.TokenBudget}")
            .StatusCode(StatusCodes.Status429TooManyRequests)
            .WithData(new
            {
                code = BudgetExhaustedCode,
                tokenConsumed = project.TokenConsumed,
                tokenBudget = project.TokenBudget,
            });
    }

    private void ThrowCallRejected(long pipelineId, string skillId, string runId, string code, string message)
    {
        PushSseError(pipelineId, skillId, runId, code, message);
        throw Oops.Oh(message)
            .StatusCode(StatusCodes.Status429TooManyRequests)
            .WithData(new { code, skillId, runId });
    }

    private void PushSseError(long pipelineId, string? skillId, string? runId, string code, string message)
    {
        if (pipelineId <= 0) return;

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            skillId,
            runId,
            code,
            message,
        });
        _sseHub.TryPush(pipelineId, SseEventType.SkillProgress, payload);
    }

    private static string UsageKey(string runId, string skillId) => $"{runId}:{skillId}";
}
