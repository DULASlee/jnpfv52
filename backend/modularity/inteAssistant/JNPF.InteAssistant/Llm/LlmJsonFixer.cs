using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JNPF.InteAssistant.Llm;

/// <summary>
/// LLM JSON 响应修复器。
/// 处理 LLM 常见输出格式问题：markdown 代码块包裹、尾部逗号、未闭合括号、截断。
/// 纯字符串操作，无 LLM 调用。
/// </summary>
public static class LlmJsonFixer
{
    // 匹配 ```json ... ``` 或 ``` ... ``` 代码块
    private static readonly Regex MarkdownCodeBlockRegex = new(
        @"```(?:json)?\s*\n?([\s\S]*?)\n?```",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // 尾部逗号（在 } 或 ] 之前）
    private static readonly Regex TrailingCommaRegex = new(
        @",(\s*[}\]])",
        RegexOptions.Compiled);

    /// <summary>
    /// 尝试修复常见 JSON 格式问题。
    /// 返回 (修复后文本, 是否做了修复)。修复失败时 Fixed 为 null。
    /// </summary>
    public static (string? Fixed, bool WasFixed) TryFix(string rawContent)
        => TryFixInternal(rawContent);

    /// <summary>
    /// ParseResponse 前置修复入口（26 号 §12.4 契约）。
    /// 对有效 JSON 直接原样返回（不做改动）；对带 prose/markdown/尾逗号包裹的文本修复后返回；
    /// 修复失败（无法解析）返回 null，调用方可保留原文继续走原有解析逻辑。
    /// </summary>
    public static string? FixJsonResponse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // 已经是合法 JSON → 原样返回，不做任何改动（不破坏正常路径）
        if (TryParseJson(raw.Trim(), out _))
            return raw;

        // 不合法 → 尝试修复
        var (fixedJson, wasFixed) = TryFixInternal(raw);
        return wasFixed ? fixedJson : null;
    }

    private static (string? Fixed, bool WasFixed) TryFixInternal(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return (null, false);

        var trimmed = rawContent.Trim();

        // Step 1: 提取 markdown 代码块
        var (extracted, wasExtracted) = TryExtractMarkdownBlock(trimmed);
        if (wasExtracted && extracted != null)
        {
            if (TryParseJson(extracted, out _))
                return (extracted, true);
            // 提取成功了但 JSON 仍不合法，继续后续修复
            trimmed = extracted;
        }

        // Step 1.5: 取首个 { 到末个 }（剥除 JSON 前后多余文本/解释性 prose）
        // 若直接合法则立即返回；否则把提取后的子串交给后续步骤。
        var (sliced, wasSliced) = ExtractJsonObject(trimmed);
        if (wasSliced && sliced != null)
        {
            if (TryParseJson(sliced, out _))
                return (sliced, true);
            trimmed = sliced; // 缩小范围后继续修复
        }

        // Step 2: 平衡括号/大括号
        var (balanced, wasBalanced) = BalanceBrackets(trimmed);
        if (wasBalanced && balanced != trimmed)
        {
            if (TryParseJson(balanced, out _))
                return (balanced, true);
            trimmed = balanced;
        }

        // Step 3: 移除尾部逗号
        var noTrailing = RemoveTrailingCommas(trimmed);
        if (noTrailing != trimmed)
        {
            if (TryParseJson(noTrailing, out _))
                return (noTrailing, true);
            trimmed = noTrailing;
        }

        // Step 4: 截断字符串修复
        var (fixed_, wasFixed_) = FixTruncatedString(trimmed);
        if (wasFixed_ && fixed_ != null)
        {
            if (TryParseJson(fixed_, out _))
                return (fixed_, true);
        }

        // 综合修复：按顺序全部应用
        var combined = trimmed;
        combined = BalanceBracketsOnly(combined);
        combined = RemoveTrailingCommas(combined);
        if (combined != rawContent.Trim() && TryParseJson(combined, out _))
            return (combined, true);

        return (null, false);
    }

    // ─── 私有辅助 ───

    /// <summary>尝试从 markdown 代码块中提取 JSON。</summary>
    private static (string? Content, bool Found) TryExtractMarkdownBlock(string content)
    {
        var match = MarkdownCodeBlockRegex.Match(content);
        if (!match.Success)
            return (null, false);

        var extracted = match.Groups[1].Value.Trim();
        return string.IsNullOrEmpty(extracted) ? (null, false) : (extracted, true);
    }

    /// <summary>
    /// 从混杂文本中提取首个完整 JSON 对象/数组（首个 { 或 [ 到其匹配的闭合符）。
    /// 使用括号深度计数 + 字符串感知（忽略字符串内的括号与转义），避免错误截断嵌套结构。
    /// 仅当首个 {/[ 之前存在非空白字符（说明有 prose 前缀）时才需要切片；
    /// 若首尾已紧贴 JSON 边界则返回 (原文, false) 表示无需修改。
    /// </summary>
    private static (string? Sliced, bool Changed) ExtractJsonObject(string content)
    {
        if (string.IsNullOrEmpty(content))
            return (null, false);

        // 定位首个 { 或 [
        var realOpenIdx = -1;
        var openCh = '\0';
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '{' || content[i] == '[')
            {
                realOpenIdx = i;
                openCh = content[i];
                break;
            }
        }
        if (realOpenIdx < 0)
            return (null, false);

        // 判断是否需要切片：首个开括号前有非空白 → 有 prose 前缀
        var hasPrefix = false;
        for (int i = 0; i < realOpenIdx; i++)
        {
            if (!char.IsWhiteSpace(content[i])) { hasPrefix = true; break; }
        }

        // 字符串感知深度计数，找匹配的闭合符
        var closeCh = openCh == '{' ? '}' : ']';
        var depth = 0;
        var inStr = false;
        var escaped = false;
        var closeIdx = -1;

        for (int i = realOpenIdx; i < content.Length; i++)
        {
            var ch = content[i];
            if (escaped) { escaped = false; continue; }
            if (inStr)
            {
                if (ch == '\\') { escaped = true; continue; }
                if (ch == '"') inStr = false;
                continue;
            }
            if (ch == '"') { inStr = true; continue; }
            if (ch == openCh) depth++;
            else if (ch == closeCh)
            {
                depth--;
                if (depth == 0) { closeIdx = i; break; }
            }
        }

        if (closeIdx < 0)
            return (null, false);

        // 判断闭合符之后是否有尾随非空白 → 有 prose 后缀
        var hasSuffix = false;
        for (int i = closeIdx + 1; i < content.Length; i++)
        {
            if (!char.IsWhiteSpace(content[i])) { hasSuffix = true; break; }
        }

        if (!hasPrefix && !hasSuffix)
            return (null, false); // 已紧贴边界，无需切片

        return (content.Substring(realOpenIdx, closeIdx - realOpenIdx + 1), true);
    }

    /// <summary>补全未闭合的括号/大括号。</summary>
    private static (string Result, bool Changed) BalanceBrackets(string content)
    {
        int braceDepth = 0;
        int bracketDepth = 0;
        bool inString = false;
        bool escaped = false;

        foreach (var ch in content)
        {
            if (escaped) { escaped = false; continue; }
            if (ch == '\\' && inString) { escaped = true; continue; }
            if (ch == '"') { inString = !inString; continue; }
            if (inString) continue;

            switch (ch)
            {
                case '{': braceDepth++; break;
                case '}': braceDepth--; break;
                case '[': bracketDepth++; break;
                case ']': bracketDepth--; break;
            }
        }

        // 如果字符串内未闭合，先补引号
        if (inString)
        {
            content += "\"";
        }

        var changed = false;
        var sb = new StringBuilder(content);

        // 补缺失的闭合符（后进先出）
        while (bracketDepth > 0) { sb.Append(']'); bracketDepth--; changed = true; }
        while (braceDepth > 0) { sb.Append('}'); braceDepth--; changed = true; }

        // 多余的闭合符不处理（说明 JSON 结构根本错了）

        return changed ? (sb.ToString(), true) : (content, false);
    }

    /// <summary>仅平衡括号（不检查是否成功修复）。</summary>
    private static string BalanceBracketsOnly(string content)
    {
        var (result, _) = BalanceBrackets(content);
        return result;
    }

    /// <summary>移除尾部逗号（在 } 或 ] 之前）。</summary>
    private static string RemoveTrailingCommas(string content)
    {
        var result = TrailingCommaRegex.Replace(content, "$1");
        // 多次迭代以处理连续尾部逗号
        var prev = content;
        while (result != prev)
        {
            prev = result;
            result = TrailingCommaRegex.Replace(result, "$1");
        }
        return result;
    }

    /// <summary>修复截断的字符串：检测并关闭未闭合的引号。</summary>
    private static (string? Result, bool WasFixed) FixTruncatedString(string content)
    {
        // 识别最后一个完整的 JSON 结构位置
        // 策略：从末尾向前找最后一个合法的 } 或 ]，截断到此处再平衡
        var trimmed = content.TrimEnd();

        // 如果末尾已经是 } 或 ]，大概率不是截断问题
        if (trimmed.EndsWith('}') || trimmed.EndsWith(']'))
            return (null, false);

        // 尝试：找到最后一个逗号后的未闭合字符串，关闭它
        // 简化为：在末尾补 "}] 或 "} 来闭合
        // 更实际的策略：找最后一个合法的键值对末尾，截断+闭合

        var lastBrace = trimmed.LastIndexOf('}');
        var lastBracket = trimmed.LastIndexOf(']');
        var lastComma = trimmed.LastIndexOf(',');

        // 如果最后是逗号，移除它并闭合
        if (lastComma > Math.Max(lastBrace, lastBracket))
        {
            var beforeComma = trimmed[..lastComma].TrimEnd();
            return (beforeComma + "}]", true);
        }

        // 如果截断在字符串中间（有未闭合引号），补引号+闭合
        var quoteCount = 0;
        bool inStr = false, esc = false;
        foreach (var ch in trimmed)
        {
            if (esc) { esc = false; continue; }
            if (ch == '\\') { esc = true; continue; }
            if (ch == '"') inStr = !inStr;
        }

        if (inStr)
        {
            return (trimmed + "\"}]", true);
        }

        return (null, false);
    }

    /// <summary>安全尝试解析 JSON，返回是否成功。</summary>
    private static bool TryParseJson(string content, out JsonDocument? doc)
    {
        try
        {
            doc = JsonDocument.Parse(content);
            return true;
        }
        catch
        {
            doc = null;
            return false;
        }
    }
}
