using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H7: assesses whether a compilation is clean enough for its
/// resolutions to be reported as <see cref="SemanticQuality.Perfect"/>.
/// A compilation with errors can still yield real symbols (Roslyn
/// recovers); those resolutions are <see cref="SemanticQuality.Degraded"/>.
/// </summary>
public static class SemanticQualityAssessor
{
    public static SemanticQuality AssessQuality(Compilation compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        return compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)
            ? SemanticQuality.Degraded
            : SemanticQuality.Perfect;
    }
}
