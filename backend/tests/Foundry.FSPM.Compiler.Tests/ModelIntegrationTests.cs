using Foundry.FSPM.Compiler.Compiler;
using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-01 STEP 6-L: full-project integration (package §17). Builds Facts
// for every indexed record of the real SemanticGolden compilation,
// assembles one model, and asserts the whole structure at once — not a
// single API in isolation.
[Collection("RoslynWorkspace")]
public sealed class ModelIntegrationTests
{
    [Fact]
    public async Task GoldenProject_Assembles_Complete_Model()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
                compiled.Snapshot.Compilation,
                compiled.Snapshot.ProjectName,
                compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
                "model-integration");

            var facts = new List<NativeSemanticFact>();
            foreach (var record in compiled.Index.Records)
            {
                var anchor = new SemanticSourceAnchor(
                    SemanticIdentityMint.MintLogicalIdentity(record.Symbol),
                    record.Symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "<metadata>",
                    DocumentationCommentId.CreateDeclarationId(record.Symbol) ?? "<none>",
                    FspmSourceLocation.From(record.Symbol.Locations.FirstOrDefault() ?? Location.None));
                facts.Add(NativeSemanticFactFactory.Create(
                    record.Symbol, compilationIdentity, anchor));
            }

            var metadata = new Foundry.FSPM.SemanticModel.FspmSemanticModelMetadata(
                SnapshotId: "model-integration",
                SourceAssembly: "SemanticGolden",
                FactCount: facts.Count);

            var (model, notes) = FspmModelBinder.Assemble(facts, metadata);

            // Types.
            Assert.Contains(model.Types, t => t.Name == "User");
            Assert.Contains(model.Types, t => t.Name == "OpService");
            Assert.Contains(model.Types, t => t.GenericArity == 1);

            // Members: User.PhoneNumber present with owner linkage.
            var phone = model.Members.First(m =>
                m.Name == "PhoneNumber" && m.DeclaringTypeId.Contains("Domain.User"));
            Assert.Equal("Property", phone.MemberKind);
            Assert.Equal("string", phone.Type);

            // Operations: Create overloads stay distinct.
            Assert.True(model.Operations.Count(o => o.Name == "Create") >= 2);

            // Relations: AdminUser inheritance present.
            Assert.Contains(model.Relations, r =>
                r.Kind == "Inheritance" && r.Target.Contains("BaseAccount"));

            // Identity pairs unique across the whole model.
            var pairs = model.Types.Select(t => t.Identity.LogicalId + "|" + t.Identity.Fingerprint)
                .Concat(model.Members.Select(m => m.Identity.LogicalId + "|" + m.Identity.Fingerprint))
                .Concat(model.Operations.Select(o => o.Identity.LogicalId + "|" + o.Identity.Fingerprint))
                .ToArray();
            Assert.Equal(pairs.Length, pairs.Distinct().Count());

            // Parameters flattened from operations.
            Assert.True(model.Parameters.Count > 0);
            Assert.All(model.Parameters, p => Assert.False(string.IsNullOrEmpty(p.OwnerId)));

            // Metadata echoes the build.
            Assert.Equal("model-integration", model.Metadata.SnapshotId);
            Assert.Equal(facts.Count, model.Metadata.FactCount);

            // Closure: every indexed record bound, nothing unexplained.
            Assert.Empty(notes);
        }
    }
}
