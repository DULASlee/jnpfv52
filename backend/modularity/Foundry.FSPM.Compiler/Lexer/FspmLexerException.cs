using System;

namespace Foundry.FSPM.Compiler.Lexer;

/// <summary>
/// Thrown by <see cref="FspmLexer"/> when the source contains a character
/// that is not part of the Phase 2 FSPM grammar. Diagnostics pipeline
/// will catch this exception at the FSPM entry point and turn it into a
/// real <c>FspmDiagnostic</c> with source location.
/// </summary>
public sealed class FspmLexerException : Exception
{
    public FspmLexerException(string message) : base(message) { }
}
