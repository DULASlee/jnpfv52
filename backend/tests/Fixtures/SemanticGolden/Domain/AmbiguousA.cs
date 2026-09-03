namespace SemanticGolden.NamespaceA;

/// <summary>
/// Phase 7 namespace-collision fixture (directive §九).
/// Short name <c>User</c> collides with <c>NamespaceB.User</c> and
/// <c>SemanticGolden.Domain.User</c> — only the fully qualified identity
/// (namespace + assembly) may distinguish them.
/// </summary>
public sealed class User
{
    public string Code { get; set; } = string.Empty;
}
