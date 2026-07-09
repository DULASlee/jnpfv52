using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JNPF.Tests.Gate.Gates;

/// <summary>
/// RequirementGateService.EvaluateMaturity 的 fail-safe 行为测试。
///
/// 缺陷背景（2026-07-08）：
///   EvaluateMaturity 在 LLM 故障/异常时曾降级 Mode="refine"，
///   refine 会跳过追问直接进入 SA 深度分析 = 放行不完整需求（fail-open）。
///   修复后降级 Mode="confirm"（继续追问，不进分析），与主门控 fail-closed 策略一致。
///
/// 这组测试守护该修复，防止回归到 fail-open。
/// </summary>
public class RequirementGateFailSafeTests
{
    [Fact]
    public async Task LLM调用失败_应降级confirm_不降级refine()
    {
        var fakeLlm = new FakeLlmGateway
        {
            NextResponse = new ChatCompletionResponse { IsSuccess = false, Error = "gateway timeout" }
        };
        var gate = new RequirementGateService(fakeLlm, new FakeLogger<RequirementGateService>(), null!);

        var result = await gate.EvaluateMaturity(
            new List<ChatMessage> { new("user", "做个系统") },
            "deepseek",
            CancellationToken.None);

        // 核心断言：必须 confirm（继续追问），绝不能 refine（直接分析）
        Assert.Equal("confirm", result.Mode);
        Assert.NotEqual("refine", result.Mode);
    }

    [Fact]
    public async Task LLM返回空内容_应降级confirm_不降级refine()
    {
        var fakeLlm = new FakeLlmGateway
        {
            NextResponse = new ChatCompletionResponse { IsSuccess = true, Content = "" }
        };
        var gate = new RequirementGateService(fakeLlm, new FakeLogger<RequirementGateService>(), null!);

        var result = await gate.EvaluateMaturity(
            new List<ChatMessage> { new("user", "做个系统") },
            "deepseek",
            CancellationToken.None);

        Assert.Equal("confirm", result.Mode);
        Assert.NotEqual("refine", result.Mode);
    }

    [Fact]
    public async Task LLM抛异常_应降级confirm_不降级refine()
    {
        var fakeLlm = new ThrowingLlmGateway();
        var gate = new RequirementGateService(fakeLlm, new FakeLogger<RequirementGateService>(), null!);

        var result = await gate.EvaluateMaturity(
            new List<ChatMessage> { new("user", "做个系统") },
            "deepseek",
            CancellationToken.None);

        Assert.Equal("confirm", result.Mode);
        Assert.NotEqual("refine", result.Mode);
    }

    [Fact]
    public async Task LLM返回非法JSON_应降级confirm_不降级refine()
    {
        var fakeLlm = new FakeLlmGateway
        {
            NextResponse = new ChatCompletionResponse
            {
                IsSuccess = true,
                Content = "这不是JSON，是LLM胡言乱语"
            }
        };
        var gate = new RequirementGateService(fakeLlm, new FakeLogger<RequirementGateService>(), null!);

        var result = await gate.EvaluateMaturity(
            new List<ChatMessage> { new("user", "做个系统") },
            "deepseek",
            CancellationToken.None);

        // 非法 JSON → JsonSerializer 返回 null → 旧代码降级 refine，新代码降级 confirm
        Assert.Equal("confirm", result.Mode);
    }

    /// <summary>正常路径不受影响：LLM 返回合法 refine → 保持 refine</summary>
    [Fact]
    public async Task LLM正常返回refine_保持refine_不被误改为confirm()
    {
        var fakeLlm = new FakeLlmGateway
        {
            NextResponse = new ChatCompletionResponse
            {
                IsSuccess = true,
                Content = """{"score":85,"mode":"refine","domain":"OA","entities":["请假单"],"missing":[],"strengths":["角色清晰"],"nextQuestion":"","clarifications":[]}"""
            }
        };
        var gate = new RequirementGateService(fakeLlm, new FakeLogger<RequirementGateService>(), null!);

        var result = await gate.EvaluateMaturity(
            new List<ChatMessage> { new("user", "员工请假审批系统，角色员工/主管/HR") },
            "deepseek",
            CancellationToken.None);

        // score=85 → refine 是正确的，不应被改成 confirm
        Assert.Equal("refine", result.Mode);
    }
}

/// <summary>总是抛异常的 LLM mock，模拟 LLM 服务宕机</summary>
public class ThrowingLlmGateway : ILlmGatewayService
{
    Task<ChatCompletionResponse> ILlmGatewayService.ChatAsync(ChatCompletionRequest request, CancellationToken ct) =>
        throw new HttpRequestException("LLM service unavailable");

    [Obsolete] Task<string> ILlmGatewayService.ChatAsync(string prompt, string? model) =>
        throw new NotSupportedException();
    [Obsolete] Task<ProviderHealth> ILlmGatewayService.HealthCheckAsync() =>
        throw new NotSupportedException();
    IAsyncEnumerable<string> ILlmGatewayService.ChatStreamAsync(ChatCompletionRequest request, CancellationToken ct) =>
        throw new NotSupportedException();
    Task<bool> ILlmGatewayService.HealthCheckAsync(string providerCode, CancellationToken ct) =>
        throw new NotSupportedException();
    Task<ProviderInfo> ILlmGatewayService.GetProviderInfoAsync(string providerCode) =>
        throw new NotSupportedException();
    Task<TreeSearchResult> ILlmGatewayService.TreeSearchAsync(TreeSearchRequest request, CancellationToken ct) =>
        throw new NotSupportedException();
}
