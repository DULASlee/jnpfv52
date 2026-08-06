using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>需求分析 PM LLM 统一 Invoker 单测。</summary>
public class RequirementAnalysisLlmInvokerTests
{
    [Fact]
    public async Task ChatAsync_UsesBudgetGuard_WhenEnforceBudgetTrue()
    {
        var gateway = new RecordingLlmGateway
        {
            NextResponse = new ChatCompletionResponse { IsSuccess = true, Content = "direct-should-not-use" },
        };
        var guard = new RecordingBudgetGuard
        {
            GuardedResponse = new ChatCompletionResponse { IsSuccess = true, Content = "guarded" },
        };

        var invoker = new RequirementAnalysisLlmInvoker(
            guard,
            gateway,
            NullLogger<RequirementAnalysisLlmInvoker>.Instance);

        var context = MakeContext();
        var response = await invoker.ChatAsync(
            context,
            new ChatCompletionRequest { Messages = new List<ChatMessage> { new("user", "hi") } },
            new PmLlmCallOptions { Purpose = "enhance" },
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal("guarded", response.Content);
        Assert.Equal(1, guard.ExecuteCount);
    }

    [Fact]
    public async Task ChatAsync_BypassesBudgetGuard_WhenEnforceBudgetFalse()
    {
        var gateway = new RecordingLlmGateway
        {
            NextResponse = new ChatCompletionResponse { IsSuccess = true, Content = "direct" },
        };
        var guard = new RecordingBudgetGuard();

        var invoker = new RequirementAnalysisLlmInvoker(
            guard,
            gateway,
            NullLogger<RequirementAnalysisLlmInvoker>.Instance);

        var response = await invoker.ChatAsync(
            MakeContext(),
            new ChatCompletionRequest { Messages = new List<ChatMessage> { new("user", "hi") } },
            new PmLlmCallOptions { EnforceBudget = false },
            CancellationToken.None);

        Assert.Equal("direct", response.Content);
        Assert.Equal(0, guard.ExecuteCount);
        Assert.Equal(1, gateway.ChatCount);
    }

    private static SkillContext MakeContext() => new()
    {
        RunId = "run-1",
        TenantId = "t1",
        ProjectId = "p1",
        PipelineId = 42,
        UserRequirement = "请假系统",
    };

    private sealed class RecordingBudgetGuard : ISkillLlmBudgetGuard
    {
        public ChatCompletionResponse GuardedResponse { get; init; } =
            new() { IsSuccess = true, Content = "guarded" };

        public int ExecuteCount { get; private set; }

        public Task<LlmCallSlot> AcquireAsync(
            string projectId, string skillId, string runId, string tenantId, long pipelineId, CancellationToken ct = default)
            => Task.FromResult(new LlmCallSlot());

        public Task<ChatCompletionResponse> ExecuteAsync(LlmCallSlot slot, ChatCompletionRequest request, CancellationToken ct = default)
        {
            ExecuteCount++;
            return Task.FromResult(GuardedResponse);
        }

        public IAsyncEnumerable<string> ExecuteStreamAsync(LlmCallSlot slot, ChatCompletionRequest request, CancellationToken ct = default)
            => throw new NotSupportedException();

        public void ReleaseRun(string runId, string skillId) { }

        public Task AccumulateProjectTokensAsync(string projectId, string tenantId, long delta, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task ValidateProjectBudgetAsync(string projectId, string tenantId, double reserveRatio = 0.95, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class RecordingLlmGateway : ILlmGatewayService
    {
        public ChatCompletionResponse NextResponse { get; init; } =
            new() { IsSuccess = false, Error = "unset" };

        public int ChatCount { get; private set; }

        public Task<ChatCompletionResponse> ChatAsync(ChatCompletionRequest request, CancellationToken ct = default)
        {
            ChatCount++;
            return Task.FromResult(NextResponse);
        }

        public string ResolveProvider(string skillId) => "deepseek";

        public int ResolveTimeoutMs(string skillId) => 60_000;

        public Task<string> ChatAsync(string prompt, string? model = null) => throw new NotSupportedException();

        public Task<ProviderHealth> HealthCheckAsync() => throw new NotSupportedException();

        public IAsyncEnumerable<string> ChatStreamAsync(ChatCompletionRequest request, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<bool> HealthCheckAsync(string providerCode, CancellationToken ct = default) => Task.FromResult(true);

        public Task<ProviderInfo> GetProviderInfoAsync(string providerCode) => throw new NotSupportedException();

        public Task<TreeSearchResult> TreeSearchAsync(TreeSearchRequest request, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
