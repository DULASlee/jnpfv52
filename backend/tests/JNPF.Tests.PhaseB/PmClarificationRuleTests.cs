using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// CR-20260714-01 改动3：铁律1 验证 — 多选题每题必有"其他+文本框"，矩阵题无"其他"。
/// </summary>
public class PmClarificationRuleTests
{
    /// <summary>构造最小 SaNineViewCompileResult（ApplyMatrixFallback 只读 EventResults）。</summary>
    private static SaNineViewCompileResult MakeCompile(params string[] eventNames) => new()
    {
        Source = new PreAnalysisModel(),
        ProjectSteps = new Dictionary<string, object>(),
        EventResults = eventNames
            .Select(n => new SaEventResult { EventName = n })
            .ToList(),
    };

    // ── EnsureEscapeHatch：多选题每题必加"其他" ──

    [Fact]
    public void EnsureEscapeHatch_MultiQuestionWithoutOther_AddsOtherOption()
    {
        var questions = new List<ClarificationQuestion>
        {
            new()
            {
                Id = "q1",
                Text = "审批几级？",
                QuestionFormat = "MULTI",
                Options = new List<ClarificationOption>
                {
                    new() { Id = "opt-1", Label = "1级" },
                    new() { Id = "opt-2", Label = "2级" },
                },
            },
        };

        PmSkillService.EnsureEscapeHatch(questions);

        var q = Assert.Single(questions);
        var lastOpt = q.Options[^1];
        Assert.True(lastOpt.FreeText);
        Assert.Equal("其他", lastOpt.Label);
    }

    [Fact]
    public void EnsureEscapeHatch_MultiQuestionAlreadyHasOther_NoDuplicate()
    {
        var questions = new List<ClarificationQuestion>
        {
            new()
            {
                Id = "q1",
                Text = "审批几级？",
                QuestionFormat = "MULTI",
                Options = new List<ClarificationOption>
                {
                    new() { Id = "opt-1", Label = "1级" },
                    new() { Id = "opt-2", Label = "2级" },
                    new() { Id = "o_other", Label = "其他", FreeText = true },
                },
            },
        };

        PmSkillService.EnsureEscapeHatch(questions);

        var q = Assert.Single(questions);
        Assert.Equal(3, q.Options.Count);
        Assert.Single(q.Options.Where(o => o.FreeText));
    }

    // ── EnsureEscapeHatch：矩阵题不加"其他" ──

    [Fact]
    public void EnsureEscapeHatch_MatrixMulti_NoOtherAdded()
    {
        var questions = new List<ClarificationQuestion>
        {
            new()
            {
                Id = "q1",
                Text = "各模块的优先级？",
                QuestionFormat = "MATRIX_MULTI",
                Options = new List<ClarificationOption>
                {
                    new() { Id = "opt-1", Label = "高" },
                    new() { Id = "opt-2", Label = "中" },
                },
                MatrixSubItems = new List<MatrixSubItem>
                {
                    new() { RowId = "m1", RowLabel = "请假" },
                    new() { RowId = "m2", RowLabel = "考勤" },
                },
            },
        };

        PmSkillService.EnsureEscapeHatch(questions);

        var q = Assert.Single(questions);
        Assert.DoesNotContain(q.Options, o => o.FreeText || o.Label is "其他" or "其它");
        Assert.Equal(2, q.Options.Count);
    }

    [Fact]
    public void EnsureEscapeHatch_MatrixSingle_NoOtherAdded()
    {
        var questions = new List<ClarificationQuestion>
        {
            new()
            {
                Id = "q1",
                Text = "各事件的触发方式？",
                QuestionFormat = "MATRIX_SINGLE",
                Options = new List<ClarificationOption>
                {
                    new() { Id = "opt-1", Label = "手动" },
                    new() { Id = "opt-2", Label = "自动" },
                },
                MatrixSubItems = new List<MatrixSubItem>
                {
                    new() { RowId = "m1", RowLabel = "请假申请" },
                },
            },
        };

        PmSkillService.EnsureEscapeHatch(questions);

        var q = Assert.Single(questions);
        Assert.DoesNotContain(q.Options, o => o.FreeText);
    }

    // ── ApplyMatrixFallback：MULTI 升级 MATRIX 时清 Options ──

    [Fact]
    public void ApplyMatrixFallback_UpgradeClearsOptions_NoOtherResidue()
    {
        // 一道多选题，含"其他"选项，且题干包含多个事件名 → 应被升级为矩阵
        var questions = new List<ClarificationQuestion>
        {
            new()
            {
                Id = "q1",
                Text = "请假申请和加班申请的审批方式？",
                QuestionFormat = "MULTI",
                Options = new List<ClarificationOption>
                {
                    new() { Id = "opt-1", Label = "逐级审批" },
                    new() { Id = "opt-2", Label = "并行审批" },
                    new() { Id = "o_other", Label = "其他", FreeText = true },
                },
            },
        };

        PmSkillService.ApplyMatrixFallback(questions, MakeCompile("请假申请", "加班申请"));

        var q = Assert.Single(questions);
        Assert.Equal("MATRIX_MULTI", q.QuestionFormat);
        // 关键断言：升级后 Options 被清空，残留的"其他"消失
        Assert.Empty(q.Options);
        Assert.NotNull(q.MatrixSubItems);
        Assert.Equal(2, q.MatrixSubItems!.Count);
    }

    [Fact]
    public void ApplyMatrixFallback_PlainMultiNotMatchingEvents_Unchanged()
    {
        var questions = new List<ClarificationQuestion>
        {
            new()
            {
                Id = "q1",
                Text = "审批几级？",
                QuestionFormat = "MULTI",
                Options = new List<ClarificationOption>
                {
                    new() { Id = "opt-1", Label = "1级" },
                    new() { Id = "opt-2", Label = "2级" },
                },
            },
        };

        PmSkillService.ApplyMatrixFallback(questions, MakeCompile("请假申请", "加班申请"));

        var q = Assert.Single(questions);
        Assert.Equal("MULTI", q.QuestionFormat);
        Assert.Equal(2, q.Options.Count);
    }

    // ── 组合：先升级矩阵再 EscapeHatch，矩阵仍无"其他" ──

    [Fact]
    public void Combined_ApplyMatrixThenEscapeHatch_MatrixHasNoOther()
    {
        var questions = new List<ClarificationQuestion>
        {
            new()
            {
                Id = "q1",
                Text = "请假申请和加班申请的审批方式？",
                QuestionFormat = "MULTI",
                Options = new List<ClarificationOption>
                {
                    new() { Id = "opt-1", Label = "逐级审批" },
                    new() { Id = "opt-2", Label = "并行审批" },
                    new() { Id = "o_other", Label = "其他", FreeText = true },
                },
            },
        };

        // 模拟 GenerateClarificationAsync 中的调用顺序
        PmSkillService.ApplyMatrixFallback(questions, MakeCompile("请假申请", "加班申请"));
        PmSkillService.EnsureEscapeHatch(questions);

        var q = Assert.Single(questions);
        Assert.Equal("MATRIX_MULTI", q.QuestionFormat);
        Assert.Empty(q.Options);
    }
}
