namespace JNPF.InteAssistant.Skills;

/// <summary>
/// ArchGuard Critical 或编排链中断（A3）— Harness 捕获后 run failed，禁止启动下游 Skill。
/// </summary>
public sealed class AbortSkillChainException : Exception
{
    public AbortSkillChainException(string message, string phase = "ArchAbort")
        : base(message)
    {
        Phase = phase;
    }

    public string Phase { get; }
}
