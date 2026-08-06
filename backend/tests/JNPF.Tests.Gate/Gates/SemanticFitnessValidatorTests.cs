using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JNPF.Tests.Gate.Gates;

/// <summary>
/// SemanticFitnessValidator 红绿测试 — 9 个用例
/// 使用 FakeLlmGateway 替代 Moq 以保证确定性
/// </summary>
public class SemanticFitnessValidatorTests
{
    private readonly FakeLlmGateway _fakeLlm;
    private readonly SemanticFitnessValidator _validator;
    private readonly GatePipelineOptions _options;

    public SemanticFitnessValidatorTests()
    {
        _fakeLlm = new FakeLlmGateway();
        var logger = new FakeLogger<SemanticFitnessValidator>();
        _validator = new SemanticFitnessValidator(_fakeLlm, logger);
        _options = new GatePipelineOptions
        {
            SemanticMinScore = 60,
            MinBusinessEvents = 1,
            MinRoles = 1,
            MinDataEntities = 1,
            MinFieldsPerEntity = 5,
            SemanticProvider = "deepseek"
        };
    }

    // ═══════════════════════════════════════════
    // 绿灯（应该通过）
    // ═══════════════════════════════════════════

    [Fact]
    public async Task 详细MES需求_应该通过()
    {
        var input = "我们是汽车零部件工厂，需要一个报工管理系统。工人完成工序后扫描工单号。车间主任审核报工记录。系统需要管理：工单、工序、报工记录。";

        _fakeLlm.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """
            {
                "passed": true,
                "score": 85,
                "level": "sufficient",
                "identified": [
                    {"category":"业务事件","description":"工人提交工序报工","evidence":"工人完成工序"},
                    {"category":"业务事件","description":"车间主任审核报工","evidence":"车间主任审核"},
                    {"category":"角色","description":"车间工人","evidence":"工人完成工序"},
                    {"category":"角色","description":"车间主任","evidence":"车间主任审核"},
                    {"category":"数据实体","description":"工单","evidence":"工单"},
                    {"category":"数据实体","description":"工序","evidence":"工序"},
                    {"category":"数据实体","description":"报工记录","evidence":"报工记录"}
                ],
                "missing": [],
                "nextStepGuidance": "需求材料充分。"
            }
            """
        };

        var result = await _validator.EvaluateAsync(input, _options, CancellationToken.None);

        Assert.True(result.Passed);
        Assert.True(result.Identified.Any(e => e.Category == "业务事件"));
        Assert.True(result.Identified.Any(e => e.Category == "角色"));
        Assert.True(result.Identified.Any(e => e.Category == "数据实体"));
        Assert.True(result.Score >= 60);
        Assert.Equal(FitnessLevel.Sufficient, result.Level);
    }

    [Fact]
    public async Task 简短需求含实体和角色_应该通过()
    {
        var input = "管理我们的产品库存，库管员负责入库出库，系统需要管理产品信息和库存量。";

        _fakeLlm.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """
            {
                "passed": true,
                "score": 65,
                "level": "sufficient",
                "identified": [
                    {"category":"业务事件","description":"库管员入库出库","evidence":"库管员负责入库出库"},
                    {"category":"角色","description":"库管员","evidence":"库管员负责"},
                    {"category":"数据实体","description":"产品","evidence":"管理产品库存"}
                ],
                "missing": [],
                "nextStepGuidance": ""
            }
            """
        };

        var result = await _validator.EvaluateAsync(input, _options, CancellationToken.None);

        Assert.True(result.Passed);
        Assert.True(result.Identified.Any(e => e.Category == "数据实体"));
    }

    // ═══════════════════════════════════════════
    // 红灯（应该不通过）
    // ═══════════════════════════════════════════

    [Fact]
    public async Task 仅写管理系统_应该不通过()
    {
        var input = "我要做个管理系统";

        _fakeLlm.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """
            {
                "passed": false,
                "score": 5,
                "level": "insufficient",
                "identified": [],
                "missing": [
                    {"category":"业务事件","description":"未识别到任何业务事件","severity":"critical","howToFix":"请描述具体的业务场景"},
                    {"category":"角色","description":"未识别到任何角色","severity":"critical","howToFix":"请说明系统使用者"}
                ],
                "nextStepGuidance": "需求信息严重不足。"
            }
            """
        };

        var result = await _validator.EvaluateAsync(input, _options, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.True(result.Missing.Any(m => m.Category == "业务事件" && m.Severity == "critical"));
        Assert.False(string.IsNullOrEmpty(result.Missing.First(m => m.Category == "业务事件").HowToFix));
    }

    [Fact]
    public async Task 有角色无业务事件_应该不通过()
    {
        var input = "我想做一个仓库管理系统，仓库管理员负责入库和出库。";

        _fakeLlm.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """
            {
                "passed": false,
                "score": 30,
                "level": "partial",
                "identified": [
                    {"category":"角色","description":"仓库管理员","evidence":"仓库管理员负责"}
                ],
                "missing": [
                    {"category":"业务事件","description":"未明确识别到业务事件","severity":"critical","howToFix":"请描述具体业务操作"},
                    {"category":"数据实体","description":"未识别到数据实体","severity":"critical","howToFix":"请说明管理哪些数据"}
                ],
                "nextStepGuidance": "有角色但缺业务事件和数据实体。"
            }
            """
        };

        var result = await _validator.EvaluateAsync(input, _options, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.True(result.Missing.Any(m => m.Severity == "critical"));
        Assert.False(string.IsNullOrEmpty(result.BuildGuidance()));
    }

    // ═══════════════════════════════════════════
    // 硬阈值覆盖（缺陷3修复）
    // ═══════════════════════════════════════════

    [Fact]
    public async Task LLM返回通过但无业务事件_硬阈值覆盖为不通过()
    {
        // LLM 说 passed=true 但没有识别到业务事件 → PostProcess 强制拦截
        _fakeLlm.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """
            {
                "passed": true,
                "score": 65,
                "level": "sufficient",
                "identified": [
                    {"category":"角色","description":"管理员","evidence":"管理系统"}
                ],
                "missing": []
            }
            """
        };

        var result = await _validator.EvaluateAsync("test input", _options, CancellationToken.None);

        // 硬阈值覆盖：虽然 LLM 说通过，但没有业务事件 → 强制拦截
        Assert.False(result.Passed);
        Assert.True(result.Missing.Any(m => m.Category == "业务事件"));
    }

    // ═══════════════════════════════════════════
    // Fail-Closed 测试（缺陷1修复）
    // ═══════════════════════════════════════════

    [Fact]
    public async Task LLM调用失败_应该FailClosed()
    {
        _fakeLlm.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = false,
            Error = "timeout"
        };

        var result = await _validator.EvaluateAsync("test", _options, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains("GATE_LLM_ERR", result.NextStepGuidance);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public async Task LLM返回乱码非JSON_应该FailClosed()
    {
        _fakeLlm.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = "这不是 JSON，而是一段随意的文字回复。"
        };

        var result = await _validator.EvaluateAsync("test", _options, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains("GATE_JSON_ERR", result.NextStepGuidance);
    }

    [Fact]
    public async Task LLM返回markdown包裹JSON_应该解析成功()
    {
        _fakeLlm.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """
            ```json
            {
                "passed": true,
                "score": 80,
                "level": "sufficient",
                "identified": [
                    {"category":"业务事件","description":"工人报工","evidence":"报工"},
                    {"category":"角色","description":"工人","evidence":"工人"},
                    {"category":"数据实体","description":"工单","evidence":"工单"}
                ],
                "missing": [],
                "nextStepGuidance": "ok"
            }
            ```
            """
        };

        var result = await _validator.EvaluateAsync("报工系统", _options, CancellationToken.None);

        Assert.True(result.Passed);
        Assert.True(result.Score >= 60);
    }

    [Fact]
    public async Task LLM返回空内容_应该FailClosed且标记EMPTY()
    {
        _fakeLlm.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = "   "
        };

        var result = await _validator.EvaluateAsync("test", _options, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains("GATE_LLM_EMPTY", result.NextStepGuidance);
    }

    [Fact]
    public async Task LLM返回JSON缺少核心字段_应该FailClosed()
    {
        _fakeLlm.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """{"passed":true}"""
        };

        var result = await _validator.EvaluateAsync("test", _options, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains("GATE_SCHEMA_ERR", result.NextStepGuidance);
    }

    [Fact]
    public async Task CancellationToken取消_应该FailClosed()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _validator.EvaluateAsync("test", _options, cts.Token);

        Assert.False(result.Passed);
        Assert.Contains("GATE_TIMEOUT", result.NextStepGuidance);
    }
}

// ═══════════════════════════════════════════
// 测试用 Fake 实现
// ═══════════════════════════════════════════

/// <summary>Fake LLM Gateway — 返回预设响应（显式接口实现确保正确的 virtual dispatch）</summary>
public class FakeLlmGateway : ILlmGatewayService
{
    public ChatCompletionResponse? NextResponse { get; set; }

    Task<ChatCompletionResponse> ILlmGatewayService.ChatAsync(ChatCompletionRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(NextResponse ?? new ChatCompletionResponse { IsSuccess = false, Error = "no response set" });
    }

    // 未使用的方法 — 抛异常表示不应被调用
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

/// <summary>Fake Logger — 记录日志和异常</summary>
public class FakeLogger<T> : ILogger<T>
{
    public List<string> LoggedMessages { get; } = new();
    public Exception? LastException { get; set; }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (exception != null) LastException = exception;
        LoggedMessages.Add(formatter(state, exception));
    }
}
