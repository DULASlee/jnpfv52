namespace JNPF.InteAssistant.Sa;

/// <summary>S2 主链配置（Configurations/SaPipeline.json）。</summary>
public sealed class SaPipelineOptions
{
    public const string SectionName = "SaPipeline";

    /// <summary>compile = SaNineViewCompiler（默认）；agent = sa-service LLM 九步（回归对比）。</summary>
    public string S2Mode { get; set; } = "compile";

    public bool IsCompileMode =>
        !string.Equals(S2Mode, "agent", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 是否执行 Round 3 工程接线（投影 + 门禁 + 物化 + 假设落库）。
    /// 默认 true（当前单轮 pipeline）；27 号多轮编排器对 Round 1/2 设为 false。
    /// </summary>
    public bool EnableEngineeringWiring { get; set; } = true;
}
