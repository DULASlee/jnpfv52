using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.Syntax;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Binding;

/// <summary>
/// Phase 8.2 — binds an FSPM property declaration to a REAL Roslyn property symbol.
///
/// <para>Order is load-bearing: resolve the OWNER entity first via the same
/// candidate rule as <see cref="EntityBinder"/>. Only when the owner is unique
/// are ITS members queried — so <c>OtherUser.PhoneNumber</c> can never
/// misbind to <c>User.PhoneNumber</c> by construction.</para>
///
/// <para>Owner unresolvable → status Invalid with the owner's diagnostic(s)
/// propagated verbatim (no new guessing, no silent failure).</para>
///
/// <para>Member rule on the owner's REAL members (<see cref="IPropertySymbol"/> only,
/// hierarchy-aware via <see cref="MemberLookup"/> — inherited members resolve
/// deterministically per 施工包 §57): zero → Unknown (FSPM102); more than one
/// (e.g. <c>new</c>-shadowed duplicates) → Ambiguous (FSPM112); exactly one →
/// Success. A name that exists only as a non-property member is still
/// Unknown-as-a-property, with that fact stated in the message.</para>
/// </summary>
public static class PropertyBinder
{
    /// <summary>
    /// Binds <paramref name="declaration"/> against a real <paramref name="compilation"/>.
    /// </summary>
    public static FspmBindingResult Bind(
        FspmPropertyDeclarationSyntax declaration,
        Compilation compilation)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(compilation);

        var owners = EntityBinder.FindCandidates(compilation, declaration.EntityName);
        if (owners.Count != 1)
        {
            var ownerResult = owners.Count == 0
                ? FspmBindingResult.Fail(
                    FspmBindingStatus.Unknown,
                    declaration,
                    new[]
                    {
                        new FspmDiagnostic(
                            FspmDiagnosticCodes.EntityNotFound,
                            FspmDiagnosticSeverity.Error,
                            $"Unknown entity '{declaration.EntityName}' for property '{declaration.EntityName}.{declaration.PropertyName}'.",
                            declaration.Line,
                            declaration.Column),
                    })
                : FspmBindingResult.Fail(
                    FspmBindingStatus.Ambiguous,
                    declaration,
                    new[]
                    {
                        new FspmDiagnostic(
                            FspmDiagnosticCodes.AmbiguousEntity,
                            FspmDiagnosticSeverity.Error,
                            $"Ambiguous entity '{declaration.EntityName}' for property '{declaration.EntityName}.{declaration.PropertyName}': {owners.Count} candidates.",
                            declaration.Line,
                            declaration.Column),
                    });

            // The property itself is neither unknown nor ambiguous — its OWNER
            // is unresolvable. Report Invalid with the owner diagnostic.
            return FspmBindingResult.Fail(
                FspmBindingStatus.Invalid,
                declaration,
                ownerResult.Diagnostics);
        }

        var owner = owners[0];
        var properties = MemberLookup.FindInHierarchy<IPropertySymbol>(owner, declaration.PropertyName)
            .OrderBy(p => p.ToDisplayString(), StringComparer.Ordinal)
            .ToArray();

        if (properties.Length == 0)
        {
            var nonProperty = MemberLookup.FindInHierarchy<ISymbol>(owner, declaration.PropertyName)
                .FirstOrDefault(m => m is not IPropertySymbol);
            var hint = nonProperty is null
                ? $"no member named '{declaration.PropertyName}'."
                : $"'{declaration.PropertyName}' exists but is a {nonProperty.Kind}, not a property.";
            return FspmBindingResult.Fail(
                FspmBindingStatus.Unknown,
                declaration,
                new[]
                {
                    new FspmDiagnostic(
                        FspmDiagnosticCodes.PropertyNotFound,
                        FspmDiagnosticSeverity.Error,
                        $"Unknown property '{declaration.EntityName}.{declaration.PropertyName}': {hint}",
                        declaration.Line,
                        declaration.Column),
                });
        }

        if (properties.Length > 1)
        {
            var listed = string.Join(", ", properties.Select(p => p.ToDisplayString()));
            return FspmBindingResult.Fail(
                FspmBindingStatus.Ambiguous,
                declaration,
                new[]
                {
                    new FspmDiagnostic(
                        FspmDiagnosticCodes.AmbiguousProperty,
                        FspmDiagnosticSeverity.Error,
                        $"Ambiguous property '{declaration.EntityName}.{declaration.PropertyName}': {properties.Length} candidates: {listed}.",
                        declaration.Line,
                        declaration.Column),
                });
        }

        var symbol = properties[0];
        return FspmBindingResult.Success(declaration, symbol, FspmSymbolIdentity.Create(symbol));
    }
}
