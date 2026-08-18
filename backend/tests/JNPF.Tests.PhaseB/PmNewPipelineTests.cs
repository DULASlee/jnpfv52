using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// CR-20260713-03:新 4 步线性 PM 流程的契约测试。
///
/// 覆盖:
///   - DTO 契约(RequirementEnhanceResult / PmClarificationTurn / ClarificationSource)
///   - UseNewPipeline 开关默认 false(向后兼容)
///   - RequirementAnalysisOptions 新字段
///   - RequirementAnalysisOrchestratorResult 新字段(awaiting-clarification / awaiting-spec-confirm)
/// </summary>
public class PmNewPipelineTests
{
    // ── DTO 契约 ──

    [Fact]
    public void RequirementEnhanceResult_DefaultStatus_IsCompleted()
    {
        var result = new RequirementEnhanceResult();
        Assert.Equal("completed", result.Status);
    }

    [Fact]
    public void RequirementEnhanceResult_CanRepresentPendingQuestion()
    {
        var result = new RequirementEnhanceResult
        {
            Status = "pending_question",
            PendingQuestion = "请假审批流程是几级?",
            QuestionReason = "影响状态机设计",
            PartialEnhancement = "目前已完善请假申请部分",
        };

        Assert.Equal("pending_question", result.Status);
        Assert.Equal("请假审批流程是几级?", result.PendingQuestion);
        Assert.Equal("影响状态机设计", result.QuestionReason);
        Assert.NotEmpty(result.PartialEnhancement);
    }

    [Fact]
    public void RequirementEnhanceResult_CanRepresentCompleted()
    {
        var result = new RequirementEnhanceResult
        {
            Status = "completed",
            EnhancedText = "# 请假管理系统需求\n\n## 业务事件\n...",
            CompletenessNotes = new[] { "补全了审批层级", "补全了异常路径" },
            SeedIds = new long[] { 101, 102 },
            ClarificationTurns = 2,
        };

        Assert.Equal("completed", result.Status);
        Assert.Contains("# 请假管理系统需求", result.EnhancedText);
        Assert.Equal(2, result.CompletenessNotes.Count);
        Assert.Equal(2, result.SeedIds.Count);
        Assert.Equal(2, result.ClarificationTurns);
    }

    // ── PmClarificationTurn DTO ──

    [Fact]
    public void PmClarificationTurn_GeneratesTurnId()
    {
        var turn = new PmClarificationTurn
        {
            Question = "考勤规则是固定班次还是排班?",
            Source = ClarificationSource.Step1Enhance,
        };

        Assert.NotEmpty(turn.TurnId);
        Assert.Equal("考勤规则是固定班次还是排班?", turn.Question);
        Assert.Equal(ClarificationSource.Step1Enhance, turn.Source);
        Assert.Null(turn.UserAnswer); // 首轮发出时为空
    }

    [Fact]
    public void PmClarificationTurn_CanCarryUserAnswer()
    {
        var turn = new PmClarificationTurn
        {
            Question = "考勤规则?",
            UserAnswer = "固定班次,9:00-18:00",
            Source = ClarificationSource.Step3Refine,
        };

        Assert.Equal("固定班次,9:00-18:00", turn.UserAnswer);
        Assert.Equal(ClarificationSource.Step3Refine, turn.Source);
    }

    [Theory]
    [InlineData(ClarificationSource.Step1Enhance)]
    [InlineData(ClarificationSource.Step3Refine)]
    [InlineData(ClarificationSource.Step4Feedback)]
    public void ClarificationSource_AllValues_RoundTrip(ClarificationSource source)
    {
        var turn = new PmClarificationTurn { Source = source };
        Assert.Equal(source, turn.Source);
    }

    // ── UseNewPipeline 历史字段（RunAsync 已固定新主链，字段保留兼容）──

    [Fact]
    public void RequirementAnalysisOptions_UseNewPipeline_DefaultFalse_LegacyField()
    {
        var options = new RequirementAnalysisOptions();
        Assert.False(options.UseNewPipeline);
    }

    [Fact]
    public void RequirementAnalysisOptions_CanEnableNewPipeline()
    {
        var options = new RequirementAnalysisOptions { UseNewPipeline = true };
        Assert.True(options.UseNewPipeline);
    }

    [Fact]
    public void RequirementAnalysisOptions_NewFields_DefaultNull()
    {
        var options = new RequirementAnalysisOptions();
        Assert.Null(options.PmClarificationAnswer);
        Assert.Null(options.SpecFeedback);
    }

    // ── OrchestratorResult 新字段 ──

    [Fact]
    public void OrchestratorResult_NewFields_DefaultNull()
    {
        var result = new RequirementAnalysisOrchestratorResult();
        Assert.Null(result.PendingPmQuestion);
        Assert.Null(result.RenderedSpec);
    }

    [Fact]
    public void OrchestratorResult_CanRepresentAwaitingClarification()
    {
        var result = new RequirementAnalysisOrchestratorResult
        {
            Status = "awaiting-clarification",
            PendingPmQuestion = "审批节点需要几个?",
        };

        Assert.Equal("awaiting-clarification", result.Status);
        Assert.Equal("审批节点需要几个?", result.PendingPmQuestion);
    }

    [Fact]
    public void OrchestratorResult_CanRepresentAwaitingSpecConfirm()
    {
        var result = new RequirementAnalysisOrchestratorResult
        {
            Status = "awaiting-spec-confirm",
            RenderedSpec = "# 需求分析说明书\n\n...",
        };

        Assert.Equal("awaiting-spec-confirm", result.Status);
        Assert.Contains("# 需求分析说明书", result.RenderedSpec!);
    }

    // ── 4 步流程状态值合法性 ──

    [Theory]
    [InlineData("completed")]
    [InlineData("awaiting-answer")]            // 旧流程
    [InlineData("awaiting-clarification")]     // 新流程:PM 追问
    [InlineData("awaiting-spec-confirm")]      // 新流程:需求说明书确认
    [InlineData("gate-rejected")]              // 新流程:门控拒绝
    [InlineData("pm-review-failed")]
    [InlineData("failed")]
    public void OrchestratorResult_AllStatusValues_AreValidStrings(string status)
    {
        var result = new RequirementAnalysisOrchestratorResult { Status = status };
        Assert.Equal(status, result.Status);
    }

    // ── 门控门拒绝结果 ──

    [Fact]
    public void OrchestratorResult_CanRepresentGateRejected()
    {
        var result = new RequirementAnalysisOrchestratorResult
        {
            Status = "gate-rejected",
            ErrorMessage = "请输入需求描述",
            GateHint = "请描述您要构建的系统，或上传需求文档/截图。",
        };

        Assert.Equal("gate-rejected", result.Status);
        Assert.Equal("请输入需求描述", result.ErrorMessage);
        Assert.Contains("描述您要构建的系统", result.GateHint!);
    }

    [Fact]
    public void OrchestratorResult_GateHint_DefaultNull()
    {
        var result = new RequirementAnalysisOrchestratorResult();
        Assert.Null(result.GateHint);
    }
}
