using Model = Foundry.FSPM.SemanticModel;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P14-01 STEP 6-E/F: Fact-to-Model projector. Strictly
/// NativeSemanticFact → SemanticModel records. It NEVER performs symbol
/// lookup, overload resolution, generic inference, or compilation access:
/// every input fact already carries its resolved data. Non-conforming
/// facts are reported as notes, never silently dropped, never guessed.
///
/// <para>
/// P14-01 Post-Freeze Correctness Sweep — owner-index fix.
/// The pre-sweep code constructed the owner index as
/// <c>Dictionary&lt;(Assembly, Namespace, MemberName), NativeSemanticFact&gt;</c>
/// using <c>group.First()</c> for collision: with two Type facts sharing
/// the same key, one of them was silently dropped on the floor. This
/// rewrite makes the index value a list of every Type fact matching the
/// key, and the consumer logic walks the 0/1/&gt;1 cases
/// explicitly:
/// <list type="bullet">
/// <item>0 candidates  → owner missing   → DeclaringTypeId empty + note.</item>
/// <item>1 candidate   → owner resolved  → DeclaringTypeId = its identity, state preserved.</item>
/// <item>&gt;1 candidates → owner ambiguous → DeclaringTypeId empty, state forced to
///     <see cref="FspmResolutionStatus.Ambiguous"/>, note lists the candidate identities.</item>
/// </list>
/// The <c>group.First()</c> call is gone — the binder is honest about
/// which branches admit ambiguity and never auto-picks a candidate.
/// </para>
///
/// <para>
/// The owner key itself is centralised in
/// <see cref="TypeFactKeys.KeyForMember"/>: it is
/// <c>(Assembly, Namespace, MetadataName)</c> where <c>MetadataName</c>
/// comes from the Type fact's <c>Logical.MemberName</c>
/// (<see cref="SemanticIdentityMint.MintLogicalIdentity"/> documents
/// that for <see cref="INamedTypeSymbol"/> the field is set to
/// <c>type.MetadataName</c>). This avoids a "MemberName means type
/// name" implicit overloading that a future
/// <see cref="NativeSymbolKind"/> would silently break.
/// </para>
/// </summary>
public static class FspmModelBinder
{
    internal enum OwnerCandidateCount
    {
        Zero = 0,
        One = 1,
        Many = 2,
    }

    /// <summary>
    /// Outcome of looking up a single member fact's owner Type fact
    /// inside one batch. <see cref="CandidateIdentities"/> is the
    /// ordered (ordinal by <see cref="FspmSymbolId.Value"/>) list of
    /// Type-fact identities that match the owner key; it is empty for
    /// the missing case and has 2+ entries for the ambiguous case.
    /// </summary>
    internal readonly record struct OwnerResolution(
        OwnerCandidateCount Count,
        string? SingleIdentity,
        IReadOnlyList<string> CandidateIdentities)
    {
        public static OwnerResolution Missing() =>
            new(OwnerCandidateCount.Zero, null, Array.Empty<string>());

        public static OwnerResolution Resolved(string identity) =>
            new(OwnerCandidateCount.One, identity, new[] { identity });

        public static OwnerResolution Ambiguous(IReadOnlyList<string> identities) =>
            new(OwnerCandidateCount.Many, null, identities);
    }

    public static (IReadOnlyList<Model.FspmSemanticType> Types, IReadOnlyList<string> Notes) BindTypes(
        IEnumerable<NativeSemanticFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var types = new List<Model.FspmSemanticType>();
        var notes = new List<string>();

        foreach (var fact in facts)
        {
            if (fact.Kind != NativeSymbolKind.Type)
            {
                notes.Add($"Skipped non-Type fact '{fact.Name}' ({fact.Kind}).");
                continue;
            }

            if (!string.Equals(fact.Logical.ContainingTypeName, string.Empty, StringComparison.Ordinal))
            {
                // FIX-OWNER-03: TypeFact must carry an empty
                // ContainingTypeName. Promote a non-empty value from
                // a comment ("appears impossible") into an executable
                // invariant: a Type fact that violates it is reported
                // and excluded from the model rather than silently
                // built into a malformed type record.
                notes.Add(
                    $"Type fact '{fact.Name}' carries non-empty ContainingTypeName " +
                    $"'{fact.Logical.ContainingTypeName}'; excluded. " +
                    $"{TypeFactKeys.TypeContainingTypeNameMustBeEmpty}");
                continue;
            }

            types.Add(new Model.FspmSemanticType(
                Identity: new Model.FspmSemanticIdentity(
                    fact.Identity.Value,
                    fact.Fingerprint.Value),
                Name: fact.Name,
                Namespace: fact.Logical.Namespace,
                Kind: "Type",
                TypeKind: fact.TypeKind.ToString(),
                GenericArity: fact.TypeShape?.Arity ?? 0,
                NullableShape: fact.TypeShape?.NullableAnnotation,
                BaseType: fact.Relationships?.BaseType,
                Interfaces: fact.Relationships?.Interfaces ?? Array.Empty<string>(),
                Fingerprint: fact.Fingerprint.Value,
                Anchor: new Model.FspmSemanticAnchor(
                    fact.Anchor.DocumentIdentity,
                    fact.Anchor.DeclarationAnchor,
                    fact.Anchor.CurrentSpan.StartLine,
                    fact.Anchor.CurrentSpan.StartColumn,
                    fact.Anchor.CurrentSpan.EndLine,
                    fact.Anchor.CurrentSpan.EndColumn),
                State: FspmSemanticStateMapper.FromResolutionStatus(fact.Status)));
        }

        return (types.ToArray(), notes.ToArray());
    }

    /// <summary>
    /// Binds Property/Field/Event facts. Owner linkage is batch-scoped:
    /// a member resolves its DeclaringTypeId against Type-kind facts in
    /// the SAME batch by (Assembly, Namespace, MetadataName) lookup.
    /// Facts of other kinds are outside this method's input domain and
    /// ignored by design. A member whose owner is absent keeps an empty
    /// DeclaringTypeId plus a note — never an invented identity. A
    /// member whose owner is AMBIGUOUS (two or more Type facts share
    /// the same owner key) is reported as Ambiguous and its
    /// DeclaringTypeId is left empty — never the identity of the first
    /// match.
    /// </summary>
    public static (IReadOnlyList<Model.FspmSemanticMember> Members, IReadOnlyList<string> Notes) BindMembers(
        IEnumerable<NativeSemanticFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var all = facts.ToArray();
        var (owners, ownerIndexNotes) = BuildOwnerIndex(all);

        var members = new List<Model.FspmSemanticMember>();
        var notes = new List<string>(ownerIndexNotes);

        foreach (var fact in all)
        {
            var memberKind = fact.Kind switch
            {
                NativeSymbolKind.Property => "Property",
                NativeSymbolKind.Field => "Field",
                NativeSymbolKind.Event => "Event",
                _ => null,
            };

            if (memberKind is null)
            {
                continue;
            }

            var ownerKey = TypeFactKeys.KeyForMember(fact);
            var ownerResolution = owners.TryGetValue(ownerKey, out var bucket)
                ? bucket
                : OwnerResolution.Missing();

            var (declaringTypeId, ownerState) = MaterializeOwner(ownerResolution, fact, notes);
            var memberState = ownerState ?? FspmSemanticStateMapper.FromResolutionStatus(fact.Status);

            members.Add(new Model.FspmSemanticMember(
                Identity: new Model.FspmSemanticIdentity(
                    fact.Identity.Value,
                    fact.Fingerprint.Value),
                Name: fact.Name,
                DeclaringTypeId: declaringTypeId,
                MemberKind: memberKind,
                Type: fact.TypeShape?.OriginalDefinition ?? string.Empty,
                Fingerprint: fact.Fingerprint.Value,
                Anchor: new Model.FspmSemanticAnchor(
                    fact.Anchor.DocumentIdentity,
                    fact.Anchor.DeclarationAnchor,
                    fact.Anchor.CurrentSpan.StartLine,
                    fact.Anchor.CurrentSpan.StartColumn,
                    fact.Anchor.CurrentSpan.EndLine,
                    fact.Anchor.CurrentSpan.EndColumn),
                State: memberState));
        }

        return (members.ToArray(), notes.ToArray());
    }

    /// <summary>
    /// Binds Method/Constructor facts (Indexer facts bind here with empty
    /// parameters). Overloads stay distinct entries; parameter OwnerId is
    /// wired to the operation's LogicalId. Facts of other kinds are
    /// outside this method's input domain and ignored by design.
    /// Owner ambiguity / missing follows the same rule as
    /// <see cref="BindMembers"/>: never pick a first candidate.
    /// </summary>
    public static (IReadOnlyList<Model.FspmSemanticOperation> Operations, IReadOnlyList<string> Notes) BindOperations(
        IEnumerable<NativeSemanticFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var all = facts.ToArray();
        var (owners, ownerIndexNotes) = BuildOwnerIndex(all);

        var operations = new List<Model.FspmSemanticOperation>();
        var notes = new List<string>(ownerIndexNotes);

        foreach (var fact in all)
        {
            var operationKind = fact.Kind switch
            {
                NativeSymbolKind.Method => "Method",
                NativeSymbolKind.Constructor => "Constructor",
                NativeSymbolKind.Indexer => "Indexer",
                _ => null,
            };

            if (operationKind is null || fact.Operation is null)
            {
                continue;
            }

            var ownerKey = TypeFactKeys.KeyForMember(fact);
            var ownerResolution = owners.TryGetValue(ownerKey, out var bucket)
                ? bucket
                : OwnerResolution.Missing();

            var (declaringTypeId, ownerState) = MaterializeOwner(ownerResolution, fact, notes);
            var operationState = ownerState ?? FspmSemanticStateMapper.FromResolutionStatus(fact.Status);

            var identity = new Model.FspmSemanticIdentity(
                fact.Identity.Value,
                fact.Fingerprint.Value);

            var parameters = fact.Operation.Parameters
                .Select((p, index) => new Model.FspmSemanticParameter(
                    Name: p.Name,
                    Type: p.ParameterType,
                    Position: index,
                    RefKind: p.RefKind,
                    IsOptional: p.IsOptional,
                    HasDefaultValue: p.DefaultValue is not null,
                    DefaultValue: p.DefaultValue,
                    IsParams: p.IsParams,
                    NullableShape: p.NullableAnnotation,
                    OwnerId: identity.LogicalId))
                .ToArray();

            operations.Add(new Model.FspmSemanticOperation(
                Identity: identity,
                Name: fact.Name,
                DeclaringTypeId: declaringTypeId,
                OperationKind: operationKind,
                Parameters: parameters,
                ReturnType: fact.Operation.ReturnType,
                GenericArity: fact.Operation.Arity,
                Fingerprint: fact.Fingerprint.Value,
                Anchor: new Model.FspmSemanticAnchor(
                    fact.Anchor.DocumentIdentity,
                    fact.Anchor.DeclarationAnchor,
                    fact.Anchor.CurrentSpan.StartLine,
                    fact.Anchor.CurrentSpan.StartColumn,
                    fact.Anchor.CurrentSpan.EndLine,
                    fact.Anchor.CurrentSpan.EndColumn),
                State: operationState));
        }

        return (operations.ToArray(), notes.ToArray());
    }

    /// <summary>
    /// Binds Inheritance / Implementation / Override /
    /// ExplicitInterfaceImplementation relations from facts carrying
    /// relationship data, plus Declares / Contains containment links
    /// between owner Type facts and directly-declared member facts.
    /// ResolvedTargetId stays empty for cross-display links: resolving
    /// targets to model identities is P14-02 Reference System work.
    /// Declares/Contains always carry BOTH resolved ids (they are only
    /// emitted when the owner Type fact is in the batch). A member whose
    /// owner is absent or AMBIGUOUS yields no containment link here; the
    /// corresponding BindMembers / BindOperations note is the single
    /// record of that fact (no duplicate).
    /// </summary>
    public static (IReadOnlyList<Model.FspmSemanticRelation> Relations, IReadOnlyList<string> Notes) BindRelations(
        IEnumerable<NativeSemanticFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var all = facts.ToArray();
        var relations = new List<Model.FspmSemanticRelation>();
        var notes = new List<string>();

        foreach (var fact in all)
        {
            var fromId = fact.Identity.Value;
            var relationships = fact.Relationships;
            if (relationships is null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(relationships.BaseType))
            {
                relations.Add(new Model.FspmSemanticRelation(fromId, "Inheritance", relationships.BaseType, string.Empty));
            }

            foreach (var iface in relationships.Interfaces)
            {
                relations.Add(new Model.FspmSemanticRelation(fromId, "Implementation", iface, string.Empty));
            }

            var overridden = relationships.OverriddenMethod ?? relationships.OverriddenProperty;
            if (!string.IsNullOrEmpty(overridden))
            {
                relations.Add(new Model.FspmSemanticRelation(fromId, "Override", overridden, string.Empty));
            }

            foreach (var explicitImpl in relationships.ExplicitInterfaceImplementations)
            {
                relations.Add(new Model.FspmSemanticRelation(fromId, "ExplicitInterfaceImplementation", explicitImpl, string.Empty));
            }
        }

        var (owners, ownerIndexNotes) = BuildOwnerIndex(all);
        notes.AddRange(ownerIndexNotes);
        foreach (var fact in all)
        {
            if (!IsMemberKind(fact.Kind))
            {
                continue;
            }

            var ownerKey = TypeFactKeys.KeyForMember(fact);
            if (!owners.TryGetValue(ownerKey, out var ownerResolution))
            {
                continue;
            }

            // Declares/Contains only fire when the owner is uniquely
            // identified — ambiguity is recorded by the owner lookup
            // once (BindMembers / BindOperations already wrote the
            // note) and never duplicated here.
            if (ownerResolution.Count != OwnerCandidateCount.One || ownerResolution.SingleIdentity is null)
            {
                continue;
            }

            var ownerIdentity = ownerResolution.SingleIdentity;
            relations.Add(new Model.FspmSemanticRelation(
                ownerIdentity, "Declares", fact.QualifiedName, fact.Identity.Value));
            relations.Add(new Model.FspmSemanticRelation(
                fact.Identity.Value, "Contains", fact.QualifiedName, ownerIdentity));
        }

        return (relations.ToArray(), notes.ToArray());
    }

    /// <summary>
    /// Assembles a full model from one batch of facts: runs all four
    /// bind paths, flattens operation parameters, aggregates notes.
    /// Pure projection — see class documentation for the prohibitions.
    /// </summary>
    public static (Model.FspmSemanticModel Model, IReadOnlyList<string> Notes) Assemble(
        IEnumerable<NativeSemanticFact> facts,
        Model.FspmSemanticModelMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(metadata);

        var all = facts.ToArray();

        // Partition by kind first so per-path "skipped" notes only fire
        // for genuinely unmappable facts, not for out-of-domain input.
        var typeFacts = all.Where(f => f.Kind == NativeSymbolKind.Type).ToArray();
        var memberFacts = all.Where(f =>
            f.Kind == NativeSymbolKind.Property ||
            f.Kind == NativeSymbolKind.Field ||
            f.Kind == NativeSymbolKind.Event).ToArray();
        var operationFacts = all.Where(f =>
            f.Kind == NativeSymbolKind.Method ||
            f.Kind == NativeSymbolKind.Constructor).ToArray();

        var (types, typeNotes) = BindTypes(typeFacts);
        var (members, memberNotes) = BindMembers(memberFacts.Concat(typeFacts).ToArray());
        var (operations, operationNotes) = BindOperations(operationFacts.Concat(typeFacts).ToArray());
        var (relations, relationNotes) = BindRelations(all);

        var parameters = operations.SelectMany(o => o.Parameters).ToArray();
        var notes = typeNotes
            .Concat(memberNotes)
            .Concat(operationNotes)
            .Concat(relationNotes)
            .ToArray();

        var model = new Model.FspmSemanticModel(
            Types: types,
            Members: members,
            Operations: operations,
            Parameters: parameters,
            Relations: relations,
            Diagnostics: notes,
            Metadata: metadata);

        return (model, notes);
    }

    private static bool IsMemberKind(NativeSymbolKind kind) =>
        kind == NativeSymbolKind.Property ||
        kind == NativeSymbolKind.Field ||
        kind == NativeSymbolKind.Event ||
        kind == NativeSymbolKind.Method ||
        kind == NativeSymbolKind.Constructor ||
        kind == NativeSymbolKind.Indexer;

    /// <summary>
    /// FIX-OWNER-01: replaces the old <c>group.First()</c> aggregator.
    /// Walks the input facts once, partitioning on
    /// <see cref="TypeFactKeys.TryGetOwnerKey"/>. Rejected Type facts
    /// (invariant violation, missing assembly) are reported as notes
    /// and excluded from the index entirely. The dictionary value is
    /// the full ordered list of Type facts sharing a key, so the
    /// consumer can see exactly which identities collide.
    /// </summary>
    internal static (IReadOnlyDictionary<TypeFactOwnerKey, OwnerResolution> Index, IReadOnlyList<string> Notes) BuildOwnerIndex(
        NativeSemanticFact[] facts)
    {
        var byKey = new Dictionary<TypeFactOwnerKey, List<NativeSemanticFact>>();
        var notes = new List<string>();

        foreach (var fact in facts)
        {
            if (!TypeFactKeys.TryGetOwnerKey(fact, out var key, out var rejection))
            {
                if (TypeFactKeys.IsTypeFact(fact))
                {
                    // Rejection of a Type fact is an invariant
                    // break — log it. Non-Type facts are filtered by
                    // TryGetOwnerKey without comment (the per-bind
                    // path emits its own skip note for those).
                    notes.Add($"Rejected Type fact '{fact.Name}': {rejection}");
                }

                continue;
            }

            if (!byKey.TryGetValue(key, out var bucket))
            {
                bucket = new List<NativeSemanticFact>();
                byKey[key] = bucket;
            }

            bucket.Add(fact);
        }

        var index = new Dictionary<TypeFactOwnerKey, OwnerResolution>(byKey.Count);
        foreach (var (key, bucket) in byKey)
        {
            // Order the candidate identities ordinally so notes /
            // diagnostics are stable across runs.
            var orderedIdentities = bucket
                .Select(f => f.Identity.Value)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            var resolution = bucket.Count switch
            {
                0 => OwnerResolution.Missing(),
                1 => OwnerResolution.Resolved(orderedIdentities[0]),
                _ => OwnerResolution.Ambiguous(orderedIdentities),
            };

            index[key] = resolution;
        }

        return (index, notes);
    }

    /// <summary>
    /// Returns the DeclaringTypeId to embed in the model record plus
    /// the owner-forced state (if any). When the owner is
    /// uniquely resolved the returned state is <c>null</c> and the
    /// caller keeps the fact's own status; when the owner is
    /// missing or ambiguous the returned state is
    /// <see cref="FspmResolutionStatus.NotFound"/> /
    /// <see cref="FspmResolutionStatus.Ambiguous"/> and a note
    /// describing the cause is appended.
    /// </summary>
    private static (string DeclaringTypeId, Model.FspmSemanticState? ForcedState) MaterializeOwner(
        OwnerResolution owner,
        NativeSemanticFact memberFact,
        List<string> notes)
    {
        switch (owner.Count)
        {
            case OwnerCandidateCount.One:
                if (owner.SingleIdentity is null)
                {
                    notes.Add(
                        $"Owner index for '{memberFact.Name}' reports One candidate but identity is null; " +
                        "declaring DeclaringTypeId empty and demoting to NotFound.");
                    return (string.Empty, Model.FspmSemanticState.NotFound);
                }

                return (owner.SingleIdentity, null);

            case OwnerCandidateCount.Many:
                var listed = string.Join(", ", owner.CandidateIdentities);
                notes.Add(
                    $"Owner key for '{memberFact.Name}' is Ambiguous ({owner.CandidateIdentities.Count} Type facts): {listed}. " +
                    "DeclaringTypeId left empty; never auto-selected a candidate.");
                return (string.Empty, Model.FspmSemanticState.Ambiguous);

            default:
                notes.Add(
                    $"Owner Type fact absent for '{memberFact.Name}'; DeclaringTypeId left empty.");
                return (string.Empty, Model.FspmSemanticState.NotFound);
        }
    }
}
