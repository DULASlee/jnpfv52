namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H5: assembly identity as Roslyn reports it. Source tells whether
/// the assembly IS the snapshot's own output ("SourceProject") or a
/// referenced assembly ("ReferencedAssembly") — determined by assembly
/// name equality, never guessed.
/// </summary>
public sealed record AssemblyIdentity(
    string Name,
    string Version,
    string Culture,
    string PublicKeyToken,
    string Source);
