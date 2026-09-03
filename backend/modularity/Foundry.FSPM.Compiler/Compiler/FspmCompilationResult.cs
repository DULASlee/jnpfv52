using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Parser;
using Foundry.FSPM.Compiler.Syntax;

namespace Foundry.FSPM.Compiler.Compiler;

/// <summary>
/// Top-level result of FspmCompiler.Compile (Phase 4 minimal — 施工包 §47).
/// In Phase 10 this will also carry FspmSemanticModel. Today only Syntax + Diagnostics are populated.
/// </summary>
public sealed record FspmCompilationResult(
    bool Succeeded,
    FspmCompilationUnitSyntax Syntax,
    IReadOnlyList<FspmDiagnostic> Diagnostics);
