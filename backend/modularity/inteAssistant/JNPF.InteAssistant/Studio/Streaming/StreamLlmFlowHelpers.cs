using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Gates;

namespace JNPF.InteAssistant.Studio.Streaming;

/// <summary>
/// Vision multi-modal gate for LEGACY_REQUIREMENT_GATE_FLOW (config + image presence).
/// </summary>
public enum VisionExtractionDecision
{
    SkipNoImages = 0,
    SkipNotConfigured = 1,
    Run = 2,
}

/// <summary>
/// Pure helpers extracted from StreamLlmResponseAsync (W-续).
/// Boundary: legacy requirement SSE gate stays in AIDevelopmentPipelineService;
/// PM main chain remains SkillsApiService requirement-analysis run.
/// </summary>
public static class StreamLlmFlowHelpers
{
    /// <summary>
    /// Keep only user/assistant roles for LLM chat context.
    /// </summary>
    public static List<ChatMessage> ToChatMessages(
        IEnumerable<(string Role, string Content)> history)
    {
        return history
            .Where(x => x.Role is "user" or "assistant")
            .Select(x => new ChatMessage(x.Role, x.Content))
            .ToList();
    }

    /// <summary>
    /// Relative attachment URLs need request base; absolute http(s) pass through.
    /// </summary>
    public static string ResolveAttachmentDownloadUrl(string fileUrl, string? baseUrl, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new InvalidOperationException("附件 URL 为空");

        if (fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return fileUrl;

        if (string.IsNullOrEmpty(baseUrl))
        {
            var suffix = string.IsNullOrEmpty(fileName) ? string.Empty : $"，跳过附件 {fileName}";
            throw new InvalidOperationException($"无法解析附件下载基 URL（ctx.Host 为空）{suffix}");
        }

        return $"{baseUrl}{fileUrl}";
    }

    /// <summary>
    /// Append text to the last user message (attachment / vision inject).
    /// </summary>
    public static bool AppendToLastUserMessage(IList<ChatMessage> chatMessages, string text)
    {
        if (chatMessages == null || string.IsNullOrWhiteSpace(text))
            return false;

        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            if (chatMessages[i].Role == "user")
            {
                chatMessages[i] = new ChatMessage("user", chatMessages[i].Content + text);
                return true;
            }
        }

        return false;
    }

    public static int ClampClarificationMaxRounds(int? configured)
    {
        var maxRounds = configured ?? 7;
        if (maxRounds < 1) maxRounds = 7;
        if (maxRounds > 20) maxRounds = 20;
        return maxRounds;
    }

    public static int ComputeClarificationRound(int assistantMsgCount, int maxRounds)
        => Math.Min(assistantMsgCount / 2 + 1, maxRounds);

    /// <summary>
    /// Rough token estimate used for stream budget accumulate (chars/4).
    /// </summary>
    public static (int Input, int Output, int Total) EstimateStreamTokens(
        int inputCharCount,
        int outputCharCount)
    {
        var input = inputCharCount / 4;
        var output = outputCharCount / 4;
        return (input, output, input + output);
    }

    public static string FormatMaturityModeLabel(string? mode) => mode switch
    {
        "explore" => "探索模式 — 需要补充更多信息",
        "confirm" => "确认模式 — 需要确认部分细节",
        "refine" => "精化模式 — 开始深度分析",
        _ => mode ?? string.Empty,
    };

    /// <summary>
    /// Gateway ChatStreamAsync error sentinel lines.
    /// </summary>
    public static bool IsGatewayStreamError(string? json)
        => !string.IsNullOrEmpty(json)
           && (json.StartsWith("[ERROR]", StringComparison.Ordinal)
               || json.StartsWith("[error]", StringComparison.Ordinal));

    public static bool HasImageFileNames(IEnumerable<string?>? fileNames)
        => fileNames?.Any(f => f != null && GateConstants.IsImageFile(f)) ?? false;

    public static VisionExtractionDecision DecideVisionExtraction(
        bool hasImageAttachments,
        string? apiUrl,
        string? apiKey)
    {
        if (!hasImageAttachments)
            return VisionExtractionDecision.SkipNoImages;
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiUrl))
            return VisionExtractionDecision.SkipNotConfigured;
        return VisionExtractionDecision.Run;
    }

    /// <summary>Legacy StreamLlm ChatCompletionRequest defaults (MaxTokens/Temperature/Retries/Timeout).</summary>
    public static ChatCompletionRequest BuildDefaultStreamRequest(
        string provider,
        string systemPrompt,
        List<ChatMessage> messages)
        => new()
        {
            ProviderCode = provider,
            SystemPrompt = systemPrompt,
            Messages = messages,
            MaxTokens = 4096,
            Temperature = 0.7,
            MaxRetries = 2,
            TimeoutMs = 120000,
        };

    public static string FormatLlmStreamFailure(Exception ex)
        => ex.InnerException != null
            ? $"LLM 调用失败: {ex.Message} (Inner: {ex.InnerException.Message})"
            : $"LLM 调用失败: {ex.Message}";

    public static bool ShouldUploadDevelopmentArtifacts(string? stageName)
        => string.Equals(stageName, PipelineStage.Development, StringComparison.Ordinal);

    public static bool ShouldAccumulateEstimatedTokens(int estimatedTotal, string? tenantId)
        => estimatedTotal > 0 && !string.IsNullOrEmpty(tenantId);

    public static string PrefixVisionAnalysis(string imageAnalysis)
        => "\n\n" + imageAnalysis;

    /// <summary>
    /// Parse SSE/gateway chunk JSON → visible token text (OpenAI-ish + delta.text).
    /// </summary>
    public static string? ExtractToken(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var text))
                return text.GetString();

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("delta", out var delta2) &&
                    delta2.TryGetProperty("content", out var content))
                    return content.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
