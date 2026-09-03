using Model = Foundry.FSPM.SemanticModel;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P14-01 STEP 6-E/F: Fact-to-Model projector. Strictly
/// NativeSemanticFact → SemanticModel records. It NEVER performs symbol
/// lookup, overload resolution, generic inference, or compilation access:
/// every input fact already carries its resolved data. Non-conforming
/// facts are reported as notes, never silently dropped, never guessed.
/// </summary>
public static class FspmModelBinder
{
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

            types.Add(new Model.FspmSemanticType(
                Identity: new Model.FspmSemanticIdentity(
                    fact.Identity.Value,
                    fact.Fingerprint.Value),
                Name: fact.Name,
                Namespace: fact.Logical.Namespace,
                Kind: "Type",
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
    /// the SAME batch by (Namespace, ContainingTypeName == Name) display
    /// lookup (text as lookup key only, per architecture). Facts of other
    /// kinds are outside this method's input domain and ignored by design.
    /// A member whose owner is absent keeps an empty DeclaringTypeId plus
    /// a note — never an invented identity.
    /// </summary>
    public static (IReadOnlyList<Model.FspmSemanticMember> Members, IReadOnlyList<string> Notes) BindMembers(
        IEnumerable<NativeSemanticFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var all = facts.ToArray();
        // Owner index is keyed by the TYPE's own (Namespace, MemberName):
        // a type fact carries an empty ContainingTypeName by construction,
        // so member lookup matches (Namespace, member.ContainingTypeName)
        // against (Namespace, type.MemberName).
        var owners = BuildOwnerIndex(all);

        var members = new List<Model.FspmSemanticMember>();
        var notes = new List<string>();

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

            var declaringTypeId = ResolveOwner(fact, owners, notes);

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
                State: FspmSemanticStateMapper.FromResolutionStatus(fact.Status)));
        }

        return (members.ToArray(), notes.ToArray());
    }

    /// <summary>
    /// Binds Method/Constructor facts (Indexer facts bind here with empty
    /// parameters). Overloads stay distinct entries; parameter OwnerId is
    /// wired to the operation's LogicalId. Facts of other kinds are
    /// outside this method's input domain and ignored by design.
    /// </summary>
    public static (IReadOnlyList<Model.FspmSemanticOperation> Operations, IReadOnlyList<string> Notes) BindOperations(
        IEnumerable<NativeSemanticFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var all = facts.ToArray();
        var owners = BuildOwnerIndex(all);

        var operations = new List<Model.FspmSemanticOperation>();
        var notes = new List<string>();

        foreach (var fact in all)
        {
            var operationKind = fact.Kind switch
            {
                NativeSymbolKind.Method => "Method",
                NativeSymbolKind.Constructor => "Constructor",
                _ => null,
            };

            if (operationKind is null || fact.Operation is null)
            {
                continue;
            }

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
                DeclaringTypeId: ResolveOwner(fact, owners, notes),
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
                State: FspmSemanticStateMapper.FromResolutionStatus(fact.Status)));
        }

        return (operations.ToArray(), notes.ToArray());
    }

    /// <summary>
    /// Binds Inheritance / Implementation / Override /
    /// ExplicitInterfaceImplementation relations from facts carrying
    /// relationship data. ResolvedTargetId stays empty: resolving targets
    /// to model identities is P14-02 Reference System work, not P14-01.
    /// </summary>
    public static (IReadOnlyList<Model.FspmSemanticRelation> Relations, IReadOnlyList<string> Notes) BindRelations(
        IEnumerable<NativeSemanticFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var relations = new List<Model.FspmSemanticRelation>();
        var notes = new List<string>();

        foreach (var fact in facts)
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

    private static Dictionary<(string Assembly, string Namespace, string TypeName), string> BuildOwnerIndex(
        NativeSemanticFact[] facts) =>
        facts
            .Where(f => f.Kind == NativeSymbolKind.Type)
            .GroupBy(f => (f.Logical.AssemblyName, f.Logical.Namespace, f.Logical.MemberName))
            .ToDictionary(
                g => g.Key,
                g => g.First().Identity.Value);

    private static string ResolveOwner(
        NativeSemanticFact fact,
        Dictionary<(string Assembly, string Namespace, string TypeName), string> owners,
        List<string> notes)
    {
        if (owners.TryGetValue(
            (fact.Logical.AssemblyName, fact.Logical.Namespace, fact.Logical.ContainingTypeName),
            out var ownerId))
        {
            return ownerId;
        }

        notes.Add($"Owner Type fact absent for '{fact.Name}'; DeclaringTypeId left empty.");
        return string.Empty;
    }
}
