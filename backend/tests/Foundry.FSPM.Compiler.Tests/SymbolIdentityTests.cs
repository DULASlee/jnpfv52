using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.WorkspaceNS;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// Phase 7 shared loader for REAL Roslyn compilations (no mocks, no fakes).
/// Each call performs an independent MSBuildWorkspace load — required for
/// determinism tests (directive §八). Callers own disposal of the workspace.
/// </summary>
internal static class GoldenIdentity
{
    internal static string GoldenCsproj =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Fixtures", "SemanticGolden", "SemanticGolden.csproj"));

    internal static async Task<(FspmWorkspace Workspace, Compilation Compilation)> LoadGoldenAsync()
    {
        var loader = new FspmProjectLoader();
        var workspace = await loader.LoadAsync(GoldenCsproj);
        var compilation = await FspmProjectLoader.GetCompilationAsync(workspace);
        return (workspace, compilation);
    }

    internal static INamedTypeSymbol RequireType(Compilation compilation, string metadataName) =>
        compilation.GetTypeByMetadataName(metadataName)
        ?? throw new InvalidOperationException($"Fixture broken: type '{metadataName}' not found.");

    internal static IPropertySymbol RequireProperty(INamedTypeSymbol type, string name) =>
        type.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == name)
        ?? throw new InvalidOperationException($"Fixture broken: property '{type.Name}.{name}' not found.");

    internal static IMethodSymbol RequireMethod(INamedTypeSymbol type, string name, string parameterTypeName) =>
        type.GetMembers().OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Name == name
                && m.MethodKind == MethodKind.Ordinary
                && m.Parameters.Length == 1
                && m.Parameters[0].Type.ToDisplayString() == parameterTypeName)
        ?? throw new InvalidOperationException(
            $"Fixture broken: method '{type.Name}.{name}({parameterTypeName})' not found.");
}

/// <summary>
/// G07-1 Type + G07-2 Property + G07-3 Method + G07-8 Real Roslyn + §十一 re-resolve.
/// </summary>
[Collection("RoslynWorkspace")]
public sealed class SymbolIdentityTests
{
    [Fact]
    public async Task G07_1_TypeIdentity_User_IsStableAndQualified()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var user = GoldenIdentity.RequireType(loaded.Compilation, "SemanticGolden.Domain.User");
            var id = FspmSymbolIdentity.Create(user);

            // Golden canonical value: assembly-qualified DocumentationCommentId.
            Assert.Equal("SemanticGolden|T:SemanticGolden.Domain.User", id.Value);
            Assert.Equal(FspmSymbolKind.Entity, FspmSymbolIdentity.GetKind(id));
            Assert.IsAssignableFrom<INamedTypeSymbol>(user);
        }
    }

    [Fact]
    public async Task G07_2_PropertyIdentity_PhoneNumber_CarriesContainingTypeAndStringType()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var user = GoldenIdentity.RequireType(loaded.Compilation, "SemanticGolden.Domain.User");
            var phone = GoldenIdentity.RequireProperty(user, "PhoneNumber");
            var id = FspmSymbolIdentity.Create(phone);

            Assert.Equal("SemanticGolden|P:SemanticGolden.Domain.User.PhoneNumber", id.Value);
            Assert.Equal(FspmSymbolKind.Property, FspmSymbolIdentity.GetKind(id));
            Assert.IsAssignableFrom<IPropertySymbol>(phone);
            Assert.Equal(SpecialType.System_String, phone.Type.SpecialType);
        }
    }

    [Fact]
    public async Task G07_3_MethodIdentity_CreateString_EncodesParameterType()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var user = GoldenIdentity.RequireType(loaded.Compilation, "SemanticGolden.Domain.User");
            var create = GoldenIdentity.RequireMethod(user, "Create", "string");
            var id = FspmSymbolIdentity.Create(create);

            Assert.Equal("SemanticGolden|M:SemanticGolden.Domain.User.Create(System.String)~SemanticGolden.Domain.User", id.Value);
            Assert.Equal(FspmSymbolKind.Operation, FspmSymbolIdentity.GetKind(id));
            Assert.IsAssignableFrom<IMethodSymbol>(create);
            Assert.True(create.IsStatic);
        }
    }

    [Fact]
    public async Task G07_8_and_11_ReResolve_TwiceFetchedSymbol_YieldsSameId_WithoutReferenceEquality()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var compilation = loaded.Compilation;

            // Directive §十一: fetch the SAME symbol twice via metadata name.
            var first = GoldenIdentity.RequireType(compilation, "SemanticGolden.Domain.User");
            var second = GoldenIdentity.RequireType(compilation, "SemanticGolden.Domain.User");

            var idFirst = FspmSymbolIdentity.Create(first);
            var idSecond = FspmSymbolIdentity.Create(second);

            Assert.Equal(idFirst, idSecond);

            // §七 proof: Resolve(identity) returns the same SEMANTIC symbol.
            var resolved = FspmSymbolIdentity.Resolve(idFirst, compilation);
            Assert.Equal(idFirst, FspmSymbolIdentity.Create((INamedTypeSymbol)resolved));
            Assert.Equal("User", resolved.Name);
        }
    }

    [Fact]
    public async Task Resolve_UnknownAssembly_ThrowsInsteadOfGuessing()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var bogus = new FspmSymbolId("NoSuchAssembly|T:SemanticGolden.Domain.User");
            Assert.Throws<InvalidOperationException>(
                () => FspmSymbolIdentity.Resolve(bogus, loaded.Compilation));
        }
    }

    [Fact]
    public async Task Resolve_MalformedId_ThrowsArgumentException()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            Assert.Throws<ArgumentException>(
                () => FspmSymbolIdentity.Resolve(new FspmSymbolId("no-separator"), loaded.Compilation));
        }
    }
}

/// <summary>
/// G07-4 Overload identity (directive §十).
/// </summary>
[Collection("RoslynWorkspace")]
public sealed class OverloadIdentityTests
{
    [Fact]
    public async Task G07_4_CreateString_And_CreateInt_HaveDifferentIds()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var compilation = loaded.Compilation;
            var user = GoldenIdentity.RequireType(compilation, "SemanticGolden.Domain.User");
            var createString = GoldenIdentity.RequireMethod(user, "Create", "string");
            var createInt = GoldenIdentity.RequireMethod(user, "Create", "int");

            var idString = FspmSymbolIdentity.Create(createString);
            var idInt = FspmSymbolIdentity.Create(createInt);

            Assert.Equal("SemanticGolden|M:SemanticGolden.Domain.User.Create(System.String)~SemanticGolden.Domain.User", idString.Value);
            Assert.Equal("SemanticGolden|M:SemanticGolden.Domain.User.Create(System.Int32)~SemanticGolden.Domain.User", idInt.Value);
            Assert.NotEqual(idString, idInt);

            // Each overload re-resolves to its OWN semantic symbol.
            var resolvedString = (IMethodSymbol)FspmSymbolIdentity.Resolve(idString, compilation);
            var resolvedInt = (IMethodSymbol)FspmSymbolIdentity.Resolve(idInt, compilation);
            Assert.Equal("string", resolvedString.Parameters[0].Type.ToDisplayString());
            Assert.Equal("int", resolvedInt.Parameters[0].Type.ToDisplayString());
        }
    }
}

/// <summary>
/// G07-6 Determinism (directive §八): two independent loads → identical IDs.
/// </summary>
[Collection("RoslynWorkspace")]
public sealed class IdentityDeterminismTests
{
    [Fact]
    public async Task G07_6_TwoIndependentLoads_ProduceIdenticalIds()
    {
        var run1 = await GoldenIdentity.LoadGoldenAsync();
        using (run1.Workspace)
        {
            var run2 = await GoldenIdentity.LoadGoldenAsync();
            using (run2.Workspace)
            {
                // Run #1
                var user1 = GoldenIdentity.RequireType(run1.Compilation, "SemanticGolden.Domain.User");
                var userId1 = FspmSymbolIdentity.Create(user1);
                var phoneId1 = FspmSymbolIdentity.Create(GoldenIdentity.RequireProperty(user1, "PhoneNumber"));
                var createId1 = FspmSymbolIdentity.Create(GoldenIdentity.RequireMethod(user1, "Create", "string"));

                // Run #2
                var user2 = GoldenIdentity.RequireType(run2.Compilation, "SemanticGolden.Domain.User");
                var userId2 = FspmSymbolIdentity.Create(user2);
                var phoneId2 = FspmSymbolIdentity.Create(GoldenIdentity.RequireProperty(user2, "PhoneNumber"));
                var createId2 = FspmSymbolIdentity.Create(GoldenIdentity.RequireMethod(user2, "Create", "string"));

                Assert.Equal(userId1, userId2);
                Assert.Equal(phoneId1, phoneId2);
                Assert.Equal(createId1, createId2);
            }
        }
    }
}

/// <summary>
/// G07-7 Collision + G07-5 CrossProject (directive §九).
/// Same short names must NEVER merge across types / namespaces / assemblies.
/// </summary>
[Collection("RoslynWorkspace")]
public sealed class IdentityCollisionTests
{
    [Fact]
    public async Task G07_7_SameMemberName_DifferentContainingTypes_DoNotMerge()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var user = GoldenIdentity.RequireType(loaded.Compilation, "SemanticGolden.Domain.User");
            var other = GoldenIdentity.RequireType(loaded.Compilation, "SemanticGolden.Domain.OtherUser");

            Assert.NotEqual(FspmSymbolIdentity.Create(user), FspmSymbolIdentity.Create(other));

            var userPhone = FspmSymbolIdentity.Create(GoldenIdentity.RequireProperty(user, "PhoneNumber"));
            var otherPhone = FspmSymbolIdentity.Create(GoldenIdentity.RequireProperty(other, "PhoneNumber"));

            Assert.NotEqual(userPhone, otherPhone);
        }
    }

    [Fact]
    public async Task G07_7_SameShortName_DifferentNamespaces_DoNotMerge()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var domainUser = FspmSymbolIdentity.Create(
                GoldenIdentity.RequireType(loaded.Compilation, "SemanticGolden.Domain.User"));
            var nsAUser = FspmSymbolIdentity.Create(
                GoldenIdentity.RequireType(loaded.Compilation, "SemanticGolden.NamespaceA.User"));
            var nsBUser = FspmSymbolIdentity.Create(
                GoldenIdentity.RequireType(loaded.Compilation, "SemanticGolden.NamespaceB.User"));

            Assert.NotEqual(domainUser, nsAUser);
            Assert.NotEqual(domainUser, nsBUser);
            Assert.NotEqual(nsAUser, nsBUser);
        }
    }

    [Fact]
    public async Task G07_5_CrossProject_SameShortName_DifferentAssemblies_DoNotMerge()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var contractsProject = loaded.Workspace.Projects.FirstOrDefault(
                p => string.Equals(p.AssemblyName, "SemanticGolden.Contracts", StringComparison.Ordinal));
            Assert.NotNull(contractsProject);

            var contractsCompilation = await contractsProject!.GetCompilationAsync();
            Assert.NotNull(contractsCompilation);

            var contractsUser = GoldenIdentity.RequireType(contractsCompilation!, "SemanticGolden.Contracts.User");
            var contractsId = FspmSymbolIdentity.Create(contractsUser);

            Assert.Equal("SemanticGolden.Contracts|T:SemanticGolden.Contracts.User", contractsId.Value);

            // Same short name "User" as Domain.User, but different assembly → different ID.
            var domainUser = GoldenIdentity.RequireType(loaded.Compilation, "SemanticGolden.Domain.User");
            var domainId = FspmSymbolIdentity.Create(domainUser);

            Assert.NotEqual(domainId, contractsId);

            // Each resolves inside its OWN compilation (no cross-contamination).
            var resolvedContracts = FspmSymbolIdentity.Resolve(contractsId, contractsCompilation!);
            Assert.Equal("SemanticGolden.Contracts", resolvedContracts.ContainingNamespace.ToDisplayString());
            var resolvedDomain = FspmSymbolIdentity.Resolve(domainId, loaded.Compilation);
            Assert.Equal("SemanticGolden.Domain", resolvedDomain.ContainingNamespace.ToDisplayString());
        }
    }
}
