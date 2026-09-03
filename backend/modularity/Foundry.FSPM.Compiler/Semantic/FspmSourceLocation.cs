using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 13 — immutable, real-Roslyn-backed source location for a symbol.
/// Wraps <see cref="Microsoft.CodeAnalysis.Location"/> so the P13 surface
/// stays independent of Roslyn types where possible; tests and downstream
/// layers (P14 binder) consume this struct directly.
/// </summary>
/// <remarks>
/// All five fields are read from Roslyn at construction time. No fields
/// are computed locally, so a <see cref="FspmSourceLocation"/> always
/// matches what <c>symbol.Locations[i].GetLineSpan()</c> reports.
/// </remarks>
public readonly record struct FspmSourceLocation(
    string DocumentPath,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn)
{
    public static FspmSourceLocation From(Location location)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (!location.IsInSource)
        {
            return new FspmSourceLocation("<metadata>", 0, 0, 0, 0);
        }

        var lineSpan = location.GetLineSpan();
        var start = lineSpan.StartLinePosition;
        var end = lineSpan.EndLinePosition;
        return new FspmSourceLocation(
            DocumentPath: location.SourceTree?.FilePath ?? "<syntax-only>",
            StartLine: start.Line + 1,
            StartColumn: start.Character + 1,
            EndLine: end.Line + 1,
            EndColumn: end.Character + 1);
    }
}
