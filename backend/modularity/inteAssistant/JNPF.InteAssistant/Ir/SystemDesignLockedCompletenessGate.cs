using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;

namespace JNPF.InteAssistant.Ir;

public interface ISystemDesignLockedCompletenessGate
{
    Task<SkillValidationResult> ValidateAsync(IrSnapshot snapshot, CancellationToken ct = default);
}

/// <summary>
/// SystemDesignLocked 前置完整性门禁（P3-R03 / 阶段四 developer-skill 激活条件）
/// </summary>
public sealed class SystemDesignLockedCompletenessGate : ISystemDesignLockedCompletenessGate, ITransient
{
    public Task<SkillValidationResult> ValidateAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("架构片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("DDL 片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("FormPageIR 片段未 stable"));

        if (snapshot.Find(IrFragmentTypes.SystemDesign, IrStabilityStates.Locked) == null)
            return Task.FromResult(SkillValidationResult.Fail("SystemDesign 未 locked"));

        return Task.FromResult(SkillValidationResult.Ok());
    }
}
