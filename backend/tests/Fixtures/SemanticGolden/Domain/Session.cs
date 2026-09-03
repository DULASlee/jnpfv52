namespace SemanticGolden.Domain;

/// <summary>
/// Phase 8 / Phase 10 E2E fixture: a uniquely-named type so
/// <c>entity Session</c> / <c>operation Session.Ping</c> and
/// <c>operation Session.Lookup</c> resolve to a SINGLE owning type.
/// Session also carries two <c>Lookup</c> overloads so Phase 10 G10-7 can
/// prove that a short-name operation with no FSPM parameter syntax reports
/// AmbiguousOperation — never First() / Last().
/// </summary>
public sealed class Session
{
    public string SessionId { get; set; } = string.Empty;

    public bool Ping() => true;

    public string Lookup(string byId) => byId;

    public string Lookup(int byNumericId) => byNumericId.ToString();
}
