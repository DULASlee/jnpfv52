using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 13 — Roslyn-backed symbol resolver. Takes a
/// <see cref="FspmCompilationSnapshot"/> (a single Compilation scope)
/// and answers Type / Property / Method / Expression queries with the
/// eight-state <see cref="FspmResolutionResult"/> (H7).
///
/// <para><b>Architecture rule (施工包 §4 / §16 / §38):</b>
/// Resolver performs NO manual C# parsing, NO <c>Split(".")</c>, NO
/// name-based heuristics. Every resolution goes through Roslyn APIs:
/// <c>compilation.GetTypeByMetadataName</c>,
/// <c>compilation.GetSymbolsWithName</c>,
/// <c>SemanticModel.GetSymbolInfo</c>. The Resolver ONLY orchestrates
/// these calls and packs the result into a four-state record.
/// </para>
/// </summary>
public sealed class CSharpResolver
{
    // Pinned (not Latest): host-parse behavior must not drift with SDK
    // upgrades. Roslyn 4.8 supports up to C# 12.
    private static readonly LanguageVersion HostLanguageVersion = LanguageVersion.CSharp12;

    public CSharpResolver(FspmCompilationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Snapshot = snapshot;
    }

    public FspmCompilationSnapshot Snapshot { get; }

    public Compilation Compilation => Snapshot.Compilation;

    // ===== Type =====

    /// <summary>Resolve a type by metadata name (e.g. "SemanticGolden.Domain.User").</summary>
    public FspmResolutionResult ResolveType(string metadataName)
    {
        if (string.IsNullOrWhiteSpace(metadataName))
        {
            return FspmResolutionResult.InvalidResult("Type name is empty.", null);
        }

        var byMetadata = Compilation.GetTypeByMetadataName(metadataName);
        if (byMetadata is not null)
        {
            return ResolvedRecord(byMetadata, "MetadataName exact match");
        }

        // Fallback: short name in the global namespace, but only if unambiguous.
        var byShort = Compilation.GetSymbolsWithName(
            n => string.Equals(n, metadataName, StringComparison.Ordinal),
            SymbolFilter.Type).OfType<INamedTypeSymbol>().ToArray();

        if (byShort.Length == 0)
        {
            return FspmResolutionResult.NotFoundResult(
                $"No type named '{metadataName}' in compilation '{Compilation.AssemblyName}'.",
                null);
        }

        if (byShort.Length == 1)
        {
            return ResolvedRecord(byShort[0], "Short name unique match");
        }

        return FspmResolutionResult.AmbiguousResult(
            byShort.Select(BuildRecord).ToArray(),
            $"Type short name '{metadataName}' matches {byShort.Length} types in scope.",
            null);
    }

    // ===== Property =====

    /// <summary>
    /// Resolve a property of the form <c>User.PhoneNumber</c>. The receiver
    /// type is resolved first via <see cref="ResolveType"/>; the property
    /// name is then resolved through Roslyn's own member enumeration.
    /// </summary>
    public FspmResolutionResult ResolveProperty(string containingTypeMetadataName, string propertyName)
    {
        var typeResult = ResolveType(containingTypeMetadataName);
        if (typeResult.Status != FspmResolutionStatus.Resolved || typeResult.Selected is null)
        {
            return FspmResolutionResult.NotFoundResult(
                $"Containing type '{containingTypeMetadataName}' could not be resolved ({typeResult.Status}).",
                null);
        }

        var type = (INamedTypeSymbol)typeResult.Selected.Symbol;
        var property = type.GetMembers(propertyName).OfType<IPropertySymbol>().ToArray();

        if (property.Length == 0)
        {
            return FspmResolutionResult.NotFoundResult(
                $"Type '{type.ToDisplayString()}' has no property named '{propertyName}'.",
                FspmSourceLocation.From(type.Locations.FirstOrDefault() ?? Location.None));
        }

        if (property.Length == 1)
        {
            return ResolvedRecord(property[0], "Property single match");
        }

        return FspmResolutionResult.AmbiguousResult(
            property.Select(BuildRecord).ToArray(),
            $"Property name '{propertyName}' resolves to {property.Length} symbols on '{type.ToDisplayString()}' (C# indexer overload?).",
            null);
    }

    // ===== Method =====

    /// <summary>
    /// Resolve a method by simple name on a containing type. Overloads
    /// are ALWAYS returned as Ambiguous — caller must disambiguate by
    /// parameter list / arity (resolver intentionally does NOT auto-pick).
    /// </summary>
    public FspmResolutionResult ResolveMethod(string containingTypeMetadataName, string methodName)
    {
        var typeResult = ResolveType(containingTypeMetadataName);
        if (typeResult.Status != FspmResolutionStatus.Resolved || typeResult.Selected is null)
        {
            return FspmResolutionResult.NotFoundResult(
                $"Containing type '{containingTypeMetadataName}' could not be resolved ({typeResult.Status}).",
                null);
        }

        var type = (INamedTypeSymbol)typeResult.Selected.Symbol;
        var methods = type.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .ToArray();

        if (methods.Length == 0)
        {
            return FspmResolutionResult.NotFoundResult(
                $"Type '{type.ToDisplayString()}' has no ordinary method named '{methodName}'.",
                null);
        }

        if (methods.Length == 1)
        {
            return ResolvedRecord(methods[0], "Method single match");
        }

        return FspmResolutionResult.AmbiguousResult(
            methods.Select(BuildRecord).ToArray(),
            $"Method name '{methodName}' has {methods.Length} overloads on '{type.ToDisplayString()}'. Caller must disambiguate.",
            null);
    }

    /// <summary>
    /// Resolve a method overload by parameter type list. Returns Resolved
    /// only if the parameter types match exactly; otherwise NotFound or
    /// Ambiguous (rare; only if a name+arity also matches multiple).
    /// </summary>
    public FspmResolutionResult ResolveMethodBySignature(
        string containingTypeMetadataName, string methodName, IReadOnlyList<string> parameterTypeDisplayStrings)
    {
        var typeResult = ResolveType(containingTypeMetadataName);
        if (typeResult.Status != FspmResolutionStatus.Resolved || typeResult.Selected is null)
        {
            return FspmResolutionResult.NotFoundResult(
                $"Containing type '{containingTypeMetadataName}' could not be resolved ({typeResult.Status}).",
                null);
        }

        var type = (INamedTypeSymbol)typeResult.Selected.Symbol;
        var matches = type.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .Where(m => m.Parameters.Length == parameterTypeDisplayStrings.Count)
            .Where(m => ParametersMatch(m, parameterTypeDisplayStrings))
            .ToArray();

        if (matches.Length == 0)
        {
            return FspmResolutionResult.NotFoundResult(
                $"No overload of '{type.ToDisplayString()}.{methodName}' matches the given parameter list.",
                null);
        }

        if (matches.Length == 1)
        {
            return ResolvedRecord(matches[0], "Method signature exact match");
        }

        return FspmResolutionResult.AmbiguousResult(
            matches.Select(BuildRecord).ToArray(),
            $"Method signature matches {matches.Length} candidates (ref/out differences?)",
            null);
    }

    // ===== Expression =====

    /// <summary>
    /// Resolve a C# expression captured verbatim by P12
    /// <see cref="Foundry.FSPM.Compiler.Syntax.FspmNativeExpressionSyntax"/>.
    /// The text is parsed by Roslyn (host language pinned to C# 12) and
    /// resolved through <see cref="SemanticModel.GetSymbolInfo"/>. P13
    /// performs NO manual parsing; this is the ONLY P12→P13 boundary.
    /// </summary>
    public FspmResolutionResult ResolveExpression(string expressionText)
    {
        if (string.IsNullOrWhiteSpace(expressionText))
        {
            return FspmResolutionResult.InvalidResult("Expression text is empty.", null);
        }

        // Wrap the verbatim expression in a small "using"-aware host
        // so identifiers like "User" can resolve against the
        // snapshot's real types. P12 captured the expression verbatim
        // (NativeExpressionNode.Text) — the wrapper is plumbing only
        // and the resolver later asks Roslyn for the SymbolInfo on
        // the inner expression node.
        var usingClauses = Snapshot.PrimaryProject
            .Documents
            .Select(d => d.GetSyntaxTreeAsync().GetAwaiter().GetResult())
            .Where(t => t is not null)
            .Cast<SyntaxTree>()
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax>())
            .Select(u => u.ToFullString())
            .Distinct(StringComparer.Ordinal);

        // Also derive a using for every namespace declared in the
        // snapshot (Roslyn only looks at `using` directives for global
        // imports, not at `namespace` declarations).
        var declaredNamespaces = Snapshot.PrimaryProject
            .Documents
            .Select(d => d.GetSyntaxTreeAsync().GetAwaiter().GetResult())
            .Where(t => t is not null)
            .Cast<SyntaxTree>()
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax>())
            .Select(n => n.Name.ToString())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.Ordinal)
            .Select(s => "using " + s + ";\n");

        var header = string.Concat(usingClauses) + string.Concat(declaredNamespaces);
        var host = header + "static class __FspmHost { public static object? __V => " + expressionText + "; }";

        SyntaxTree tree;
        try
        {
            tree = CSharpSyntaxTree.ParseText(
                text: host,
                options: new CSharpParseOptions(HostLanguageVersion));
        }
        catch (Exception ex)
        {
            return FspmResolutionResult.InvalidResult(
                $"Roslyn failed to parse expression: {ex.Message}",
                null);
        }

        var root = tree.GetRoot();
        var expr = root.DescendantNodesAndSelf()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ArrowExpressionClauseSyntax>()
            .FirstOrDefault()?.Expression;
        if (expr is null)
        {
            return FspmResolutionResult.InvalidResult(
                "Parsed host has no expression body (not a value expression).",
                null);
        }

        var parseDiagnostics = tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();
        if (parseDiagnostics.Length > 0)
        {
            return FspmResolutionResult.InvalidResult(
                "C# parse errors: " + string.Join("; ", parseDiagnostics.Select(d => d.GetMessage())),
                FspmSourceLocation.From(parseDiagnostics[0].Location));
        }

        // The snapshot assembly is emitted ONCE per Compilation and
        // cached (ConditionalWeakTable: lifetime follows the compilation,
        // no unbounded growth). A per-call full emit would turn every
        // expression query into a seconds-scale operation on real-size
        // snapshots.
        var snapshotReference = SnapshotReferenceCache.GetOrEmit(Compilation);
        if (snapshotReference is null)
        {
            return FspmResolutionResult.InvalidResult(
                "Failed to materialize snapshot for expression resolution.",
                null);
        }

        // mscorlib + System.Runtime: always present in the snapshot
        // references (transitive CoreLib/Standard), but we union with
        // the snapshot's own references so extension methods from
        // System.* still resolve.
        var references = new List<MetadataReference> { snapshotReference };
        foreach (var r in Compilation.References)
        {
            if (!references.Contains(r))
            {
                references.Add(r);
            }
        }

        var adhocCompilation = CSharpCompilation.Create(
            assemblyName: Compilation.AssemblyName + ".Adhoc",
            syntaxTrees: new[] { tree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var model = adhocCompilation.GetSemanticModel(tree);
        var symbolInfo = model.GetSymbolInfo(expr);

        var location = FspmSourceLocation.From(expr.GetLocation());
        if (symbolInfo.Symbol is not null)
        {
            return ResolvedRecord(symbolInfo.Symbol, "Expression symbol info", location);
        }

        var candidates = symbolInfo.CandidateSymbols;
        if (candidates.Length == 0)
        {
            return FspmResolutionResult.NotFoundResult(
                $"No symbol bound to expression '{expressionText}'.",
                location);
        }

        if (candidates.Length == 1
            && TryResolveMemberReference(model, expr, candidates[0], out var memberReason))
        {
            // `Type.Member` where Member is an instance member is not a
            // valid C# *value* expression, so Roslyn leaves Symbol null —
            // but the single candidate IS the referenced member when its
            // receiver binds to the candidate's own containing type.
            // Decided on Roslyn syntax+semantic facts only (no Split(".")).
            return ResolvedRecord(candidates[0], memberReason, location);
        }

        if (candidates.Length == 1)
        {
            // Single candidate that is NOT a verifiable member reference:
            // binding failed and we refuse to promote it to Resolved.
            return FspmResolutionResult.AmbiguousResult(
                candidates.Select(BuildRecord).ToArray(),
                $"Expression '{expressionText}' failed to bind; single candidate kept for audit.",
                location);
        }

        return FspmResolutionResult.AmbiguousResult(
            candidates.Select(BuildRecord).ToArray(),
            $"Expression '{expressionText}' has {candidates.Length} candidate symbols.",
            location);
    }

    // Verifies `Type.Member` member references using Roslyn facts only:
    // the receiver sub-expression must bind to the candidate's own
    // containing type. Returns false for anything else (method groups
    // with 2+ overloads never reach here with Length == 1).
    private static bool TryResolveMemberReference(
        Microsoft.CodeAnalysis.SemanticModel model,
        ExpressionSyntax expr,
        ISymbol candidate,
        out string reason)
    {
        reason = string.Empty;

        if (expr is not MemberAccessExpressionSyntax access
            || candidate.ContainingType is null)
        {
            return false;
        }

        var receiver = model.GetSymbolInfo(access.Expression).Symbol;
        if (receiver is INamedTypeSymbol receiverType
            && SymbolEqualityComparer.Default.Equals(receiverType, candidate.ContainingType)
            && string.Equals(access.Name.Identifier.Text, candidate.Name, StringComparison.Ordinal))
        {
            reason = $"MemberReference (non-value context): '{candidate.Name}' " +
                $"referenced on containing type '{receiverType.ToDisplayString()}'.";
            return true;
        }

        return false;
    }

    // ===== Helpers =====

    private static FspmResolutionResult ResolvedRecord(ISymbol symbol, string reason, FspmSourceLocation? at = null)
    {
        var record = BuildRecord(symbol);
        return FspmResolutionResult.ResolvedResult(record, at ?? record.Location, reason);
    }

    private static FspmSourceLocation PrimaryLocation(ISymbol symbol) =>
        FspmSourceLocation.From(symbol.Locations.FirstOrDefault() ?? Location.None);

    private static FspmSymbolRecord BuildRecord(ISymbol symbol)
    {
        // Route to the type-specific factory overloads (Phase 7 contract).
        var id = symbol switch
        {
            INamedTypeSymbol t => FspmSymbolIdentity.Create(t),
            IPropertySymbol p => FspmSymbolIdentity.Create(p),
            IMethodSymbol m => FspmSymbolIdentity.Create(m),
            _ => throw new InvalidOperationException(
                $"Cannot mint FspmSymbolId for symbol kind '{symbol.Kind}'. P13 inventory only collects Type/Property/Method."),
        };

        return new FspmSymbolRecord(symbol, id, PrimaryLocation(symbol));
    }

    private static bool ParametersMatch(IMethodSymbol method, IReadOnlyList<string> parameterTypeDisplayStrings)
    {
        for (var i = 0; i < method.Parameters.Length; i++)
        {
            if (!string.Equals(method.Parameters[i].Type.ToDisplayString(),
                parameterTypeDisplayStrings[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
