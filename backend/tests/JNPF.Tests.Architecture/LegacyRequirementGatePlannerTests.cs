using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Studio.Streaming;
using Xunit;

namespace JNPF.Tests.Architecture;

/// <summary>
/// Characterization: LEGACY requirement-gate decisions extracted from StreamLlmResponseAsync.
/// </summary>
public sealed class LegacyRequirementGatePlannerTests
{
    [Fact]
    public void ShouldRunLegacyGate_OnlyRequirement()
    {
        Assert.True(LegacyRequirementGatePlanner.ShouldRunLegacyGate(PipelineStage.Requirement));
        Assert.False(LegacyRequirementGatePlanner.ShouldRunLegacyGate("architecture"));
        Assert.False(LegacyRequirementGatePlanner.ShouldRunLegacyGate(null));
    }

    [Fact]
    public void ComposeFullText_ConcatenatesUserAndAttachments()
    {
        Assert.Equal("hiDOC", LegacyRequirementGatePlanner.ComposeFullText("hi", "DOC"));
        Assert.Equal("hi", LegacyRequirementGatePlanner.ComposeFullText("hi", null));
    }

    [Theory]
    [InlineData(true, false, LegacyGatePromptBranch.ForceRefine)]
    [InlineData(false, true, LegacyGatePromptBranch.MaxRoundsForceRefine)]
    [InlineData(true, true, LegacyGatePromptBranch.ForceRefine)]
    [InlineData(false, false, LegacyGatePromptBranch.EvaluateMaturity)]
    public void DecidePromptBranch_ForceWinsOverMaxRounds(
        bool force, bool maxRounds, LegacyGatePromptBranch expected)
        => Assert.Equal(expected, LegacyRequirementGatePlanner.DecidePromptBranch(force, maxRounds));

    [Theory]
    [InlineData("refine", 1, 7, LegacyClarificationAction.ContinueStream)]
    [InlineData("explore", 7, 7, LegacyClarificationAction.ForceRefineAtCap)]
    [InlineData("confirm", 3, 7, LegacyClarificationAction.RequestClarification)]
    [InlineData("explore", 6, 7, LegacyClarificationAction.RequestClarification)]
    public void DecideClarificationAction_ModesAndCap(
        string mode, int round, int max, LegacyClarificationAction expected)
        => Assert.Equal(
            expected,
            LegacyRequirementGatePlanner.DecideClarificationAction(mode, round, max));

    [Fact]
    public void FormatMaturityInfo_IncludesScoreAndModeLabel()
    {
        var info = LegacyRequirementGatePlanner.FormatMaturityInfo(80, "explore");
        Assert.Contains("80/100", info);
        Assert.Contains("探索", info);
    }

    [Fact]
    public void SummarizeUserContentsForStrengths_Truncates()
    {
        var msgs = new List<ChatMessage>
        {
            new("user", new string('a', 60)),
            new("assistant", "skip"),
            new("user", "short"),
        };
        var strengths = LegacyRequirementGatePlanner.SummarizeUserContentsForStrengths(msgs);
        Assert.Equal(2, strengths.Count);
        Assert.EndsWith("...", strengths[0]);
        Assert.Equal(53, strengths[0].Length); // 50 + "..."
        Assert.Equal("short", strengths[1]);
    }

    [Fact]
    public void BuildClarificationFragmentId_UsesRequirementStage()
    {
        Assert.Equal(
            $"clarification:{ClarificationStages.Requirement}:p1",
            LegacyRequirementGatePlanner.BuildClarificationFragmentId("p1"));
    }

    [Fact]
    public void FormatHardRuleReject_KeepsEmojiLayout()
    {
        Assert.Equal("❌ r\n\nh", LegacyRequirementGatePlanner.FormatHardRuleReject("r", "h"));
    }

    [Theory]
    [InlineData("confirm", 7, 7, LegacyClarificationAction.ForceRefineAtCap)]
    [InlineData(null, 1, 7, LegacyClarificationAction.ContinueStream)]
    public void DecideClarificationAction_ConfirmCapAndNullMode(
        string? mode, int round, int max, LegacyClarificationAction expected)
        => Assert.Equal(
            expected,
            LegacyRequirementGatePlanner.DecideClarificationAction(mode, round, max));
}
