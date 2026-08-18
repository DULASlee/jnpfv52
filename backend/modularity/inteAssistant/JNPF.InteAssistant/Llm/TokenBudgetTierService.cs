namespace JNPF.InteAssistant.Llm;

/// <summary>
/// P6-L01 Token Budget 四级降级 tier 计算（纯函数，无副作用）。
///
/// tier 状态机：
///   green:  consumed &lt; 70% budget
///   yellow: 70% ≤ consumed &lt; 95%（预警，不阻断）
///   red:    95% ≤ consumed &lt; 100%（strong Skill 降级为 fast provider）
///   fuse:   consumed ≥ 100%（硬熔断，拒绝新 LLM 调用）
/// </summary>
public static class TokenBudgetTierService
{
    public const string Green = "green";
    public const string Yellow = "yellow";
    public const string Red = "red";
    public const string Fuse = "fuse";

    /// <summary>Yellow 阈值（consumed/budget ≥ 此值触发预警）。</summary>
    public const double YellowThreshold = 0.70;

    /// <summary>Red 阈值（strong→fast 降级触发点）。</summary>
    public const double RedThreshold = 0.95;

    /// <summary>
    /// 根据已消耗 token 与预算计算 budget tier。
    /// </summary>
    public static string ComputeTier(long tokenConsumed, long tokenBudget)
    {
        if (tokenBudget <= 0)
            return tokenConsumed > 0 ? Fuse : Green;

        var ratio = (double)tokenConsumed / tokenBudget;

        if (ratio >= 1.0) return Fuse;
        if (ratio >= RedThreshold) return Red;
        if (ratio >= YellowThreshold) return Yellow;
        return Green;
    }

    /// <summary>tier 是否允许 LLM 调用（fuse 拒绝，其余放行）。</summary>
    public static bool CanCall(string tier) => tier != Fuse;

    /// <summary>tier 是否需要将 strong provider 降级为 fast（red/fuse 触发）。</summary>
    public static bool ShouldDegradeToFast(string tier) => tier is Red or Fuse;

    /// <summary>所有合法 tier 值（用于校验/文档）。</summary>
    public static readonly string[] All = { Green, Yellow, Red, Fuse };
}
