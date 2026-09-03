using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.Syntax;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Binding;

/// <summary>
/// Phase 8.1 — binds an FSPM entity declaration to a REAL Roslyn type symbol.
///
/// <para>Matching (施工包 §37-§38, no string binding):</para>
/// <list type="bullet">
/// <item>Short name (<c>User</c>): every type in the compilation (source AND
/// referenced assemblies, nested namespaces and nested types included) whose
/// simple name equals the declaration, ordinal comparison.</item>
/// <item>Fully qualified (<c>SemanticGolden.Domain.User</c>, contains '.'):
/// types whose display-qualified name equals the declaration.</item>
/// </list>
///
/// <para>Count rule — the ONLY success condition is exactly one candidate;
/// zero → Unknown (FSPM101), more than one → Ambiguous (FSPM111).
/// Never First(), never priority hacks.</para>
/// </summary>
public static class EntityBinder
{
    /// <summary>
    /// Binds <paramref name="declaration"/> against a real <paramref name="compilation"/>.
    /// </summary>
    public static FspmBindingResult Bind(
        FspmEntityDeclarationSyntax declaration,
        Compilation compilation)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(compilation);

        var candidates = FindCandidates(compilation, declaration.Name);

        if (candidates.Count == 0)
        {
            return FspmBindingResult.Fail(
                FspmBindingStatus.Unknown,
                declaration,
                new[]
                {
                    new FspmDiagnostic(
                        FspmDiagnosticCodes.EntityNotFound,
                        FspmDiagnosticSeverity.Error,
                        $"Unknown entity '{declaration.Name}': no such type in compilation '{compilation.AssemblyName}'.",
                        declaration.Line,
                        declaration.Column),
                });
        }

        if (candidates.Count > 1)
        {
            var listed = string.Join(", ", candidates.Select(c => c.ToDisplayString()).OrderBy(n => n, StringComparer.Ordinal));
            return FspmBindingResult.Fail(
                FspmBindingStatus.Ambiguous,
                declaration,
                new[]
                {
                    new FspmDiagnostic(
                        FspmDiagnosticCodes.AmbiguousEntity,
                        FspmDiagnosticSeverity.Error,
                        $"Ambiguous entity '{declaration.Name}': {candidates.Count} candidates: {listed}.",
                        declaration.Line,
                        declaration.Column),
                });
        }

        var symbol = candidates[0];
        return FspmBindingResult.Success(declaration, symbol, FspmSymbolIdentity.Create(symbol));
    }

    /// <summary>
    /// Shared candidate enumeration reused by Property/Operation binders to
    /// resolve the owner entity FIRST (no member lookup before the owner is unique).
    /// Deterministic order: fully qualified name, ordinal.
    /// </summary>
    internal static IReadOnlyList<INamedTypeSymbol> FindCandidates(Compilation compilation, string name)
    {
        var qualified = name.Contains('.');
        var found = new List<INamedTypeSymbol>();
        CollectTypes(compilation.GlobalNamespace, found);

        var matched = qualified
            ? found.Where(t => string.Equals(t.ToDisplayString(), name, StringComparison.Ordinal))
            : found.Where(t => string.Equals(t.Name, name, StringComparison.Ordinal));

        return matched
            .OrderBy(t => t.ToDisplayString(), StringComparer.Ordinal)
            .ToArray();
    }

    private static void CollectTypes(INamespaceSymbol ns, List<INamedTypeSymbol> into)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            CollectNested(type, into);
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            CollectTypes(child, into);
        }
    }

    private static void CollectNested(INamedTypeSymbol type, List<INamedTypeSymbol> into)
    {
        into.Add(type);
        foreach (var nested in type.GetTypeMembers())
        {
            CollectNested(nested, into);
        }
    }
}
