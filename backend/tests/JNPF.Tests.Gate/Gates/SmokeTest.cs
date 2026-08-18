using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Interfaces;
using Xunit;

namespace JNPF.Tests.Gate.Gates;

/// <summary>
/// 冒烟测试 — 验证基础组件工作正常
/// </summary>
public class SmokeTest
{
    [Fact]
    public async Task FakeLlmGateway_CanBeCalled()
    {
        var fake = new FakeLlmGateway();
        fake.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """{"passed":true,"score":80,"level":"sufficient","identified":[{"category":"业务事件","description":"test","evidence":"test"}],"missing":[],"nextStepGuidance":""}"""
        };

        // 通过接口调用
        ILlmGatewayService svc = fake;
        var resp = await svc.ChatAsync(new ChatCompletionRequest(), CancellationToken.None);
        Assert.True(resp.IsSuccess);
        Assert.Contains("业务事件", resp.Content);
    }

    [Fact]
    public async Task FakeLlmGateway_ThroughInterface()
    {
        ILlmGatewayService svc = new FakeLlmGateway
        {
            NextResponse = new ChatCompletionResponse
            {
                IsSuccess = true,
                Content = """{"passed":true,"score":80,"level":"sufficient","identified":[{"category":"业务事件","description":"test","evidence":"test"}],"missing":[],"nextStepGuidance":""}"""
            }
        };

        // 通过接口调用
        var resp = await svc.ChatAsync(new ChatCompletionRequest(), CancellationToken.None);
        Assert.True(resp.IsSuccess);
        Assert.Contains("业务事件", resp.Content);
    }

    [Fact]
    public async Task Evaluator_EndToEnd_GreenPath()
    {
        var fake = new FakeLlmGateway();
        fake.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """
            {
                "passed": true,
                "score": 85,
                "level": "sufficient",
                "identified": [
                    {"category":"业务事件","description":"工人报工","evidence":"工人完成工序"},
                    {"category":"角色","description":"车间工人","evidence":"工人"},
                    {"category":"数据实体","description":"工单","evidence":"工单"}
                ],
                "missing": [],
                "nextStepGuidance": "ok"
            }
            """
        };

        var logger = new FakeLogger<SemanticFitnessValidator>();
        var validator = new SemanticFitnessValidator(fake, logger);

        var options = new GatePipelineOptions
        {
            SemanticMinScore = 60,
            MinBusinessEvents = 1,
            MinRoles = 1,
            MinDataEntities = 1,
            MinFieldsPerEntity = 5,
            SemanticProvider = "deepseek"
        };

        // 先验证 Fake 通过接口确实返回正确的响应
        ILlmGatewayService svc = fake;
        var directResp = await svc.ChatAsync(new ChatCompletionRequest
        {
            ProviderCode = "deepseek",
            SystemPrompt = "test",
            Messages = new List<ChatMessage> { new() { Role = "user", Content = "test input" } },
            MaxTokens = 1500,
            Temperature = 0.1,
            ResponseFormat = "json",
            MaxRetries = 2,
            TimeoutMs = 45000
        }, CancellationToken.None);
        Assert.True(directResp.IsSuccess, "Direct call to Fake through interface should succeed");
        Assert.Contains("业务事件", directResp.Content);

        // 然后验证 validator 使用了这个 Fake
        var result = await validator.EvaluateAsync("test input", options, CancellationToken.None);

        Assert.True(result.Passed, $"Expected Passed=true but got false. Score={result.Score}, Missing count={result.Missing.Count}, Guidance={result.NextStepGuidance}, LastException={logger.LastException?.Message}, ExType={logger.LastException?.GetType().Name}");
        Assert.Equal(85, result.Score);
    }
}
