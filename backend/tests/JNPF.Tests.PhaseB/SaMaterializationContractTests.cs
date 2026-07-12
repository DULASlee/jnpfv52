using JNPF.InteAssistant.Sa;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 30 号 SG-C6 / W1-T6：物化写入契约回归（防 COMPILED / BIT 字符串回潮）。
/// </summary>
public class SaMaterializationContractTests
{
    [Fact]
    public void AllowedValidationStatuses_ContainsOnlyPassFailPending()
    {
        Assert.Equal(3, SaMaterializationContracts.AllowedValidationStatuses.Count);
        Assert.True(SaMaterializationContracts.IsAllowedValidationStatus("PASS"));
        Assert.True(SaMaterializationContracts.IsAllowedValidationStatus("FAIL"));
        Assert.True(SaMaterializationContracts.IsAllowedValidationStatus("PENDING"));
        Assert.False(SaMaterializationContracts.IsAllowedValidationStatus("COMPILED"));
        Assert.False(SaMaterializationContracts.IsAllowedValidationStatus("pass")); // 大小写敏感，与 CHECK 一致
        Assert.False(SaMaterializationContracts.IsAllowedValidationStatus(null));
    }

    [Fact]
    public void ComputeErValidationFlags_ReturnsBoolTuple_NotStrings()
    {
        var entities = """[{"name":"LeaveRequest"},{"name":"Employee"}]""";
        var rels = """[{"fromEntity":"LeaveRequest","toEntity":"Employee"}]""";

        var (fkInDict, thirdNormalForm, noCalculatedColumns) =
            SaMaterializationContracts.ComputeErValidationFlags(entities, rels, dictId: 1);

        Assert.True(fkInDict);
        Assert.True(thirdNormalForm);
        Assert.True(noCalculatedColumns);
        Assert.IsType<bool>(fkInDict);
    }

    [Fact]
    public void ComputeErValidationFlags_MissingToEntity_SetsFkFalse()
    {
        var entities = """[{"name":"LeaveRequest"}]""";
        var rels = """[{"fromEntity":"LeaveRequest","toEntity":"MissingDept"}]""";

        var (fkInDict, _, _) =
            SaMaterializationContracts.ComputeErValidationFlags(entities, rels, dictId: 1);

        Assert.False(fkInDict);
    }

    [Fact]
    public void StatusConstants_MatchAllowedSet()
    {
        Assert.Contains(SaMaterializationContracts.StatusPass, SaMaterializationContracts.AllowedValidationStatuses);
        Assert.Contains(SaMaterializationContracts.StatusFail, SaMaterializationContracts.AllowedValidationStatuses);
        Assert.Contains(SaMaterializationContracts.StatusPending, SaMaterializationContracts.AllowedValidationStatuses);
        Assert.DoesNotContain("COMPILED", SaMaterializationContracts.AllowedValidationStatuses);
    }
}
