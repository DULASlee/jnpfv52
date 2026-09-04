using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Semantic.Construction;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-03: Semantic Construction over P14-02 references. Every failure
// asserts Status/validity/Reason (+Owner/Target/Kind/Identity where
// applicable) — never a bare boolean.
[Collection("RoslynWorkspace")]
public sealed class ConstructionTests
{
    private static Foundry.FSPM.SemanticModel.FspmSemanticModel BuildModel(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled)
    {
        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compiled.Snapshot.Compilation,
            compiled.Snapshot.ProjectName,
            compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
            "construction");
        var facts = compiled.Index.Records.Select(record =>
        {
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(record.Symbol),
                record.Symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "<metadata>",
                DocumentationCommentId.CreateDeclarationId(record.Symbol) ?? "<none>",
                FspmSourceLocation.From(record.Symbol.Locations.FirstOrDefault() ?? Location.None));
            return NativeSemanticFactFactory.Create(record.Symbol, compilationIdentity, anchor);
        }).ToArray();
        var metadata = new FspmSemanticModelMetadata("construction", "SemanticGolden", facts.Length);
        return FspmModelBinder.Assemble(facts, metadata).Model;
    }

    private static FspmConstruction BuildUserForm(Foundry.FSPM.SemanticModel.FspmSemanticModel model)
    {
        var user = model.Types.First(t => t.Name == "User");
        var name = model.Members.First(m => m.Name == "UserName");
        var phone = model.Members.First(m => m.Name == "PhoneNumber");
        var create = model.Operations.First(o => o.Name == "Create");

        var form = FspmConstructionBuilder.Create("Form", "UserForm");
        form = FspmConstructionBuilder.Attach(form, form.Id, "EntityBinding", "User",
            "entity", new FspmEntityRef(user.Identity, "User"));
        form = FspmConstructionBuilder.Attach(form, form.Id, "FieldBinding", "Name",
            "field", new FspmPropertyRef(name.Identity, "User.UserName", name.DeclaringTypeId));
        form = FspmConstructionBuilder.Attach(form, form.Id, "FieldBinding", "Phone",
            "field", new FspmPropertyRef(phone.Identity, "User.PhoneNumber", phone.DeclaringTypeId));
        form = FspmConstructionBuilder.Attach(form, form.Id, "SubmitBinding", "Create",
            "submit", new FspmOperationRef(create.Identity, "Create", create.DeclaringTypeId));
        return form;
    }

    [Fact]
    public async Task Golden_UserForm_Validates_And_Freezes()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var form = BuildUserForm(model);

            var validation = FspmConstructionBuilder.Validate(form, model);
            Assert.True(validation.IsValid);
            Assert.Empty(validation.Issues);
            Assert.All(validation.Bindings, b => Assert.False(string.IsNullOrWhiteSpace(b.Reason)));

            var frozen = FspmConstructionBuilder.Freeze(form, validation);
            Assert.Equal(FspmConstructionState.Frozen, frozen.State);
            Assert.False(string.IsNullOrEmpty(frozen.Fingerprint));
            Assert.All(frozen.Nodes, n => Assert.Equal(FspmConstructionState.Frozen, n.State));
        }
    }

    [Fact]
    public async Task EntityRef_With_OwnerId_Fails_Validation()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var user = model.Types.First(t => t.Name == "User");

            var form = FspmConstructionBuilder.Create("Form", "UserForm");
            form = FspmConstructionBuilder.Attach(form, form.Id, "EntityBinding", "User",
                "entity", new FspmEntityRef(user.Identity, "User", OwnerId: "Something"));

            var validation = FspmConstructionBuilder.Validate(form, model);

            Assert.False(validation.IsValid);
            Assert.Contains(validation.Issues, i => i.Contains("OwnerId"));
        }
    }

    [Fact]
    public async Task PropertyRef_With_Wrong_Owner_Fails_With_Owner_Evidence()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var phone = model.Members.First(m => m.Name == "PhoneNumber");
            var other = model.Types.First(t => t.Name == "OtherUser");

            var form = FspmConstructionBuilder.Create("Form", "UserForm");
            form = FspmConstructionBuilder.Attach(form, form.Id, "FieldBinding", "Phone",
                "field", new FspmPropertyRef(phone.Identity, "PhoneNumber", other.Identity.LogicalId));

            var validation = FspmConstructionBuilder.Validate(form, model);

            Assert.False(validation.IsValid);
            var binding = Assert.Single(validation.Bindings);
            Assert.False(binding.IsValid);
            // Frozen P14-02 contract: WrongOwner carries no TargetIdentity,
            // but Owner always names the actual declaring type.
            Assert.Equal(string.Empty, binding.TargetIdentity);
            Assert.Equal(phone.DeclaringTypeId, binding.Owner);
            Assert.Contains(phone.DeclaringTypeId, binding.Owner);
            Assert.Contains(phone.DeclaringTypeId, binding.Reason);
        }
    }

    [Fact]
    public async Task OperationRef_Kind_Mismatch_Fails_With_Kinds()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var phone = model.Members.First(m => m.Name == "PhoneNumber");

            var form = FspmConstructionBuilder.Create("Form", "UserForm");
            form = FspmConstructionBuilder.Attach(form, form.Id, "SubmitBinding", "Create",
                "submit", new FspmOperationRef(phone.Identity, "PhoneNumber"));

            var validation = FspmConstructionBuilder.Validate(form, model);

            Assert.False(validation.IsValid);
            var binding = Assert.Single(validation.Bindings);
            Assert.Contains("exists as Property", binding.Reason);
            Assert.Contains("not as Operation", binding.Reason);
        }
    }

    [Fact]
    public async Task Missing_Target_Fails_With_Reason()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var bogus = new FspmSemanticIdentity("Nope|T:Nope", "00");

            var form = FspmConstructionBuilder.Create("Form", "UserForm");
            form = FspmConstructionBuilder.Attach(form, form.Id, "EntityBinding", "Nope",
                "entity", new FspmEntityRef(bogus, "Nope"));

            var validation = FspmConstructionBuilder.Validate(form, model);

            Assert.False(validation.IsValid);
            Assert.False(Assert.Single(validation.Bindings).IsValid);
        }
    }

    [Fact]
    public async Task Stale_Target_Fails_With_Both_Fingerprints()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var phone = model.Members.First(m => m.Name == "PhoneNumber");

            var form = FspmConstructionBuilder.Create("Form", "UserForm");
            form = FspmConstructionBuilder.Attach(form, form.Id, "FieldBinding", "Phone",
                "field", new FspmPropertyRef(
                    phone.Identity, "User.PhoneNumber", phone.DeclaringTypeId, "DEADBEEF"));

            var validation = FspmConstructionBuilder.Validate(form, model);

            Assert.False(validation.IsValid);
            var binding = Assert.Single(validation.Bindings);
            Assert.Contains("DEADBEEF", binding.Reason);
            Assert.Contains(phone.Fingerprint, binding.Reason);
        }
    }

    [Fact]
    public async Task Freeze_Invalid_Throws_And_Frozen_Rejects_Attach()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var bogus = new FspmSemanticIdentity("Nope|T:Nope", "00");
            var form = FspmConstructionBuilder.Create("Form", "UserForm");
            form = FspmConstructionBuilder.Attach(form, form.Id, "EntityBinding", "Nope",
                "entity", new FspmEntityRef(bogus, "Nope"));

            var validation = FspmConstructionBuilder.Validate(form, model);
            Assert.False(validation.IsValid);
            Assert.Throws<System.InvalidOperationException>(
                () => FspmConstructionBuilder.Freeze(form, validation));

            var good = BuildUserForm(model);
            var frozen = FspmConstructionBuilder.Freeze(
                good, FspmConstructionBuilder.Validate(good, model));
            var user = model.Types.First(t => t.Name == "User");
            Assert.Throws<System.InvalidOperationException>(
                () => FspmConstructionBuilder.Attach(frozen, frozen.Id, "FieldBinding", "X",
                    "field", new FspmPropertyRef(user.Identity, "X")));
        }
    }

    [Fact]
    public async Task Determinism_Swapped_Attach_Order_Same_Fingerprint()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var user = model.Types.First(t => t.Name == "User");
            var name = model.Members.First(m => m.Name == "UserName");
            var phone = model.Members.First(m => m.Name == "PhoneNumber");

            FspmConstruction Build(bool swapped)
            {
                var form = FspmConstructionBuilder.Create("Form", "UserForm");
                form = FspmConstructionBuilder.Attach(form, form.Id, "EntityBinding", "User",
                    "entity", new FspmEntityRef(user.Identity, "User"));
                var first = swapped ? phone : name;
                var second = swapped ? name : phone;
                var firstKind = "FieldBinding";
                form = FspmConstructionBuilder.Attach(form, form.Id, firstKind, first.Name,
                    "field", new FspmPropertyRef(first.Identity, first.Name, first.DeclaringTypeId));
                form = FspmConstructionBuilder.Attach(form, form.Id, firstKind, second.Name,
                    "field", new FspmPropertyRef(second.Identity, second.Name, second.DeclaringTypeId));
                return form;
            }

            FspmConstruction FreezeOf(FspmConstruction form) =>
                FspmConstructionBuilder.Freeze(form, FspmConstructionBuilder.Validate(form, model));

            var a = FreezeOf(Build(swapped: false));
            var b = FreezeOf(Build(swapped: true));

            Assert.Equal(a.Fingerprint, b.Fingerprint);
            Assert.Equal(
                a.Edges.Select(e => e.TargetIdentity).OrderBy(x => x),
                b.Edges.Select(e => e.TargetIdentity).OrderBy(x => x));
        }
    }
}
