namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// AST node for: page Name { form* }  (L2 semantic composite container).
/// </summary>
public sealed record FspmPageSyntax(
    string Name,
    IReadOnlyList<FspmFormSyntax> Forms,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
