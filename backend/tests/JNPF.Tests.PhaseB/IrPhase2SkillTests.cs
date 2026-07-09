using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Skills.Cognitive;
using JNPF.InteAssistant.Skills.Cognitive.Mcp;
using Microsoft.Extensions.Configuration;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 阶段二 Skill Harness 单元测试（P2-Q01 子集）
/// </summary>
public static class IrPhase2SkillTests
{
    private static void TestEventSpecRevisionPlanner()
    {
        var affected = EventSpecRevisionPlanner.GetAffectedSteps(EventSpecRevisionPlanner.FieldTypeOrConstraint);
        if (affected.Count != 2
            || !affected.Contains("CommandQuery")
            || !affected.Contains("DataModel"))
            throw new InvalidOperationException("FieldTypeOrConstraint mapping failed");

        var trimmed = EventSpecRevisionPlanner.TrimCompletedSteps(
            IrSaSteps.All,
            affected);
        if (trimmed.Count != IrSaSteps.All.Length - 2
            || trimmed.Contains("CommandQuery")
            || trimmed.Contains("DataModel"))
            throw new InvalidOperationException("TrimCompletedSteps failed");

        if (!EventSpecRevisionPlanner.IsKnownRevisionType("fieldTypeOrConstraint"))
            throw new InvalidOperationException("IsKnownRevisionType failed");
    }

    private static void TestSaStepMapping()
    {
        if (SaStepMapping.IrStepOrder.Count != 9)
            throw new InvalidOperationException("SaStepMapping should have 9 IR steps");

        if (SaStepMapping.ToAgentName("CommandQuery") != "DictAgent")
            throw new InvalidOperationException("CommandQuery → DictAgent failed");

        if (SaStepMapping.ToIrStepName("ERAgent") != "DataModel")
            throw new InvalidOperationException("ERAgent → DataModel failed");

        if (SaStepMapping.ToAgentName("UnknownStep") != "UnknownStep")
            throw new InvalidOperationException("Unknown step passthrough failed");
    }

    public static void RunAll()
    {
        TestSkillRunGuard_Mutex();
        TestDomainSeedService_Scoring();
        TestPmSkill_ValidateOutput();
        TestCompletenessGate_ExcludesCurrentRun();
        TestEventSpecRevisionPlanner();
        TestSaStepMapping();
        TestTenantPipelineQuotaGuard();
        Console.WriteLine("[Phase2] All skill tests passed.");
    }

    private static void TestTenantPipelineQuotaGuard()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StudioRuntime:MaxConcurrentPipelinesPerTenant"] = "3",
            })
            .Build();
        var guard = new TenantPipelineQuotaGuard(config);

        guard.TryAcquire("1", 101, out _, out _);
        guard.TryAcquire("1", 102, out _, out _);
        guard.TryAcquire("1", 103, out _, out _);
        var blocked = guard.TryAcquire("1", 104, out var reason, out var activeIds);

        guard.Release("1", 101);
        guard.Release("1", 102);
        guard.Release("1", 103);

        if (blocked || string.IsNullOrWhiteSpace(reason) || activeIds.Count != 3)
            throw new InvalidOperationException("TenantPipelineQuotaGuard quota failed");

        if (!guard.TryAcquire("1", 104, out _, out _))
            throw new InvalidOperationException("TenantPipelineQuotaGuard release failed");
        guard.Release("1", 104);
    }

    private static void TestSkillRunGuard_Mutex()
    {
        var guard = new SkillRunGuard();
        var ok1 = guard.TryAcquire("t1", 100, "pm-skill", "run-a", out _);
        var ok2 = guard.TryAcquire("t1", 100, "pm-skill", "run-b", out var conflict);
        guard.Release("t1", 100, "pm-skill");
        var ok3 = guard.TryAcquire("t1", 100, "pm-skill", "run-c", out _);

        if (!ok1 || ok2 || !ok3 || conflict != "run-a")
            throw new InvalidOperationException("SkillRunGuard mutex failed");
        guard.Release("t1", 100, "pm-skill");
    }

    private static void TestDomainSeedService_Scoring()
    {
        var seeds = new List<SeedTemplateMatch>
        {
            new() { EventNamePattern = "请假", CoverageScore = 0.9m },
            new() { EventNamePattern = "报销", CoverageScore = 0.8m },
        };
        var service = new DomainSeedService(null!);
        var score = service.ScoreCandidate("{\"businessEvents\":[{\"eventName\":\"请假申请\"}]}", seeds);
        if (score <= 0)
            throw new InvalidOperationException("DomainSeedService scoring failed");
    }

    private static void TestPmSkill_ValidateOutput()
    {
        var pm = new PmSkillService(new FakePmToolkit(), null!);
        var result = pm.ValidateOutputAsync(new[]
        {
            new JNPF.InteAssistant.Entitys.Dto.Ir.AppendIrEventRequest
            {
                EventType = JNPF.InteAssistant.Entitys.Ir.IrEventTypes.SkeletonCreated,
                Payload = "{}",
            },
        }).GetAwaiter().GetResult();

        if (!result.IsValid)
            throw new InvalidOperationException("PmSkill ValidateOutput failed");
    }

    private sealed class FakePmToolkit : ICognitiveSkillToolkit
    {
        public ILlmGatewayService Llm => null!;
        public IMcpClient Mcp => null!;
        public IEventStream Events => null!;
        public IExperienceRecorder Experience => null!;
    }

    private static void TestCompletenessGate_ExcludesCurrentRun()
    {
        var gate = new JNPF.InteAssistant.Ir.AnalysisCompletedCompletenessGate(null!, Microsoft.Extensions.Options.Options.Create(new SaPipelineOptions()));
        var snapshot = new IrSnapshot
        {
            Fragments = new List<IrSnapshotFragment>
            {
                new()
                {
                    FragmentId = "skeleton:SK-001",
                    FragmentType = JNPF.InteAssistant.Entitys.Ir.IrFragmentTypes.Skeleton,
                    StabilityState = JNPF.InteAssistant.Entitys.Ir.IrStabilityStates.Stable,
                    Payload = """{"businessEvents":[{"eventId":"BE-001"}]}""",
                },
                new()
                {
                    FragmentId = "eventspec:BE-001",
                    FragmentType = JNPF.InteAssistant.Entitys.Ir.IrFragmentTypes.EventSpec,
                    StabilityState = JNPF.InteAssistant.Entitys.Ir.IrStabilityStates.Stable,
                    SaStepsCompleted = JNPF.InteAssistant.Entitys.Ir.IrSaSteps.All,
                    Payload = """{"saStepsCompleted":["DomainModel","AggregateDesign","EventCatalog","CommandQuery","IntegrationPoints","WorkflowSpec","UISpec","DataModel","DeliveryChecklist"]}""",
                },
            },
        };

        // excludeRunId 传入时不应因「当前 run 仍在 running」误杀（DB 为 null 时只测 snapshot 逻辑）
        var result = gate.ValidateAsync("t1", "100", snapshot, excludeRunId: "run-x").GetAwaiter().GetResult();
        if (!result.IsValid)
            throw new InvalidOperationException($"CompletenessGate failed: {result.ErrorMessage}");
    }
}
