using Foundry.FSPM.Compiler.WorkspaceNS;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Serializes with Phase 7 identity tests: MSBuild BuildManager allows only
// one concurrent design-time build (see RoslynWorkspaceCollection).
[Collection("RoslynWorkspace")]
public sealed class WorkspaceTests
{

    private static string GoldenCsproj =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Fixtures", "SemanticGolden", "SemanticGolden.csproj"));

    private static INamedTypeSymbol? FindUser(Compilation compilation)
    {
        var byName = compilation.GetTypeByMetadataName("SemanticGolden.Domain.User");
        if (byName != null) return byName;
        return compilation.GlobalNamespace.GetTypeMembers("User").FirstOrDefault();
    }

    [Fact]
    public async Task Debug_DumpCompilation()
    {
        var loader = new FspmProjectLoader();
        using var workspace = await loader.LoadAsync(GoldenCsproj);
        var compilation = await FspmProjectLoader.GetCompilationAsync(workspace);

        var project = workspace.Projects[0];
        System.Console.WriteLine("Project file: " + project.FilePath);
        System.Console.WriteLine($"Project Documents: {project.Documents.Count()}");
        foreach (var d in project.Documents)
        {
            System.Console.WriteLine("  Doc: " + d.FilePath);
        }

        System.Console.WriteLine($"ProjectReferences: {project.ProjectReferences.Count()}");
        System.Console.WriteLine($"MetadataReferences: {project.MetadataReferences.Count()}");
        System.Console.WriteLine($"AssemblyName: {compilation.AssemblyName}");
        System.Console.WriteLine($"SyntaxTrees: {compilation.SyntaxTrees.Count()}");
        System.Console.WriteLine($"ReferencedAssemblies: {compilation.ReferencedAssemblyNames.Count()}");
        foreach (var t in compilation.SyntaxTrees)
        {
            System.Console.WriteLine("  Tree: " + t.FilePath);
        }

        var diagCount = compilation.GetDiagnostics().Length;
        System.Console.WriteLine($"Compilation diagnostics: {diagCount}");
        foreach (var diag in compilation.GetDiagnostics().Take(5))
        {
            System.Console.WriteLine($"  Diag: {diag.Id} {diag.GetMessage()}");
        }

        // Traverse namespace to find SemanticGolden.Domain.User
        var globalNs = compilation.GlobalNamespace;
        var semanticGoldenNs = globalNs.GetNamespaceMembers().FirstOrDefault(n => n.Name == "SemanticGolden");
        if (semanticGoldenNs != null)
        {
            var domainNs = semanticGoldenNs.GetNamespaceMembers().FirstOrDefault(n => n.Name == "Domain");
            if (domainNs != null)
            {
                System.Console.WriteLine("SemanticGolden.Domain types:");
                foreach (var t in domainNs.GetTypeMembers())
                {
                    System.Console.WriteLine($"  Type: {t.ToDisplayString()} Kind={t.TypeKind}");
                }
            }
        }

        var user = FindUser(compilation);
        System.Console.WriteLine($"FindUser: {user?.ToDisplayString() ?? "<null>"}");
        if (user != null)
        {
            foreach (var m in user.GetMembers().OfType<IPropertySymbol>())
            {
                System.Console.WriteLine($"  Property: {m.Name} : {m.Type.ToDisplayString()}");
            }

            foreach (var m in user.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary))
            {
                System.Console.WriteLine($"  Method: {m.Name}({string.Join(", ", m.Parameters.Select(p => p.Type.ToDisplayString() + " " + p.Name))}) -> {m.ReturnType.ToDisplayString()} Static={m.IsStatic}");
            }
        }
    }

    [Fact]
    public async Task LoadSolutionAsync_RealGoldenProject_OpensSuccessfully()
    {
        var loader = new FspmProjectLoader();
        using var workspace = await loader.LoadAsync(GoldenCsproj);

        Assert.NotEmpty(workspace.Projects);
        Assert.NotNull(workspace.MSBuildWorkspace);
    }

    [Fact]
    public async Task GetCompilation_RealGoldenProject_ReturnsRealRoslynCompilation()
    {
        var loader = new FspmProjectLoader();
        using var workspace = await loader.LoadAsync(GoldenCsproj);
        var compilation = await FspmProjectLoader.GetCompilationAsync(workspace);

        Assert.NotNull(compilation);
        Assert.NotNull(compilation.AssemblyName);
        Assert.NotNull(compilation.SourceModule);
        Assert.NotNull(compilation.GlobalNamespace);
    }

    [Fact]
    public async Task RealGoldenProject_Contains_User_AsReal_INamedTypeSymbol()
    {
        var loader = new FspmProjectLoader();
        using var workspace = await loader.LoadAsync(GoldenCsproj);
        var compilation = await FspmProjectLoader.GetCompilationAsync(workspace);

        var userSymbol = FindUser(compilation);
        Assert.NotNull(userSymbol);
        Assert.Equal(TypeKind.Class, userSymbol!.TypeKind);
        Assert.True(userSymbol.IsSealed);
    }

    [Fact]
    public async Task RealGoldenProject_User_HasPhoneNumber_AsReal_IPropertySymbol()
    {
        var loader = new FspmProjectLoader();
        using var workspace = await loader.LoadAsync(GoldenCsproj);
        var compilation = await FspmProjectLoader.GetCompilationAsync(workspace);

        var userSymbol = FindUser(compilation);
        Assert.NotNull(userSymbol);

        var phoneNumber = userSymbol!.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name == "PhoneNumber");

        Assert.NotNull(phoneNumber);
        Assert.Equal(SpecialType.System_String, phoneNumber!.Type.SpecialType);
    }

    [Fact]
    public async Task RealGoldenProject_User_HasCreate_AsReal_IMethodSymbol()
    {
        var loader = new FspmProjectLoader();
        using var workspace = await loader.LoadAsync(GoldenCsproj);
        var compilation = await FspmProjectLoader.GetCompilationAsync(workspace);

        var userSymbol = FindUser(compilation);
        Assert.NotNull(userSymbol);

        var createMethod = userSymbol!.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Name == "Create" && m.IsStatic);

        Assert.NotNull(createMethod);
        Assert.Single(createMethod!.Parameters);
        Assert.Equal("phoneNumber", createMethod.Parameters[0].Name);
    }

    [Fact]
    public async Task LoadAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var loader = new FspmProjectLoader();
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => loader.LoadAsync("Z:/this/does/not/exist.csproj"));
    }

    [Fact]
    public async Task GetCompilation_EmptyWorkspace_Throws()
    {
        var ws = new FspmWorkspace
        {
            RootPath = "C:\\nope",
            MSBuildWorkspace = null!,
            Projects = Array.Empty<Project>(),
        };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => FspmProjectLoader.GetCompilationAsync(ws));
    }
}
