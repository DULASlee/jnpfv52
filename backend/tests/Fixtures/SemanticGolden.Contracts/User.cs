namespace SemanticGolden.Contracts;

/// <summary>
/// Phase 7 cross-project fixture (directive §九, G07-5).
/// Same short name <c>User</c> as <c>SemanticGolden.Domain.User</c> but lives
/// in a DIFFERENT project/assembly (<c>SemanticGolden.Contracts</c>).
/// Referenced by SemanticGolden.csproj via ProjectReference so a single
/// MSBuildWorkspace load exposes BOTH compilations.
/// Identity MUST differ by assembly even though namespace+name rhyme.
/// </summary>
public sealed class User
{
    public string ExternalId { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}
