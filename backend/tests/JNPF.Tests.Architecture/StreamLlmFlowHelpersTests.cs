using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Studio.Streaming;
using Xunit;

namespace JNPF.Tests.Architecture;

/// <summary>
/// Characterization tests for StreamLlmResponseAsync pure extracts (W-续).
/// </summary>
public sealed class StreamLlmFlowHelpersTests
{
    [Fact]
    public void ToChatMessages_FiltersSystemAndToolRoles()
    {
        var msgs = StreamLlmFlowHelpers.ToChatMessages(new[]
        {
            ("system", "ignore"),
            ("user", "hi"),
            ("assistant", "ok"),
            ("tool", "x"),
        });
        Assert.Equal(2, msgs.Count);
        Assert.Equal("user", msgs[0].Role);
        Assert.Equal("assistant", msgs[1].Role);
    }

    [Fact]
    public void ResolveAttachmentDownloadUrl_JoinsBaseForRelative()
    {
        Assert.Equal(
            "https://host/api/file/a.png",
            StreamLlmFlowHelpers.ResolveAttachmentDownloadUrl("/api/file/a.png", "https://host"));
        Assert.Equal(
            "https://cdn/x.png",
            StreamLlmFlowHelpers.ResolveAttachmentDownloadUrl("https://cdn/x.png", "https://host"));
    }

    [Fact]
    public void ResolveAttachmentDownloadUrl_MissingBase_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            StreamLlmFlowHelpers.ResolveAttachmentDownloadUrl("/rel", null));
    }

    [Fact]
    public void AppendToLastUserMessage_AppendsOnlyLastUser()
    {
        var list = new List<ChatMessage>
        {
            new("user", "a"),
            new("assistant", "b"),
            new("user", "c"),
        };
        Assert.True(StreamLlmFlowHelpers.AppendToLastUserMessage(list, "+att"));
        Assert.Equal("a", list[0].Content);
        Assert.Equal("c+att", list[2].Content);
    }

    [Theory]
    [InlineData(null, 7)]
    [InlineData(0, 7)]
    [InlineData(3, 3)]
    [InlineData(99, 20)]
    public void ClampClarificationMaxRounds(int? configured, int expected)
    {
        Assert.Equal(expected, StreamLlmFlowHelpers.ClampClarificationMaxRounds(configured));
    }

    [Theory]
    [InlineData(0, 7, 1)]
    [InlineData(2, 7, 2)]
    [InlineData(20, 7, 7)]
    public void ComputeClarificationRound(int assistantCount, int max, int expected)
    {
        Assert.Equal(expected, StreamLlmFlowHelpers.ComputeClarificationRound(assistantCount, max));
    }

    [Fact]
    public void EstimateStreamTokens_CharsOverFour()
    {
        var (input, output, total) = StreamLlmFlowHelpers.EstimateStreamTokens(40, 20);
        Assert.Equal(10, input);
        Assert.Equal(5, output);
        Assert.Equal(15, total);
    }

    [Fact]
    public void ExtractToken_SupportsDeltaTextAndChoicesContent()
    {
        Assert.Equal("hi", StreamLlmFlowHelpers.ExtractToken("""{"delta":{"text":"hi"}}"""));
        Assert.Equal(
            "yo",
            StreamLlmFlowHelpers.ExtractToken("""{"choices":[{"delta":{"content":"yo"}}]}"""));
        Assert.Null(StreamLlmFlowHelpers.ExtractToken("not-json"));
    }

    [Theory]
    [InlineData("[ERROR] boom", true)]
    [InlineData("[error] x", true)]
    [InlineData("{\"delta\":{}}", false)]
    [InlineData(null, false)]
    public void IsGatewayStreamError(string? json, bool expected)
        => Assert.Equal(expected, StreamLlmFlowHelpers.IsGatewayStreamError(json));

    [Fact]
    public void FormatMaturityModeLabel_KnownModes()
    {
        Assert.Contains("探索", StreamLlmFlowHelpers.FormatMaturityModeLabel("explore"));
        Assert.Contains("精化", StreamLlmFlowHelpers.FormatMaturityModeLabel("refine"));
    }

    [Theory]
    [InlineData(false, "http://x", "k", VisionExtractionDecision.SkipNoImages)]
    [InlineData(true, null, "k", VisionExtractionDecision.SkipNotConfigured)]
    [InlineData(true, "http://x", "  ", VisionExtractionDecision.SkipNotConfigured)]
    [InlineData(true, "http://x", "k", VisionExtractionDecision.Run)]
    public void DecideVisionExtraction(bool hasImages, string? apiUrl, string? apiKey, VisionExtractionDecision expected)
        => Assert.Equal(expected, StreamLlmFlowHelpers.DecideVisionExtraction(hasImages, apiUrl, apiKey));

    [Fact]
    public void HasImageFileNames_DetectsImageExtensions()
    {
        Assert.True(StreamLlmFlowHelpers.HasImageFileNames(new[] { "a.pdf", "b.PNG" }));
        Assert.False(StreamLlmFlowHelpers.HasImageFileNames(new[] { "a.pdf", "b.docx" }));
        Assert.False(StreamLlmFlowHelpers.HasImageFileNames(null));
    }

    [Fact]
    public void BuildDefaultStreamRequest_PinsLegacyDefaults()
    {
        var msgs = new List<ChatMessage> { new("user", "hi") };
        var req = StreamLlmFlowHelpers.BuildDefaultStreamRequest("deepseek", "sys", msgs);
        Assert.Equal("deepseek", req.ProviderCode);
        Assert.Equal("sys", req.SystemPrompt);
        Assert.Same(msgs, req.Messages);
        Assert.Equal(4096, req.MaxTokens);
        Assert.Equal(0.7, req.Temperature);
        Assert.Equal(2, req.MaxRetries);
        Assert.Equal(120000, req.TimeoutMs);
    }

    [Fact]
    public void FormatLlmStreamFailure_IncludesInnerWhenPresent()
    {
        var outer = new InvalidOperationException("outer", new Exception("inner"));
        Assert.Equal("LLM 调用失败: outer (Inner: inner)", StreamLlmFlowHelpers.FormatLlmStreamFailure(outer));
        Assert.Equal("LLM 调用失败: alone", StreamLlmFlowHelpers.FormatLlmStreamFailure(new Exception("alone")));
    }

    [Fact]
    public void ShouldUploadDevelopmentArtifacts_OnlyDevelopmentStage()
    {
        Assert.True(StreamLlmFlowHelpers.ShouldUploadDevelopmentArtifacts(PipelineStage.Development));
        Assert.False(StreamLlmFlowHelpers.ShouldUploadDevelopmentArtifacts(PipelineStage.Requirement));
    }

    [Theory]
    [InlineData(0, "t1", false)]
    [InlineData(10, "", false)]
    [InlineData(10, null, false)]
    [InlineData(10, "t1", true)]
    public void ShouldAccumulateEstimatedTokens(int total, string? tenantId, bool expected)
        => Assert.Equal(expected, StreamLlmFlowHelpers.ShouldAccumulateEstimatedTokens(total, tenantId));

    [Fact]
    public void PrefixVisionAnalysis_AddsBlankLinePrefix()
        => Assert.Equal("\n\n分析结果", StreamLlmFlowHelpers.PrefixVisionAnalysis("分析结果"));
}
