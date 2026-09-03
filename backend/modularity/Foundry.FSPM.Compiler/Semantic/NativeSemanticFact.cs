using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13 public contract (chief §五/§六 frozen): the ONLY Native Semantic
/// output P14 may consume. Composes every Hardening fact; carries ZERO
/// Roslyn runtime objects (no ISymbol/SemanticModel/SyntaxNode/
/// Compilation/Workspace) — enforced by FactIsolationTests, which compile
/// a client against this assembly WITHOUT any Roslyn references.
/// </summary>
public sealed record NativeSemanticFact(
    FspmSymbolId Identity,
    LogicalSemanticIdentity Logical,
    SemanticFingerprint Fingerprint,
    NativeSymbolKind Kind,
    NativeTypeKind TypeKind,
    string Name,
    string QualifiedName,
    NativeVisibilityFacts Visibility,
    NativeTypeShape? TypeShape,
    NativeOperationIdentity? Operation,
    NativeTypeRelationships? Relationships,
    CompilationIdentity Compilation,
    AssemblyIdentity Assembly,
    SemanticSourceAnchor Anchor,
    FspmResolutionStatus Status,
    SemanticQuality Quality,
    IReadOnlyList<NativeDiagnostic> Diagnostics);

/// <summary>
/// P13 factory: builds a <see cref="NativeSemanticFact"/> from one real
/// Roslyn symbol plus its compilation context. The ISymbol never escapes:
/// every field is projected to plain data before the Fact is returned.
/// </summary>
public static class NativeSemanticFactFactory
{
    public static NativeSemanticFact Create(
        ISymbol symbol,
        CompilationIdentity compilation,
        SemanticSourceAnchor anchor,
        FspmResolutionStatus status = FspmResolutionStatus.Resolved,
        SemanticQuality quality = SemanticQuality.Perfect,
        IReadOnlyList<NativeDiagnostic>? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(anchor);

        var classification = SymbolClassifier.Classify(symbol);
        var assemblySymbol = symbol.ContainingAssembly;

        NativeTypeShape? shape = symbol switch
        {
            ITypeSymbol typeSymbol => TypeShapeExtractor.ExtractTypeShape(typeSymbol),
            IPropertySymbol property => TypeShapeExtractor.ExtractTypeShape(property.Type),
            IFieldSymbol field => TypeShapeExtractor.ExtractTypeShape(field.Type),
            _ => null,
        };

        NativeOperationIdentity? operation = symbol is IMethodSymbol method
            ? MethodSignatureExtractor.ExtractOperationIdentity(method)
            : null;

        NativeTypeRelationships? relationships = symbol switch
        {
            INamedTypeSymbol type => TypeRelationshipExtractor.ExtractTypeRelationships(type),
            IMethodSymbol m => TypeRelationshipExtractor.ExtractMethodRelationships(m),
            IPropertySymbol p => TypeRelationshipExtractor.ExtractPropertyRelationships(p),
            _ => null,
        };

        var assemblyName = assemblySymbol?.Name ?? "<unknown>";
        var assemblyIdentity = assemblySymbol is null
            ? new AssemblyIdentity(assemblyName, string.Empty, string.Empty, string.Empty, "Unknown")
            : new AssemblyIdentity(
                assemblyName,
                assemblySymbol.Identity.Version.ToString(),
                assemblySymbol.Identity.CultureName,
                assemblySymbol.Identity.PublicKeyToken.IsDefaultOrEmpty
                    ? string.Empty
                    : Convert.ToHexString(assemblySymbol.Identity.PublicKeyToken.ToArray()),
                string.Equals(assemblyName, compilation.AssemblyName, StringComparison.Ordinal)
                    ? "SourceProject"
                    : "ReferencedAssembly");

        // Type/Property/Field/Event/Method have first-class identity
        // factories. Anything else (Namespace/Parameter/…) is NOT a
        // semantic node P14 can consume → the caller reports
        // FspmResolutionStatus.Unsupported; only truly unknown kinds
        // throw here.
        FspmSymbolId identity = symbol switch
        {
            INamedTypeSymbol t => FspmSymbolIdentity.Create(t),
            IPropertySymbol p => FspmSymbolIdentity.Create(p),
            IFieldSymbol f => FspmSymbolIdentity.Create(f),
            IEventSymbol e => FspmSymbolIdentity.Create(e),
            IMethodSymbol m => FspmSymbolIdentity.Create(m),
            _ => throw new InvalidOperationException(
                $"NativeSemanticFact supports Type/Property/Field/Event/Method symbols, got {symbol.Kind}."),
        };

        return new NativeSemanticFact(
            Identity: identity,
            Logical: SemanticIdentityMint.MintLogicalIdentity(symbol),
            Fingerprint: SemanticIdentityMint.MintFingerprint(symbol),
            Kind: classification.Kind,
            TypeKind: classification.TypeKind,
            Name: symbol.Name,
            QualifiedName: symbol.ToDisplayString(),
            Visibility: classification.Visibility,
            TypeShape: shape,
            Operation: operation,
            Relationships: relationships,
            Compilation: compilation,
            Assembly: assemblyIdentity,
            Anchor: anchor,
            Status: status,
            Quality: quality,
            Diagnostics: diagnostics ?? Array.Empty<NativeDiagnostic>());
    }
}
