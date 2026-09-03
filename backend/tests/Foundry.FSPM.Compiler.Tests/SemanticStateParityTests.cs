using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.SemanticModel;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// G14-01-STATE-01: the two 8-state enums are one protocol in two
// assemblies. Name + ordinal + count parity, plus a full 8-way mapping
// assertion through FspmSemanticStateMapper. Any drift fails here.
public sealed class SemanticStateParityTests
{
    [Fact]
    public void Member_Count_Matches()
    {
        Assert.Equal(
            System.Enum.GetValues<FspmResolutionStatus>().Length,
            System.Enum.GetValues<FspmSemanticState>().Length);
    }

    [Fact]
    public void Names_Match_In_Order()
    {
        var compilerNames = System.Enum.GetNames<FspmResolutionStatus>();
        var modelNames = System.Enum.GetNames<FspmSemanticState>();

        Assert.Equal(compilerNames, modelNames);
    }

    [Fact]
    public void Ordinals_Match()
    {
        foreach (var name in System.Enum.GetNames<FspmResolutionStatus>())
        {
            Assert.Equal(
                (int)System.Enum.Parse<FspmResolutionStatus>(name),
                (int)System.Enum.Parse<FspmSemanticState>(name));
        }
    }

    [Theory]
    [InlineData(FspmResolutionStatus.Resolved, FspmSemanticState.Resolved)]
    [InlineData(FspmResolutionStatus.NotFound, FspmSemanticState.NotFound)]
    [InlineData(FspmResolutionStatus.Ambiguous, FspmSemanticState.Ambiguous)]
    [InlineData(FspmResolutionStatus.Invalid, FspmSemanticState.Invalid)]
    [InlineData(FspmResolutionStatus.Unsupported, FspmSemanticState.Unsupported)]
    [InlineData(FspmResolutionStatus.Degraded, FspmSemanticState.Degraded)]
    [InlineData(FspmResolutionStatus.Cancelled, FspmSemanticState.Cancelled)]
    [InlineData(FspmResolutionStatus.InfrastructureFailure, FspmSemanticState.InfrastructureFailure)]
    public void Mapper_Maps_Every_State(FspmResolutionStatus from, FspmSemanticState expected)
    {
        Assert.Equal(expected, FspmSemanticStateMapper.FromResolutionStatus(from));
    }
}
