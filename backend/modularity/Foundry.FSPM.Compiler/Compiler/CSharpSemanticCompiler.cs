using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.WorkspaceNS;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Compiler;

/// <summary>
/// Phase 13 — Roslyn Semantic Compiler facade. Wraps the existing
/// <see cref="FspmProjectLoader"/> to build a snapshot, walks every
/// declared type in the primary project, and emits an
/// <see cref="FspmSymbolIndex"/> plus an eagerly-resolved
/// <see cref="CSharpResolver"/> for downstream phases (P14 binder).
/// </summary>
public sealed class CSharpSemanticCompiler
{
    public static async Task<FspmSemanticCompilationResult> CompileAsync(
        FspmProjectLoader loader,
        string solutionOrProjectPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(solutionOrProjectPath);

        FspmWorkspace workspace = await loader.LoadAsync(solutionOrProjectPath, cancellationToken).ConfigureAwait(false);
        try
        {
            var project = workspace.Projects[0];
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"Project '{project.FilePath ?? project.Name}' did not produce a Compilation. FSPM Compiler = FAIL.");

            using var snapshot = new FspmCompilationSnapshot(compilation, project);
            var index = BuildIndex(snapshot);
            var resolver = new CSharpResolver(snapshot);

            return new FspmSemanticCompilationResult(workspace, snapshot, index, resolver);
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    private static FspmSymbolIndex BuildIndex(FspmCompilationSnapshot snapshot)
    {
        var records = new List<FspmSymbolRecord>();
        CollectTypes(snapshot.Compilation.GlobalNamespace, records, seen: new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default));
        return new FspmSymbolIndex(records);
    }

    private static void CollectTypes(INamespaceSymbol ns, List<FspmSymbolRecord> sink, HashSet<INamedTypeSymbol> seen)
    {
        foreach (var t in ns.GetTypeMembers())
        {
            if (!seen.Add(t))
            {
                continue;
            }

            AddMembers(t, sink);
            foreach (var nested in t.GetTypeMembers())
            {
                if (seen.Add(nested))
                {
                    AddMembers(nested, sink);
                }
            }
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            CollectTypes(child, sink, seen);
        }
    }

    private static void AddMembers(INamedTypeSymbol type, List<FspmSymbolRecord> sink)
    {
        var typeId = FspmSymbolIdentity.Create(type);
        var typeLocation = FspmSourceLocation.From(type.Locations.FirstOrDefault() ?? Location.None);
        sink.Add(new FspmSymbolRecord(type, typeId, typeLocation));

        foreach (var member in type.GetMembers())
        {
            switch (member)
            {
                case IPropertySymbol p:
                    sink.Add(new FspmSymbolRecord(
                        p, FspmSymbolIdentity.Create(p), FspmSourceLocation.From(p.Locations.FirstOrDefault() ?? Location.None)));
                    break;
                case IMethodSymbol m when m.MethodKind == MethodKind.Ordinary:
                    sink.Add(new FspmSymbolRecord(
                        m, FspmSymbolIdentity.Create(m), FspmSourceLocation.From(m.Locations.FirstOrDefault() ?? Location.None)));
                    break;
            }
        }
    }
}

public sealed record FspmSemanticCompilationResult(
    FspmWorkspace Workspace,
    FspmCompilationSnapshot Snapshot,
    FspmSymbolIndex Index,
    CSharpResolver Resolver);
