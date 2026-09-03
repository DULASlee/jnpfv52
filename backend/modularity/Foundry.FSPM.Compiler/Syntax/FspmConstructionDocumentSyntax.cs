namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// Root AST node for the Phase 12 Construction language:
/// document := page*  (each page holds forms, each form holds bindings).
/// L1 flat declarations (entity/property/operation) stay in
/// <see cref="FspmCompilationUnitSyntax"/>; this document is the L2 layer.
/// </summary>
public sealed record FspmConstructionDocumentSyntax(
    IReadOnlyList<FspmPageSyntax> Pages,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
