namespace SemanticGolden.NotReferenced;

/// <summary>
/// Phase 10 G10-8 Cross Project / missing-ProjectReference fixture.
/// Declared in its own project / assembly (<c>SemanticGolden.NotReferenced</c>)
/// that is intentionally NOT referenced by <c>SemanticGolden.csproj</c>. Tests
/// confirm the binder never reaches into this assembly by name, so a
/// short-name <c>entity User</c> binding against the SemanticGolden workspace
/// MUST resolve only to the four already-referenced assemblies (Domain +
/// NamespaceA + NamespaceB + Contracts) and stay Ambiguous — proving the
/// binder does not silently look at un-referenced assemblies.
/// </summary>
public sealed class User
{
    public string Email { get; set; } = string.Empty;
}
