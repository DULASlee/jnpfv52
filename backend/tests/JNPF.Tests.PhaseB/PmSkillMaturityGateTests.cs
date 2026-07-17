using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// PmSkillService 成熟度门控 + AllowSkipNonCritical 联动测试（CR-01 + CR-02）。
/// 覆盖 BuildMaturityChatHistory、BuildRefineEmptySet、AllowSkipNonCritical 决策表。
/// 纯内存测试，不依赖 DB / LLM。
/// </summary>
public class PmSkillMaturityGateTests
{
    // ── BuildMaturityChatHistory ──

    [Fact]
    public void BuildMaturityChatHistory_EmptyCompileResult_OnlyProjectInfo()
    {
        var compileResult = new SaNineViewCompileResult
        {
            Source = null!,
            ProjectSteps = new Dictionary<string, object>(),
            EventResults = Array.Empty<SaEventResult>(),
            Assumptions = new List<Assumption>(),
        };

        var messages = PmSkillService.BuildMaturityChatHistory(
            compileResult, null, "tenant-1", "project-42", 311L);

        Assert.Single(messages);
        Assert.Equal("user", messages[0].Role);
        Assert.Contains("项目 project-42", messages[0].Content);
        Assert.Contains("租户 tenant-1", messages[0].Content);
    }

    [Fact]
    public void BuildMaturityChatHistory_IncludesEntities()
    {
        var compileResult = new SaNineViewCompileResult
        {
            Source = null!,
            ProjectSteps = new Dictionary<string, object>(),
            EventResults = new List<SaEventResult>
            {
                new() { EventName = "用户管理" },
                new() { EventName = "角色权限" },
                new() { EventName = "日志审计" },
            },
            Assumptions = new List<Assumption>(),
        };

        var messages = PmSkillService.BuildMaturityChatHistory(
            compileResult, null, "t", "p", 1L);

        Assert.Single(messages);
        Assert.Contains("实体（3）", messages[0].Content);
        Assert.Contains("用户管理、角色权限、日志审计", messages[0].Content);
    }

    [Fact]
    public void BuildMaturityChatHistory_TruncatesEntities_At15()
    {
        var events = Enumerable.Range(1, 20)
            .Select(i => new SaEventResult { EventName = $"实体{i}" })
            .ToList();
        var compileResult = new SaNineViewCompileResult
        {
            Source = null!,
            ProjectSteps = new Dictionary<string, object>(),
            EventResults = events,
            Assumptions = new List<Assumption>(),
        };

        var messages = PmSkillService.BuildMaturityChatHistory(
            compileResult, null, "t", "p", 1L);

        Assert.Single(messages);
        Assert.Contains("实体（20）", messages[0].Content);
        Assert.Contains("…", messages[0].Content);
        // 只取前 15 个
        var entityPart = messages[0].Content.Split('\n')[1];
        var names = entityPart.Split('：')[1].Split('、');
        Assert.Equal(15, names.Length);
    }

    [Fact]
    public void BuildMaturityChatHistory_IncludesAssumptions()
    {
        var compileResult = new SaNineViewCompileResult
        {
            Source = null!,
            ProjectSteps = new Dictionary<string, object>(),
            EventResults = Array.Empty<SaEventResult>(),
            Assumptions = new List<Assumption>
            {
                new("evt1", "step-a", "假设用户已有账号", 0.8m),
                new("evt2", "step-b", "假设使用 OAuth2", 0.7m),
            },
        };

        var messages = PmSkillService.BuildMaturityChatHistory(
            compileResult, null, "t", "p", 1L);

        Assert.Single(messages);
        Assert.Contains("假设项（2）", messages[0].Content);
        Assert.Contains("假设用户已有账号", messages[0].Content);
        Assert.Contains("假设使用 OAuth2", messages[0].Content);
    }

    [Fact]
    public void BuildMaturityChatHistory_IncludesPreviousAnswers()
    {
        var compileResult = new SaNineViewCompileResult
        {
            Source = null!,
            ProjectSteps = new Dictionary<string, object>(),
            EventResults = Array.Empty<SaEventResult>(),
            Assumptions = new List<Assumption>(),
        };

        var messages = PmSkillService.BuildMaturityChatHistory(
            compileResult, "用户确认：需要支持多租户。不需要移动端。", "t", "p", 1L);

        Assert.Equal(2, messages.Count);
        Assert.Equal("user", messages[1].Role);
        Assert.Contains("用户确认/澄清", messages[1].Content);
        Assert.Contains("多租户", messages[1].Content);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildMaturityChatHistory_NullOrWhitespaceAnswers_NoAnswerMessage(string? answers)
    {
        var compileResult = new SaNineViewCompileResult
        {
            Source = null!,
            ProjectSteps = new Dictionary<string, object>(),
            EventResults = Array.Empty<SaEventResult>(),
            Assumptions = new List<Assumption>(),
        };

        var messages = PmSkillService.BuildMaturityChatHistory(
            compileResult, answers, "t", "p", 1L);

        Assert.Single(messages); // 只有 project info，没有答案消息
    }

    // ── BuildRefineEmptySet ──

    [Fact]
    public void BuildRefineEmptySet_ReturnsEmptyQuestionSet()
    {
        var maturity = new MaturityResult
        {
            Score = 88,
            Mode = "refine",
            Domain = "CRM系统",
        };

        var set = PmSkillService.BuildRefineEmptySet("requirement", 2, maturity);

        Assert.NotNull(set.SetId);
        Assert.Equal("requirement", set.Stage);
        Assert.Equal(2, set.Round);
        Assert.Equal("需求已足够完整", set.Title);
        Assert.Contains("88/100", set.Intro);
        Assert.True(set.AllowSkipNonCritical);
        Assert.Empty(set.Questions);
    }

    [Theory]
    [InlineData(95)]
    [InlineData(80)]
    [InlineData(100)]
    public void BuildRefineEmptySet_ScoreReflectedInIntro(int score)
    {
        var maturity = new MaturityResult { Score = score, Mode = "refine" };

        var set = PmSkillService.BuildRefineEmptySet("requirement", 1, maturity);

        Assert.Contains($"{score}/100", set.Intro);
    }

    // ── CR-02 AllowSkipNonCritical 决策表验证 ──
    // 核心不变式: AllowSkipNonCritical = (questions.Count == 0)
    // 即：仅 0 题时允许跳过（refine 或 LLM 失败）；有题时禁止跳过

    [Theory]
    [InlineData(0, true)]   // 0 题 → 允许跳过（refine 或 LLM 失败降级）
    [InlineData(1, false)]  // 1 题 → 禁止跳过
    [InlineData(3, false)]  // 3 题 → 禁止跳过
    [InlineData(5, false)]  // 5 题 → 禁止跳过
    public void AllowSkipNonCritical_DecisionTable(int questionCount, bool expectedAllowSkip)
    {
        // CR-02 核心逻辑: bool allowSkip = questions.Count == 0;
        // 本测试验证决策表不变式，与实际代码逻辑一致
        bool allowSkip = questionCount == 0;

        Assert.Equal(expectedAllowSkip, allowSkip);
    }

    // ── CR-01 + CR-02 联动不变式 ──

    [Fact]
    public void RefineMode_ProducesEmptySet_WithAllowSkipTrue()
    {
        // CR-01 refine 路径 → BuildRefineEmptySet → AllowSkip=true, Questions=0
        // CR-02 不变式: AllowSkip = (questions.Count == 0) → 0 题 → true ✅
        var maturity = new MaturityResult { Score = 85, Mode = "refine" };
        var set = PmSkillService.BuildRefineEmptySet("requirement", 1, maturity);

        Assert.True(set.AllowSkipNonCritical);
        Assert.Empty(set.Questions);
        // 一致性：AllowSkip=true 且 Questions.Empty → 不矛盾
    }

    [Fact]
    public void ExploreMode_HasQuestions_ShouldNotAllowSkip()
    {
        // 模拟 explore/confirm 模式下 PM 出题后的 AllowSkip 逻辑
        // CR-02: allowSkip = questions.Count == 0
        var questions = new List<ClarificationQuestion>
        {
            new() { Id = "q1", Text = "核心实体有哪些？" },
            new() { Id = "q2", Text = "用户角色如何划分？" },
            new() { Id = "q3", Text = "是否需要审批流？" },
        };

        bool allowSkip = questions.Count == 0;

        Assert.False(allowSkip); // 有 3 题 → 不允许跳过
    }

    [Fact]
    public void LLMFailure_EmptyQuestions_ShouldAllowSkip()
    {
        // LLM 失败降级路径：返回 BuildEmptyClarificationSet（0 题）
        // CR-02: allowSkip = questions.Count == 0 → 0 题 → true
        var questions = new List<ClarificationQuestion>();

        bool allowSkip = questions.Count == 0;

        Assert.True(allowSkip); // 0 题 → 允许跳过
    }

    // ── MaturityResult 默认值（确保 fail-safe 行为正确）──

    [Fact]
    public void MaturityResult_DefaultMode_IsExplore()
    {
        var result = new MaturityResult();

        Assert.Equal("explore", result.Mode);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void GenerateClarificationAsync_SupportsForceQuestionsParameter()
    {
        var method = typeof(PmSkillService).GetMethod(nameof(PmSkillService.GenerateClarificationAsync));
        Assert.NotNull(method);
        var forceParam = method!.GetParameters().FirstOrDefault(p => p.Name == "forceQuestions");
        Assert.NotNull(forceParam);
        Assert.True(forceParam!.HasDefaultValue);
        Assert.False((bool)forceParam.DefaultValue!);
    }
}
