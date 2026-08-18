using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Skills.Cognitive;
using JNPF.InteAssistant.Skills.Cognitive.Mcp;
using Microsoft.Extensions.Logging.Abstractions;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// CognitiveSkill R0 契约铸造测试（施工包 21 §6）：
/// 温度梯度规划、模具盖戳、输出白名单质检、输入前置校验、InProc MCP 路由。
/// 全部纯内存，不依赖 DB / LLM。
/// </summary>
public static class CognitiveSkillR0Tests
{
    public static async Task RunAllAsync()
    {
        T1_TemperatureSchedule_ClampsBranchesAndRange();
        T2_TemperatureSchedule_IsMonotonicWithStep();
        await T3_ReasonAsync_StampsSkillId();
        await T4_ValidateOutput_RejectsUndeclaredEventType();
        await T5_ValidateInput_RequiresDeclaredFragments();
        await T6_InProcMcp_RoutesAndReportsUnknownTool();
    }

    // ── T1/T2: TreeSearchPlanner 纯函数 ──

    private static void T1_TemperatureSchedule_ClampsBranchesAndRange()
    {
        var tooFew = TreeSearchPlanner.BuildTemperatureSchedule(1, 0.3, 0.35);
        Assert(tooFew.Length == TreeSearchPlanner.MinBranches, $"T1 分支下限应为 2，实际 {tooFew.Length}");

        var tooMany = TreeSearchPlanner.BuildTemperatureSchedule(99, 0.3, 0.35);
        Assert(tooMany.Length == TreeSearchPlanner.MaxBranches, $"T1 分支上限应为 6，实际 {tooMany.Length}");

        var extreme = TreeSearchPlanner.BuildTemperatureSchedule(6, 1.9, 1.0);
        Assert(extreme.All(t => t is >= 0 and <= 2), "T1 温度必须收敛在 [0,2]");
    }

    private static void T2_TemperatureSchedule_IsMonotonicWithStep()
    {
        var schedule = TreeSearchPlanner.BuildTemperatureSchedule(4, 0.3, 0.35);
        Assert(schedule.Length == 4, $"T2 应生成 4 路，实际 {schedule.Length}");
        Assert(Math.Abs(schedule[0] - 0.3) < 0.001, $"T2 首路温度应为 0.3，实际 {schedule[0]}");
        for (var i = 1; i < schedule.Length; i++)
            Assert(schedule[i] >= schedule[i - 1], "T2 温度梯度必须单调不减");
    }

    // ── T3: 模具盖戳 ──

    private static async Task T3_ReasonAsync_StampsSkillId()
    {
        var skill = new FakeCognitiveSkill();
        var events = new List<AppendIrEventRequest>();
        await foreach (var evt in skill.ReasonAsync(BuildContext()))
            events.Add(evt);

        Assert(events.Count == 2, $"T3 应产出 2 事件，实际 {events.Count}");
        Assert(events[0].SkillId == "fake-cognitive-skill", "T3 无戳事件必须自动盖 SkillId");
        Assert(events[1].SkillId == "manual-override", "T3 已有 SkillId 的事件不得被覆盖");
    }

    // ── T4: 输出白名单 ──

    private static async Task T4_ValidateOutput_RejectsUndeclaredEventType()
    {
        var skill = new FakeCognitiveSkill();

        var ok = await skill.ValidateOutputAsync(new[]
        {
            new AppendIrEventRequest { EventType = IrEventTypes.SkeletonCreated },
        });
        Assert(ok.IsValid, "T4 声明内事件类型应通过质检");

        var bad = await skill.ValidateOutputAsync(new[]
        {
            new AppendIrEventRequest { EventType = IrEventTypes.CodeGenerated },
        });
        Assert(!bad.IsValid, "T4 未声明事件类型必须被拒绝");
        Assert(bad.ErrorMessage!.Contains("CodeGenerated"), "T4 拒绝原因须含违规事件类型");
    }

    // ── T5: 输入前置校验 ──

    private static async Task T5_ValidateInput_RequiresDeclaredFragments()
    {
        var skill = new NeedsSkeletonSkill();

        var missing = await skill.ValidateInputAsync(IrSnapshot.Empty);
        Assert(!missing.IsValid, "T5 缺前置片段必须校验失败");

        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                new IrSnapshotFragment
                {
                    FragmentId = "f1",
                    FragmentType = IrFragmentTypes.Skeleton,
                    StabilityState = IrStabilityStates.Stable,
                },
            },
        };
        var ok = await skill.ValidateInputAsync(snapshot);
        Assert(ok.IsValid, $"T5 前置片段齐备应通过: {ok.ErrorMessage}");
    }

    // ── T6: InProc MCP 路由 ──

    private static async Task T6_InProcMcp_RoutesAndReportsUnknownTool()
    {
        var client = new InProcMcpClient(
            new IMcpToolHandler[] { new EchoTool() },
            NullLogger<InProcMcpClient>.Instance);

        Assert(client.ListTools().Count == 1, "T6 manifest 应含 1 个工具");

        var ok = await client.CallToolAsync("test.echo", """{"msg":"hi"}""");
        Assert(ok.IsSuccess && ok.ContentJson.Contains("hi"), "T6 已注册工具应路由成功");

        var unknown = await client.CallToolAsync("test.missing", "{}");
        Assert(!unknown.IsSuccess && unknown.Error!.Contains("test.missing"), "T6 未知工具应返回失败态而非抛异常");

        var thrown = await client.CallToolAsync("test.echo", "THROW");
        Assert(!thrown.IsSuccess, "T6 工具异常应转译为失败态");
    }

    // ── 测试替身 ──

    private static SkillContext BuildContext() => new()
    {
        RunId = "run-r0",
        TenantId = "tenant-r0",
        ProjectId = "project-r0",
        PipelineId = 1,
        UserRequirement = "R0 契约铸造自检",
    };

    /// <summary>兵器库空壳——R0 契约测试不触达 LLM/DB。</summary>
    private sealed class FakeToolkit : ICognitiveSkillToolkit
    {
        public ILlmGatewayService Llm => null!;
        public IMcpClient Mcp => null!;
        public IEventStream Events => null!;
        public IExperienceRecorder Experience => null!;
    }

    private class FakeCognitiveSkill : CognitiveSkill
    {
        public FakeCognitiveSkill() : base(new FakeToolkit()) { }

        public override string SkillId => "fake-cognitive-skill";
        public override string Version => "0.1.0";
        public override SkillLayer Layer => SkillLayer.Decision;
        public override SkillMission Mission => SkillMission.DefineBoundary;

        public override SkillInformationNeeds InformationNeeds { get; } = new();

        public override SkillOutputDeclaration Outputs { get; } = new()
        {
            IrEventTypes = new[] { IrEventTypes.SkeletonCreated },
        };

        protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
            SkillPerception perception,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.Yield();
            yield return new AppendIrEventRequest { EventType = IrEventTypes.SkeletonCreated };
            yield return new AppendIrEventRequest
            {
                EventType = IrEventTypes.SkeletonCreated,
                SkillId = "manual-override",
            };
        }
    }

    private sealed class NeedsSkeletonSkill : FakeCognitiveSkill
    {
        public override SkillInformationNeeds InformationNeeds { get; } = new()
        {
            IrFragmentTypes = new[] { IrFragmentTypes.Skeleton },
            RequiredStability = IrStabilityStates.Stable,
        };
    }

    private sealed class EchoTool : IMcpToolHandler
    {
        public McpToolDescriptor Descriptor { get; } = new()
        {
            Name = "test.echo",
            Description = "回显参数",
        };

        public Task<McpToolResult> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            if (argumentsJson == "THROW") throw new InvalidOperationException("boom");
            return Task.FromResult(McpToolResult.Ok(argumentsJson));
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"[cognitive-r0] {message}");
    }
}
