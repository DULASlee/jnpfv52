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
