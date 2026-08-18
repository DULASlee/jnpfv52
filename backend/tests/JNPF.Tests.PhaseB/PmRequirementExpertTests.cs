using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

public class PmRequirementExpertTests
{
    [Fact]
    public void ClarificationQuestion_DefaultFormat_IsMulti()
    {
        var question = new ClarificationQuestion();

        Assert.Equal("MULTI", question.QuestionFormat);
    }

    [Theory]
    [InlineData(null, "MULTI")]
    [InlineData("", "MULTI")]
    [InlineData("single", "MULTI")]
    [InlineData("MULTI", "MULTI")]
    [InlineData("MATRIX_SINGLE", "MATRIX_SINGLE")]
    [InlineData("matrix_multi", "MATRIX_MULTI")]
    public void NormalizeQuestionFormat_UpgradesPlainSingle_AndPreservesMatrix(string? input, string expected)
    {
        Assert.Equal(expected, ClarificationQuestion.NormalizeQuestionFormat(input));
    }

    [Fact]
    public void ParseSpecReviewResult_ReadsScoreVerdictAndGaps()
    {
        var result = PmSkillService.ParseSpecReviewResult("""
            {"score":84,"verdict":"fail","gaps":["缺少异常流程","实体表不完整"]}
            """);

        Assert.Equal(84, result.Score);
        Assert.Equal("fail", result.Verdict);
        Assert.Equal(new[] { "缺少异常流程", "实体表不完整" }, result.Gaps);
    }
}
