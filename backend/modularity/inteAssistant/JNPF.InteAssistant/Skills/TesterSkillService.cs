using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills.Testing;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 阶段四 P4-B03 — tester-skill：Q1 结构化输入 → 确定性 TestSuite（无 LLM MVP）。
/// 字段源优先 ai_entity_field（25 §6 / 声明 3）。
/// </summary>
public sealed class TesterSkillService : IBaseSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly EntityDesignRepository _entityDesignRepo;
    private readonly ILogger<TesterSkillService> _logger;

    public TesterSkillService(
        EntityDesignRepository entityDesignRepo,
        ILogger<TesterSkillService> logger)
    {
        _entityDesignRepo = entityDesignRepo;
        _logger = logger;
    }

    public string SkillId => DevelopmentSkillIds.Tester;
    public string Version { get; } = "1.0.0-d8";

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[]
        {
            IrFragmentTypes.GeneratedCode,
            IrFragmentTypes.EventSpec,
            IrFragmentTypes.FormPageIR,
            IrFragmentTypes.SystemDesign,
        },
        RequiredStability = IrStabilityStates.Stable,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[]
        {
            IrEventTypes.TestSuiteGenerated,
            IrEventTypes.TesterSkillCompleted,
        },
    };

    public Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        _ = ct;
        var codegen = snapshot.Find(IrFragmentTypes.GeneratedCode, IrStabilityStates.Stable);
        if (codegen == null)
            return Task.FromResult(SkillValidationResult.Fail("IR3_GeneratedCode 须 stable 后才可运行 tester-skill"));

        var hasInput = snapshot.Find(IrFragmentTypes.EventSpec) != null
            || snapshot.Find(IrFragmentTypes.FormPageIR) != null;
        if (!hasInput)
            return Task.FromResult(SkillValidationResult.Fail("缺少 IR1_EventSpec 或 IR2_FormPageIR"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var entityFields = await _entityDesignRepo.ListFieldsAsync(
            context.TenantId, context.ProjectId, context.PipelineId.ToString(), ct);
        var built = TesterSkillInputBuilder.Build(context, entityFields);
        var cases = TestCaseDeriver.DeriveAll(
            built.DerivationMode,
            built.ConfirmedFields,
            built.Transitions,
            built.States);

        var fragmentId = $"testsuite:{context.ProjectId}";
        var payload = TestSuiteManifestBuilder.BuildTestSuiteGeneratedPayload(
            context.ProjectId,
            context.RunId,
            built,
            cases);

        _logger.LogInformation(
            "Tester skill derived {Count} scenarios mode={Mode} fieldSource={FieldSource} pipeline={PipelineId}",
            cases.Count,
            built.DerivationMode,
            built.FieldSource,
            context.PipelineId);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.TestSuiteGenerated,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.TestSuite,
            FragmentVersion = 1,
            Payload = payload,
            SkillId = SkillId,
        };

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.TesterSkillCompleted,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.TestSuite,
            FragmentVersion = 1,
            Payload = TestSuiteManifestBuilder.BuildTesterSkillCompletedPayload(context.ProjectId, cases.Count),
            SkillId = SkillId,
        };

        await Task.CompletedTask;
    }

    public Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events,
        CancellationToken ct = default)
    {
        _ = ct;
        if (events.Count != 2)
            return Task.FromResult(SkillValidationResult.Fail("tester-skill 须产出 TestSuiteGenerated + TesterSkillCompleted"));

        if (events[0].EventType != IrEventTypes.TestSuiteGenerated
            || events[1].EventType != IrEventTypes.TesterSkillCompleted)
        {
            return Task.FromResult(SkillValidationResult.Fail("tester-skill 事件顺序或类型不正确"));
        }

        try
        {
            using var doc = JsonDocument.Parse(events[0].Payload);
            if (!doc.RootElement.TryGetProperty("stabilityState", out var st)
                || st.GetString() != IrStabilityStates.Stable)
            {
                return Task.FromResult(SkillValidationResult.Fail("TestSuite payload 须 stabilityState=stable"));
            }

            if (!doc.RootElement.TryGetProperty("scenarioCount", out var countEl))
                return Task.FromResult(SkillValidationResult.Fail("TestSuite 缺少 scenarioCount"));

            var mode = doc.RootElement.TryGetProperty("derivationMode", out var m)
                ? m.GetString() ?? "field-only"
                : "field-only";
            var min = mode == "field-and-state-machine"
                ? TestCaseDeriver.MinFieldAndStateMachine
                : TestCaseDeriver.MinFieldOnly;
            if (countEl.GetInt32() < min)
            {
                return Task.FromResult(SkillValidationResult.Fail(
                    $"TestSuite scenarioCount {countEl.GetInt32()} < {min}"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillValidationResult.Fail($"TestSuite payload 解析失败: {ex.Message}"));
        }

        return Task.FromResult(SkillValidationResult.Ok());
    }
}
