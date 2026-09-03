using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H1: conditional-compilation context of a snapshot. Only facts
/// Roslyn truthfully exposes: preprocessor symbols (union of the
/// snapshot trees' parse options), optimization level, language version.
/// TargetFramework/Configuration are MSBuild properties, NOT part of the
/// Roslyn project model — deliberately absent (no guessing).
/// </summary>
public sealed record ConditionalCompilationFacts(
    IReadOnlyList<string> PreprocessorSymbols,
    string OptimizationLevel,
    string LanguageVersion)
{
    public static ConditionalCompilationFacts From(FspmCompilationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var symbols = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var document in snapshot.Documents)
        {
            var tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult();
            if (tree?.Options is CSharpParseOptions parseOptions)
            {
                foreach (var symbol in parseOptions.PreprocessorSymbolNames)
                {
                    symbols.Add(symbol);
                }
            }
        }

        var language = snapshot.Compilation is CSharpCompilation cs
            ? cs.LanguageVersion.ToString()
            : snapshot.Compilation.Language;

        return new ConditionalCompilationFacts(
            symbols.ToArray(),
            snapshot.Compilation.Options.OptimizationLevel.ToString(),
            language);
    }
}
