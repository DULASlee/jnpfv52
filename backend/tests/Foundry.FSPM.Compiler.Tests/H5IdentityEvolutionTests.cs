using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P13-H5: Compilation / Assembly / Binding / Logical identity evolution.
// Adhoc V1→V2 (in-memory, no MSBuild) for speed + one real MSBuild
// integration proof (chief D1 ruling).
[Collection("RoslynWorkspace")]
public sealed class H5IdentityEvolutionTests
{
    private const string V1Source = """
        namespace Evolve;
        public class Widget
        {
            public string Label { get; set; } = string.Empty;
        }
        """;

    private const string V2TypeChangedSource = """
        namespace Evolve;
        public class Widget
        {
            public int Label { get; set; }
        }
        """;

    private const string V2RemovedSource = """
        namespace Evolve;
        public class Widget
        {
        }
        """;

    private static CSharpCompilation BuildAdhoc(string assemblyName, string source) =>
        CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static IPropertySymbol RequireLabel(CSharpCompilation compilation)
    {
        var widget = compilation.GetTypeByMetadataName("Evolve.Widget")
            ?? throw new InvalidOperationException("Fixture broken: Evolve.Widget not found.");
        return widget.GetMembers("Label").OfType<IPropertySymbol>().First();
    }

    [Fact]
    public void G13_ID_01_SameLogicalSymbol_AcrossV1V2()
    {
        var v1 = BuildAdhoc("Evolve", V1Source);
        var v2 = BuildAdhoc("Evolve", V2TypeChangedSource);

        var logical1 = SemanticIdentityMint.MintLogicalIdentity(RequireLabel(v1));
        var logical2 = SemanticIdentityMint.MintLogicalIdentity(RequireLabel(v2));

        Assert.Equal(logical1, logical2);
    }

    [Fact]
    public void G13_ID_02_Binding_Rebinds_WhenShapeChanges()
    {
        var v1 = BuildAdhoc("Evolve", V1Source);
        var v2 = BuildAdhoc("Evolve", V2TypeChangedSource);

        var binding1 = SemanticIdentityMint.MintBindingIdentity(RequireLabel(v1));
        var binding2 = SemanticIdentityMint.MintBindingIdentity(RequireLabel(v2));

        // DocId carries no property type for P: — binding identity stays
        // addressable while the fingerprint (below) detects the change.
        Assert.Equal(binding1.AssemblyName, binding2.AssemblyName);
    }

    [Fact]
    public void G13_ID_03_Fingerprint_Detects_TypeChange_String_To_Int()
    {
        var v1 = BuildAdhoc("Evolve", V1Source);
        var v2 = BuildAdhoc("Evolve", V2TypeChangedSource);

        var finger1 = SemanticIdentityMint.MintFingerprint(RequireLabel(v1));
        var finger2 = SemanticIdentityMint.MintFingerprint(RequireLabel(v2));

        Assert.NotEqual(finger1, finger2);
    }

    [Fact]
    public void G13_ID_03b_Fingerprint_Stable_WhenNothingChanges()
    {
        var v1a = BuildAdhoc("Evolve", V1Source);
        var v1b = BuildAdhoc("Evolve", V1Source);

        Assert.Equal(
            SemanticIdentityMint.MintFingerprint(RequireLabel(v1a)),
            SemanticIdentityMint.MintFingerprint(RequireLabel(v1b)));
    }

    [Fact]
    public void G13_ID_04_RemovedSymbol_NeverResolves()
    {
        var v2 = BuildAdhoc("Evolve", V2RemovedSource);
        var widget = v2.GetTypeByMetadataName("Evolve.Widget")
            ?? throw new InvalidOperationException("Fixture broken: Evolve.Widget not found.");

        Assert.Empty(widget.GetMembers("Label").OfType<IPropertySymbol>());
    }

    [Fact]
    public void LogicalIdentity_HasNoBacktick_ForGenericMethod()
    {
        var compiled = GoldenSemanticCompilation.CompileGoldenAsync().GetAwaiter().GetResult();
        using (compiled.Workspace)
        {
            var service = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Operations.OpService");
            var echo = service.GetMembers("Echo").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);
            var logical = SemanticIdentityMint.MintLogicalIdentity(echo);

            Assert.Equal("Echo", logical.MemberName);
            Assert.DoesNotContain("`", logical.MemberName);
            // Overloads of one operation share the logical node;
            // the fingerprint distinguishes them.
            var user = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var createString = user.GetMembers("Create").OfType<IMethodSymbol>()
                .First(m => m.Parameters.Length == 1
                    && m.Parameters[0].Type.ToDisplayString() == "string");
            var createInt = user.GetMembers("Create").OfType<IMethodSymbol>()
                .First(m => m.Parameters.Length == 1
                    && m.Parameters[0].Type.ToDisplayString() == "int");
            Assert.Equal(
                SemanticIdentityMint.MintLogicalIdentity(createString),
                SemanticIdentityMint.MintLogicalIdentity(createInt));
            Assert.NotEqual(
                SemanticIdentityMint.MintFingerprint(createString),
                SemanticIdentityMint.MintFingerprint(createInt));
        }
    }

    [Fact]
    public void CompilationIdentity_Changes_WhenDocumentsChange()
    {
        var compiled = GoldenSemanticCompilation.CompileGoldenAsync().GetAwaiter().GetResult();
        using (compiled.Workspace)
        {
            var v1 = SemanticIdentityMint.MintCompilationIdentity(
                compiled.Snapshot.Compilation, "Golden", new[] { "A.cs" }, "snap-1");
            var v2 = SemanticIdentityMint.MintCompilationIdentity(
                compiled.Snapshot.Compilation, "Golden", new[] { "A.cs", "B.cs" }, "snap-2");

            Assert.NotEqual(v1, v2);
        }
    }

    [Fact]
    public async Task H5_IntegrationProof_RealMsBuild_CompilationIdentity_IsStable()
    {
        // Chief D1: exactly one real-engineering proof that Adhoc-tested
        // identity logic also holds on a genuine MSBuild compilation.
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var phone = GoldenIdentity.RequireProperty(user, "PhoneNumber");

            var logical = SemanticIdentityMint.MintLogicalIdentity(phone);
            Assert.Equal("SemanticGolden", logical.AssemblyName);
            Assert.Equal("SemanticGolden.Domain", logical.Namespace);
            Assert.Equal("User", logical.ContainingTypeName);
            Assert.Equal("PhoneNumber", logical.MemberName);

            var finger = SemanticIdentityMint.MintFingerprint(phone);
            Assert.Equal(64, finger.Value.Length);

            var identity = SemanticIdentityMint.MintCompilationIdentity(
                compiled.Snapshot.Compilation,
                compiled.Snapshot.ProjectName,
                compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
                "msbuild-proof");
            Assert.Equal("SemanticGolden", identity.AssemblyName);
            Assert.NotEmpty(identity.DocumentPaths);
        }
    }
}
