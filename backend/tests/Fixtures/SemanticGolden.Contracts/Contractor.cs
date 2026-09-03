namespace SemanticGolden.Contracts;

/// <summary>
/// Phase 10 G10-8 / G10-9 sole-member fixture.
/// <c>Contractor</c> lives ONLY in this assembly. A short-name
/// <c>property Contractor.LicenseNumber</c> therefore resolves to the
/// Contracts assembly's <c>LicenseNumber</c> property — never to a
/// same-named type in another assembly (there isn't one) — proving
/// the binder's owner lookup returns EXACTLY ONE candidate.
/// </summary>
public sealed class Contractor
{
    public string LicenseNumber { get; set; } = string.Empty;
}
