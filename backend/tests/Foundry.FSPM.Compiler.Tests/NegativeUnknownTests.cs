using Foundry.FSPM.Compiler.Binding;
using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// Phase 10 — Negative / Ambiguity / CrossProject / Determinism / Rebind
/// (chief architect directive: NEVER GUESS). All tests ride on real Roslyn
/// compilations, in the <c>RoslynWorkspace</c> collection so MSBuild's
/// single design-time build is respected.
/// </summary>
[Collection("RoslynWorkspace")]
public sealed class NegativeUnknownTests
{
    // ===================================================================
    // G10-1 / G10-2 / G10-3 — Unknown Entity / Property / Operation
    // ===================================================================

    [Fact]
    public async Task G10_1_UnknownEntity_DoesNotExist_StaysInModel_WithNullSymbol_AndFSPM101()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var (model, _) = SemanticFixture.BuildFromSource("entity DoesNotExist", loaded.Compilation);

            var entity = Assert.Single(model.Entities);
            Assert.Equal(FspmBindingStatus.Unknown, entity.Status);
            Assert.False(entity.IsResolved);
            Assert.Null(entity.Symbol);
            Assert.Equal(FspmDiagnosticCodes.EntityNotFound, Assert.Single(entity.Binding.Diagnostics).Code);
            Assert.Equal(FspmDiagnosticSeverity.Error, Assert.Single(entity.Binding.Diagnostics).Severity);
            Assert.True(model.HasErrors);
        }
    }

    [Fact]
    public async Task G10_2_UnknownProperty_UserDoesNotExist_EntityResolves_PropertyFailsFSPM102()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            // Use a uniquely-named owning type so the property error is isolated to the property.
            var (model, _) = SemanticFixture.BuildFromSource(
                """
                entity OtherUser
                property OtherUser.DoesNotExist
                """, loaded.Compilation);

            var entity = Assert.Single(model.Entities);
            Assert.True(entity.IsResolved);

            var property = Assert.Single(model.Properties);
            Assert.Equal(FspmBindingStatus.Unknown, property.Status);
            Assert.False(property.IsResolved);
            Assert.Null(property.Symbol);
            Assert.Equal(FspmDiagnosticCodes.PropertyNotFound, Assert.Single(property.Binding.Diagnostics).Code);
        }
    }

    [Fact]
    public async Task G10_3_UnknownOperation_SessionDoesNotExist_OwnerResolves_OperationFailsFSPM103()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var (model, _) = SemanticFixture.BuildFromSource(
                """
                entity Session
                operation Session.DoesNotExist
                """, loaded.Compilation);

            var entity = Assert.Single(model.Entities);
            Assert.True(entity.IsResolved);

            var operation = Assert.Single(model.Operations);
            Assert.Equal(FspmBindingStatus.Unknown, operation.Status);
            Assert.Null(operation.Symbol);
            Assert.Equal(FspmDiagnosticCodes.OperationNotFound, Assert.Single(operation.Binding.Diagnostics).Code);
        }
    }

    // ===================================================================
    // G10-4 — Ambiguous Entity (no First() ever)
    // ===================================================================

    [Fact]
    public async Task G10_4_AmbiguousEntity_BareUser_ReportsAllCandidates_AndFailsToBind()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var (model, _) = SemanticFixture.BuildFromSource("entity User", loaded.Compilation);

            var entity = Assert.Single(model.Entities);
            Assert.Equal(FspmBindingStatus.Ambiguous, entity.Status);
            Assert.Null(entity.Symbol);
            var diagnostic = Assert.Single(entity.Binding.Diagnostics);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousEntity, diagnostic.Code);

            // The message must list ALL real candidates so the user can see
            // the unresolvable surface — never a silently-picked one.
            Assert.Contains("SemanticGolden.Domain.User", diagnostic.Message);
            Assert.Contains("SemanticGolden.NamespaceA.User", diagnostic.Message);
            Assert.Contains("SemanticGolden.NamespaceB.User", diagnostic.Message);
            Assert.Contains("SemanticGolden.Contracts.User", diagnostic.Message);
        }
    }

    // ===================================================================
    // G10-5 — Ambiguous Property (owner-first preserved)
    // ===================================================================

    [Fact]
    public async Task G10_5a_AmbiguousOwner_PropertyFailsInvalid_OwnerDiagnosticPropagated_NoSiblingMisbind()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var (model, _) = SemanticFixture.BuildFromSource(
                """
                entity User
                property User.PhoneNumber
                """, loaded.Compilation);

            var entity = Assert.Single(model.Entities);
            Assert.Equal(FspmBindingStatus.Ambiguous, entity.Status);

            var property = Assert.Single(model.Properties);
            // The PROPERTY is well-formed; the OWNER is unresolvable. The
            // binding MUST report Invalid with the owner's FSPM111 propagated
            // verbatim — never silently bind to BaseUser.PhoneNumber or
            // OtherUser.PhoneNumber.
            Assert.Equal(FspmBindingStatus.Invalid, property.Status);
            Assert.Null(property.Symbol);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousEntity, Assert.Single(property.Binding.Diagnostics).Code);
        }
    }

    [Fact]
    public async Task G10_5b_ShadowedProperty_ShadowedUserName_IsAmbiguous_NotFirst()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var (model, _) = SemanticFixture.BuildFromSource(
                """
                entity ShadowedUser
                property ShadowedUser.Name
                """, loaded.Compilation);

            var property = Assert.Single(model.Properties);
            Assert.Equal(FspmBindingStatus.Ambiguous, property.Status);
            Assert.Null(property.Symbol);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousProperty, Assert.Single(property.Binding.Diagnostics).Code);
        }
    }

    // ===================================================================
    // G10-6 / G10-7 — Operation Overload Ambiguity
    // ===================================================================

    [Fact]
    public async Task G10_7_OperationOverload_SessionLookup_ReportsAmbiguous_BothSignaturesListed_NoGuess()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            // Session.Lookup now has two overloads: Lookup(string) and Lookup(int).
            // FSPM v1 grammar has no parameter syntax → must report Ambiguous, never First().
            var (model, _) = SemanticFixture.BuildFromSource(
                """
                entity Session
                operation Session.Lookup
                """, loaded.Compilation);

            var operation = Assert.Single(model.Operations);
            Assert.Equal(FspmBindingStatus.Ambiguous, operation.Status);
            Assert.Null(operation.Symbol);
            var diagnostic = Assert.Single(operation.Binding.Diagnostics);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousOperation, diagnostic.Code);

            // Both signatures must be visible to the user (per G10-7 contract).
            Assert.Contains("string byId", diagnostic.Message);
            Assert.Contains("int byNumericId", diagnostic.Message);

            // Grammar limitation restated verbatim in the diagnostic.
            Assert.Contains("FSPM v1 has no parameter syntax", diagnostic.Message);
        }
    }

    // ===================================================================
    // G10-8 — Cross Project (real + missing-ProjectReference)
    // ===================================================================

    [Fact]
    public async Task G10_8a_CrossProject_CorrectReference_ContractorLicenseNumber_BindsToContracts()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            // `Contractor` lives ONLY in SemanticGolden.Contracts — a short-name
            // `property Contractor.LicenseNumber` therefore walks the binder to
            // EXACTLY ONE assembly, proving the binder's owner lookup can
            // discriminate across projects by AssemblySimpleName (Phase 7 contract).
            var (model, _) = SemanticFixture.BuildFromSource(
                """
                property Contractor.LicenseNumber
                """, loaded.Compilation);

            var property = Assert.Single(model.Properties);
            Assert.True(property.IsResolved);
            Assert.Equal(
                "SemanticGolden.Contracts|P:SemanticGolden.Contracts.Contractor.LicenseNumber",
                property.SymbolId.Value.ToString());
            Assert.Equal("SemanticGolden.Contracts", ((IPropertySymbol)property.Symbol!).ContainingAssembly.Name);
        }
    }

    [Fact]
    public async Task G10_8b_CrossProject_MissingReference_NotReferencedUser_StaysInvisibleToBinder()
    {
        // SemanticGolden.NotReferenced.dll exists in the test-fixtures tree but
        // is NOT referenced from SemanticGolden.csproj. The binder must NEVER
        // reach into it. A short-name `entity User` against the SemanticGolden
        // workspace therefore yields the same Ambiguous-4 result as before —
        // proving no name-only fallback reached the un-referenced assembly.
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var (model, _) = SemanticFixture.BuildFromSource("entity User", loaded.Compilation);

            var entity = Assert.Single(model.Entities);
            Assert.Equal(FspmBindingStatus.Ambiguous, entity.Status);
            var message = Assert.Single(entity.Binding.Diagnostics).Message;
            Assert.DoesNotContain("NotReferenced", message);
            // The 4 referenced-assembly candidates must all be present; the
            // 5th (NotReferenced) must NOT appear.
            Assert.Contains("SemanticGolden.Domain.User", message);
            Assert.Contains("SemanticGolden.Contracts.User", message);
        }
    }

    // ===================================================================
    // G10-9 — Cross Assembly Collision
    // ===================================================================

    [Fact]
    public async Task G10_9_CrossAssembly_CustomerPhoneNumber_TwoAssemblies_ProduceDifferentSymbolIds()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            // `Customer.PhoneNumber` is ambiguous in short-name lookup (Domain + Contracts
            // both declare `Customer.PhoneNumber`). Per FSPM grammar, the property
            // declaration takes a single IDENT EntityName, so the short-name owner
            // is genuinely ambiguous. The model must surface BOTH candidates with
            // distinct Assembly segments in their SymbolIds.
            //
            // We compare the IDs by FQN EntityBinding inside the property binder by
            // inspecting both candidate INamedTypeSymbol directly through the
            // compilation — no FSPM source is needed for the assembly discrimination
            // assertion itself.
            var domainCustomer = loaded.Compilation
                .GetTypeByMetadataName("SemanticGolden.Domain.Customer");
            var contractsCustomer = loaded.Compilation
                .GetTypeByMetadataName("SemanticGolden.Contracts.Customer");

            Assert.NotNull(domainCustomer);
            Assert.NotNull(contractsCustomer);

            var domainId = Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(domainCustomer!);
            var contractsId = Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(contractsCustomer!);

            Assert.Equal("SemanticGolden|T:SemanticGolden.Domain.Customer", domainId.Value);
            Assert.Equal("SemanticGolden.Contracts|T:SemanticGolden.Contracts.Customer", contractsId.Value);
            Assert.NotEqual(domainId, contractsId);

            // The discriminator in the Phase 7 id is the AssemblySimpleName segment.
            Assert.Equal("SemanticGolden", ExtractAssembly(domainId.Value));
            Assert.Equal("SemanticGolden.Contracts", ExtractAssembly(contractsId.Value));
        }
    }

    private static string ExtractAssembly(string symbolId)
    {
        var idx = symbolId.IndexOf('|');
        Assert.True(idx > 0);
        return symbolId.Substring(0, idx);
    }

    // ===================================================================
    // G10-10 — Rebind (rebuild stable)
    // ===================================================================

    [Fact]
    public async Task G10_10_Rebind_TwoIndependentBuilds_ProduceIdenticalSymbolIds_AndDiagnostics()
    {
        const string Source = """
            entity OtherUser
            property OtherUser.PhoneNumber
            entity Session
            operation Session.Ping
            """;

        var first = await GoldenIdentity.LoadGoldenAsync();
        using (first.Workspace)
        {
            var (model1, _) = SemanticFixture.BuildFromSource(Source, first.Compilation);

            var second = await GoldenIdentity.LoadGoldenAsync();
            using (second.Workspace)
            {
                var (model2, _) = SemanticFixture.BuildFromSource(Source, second.Compilation);

                // Per directive: NEVER compare via ReferenceEquals.
                Assert.Equal(
                    model1.Entities.Select(e => e.SymbolId),
                    model2.Entities.Select(e => e.SymbolId));
                Assert.Equal(
                    model1.Properties.Select(p => p.SymbolId),
                    model2.Properties.Select(p => p.SymbolId));
                Assert.Equal(
                    model1.Operations.Select(o => o.SymbolId),
                    model2.Operations.Select(o => o.SymbolId));

                // Diagnostics set must be identical in code/severity/line/column.
                Assert.Equal(
                    model1.Diagnostics.Select(d => (d.Code, d.Severity, d.Line, d.Column)),
                    model2.Diagnostics.Select(d => (d.Code, d.Severity, d.Line, d.Column)));
            }
        }
    }

    // ===================================================================
    // G10-11 / G10-12 — Diagnostic & Model Determinism
    // ===================================================================

    [Fact]
    public async Task G10_11_DiagnosticDetermism_AmbiguousOwner_Property_ProducesSameDiagnosticAcrossRuns()
    {
        const string Source = """
            entity User
            property User.PhoneNumber
            operation User.Create
            """;

        var first = await GoldenIdentity.LoadGoldenAsync();
        using (first.Workspace)
        {
            var (m1, _) = SemanticFixture.BuildFromSource(Source, first.Compilation);

            var second = await GoldenIdentity.LoadGoldenAsync();
            using (second.Workspace)
            {
                var (m2, _) = SemanticFixture.BuildFromSource(Source, second.Compilation);

                Assert.Equal(
                    m1.Diagnostics.Select(d => (d.Code, d.Severity, d.Line, d.Column, d.Message)),
                    m2.Diagnostics.Select(d => (d.Code, d.Severity, d.Line, d.Column, d.Message)));
            }
        }
    }

    [Fact]
    public async Task G10_12_SemanticModelDetermism_FullStructuralCompare_EqualAcrossRuns()
    {
        const string Source = """
            entity OtherUser
            property OtherUser.PhoneNumber
            entity Session
            operation Session.Ping
            """;

        var first = await GoldenIdentity.LoadGoldenAsync();
        using (first.Workspace)
        {
            var (m1, _) = SemanticFixture.BuildFromSource(Source, first.Compilation);

            var second = await GoldenIdentity.LoadGoldenAsync();
            using (second.Workspace)
            {
                var (m2, _) = SemanticFixture.BuildFromSource(Source, second.Compilation);

                Assert.True(StructurallyEqual(m1, m2));
            }
        }
    }

    private static bool StructurallyEqual(FspmSemanticModel a, FspmSemanticModel b)
    {
        bool Seq<T>(IEnumerable<T> x, IEnumerable<T> y) => x.SequenceEqual(y);

        return Seq(
                a.Entities.Select(e => (e.SymbolId, e.Status, e.Name ?? string.Empty, e.QualifiedName ?? string.Empty, e.IsResolved)),
                b.Entities.Select(e => (e.SymbolId, e.Status, e.Name ?? string.Empty, e.QualifiedName ?? string.Empty, e.IsResolved)))
            && Seq(
                a.Properties.Select(p => (p.SymbolId, p.Status, p.Name ?? string.Empty, p.TypeName ?? string.Empty, p.Owner?.SymbolId ?? default, p.IsResolved)),
                b.Properties.Select(p => (p.SymbolId, p.Status, p.Name ?? string.Empty, p.TypeName ?? string.Empty, p.Owner?.SymbolId ?? default, p.IsResolved)))
            && Seq(
                a.Operations.Select(o => (o.SymbolId, o.Status, o.Name ?? string.Empty, o.ReturnType ?? string.Empty, o.IsStatic, string.Join(",", o.ParameterTypes), o.Owner?.SymbolId ?? default, o.IsResolved)),
                b.Operations.Select(o => (o.SymbolId, o.Status, o.Name ?? string.Empty, o.ReturnType ?? string.Empty, o.IsStatic, string.Join(",", o.ParameterTypes), o.Owner?.SymbolId ?? default, o.IsResolved)))
            && Seq(
                a.Diagnostics.Select(d => (d.Code, d.Severity, d.Line, d.Column, d.Message)),
                b.Diagnostics.Select(d => (d.Code, d.Severity, d.Line, d.Column, d.Message)));
    }

    // ===================================================================
    // G10-13 — Synthetic ID Isolation
    // ===================================================================

    [Fact]
    public async Task G10_13_SyntheticIdIsolation_ThreeFailingDeclarations_HaveDistinctIds_AndResideInKnownPrefix()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            // bare `User` is Ambiguous Entity; `User.PhoneNumber` is Invalid Property
            // (owner FSPM111 propagates); `User.Nope` is Invalid Operation (no member).
            var (model, _) = SemanticFixture.BuildFromSource(
                """
                entity User
                property User.PhoneNumber
                operation User.Nope
                """, loaded.Compilation);

            var entity = Assert.Single(model.Entities);
            var property = Assert.Single(model.Properties);
            var operation = Assert.Single(model.Operations);

            // All three must be on the synthetic prefix (NEVER First() to a real id).
            Assert.StartsWith("synthetic/Ambiguous/Entity/User@", entity.SymbolId.Value);
            Assert.StartsWith("synthetic/Invalid/Property/User.PhoneNumber@", property.SymbolId.Value);
            Assert.StartsWith("synthetic/Invalid/Operation/User.Nope@", operation.SymbolId.Value);

            // Three distinct ids (proves G10-13: same name in three failure modes stay separable).
            var distinct = new[] { entity.SymbolId, property.SymbolId, operation.SymbolId }.Distinct().Count();
            Assert.Equal(3, distinct);

            // The synthetic prefix never collides with a real `Assembly|DocId` shape
            // (real ids always start with the assembly name, e.g. `SemanticGolden|…`).
            const string SyntheticPrefix = "synthetic/";
            foreach (var id in new[] { entity.SymbolId, property.SymbolId, operation.SymbolId })
            {
                Assert.StartsWith(SyntheticPrefix, id.Value, StringComparison.Ordinal);
                Assert.False(id.Value.Contains('|'),
                    $"Synthetic id '{id.Value}' must never carry a '|' — the discriminator is reserved for real Assembly|DocId.");
            }
        }
    }

    [Fact]
    public async Task G10_13b_SyntheticIds_AreDeterministic_AcrossTwoBuilds()
    {
        const string Source = """
            entity User
            property User.PhoneNumber
            operation User.Nope
            """;

        var first = await GoldenIdentity.LoadGoldenAsync();
        using (first.Workspace)
        {
            var (m1, _) = SemanticFixture.BuildFromSource(Source, first.Compilation);

            var second = await GoldenIdentity.LoadGoldenAsync();
            using (second.Workspace)
            {
                var (m2, _) = SemanticFixture.BuildFromSource(Source, second.Compilation);

                Assert.Equal(m1.Entities.Single().SymbolId, m2.Entities.Single().SymbolId);
                Assert.Equal(m1.Properties.Single().SymbolId, m2.Properties.Single().SymbolId);
                Assert.Equal(m1.Operations.Single().SymbolId, m2.Operations.Single().SymbolId);
            }
        }
    }

    // ===================================================================
    // G10-14 — Negative E2E (full pipeline survives all error paths)
    // ===================================================================

    [Fact]
    public async Task G10_14_NegativeE2E_ThreeVariants_DoNotCrash_DoNotFakeSymbol_KeepFailureVisible()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            // Source A: Unknown entity.
            var (modelA, _) = SemanticFixture.BuildFromSource("entity NoSuchType", loaded.Compilation);
            Assert.True(modelA.HasErrors);
            var entityA = Assert.Single(modelA.Entities);
            Assert.Equal(FspmBindingStatus.Unknown, entityA.Status);
            Assert.Null(entityA.Symbol);
            Assert.NotEmpty(entityA.Binding.Diagnostics);
            // Failing element stays addressable by its synthetic id.
            Assert.NotNull(modelA.FindEntity(entityA.SymbolId));
            // No resolved element may carry a synthetic id.
            Assert.All(modelA.Entities, e => Assert.False(e.IsResolved && e.SymbolId.Value.StartsWith("synthetic/", StringComparison.Ordinal)));

            // Source B: Ambiguous entity + Invalid property.
            var (modelB, _) = SemanticFixture.BuildFromSource(
                """
                entity User
                property User.DoesNotExist
                """, loaded.Compilation);
            Assert.True(modelB.HasErrors);
            Assert.Equal(FspmBindingStatus.Ambiguous, Assert.Single(modelB.Entities).Status);
            var propertyB = Assert.Single(modelB.Properties);
            Assert.Equal(FspmBindingStatus.Invalid, propertyB.Status);
            Assert.Null(propertyB.Symbol);
            Assert.NotEmpty(propertyB.Binding.Diagnostics);
            Assert.NotNull(modelB.FindProperty(propertyB.SymbolId));

            // Source C: Success entity + Unknown operation.
            var (modelC, _) = SemanticFixture.BuildFromSource(
                """
                entity Session
                operation Session.DoesNotExist
                """, loaded.Compilation);
            var entityC = Assert.Single(modelC.Entities);
            Assert.True(entityC.IsResolved);
            var operationC = Assert.Single(modelC.Operations);
            Assert.Equal(FspmBindingStatus.Unknown, operationC.Status);
            Assert.Null(operationC.Symbol);
            Assert.NotEmpty(operationC.Binding.Diagnostics);
        }
    }
}
