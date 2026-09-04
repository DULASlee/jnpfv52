namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P14-01 Post-Freeze Correctness Sweep — FIX-OWNER-02 / FIX-OWNER-04.
///
/// <para>
/// The owner key of a <see cref="NativeSemanticFact"/> with
/// <see cref="NativeSymbolKind.Type"/> is the triple
/// <c>(Assembly, Namespace, MetadataName)</c>. A member fact looks up
/// its owner by joining <c>fact.Logical.ContainingTypeName</c> against
/// the Type fact's <c>MetadataName</c> — see
/// <see cref="NativeSemanticFact.Logical"/> for why
/// <c>ContainingTypeName</c> is already <see cref="INamedTypeSymbol.MetadataName"/>
/// for members.
/// </para>
///
/// <para>
/// Previously this key was assembled inline inside
/// <see cref="Model.FspmModelBinder"/> as
/// <c>(fact.Logical.AssemblyName, fact.Logical.Namespace, fact.Logical.MemberName)</c>,
/// which silently over-loaded <c>MemberName</c> to mean the type's
/// <see cref="INamedTypeSymbol.MetadataName"/>. That worked only by
/// happy accident of the minting rule and would break the day a new
/// <see cref="NativeSymbolKind"/> was added without a parallel
/// minting branch. Centralising the key + the type-name accessor
/// makes the dependency an executable contract: the
/// <see cref="Model.FspmModelBinder"/> only depends on this file, the
/// invariant <c>TypeFact.ContainingTypeName == string.Empty</c> is
/// checkable, and any future deviation is one test away from
/// failing the build.
/// </para>
///
/// <para>
/// This file is part of the post-freeze correctness sweep; it does
/// NOT modify the <see cref="NativeSemanticFact"/> record, the
/// P13 eight-state contract, the snapshot, the resolver, the
/// identity, or the public model. It is a pure value-type helper
/// plus a small set of static accessors.
/// </para>
/// </summary>
public readonly record struct TypeFactOwnerKey(
    string AssemblyName,
    string Namespace,
    string MetadataName)
{
    public bool IsValid =>
        !string.IsNullOrEmpty(AssemblyName)
        && !string.IsNullOrEmpty(MetadataName);

    public override string ToString() =>
        $"{AssemblyName}|{Namespace}|{MetadataName}";
}

public static class TypeFactKeys
{
    public const string TypeContainingTypeNameMustBeEmpty =
        "TypeFact must carry an empty ContainingTypeName; " +
        "MintLogicalIdentity is the only legitimate source of NativeSemanticFact " +
        "and it nulls ContainingTypeName for INamedTypeSymbol.";

    public static bool IsTypeFact(NativeSemanticFact fact) =>
        fact.Kind == NativeSymbolKind.Type;

    public static bool TryGetOwnerKey(
        NativeSemanticFact fact,
        out TypeFactOwnerKey key,
        out string? rejectionReason)
    {
        rejectionReason = null;

        if (!IsTypeFact(fact))
        {
            key = default;
            rejectionReason = "Not a Type fact.";
            return false;
        }

        if (fact.Logical.AssemblyName == "<unknown>")
        {
            key = default;
            rejectionReason = "Type fact has no containing assembly.";
            return false;
        }

        if (!string.Equals(fact.Logical.ContainingTypeName, string.Empty, StringComparison.Ordinal))
        {
            key = default;
            rejectionReason = TypeContainingTypeNameMustBeEmpty;
            return false;
        }

        key = new TypeFactOwnerKey(
            AssemblyName: fact.Logical.AssemblyName,
            Namespace: fact.Logical.Namespace,
            MetadataName: fact.Logical.MemberName);
        return key.IsValid;
    }

    public static TypeFactOwnerKey KeyForMember(NativeSemanticFact memberFact) =>
        new(
            AssemblyName: memberFact.Logical.AssemblyName,
            Namespace: memberFact.Logical.Namespace,
            MetadataName: memberFact.Logical.ContainingTypeName);
}
