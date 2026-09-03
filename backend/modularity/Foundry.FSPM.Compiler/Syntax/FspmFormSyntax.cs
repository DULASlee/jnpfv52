namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// AST node for: form Name { binding* }  (L2 semantic composite).
/// </summary>
public sealed record FspmFormSyntax(
    string Name,
    IReadOnlyList<FspmSyntaxNode> Bindings,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
