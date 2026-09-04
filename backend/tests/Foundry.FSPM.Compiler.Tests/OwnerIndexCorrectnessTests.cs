using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.SemanticModel;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// G14-01-POST-OWNER-01 — P14-01 Post-Freeze Correctness Sweep.
//
// The pre-sweep BuildOwnerIndex used group.First() and silently
// collapsed a batch with two Type facts sharing the same owner key
// into a single arbitrary winner. These tests pin the 0/1/>1
// contract as an executable invariant and assert that the binder
// never auto-picks a candidate:
//
//   0 Type facts for a member's owner key
//     -> DeclaringTypeId == ""  + note + member.State == NotFound
//
//   1 Type fact for the same key
//     -> DeclaringTypeId == that Type fact's identity + no note
//
//   2 (and 3+) Type facts for the same key
//     -> DeclaringTypeId == ""  + note listing every candidate
//     -> member.State forced to Ambiguous (not Invalid, not the
//        first candidate's State, never a fabricated identity)
//
// Plus the auxiliary invariants:
//
//   same Assembly + Namespace + MetadataName, different Type facts
//     -> exactly one entry in the owner index with CandidateCount = Many
//
//   different Assembly, same Namespace + MetadataName
//     -> two distinct owner-index entries (keys differ on Assembly)
//
//   TypeFact.ContainingTypeName != ""
//     -> rejected; surfaced as a note; not used in the owner index
//
//   TypeFact.Name != TypeFact.Logical.MemberName
//     -> rejected; surfaced as a note; not used in the owner index
//
// All facts in this file are hand-built: no Roslyn workspace, no
// SymbolKind ambiguity. The Roslyn-backed test fixture path
// (ModelMemberTests, ModelRelationTests, ModelContainmentTests)
// still exercises the same binder end-to-end.
public sealed class OwnerIndexCorrectnessTests
{
    private static NativeSemanticFact MakeTypeFact(
        string assemblyName,
        string @namespace,
        string metadataName,
        string identityValue,
        string containingTypeName = "",
        string? overrideName = null)
    {
        var identity = new FspmSymbolId(identityValue);
        var logical = new LogicalSemanticIdentity(
            AssemblyName: assemblyName,
            Namespace: @namespace,
            ContainingTypeName: containingTypeName,
            MemberName: metadataName,
            MemberKind: "NamedType");
        var anchor = new SemanticSourceAnchor(
            Logical: logical,
            DocumentIdentity: "<test>",
            DeclarationAnchor: "T:test",
            CurrentSpan: new FspmSourceLocation("<test>", 1, 1, 1, 1));
        var compilation = new CompilationIdentity(
            ProjectName: "TestProject",
            AssemblyName: assemblyName,
            ReferenceDisplayNames: System.Array.Empty<string>(),
            OptimizationLevel: "Release",
            LanguageVersion: "C# 12",
            DocumentPaths: new[] { "<test>" },
            SnapshotId: "sweep");
        var assembly = new AssemblyIdentity(
            Name: assemblyName, Version: "0.0.0.0",
            Culture: "", PublicKeyToken: "", Source: "SourceProject");

        return new NativeSemanticFact(
            Identity: identity,
            Logical: logical,
            Fingerprint: new SemanticFingerprint("fp-" + identityValue),
            Kind: NativeSymbolKind.Type,
            TypeKind: NativeTypeKind.Class,
            Name: overrideName ?? metadataName,
            QualifiedName: string.IsNullOrEmpty(@namespace)
                ? metadataName
                : @namespace + "." + metadataName,
            Visibility: new NativeVisibilityFacts(
                Accessibility: "Public",
                IsStatic: false,
                IsAbstract: false,
                IsVirtual: false,
                IsOverride: false,
                IsSealed: false,
                IsReadOnly: false,
                IsConst: false,
                IsAsync: false,
                IsExtensionMethod: false),
            TypeShape: new NativeTypeShape(
                Kind: NativeTypeShapeKind.NamedType,
                OriginalDefinition: identityValue,
                TypeArguments: System.Array.Empty<string>(),
                ContainingType: null,
                BaseType: null,
                ArrayRank: 0,
                ElementType: null,
                TupleElementNames: System.Array.Empty<string>(),
                NullableAnnotation: "None",
                Arity: 0),
            Operation: null,
            Relationships: new NativeTypeRelationships(
                BaseType: null,
                Interfaces: System.Array.Empty<string>(),
                OverriddenMethod: null,
                OverriddenProperty: null,
                ExplicitInterfaceImplementations: System.Array.Empty<string>()),
            Compilation: compilation,
            Assembly: assembly,
            Anchor: anchor,
            Status: FspmResolutionStatus.Resolved,
            Quality: SemanticQuality.Perfect,
            Diagnostics: System.Array.Empty<NativeDiagnostic>());
    }

    private static NativeSemanticFact MakeMemberFact(
        string assemblyName,
        string @namespace,
        string containingTypeMetadataName,
        string memberName,
        string memberKind,
        string identityValue)
    {
        var identity = new FspmSymbolId(identityValue);
        var logical = new LogicalSemanticIdentity(
            AssemblyName: assemblyName,
            Namespace: @namespace,
            ContainingTypeName: containingTypeMetadataName,
            MemberName: memberName,
            MemberKind: memberKind);
        var anchor = new SemanticSourceAnchor(
            Logical: logical,
            DocumentIdentity: "<test>",
            DeclarationAnchor: "M:test",
            CurrentSpan: new FspmSourceLocation("<test>", 1, 1, 1, 1));
        var compilation = new CompilationIdentity(
            ProjectName: "TestProject",
            AssemblyName: assemblyName,
            ReferenceDisplayNames: System.Array.Empty<string>(),
            OptimizationLevel: "Release",
            LanguageVersion: "C# 12",
            DocumentPaths: new[] { "<test>" },
            SnapshotId: "sweep");
        var assembly = new AssemblyIdentity(
            Name: assemblyName, Version: "0.0.0.0",
            Culture: "", PublicKeyToken: "", Source: "SourceProject");

        var kind = memberKind switch
        {
            "Property" => NativeSymbolKind.Property,
            "Field" => NativeSymbolKind.Field,
            "Event" => NativeSymbolKind.Event,
            "Method" => NativeSymbolKind.Method,
            "Constructor" => NativeSymbolKind.Constructor,
            _ => NativeSymbolKind.Unknown,
        };

        return new NativeSemanticFact(
            Identity: identity,
            Logical: logical,
            Fingerprint: new SemanticFingerprint("fp-" + identityValue),
            Kind: kind,
            TypeKind: NativeTypeKind.Unknown,
            Name: memberName,
            QualifiedName: string.IsNullOrEmpty(@namespace)
                ? containingTypeMetadataName + "." + memberName
                : @namespace + "." + containingTypeMetadataName + "." + memberName,
            Visibility: new NativeVisibilityFacts(
                Accessibility: "Public",
                IsStatic: false,
                IsAbstract: false,
                IsVirtual: false,
                IsOverride: false,
                IsSealed: false,
                IsReadOnly: false,
                IsConst: false,
                IsAsync: false,
                IsExtensionMethod: false),
            TypeShape: new NativeTypeShape(
                Kind: NativeTypeShapeKind.NamedType,
                OriginalDefinition: "string",
                TypeArguments: System.Array.Empty<string>(),
                ContainingType: null,
                BaseType: null,
                ArrayRank: 0,
                ElementType: null,
                TupleElementNames: System.Array.Empty<string>(),
                NullableAnnotation: "None",
                Arity: 0),
            Operation: null,
            Relationships: null,
            Compilation: compilation,
            Assembly: assembly,
            Anchor: anchor,
            Status: FspmResolutionStatus.Resolved,
            Quality: SemanticQuality.Perfect,
            Diagnostics: System.Array.Empty<NativeDiagnostic>());
    }

    // -------------------------------------------------------------
    // 0 / 1 / >1 candidate matrix
    // -------------------------------------------------------------

    [Fact]
    public void Zero_Candidates_Owner_Missing_Leaves_DeclaringTypeId_Empty_And_State_NotFound()
    {
        var phone = MakeMemberFact(
            assemblyName: "A",
            @namespace: "A.N",
            containingTypeMetadataName: "User",
            memberName: "Phone",
            memberKind: "Property",
            identityValue: "A|P:A.N.User.Phone");

        var (members, notes) = FspmModelBinder.BindMembers(new[] { phone });

        var member = Assert.Single(members);
        Assert.Equal(string.Empty, member.DeclaringTypeId);
        Assert.Equal(FspmSemanticState.NotFound, member.State);
        Assert.Single(notes);
        Assert.Contains("Owner Type fact absent", notes[0]);
        Assert.Contains(phone.Name, notes[0]);
    }

    [Fact]
    public void One_Candidate_Owner_Resolved_DeclaringTypeId_Matches_And_State_Preserved()
    {
        var user = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User");
        var phone = MakeMemberFact(
            assemblyName: "A", @namespace: "A.N",
            containingTypeMetadataName: "User",
            memberName: "Phone", memberKind: "Property",
            identityValue: "A|P:A.N.User.Phone");

        var (members, notes) = FspmModelBinder.BindMembers(new[] { user, phone });

        var member = Assert.Single(members);
        Assert.Equal("A|T:A.N.User", member.DeclaringTypeId);
        Assert.Equal(FspmSemanticState.Resolved, member.State);
        Assert.Empty(notes);
    }

    [Fact]
    public void Two_Candidates_Owner_Ambiguous_Leaves_DeclaringTypeId_Empty_Forces_Ambiguous_State_And_Lists_Candidates()
    {
        // Two Type facts with the same Assembly + Namespace + MetadataName
        // but distinct identities (different DocId). Pre-sweep, the second
        // would have been silently dropped. Post-sweep, the binder must
        // report Ambiguous and surface every candidate.
        var user1 = MakeTypeFact(
            "A", "A.N", "User", "A|T:A.N.User#file1",
            overrideName: "User");
        var user2 = MakeTypeFact(
            "A", "A.N", "User", "A|T:A.N.User#file2",
            overrideName: "User");
        var phone = MakeMemberFact(
            assemblyName: "A", @namespace: "A.N",
            containingTypeMetadataName: "User",
            memberName: "Phone", memberKind: "Property",
            identityValue: "A|P:A.N.User.Phone");

        var (members, notes) = FspmModelBinder.BindMembers(new[] { user1, user2, phone });

        var member = Assert.Single(members);
        Assert.Equal(string.Empty, member.DeclaringTypeId);
        Assert.Equal(FspmSemanticState.Ambiguous, member.State);

        // One note must mention BOTH candidate identities so the
        // operator can disambiguate, plus the member name.
        var note = Assert.Single(notes);
        Assert.Contains("Ambiguous", note);
        Assert.Contains("A|T:A.N.User#file1", note);
        Assert.Contains("A|T:A.N.User#file2", note);
        Assert.Contains(phone.Name, note);
    }

    [Fact]
    public void Three_Plus_Candidates_Owner_Ambiguous_All_Identities_Listed()
    {
        var userA = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#a");
        var userB = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#b");
        var userC = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#c");
        var phone = MakeMemberFact(
            assemblyName: "A", @namespace: "A.N",
            containingTypeMetadataName: "User",
            memberName: "Phone", memberKind: "Property",
            identityValue: "A|P:A.N.User.Phone");

        var (members, notes) = FspmModelBinder.BindMembers(new[] { userA, userB, userC, phone });

        var member = Assert.Single(members);
        Assert.Equal(string.Empty, member.DeclaringTypeId);
        Assert.Equal(FspmSemanticState.Ambiguous, member.State);
        var note = Assert.Single(notes);
        Assert.Contains("3 Type facts", note);
        Assert.Contains("A|T:A.N.User#a", note);
        Assert.Contains("A|T:A.N.User#b", note);
        Assert.Contains("A|T:A.N.User#c", note);
    }

    // -------------------------------------------------------------
    // Owner-key collision matrix
    // -------------------------------------------------------------

    [Fact]
    public void Same_Name_Different_Assembly_Produces_Distinct_Owner_Keys()
    {
        var aUser = MakeTypeFact("A", "Shared.N", "User", "A|T:Shared.N.User");
        var bUser = MakeTypeFact("B", "Shared.N", "User", "B|T:Shared.N.User");

        // Build directly through the public helper to assert the
        // owner-index dictionary distinguishes the two keys.
        var facts = new[] { aUser, bUser };
        var (index, notes) = FspmModelBinder.BuildOwnerIndex(facts);

        Assert.Equal(2, index.Count);
        Assert.True(index.ContainsKey(new TypeFactOwnerKey("A", "Shared.N", "User")));
        Assert.True(index.ContainsKey(new TypeFactOwnerKey("B", "Shared.N", "User")));
        Assert.Equal(FspmModelBinder.OwnerCandidateCount.One,
            index[new TypeFactOwnerKey("A", "Shared.N", "User")].Count);
        Assert.Equal(FspmModelBinder.OwnerCandidateCount.One,
            index[new TypeFactOwnerKey("B", "Shared.N", "User")].Count);
        Assert.Equal("A|T:Shared.N.User",
            index[new TypeFactOwnerKey("A", "Shared.N", "User")].SingleIdentity);
        Assert.Equal("B|T:Shared.N.User",
            index[new TypeFactOwnerKey("B", "Shared.N", "User")].SingleIdentity);
        Assert.Empty(notes);
    }

    [Fact]
    public void Same_Assembly_Namespace_MetadataName_Collapses_Into_One_Ambiguous_Entry()
    {
        var a = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#1");
        var b = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#2");

        var (index, _) = FspmModelBinder.BuildOwnerIndex(new[] { a, b });

        Assert.Single(index);
        var entry = index[new TypeFactOwnerKey("A", "A.N", "User")];
        Assert.Equal(FspmModelBinder.OwnerCandidateCount.Many, entry.Count);
        Assert.Equal(2, entry.CandidateIdentities.Count);
        Assert.Contains("A|T:A.N.User#1", entry.CandidateIdentities);
        Assert.Contains("A|T:A.N.User#2", entry.CandidateIdentities);
    }

    // -------------------------------------------------------------
    // ContainingTypeName invariant
    // -------------------------------------------------------------

    [Fact]
    public void TypeFact_With_NonEmpty_ContainingTypeName_Is_Rejected_And_Excluded_From_OwnerIndex()
    {
        // A Type fact whose ContainingTypeName is non-empty violates
        // the MintLogicalIdentity invariant (types null this field).
        // Pre-sweep this would have entered the owner index under a
        // silently-wrong key. Post-sweep it is rejected and surfaced.
        var bogusType = MakeTypeFact(
            assemblyName: "A",
            @namespace: "A.N",
            metadataName: "User",
            identityValue: "A|T:A.N.User",
            containingTypeName: "SomeOtherType");

        var (types, typeNotes) = FspmModelBinder.BindTypes(new[] { bogusType });
        var (index, indexNotes) = FspmModelBinder.BuildOwnerIndex(new[] { bogusType });

        Assert.Empty(types);
        var note = Assert.Single(typeNotes);
        Assert.Contains("non-empty ContainingTypeName", note);
        Assert.Contains("User", note);

        Assert.Empty(index);
        var indexNote = Assert.Single(indexNotes);
        Assert.Contains("Rejected Type fact", indexNote);
        Assert.Contains("User", indexNote);
    }

    [Fact]
    public void TypeFact_Name_And_Logical_MemberName_May_Differ_For_Generic_Types()
    {
        // Roslyn reports Name = "List" and MetadataName = "List`1"
        // for a generic type. MintLogicalIdentity stores
        // MetadataName in Logical.MemberName so the owner key
        // (which is the CLR identity, not the user-facing name) is
        // stable across edits. The binder must NOT equate the two
        // and must NOT treat the difference as an invariant break.
        // The owner key here uses MetadataName and the lookup
        // succeeds.
        var listUser = MakeTypeFact(
            assemblyName: "A",
            @namespace: "System.Collections.Generic",
            metadataName: "List`1",
            identityValue: "A|T:System.Collections.Generic.List`1",
            overrideName: "List");

        var (index, notes) = FspmModelBinder.BuildOwnerIndex(new[] { listUser });

        Assert.Single(index);
        Assert.Empty(notes);
        Assert.True(index.ContainsKey(new TypeFactOwnerKey(
            "A",
            "System.Collections.Generic",
            "List`1")));
    }

    // -------------------------------------------------------------
    // Operations + Relations mirror the member result
    // -------------------------------------------------------------

    [Fact]
    public void Operation_Owner_Ambiguous_Propagates_Ambiguous_State_To_Operation()
    {
        var user1 = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#1");
        var user2 = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#2");
        var create = MakeMemberFact(
            assemblyName: "A", @namespace: "A.N",
            containingTypeMetadataName: "User",
            memberName: "Create", memberKind: "Method",
            identityValue: "A|M:A.N.User.Create");

        // BindOperations requires Operation != null on the fact; build one.
        var createWithOp = create with
        {
            Operation = new NativeOperationIdentity(
                ContainingType: "A.N.User",
                Name: "Create",
                Arity: 0,
                Parameters: System.Array.Empty<NativeParameterFact>(),
                ReturnType: "void",
                GenericParameters: System.Array.Empty<string>(),
                Kind: NativeSymbolKind.Method,
                StableId: "A|M:A.N.User.Create"),
        };

        var (ops, notes) = FspmModelBinder.BindOperations(
            new NativeSemanticFact[] { user1, user2, createWithOp });

        var op = Assert.Single(ops);
        Assert.Equal(string.Empty, op.DeclaringTypeId);
        Assert.Equal(FspmSemanticState.Ambiguous, op.State);
        var note = Assert.Single(notes);
        Assert.Contains("Ambiguous", note);
        Assert.Contains("A|T:A.N.User#1", note);
        Assert.Contains("A|T:A.N.User#2", note);
    }

    [Fact]
    public void Ambiguous_Owner_Suppresses_Declares_And_Contains_Relations()
    {
        // Pre-sweep, the first candidate's identity would have been
        // used to mint Declares/Contains relations on the model.
        // Post-sweep, ambiguous owner -> no Declares/Contains
        // relations emitted (the owner-ambiguity note from
        // BindMembers is the single record of the fact).
        var user1 = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#1");
        var user2 = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#2");
        var phone = MakeMemberFact(
            assemblyName: "A", @namespace: "A.N",
            containingTypeMetadataName: "User",
            memberName: "Phone", memberKind: "Property",
            identityValue: "A|P:A.N.User.Phone");

        var (relations, _) = FspmModelBinder.BindRelations(new[] { user1, user2, phone });

        Assert.DoesNotContain(relations, r => r.Kind == "Declares");
        Assert.DoesNotContain(relations, r => r.Kind == "Contains");
    }

    [Fact]
    public void BuildOwnerIndex_Does_Not_Call_First_On_Multi_Element_Groups()
    {
        // Direct defense-in-depth: a one-off static analysis check
        // that the BuildOwnerIndex path no longer references
        // LINQ First / FirstOrDefault on grouped Type facts. The
        // code path is the only one that previously called First().
        // This test fails the build if a future refactor reverts
        // the fix without updating the test.
        var user1 = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#1");
        var user2 = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#2");
        var user3 = MakeTypeFact("A", "A.N", "User", "A|T:A.N.User#3");
        var phone = MakeMemberFact(
            assemblyName: "A", @namespace: "A.N",
            containingTypeMetadataName: "User",
            memberName: "Phone", memberKind: "Property",
            identityValue: "A|P:A.N.User.Phone");

        var (index, _) = FspmModelBinder.BuildOwnerIndex(new[] { user1, user2, user3, phone });

        var entry = Assert.Single(index);
        Assert.Equal(FspmModelBinder.OwnerCandidateCount.Many, entry.Value.Count);
        Assert.Equal(3, entry.Value.CandidateIdentities.Count);
        Assert.Null(entry.Value.SingleIdentity);
    }
}
