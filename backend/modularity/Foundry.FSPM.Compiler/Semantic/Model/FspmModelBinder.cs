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
        var owners = all
            .Where(f => f.Kind == NativeSymbolKind.Type)
            .ToDictionary(
                f => (f.Logical.Namespace, f.Logical.MemberName),
                f => f.Identity.Value);

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

            var declaringTypeId = string.Empty;
            if (!owners.TryGetValue((fact.Logical.Namespace, fact.Logical.ContainingTypeName), out var ownerId))
            {
                notes.Add($"Owner Type fact absent for member '{fact.Name}'; DeclaringTypeId left empty.");
            }
            else
            {
                declaringTypeId = ownerId;
            }

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
}
