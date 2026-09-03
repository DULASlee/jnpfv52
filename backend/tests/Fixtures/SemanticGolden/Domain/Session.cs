namespace SemanticGolden.Domain;

/// <summary>
/// Phase 8 E2E fixture: a uniquely-named type with exactly ONE ordinary method,
/// so the parser-expressible short-name path
/// (<c>entity Session</c> / <c>operation Session.Ping</c>) has a truthful
/// Success outcome. Contrasts with <see cref="User"/> whose short name is
/// ambiguous (NamespaceA/B + Contracts collisions) and whose Create has two
/// overloads (AmbiguousOperation).
/// </summary>
public sealed class Session
{
    public string SessionId { get; set; } = string.Empty;

    public bool Ping() => true;
}
