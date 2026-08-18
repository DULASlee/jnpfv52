using JNPF.Bridges;
using Xunit;

namespace JNPF.Tests.Architecture;

/// <summary>
/// Characterization for W4 trigger remapping (CreateInte semantics).
/// </summary>
public sealed class InteAssistantBridgeTests
{
    [Theory]
    [InlineData(4, 1)]
    [InlineData(5, 3)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    public void ToStoredTriggerType_MatchesCreateInteRemap(int eventTrigger, int expectedStored)
    {
        Assert.Equal(expectedStored, InteAssistantTriggerTypes.ToStoredTriggerType(eventTrigger));
    }
}
