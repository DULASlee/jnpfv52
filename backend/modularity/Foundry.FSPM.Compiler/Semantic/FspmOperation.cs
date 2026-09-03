using Foundry.FSPM.Compiler.Binding;
using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// Phase 9 — 施工包 §32. FSPM Operation. One-to-one with REAL <see cref="IMethodSymbol"/>.
///
/// <para>Identity rule: <see cref="SymbolId"/> is REUSED verbatim from the
/// <see cref="FspmBindingResult"/>. The model never recomputes identity.</para>
///
/// <para>Failure rule: a non-Success operation has <c>Symbol == null</c> and
/// carries its diagnostics. The owning entity is recorded (or null when the
/// owner itself was unresolvable) purely for diagnostic context.</para>
/// </summary>
public sealed record FspmOperation
{
    public required FspmSymbolId SymbolId { get; init; }

    public required IMethodSymbol? Symbol { get; init; }

    public required FspmBindingResult Binding { get; init; }

    public FspmEntity? Owner { get; init; }

    public FspmBindingStatus Status => Binding.Status;

    public string? Name => Symbol?.Name;

    public string? ReturnType => Symbol?.ReturnType.ToDisplayString();

    public bool IsStatic => Symbol?.IsStatic ?? false;

    public IReadOnlyList<string> ParameterTypes =>
        Symbol is null
            ? Array.Empty<string>()
            : Symbol.Parameters.Select(p => p.Type.ToDisplayString()).ToArray();

    public bool IsResolved => Status == FspmBindingStatus.Success;
}
