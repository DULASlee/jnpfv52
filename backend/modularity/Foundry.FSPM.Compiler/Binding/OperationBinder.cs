using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.Syntax;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Binding;

/// <summary>
/// Phase 8.3 — binds an FSPM operation declaration to a REAL Roslyn method symbol.
///
/// <para>Order is load-bearing (same as <see cref="PropertyBinder"/>): unique
/// owner first, then ITS ordinary methods. Binding is by REAL candidate
/// <see cref="IMethodSymbol"/>s, never by <c>Method.Name</c> string alone:
/// candidates are hierarchy-aware (<see cref="MemberLookup"/>, overrides collapsed)
/// and filtered to <see cref="MethodKind.Ordinary"/> (property
/// accessors, constructors and operators never qualify), then the count rule
/// applies — zero → Unknown (FSPM103), more than one overload →
/// Ambiguous (FSPM113) with signatures listed. FSPM v1 has no parameter
/// syntax, so multiple overloads can never be guessed apart (施工包 §42).</para>
///
/// <para>A name that exists on the owner but denotes no ordinary method
/// (e.g. a property) → Invalid with FSPM104 InvalidOperationSignature.
/// Owner unresolvable → Invalid with the owner's diagnostic propagated.</para>
/// </summary>
public static class OperationBinder
{
    /// <summary>
    /// Binds <paramref name="declaration"/> against a real <paramref name="compilation"/>.
    /// </summary>
    public static FspmBindingResult Bind(
        FspmOperationDeclarationSyntax declaration,
        Compilation compilation)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(compilation);

        var owners = EntityBinder.FindCandidates(compilation, declaration.EntityName);
        if (owners.Count != 1)
        {
            var ownerDiagnostics = owners.Count == 0
                ? new[]
                {
                    new FspmDiagnostic(
                        FspmDiagnosticCodes.EntityNotFound,
                        FspmDiagnosticSeverity.Error,
                        $"Unknown entity '{declaration.EntityName}' for operation '{declaration.EntityName}.{declaration.OperationName}'.",
                        declaration.Line,
                        declaration.Column),
                }
                : new[]
                {
                    new FspmDiagnostic(
                        FspmDiagnosticCodes.AmbiguousEntity,
                        FspmDiagnosticSeverity.Error,
                        $"Ambiguous entity '{declaration.EntityName}' for operation '{declaration.EntityName}.{declaration.OperationName}': {owners.Count} candidates.",
                        declaration.Line,
                        declaration.Column),
                };

            return FspmBindingResult.Fail(
                FspmBindingStatus.Invalid,
                declaration,
                ownerDiagnostics);
        }

        var owner = owners[0];
        var methods = MemberLookup.FindInHierarchy<IMethodSymbol>(owner, declaration.OperationName)
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .OrderBy(m => m.ToDisplayString(), StringComparer.Ordinal)
            .ToArray();

        if (methods.Length == 0)
        {
            var nonMethod = MemberLookup.FindInHierarchy<ISymbol>(owner, declaration.OperationName)
                .FirstOrDefault(m => m is not IMethodSymbol || ((IMethodSymbol)m).MethodKind != MethodKind.Ordinary);
            if (nonMethod is not null)
            {
                return FspmBindingResult.Fail(
                    FspmBindingStatus.Invalid,
                    declaration,
                    new[]
                    {
                        new FspmDiagnostic(
                            FspmDiagnosticCodes.InvalidOperationSignature,
                            FspmDiagnosticSeverity.Error,
                            $"Invalid operation '{declaration.EntityName}.{declaration.OperationName}': '{declaration.OperationName}' exists on '{owner.ToDisplayString()}' but is a {Describe(nonMethod)}, not an operation.",
                            declaration.Line,
                            declaration.Column),
                    });
            }

            return FspmBindingResult.Fail(
                FspmBindingStatus.Unknown,
                declaration,
                new[]
                {
                    new FspmDiagnostic(
                        FspmDiagnosticCodes.OperationNotFound,
                        FspmDiagnosticSeverity.Error,
                        $"Unknown operation '{declaration.EntityName}.{declaration.OperationName}': no such method on '{owner.ToDisplayString()}'.",
                        declaration.Line,
                        declaration.Column),
                });
        }

        if (methods.Length > 1)
        {
            var listed = string.Join("; ", methods.Select(m => FormatSignature(m)));
            return FspmBindingResult.Fail(
                FspmBindingStatus.Ambiguous,
                declaration,
                new[]
                {
                    new FspmDiagnostic(
                        FspmDiagnosticCodes.AmbiguousOperation,
                        FspmDiagnosticSeverity.Error,
                        $"Ambiguous operation '{declaration.EntityName}.{declaration.OperationName}': {methods.Length} overloads: {listed}. FSPM v1 has no parameter syntax — cannot choose.",
                        declaration.Line,
                        declaration.Column),
                });
        }

        var symbol = methods[0];
        return FspmBindingResult.Success(declaration, symbol, FspmSymbolIdentity.Create(symbol));
    }

    private static string Describe(ISymbol member) =>
        member is IMethodSymbol method && method.MethodKind != MethodKind.Ordinary
            ? $"compiler-generated {method.MethodKind} method"
            : member.Kind.ToString();

    private static string FormatSignature(IMethodSymbol method)
    {
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
        var statik = method.IsStatic ? "static " : string.Empty;
        return $"{statik}{method.ReturnType.ToDisplayString()} {method.Name}({parameters})";
    }
}
