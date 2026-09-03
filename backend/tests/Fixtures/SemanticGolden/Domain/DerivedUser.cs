namespace SemanticGolden.Domain;

/// <summary>
/// Phase 8 inheritance fixture (施工包 §57): inherits <c>Name</c> from
/// <see cref="BaseUser"/> (must bind deterministically, never NOT_FOUND)
/// and overrides <c>Describe</c> (override collapses to ONE slot — must bind,
/// never false-ambiguous).
/// </summary>
public sealed class DerivedUser : BaseUser
{
    public string NickName { get; set; } = string.Empty;

    public override string Describe() => "derived";
}
