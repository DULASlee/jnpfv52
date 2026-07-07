using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Skills.Cognitive;
using JNPF.InteAssistant.Skills.Cognitive.Mcp;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// PM Skill R1 认知模具迁移测试（施工包 21 R1）.
/// </summary>
public static class PmSkillR1Tests
{
    public static void RunAll()
    {
        T1_SelectTopCandidate_PicksHighestScore();
        T2_ExtractSearchKeyword_PrefersRequirement();
        T3_ExtractJson_StripsMarkdownFence();
        T4_ValidateOutput_RequiresSingleSkeletonCreated();
    }

    private static void T1_SelectTopCandidate_PicksHighestScore()
    {
        var scored = new List<(string Json, decimal Score, int BranchIndex, double Temperature)>
        {
            ("{\"a\":1}", 0.3m, 0, 0.3),
            ("{\"a\":2}", 0.9m, 1, 0.65),
            ("{\"a\":3}", 0.5m, 2, 1.0),
        };

        var top = PmSkillService.SelectTopCandidate(scored);
        if (top.BranchIndex != 1 || top.Score != 0.9m)
            throw new Exception($"T1 Top-1 应为 branch=1 score=0.9，实际 branch={top.BranchIndex} score={top.Score}");
    }

    private static void T2_ExtractSearchKeyword_PrefersRequirement()
    {
        var ctx = new SkillContext
        {
            RunId = "r",
            TenantId = "t",
            ProjectId = "p",
            PipelineId = 1,
            UserRequirement = "  请假管理系统  ",
        };
        var kw = PmSkillService.ExtractSearchKeyword(ctx);
        if (kw != "请假管理系统")
            throw new Exception($"T2 keyword 应为 '请假管理系统'，实际 '{kw}'");
    }

    private static void T3_ExtractJson_StripsMarkdownFence()
    {
        var raw = "```json\n{\"businessEvents\":[]}\n```";
        var json = PmSkillService.ExtractJson(raw);
        if (!json.StartsWith('{') || !json.Contains("businessEvents"))
            throw new Exception($"T3 ExtractJson 失败: {json}");
    }

    private static void T4_ValidateOutput_RequiresSingleSkeletonCreated()
    {
        var pm = new PmSkillService(new FakePmToolkit(), null!);
        var ok = pm.ValidateOutputAsync(new[]
        {
            new JNPF.InteAssistant.Entitys.Dto.Ir.AppendIrEventRequest
            {
                EventType = JNPF.InteAssistant.Entitys.Ir.IrEventTypes.SkeletonCreated,
                Payload = "{}",
            },
        }).GetAwaiter().GetResult();

        if (!ok.IsValid)
            throw new Exception($"T4 ValidateOutput 应通过: {ok.ErrorMessage}");

        var bad = pm.ValidateOutputAsync(Array.Empty<JNPF.InteAssistant.Entitys.Dto.Ir.AppendIrEventRequest>())
            .GetAwaiter().GetResult();
        if (bad.IsValid)
            throw new Exception("T4 空产出应校验失败");
    }

    private sealed class FakePmToolkit : ICognitiveSkillToolkit
    {
        public ILlmGatewayService Llm => null!;
        public IMcpClient Mcp => null!;
        public IEventStream Events => null!;
        public IExperienceRecorder Experience => null!;
    }
}
