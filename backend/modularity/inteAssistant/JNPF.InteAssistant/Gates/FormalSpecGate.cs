using JNPF.InteAssistant.Entitys.Dto.Skills;

namespace JNPF.InteAssistant.Gates;

/// <summary>02 正式版 Markdown 硬校验（与 RequirementDocumentRenderer / 前端 requirementSpec.ts 对齐）。</summary>
public static class FormalSpecGate
{
    public static FormalSpecGateResult Validate(string? markdown)
    {
        var violations = new List<string>();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            violations.Add("正文为空");
            return new FormalSpecGateResult { IsValid = false, Violations = violations };
        }

        if (!markdown.Contains(RequirementSpecConstants.FormalTitleMarker, StringComparison.Ordinal))
            violations.Add($"缺少封面标题「{RequirementSpecConstants.FormalTitleMarker}」");

        if (!markdown.Contains(RequirementSpecConstants.FormalCtaMarker, StringComparison.Ordinal))
            violations.Add($"缺少确认 CTA「{RequirementSpecConstants.FormalCtaMarker}」");

        return new FormalSpecGateResult
        {
            IsValid = violations.Count == 0,
            Violations = violations,
        };
    }
}
