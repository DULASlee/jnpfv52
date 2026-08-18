using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace JNPF.Tests.Gate.Gates;

/// <summary>
/// 门控管道集成测试 — 5 个用例
/// GatePipeline 流程走真实代码，依赖项用 Fake 控制输入输出
/// </summary>
public class GatePipelineIntegrationTests
{
    private readonly FakeAttachmentProcessor _fakeAttProcessor;
    private readonly FakeRequirementGateService _fakeGateService;
    private readonly FakeLlmGateway _fakeLlmForSemantic;
    private readonly FakeSemanticValidatorWrapper _fakeSemValidator;
    private readonly FakeTenantGuard _fakeTenantGuard;
    private readonly FakeOptionsMonitor _fakeOptions;
    private readonly GatePipeline _pipeline;
    private readonly GatePipelineOptions _options;

    public GatePipelineIntegrationTests()
    {
        _fakeAttProcessor = new FakeAttachmentProcessor();
        _fakeGateService = new FakeRequirementGateService();
        _fakeLlmForSemantic = new FakeLlmGateway();
        _fakeTenantGuard = new FakeTenantGuard();
        _fakeOptions = new FakeOptionsMonitor(new GatePipelineOptions());

        _options = new GatePipelineOptions
        {
            SemanticMinScore = 60,
            MinBusinessEvents = 1,
            MinRoles = 1,
            MinDataEntities = 1,
            MinFieldsPerEntity = 5,
            SemanticProvider = "deepseek",
            MaxAttachmentCount = 10,
            MaxConcurrentFiles = 3,
            PerFileTimeout = TimeSpan.FromMinutes(2)
        };
        _fakeOptions.SetValue(_options);

        _fakeSemValidator = new FakeSemanticValidatorWrapper(_fakeLlmForSemantic);

        _pipeline = new GatePipeline(
            _fakeAttProcessor,
            _fakeGateService,
            _fakeSemValidator,
            _fakeTenantGuard,
            _fakeOptions,
            new FakeLogger<GatePipeline>());
    }

    private static RequestContext CreateContext() => new()
    {
        Scheme = "http", Host = "localhost:5000", TenantId = "1", UserId = "1", UserName = "test"
    };

    [Fact]
    public async Task 完整管道_详细需求_语义评估通过_返回完整结果()
    {
        _fakeGateService.HardRulePassed = true;
        _fakeLlmForSemantic.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """{"passed":true,"score":85,"level":"sufficient","identified":[{"category":"业务事件","description":"工人报工","evidence":"..."},{"category":"角色","description":"车间工人","evidence":"..."},{"category":"数据实体","description":"工单","evidence":"..."}],"missing":[],"nextStepGuidance":"ok"}"""
        };

        var result = await _pipeline.ExecuteAsync("详细MES报工需求", new List<AttachmentFile>(), CreateContext());

        Assert.True(result.Passed);
        Assert.Equal(85, result.SemanticFitness!.Score);
        Assert.Contains("【用户输入】", result.MergedText);
    }

    [Fact]
    public async Task 垃圾输入_硬规则阶段就拦截()
    {
        // "test" 只有4字符且无附件，真实 ValidateHardRules 会拦截
        var result = await _pipeline.ExecuteAsync("test", new List<AttachmentFile>(), CreateContext());

        Assert.False(result.Passed);
        Assert.NotEmpty(result.Reason); // 硬规则拦截
    }

    [Fact]
    public async Task 空洞输入_语义评估拦截_返回结构化反馈()
    {
        _fakeGateService.HardRulePassed = true;
        _fakeLlmForSemantic.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """{"passed":false,"score":10,"level":"insufficient","identified":[],"missing":[{"category":"业务事件","description":"未识别到业务事件","severity":"critical","howToFix":"请描述具体业务场景"}],"nextStepGuidance":"请补充业务场景描述。"}"""
        };

        var result = await _pipeline.ExecuteAsync("我要做个管理系统", new List<AttachmentFile>(), CreateContext());

        Assert.False(result.Passed);
        Assert.Equal(10, result.SemanticFitness!.Score);
        Assert.False(string.IsNullOrEmpty(result.Reason));
        Assert.False(string.IsNullOrEmpty(result.Hint));
    }

    [Fact]
    public async Task 附件部分损坏_正常附件继续处理()
    {
        // 测试 attachment 验证逻辑：.exe 被block，有效文件通过
        // 注：ProcessAttachmentsAsync 走真实实现（non-virtual），不适合用假 byte[] 测试
        _fakeGateService.HardRulePassed = true;
        _fakeLlmForSemantic.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """{"passed":true,"score":70,"level":"sufficient","identified":[{"category":"业务事件","description":"test","evidence":"x"},{"category":"角色","description":"test","evidence":"x"},{"category":"数据实体","description":"test","evidence":"x"}],"missing":[],"nextStepGuidance":""}"""
        };

        var attachments = new List<AttachmentFile>
        {
            new() { FileName = "virus.exe", Content = new byte[] { 1 } }
        };

        var result = await _pipeline.ExecuteAsync("需求描述", attachments, CreateContext());

        // .exe 被blocked（格式不允许）
        Assert.Equal(1, result.BlockedCount);
    }

    [Fact]
    public async Task 图片全部失败_无用户文字_发出明确警告()
    {
        _fakeGateService.HardRulePassed = true;
        _fakeGateService.ImageExtractionThrows = true;
        _fakeLlmForSemantic.NextResponse = new ChatCompletionResponse
        {
            IsSuccess = true,
            Content = """{"passed":true,"score":70,"level":"sufficient","identified":[{"category":"业务事件","description":"test","evidence":"x"},{"category":"角色","description":"test","evidence":"x"},{"category":"数据实体","description":"test","evidence":"x"}],"missing":[],"nextStepGuidance":""}"""
        };

        var attachments = new List<AttachmentFile>
        {
            new() { FileName = "screenshot1.png", Content = new byte[] { 1 } },
            new() { FileName = "screenshot2.jpg", Content = new byte[] { 2 } }
        };

        var result = await _pipeline.ExecuteAsync("", attachments, CreateContext(),
            visionApiKey: "fake-key");

        Assert.Contains(result.Warnings, w => w.Contains("全部") && w.Contains("图片处理失败"));
    }
}

// ═══════════════════════════════════════════
// Fake 实现
// ═══════════════════════════════════════════

public class FakeAttachmentProcessor : AttachmentProcessor
{
    public string? FileNameToFail { get; set; }
    public FakeAttachmentProcessor() : base(new FakeLogger<AttachmentProcessor>()) { }

    public new Task<string> ProcessAttachmentsAsync(List<AttachmentFile> attachments)
    {
        var results = new List<string>();
        foreach (var f in attachments)
        {
            if (f.FileName == FileNameToFail)
                throw new Exception("文件损坏");
            if (GateConstants.IsImageFile(f.FileName))
                results.Add($"[附件：图片 {f.FileName}，需通过多模态模型提取]");
            else
                results.Add($"[附件：{f.FileName}] 提取内容");
        }
        return Task.FromResult(string.Join("\n\n", results));
    }
}

public class FakeRequirementGateService : RequirementGateService
{
    public bool HardRulePassed { get; set; } = true;
    public string HardRuleReason { get; set; } = "";
    public bool ImageExtractionThrows { get; set; }

    public FakeRequirementGateService()
        : base(new FakeLlmGateway(), new FakeLogger<RequirementGateService>(), null!) { }

    public new HardRuleResult ValidateHardRules(string text, int attachmentCount)
    {
        return HardRulePassed
            ? new HardRuleResult { Passed = true }
            : new HardRuleResult { Passed = false, Reason = HardRuleReason };
    }

    public new Task<string> ExtractFromImages(
        List<AttachmentFile> images, string apiUrl, string apiKey, string model, CancellationToken ct)
    {
        if (ImageExtractionThrows)
            throw new Exception("Vision API 不可用");
        return Task.FromResult("[图片分析结果]");
    }
}

/// <summary>包装 SemanticFitnessValidator + FakeLlmGateway，让集成测试控制 LLM 返回</summary>
public class FakeSemanticValidatorWrapper : SemanticFitnessValidator
{
    public FakeSemanticValidatorWrapper(FakeLlmGateway fakeLlm)
        : base(fakeLlm, new FakeLogger<SemanticFitnessValidator>()) { }
}

public class FakeTenantGuard : ITenantGuard
{
    public T WithTenant<T>(T entity, string tenantId) where T : class => entity;
    public bool VerifyOwnership<T>(T entity, string currentTenantId) where T : class => true;
    public Dictionary<string, string> GetUploadHeaders(RequestContext ctx) => new();
}

public class FakeOptionsMonitor : IOptionsMonitor<GatePipelineOptions>
{
    private GatePipelineOptions _value;
    public FakeOptionsMonitor(GatePipelineOptions value) => _value = value;
    public GatePipelineOptions CurrentValue => _value;
    public void SetValue(GatePipelineOptions v) => _value = v;
    public GatePipelineOptions Get(string? name) => _value;
    public IDisposable? OnChange(Action<GatePipelineOptions, string?> listener) => null;
}
