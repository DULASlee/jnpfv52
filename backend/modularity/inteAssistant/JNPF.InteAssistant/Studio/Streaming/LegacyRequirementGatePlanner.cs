using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Studio.Streaming;

/// <summary>
/// Pure decision / message helpers for LEGACY_REQUIREMENT_GATE_FLOW inside StreamLlmResponseAsync.
/// I/O (DB, HTTP, LLM maturity eval, IR append, SSE send) stays at the call site.
/// </summary>
public enum LegacyGatePromptBranch
{
    ForceRefine = 0,
    MaxRoundsForceRefine = 1,
    EvaluateMaturity = 2,
}

public enum LegacyClarificationAction
{
    /// <summary>Not explore/confirm — keep streaming with current system prompt.</summary>
    ContinueStream = 0,

    /// <summary>Round capped — switch system prompt to refine and stream.</summary>
    ForceRefineAtCap = 1,

    /// <summary>Emit clarification_requested and end the SSE stream.</summary>
    RequestClarification = 2,
}

public static class LegacyRequirementGatePlanner
{
    public static bool ShouldRunLegacyGate(string? stageName)
        => string.Equals(stageName, PipelineStage.Requirement, StringComparison.Ordinal);

    public static string ComposeFullText(string? lastUserMsg, string? attachmentText)
        => (lastUserMsg ?? string.Empty) + (attachmentText ?? string.Empty);

    public static string FormatHardRuleReject(string? reason, string? hint)
        => $"❌ {reason}\n\n{hint}";

    public static LegacyGatePromptBranch DecidePromptBranch(bool forceRefine, bool maxRoundsReached)
    {
        if (forceRefine) return LegacyGatePromptBranch.ForceRefine;
        if (maxRoundsReached) return LegacyGatePromptBranch.MaxRoundsForceRefine;
        return LegacyGatePromptBranch.EvaluateMaturity;
    }

    public static string FormatForceRefineInfo()
        => "\n\n> 📊 已进入精化模式 — 开始深度分析\n\n";

    public static string FormatMaxRoundsInfo(int assistantMsgCount)
        => $"\n\n> 📊 已进行{assistantMsgCount}轮追问，系统将基于当前信息开始分析\n\n";

    public static string FormatMaturityInfo(int score, string? mode)
    {
        var modeLabel = StreamLlmFlowHelpers.FormatMaturityModeLabel(mode);
        return $"\n\n> 📊 需求成熟度：{score}/100（{modeLabel}）\n\n";
    }

    public static List<string> SummarizeUserContentsForStrengths(
        IEnumerable<ChatMessage> chatMessages,
        int maxLen = 50)
    {
        return chatMessages
            .Where(m => m.Role == "user")
            .Select(m => m.Content.Length > maxLen ? m.Content[..maxLen] + "..." : m.Content)
            .ToList();
    }

    public static LegacyClarificationAction DecideClarificationAction(
        string? maturityMode,
        int clarificationRound,
        int maxRounds)
    {
        if (maturityMode is not ("explore" or "confirm"))
            return LegacyClarificationAction.ContinueStream;

        if (clarificationRound >= maxRounds)
            return LegacyClarificationAction.ForceRefineAtCap;

        return LegacyClarificationAction.RequestClarification;
    }

    public static string BuildClarificationFragmentId(string projectId)
        => $"clarification:{ClarificationStages.Requirement}:{projectId}";
}
