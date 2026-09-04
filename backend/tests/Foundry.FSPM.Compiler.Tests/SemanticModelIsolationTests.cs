using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// G14-01-API-01 + G14-01-API-02: the SemanticModel assembly builds with
// zero Roslyn references, and an external consumer compiles against
// SemanticModel alone (no Roslyn, no Compiler) yet reads a full
// Identity + Anchor + State.
public sealed class SemanticModelIsolationTests
{
    private const string ClientSource = """
        using System;
        using Foundry.FSPM.SemanticModel;

        public static class ModelClient
        {
            public static string Describe(
                FspmSemanticIdentity identity,
                FspmSemanticAnchor anchor,
                FspmSemanticState state)
            {
                return string.Join("|",
                    identity.LogicalId,
                    identity.Fingerprint,
                    anchor.Document,
                    anchor.DeclarationAnchor,
                    anchor.StartLine.ToString(),
                    state.ToString(),
                    ((int)state).ToString());
            }

            public static string DescribeRef(
                FspmSemanticReference reference,
                FspmReferenceStatus status)
            {
                return string.Join("|",
                    reference.TargetIdentity.LogicalId,
                    reference.DisplayName,
                    reference.OwnerId,
                    reference.ExpectedFingerprint,
                    status.ToString(),
                    ((int)status).ToString());
            }

            public static string DescribeConstruction(
                FspmConstruction construction,
                FspmConstructionValidation validation)
            {
                return string.Join("|",
                    construction.Id,
                    construction.Kind,
                    construction.State.ToString(),
                    construction.Fingerprint,
                    validation.IsValid.ToString(),
                    validation.Bindings.Count.ToString(),
                    validation.Issues.Count.ToString());
            }
        }
        """;

    [Fact]
    public void Consumer_Compiles_Against_SemanticModel_Only_And_Reads_Full_Contract()
    {
        var modelAssembly = typeof(FspmSemanticState).Assembly.Location;
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        var clientCompilation = CSharpCompilation.Create(
            "ModelClient",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(ClientSource) },
            references: new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(modelAssembly),
                MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        EmitResult emit = clientCompilation.Emit(ms);
        Assert.True(emit.Success,
            "Consumer must compile against SemanticModel with zero Roslyn references: " +
            string.Join("; ", emit.Diagnostics.Select(d => d.GetMessage())));

        var clientAssembly = Assembly.Load(ms.ToArray());
        var clientType = clientAssembly.GetType("ModelClient")
            ?? throw new InvalidOperationException("Fixture broken: ModelClient missing.");
        var describe = clientType.GetMethod("Describe")
            ?? throw new InvalidOperationException("Fixture broken: Describe missing.");

        var summary = (string)describe.Invoke(null, new object[]
        {
            new FspmSemanticIdentity("SemanticGolden|P:SemanticGolden.Domain.User.PhoneNumber", "0123456789ABCDEF"),
            new FspmSemanticAnchor("User.cs", "P:SemanticGolden.Domain.User.PhoneNumber", 10, 5, 10, 30),
            FspmSemanticState.Resolved,
        })!;

        Assert.Contains("SemanticGolden", summary);
        Assert.Contains("PhoneNumber", summary);
        Assert.Contains("User.cs", summary);
        Assert.Contains("Resolved", summary);
    }

    [Fact]
    public void G14_02_ROSYNLISOLATION_Reference_Types_Readable_Without_Roslyn()
    {
        var modelAssembly = typeof(FspmSemanticState).Assembly.Location;
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        var clientCompilation = CSharpCompilation.Create(
            "ModelClient",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(ClientSource) },
            references: new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(modelAssembly),
                MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        EmitResult emit = clientCompilation.Emit(ms);
        Assert.True(emit.Success,
            "Reference records must compile against SemanticModel with zero Roslyn references: " +
            string.Join("; ", emit.Diagnostics.Select(d => d.GetMessage())));

        var clientAssembly = Assembly.Load(ms.ToArray());
        var clientType = clientAssembly.GetType("ModelClient")
            ?? throw new InvalidOperationException("Fixture broken: ModelClient missing.");
        var describeRef = clientType.GetMethod("DescribeRef")
            ?? throw new InvalidOperationException("Fixture broken: DescribeRef missing.");

        var summary = (string)describeRef.Invoke(null, new object[]
        {
            new FspmPropertyRef(
                new FspmSemanticIdentity("SemanticGolden|P:SemanticGolden.Domain.User.PhoneNumber", "AB"),
                "User.PhoneNumber",
                "SemanticGolden|T:SemanticGolden.Domain.User",
                "CD"),
            FspmReferenceStatus.Stale,
        })!;

        Assert.Contains("SemanticGolden|P:SemanticGolden.Domain.User.PhoneNumber", summary);
        Assert.Contains("User.PhoneNumber", summary);
        Assert.Contains("Stale", summary);
    }

    [Fact]
    public void G14_03_ROSYNLISOLATION_Construction_Readable_Without_Roslyn()
    {
        var modelAssembly = typeof(FspmSemanticState).Assembly.Location;
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        var clientCompilation = CSharpCompilation.Create(
            "ModelClient",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(ClientSource) },
            references: new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(modelAssembly),
                MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        EmitResult emit = clientCompilation.Emit(ms);
        Assert.True(emit.Success,
            "Construction records must compile against SemanticModel with zero Roslyn references: " +
            string.Join("; ", emit.Diagnostics.Select(d => d.GetMessage())));

        var clientAssembly = Assembly.Load(ms.ToArray());
        var clientType = clientAssembly.GetType("ModelClient")
            ?? throw new InvalidOperationException("Fixture broken: ModelClient missing.");
        var describe = clientType.GetMethod("DescribeConstruction")
            ?? throw new InvalidOperationException("Fixture broken: DescribeConstruction missing.");

        var summary = (string)describe.Invoke(null, new object[]
        {
            new FspmConstruction(
                "Form:UserForm", "Form", "UserForm", "",
                System.Array.Empty<FspmConstructionNode>(),
                System.Array.Empty<FspmConstructionEdge>(),
                FspmConstructionState.Frozen, "ABCDEF"),
            new FspmConstructionValidation(
                true,
                System.Array.Empty<FspmReferenceBinding>(),
                System.Array.Empty<string>()),
        })!;

        Assert.Contains("Form:UserForm", summary);
        Assert.Contains("Frozen", summary);
        Assert.Contains("ABCDEF", summary);
        Assert.Contains("True", summary);
    }

    [Fact]
    public void SemanticModel_Assembly_Has_Zero_Roslyn_References()
    {
        var referenced = typeof(FspmSemanticState).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToArray();

        Assert.DoesNotContain(referenced, n => n != null && n.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, n => string.Equals(n, "Foundry.FSPM.Compiler", StringComparison.Ordinal));
    }
}
