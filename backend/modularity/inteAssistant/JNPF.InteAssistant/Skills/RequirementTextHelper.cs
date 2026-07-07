namespace JNPF.InteAssistant.Skills;

/// <summary>
/// PM/Analyst 长文档输入裁剪（P0：万字需求不丢首尾语义）。
/// </summary>
public static class RequirementTextHelper
{
    public const int PmPromptMaxChars = 24_000;
    public const int HeadChars = 12_000;
    public const int TailChars = 12_000;

    public static string ForPmPrompt(SkillContext context)
    {
        var text = context.UserRequirement?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (text.Length <= PmPromptMaxChars)
            return text;

        var head = text[..HeadChars];
        var tail = text[^TailChars..];
        return $"""
            {head}

            …【需求文本已截断：共 {text.Length} 字，中间部分省略，保留首尾各 {HeadChars} 字】…

            {tail}
            """;
    }
}
