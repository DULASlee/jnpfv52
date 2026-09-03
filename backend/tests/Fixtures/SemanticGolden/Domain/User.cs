namespace SemanticGolden.Domain;

/// <summary>
/// Phase 6 / Phase 11 Golden Fixture (施工包 §50 + 首席架构师 directive).
/// Used to verify that the FSPM Compiler can find REAL Roslyn INamedTypeSymbol /
/// IPropertySymbol / IMethodSymbol via MSBuildWorkspace.
///
/// NOT a fixture of convenience: every test in WorkspaceTests.cs depends on the
/// real Compilation produced by SemanticGolden.csproj + this file.
/// </summary>
public sealed class User
{
    public string UserName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public bool Login() => true;

    public static User Create(string phoneNumber)
    {
        return new User
        {
            PhoneNumber = phoneNumber,
        };
    }

    /// <summary>
    /// Phase 7 overload fixture (directive §十).
    /// Must produce a DIFFERENT FspmSymbolId than <see cref="Create(string)"/>.
    /// Declared AFTER the string overload so Phase 6 WorkspaceTests
    /// (FirstOrDefault string Create) keep resolving the string overload.
    /// </summary>
    public static User Create(int legacyId)
    {
        return new User
        {
            PhoneNumber = legacyId.ToString(),
        };
    }
}
