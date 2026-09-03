namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// AST node for: operation EntityName.OperationName
/// Both EntityName and OperationName are raw source text. Resolution to a real
/// C# IMethodSymbol happens in Phase 8 Operation Binder.
/// Position fields point at the operation keyword (entire declaration).
/// </summary>
public sealed record FspmOperationDeclarationSyntax(
    string EntityName,
    string OperationName,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
