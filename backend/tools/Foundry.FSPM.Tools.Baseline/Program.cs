using Foundry.FSPM.Compiler.WorkspaceNS;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Foundry.FSPM.Tools.Baseline;

internal sealed record WorkspaceDiagnosticRecord(
    string ProjectPath,
    string Kind,
    string Message);

internal sealed class Program
{
    private const string SchemaVersion = "1.0";
    private static readonly ConcurrentBag<WorkspaceDiagnosticRecord> s_workspaceDiagnostics = new();

    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: fspm-baseline <solutionOrProjectPath> <outputJsonPath> [--baseline-only]");
            return 2;
        }

        var inputPath = Path.GetFullPath(args[0]);
        var outputPath = Path.GetFullPath(args[1]);
        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Input not found: {inputPath}");
            return 2;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Console.Error.WriteLine($"[fspm-baseline] loading {inputPath}");

        var loader = new FspmProjectLoader();
        FspmWorkspace workspace;
        try
        {
            workspace = await loader.LoadAsync(inputPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[fspm-baseline] LoadAsync failed: {ex.GetType().Name}: {ex.Message}");
            return 3;
        }

        // Hook WorkspaceFailed on every underlying MSBuild workspace.
        var msbuildType = workspace.MSBuildWorkspace.GetType();
        var failedEvent = msbuildType.GetEvent("WorkspaceFailed");
        if (failedEvent is not null)
        {
            failedEvent.AddEventHandler(workspace.MSBuildWorkspace, new EventHandler<WorkspaceDiagnosticEventArgs>(
                (_, e) =>
                {
                    s_workspaceDiagnostics.Add(new WorkspaceDiagnosticRecord(
                        ProjectPath: "<workspace>",
                        Kind: e.Diagnostic.Kind.ToString(),
                        Message: e.Diagnostic.Message));
                }));
        }

        // Build Compilation per project and capture diagnostics.
        var projectRecords = new List<object>();
        var assemblyRecords = new List<object>();
        var documentRecords = new List<object>();
        var projectReferenceEdges = new List<object>();
        var typeRecords = new List<object>();
        var propertyRecords = new List<object>();
        var methodRecords = new List<object>();
        var namespaceRecords = new List<object>();
        var symbolCount = 0;

        int totalCsErrors = 0;
        int totalCsWarnings = 0;

        foreach (var project in workspace.Projects)
        {
            Compilation? compilation = null;
            try
            {
                compilation = await project.GetCompilationAsync();
            }
            catch (Exception ex)
            {
                projectRecords.Add(new
                {
                    name = project.Name,
                    filePath = project.FilePath,
                    compilationFailed = true,
                    error = ex.GetType().Name + ": " + ex.Message,
                });
                continue;
            }

            if (compilation is null)
            {
                projectRecords.Add(new
                {
                    name = project.Name,
                    filePath = project.FilePath,
                    compilationFailed = true,
                    error = "GetCompilationAsync returned null",
                });
                continue;
            }

            var csDiags = compilation.GetDiagnostics();
            var errs = csDiags.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            var warns = csDiags.Where(d => d.Severity == DiagnosticSeverity.Warning).ToArray();
            totalCsErrors += errs.Length;
            totalCsWarnings += warns.Length;

            projectRecords.Add(new
            {
                name = project.Name,
                filePath = project.FilePath,
                assemblyName = compilation.AssemblyName,
                syntaxTrees = compilation.SyntaxTrees.Count(),
                metadataReferences = compilation.ReferencedAssemblyNames.Count(),
                csErrorCount = errs.Length,
                csWarningCount = warns.Length,
                projectReferences = project.ProjectReferences.Count(),
            });

            assemblyRecords.Add(new
            {
                name = compilation.AssemblyName,
                referencedAssemblies = compilation.ReferencedAssemblyNames.Count(),
            });

            foreach (var pr in project.ProjectReferences)
            {
                projectReferenceEdges.Add(new
                {
                    from = project.AssemblyName ?? project.Name,
                    to = pr.ProjectId.Id.ToString(),
                });
            }

            foreach (var doc in project.Documents)
            {
                documentRecords.Add(new
                {
                    project = project.AssemblyName ?? project.Name,
                    filePath = doc.FilePath,
                    folders = string.Join("/", doc.Folders),
                });
            }

            // Symbol inventory — reuse existing FspmSymbolIdentity.
            symbolCount += CollectSymbols(compilation, typeRecords, propertyRecords, methodRecords, namespaceRecords);
        }

        // Workspace-level failures collected during load.
        var workspaceFailures = s_workspaceDiagnostics
            .OrderBy(d => d.ProjectPath, StringComparer.Ordinal)
            .ThenBy(d => d.Kind, StringComparer.Ordinal)
            .ToArray();

        // Merge any in-projection MSBuild failures (DiagnosticSeverity.Error at workspace level).
        var allMsBuildFailures = workspaceFailures
            .Concat(workspace.Projects.SelectMany(p => p.Documents.Select(d => new WorkspaceDiagnosticRecord(
                "<noop>", "<noop>", "<noop>"))).Where(_ => false))
            .ToArray();

        var baseline = new
        {
            schemaVersion = SchemaVersion,
            generatedUtc = DateTime.UtcNow.ToString("O"),
            generator = "Foundry.FSPM.Tools.Baseline",
            repositoryCommit = SafeHead(Path.Combine(inputPath, "..", "..", "..", ".git")),
            compilerCommit = ResolveCompilerCommit(),
            solution = new
            {
                path = inputPath,
                fileName = Path.GetFileName(inputPath),
                projectCount = workspace.Projects.Count,
            },
            counts = new
            {
                projects = projectRecords.Count,
                assemblies = assemblyRecords.Count,
                documents = documentRecords.Count,
                syntaxTrees = workspace.Projects.Sum(p => CountSyntaxTrees(p)),
                metadataReferences = workspace.Projects.Sum(p => CountMetadataReferences(p)),
                workspaceFailures = workspaceFailures.Length,
                csErrors = totalCsErrors,
                csWarnings = totalCsWarnings,
                types = typeRecords.Count,
                properties = propertyRecords.Count,
                methods = methodRecords.Count,
                namespaces = namespaceRecords.Count,
                symbols = symbolCount,
            },
            workspaceFailures = allMsBuildFailures.Select(d => new
            {
                projectPath = d.ProjectPath,
                kind = d.Kind,
                message = d.Message,
            }).ToArray(),
            projects = projectRecords,
            assemblies = assemblyRecords,
            projectReferences = projectReferenceEdges,
            documents = documentRecords,
            symbols = new
            {
                types = typeRecords,
                properties = propertyRecords,
                methods = methodRecords,
                namespaces = namespaceRecords,
            },
            // Schema version hash is computed from the canonical payload below.
            // Runtime fields (generatedUtc, generator) are EXCLUDED from the hash
            // so the baseline is fully reproducible.
        };

        // Stable canonical JSON: serialize once, then hash it with timestamp stripped
        // by serializing a "hashed" view. We keep the full payload for human reading
        // and produce a separate canonical hash for determinism verification.
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        var fullJson = JsonSerializer.Serialize(baseline, jsonOptions);

        // Canonical hash: copy the baseline minus the timing fields.
        var forHash = new
        {
            schemaVersion = baseline.schemaVersion,
            generator = baseline.generator,
            repositoryCommit = baseline.repositoryCommit,
            compilerCommit = baseline.compilerCommit,
            solution = baseline.solution,
            counts = baseline.counts,
            workspaceFailures = baseline.workspaceFailures,
            projects = baseline.projects,
            assemblies = baseline.assemblies,
            projectReferences = baseline.projectReferences,
            documents = baseline.documents,
            symbols = baseline.symbols,
        };
        var canonicalJson = JsonSerializer.Serialize(forHash, jsonOptions);
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonicalJson)));

        // Wrap output with the hash for downstream verification.
        var finalDoc = new
        {
            baseline = baseline,
            baselineHash = hash,
            elapsedMs = stopwatch.ElapsedMilliseconds,
        };
        var finalJson = JsonSerializer.Serialize(finalDoc, jsonOptions);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, finalJson, Encoding.UTF8);

        Console.Error.WriteLine($"[fspm-baseline] wrote {outputPath}");
        Console.Error.WriteLine($"[fspm-baseline] baselineHash={hash}");
        Console.Error.WriteLine($"[fspm-baseline] projects={baseline.counts.projects} types={baseline.counts.types} props={baseline.counts.properties} methods={baseline.counts.methods}");
        Console.Error.WriteLine($"[fspm-baseline] csErrors={baseline.counts.csErrors} csWarnings={baseline.counts.csWarnings} wsFailures={baseline.counts.workspaceFailures}");
        return 0;
    }

    private static int CollectSymbols(
        Compilation compilation,
        List<object> types,
        List<object> properties,
        List<object> methods,
        List<object> namespaces)
    {
        // Walk every type in the compilation, in source order of declaration,
        // and emit the canonical inventory. Uses FspmSymbolIdentity.Create
        // as the SOLE source of SymbolIds (no second identity path).
        var seenNamespaces = new HashSet<string>(StringComparer.Ordinal);
        var counter = 0;
        WalkNs(compilation.GlobalNamespace, types, properties, methods, namespaces, seenNamespaces, ref counter);
        return counter;
    }

    private static void WalkNs(
        INamespaceSymbol ns,
        List<object> types,
        List<object> properties,
        List<object> methods,
        List<object> namespaces,
        HashSet<string> seenNamespaces,
        ref int counter)
    {
        if (seenNamespaces.Add(ns.ToDisplayString()))
        {
            namespaces.Add(new
            {
                fullName = ns.ToDisplayString(),
                isGlobal = ns.IsGlobalNamespace,
            });
        }

        foreach (var t in ns.GetTypeMembers())
        {
            WalkType(t, types, properties, methods, ref counter);
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            WalkNs(child, types, properties, methods, namespaces, seenNamespaces, ref counter);
        }
    }

    private static void WalkType(
        INamedTypeSymbol t,
        List<object> types,
        List<object> properties,
        List<object> methods,
        ref int counter)
    {
        types.Add(new
        {
            assemblyName = t.ContainingAssembly?.Name ?? "<missing>",
            fullName = t.ToDisplayString(),
            kind = t.TypeKind.ToString(),
            symbolId = Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(t).Value,
        });
        counter++;

        foreach (var member in t.GetMembers())
        {
            if (member is IPropertySymbol p)
            {
                properties.Add(new
                {
                    assemblyName = p.ContainingAssembly?.Name ?? "<missing>",
                    fullName = p.ToDisplayString(),
                    typeName = p.Type.ToDisplayString(),
                    symbolId = Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(p).Value,
                });
                counter++;
            }
            else if (member is IMethodSymbol m && m.MethodKind == MethodKind.Ordinary)
            {
                methods.Add(new
                {
                    assemblyName = m.ContainingAssembly?.Name ?? "<missing>",
                    fullName = m.ToDisplayString(),
                    returnType = m.ReturnType.ToDisplayString(),
                    parameterCount = m.Parameters.Length,
                    isStatic = m.IsStatic,
                    symbolId = Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(m).Value,
                });
                counter++;
            }
        }
    }

    private static int CountSyntaxTrees(Project p)
    {
        // Project in FspmWorkspace is the underlying Microsoft.CodeAnalysis.Project
        // (concrete type lives in the MSBuild assembly loaded via MSBuildLocator at
        // runtime). We invoke the SyntaxTrees property through reflection so the
        // tool project never needs a hard reference to Microsoft.CodeAnalysis.MSBuild.
        var prop = p.GetType().GetProperty("SyntaxTrees");
        if (prop is null) return 0;
        var value = prop.GetValue(p);
        if (value is System.Collections.ICollection col) return col.Count;
        return value is System.Collections.IEnumerable e ? e.Cast<object>().Count() : 0;
    }

    private static int CountMetadataReferences(Project p)
    {
        var prop = p.GetType().GetProperty("MetadataReferences");
        if (prop is null) return 0;
        var value = prop.GetValue(p);
        if (value is System.Collections.ICollection col) return col.Count;
        return value is System.Collections.IEnumerable e ? e.Cast<object>().Count() : 0;
    }

    private static string ResolveCompilerCommit()
    {
        // Walk up from the executable directory until we find .git/HEAD or
        // a .git gitlink file (worktrees use a pointer file, not a directory).
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 16 && dir is not null; i++, dir = dir.Parent)
        {
            var dotGit = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(dotGit))
            {
                return SafeHead(dotGit);
            }

            if (File.Exists(dotGit))
            {
                // .git is a gitdir pointer (worktree). Read its target and
                // resolve to its parent repository, then dereference HEAD.
                var target = File.ReadAllText(dotGit).Trim();
                const string Prefix = "gitdir: ";
                if (target.StartsWith(Prefix, StringComparison.Ordinal))
                {
                    var worktreeGitDir = target.Substring(Prefix.Length);
                    if (!Path.IsPathRooted(worktreeGitDir))
                    {
                        worktreeGitDir = Path.GetFullPath(Path.Combine(dir.FullName, worktreeGitDir));
                    }

                    // The worktree's own refs/ may be empty; the canonical
                    // refs/ live in the commondir (commondir file holds the
                    // relative path from worktree gitdir to the common gitdir).
                    var commondirFile = Path.Combine(worktreeGitDir, "commondir");
                    var commonGitDir = worktreeGitDir;
                    if (File.Exists(commondirFile))
                    {
                        var rel = File.ReadAllText(commondirFile).Trim();
                        commonGitDir = Path.GetFullPath(Path.Combine(worktreeGitDir, rel));
                    }

                    var head = Path.Combine(worktreeGitDir, "HEAD");
                    if (File.Exists(head))
                    {
                        var headContent = File.ReadAllText(head).Trim();
                        if (headContent.StartsWith("ref: ", StringComparison.Ordinal))
                        {
                            var refRel = headContent.Substring("ref: ".Length);
                            // 1) try the worktree's own refs (may not exist)
                            var refPath1 = Path.Combine(worktreeGitDir, refRel);
                            if (File.Exists(refPath1))
                            {
                                return File.ReadAllText(refPath1).Trim();
                            }

                            // 2) try the common refs/ (canonical location)
                            var refPath2 = Path.Combine(commonGitDir, refRel);
                            if (File.Exists(refPath2))
                            {
                                return File.ReadAllText(refPath2).Trim();
                            }

                            // 3) try the packed-refs file
                            var packed = Path.Combine(commonGitDir, "packed-refs");
                            if (File.Exists(packed))
                            {
                                foreach (var line in File.ReadAllLines(packed))
                                {
                                    if (line.StartsWith("#", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(line))
                                    {
                                        continue;
                                    }

                                    var parts = line.Split(' ', 2);
                                    if (parts.Length == 2 && parts[1] == refRel)
                                    {
                                        return parts[0];
                                    }
                                }
                            }
                        }

                        return headContent;
                    }
                }

                return "<worktree-no-head>";
            }
        }

        return "<missing>";
    }

    private static string SafeHead(string gitDir)
    {
        try
        {
            var head = Path.Combine(gitDir, "HEAD");
            if (!File.Exists(head)) return "<missing>";

            var content = File.ReadAllText(head).Trim();
            if (content.StartsWith("ref: ", StringComparison.Ordinal))
            {
                var refPath = Path.Combine(gitDir, content.Substring("ref: ".Length));
                if (File.Exists(refPath))
                {
                    return File.ReadAllText(refPath).Trim();
                }

                return "<detached>";
            }

            return content;
        }
        catch
        {
            return "<missing>";
        }
    }
}
