namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// AST node for: entity &lt;nativeExpression&gt;  (L2 form binding)
/// References an L1 entity semantic resource; existence is NOT checked here.
/// </summary>
public sealed record FspmEntityBindingSyntax(
    FspmNativeExpressionSyntax Expression,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
