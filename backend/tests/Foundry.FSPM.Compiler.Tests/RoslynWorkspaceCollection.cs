using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// All tests that load a REAL MSBuildWorkspace must serialize process-wide:
/// MSBuild's static BuildManager supports only one concurrent design-time
/// build — parallel <c>OpenProjectAsync</c> calls fail with
/// "already in progress". Pure Lexer/Parser/Diagnostics unit tests stay parallel.
/// </summary>
[CollectionDefinition("RoslynWorkspace", DisableParallelization = true)]
public sealed class RoslynWorkspaceCollection
{
}
