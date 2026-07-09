using System.Text.RegularExpressions;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;

namespace JNPF.InteAssistant.Llm;

/// <summary>
/// LLM Token 预估算工具（字符级启发式，不依赖外部 tokenizer）。
/// 用于在发送 HTTP 请求前预估 token 消耗，避免明显超限的请求到达 API 层。
/// </summary>
public static class LlmTokenEstimator
{
    // CJK 统一表意文字 + 扩展 + 兼容 + 标点符号
    private static readonly Regex CjkCharRegex = new(
        @"[\u4e00-\u9fff\u3400-\u4dbf\uf900-\ufaff\u3000-\u303f]",
        RegexOptions.Compiled);

    /// <summary>
    /// 估算单段文本的 token 数。
    /// </summary>
    public static int EstimateTokens(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var totalChars = text.Length;
        if (totalChars == 0) return 0;

        var cjkCount = CjkCharRegex.Matches(text).Count;
        var cjkRatio = (double)cjkCount / totalChars;

        // 混合启发式：CJK 字符 ~2 chars/token，英文 ~4 chars/token
        double charsPerToken;
        if (cjkRatio > 0.5)
            charsPerToken = 2.0;
        else if (cjkRatio < 0.1)
            charsPerToken = 4.0;
        else
            charsPerToken = cjkRatio * 2.0 + (1.0 - cjkRatio) * 4.0;

        var rawEstimate = (int)(totalChars / charsPerToken);
        // 20% 安全余量
        return Math.Max(1, (int)(rawEstimate * 1.2));
    }

    /// <summary>
    /// 估算完整 ChatCompletionRequest 的 token 数（输入 + 期望输出）。
    /// </summary>
    public static int EstimateRequestTokens(ChatCompletionRequest request)
    {
        var total = 0;

        // 系统提示词
        total += EstimateTokens(request.SystemPrompt);

        // 消息历史
        if (request.Messages is { Count: > 0 })
        {
            foreach (var msg in request.Messages)
                total += EstimateTokens(msg.Content);
        }

        // 期望输出（MaxTokens 为上限）
        total += request.MaxTokens;

        return total;
    }

    /// <summary>
    /// 截断消息历史以满足 token 上限（26 号 §12.5 契约）。
    /// 策略：保留 SystemPrompt + 从末尾向前保留尽可能多的消息（最近上下文最相关），
    /// 直到加入下一条消息会超过 tokenBudget 为止。始终保留至少 1 条最新消息。
    /// 返回新的 ChatCompletionRequest（record with-expression，不改原对象）。
    /// </summary>
    /// <param name="request">原始请求。</param>
    /// <param name="tokenBudget">输入侧 token 预算（不含 MaxTokens 输出额度）。</param>
    public static ChatCompletionRequest TruncateForTokenLimit(
        ChatCompletionRequest request, int tokenBudget)
    {
        if (request.Messages is null || request.Messages.Count == 0)
            return request;

        var systemTokens = EstimateTokens(request.SystemPrompt);
        var remaining = tokenBudget - systemTokens;
        if (remaining < 1) remaining = 1;

        // 从末尾向前累加，直到超预算
        var kept = new List<ChatMessage>();
        var used = 0;
        for (int i = request.Messages.Count - 1; i >= 0; i--)
        {
            var msgTokens = EstimateTokens(request.Messages[i].Content);
            if (i < request.Messages.Count - 1 && used + msgTokens > remaining)
                break; // 非首条且会超预算 → 停止
            kept.Insert(0, request.Messages[i]);
            used += msgTokens;
        }

        if (kept.Count == request.Messages.Count)
            return request; // 全部保留，无需截断

        return request with { Messages = kept };
    }
}
