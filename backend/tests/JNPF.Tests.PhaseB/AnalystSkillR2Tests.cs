using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Skills.Cognitive;
using JNPF.InteAssistant.Skills.Cognitive.Mcp;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// Analyst Skill R2 认知模具迁移测试（施工包 21 R2）.
/// </summary>
public static class AnalystSkillR2Tests
{
    public static void RunAll()
    {
        T1_ParseBusinessEvents_ExtractsFromSkeleton();
        T2_ParseBusinessEvents_EmptyOnInvalidJson();
        T3_ValidateInput_RequiresStableSkeleton();
        T4_ValidateOutput_RequiresAnalysisCompleted();
        T5_LayerAndMission_Classification();
    }

    private static void T1_ParseBusinessEvents_ExtractsFromSkeleton()
    {
        var json = """
            {
              "businessEvents": [
                { "eventId": "BE-001", "eventName": "请假", "complexityHint": "simple" },
                { "eventId": "BE-002", "eventName": "报销" }
              ]
            }
            """;
        var events = AnalystSkillService.ParseBusinessEvents(json);
        if (events.Count != 2 || events[0].EventId != "BE-001" || events[1].EventName != "报销")
            throw new Exception($"T1 ParseBusinessEvents 失败: count={events.Count}");
    }

    private static void T2_ParseBusinessEvents_EmptyOnInvalidJson()
    {
        var events = AnalystSkillService.ParseBusinessEvents("not-json");
        if (events.Count != 0)
            throw new Exception("T2 非法 JSON 应返回空列表");
    }

    private static void T3_ValidateInput_RequiresStableSkeleton()
    {
        var skill = new AnalystSkillService(new FakeAnalystToolkit(), null!, null!, Microsoft.Extensions.Options.Options.Create(new SaPipelineOptions()), null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        var missing = skill.ValidateInputAsync(IrSnapshot.Empty).GetAwaiter().GetResult();
        if (missing.IsValid)
            throw new Exception("T3 无 stable 骨架应校验失败");

        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                new IrSnapshotFragment
                {
                    FragmentId = "skeleton:SK-1",
                    FragmentType = IrFragmentTypes.Skeleton,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"businessEvents":[{"eventId":"BE-001"}]}""",
                },
            },
        };
        var ok = skill.ValidateInputAsync(snapshot).GetAwaiter().GetResult();
        if (!ok.IsValid)
            throw new Exception($"T3 有 stable 骨架应通过: {ok.ErrorMessage}");
    }

    private static void T4_ValidateOutput_RequiresAnalysisCompleted()
    {
        var skill = new AnalystSkillService(new FakeAnalystToolkit(), null!, null!, Microsoft.Extensions.Options.Options.Create(new SaPipelineOptions()), null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        var bad = skill.ValidateOutputAsync(Array.Empty<JNPF.InteAssistant.Entitys.Dto.Ir.AppendIrEventRequest>())
            .GetAwaiter().GetResult();
        if (bad.IsValid)
            throw new Exception("T4 缺少 AnalysisCompleted 应失败");
    }

    private static void T5_LayerAndMission_Classification()
    {
        var skill = new AnalystSkillService(new FakeAnalystToolkit(), null!, null!, Microsoft.Extensions.Options.Options.Create(new SaPipelineOptions()), null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        if (skill.Layer != SkillLayer.Refinement || skill.Mission != SkillMission.RefineSpecification)
            throw new Exception("T5 Analyst 应为 Refinement + RefineSpecification");
        if (skill.Version != "2.0.0-cognitive")
            throw new Exception($"T5 版本应为 2.0.0-cognitive，实际 {skill.Version}");
    }

    private sealed class FakeAnalystToolkit : ICognitiveSkillToolkit
    {
        public ILlmGatewayService Llm => null!;
        public IMcpClient Mcp => null!;
        public IEventStream Events => null!;
        public IExperienceRecorder Experience => null!;
    }
}
