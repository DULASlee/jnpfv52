using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Llm;

/// <summary>
/// 需求分析阶段 PM / 编排器 LLM 调用的统一入口（预算门禁 + 审计 + 策略裁剪）。
/// 设计 Skill 仍可直接使用 <see cref="ISkillLlmBudgetGuard"/>；本 Invoker 专供 pm-skill 主链。
/// </summary>
public interface IRequirementAnalysisLlmInvoker
{
    /// <summary>非流式补全（经 BudgetGuard，写入 call log）。</summary>
    Task<ChatCompletionResponse> ChatAsync(
        SkillContext context,
        ChatCompletionRequest request,
        PmLlmCallOptions? options = null,
        CancellationToken ct = default);

    /// <summary>流式补全（经 BudgetGuard，流结束后估算 token 记账）。</summary>
    IAsyncEnumerable<string> ChatStreamAsync(
        SkillContext context,
        ChatCompletionRequest request,
        PmLlmCallOptions? options = null,
        CancellationToken ct = default);
}

/// <summary>PM 主链 LLM 调用选项。</summary>
public sealed record PmLlmCallOptions
{
    /// <summary>策略 SkillId，默认 pm-skill（对齐 LlmCallPolicyService）。</summary>
    public string SkillId { get; init; } = "pm-skill";

    /// <summary>调用用途（日志/审计），如 enhance / refine / clarification / pspec-dt。</summary>
    public string? Purpose { get; init; }

    /// <summary>显式 Provider 任务路由键（传给 ILlmGatewayService.ResolveProvider）。</summary>
    public string? ProviderTask { get; init; }

    /// <summary>为 false 时跳过 BudgetGuard（仅测试/紧急降级，生产默认 true）。</summary>
    public bool EnforceBudget { get; init; } = true;
}

/// <inheritdoc />
public sealed class RequirementAnalysisLlmInvoker : IRequirementAnalysisLlmInvoker, ITransient
{
    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly ILlmGatewayService _gateway;
    private readonly ILogger<RequirementAnalysisLlmInvoker> _logger;

    public RequirementAnalysisLlmInvoker(
        ISkillLlmBudgetGuard budgetGuard,
        ILlmGatewayService gateway,
        ILogger<RequirementAnalysisLlmInvoker> logger)
    {
        _budgetGuard = budgetGuard;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<ChatCompletionResponse> ChatAsync(
        SkillContext context,
        ChatCompletionRequest request,
        PmLlmCallOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new PmLlmCallOptions();
        var prepared = PrepareRequest(context, request, options);

        if (!options.EnforceBudget)
        {
            using (BeginAudit(context, options))
                return await _gateway.ChatAsync(prepared, ct);
        }

        var slot = await _budgetGuard.AcquireAsync(
            context.ProjectId, options.SkillId, context.RunId, context.TenantId, context.PipelineId, ct);

        _logger.LogInformation(
            "PmLlmCall Chat purpose={Purpose} skill={SkillId} pipeline={PipelineId}",
            options.Purpose, options.SkillId, context.PipelineId);

        return await _budgetGuard.ExecuteAsync(slot, prepared, ct);
    }

    public IAsyncEnumerable<string> ChatStreamAsync(
        SkillContext context,
        ChatCompletionRequest request,
        PmLlmCallOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new PmLlmCallOptions();
        var prepared = PrepareRequest(context, request, options);

        if (!options.EnforceBudget)
            return StreamWithoutBudget(context, options, prepared, ct);

        return StreamWithBudget(context, options, prepared, ct);
    }

    private async IAsyncEnumerable<string> StreamWithBudget(
        SkillContext context,
        PmLlmCallOptions options,
        ChatCompletionRequest prepared,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var slot = await _budgetGuard.AcquireAsync(
            context.ProjectId, options.SkillId, context.RunId, context.TenantId, context.PipelineId, ct);

        _logger.LogInformation(
            "PmLlmCall Stream purpose={Purpose} skill={SkillId} pipeline={PipelineId}",
            options.Purpose, options.SkillId, context.PipelineId);

        await foreach (var chunk in _budgetGuard.ExecuteStreamAsync(slot, prepared, ct))
            yield return chunk;
    }

    private async IAsyncEnumerable<string> StreamWithoutBudget(
        SkillContext context,
        PmLlmCallOptions options,
        ChatCompletionRequest prepared,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        using (BeginAudit(context, options))
        {
            await foreach (var chunk in _gateway.ChatStreamAsync(prepared, ct))
                yield return chunk;
        }
    }

    private ChatCompletionRequest PrepareRequest(
        SkillContext context,
        ChatCompletionRequest request,
        PmLlmCallOptions options)
    {
        var providerCode = !string.IsNullOrWhiteSpace(request.ProviderCode)
            ? request.ProviderCode
            : _gateway.ResolveProvider(options.ProviderTask ?? options.SkillId);

        var timeoutMs = request.TimeoutMs > 0
            ? request.TimeoutMs
            : _gateway.ResolveTimeoutMs(options.ProviderTask ?? options.SkillId);

        return request with
        {
            ProviderCode = providerCode,
            TimeoutMs = timeoutMs,
        };
    }

    private static IDisposable BeginAudit(SkillContext context, PmLlmCallOptions options)
        => LlmCallAuditContext.Begin(
            context.RunId,
            options.SkillId,
            context.ProjectId,
            context.TenantId,
            context.PipelineId.ToString());
}
