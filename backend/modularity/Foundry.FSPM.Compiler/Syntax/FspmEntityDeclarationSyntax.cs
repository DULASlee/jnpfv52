namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// AST node for: entity Name
/// Position fields point at the keyword entity (Start..Length), so the
/// caller can highlight the entire declaration or just the identifier portion.
/// Phase 3 硬门禁: this node has NO Symbol / Binding fields. The Name is the
/// raw source text; resolution to a real C# INamedTypeSymbol happens in
/// Phase 8 Entity Binder.
/// </summary>
public sealed record FspmEntityDeclarationSyntax(
    string Name,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
