namespace JNPF.InteAssistant.Llm;

/// <summary>
/// 项目级 LLM Token 预算默认值（P3-L01 / P6-L01）。
/// 开发环境默认 500 万；95% 预检阈值由调用方按 budget × 0.95 计算。
/// </summary>
public static class LlmBudgetDefaults
{
    public const long DefaultProjectTokenBudget = 5_000_000L;
}
