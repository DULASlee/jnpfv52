namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// Root AST node produced by the future Parser (Phase 4).
/// Phase 3 only constructs it directly in tests - no Parser yet.
/// </summary>
public sealed record FspmCompilationUnitSyntax(
    IReadOnlyList<FspmSyntaxNode> Declarations,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
