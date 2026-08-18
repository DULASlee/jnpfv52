using System.Runtime.CompilerServices;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills;

namespace JNPF.Tests.PhaseB;

/// <summary>测试用：跳过 BudgetGuard，直连 ILlmGatewayService。</summary>
internal sealed class PassThroughPmLlmInvoker : IRequirementAnalysisLlmInvoker
{
    private readonly ILlmGatewayService _gateway;

    public PassThroughPmLlmInvoker(ILlmGatewayService gateway) => _gateway = gateway;

    public static PassThroughPmLlmInvoker NoOp() => new(new NoOpLlmGateway());

    public Task<ChatCompletionResponse> ChatAsync(
        SkillContext context,
        ChatCompletionRequest request,
        PmLlmCallOptions? options = null,
        CancellationToken ct = default)
        => _gateway.ChatAsync(request, ct);

    public async IAsyncEnumerable<string> ChatStreamAsync(
        SkillContext context,
        ChatCompletionRequest request,
        PmLlmCallOptions? options = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var chunk in _gateway.ChatStreamAsync(request, ct))
            yield return chunk;
    }

    private sealed class NoOpLlmGateway : ILlmGatewayService
    {
        Task<ChatCompletionResponse> ILlmGatewayService.ChatAsync(ChatCompletionRequest request, CancellationToken ct)
            => Task.FromResult(new ChatCompletionResponse { IsSuccess = false, Error = "no-op" });

        [Obsolete] Task<string> ILlmGatewayService.ChatAsync(string prompt, string? model)
            => throw new NotSupportedException();

        [Obsolete] Task<ProviderHealth> ILlmGatewayService.HealthCheckAsync()
            => throw new NotSupportedException();

        IAsyncEnumerable<string> ILlmGatewayService.ChatStreamAsync(ChatCompletionRequest request, CancellationToken ct)
            => EmptyStream();

        Task<bool> ILlmGatewayService.HealthCheckAsync(string providerCode, CancellationToken ct)
            => Task.FromResult(true);

        Task<ProviderInfo> ILlmGatewayService.GetProviderInfoAsync(string providerCode)
            => throw new NotSupportedException();

        Task<TreeSearchResult> ILlmGatewayService.TreeSearchAsync(TreeSearchRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        string ILlmGatewayService.ResolveProvider(string? taskKey) => "fake";

        int ILlmGatewayService.ResolveTimeoutMs(string? taskKey) => 30_000;

        private static async IAsyncEnumerable<string> EmptyStream()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
