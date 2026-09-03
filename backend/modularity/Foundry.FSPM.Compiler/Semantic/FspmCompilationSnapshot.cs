using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 13 — immutable snapshot of a real C# <see cref="Compilation"/> and
/// its primary <see cref="Project"/>. Everything in P13 (resolver, index,
/// identity minting) operates against a snapshot so the live MSBuildWorkspace
/// can be disposed without losing resolution capability.
/// </summary>
public sealed class FspmCompilationSnapshot : IDisposable
{
    public FspmCompilationSnapshot(Compilation compilation, Project primaryProject)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(primaryProject);

        Compilation = compilation;
        PrimaryProject = primaryProject;
    }

    public Compilation Compilation { get; }

    public Project PrimaryProject { get; }

    public string ProjectName => PrimaryProject.Name;
    public string AssemblyName => Compilation.AssemblyName ?? PrimaryProject.AssemblyName ?? "<unknown>";

    public IReadOnlyList<Document> Documents => PrimaryProject.Documents.ToArray();

    public Microsoft.CodeAnalysis.SemanticModel GetSemanticModel(Document document) =>
        Compilation.GetSemanticModel(document.GetSyntaxTreeAsync().GetAwaiter().GetResult());

    public void Dispose() { /* Compilation is GC-owned; nothing to free. */ }
}
