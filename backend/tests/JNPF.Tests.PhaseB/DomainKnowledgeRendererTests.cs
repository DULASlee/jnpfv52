using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

public class DomainKnowledgeRendererTests
{
    [Fact]
    public void Render_EmptyList_ReturnsEmpty()
    {
        var result = DomainKnowledgeRenderer.Render(Array.Empty<SeedTemplateMatch>());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_NullList_ReturnsEmpty()
    {
        var result = DomainKnowledgeRenderer.Render(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_WithSeeds_ReturnsFormattedBlock()
    {
        var seeds = new List<SeedTemplateMatch>
        {
            new() { Industry = "hr", EventNamePattern = "请假", TemplateJson = """{"entities":["请假申请"],"rules":["3天以上需总监"]}""" },
        };
        var result = DomainKnowledgeRenderer.Render(seeds);
        Assert.Contains("参考方案", result);
        Assert.Contains("hr/请假", result);
    }

    [Fact]
    public void Render_MoreThan3Seeds_TakesTop3Only()
    {
        var seeds = Enumerable.Range(0, 5)
            .Select(i => new SeedTemplateMatch { Industry = "hr", EventNamePattern = $"事件{i}", TemplateJson = "{}" })
            .ToList();
        var result = DomainKnowledgeRenderer.Render(seeds);
        var lineCount = result.Count(c => c == '\n');
        // 1 标题行 + 最多 3 条种子行
        Assert.True(lineCount <= 4);
    }

    [Fact]
    public void Render_LongTemplateJson_TruncatedTo200Chars()
    {
        var longJson = new string('x', 500);
        var seeds = new List<SeedTemplateMatch>
        {
            new() { Industry = "hr", EventNamePattern = "请假", TemplateJson = longJson },
        };
        var result = DomainKnowledgeRenderer.Render(seeds);
        // 不含完整 500 字（被截断）
        Assert.DoesNotContain(new string('x', 500), result);
    }

    [Fact]
    public void RenderRules_EmptyList_ReturnsEmpty()
    {
        var result = DomainKnowledgeRenderer.RenderRules(Array.Empty<SeedTemplateMatch>());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderPitfalls_EmptyList_ReturnsEmpty()
    {
        var result = DomainKnowledgeRenderer.RenderPitfalls(Array.Empty<SeedTemplateMatch>());
        Assert.Equal(string.Empty, result);
    }
}
