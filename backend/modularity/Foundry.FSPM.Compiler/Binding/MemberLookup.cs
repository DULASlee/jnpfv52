using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Binding;

/// <summary>
/// Hierarchy-aware member lookup shared by Property/Operation binders.
///
/// <para>Proven Roslyn behavior (Phase 8 probe, NOT assumed):
/// the <c>GetMembers(name)</c> API returns DECLARED members only —
/// inherited members are invisible. So <c>property Derived.Name</c> where
/// <c>Name</c> lives on the base would wrongly report NOT_FOUND. Per
/// 施工包 §57 the honest v1 rule is: walk the <c>BaseType</c> chain and
/// deterministically return the inherited member (never NOT_FOUND for a
/// member that really exists).</para>
///
/// <para>Override collapsing (also probed, NOT assumed): an <c>override</c>'s
/// <c>OriginalDefinition</c> is ITSELF, not the base virtual — so collapsing
/// must follow <c>OverriddenMethod</c> / <c>OverriddenProperty</c> chains. The
/// walk is derived-first: a base candidate is skipped when an already
/// collected symbol transitively overrides it, leaving the most-derived
/// implementation as the single candidate. <c>new</c>-shadowing has no
/// override link, so shadowed duplicates stay distinct → Ambiguous.</para>
///
/// <para>No special cases: the full chain (including <c>System.Object</c>)
/// is walked. <c>operation Session.ToString</c> truthfully binds
/// <c>object.ToString</c>.</para>
/// </summary>
internal static class MemberLookup
{
    /// <summary>
    /// All members named <paramref name="name"/> visible on
    /// <paramref name="owner"/> or its bases, derived-first, with overrides
    /// collapsed to the most-derived implementation.
    /// </summary>
    internal static IReadOnlyList<TSymbol> FindInHierarchy<TSymbol>(
        INamedTypeSymbol owner,
        string name)
        where TSymbol : ISymbol
    {
        var collected = new List<TSymbol>();

        for (var current = owner; current is not null; current = current.BaseType)
        {
            foreach (var candidate in current.GetMembers(name).OfType<TSymbol>())
            {
                if (!IsOverriddenByAny(collected, candidate))
                {
                    collected.Add(candidate);
                }
            }
        }

        return collected;
    }

    private static bool IsOverriddenByAny<TSymbol>(List<TSymbol> collected, TSymbol candidate)
        where TSymbol : ISymbol
    {
        foreach (var existing in collected)
        {
            if (OverridesTransitively(existing, candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool OverridesTransitively(ISymbol derived, ISymbol candidate)
    {
        if (derived is IMethodSymbol method && candidate is IMethodSymbol baseMethod)
        {
            for (var o = method.OverriddenMethod; o is not null; o = o.OverriddenMethod)
            {
                if (SymbolEqualityComparer.Default.Equals(o, baseMethod))
                {
                    return true;
                }
            }

            return false;
        }

        if (derived is IPropertySymbol property && candidate is IPropertySymbol baseProperty)
        {
            for (var o = property.OverriddenProperty; o is not null; o = o.OverriddenProperty)
            {
                if (SymbolEqualityComparer.Default.Equals(o, baseProperty))
                {
                    return true;
                }
            }

            return false;
        }

        return false;
    }
}
