namespace SemanticGolden.Domain;

/// <summary>
/// Phase 7 collision fixture (directive §九).
/// Same member name <c>PhoneNumber</c> as <see cref="User"/>, but a
/// DIFFERENT containing type — must produce a DIFFERENT FspmSymbolId.
/// </summary>
public sealed class OtherUser
{
    public string PhoneNumber { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
