using System.Text;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 领域知识渲染器 — 把检索到的种子压缩成简洁的参考文本注入 PM prompt。
/// 查不到返回空字符串（零 token 消耗）；查到了最多 3 条 × 200 字。
/// </summary>
public static class DomainKnowledgeRenderer
{
    private const int MaxSeeds = 3;
    private const int MaxCharsPerSeed = 200;

    /// <summary>渲染整体方案知识（用于 EnhanceRequirement / RefineFromAnalysis）。</summary>
    public static string Render(IReadOnlyList<SeedTemplateMatch> seeds)
    {
        if (seeds == null || seeds.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("参考方案（历史积累，仅供参考不要照抄）：");
        foreach (var s in seeds.Take(MaxSeeds))
            sb.AppendLine($"- {s.Industry}/{s.EventNamePattern}: {Truncate(s.TemplateJson, MaxCharsPerSeed)}");
        return sb.ToString();
    }

    /// <summary>渲染规则知识（用于 EnhancePspecDecisionTable）。</summary>
    public static string RenderRules(IReadOnlyList<SeedTemplateMatch> seeds)
    {
        if (seeds == null || seeds.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("规则参考（历史积累）：");
        foreach (var s in seeds.Take(MaxSeeds))
            sb.AppendLine($"- {s.EventNamePattern}: {Truncate(s.TemplateJson, MaxCharsPerSeed)}");
        return sb.ToString();
    }

    /// <summary>渲染易错点知识（用于 GenerateClarification 出题）。</summary>
    public static string RenderPitfalls(IReadOnlyList<SeedTemplateMatch> seeds)
    {
        if (seeds == null || seeds.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("此类系统的常见易错点（出题时重点关注）：");
        foreach (var s in seeds.Take(MaxSeeds))
            sb.AppendLine($"- {s.EventNamePattern}: {Truncate(s.TemplateJson, MaxCharsPerSeed)}");
        return sb.ToString();
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max] + "…");
}
