using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H3 entry points. Roslyn owns overload resolution, default values,
/// ref kinds and reduced forms — P13 only projects <c>IMethodSymbol</c>
/// facts. No argument binding engine, no generic inference, no
/// <c>.First()</c> overload auto-pick (callers disambiguate explicitly).
/// </summary>
public static class MethodSignatureExtractor
{
    public static NativeOperationIdentity ExtractOperationIdentity(IMethodSymbol method)
    {
        ArgumentNullException.ThrowIfNull(method);

        var parameters = method.Parameters.Select(p => new NativeParameterFact(
            Name: p.Name,
            ParameterType: p.Type.ToDisplayString(),
            RefKind: p.RefKind.ToString(),
            IsOptional: p.IsOptional,
            DefaultValue: p.HasExplicitDefaultValue ? FormatDefault(p.ExplicitDefaultValue) : null,
            IsParams: p.IsParams,
            NullableAnnotation: p.NullableAnnotation.ToString())).ToArray();

        var kind = method.MethodKind switch
        {
            MethodKind.Constructor => NativeSymbolKind.Constructor,
            MethodKind.Ordinary => NativeSymbolKind.Method,
            _ => NativeSymbolKind.Method,
        };

        return new NativeOperationIdentity(
            ContainingType: method.ContainingType.ToDisplayString(),
            Name: method.MethodKind == MethodKind.Constructor ? ".ctor" : method.Name,
            Arity: method.Arity,
            Parameters: parameters,
            ReturnType: method.MethodKind == MethodKind.Constructor
                ? "void"
                : method.ReturnType.ToDisplayString(),
            GenericParameters: method.TypeParameters.Select(t => t.Name).ToArray(),
            Kind: kind,
            StableId: FspmSymbolIdentity.Create(method).Value);
    }

    private static string FormatDefault(object? value) => value switch
    {
        null => "null",
        string s => "\"" + s + "\"",
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "unknown",
    };
}

/// <summary>
/// P13-H3: extension-method facts from Roslyn's <c>ReducedFrom</c>.
/// </summary>
public static class ExtensionMethodFactsExtractor
{
    public static ExtensionMethodFacts Extract(IMethodSymbol method)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (!method.IsExtensionMethod)
        {
            return new ExtensionMethodFacts(false, string.Empty, string.Empty);
        }

        var reducedFrom = method.ReducedFrom;
        return new ExtensionMethodFacts(
            IsExtensionMethod: true,
            ReducedFrom: reducedFrom?.ToDisplayString() ?? method.ToDisplayString(),
            ReceiverType: method.Parameters.Length > 0
                ? method.Parameters[0].Type.ToDisplayString()
                : string.Empty);
    }
}
