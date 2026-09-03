using Foundry.FSPM.Compiler.Binding;
using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.Syntax;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 9 — the only place that orchestrates the Binders' results into an
/// <see cref="FspmSemanticModel"/>. NEVER re-implements binding logic and
/// NEVER recomputes symbol identity: each model's element is built strictly
/// from the corresponding <see cref="FspmBindingResult"/>'s already-validated
/// <c>SymbolId</c>.
///
/// <para>Order (load-bearing for owner lookup):</para>
/// <list type="number">
/// <item>Bind every <c>entity</c> first — properties/operations resolve their
/// owner from this set.</item>
/// <item>Bind every <c>property</c>, looking up the owner by declaration name.</item>
/// <item>Bind every <c>operation</c>, same way.</item>
/// </list>
///
/// <para>Parse diagnostics (FSPM001-004) flow through and merge with binder
/// diagnostics into the final <see cref="FspmSemanticModel.Diagnostics"/>.</para>
/// </summary>
public sealed class FspmSemanticBuilder
{
    /// <summary>
    /// Build a model from an already-parsed compilation unit plus a real
    /// Roslyn <paramref name="compilation"/> for the binders to look at.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance entry point reserved for future DI/options; Phase 9 keeps construction simple.")]
    public FspmSemanticModel Build(
        FspmCompilationUnitSyntax unit,
        Microsoft.CodeAnalysis.Compilation compilation,
        IReadOnlyList<FspmDiagnostic> parseDiagnostics)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(parseDiagnostics);

        var allDiagnostics = new List<FspmDiagnostic>(parseDiagnostics);

        var entities = new List<FspmEntity>();
        var properties = new List<FspmProperty>();
        var operations = new List<FspmOperation>();

        // 1) Entities first.
        foreach (var decl in unit.Declarations.OfType<FspmEntityDeclarationSyntax>())
        {
            var result = EntityBinder.Bind(decl, compilation);
            allDiagnostics.AddRange(result.Diagnostics);
            entities.Add(new FspmEntity
            {
                SymbolId = ResolveId(result, "Entity", $"{decl.Name}@{decl.Line}:{decl.Column}"),
                Symbol = result.Symbol as Microsoft.CodeAnalysis.INamedTypeSymbol,
                Binding = result,
            });
        }

        // 2) Properties: owner = first matching entity (by declaration text).
        foreach (var decl in unit.Declarations.OfType<FspmPropertyDeclarationSyntax>())
        {
            var result = PropertyBinder.Bind(decl, compilation);
            allDiagnostics.AddRange(result.Diagnostics);
            var owner = entities.FirstOrDefault(e =>
                string.Equals(e.Name, decl.EntityName, StringComparison.Ordinal)
                && e.IsResolved);
            properties.Add(new FspmProperty
            {
                SymbolId = ResolveId(result, "Property", $"{decl.EntityName}.{decl.PropertyName}@{decl.Line}:{decl.Column}"),
                Symbol = result.Symbol as Microsoft.CodeAnalysis.IPropertySymbol,
                Binding = result,
                Owner = owner,
            });
        }

        // 3) Operations: owner = first matching entity (by declaration text).
        foreach (var decl in unit.Declarations.OfType<FspmOperationDeclarationSyntax>())
        {
            var result = OperationBinder.Bind(decl, compilation);
            allDiagnostics.AddRange(result.Diagnostics);
            var owner = entities.FirstOrDefault(e =>
                string.Equals(e.Name, decl.EntityName, StringComparison.Ordinal)
                && e.IsResolved);
            operations.Add(new FspmOperation
            {
                SymbolId = ResolveId(result, "Operation", $"{decl.EntityName}.{decl.OperationName}@{decl.Line}:{decl.Column}"),
                Symbol = result.Symbol as Microsoft.CodeAnalysis.IMethodSymbol,
                Binding = result,
                Owner = owner,
            });
        }

        return new FspmSemanticModel(entities, properties, operations, allDiagnostics);
    }

    /// <summary>
    /// Success → reuse the binder's real <see cref="FspmSymbolId"/> verbatim
    /// (directive §十). Failure → mint a synthetic, fully-qualified,
    /// builder-scoped id that cannot collide with any real Roslyn id (which
    /// always uses the <c>Assembly|DocId</c> shape from
    /// <see cref="FspmSymbolIdentity"/>; the <c>synthetic/</c> prefix keeps
    /// the namespace disjoint). The synthetic id is only ever used as a
    /// unique key into the model — never to look up a real symbol.
    /// </summary>
    private static FspmSymbolId ResolveId(
        FspmBindingResult result,
        string kind,
        string declarationTag)
    {
        if (result.SymbolId is { } real)
        {
            return real;
        }

        return new FspmSymbolId($"synthetic/{result.Status}/{kind}/{declarationTag}");
    }
}
