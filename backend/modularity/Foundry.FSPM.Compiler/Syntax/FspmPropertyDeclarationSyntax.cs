namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// AST node for: property EntityName.PropertyName
/// Both EntityName and PropertyName are raw source text. Resolution to a real
/// C# IPropertySymbol happens in Phase 8 Property Binder.
/// Position fields point at the property keyword (entire declaration).
/// </summary>
public sealed record FspmPropertyDeclarationSyntax(
    string EntityName,
    string PropertyName,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
