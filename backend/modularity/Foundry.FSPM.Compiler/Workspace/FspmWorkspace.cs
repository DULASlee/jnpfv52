using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.WorkspaceNS;

/// <summary>
/// Phase 6 — 施工包 §21.
/// Holds the real MSBuildWorkspace + loaded Projects.
/// Typed as <see cref="Workspace"/> base to avoid compile-time reference
/// to Microsoft.CodeAnalysis.MSBuild before MSBuildLocator is registered.
/// </summary>
public sealed class FspmWorkspace : IDisposable
{
    public required string RootPath { get; init; }

    /// <summary>
    /// Underlying MSBuildWorkspace instance (runtime type is Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace).
    /// Exposed as base <see cref="Workspace"/> to keep MSBuild assembly load deferred until after locator.
    /// </summary>
    public required Workspace MSBuildWorkspace { get; init; }

    public required IReadOnlyList<Project> Projects { get; init; }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { (MSBuildWorkspace as IDisposable)?.Dispose(); } catch { }
    }
}
