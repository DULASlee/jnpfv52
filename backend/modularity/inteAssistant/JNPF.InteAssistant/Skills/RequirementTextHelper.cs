using JNPF.FriendlyException;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// PM/Analyst 长文档输入裁剪（P0：万字需求不丢首尾语义）。
/// 大附件场景下再抽一层标题大纲，避免中间业务事件全丢。
/// </summary>
public static class RequirementTextHelper
{
    public const int PmPromptMaxChars = 36_000;
    public const int HeadChars = 16_000;
    public const int TailChars = 14_000;
    public const int OutlineMaxChars = 4_000;

    public static string ForPmPrompt(SkillContext context)
    {
        var text = context.UserRequirement?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
            throw Oops.Bah("PM Skill 缺少用户需求文本——ForPmPrompt 是 PM Skill 必须的输入底色，请先通过 sa-gate 提交需求文档或附件");

        if (text.Length <= PmPromptMaxChars)
            return text;

        var head = text[..HeadChars];
        var tail = text[^TailChars..];
        var outline = ExtractHeadingOutline(text, OutlineMaxChars);
        var outlineBlock = string.IsNullOrWhiteSpace(outline)
            ? ""
            : $"""

               【中间章节标题大纲（自动抽取）】
               {outline}

               """;

        return $"""
            {head}
            {outlineBlock}
            …【需求文本已截断：共 {text.Length} 字，保留首 {HeadChars} + 尾 {TailChars} 字】…

            {tail}
            """;
    }

    /// <summary>从 Markdown/文档标题行抽取大纲，帮助 LLM 在截断后仍看到中间业务主题。</summary>
    internal static string ExtractHeadingOutline(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || maxChars <= 0) return "";
        var lines = new List<string>();
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var t = line.Trim();
            if (t.Length < 2) continue;
            if (t.StartsWith("## ", StringComparison.Ordinal)
                || t.StartsWith("# ", StringComparison.Ordinal)
                || (t.Length <= 40 && (t.EndsWith("：") || t.EndsWith(":"))))
            {
                lines.Add(t.Length > 120 ? t[..120] + "…" : t);
            }
            if (lines.Count >= 80) break;
        }

        if (lines.Count == 0) return "";
        var joined = string.Join("\n", lines);
        return joined.Length <= maxChars ? joined : joined[..maxChars] + "…";
    }
}
