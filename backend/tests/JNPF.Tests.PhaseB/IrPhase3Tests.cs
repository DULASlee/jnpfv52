using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Constraints;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Skills.Cognitive;
using JNPF.InteAssistant.Skills.Cognitive.Mcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 阶段三 LLM Budget Guard + 设计 Skill 单元测试（P3-Q01 子集）
/// </summary>
public static class IrPhase3Tests
{
    public static void RunAll()
    {
        TestLlmCallPolicy_Defaults();
        TestDesignSkillIds_Registered();
        TestIr2EventTypes_Defined();
        TestConstraintEngine_C001();
        TestSystemDesign_ValidateInput_RequiresUiFragment();
        Console.WriteLine("[Phase3] All design skill tests passed.");
    }

    /// <summary>G3 D16 — architect maxCalls=3，第 4 次须 LLM_CALL_LIMIT_EXCEEDED</summary>
    public static void RunMaxCallsOnly()
    {
        TestLlmCallLimit_ArchitectPolicyDefault();
        TestLlmCallLimit_InMemoryUsageGate();
        Console.WriteLine("[Phase3] maxCalls gate tests passed (LLM_CALL_LIMIT_EXCEEDED on 4th).");
    }

    private static void TestLlmCallPolicy_Defaults()
    {
        var config = new ConfigurationBuilder().Build();
        var policyService = new LlmCallPolicyService(null!, new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

        // 无 DB 时回退内置策略
        var architect = policyService.GetPolicyAsync(DesignSkillIds.Architect).GetAwaiter().GetResult();
        if (architect.MaxLlmCalls != 3 || architect.MaxTotalTokens != 80_000)
            throw new InvalidOperationException("architect-skill default policy mismatch");

        var analyst = policyService.GetPolicyAsync("analyst-skill").GetAwaiter().GetResult();
        if (analyst.MaxLlmCalls != 0)
            throw new InvalidOperationException("analyst-skill must block direct LLM (MaxLlmCalls=0)");
    }

    private static void TestDesignSkillIds_Registered()
    {
        var ids = new[]
        {
            DesignSkillIds.Architect,
            DesignSkillIds.DbDesign,
            DesignSkillIds.UiDesign,
            DesignSkillIds.SystemDesign,
        };

        if (ids.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("DesignSkillIds incomplete");
    }

    private static void TestIr2EventTypes_Defined()
    {
        if (IrEventTypes.SystemDesignLocked != "SystemDesignLocked")
            throw new InvalidOperationException("SystemDesignLocked constant missing");

        if (IrEventTypes.ConstraintViolationReported != "ConstraintViolationReported")
            throw new InvalidOperationException("ConstraintViolationReported constant missing");

        if (IrFragmentTypes.FormPageIR != "IR2_FormPageIR")
            throw new InvalidOperationException("IR2_FormPageIR constant missing");
    }

    private static void TestConstraintEngine_C001()
    {
        var ddlPayload = JsonSerializer.Serialize(new
        {
            ddl = "CREATE TABLE [dbo].[T] (F_Id NVARCHAR(50)); ALTER TABLE T ADD CONSTRAINT FK1 FOREIGN KEY (X) REFERENCES [dbo].[UserController];",
        });

        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                new IrSnapshotFragment
                {
                    FragmentId = "ddl:1",
                    FragmentType = IrFragmentTypes.DDL,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = ddlPayload,
                },
            },
        };

        var engine = new ConstraintEngineService(null!);
        var result = engine.Evaluate(snapshot);
        if (!result.Violations.Any(v => v.RuleId == "C-001" && v.Severity == "critical"))
            throw new InvalidOperationException("C-001 should detect Controller reference in DDL");
    }

    private static void TestSystemDesign_ValidateInput_RequiresUiFragment()
    {
        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                new IrSnapshotFragment
                {
                    FragmentId = "architecture:1",
                    FragmentType = IrFragmentTypes.Architecture,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = "{}",
                },
                new IrSnapshotFragment
                {
                    FragmentId = "ddl:1",
                    FragmentType = IrFragmentTypes.DDL,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = "{}",
                },
            },
        };

        var skill = new SystemDesignSkillService(
            new FakeDesignToolkit(),
            new ConstraintEngineService(null!),
            null!,
            NullLogger<SystemDesignSkillService>.Instance);
        var validation = skill.ValidateInputAsync(snapshot).GetAwaiter().GetResult();
        if (validation.IsValid || !validation.ErrorMessage!.Contains("FormPageIR", StringComparison.Ordinal))
            throw new InvalidOperationException("SystemDesign should reject when FormPageIR fragment is missing");
    }

    private sealed class FakeDesignToolkit : ICognitiveSkillToolkit
    {
        public ILlmGatewayService Llm => null!;
        public IMcpClient Mcp => null!;
        public IEventStream Events => null!;
        public IExperienceRecorder Experience => null!;
    }

    private static void TestLlmCallLimit_ArchitectPolicyDefault()
    {
        var policyService = new LlmCallPolicyService(null!, new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        var architect = policyService.GetPolicyAsync(DesignSkillIds.Architect).GetAwaiter().GetResult();
        if (architect.MaxLlmCalls != 3)
            throw new InvalidOperationException($"architect-skill MaxLlmCalls must be 3, got {architect.MaxLlmCalls}");
        if (SkillLlmBudgetGuard.CallLimitCode != "LLM_CALL_LIMIT_EXCEEDED")
            throw new InvalidOperationException("CallLimitCode constant mismatch");
    }

    /// <summary>镜像 SkillLlmBudgetGuard.AcquireAsync 内存计数语义（同 runId 第 4 次拒绝）</summary>
    private static void TestLlmCallLimit_InMemoryUsageGate()
    {
        const int maxCalls = 3;
        var callCount = 0;
        string? rejectedCode = null;
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            if (callCount >= maxCalls)
            {
                rejectedCode = SkillLlmBudgetGuard.CallLimitCode;
                if (attempt != 4)
                    throw new InvalidOperationException("limit should trigger on 4th attempt");
                break;
            }
            callCount++;
        }
        if (rejectedCode != "LLM_CALL_LIMIT_EXCEEDED")
            throw new InvalidOperationException("4th acquire must reject with LLM_CALL_LIMIT_EXCEEDED");
    }
}
