using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Chief §十七 isolation proof: a client compiled against
// Foundry.FSPM.Compiler.dll WITHOUT any Roslyn references must be able
// to read a full NativeSemanticFact (Kind/Identity/TypeShape/Signature/
// Visibility/Assembly/SourceAnchor/Diagnostic). If any Fact field leaked
// a Roslyn runtime object, this client could not compile or run.
[Collection("RoslynWorkspace")]
public sealed class FactIsolationTests
{
    private const string ClientSource = """
        using System;
        using Foundry.FSPM.Compiler.Semantic;
        using Foundry.FSPM.Compiler.Symbols;

        public static class FactClient
        {
            public static string Describe(NativeSemanticFact fact)
            {
                return string.Join("|",
                    fact.Kind.ToString(),
                    fact.Identity.Value,
                    fact.Name,
                    fact.QualifiedName,
                    fact.Visibility.Accessibility,
                    fact.TypeShape != null ? fact.TypeShape.NullableAnnotation : "<no-shape>",
                    fact.Operation != null ? fact.Operation.Name : "<no-op>",
                    fact.Assembly.Name,
                    fact.Anchor.DocumentIdentity,
                    fact.Status.ToString(),
                    fact.Quality.ToString(),
                    fact.Diagnostics.Count.ToString(),
                    fact.Logical.MemberName,
                    fact.Fingerprint.Value.Substring(0, 8));
            }
        }
        """;

    [Fact]
    public async Task IsolatedClient_WithoutRoslynRefs_Reads_FullFact()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var phone = GoldenIdentity.RequireProperty(user, "PhoneNumber");

            var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
                compiled.Snapshot.Compilation,
                compiled.Snapshot.ProjectName,
                compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
                "isolation-proof");
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(phone),
                phone.Locations.First().SourceTree?.FilePath ?? "<unknown>",
                DocumentationCommentId.CreateDeclarationId(phone) ?? "<none>",
                FspmSourceLocation.From(phone.Locations.First()));

            var fact = NativeSemanticFactFactory.Create(phone, compilationIdentity, anchor);

            // Compile the client with ZERO Roslyn assemblies referenced.
            var fspmAssembly = typeof(NativeSemanticFact).Assembly.Location;
            var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var clientCompilation = CSharpCompilation.Create(
                "FactClient",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(ClientSource) },
                references: new MetadataReference[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(fspmAssembly),
                    MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            EmitResult emit = clientCompilation.Emit(ms);
            Assert.True(emit.Success,
                "Isolated client must compile without Roslyn references: " +
                string.Join("; ", emit.Diagnostics.Select(d => d.GetMessage())));

            var clientAssembly = Assembly.Load(ms.ToArray());
            var clientType = clientAssembly.GetType("FactClient")
                ?? throw new InvalidOperationException("Fixture broken: FactClient type missing.");
            var describe = clientType.GetMethod("Describe")
                ?? throw new InvalidOperationException("Fixture broken: Describe method missing.");
            var summary = (string)describe.Invoke(null, new object[] { fact })!;

            Assert.Contains("Property", summary);
            Assert.Contains("SemanticGolden|P:SemanticGolden.Domain.User.PhoneNumber", summary);
            Assert.Contains("PhoneNumber", summary);
            Assert.Contains("Public", summary);
            Assert.Contains("SemanticGolden", summary);
            Assert.Contains("Resolved", summary);
            Assert.Contains("Perfect", summary);
        }
    }

    [Fact]
    public async Task OperationFact_IsolatedClient_Reads_Signature()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var create = GoldenIdentity.RequireMethod(user, "Create", "string");

            var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
                compiled.Snapshot.Compilation,
                compiled.Snapshot.ProjectName,
                compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
                "isolation-proof-op");
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(create),
                create.Locations.First().SourceTree?.FilePath ?? "<unknown>",
                DocumentationCommentId.CreateDeclarationId(create) ?? "<none>",
                FspmSourceLocation.From(create.Locations.First()));

            var fact = NativeSemanticFactFactory.Create(create, compilationIdentity, anchor);

            Assert.Equal("Create", fact.Operation!.Name);
            Assert.Equal("string", fact.Operation.Parameters[0].ParameterType);
            Assert.Equal(NativeSymbolKind.Method, fact.Kind);
        }
    }
}
