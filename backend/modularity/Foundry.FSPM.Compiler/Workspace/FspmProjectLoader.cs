using System.Reflection;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.WorkspaceNS;

/// <summary>
/// Loads a real .sln or .csproj via MSBuildWorkspace (Phase 6 — 施工包 §22).
/// Uses reflection for MSBuildWorkspace to ensure <see cref="MSBuildLocator"/>
/// is registered BEFORE the Microsoft.CodeAnalysis.Workspaces.MSBuild
/// assembly is loaded (required by MSBuildLocator docs).
/// </summary>
public sealed class FspmProjectLoader
{
    private static readonly object s_locatorLock = new();
    private static bool s_locatorRegistered;

    private static void EnsureMSBuildLocator()
    {
        if (s_locatorRegistered) return;
        lock (s_locatorLock)
        {
            if (s_locatorRegistered) return;
            if (!MSBuildLocator.IsRegistered)
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();

                // Log for diagnostics if needed (not throwing)
                // Prefer .NET 8 SDK — Roslyn 4.8 is built against MSBuild 17.8.
                var preferred = instances
                    .Where(i => i.Version.Major == 8)
                    .OrderByDescending(i => i.Version)
                    .FirstOrDefault()
                    ?? instances.OrderByDescending(i => i.Version).FirstOrDefault();

                if (preferred is not null)
                {
                    MSBuildLocator.RegisterInstance(preferred);
                }
                else
                {
                    MSBuildLocator.RegisterDefaults();
                }
            }

            s_locatorRegistered = true;
        }
    }

    private static Workspace CreateMSBuildWorkspace()
    {
        EnsureMSBuildLocator();

        // Assembly must be loaded AFTER locator registration.
        var asm = Assembly.Load("Microsoft.CodeAnalysis.Workspaces.MSBuild");
        var type = asm.GetType("Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace", throwOnError: true)!;
        var create = type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, binder: null, types: Type.EmptyTypes, modifiers: null)
                     ?? throw new InvalidOperationException("MSBuildWorkspace.Create() not found.");
        var ws = (Workspace)create.Invoke(null, null)!;

        // Hook up WorkspaceFailed to surface MSBuild evaluation errors (useful for debugging 0 refs)
        ws.WorkspaceFailed += (_, e) =>
        {
            // Intentionally write to console for test diagnostics; not throwing.
            Console.Error.WriteLine($"[MSBuildWorkspace] {e.Diagnostic.Kind}: {e.Diagnostic.Message}");
        };

        return ws;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance API per施工包 §22; test uses new FspmProjectLoader()")]
    public async Task<FspmWorkspace> LoadAsync(string solutionOrProjectPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solutionOrProjectPath);

        if (!File.Exists(solutionOrProjectPath))
        {
            throw new FileNotFoundException(
                $"Solution or project file not found: {solutionOrProjectPath}",
                solutionOrProjectPath);
        }

        var msbuild = CreateMSBuildWorkspace();

        // Use dynamic/reflection to call OpenProjectAsync / OpenSolutionAsync on the Workspace instance.
        // MSBuildWorkspace declares these; Workspace base does not have them, so we reflect.
        var msbuildType = msbuild.GetType();
        Solution solution;
        var ext = Path.GetExtension(solutionOrProjectPath);
        if (string.Equals(ext, ".sln", StringComparison.OrdinalIgnoreCase))
        {
            // Look for OpenSolutionAsync overloads via reflection without hard type ref.
            var openSolution = msbuildType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "OpenSolutionAsync" && m.GetParameters().Length == 2
                    && m.GetParameters()[0].ParameterType == typeof(string))
                ?? msbuildType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "OpenSolutionAsync")
                ?? throw new InvalidOperationException("OpenSolutionAsync not found.");
            object? taskObj;
            var parms = openSolution.GetParameters();
            if (parms.Length == 2)
            {
                taskObj = openSolution.Invoke(msbuild, new object?[] { solutionOrProjectPath, cancellationToken });
            }
            else
            {
                // (string, IProgress<...>, CancellationToken)
                taskObj = openSolution.Invoke(msbuild, new object?[] { solutionOrProjectPath, null, cancellationToken });
            }

            solution = await ((Task<Solution>)taskObj!).ConfigureAwait(false);
        }
        else
        {
            var openProject = msbuildType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "OpenProjectAsync" && m.GetParameters().Length == 3)
                ?? msbuildType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "OpenProjectAsync")
                ?? throw new InvalidOperationException("OpenProjectAsync not found.");

            // (string, IProgress<...>, CancellationToken)
            var parms = openProject.GetParameters();
            object? taskObj;
            if (parms.Length == 2)
            {
                taskObj = openProject.Invoke(msbuild, new object?[] { solutionOrProjectPath, cancellationToken });
            }
            else
            {
                taskObj = openProject.Invoke(msbuild, new object?[] { solutionOrProjectPath, null, cancellationToken });
            }

            var project = await ((Task<Project>)taskObj!).ConfigureAwait(false);
            solution = project.Solution;
        }

        var projects = solution.Projects.ToArray();
        var rootPath = Path.GetDirectoryName(solutionOrProjectPath) ?? Directory.GetCurrentDirectory();

        return new FspmWorkspace
        {
            RootPath = rootPath,
            MSBuildWorkspace = msbuild,
            Projects = projects,
        };
    }

    public static async Task<Compilation> GetCompilationAsync(FspmWorkspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (workspace.Projects.Count == 0)
        {
            throw new InvalidOperationException("Workspace contains no projects.");
        }

        var firstProject = workspace.Projects[0];
        var compilation = await firstProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation == null)
        {
            throw new InvalidOperationException(
                $"Project '{firstProject.FilePath ?? firstProject.Name}' did not produce a Compilation. " +
                $"FSPM Compiler = FAIL.");
        }

        _ = compilation.AssemblyName;
        _ = compilation.SourceModule;
        _ = compilation.GlobalNamespace;

        return compilation;
    }
}


