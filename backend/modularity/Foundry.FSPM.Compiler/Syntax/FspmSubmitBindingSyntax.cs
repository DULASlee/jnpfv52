namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// AST node for: submit Name -&gt; &lt;nativeExpression&gt;  (L2 form binding)
/// References an L1 operation semantic resource; existence is NOT checked here.
/// </summary>
public sealed record FspmSubmitBindingSyntax(
    string Name,
    FspmNativeExpressionSyntax Target,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
