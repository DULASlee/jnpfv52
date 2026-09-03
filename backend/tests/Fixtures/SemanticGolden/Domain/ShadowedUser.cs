namespace SemanticGolden.Domain;

/// <summary>
/// Phase 8 shadowing fixture: <c>new</c>-shadows <see cref="BaseUser.Name"/>
/// with a DIFFERENT property symbol. Two distinct property symbols share the
/// name → binding must report Ambiguous (never First()).
/// </summary>
public sealed class ShadowedUser : BaseUser
{
#pragma warning disable CS0108 // Intentional shadowing fixture for ambiguity tests.
    public new string Name { get; set; } = string.Empty;
#pragma warning restore CS0108
}
