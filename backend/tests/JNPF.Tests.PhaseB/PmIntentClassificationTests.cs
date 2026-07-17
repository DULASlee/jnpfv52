using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// CR-20260714-01 改动5：PM 智能意图判断测试。
/// </summary>
public class PmIntentClassificationTests
{
    // 由于 ClassifyUserIntent 是 private static，通过反射测试。
    // 但更务实的方式是验证行为契约 — 这里测试可公开的 DTO 和状态值。

    [Theory]
    [InlineData("确认", "confirm_spec")]
    [InlineData("通过", "confirm_spec")]
    [InlineData("没问题", "confirm_spec")]
    [InlineData("ok", "confirm_spec")]
    [InlineData("OK", "confirm_spec")]
    [InlineData("好的", "confirm_spec")]
    [InlineData("修改审批为3级", "request_change")]
    [InlineData("把审批改成3级", "request_change")]
    [InlineData("不对，需要增加一个角色", "request_change")]
    [InlineData("调整一下流程", "request_change")]
    public void IntentClassification_ConfirmAndChange_KeywordsRecognized(string input, string expectedIntent)
    {
        // 验证关键词能被正确分类（通过反射调 private static 方法）
        var result = InvokeClassifyUserIntent(input, hasSpecRendered: true, hasPendingClarification: false);
        Assert.Equal(expectedIntent, result.Intent);
    }

    [Fact]
    public void IntentClassification_AnswerQuestion_WhenClarificationPending()
    {
        // 当前有 pending clarification → 用户输入判为 answer_question
        var result = InvokeClassifyUserIntent("2级审批", hasSpecRendered: false, hasPendingClarification: true);
        Assert.Equal("answer_question", result.Intent);
    }

    [Fact]
    public void IntentClassification_ShortTextWithSpecRendered_TendsConfirm()
    {
        // 有 specRendered + 短文本(≤10字) → 倾向 confirm_spec
        var result = InvokeClassifyUserIntent("看起来不错", hasSpecRendered: true, hasPendingClarification: false);
        Assert.Equal("confirm_spec", result.Intent);
    }

    [Fact]
    public void IntentClassification_LongTextWithSpecRendered_TendsChange()
    {
        // 有 specRendered + 长文本(>10字) → 倾向 request_change
        var result = InvokeClassifyUserIntent("我觉得整个审批流程需要重新设计，应该从员工发起开始", hasSpecRendered: true, hasPendingClarification: false);
        Assert.Equal("request_change", result.Intent);
    }

    [Fact]
    public void IntentClassification_NoState_Unknown()
    {
        // 无 specRendered 无 pending clarification → unknown
        var result = InvokeClassifyUserIntent("随便说点什么", hasSpecRendered: false, hasPendingClarification: false);
        Assert.Equal("unknown", result.Intent);
    }

    [Fact]
    public void RequirementAnalysisRunRequest_UserMessage_CanBeSet()
    {
        var req = new RequirementAnalysisRunRequest { UserMessage = "确认通过" };
        Assert.Equal("确认通过", req.UserMessage);
    }

    [Fact]
    public void RequirementAnalysisOptions_UserMessage_DefaultNull()
    {
        var opts = new RequirementAnalysisOptions();
        Assert.Null(opts.UserMessage);
    }

    // ── 反射辅助：调 private static ClassifyUserIntent ──

    private static (string Intent, double Confidence) InvokeClassifyUserIntent(
        string input, bool hasSpecRendered, bool hasPendingClarification)
    {
        var fragments = new List<IrSnapshotFragment>();
        if (hasSpecRendered)
        {
            fragments.Add(new IrSnapshotFragment
            {
                FragmentType = IrFragmentTypes.Requirement,
                StabilityState = IrStabilityStates.Stable,
            });
        }
        if (hasPendingClarification)
        {
            fragments.Add(new IrSnapshotFragment
            {
                FragmentType = IrFragmentTypes.Clarification,
                StabilityState = IrStabilityStates.InProgress,
            });
        }

        var snapshot = new IrSnapshot { Fragments = fragments };

        var method = typeof(RequirementAnalysisOrchestrator)
            .GetMethod("ClassifyUserIntent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("ClassifyUserIntent not found");

        var result = ((string Intent, double Confidence))method.Invoke(null, new object[] { input, snapshot })!;
        return result;
    }
}
