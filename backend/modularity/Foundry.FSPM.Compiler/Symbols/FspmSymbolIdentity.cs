using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Symbols;

/// <summary>
/// Phase 7 — 施工包 §27-§28. Factory + resolver for <see cref="FspmSymbolId"/>.
/// The ONLY place allowed to mint an identity from a real Roslyn symbol.
///
/// <para>
/// <b>Architecture Decision (explicit, test-proven — not "feels like"):</b>
/// identity = <c>{AssemblySimpleName}|{DocumentationCommentId}</c>.
/// </para>
///
/// <para>
/// Why DocumentationCommentId (Roslyn 4.8, real API):
/// <list type="bullet">
/// <item>Type: <c>T:Full.Namespace.Type</c> — namespace-qualified; generic arity
/// encoded by Roslyn (<c>List`1</c>).</item>
/// <item>Property: <c>P:ContainingType.Name</c> — containing-type-qualified,
/// so <c>User.PhoneNumber != OtherUser.PhoneNumber</c>.</item>
/// <item>Method: <c>M:ContainingType.Method(ParamTypes)~ReturnType</c> (Roslyn
/// appends the <c>~ReturnType</c> suffix for non-void methods) — overloads differ
/// by parameter type list, so <c>Create(string) != Create(int)</c>. C# forbids
/// overloads differing ONLY by static-ness, so no extra static bit is needed
/// within one containing type.</item>
/// </list>
/// </para>
///
/// <para>
/// Why the assembly qualifier: DocumentationCommentId alone carries NO assembly,
/// so <c>ProjectA.User</c> and <c>ProjectB.User</c> would collide. The assembly
/// simple name is the MSBuild-level discriminator. Assembly <i>version</i> is
/// deliberately EXCLUDED: it floats with CI rebuilds and would break
/// Identity(Run1) == Identity(Run2) across rebuilds (施工包 §62-§63).
/// </para>
///
/// <para>
/// Why not SymbolKey: SymbolKey targets cross-solution persistence with a
/// heavier resolution protocol; for same-compilation identity plus
/// cross-run re-resolution, DocId + assembly is simpler and provably sufficient.
/// Proven by: <c>Resolve(id, compilation)</c> round-trip tests
/// (<c>DocumentationCommentId.GetSymbolsForDeclarationId</c> + assembly match).
/// </para>
///
/// <para>
/// Failure policy (honest, never guessing — 施工包 §37/§42 spirit):
/// 0 matches → <see cref="InvalidOperationException"/>; &gt;1 match →
/// <see cref="InvalidOperationException"/>. Never First(). Never null.
/// </para>
/// </summary>
public static class FspmSymbolIdentity
{
    /// <summary>Creates the identity of a real type symbol (FSPM entity).</summary>
    public static FspmSymbolId Create(INamedTypeSymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return FromSymbol(symbol);
    }

    /// <summary>Creates the identity of a real property symbol (FSPM property).</summary>
    public static FspmSymbolId Create(IPropertySymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return FromSymbol(symbol);
    }

    /// <summary>Creates the identity of a real method symbol (FSPM operation).</summary>
    public static FspmSymbolId Create(IMethodSymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return FromSymbol(symbol);
    }

    /// <summary>Creates the identity of a real field symbol (same DocId+assembly canonical form).</summary>
    public static FspmSymbolId Create(IFieldSymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return FromSymbol(symbol);
    }

    /// <summary>Creates the identity of a real event symbol (same DocId+assembly canonical form).</summary>
    public static FspmSymbolId Create(IEventSymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return FromSymbol(symbol);
    }

    /// <summary>
    /// Derives the FSPM kind from an identity's DocumentationCommentId prefix
    /// (<c>T:</c> → Entity, <c>P:</c> → Property, <c>M:</c> → Operation).
    /// </summary>
    public static FspmSymbolKind GetKind(FspmSymbolId id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id.Value);

        var separator = id.Value.IndexOf('|');
        if (separator < 0 || separator + 2 >= id.Value.Length)
        {
            throw new ArgumentException($"Malformed FspmSymbolId: '{id.Value}'.", nameof(id));
        }

        return id.Value[separator + 1] switch
        {
            'T' when id.Value[separator + 2] == ':' => FspmSymbolKind.Entity,
            'P' when id.Value[separator + 2] == ':' => FspmSymbolKind.Property,
            'M' when id.Value[separator + 2] == ':' => FspmSymbolKind.Operation,
            _ => throw new ArgumentException($"Malformed FspmSymbolId: '{id.Value}'.", nameof(id)),
        };
    }

    /// <summary>
    /// Resolves an identity back to the SAME semantic symbol in a real compilation.
    /// Proof vehicle for directive §七: Resolve(identity) → same semantic symbol.
    /// Throws (never guesses) when unresolvable or ambiguous.
    /// </summary>
    public static ISymbol Resolve(FspmSymbolId id, Compilation compilation)
    {
        ArgumentException.ThrowIfNullOrEmpty(id.Value);
        ArgumentNullException.ThrowIfNull(compilation);

        var separator = id.Value.IndexOf('|');
        if (separator <= 0 || separator + 1 >= id.Value.Length)
        {
            throw new ArgumentException($"Malformed FspmSymbolId: '{id.Value}'.", nameof(id));
        }

        var assemblyName = id.Value.Substring(0, separator);
        var declarationId = id.Value.Substring(separator + 1);

        var candidates = DocumentationCommentId.GetSymbolsForDeclarationId(declarationId, compilation);
        var inAssembly = candidates
            .Where(s => string.Equals(s.ContainingAssembly?.Name, assemblyName, StringComparison.Ordinal))
            .ToArray();

        if (inAssembly.Length == 0)
        {
            throw new InvalidOperationException(
                $"FspmSymbolId '{id.Value}' resolved to 0 symbols in compilation " +
                $"'{compilation.AssemblyName}'. FSPM Compiler = FAIL (NOT_FOUND, not guessed).");
        }

        if (inAssembly.Length > 1)
        {
            throw new InvalidOperationException(
                $"FspmSymbolId '{id.Value}' resolved to {inAssembly.Length} symbols in compilation " +
                $"'{compilation.AssemblyName}'. FSPM Compiler = FAIL (AMBIGUOUS, never First()).");
        }

        return inAssembly[0];
    }

    private static FspmSymbolId FromSymbol(ISymbol symbol)
    {
        var declarationId = DocumentationCommentId.CreateDeclarationId(symbol);
        if (string.IsNullOrEmpty(declarationId))
        {
            throw new InvalidOperationException(
                $"Symbol '{symbol.ToDisplayString()}' has no stable DocumentationCommentId. " +
                "Refusing to mint a fake identity.");
        }

        var assemblyName = symbol.ContainingAssembly?.Name;
        if (string.IsNullOrEmpty(assemblyName))
        {
            throw new InvalidOperationException(
                $"Symbol '{symbol.ToDisplayString()}' has no containing assembly. " +
                "Refusing to mint an assembly-ambiguous identity.");
        }

        return new FspmSymbolId($"{assemblyName}|{declarationId}");
    }
}
