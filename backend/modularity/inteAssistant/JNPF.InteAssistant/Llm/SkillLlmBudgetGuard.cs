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

    /// <summary>
    /// 累加项目 Token 消耗(供流式 LLM 路径在调用完成后记账)。
    /// 非流式路径由 ExecuteAsync 内部调用,流式路径需外部显式调用。
    /// </summary>
    Task AccumulateProjectTokensAsync(string projectId, string tenantId, long delta, CancellationToken ct = default);
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
        // P6-L01 四级 tier：fuse 拒绝；red 仅 warn 预警（实际硬熔断在 AcquireAsync 中触发）
        var tier = TokenBudgetTierService.ComputeTier(project.TokenConsumed, project.TokenBudget);
        if (tier == TokenBudgetTierService.Fuse)
            ThrowBudgetExhausted(project, pipelineId: 0, skillId: null, reason: $"budget fuse (tier={tier})");
        if (tier == TokenBudgetTierService.Red)
            _logger.LogWarning("项目 {ProjectId} budget tier=red（{Consumed}/{Budget}），strong Skill 调用将被拒绝",
                projectId, project.TokenConsumed, project.TokenBudget);
    }

    public async Task<LlmCallSlot> AcquireAsync(
        string projectId, string skillId, string runId, string tenantId, long pipelineId, CancellationToken ct = default)
    {
        var policy = await _policyService.GetPolicyAsync(skillId, ct);
        if (policy.MaxLlmCalls <= 0)
            ThrowCallRejected(pipelineId, skillId, runId, CallLimitCode,
                $"Skill {skillId} 禁止直连 LLM Gateway（MaxLlmCalls=0）");

        var project = await LoadProjectAsync(projectId, tenantId, ct);
        // P6-L01 四级：fuse 硬熔断（ThrowBudgetExhausted）；red 也即抛硬错误（禁止静默切到 fast）；
        // yellow/green 保留 policy tier。
        var budgetTier = TokenBudgetTierService.ComputeTier(project.TokenConsumed, project.TokenBudget);
        if (budgetTier == TokenBudgetTierService.Fuse)
            ThrowBudgetExhausted(project, pipelineId, skillId, runId, $"budget fuse (tier={budgetTier})");

        if (budgetTier == TokenBudgetTierService.Red)
        {
            // 硬错误：red tier 已接近预算上限，禁止静默切换 fast 路由（禁止伪成功）
            PushSseError(pipelineId, skillId, runId, BudgetExhaustedCode, $"budget red (tier={budgetTier})");
            throw Oops.Bah($"Skill LLM 预算不足: 当前 tier=red，已接近预算上限。pipeline={pipelineId} skillId={skillId}。请充值或等待结算周期重置。")
                .StatusCode(StatusCodes.Status429TooManyRequests)
                .WithData(new
                {
                    code = BudgetExhaustedCode,
                    tokenConsumed = project.TokenConsumed,
                    tokenBudget = project.TokenBudget,
                });
        }

        var effectiveModelTier = policy.ModelTier;

        var usageKey = UsageKey(runId, skillId);
        var usage = RunUsage.GetOrAdd(usageKey, _ => new SkillRunLlmUsage());

        if (usage.CallCount >= policy.MaxLlmCalls)
            ThrowCallRejected(pipelineId, skillId, runId, CallLimitCode,
                $"Skill {skillId} 已达 maxCalls={policy.MaxLlmCalls}");

        if (usage.TotalTokens >= policy.MaxTotalTokens)
            ThrowCallRejected(pipelineId, skillId, runId, SkillTokenLimitCode,
                $"Skill {skillId} 已达 maxTotalTokens={policy.MaxTotalTokens}");

        var provider = ResolveProvider(effectiveModelTier, null);
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

        var providerCode = !string.IsNullOrWhiteSpace(request.ProviderCode)
            ? request.ProviderCode
            : !string.IsNullOrWhiteSpace(slot.ProviderCode)
                ? slot.ProviderCode
                : _configuration.GetValue("AI:DefaultProvider", "mimo")!;

        var adjusted = request with
        {
            ProviderCode = providerCode,
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

    public async Task AccumulateProjectTokensAsync(string projectId, string tenantId, long delta, CancellationToken ct = default)
    {
        var project = await LoadProjectAsync(projectId, tenantId, ct);
        var newConsumed = project.TokenConsumed + delta;
        // P6-L01 四级 tier：用 TokenBudgetTierService 计算 tier（替代原二态判定）
        var newTier = TokenBudgetTierService.ComputeTier(newConsumed, project.TokenBudget);
        var oldTier = project.LlmBudgetStatus;

        await _db.Updateable<AiProjectEntity>()
            .SetColumns(x => new AiProjectEntity
            {
                TokenConsumed = x.TokenConsumed + delta,
                LlmBudgetStatus = newTier,
                LastModifyTime = DateTime.UtcNow,
            })
            .Where(x => x.Id == projectId && x.TenantId == tenantId)
            .ExecuteCommandAsync(ct);

        // P6-L01 tier 变更审计：tier 变化时投递 BudgetTierChanged IR 事件 + 推送 SSE BudgetDegraded
        if (!string.Equals(oldTier, newTier, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Budget tier 变更 project={ProjectId} {Old}→{New} consumed={Consumed}/{Budget}",
                projectId, oldTier, newTier, newConsumed, project.TokenBudget);

            try
            {
                // 推送 SSE budget_tier_changed（供前端实时展示 tier 变更状态）
                if (long.TryParse(projectId, out var pid))
                {
                    _sseHub?.TryPush(pid, "budget_tier_changed", System.Text.Json.JsonSerializer.Serialize(new
                    {
                        projectId, tenantId, fromTier = oldTier, toTier = newTier,
                        tokenConsumed = newConsumed, tokenBudget = project.TokenBudget,
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Budget tier 变更通知失败（非阻断）");
            }
        }
    }

    private string ResolveProvider(string modelTier, string? overrideProvider)
    {
        if (!string.IsNullOrWhiteSpace(overrideProvider))
            return overrideProvider;

        if (string.Equals(modelTier, "fast", StringComparison.OrdinalIgnoreCase))
            return _configuration.GetValue("LlmRouting:FastProvider", "mimo")!;

        var strong = _configuration.GetValue<string>("LlmRouting:StrongProvider");
        if (!string.IsNullOrWhiteSpace(strong))
            return strong;

        return _configuration.GetValue("AI:DefaultProvider", "mimo")!;
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
