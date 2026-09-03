namespace Foundry.FSPM.Compiler.Syntax;

/// <summary>
/// Opaque native-language expression slice (Phase 12 — P12-04 frozen boundary).
/// Carries the RAW source text (e.g. "User.PhoneNumber", "User.Create(user)").
/// The Parser MUST NOT split on '.' or interpret receiver/member/arguments:
/// C# semantics belong to Roslyn (P13). P12 only guarantees the text is
/// captured verbatim with its source span.
/// </summary>
public sealed record FspmNativeExpressionSyntax(
    string Text,
    int Start,
    int Length,
    int Line,
    int Column)
    : FspmSyntaxNode(Start, Length, Line, Column);
