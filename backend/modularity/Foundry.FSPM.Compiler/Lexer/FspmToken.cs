namespace Foundry.FSPM.Compiler.Lexer;

/// <summary>
/// Immutable token produced by <see cref="FspmLexer"/>.
/// Carries full source-location info so Parser / Diagnostic / Semantic Binder
/// can always reach back to the exact source slice.
/// </summary>
public sealed record FspmToken(
    FspmTokenKind Kind,
    string Text,
    int Start,
    int Length,
    int Line,
    int Column);
