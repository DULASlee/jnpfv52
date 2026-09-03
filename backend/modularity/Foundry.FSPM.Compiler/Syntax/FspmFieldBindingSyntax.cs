namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// AST node for: field &lt;nativeExpression&gt;  (L2 form binding)
/// References an L1 property semantic resource; existence is NOT checked here.
/// </summary>
public sealed record FspmFieldBindingSyntax(
    FspmNativeExpressionSyntax Expression,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
