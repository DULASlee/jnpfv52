using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Constraints;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.FriendlyException;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 总体设计 Skill（P3-B05）— 三片段 stable 后 SystemDesignLocked；critical 约束违规时拒绝锁定。
/// </summary>
public sealed class SystemDesignSkillService : IBaseSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IConstraintEngineService _constraintEngine;
    private readonly ILogger<SystemDesignSkillService> _logger;

    public SystemDesignSkillService(
        IConstraintEngineService constraintEngine,
        ILogger<SystemDesignSkillService> logger)
    {
        _constraintEngine = constraintEngine;
        _logger = logger;
    }

    public string SkillId => DesignSkillIds.SystemDesign;
    public string Version => "1.0.0-mvp";

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[]
        {
            IrFragmentTypes.Architecture,
            IrFragmentTypes.DDL,
            IrFragmentTypes.FormPageIR,
        },
        RequiredStability = IrStabilityStates.Stable,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[]
        {
            IrEventTypes.SystemDesignLocked,
            IrEventTypes.ConstraintViolationReported,
        },
    };

    public Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("架构片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("DDL 片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("FormPageIR 片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.SystemDesign, IrStabilityStates.Locked) != null)
            return Task.FromResult(SkillValidationResult.Fail("SystemDesign 已 locked"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var fragmentId = $"systemDesign:{context.ProjectId}";
        var arch = context.Snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable)!;
        var ddl = context.Snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable)!;
        var ui = context.Snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable)!;

        var check = _constraintEngine.Evaluate(context.Snapshot);
        _logger.LogInformation(
            "SystemDesign 约束校验 project={ProjectId} critical={Critical} warning={Warning}",
            context.ProjectId, check.CriticalCount, check.WarningCount);

        if (check.Violations.Count > 0)
        {
            yield return BuildViolationEvent(context.ProjectId, check);
        }

        if (check.CriticalCount > 0)
        {
            throw Oops.Bah($"存在 {check.CriticalCount} 条 critical 约束违规，SystemDesignLocked 已拒绝");
        }

        var payload = JsonSerializer.Serialize(new
        {
            @context = "https://schema.jnpf.ai/ir/v1",
            @id = fragmentId,
            lockedAt = DateTime.UtcNow.ToString("O"),
            references = new
            {
                architectureFragmentId = arch.FragmentId,
                ddlFragmentId = ddl.FragmentId,
                formPageFragmentId = ui.FragmentId,
            },
            consistencyChecks = new object[]
            {
                new { check = "fragments-present", passed = true },
                new { check = "constraint-engine", passed = true, warningCount = check.WarningCount },
            },
            stabilityState = IrStabilityStates.Locked,
        }, JsonOptions);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.SystemDesignLocked,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.SystemDesign,
            FragmentVersion = 1,
            Payload = payload,
            SkillId = SkillId,
        };

        await Task.CompletedTask;
    }

    public Task<SkillValidationResult> ValidateOutputAsync(IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Any(e => e.EventType == IrEventTypes.SystemDesignLocked))
            return Task.FromResult(SkillValidationResult.Ok());

        if (events.Any(e => e.EventType == IrEventTypes.ConstraintViolationReported))
            return Task.FromResult(SkillValidationResult.Fail("critical 约束违规，未产出 SystemDesignLocked"));

        return Task.FromResult(SkillValidationResult.Fail("必须产出 SystemDesignLocked 或 ConstraintViolationReported"));
    }

    private static AppendIrEventRequest BuildViolationEvent(string projectId, ConstraintCheckResult check)
    {
        var payload = JsonSerializer.Serialize(new
        {
            checkedAt = DateTime.UtcNow.ToString("O"),
            criticalCount = check.CriticalCount,
            warningCount = check.WarningCount,
            violations = check.Violations.Select(v => new
            {
                v.RuleId,
                v.Severity,
                v.Message,
                v.FragmentType,
                v.FragmentId,
            }),
        }, JsonOptions);

        return new AppendIrEventRequest
        {
            EventType = IrEventTypes.ConstraintViolationReported,
            FragmentId = $"constraints:{projectId}",
            FragmentType = "IR2_ConstraintReport",
            FragmentVersion = 1,
            Payload = payload,
            SkillId = DesignSkillIds.SystemDesign,
        };
    }
}
