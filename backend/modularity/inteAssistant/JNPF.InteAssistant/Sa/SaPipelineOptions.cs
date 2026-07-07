namespace JNPF.InteAssistant.Sa;

/// <summary>S2 主链配置（Configurations/SaPipeline.json）。</summary>
public sealed class SaPipelineOptions
{
    public const string SectionName = "SaPipeline";

    /// <summary>compile = SaNineViewCompiler（默认）；agent = sa-service LLM 九步（回归对比）。</summary>
    public string S2Mode { get; set; } = "compile";

    public bool IsCompileMode =>
        !string.Equals(S2Mode, "agent", StringComparison.OrdinalIgnoreCase);
}
