using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Constraints;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Configuration;
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

        var skill = new SystemDesignSkillService(new ConstraintEngineService(null!), null!);
        var validation = skill.ValidateInputAsync(snapshot).GetAwaiter().GetResult();
        if (validation.IsValid || !validation.ErrorMessage!.Contains("FormPageIR", StringComparison.Ordinal))
            throw new InvalidOperationException("SystemDesign should reject when FormPageIR fragment is missing");
    }
}
