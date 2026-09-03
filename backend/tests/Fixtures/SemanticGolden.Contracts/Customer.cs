namespace SemanticGolden.Contracts;

/// <summary>
/// Phase 10 G10-8 / G10-9 Cross Assembly fixture.
/// Same SHORT name <c>Customer</c> lives in two distinct assemblies
/// (<c>SemanticGolden</c> and <c>SemanticGolden.Contracts</c>) with a
/// different property name each. Phase 7 Identity MUST discriminate
/// them via the <c>AssemblySimpleName</c> segment — proving that a
/// short-name property declaration walks the binder's owner lookup
/// to a UNIQUE assembly and never silently picks the wrong one.
/// </summary>
public sealed class Customer
{
    public string ExternalId { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}
