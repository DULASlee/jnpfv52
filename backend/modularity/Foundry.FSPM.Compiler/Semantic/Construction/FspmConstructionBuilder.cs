using Foundry.FSPM.Compiler.Semantic.Reference;
using Model = Foundry.FSPM.SemanticModel;

namespace Foundry.FSPM.Compiler.Semantic.Construction;

/// <summary>
/// P14-03: builds pure semantic constructions from P14-02 references.
/// Lifecycle: Create (Mutable) → Attach (Mutable) → Validate →
/// Freeze (Frozen). The builder NEVER touches Roslyn, Compilation,
/// SyntaxTree, or symbol resolution: every semantic question is answered
/// by <see cref="FspmReferenceResolver"/> against a supplied model.
/// Frozen nodes reject further Attach with InvalidOperationException
/// (programmer error, not a semantic verdict).
/// </summary>
public static class FspmConstructionBuilder
{
    public static Model.FspmConstruction Create(string kind, string name, string owner = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var id = string.IsNullOrEmpty(owner) ? $"{kind}:{name}" : $"{owner}/{kind}:{name}";
        var root = new Model.FspmConstructionNode(id, kind, name, owner, Model.FspmConstructionState.Mutable);
        return new Model.FspmConstruction(
            Id: id,
            Kind: kind,
            Name: name,
            Owner: owner,
            Nodes: new[] { root },
            Edges: Array.Empty<Model.FspmConstructionEdge>(),
            State: Model.FspmConstructionState.Mutable,
            Fingerprint: string.Empty);
    }

    public static Model.FspmConstruction Attach(
        Model.FspmConstruction construction,
        string parentId,
        string childKind,
        string childName,
        string role,
        Model.FspmSemanticReference reference)
    {
        ArgumentNullException.ThrowIfNull(construction);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(childKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(childName);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentNullException.ThrowIfNull(reference);

        if (construction.State == Model.FspmConstructionState.Frozen)
        {
            throw new InvalidOperationException(
                $"Cannot Attach to frozen construction '{construction.Id}'.");
        }

        if (!construction.Nodes.Any(n => n.Id == parentId))
        {
            throw new InvalidOperationException(
                $"Parent node '{parentId}' does not exist in construction '{construction.Id}'.");
        }

        var childId = $"{parentId}/{childKind}:{childName}";
        var child = new Model.FspmConstructionNode(
            childId, childKind, childName, parentId, Model.FspmConstructionState.Mutable);
        var edge = new Model.FspmConstructionEdge(
            ParentId: parentId,
            Role: role,
            Reference: reference,
            TargetIdentity: reference.TargetIdentity.LogicalId,
            TargetFingerprint: reference.ExpectedFingerprint);

        return construction with
        {
            Nodes = construction.Nodes.Concat(new[] { child }).ToArray(),
            Edges = construction.Edges.Concat(new[] { edge }).ToArray(),
        };
    }

    public static Model.FspmConstructionValidation Validate(
        Model.FspmConstruction construction,
        Model.FspmSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(construction);
        ArgumentNullException.ThrowIfNull(model);

        var bindings = new List<Model.FspmReferenceBinding>();
        var issues = new List<string>();

        foreach (var node in construction.Nodes.Where(n => n.Id != construction.Id))
        {
            if (string.IsNullOrEmpty(node.Owner)
                || !construction.Nodes.Any(p => p.Id == node.Owner))
            {
                issues.Add($"Orphan node '{node.Id}': owner missing or not part of the construction.");
            }
        }

        foreach (var edge in construction.Edges)
        {
            if (!construction.Nodes.Any(n => n.Id == edge.ParentId))
            {
                issues.Add($"Edge role '{edge.Role}' points at missing parent '{edge.ParentId}'.");
                continue;
            }

            var resolution = FspmReferenceResolver.ValidateReference(edge.Reference, model);
            var binding = new Model.FspmReferenceBinding(
                Reference: edge.Reference,
                Status: resolution.Status,
                IsValid: resolution.Status == Model.FspmReferenceStatus.Valid,
                Reason: resolution.Reason,
                TargetIdentity: resolution.TargetIdentity?.LogicalId ?? string.Empty,
                TargetFingerprint: resolution.TargetFingerprint,
                TargetKind: resolution.TargetKind,
                Owner: resolution.Owner);
            bindings.Add(binding);

            if (!binding.IsValid)
            {
                issues.Add($"Edge '{edge.Role}' invalid: {resolution.Reason}");
            }

            // Entity references are ownerless by contract: pinning an
            // OwnerId on one is a contract violation, not a lookup.
            if (edge.Reference is Model.FspmEntityRef entityRef
                && !string.IsNullOrEmpty(entityRef.OwnerId))
            {
                issues.Add($"EntityRef '{entityRef.DisplayName}' must not carry OwnerId.");
            }
        }

        return new Model.FspmConstructionValidation(
            IsValid: issues.Count == 0,
            Bindings: bindings.ToArray(),
            Issues: issues.ToArray());
    }

    public static Model.FspmConstruction Freeze(
        Model.FspmConstruction construction,
        Model.FspmConstructionValidation validation)
    {
        ArgumentNullException.ThrowIfNull(construction);
        ArgumentNullException.ThrowIfNull(validation);

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "Cannot Freeze an invalid construction: " + string.Join("; ", validation.Issues));
        }

        if (construction.State == Model.FspmConstructionState.Frozen)
        {
            return construction;
        }

        var orderedEdges = construction.Edges
            .OrderBy(e => e.ParentId, StringComparer.Ordinal)
            .ThenBy(e => e.Role, StringComparer.Ordinal)
            .ThenBy(e => e.TargetIdentity, StringComparer.Ordinal)
            .ToArray();
        var orderedNodes = construction.Nodes
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .ToArray();

        var canonical = string.Join("\n", orderedNodes.Select(n =>
            $"N|{n.Id}|{n.Kind}|{n.Name}|{n.Owner}"))
            + "\n" + string.Join("\n", orderedEdges.Select(e =>
                $"E|{e.ParentId}|{e.Role}|{e.TargetIdentity}|{e.TargetFingerprint}"));
        var fingerprint = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(canonical)));

        return construction with
        {
            Nodes = orderedNodes.Select(n => n with { State = Model.FspmConstructionState.Frozen }).ToArray(),
            Edges = orderedEdges,
            State = Model.FspmConstructionState.Frozen,
            Fingerprint = fingerprint,
        };
    }
}
