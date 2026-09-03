namespace Foundry.FSPM.Compiler.Symbols;

/// <summary>
/// Phase 7 — 施工包 §25.
/// The only three semantic declaration kinds FSPM v1 understands.
/// Maps 1:1 to Roslyn: Entity → INamedTypeSymbol, Property → IPropertySymbol,
/// Operation → IMethodSymbol.
/// </summary>
public enum FspmSymbolKind
{
    Entity,
    Property,
    Operation,
}
