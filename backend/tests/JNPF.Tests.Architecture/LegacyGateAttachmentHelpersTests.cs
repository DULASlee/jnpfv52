using JNPF.InteAssistant.Studio.Streaming;
using Xunit;

namespace JNPF.Tests.Architecture;

/// <summary>
/// Characterization: LEGACY gate attachment persist/cache helpers.
/// </summary>
public sealed class LegacyGateAttachmentHelpersTests
{
    [Fact]
    public void UrlAlreadyExists_MatchesExactUrl_IncludingEmpty()
    {
        var urls = new[] { "/a.png", "/b.pdf", "" };
        Assert.True(LegacyGateAttachmentHelpers.UrlAlreadyExists(urls, "/a.png"));
        Assert.False(LegacyGateAttachmentHelpers.UrlAlreadyExists(urls, "/c.doc"));
        // Legacy: empty URL also dedupes (prevents spam inserts + hard-rule count inflate)
        Assert.True(LegacyGateAttachmentHelpers.UrlAlreadyExists(urls, ""));
        Assert.False(LegacyGateAttachmentHelpers.UrlAlreadyExists(new[] { "/a.png" }, ""));
        Assert.True(LegacyGateAttachmentHelpers.UrlAlreadyExists(new string?[] { null }, null));
    }

    [Theory]
    [InlineData("spec.docx", "docx")]
    [InlineData("a.PNG", "PNG")]
    [InlineData("noext", "")]
    [InlineData(null, "")]
    public void FileTypeFromFileName(string? name, string expected)
        => Assert.Equal(expected, LegacyGateAttachmentHelpers.FileTypeFromFileName(name));

    [Fact]
    public void CreatePendingEntity_SetsPendingDefaults()
    {
        var now = new DateTime(2026, 8, 7, 12, 0, 0);
        var e = LegacyGateAttachmentHelpers.CreatePendingEntity(
            "p1", "proj", "x.pdf", "/f/x.pdf", "u1", "Admin", "t1", now, id: "fixed-id");
        Assert.Equal("fixed-id", e.F_Id);
        Assert.Equal("p1", e.PipelineId);
        Assert.Equal("proj", e.ProjectId);
        Assert.Equal("pdf", e.FileType);
        Assert.Equal(LegacyGateAttachmentHelpers.ProcessStatusPending, e.ProcessStatus);
        Assert.Null(e.FileHash);
        Assert.False(e.DeleteMark);
        Assert.Equal(now, e.CreateTime);
    }

    [Theory]
    [InlineData(2, "text", true)]
    [InlineData(2, "  ", false)]
    [InlineData(2, null, false)]
    [InlineData(1, "text", false)]
    [InlineData(0, "text", false)]
    public void IsExtractedCacheHit(int status, string? text, bool expected)
        => Assert.Equal(expected, LegacyGateAttachmentHelpers.IsExtractedCacheHit(status, text));

    [Fact]
    public void TruncateProcessError_CapsAt2000()
    {
        var longMsg = new string('x', 2500);
        var truncated = LegacyGateAttachmentHelpers.TruncateProcessError(longMsg);
        Assert.Equal(2000, truncated.Length);
        Assert.Equal("short", LegacyGateAttachmentHelpers.TruncateProcessError("short"));
        Assert.Equal(string.Empty, LegacyGateAttachmentHelpers.TruncateProcessError(null));
    }

    [Fact]
    public void JoinExtractedTexts_DoubleNewline()
    {
        Assert.Equal("a\n\nb", LegacyGateAttachmentHelpers.JoinExtractedTexts(new[] { "a", "b" }));
    }

    [Fact]
    public void TryTakeCachedBytes_HitsAndMisses()
    {
        var cache = new Dictionary<string, byte[]> { ["/a"] = new byte[] { 1, 2 } };
        Assert.True(LegacyGateAttachmentHelpers.TryTakeCachedBytes(cache, "/a", out var bytes));
        Assert.Equal(new byte[] { 1, 2 }, bytes);
        Assert.False(LegacyGateAttachmentHelpers.TryTakeCachedBytes(cache, "/missing", out _));
    }

    [Fact]
    public void RememberDownloadedBytes_StoresForLaterVisionReuse()
    {
        var cache = new Dictionary<string, byte[]>();
        var payload = new byte[] { 9, 8, 7 };
        LegacyGateAttachmentHelpers.RememberDownloadedBytes(cache, "/f.pdf", payload);
        Assert.True(LegacyGateAttachmentHelpers.TryTakeCachedBytes(cache, "/f.pdf", out var hit));
        Assert.Same(payload, hit);
    }

    [Fact]
    public void ComputeSha256Hex_LowerInvariantHex()
    {
        // SHA256("") known vector
        Assert.Equal(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            LegacyGateAttachmentHelpers.ComputeSha256Hex(Array.Empty<byte>()));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("ok", true)]
    public void ShouldAppendExtractedText(string? text, bool expected)
        => Assert.Equal(expected, LegacyGateAttachmentHelpers.ShouldAppendExtractedText(text));

    [Fact]
    public void ToProcessorFile_CopiesNameAndContent()
    {
        var bytes = new byte[] { 1 };
        var file = LegacyGateAttachmentHelpers.ToProcessorFile("a.docx", bytes);
        Assert.Equal("a.docx", file.FileName);
        Assert.Same(bytes, file.Content);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("Bearer abc", "abc")]
    [InlineData("bearer xyz", "xyz")]
    [InlineData("raw-token", "raw-token")]
    public void StripBearerPrefix(string? auth, string? expected)
        => Assert.Equal(expected, LegacyGateAttachmentHelpers.StripBearerPrefix(auth));

    [Fact]
    public void AttachmentDownloadTimeout_Is30Seconds()
        => Assert.Equal(TimeSpan.FromSeconds(30), LegacyGateAttachmentHelpers.AttachmentDownloadTimeout);

    [Fact]
    public void BuildRunningUpdate_SetsRunningAndTimestamp()
    {
        var now = new DateTime(2026, 8, 7, 23, 0, 0);
        var u = LegacyGateAttachmentHelpers.BuildRunningUpdate(now);
        Assert.Equal(LegacyGateAttachmentHelpers.ProcessStatusRunning, u.ProcessStatus);
        Assert.Equal(now, u.LastModifyTime);
    }

    [Fact]
    public void BuildDoneUpdate_CarriesExtractedTextAndHash()
    {
        var now = new DateTime(2026, 8, 7, 23, 1, 0);
        var u = LegacyGateAttachmentHelpers.BuildDoneUpdate("正文", "abc", now);
        Assert.Equal(LegacyGateAttachmentHelpers.ProcessStatusDone, u.ProcessStatus);
        Assert.Equal("正文", u.ExtractedText);
        Assert.Equal("abc", u.FileHash);
        Assert.Equal(now, u.LastModifyTime);
    }

    [Fact]
    public void BuildFailedUpdate_TruncatesError()
    {
        var now = new DateTime(2026, 8, 7, 23, 2, 0);
        var longMsg = new string('e', 2500);
        var u = LegacyGateAttachmentHelpers.BuildFailedUpdate(longMsg, now);
        Assert.Equal(LegacyGateAttachmentHelpers.ProcessStatusFailed, u.ProcessStatus);
        Assert.Equal(2000, u.ProcessError.Length);
        Assert.Equal(now, u.LastModifyTime);
    }

    [Fact]
    public void TryCollectCacheHitText_AddsOnlyOnHit()
    {
        var texts = new List<string>();
        Assert.True(LegacyGateAttachmentHelpers.TryCollectCacheHitText(
            LegacyGateAttachmentHelpers.ProcessStatusDone, "cached", texts));
        Assert.Equal(new[] { "cached" }, texts);

        Assert.False(LegacyGateAttachmentHelpers.TryCollectCacheHitText(
            LegacyGateAttachmentHelpers.ProcessStatusPending, "x", texts));
        Assert.Single(texts);
    }

    [Fact]
    public void CollectExtractedIfPresent_RespectsWhitespaceGate()
    {
        var texts = new List<string>();
        Assert.True(LegacyGateAttachmentHelpers.CollectExtractedIfPresent("ok", texts));
        Assert.False(LegacyGateAttachmentHelpers.CollectExtractedIfPresent("  ", texts));
        Assert.Equal(new[] { "ok" }, texts);
    }
}
