using Model = Foundry.FSPM.SemanticModel;

namespace Foundry.FSPM.Compiler.Semantic.Reference;

/// <summary>
/// P14-02: resolves identity-keyed references against one assembled
/// model. Lookup is by LogicalId exact match over the target collection —
/// never by short-name dictionary, never by Split("."). Each verdict
/// carries Status + Reason + target facts; Reason is never empty.
/// </summary>
public static class FspmReferenceResolver
{
    public static Model.FspmReferenceResolution ResolveTypeRef(
        Model.FspmTypeRef reference,
        Model.FspmSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(model);

        var matches = model.Types
            .Where(t => t.Identity.LogicalId == reference.TargetIdentity.LogicalId)
            .ToArray();

        if (matches.Length == 0)
        {
            // Same identity living in another collection is a kind error,
            // not an absence: report what it actually is.
            var actualKind = FindKindById(model, reference.TargetIdentity.LogicalId);
            if (actualKind is not null)
            {
                return new Model.FspmReferenceResolution(
                    Model.FspmReferenceStatus.WrongKind,
                    IsResolved: false,
                    Reason: $"TypeRef target '{reference.TargetIdentity.LogicalId}' exists as {actualKind}, not as Type.",
                    TargetIdentity: null,
                    TargetFingerprint: string.Empty,
                    TargetKind: actualKind,
                    Owner: string.Empty);
            }

            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Missing,
                IsResolved: false,
                Reason: $"TypeRef target '{reference.TargetIdentity.LogicalId}' not found in model.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: string.Empty,
                Owner: string.Empty);
        }

        if (matches.Length > 1)
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Ambiguous,
                IsResolved: false,
                Reason: $"TypeRef target '{reference.TargetIdentity.LogicalId}' matches {matches.Length} model types; refusing to pick.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: "Type",
                Owner: string.Empty);
        }

        return CheckFingerprint(reference, matches[0].Identity, matches[0].Fingerprint, "Type", string.Empty);
    }

    internal static string? FindKindById(Model.FspmSemanticModel model, string logicalId)
    {
        if (model.Types.Any(t => t.Identity.LogicalId == logicalId))
        {
            return "Type";
        }

        var member = model.Members.FirstOrDefault(m => m.Identity.LogicalId == logicalId);
        if (member is not null)
        {
            return member.MemberKind;
        }

        var operation = model.Operations.FirstOrDefault(o => o.Identity.LogicalId == logicalId);
        if (operation is not null)
        {
            return operation.OperationKind;
        }

        if (model.Relations.Any(r => r.FromId == logicalId))
        {
            return "Relation";
        }

        return null;
    }

    public static Model.FspmReferenceResolution ResolveEntityRef(
        Model.FspmEntityRef reference,
        Model.FspmSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(model);

        var matches = model.Types
            .Where(t => t.Identity.LogicalId == reference.TargetIdentity.LogicalId)
            .ToArray();

        if (matches.Length == 0)
        {
            var actualKind = FindKindById(model, reference.TargetIdentity.LogicalId);
            if (actualKind is not null)
            {
                return new Model.FspmReferenceResolution(
                    Model.FspmReferenceStatus.WrongKind,
                    IsResolved: false,
                    Reason: $"EntityRef target '{reference.TargetIdentity.LogicalId}' exists as {actualKind}, not as an entity Type.",
                    TargetIdentity: null,
                    TargetFingerprint: string.Empty,
                    TargetKind: actualKind,
                    Owner: string.Empty);
            }

            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Missing,
                IsResolved: false,
                Reason: $"EntityRef target '{reference.TargetIdentity.LogicalId}' not found in model.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: string.Empty,
                Owner: string.Empty);
        }

        if (matches.Length > 1)
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Ambiguous,
                IsResolved: false,
                Reason: $"EntityRef target '{reference.TargetIdentity.LogicalId}' matches {matches.Length} model types; refusing to pick.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: "Type",
                Owner: string.Empty);
        }

        return CheckFingerprint(reference, matches[0].Identity, matches[0].Fingerprint, "Type", string.Empty);
    }

    public static Model.FspmReferenceResolution ResolveMemberRef(
        Model.FspmMemberRef reference,
        Model.FspmSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(model);

        var matches = model.Members
            .Where(m => m.Identity.LogicalId == reference.TargetIdentity.LogicalId)
            .ToArray();

        if (matches.Length == 0)
        {
            var actualKind = FindKindById(model, reference.TargetIdentity.LogicalId);
            if (actualKind is not null)
            {
                return new Model.FspmReferenceResolution(
                    Model.FspmReferenceStatus.WrongKind,
                    IsResolved: false,
                    Reason: $"MemberRef target '{reference.TargetIdentity.LogicalId}' exists as {actualKind}, not as {reference.ExpectedMemberKind}.",
                    TargetIdentity: null,
                    TargetFingerprint: string.Empty,
                    TargetKind: actualKind,
                    Owner: string.Empty);
            }

            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Missing,
                IsResolved: false,
                Reason: $"MemberRef target '{reference.TargetIdentity.LogicalId}' not found in model.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: string.Empty,
                Owner: string.Empty);
        }

        if (matches.Length > 1)
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Ambiguous,
                IsResolved: false,
                Reason: $"MemberRef target '{reference.TargetIdentity.LogicalId}' matches {matches.Length} model members; refusing to pick.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: reference.ExpectedMemberKind,
                Owner: string.Empty);
        }

        var member = matches[0];
        if (!string.Equals(member.MemberKind, reference.ExpectedMemberKind, StringComparison.Ordinal))
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.WrongKind,
                IsResolved: false,
                Reason: $"MemberRef expects {reference.ExpectedMemberKind} but target is {member.MemberKind}.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: member.MemberKind,
                Owner: member.DeclaringTypeId);
        }

        if (!string.IsNullOrEmpty(reference.OwnerId)
            && !string.Equals(member.DeclaringTypeId, reference.OwnerId, StringComparison.Ordinal))
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.WrongOwner,
                IsResolved: false,
                Reason: $"MemberRef owner mismatch: expected '{reference.OwnerId}', actual '{member.DeclaringTypeId}'.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: member.MemberKind,
                Owner: member.DeclaringTypeId);
        }

        return CheckFingerprint(reference, member.Identity, member.Fingerprint, member.MemberKind, member.DeclaringTypeId);
    }

    public static Model.FspmReferenceResolution ResolveOperationRef(
        Model.FspmOperationRef reference,
        Model.FspmSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(model);

        var matches = model.Operations
            .Where(o => o.Identity.LogicalId == reference.TargetIdentity.LogicalId)
            .ToArray();

        if (matches.Length == 0)
        {
            var actualKind = FindKindById(model, reference.TargetIdentity.LogicalId);
            if (actualKind is not null)
            {
                return new Model.FspmReferenceResolution(
                    Model.FspmReferenceStatus.WrongKind,
                    IsResolved: false,
                    Reason: $"OperationRef target '{reference.TargetIdentity.LogicalId}' exists as {actualKind}, not as Operation.",
                    TargetIdentity: null,
                    TargetFingerprint: string.Empty,
                    TargetKind: actualKind,
                    Owner: string.Empty);
            }

            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Missing,
                IsResolved: false,
                Reason: $"OperationRef target '{reference.TargetIdentity.LogicalId}' not found in model.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: string.Empty,
                Owner: string.Empty);
        }

        if (matches.Length > 1)
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Ambiguous,
                IsResolved: false,
                Reason: $"OperationRef target '{reference.TargetIdentity.LogicalId}' matches {matches.Length} model operations; refusing to pick.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: matches[0].OperationKind,
                Owner: string.Empty);
        }

        var operation = matches[0];
        if (!string.IsNullOrEmpty(reference.OwnerId)
            && !string.Equals(operation.DeclaringTypeId, reference.OwnerId, StringComparison.Ordinal))
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.WrongOwner,
                IsResolved: false,
                Reason: $"OperationRef owner mismatch: expected '{reference.OwnerId}', actual '{operation.DeclaringTypeId}'.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: operation.OperationKind,
                Owner: operation.DeclaringTypeId);
        }

        return CheckFingerprint(reference, operation.Identity, operation.Fingerprint, operation.OperationKind, operation.DeclaringTypeId);
    }

    public static Model.FspmReferenceResolution ResolveParameterRef(
        Model.FspmParameterRef reference,
        Model.FspmSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(model);

        var owners = model.Operations
            .Where(o => o.Identity.LogicalId == reference.TargetIdentity.LogicalId)
            .ToArray();

        if (owners.Length == 0)
        {
            var actualKind = FindKindById(model, reference.TargetIdentity.LogicalId);
            if (actualKind is not null)
            {
                return new Model.FspmReferenceResolution(
                    Model.FspmReferenceStatus.WrongKind,
                    IsResolved: false,
                    Reason: $"ParameterRef owner '{reference.TargetIdentity.LogicalId}' exists as {actualKind}, not as Operation.",
                    TargetIdentity: null,
                    TargetFingerprint: string.Empty,
                    TargetKind: actualKind,
                    Owner: string.Empty);
            }

            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Missing,
                IsResolved: false,
                Reason: $"ParameterRef owner operation '{reference.TargetIdentity.LogicalId}' not found in model.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: string.Empty,
                Owner: string.Empty);
        }

        if (owners.Length > 1)
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Ambiguous,
                IsResolved: false,
                Reason: $"ParameterRef owner '{reference.TargetIdentity.LogicalId}' matches {owners.Length} operations; refusing to pick.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: "Operation",
                Owner: string.Empty);
        }

        var owner = owners[0];
        if (reference.Position < 0 || reference.Position >= owner.Parameters.Count)
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Missing,
                IsResolved: false,
                Reason: $"Operation '{owner.Identity.LogicalId}' has no parameter at position {reference.Position}.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: "Parameter",
                Owner: owner.Identity.LogicalId);
        }

        var parameter = owner.Parameters[reference.Position];
        return CheckFingerprint(reference, owner.Identity, owner.Fingerprint, "Parameter:" + parameter.Name, owner.Identity.LogicalId);
    }

    public static Model.FspmReferenceResolution ResolveRelationRef(
        Model.FspmRelationRef reference,
        Model.FspmSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(model);

        var matches = model.Relations
            .Where(r => r.FromId == reference.TargetIdentity.LogicalId
                && r.Kind == reference.RelationKind
                && (string.IsNullOrEmpty(reference.TargetDisplay)
                    || r.Target.Contains(reference.TargetDisplay, StringComparison.Ordinal)
                    || r.ResolvedTargetId.Contains(reference.TargetDisplay, StringComparison.Ordinal)))
            .ToArray();

        if (matches.Length == 0)
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Missing,
                IsResolved: false,
                Reason: $"RelationRef '{reference.RelationKind}' from '{reference.TargetIdentity.LogicalId}' not found in model.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: string.Empty,
                Owner: reference.TargetIdentity.LogicalId);
        }

        if (matches.Length > 1)
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Ambiguous,
                IsResolved: false,
                Reason: $"RelationRef '{reference.RelationKind}' from '{reference.TargetIdentity.LogicalId}' matches {matches.Length} relations; refusing to pick.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: "Relation",
                Owner: reference.TargetIdentity.LogicalId);
        }

        if (!string.IsNullOrEmpty(reference.ExpectedFingerprint))
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Invalid,
                IsResolved: false,
                Reason: "RelationRef does not support ExpectedFingerprint (relations carry no fingerprint); pin endpoint identities instead.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: "Relation",
                Owner: reference.TargetIdentity.LogicalId);
        }

        var relation = matches[0];
        return new Model.FspmReferenceResolution(
            Model.FspmReferenceStatus.Valid,
            IsResolved: true,
            Reason: $"Resolved relation '{relation.Kind}' from '{relation.FromId}'.",
            TargetIdentity: reference.TargetIdentity,
            TargetFingerprint: string.Empty,
            TargetKind: "Relation:" + relation.Kind,
            Owner: reference.TargetIdentity.LogicalId);
    }

    /// <summary>
    /// P14-02-H: unified validation entry. Dispatches on the concrete
    /// reference type; unknown record kinds are Invalid (never thrown).
    /// P14-03 consumes this instead of per-kind methods.
    /// </summary>
    public static Model.FspmReferenceResolution ValidateReference(
        Model.FspmSemanticReference reference,
        Model.FspmSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(model);

        return reference switch
        {
            Model.FspmTypeRef typeRef => ResolveTypeRef(typeRef, model),
            Model.FspmEntityRef entityRef => ResolveEntityRef(entityRef, model),
            Model.FspmMemberRef memberRef => ResolveMemberRef(memberRef, model),
            Model.FspmOperationRef operationRef => ResolveOperationRef(operationRef, model),
            Model.FspmParameterRef parameterRef => ResolveParameterRef(parameterRef, model),
            Model.FspmRelationRef relationRef => ResolveRelationRef(relationRef, model),
            _ => new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Invalid,
                IsResolved: false,
                Reason: $"Unknown reference record '{reference.GetType().Name}'; cannot validate.",
                TargetIdentity: null,
                TargetFingerprint: string.Empty,
                TargetKind: string.Empty,
                Owner: string.Empty),
        };
    }

    public static IReadOnlyList<Model.FspmReferenceResolution> ValidateAll(
        IEnumerable<Model.FspmSemanticReference> references,
        Model.FspmSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(references);
        ArgumentNullException.ThrowIfNull(model);

        return references.Select(r => ValidateReference(r, model)).ToArray();
    }

    internal static Model.FspmReferenceResolution CheckFingerprint(
        Model.FspmSemanticReference reference,
        Model.FspmSemanticIdentity actualIdentity,
        string actualFingerprint,
        string targetKind,
        string owner)
    {
        if (!string.IsNullOrEmpty(reference.ExpectedFingerprint)
            && !string.Equals(reference.ExpectedFingerprint, actualFingerprint, StringComparison.Ordinal))
        {
            return new Model.FspmReferenceResolution(
                Model.FspmReferenceStatus.Stale,
                IsResolved: true,
                Reason: $"Target '{actualIdentity.LogicalId}' exists but fingerprint changed: " +
                    $"expected {reference.ExpectedFingerprint}, actual {actualFingerprint}.",
                TargetIdentity: actualIdentity,
                TargetFingerprint: actualFingerprint,
                TargetKind: targetKind,
                Owner: owner);
        }

        return new Model.FspmReferenceResolution(
            Model.FspmReferenceStatus.Valid,
            IsResolved: true,
            Reason: $"Resolved '{actualIdentity.LogicalId}' ({targetKind}).",
            TargetIdentity: actualIdentity,
            TargetFingerprint: actualFingerprint,
            TargetKind: targetKind,
            Owner: owner);
    }
}
