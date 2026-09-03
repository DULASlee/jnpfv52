namespace SemanticGolden.NamespaceB;

/// <summary>
/// Phase 7 namespace-collision fixture (directive §九).
/// Counterpart of <c>NamespaceA.User</c> — must produce a DIFFERENT
/// FspmSymbolId despite the identical short name.
/// </summary>
public sealed class User
{
    public string Code { get; set; } = string.Empty;
}
