using System.Text.RegularExpressions;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 从用户原始需求 / 流水线名称提取系统名，生成「{系统名}系统需求分析说明书」标题。
/// </summary>
public static class RequirementTitleHelper
{
    private static readonly Regex SystemSuffixRegex = new(
        @"^(.+?)系统$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BookTitleRegex = new(
        @"[《「]([^》」]{2,40})[》」]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>提取系统简称（不含「系统」后缀）。</summary>
    public static string ExtractSystemName(string? requirementText, string? pipelineTitle = null)
    {
        if (!string.IsNullOrWhiteSpace(pipelineTitle))
        {
            var fromPipeline = NormalizeName(pipelineTitle.Trim());
            if (!string.IsNullOrWhiteSpace(fromPipeline))
                return fromPipeline;
        }

        var text = requirementText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
            return "业务";

        var book = BookTitleRegex.Match(text);
        if (book.Success)
        {
            var name = NormalizeName(book.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }

        var systemMatch = Regex.Match(text, @"([\u4e00-\u9fa5A-Za-z0-9]{2,20})系统");
        if (systemMatch.Success)
            return NormalizeName(systemMatch.Groups[1].Value);

        var firstLine = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(l => l.Length >= 2 && l.Length <= 40);
        if (!string.IsNullOrWhiteSpace(firstLine))
        {
            var cleaned = NormalizeName(firstLine);
            if (cleaned.Length >= 2 && cleaned.Length <= 20)
                return cleaned;
        }

        return "业务";
    }

    public static string BuildDocumentTitle(string systemName)
        => $"{NormalizeName(systemName)}系统需求分析说明书";

    private static string NormalizeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var s = raw.Trim();
        if (SystemSuffixRegex.Match(s) is { Success: true } m)
            s = m.Groups[1].Value.Trim();

        s = s.Trim('《', '》', '「', '」', '【', '】', '[', ']', '(', ')', '（', '）', '"', '\'', ' ');
        return s.Length > 30 ? s[..30] : s;
    }
}
