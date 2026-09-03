using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H4 entry point: fact extraction for type/method/property
/// relationships. Every value comes from an already-computed Roslyn
/// property; P13 performs no hierarchy inference of its own.
/// </summary>
public static class TypeRelationshipExtractor
{
    public static NativeTypeRelationships ExtractTypeRelationships(INamedTypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return new NativeTypeRelationships(
            BaseType: type.BaseType?.ToDisplayString(),
            Interfaces: type.Interfaces.Select(i => i.ToDisplayString()).ToArray(),
            OverriddenMethod: null,
            OverriddenProperty: null,
            ExplicitInterfaceImplementations: Array.Empty<string>());
    }

    public static NativeTypeRelationships ExtractMethodRelationships(IMethodSymbol method)
    {
        ArgumentNullException.ThrowIfNull(method);

        return new NativeTypeRelationships(
            BaseType: null,
            Interfaces: Array.Empty<string>(),
            OverriddenMethod: method.OverriddenMethod?.ToDisplayString(),
            OverriddenProperty: null,
            ExplicitInterfaceImplementations: method.ExplicitInterfaceImplementations
                .Select(e => e.ToDisplayString()).ToArray());
    }

    public static NativeTypeRelationships ExtractPropertyRelationships(IPropertySymbol property)
    {
        ArgumentNullException.ThrowIfNull(property);

        return new NativeTypeRelationships(
            BaseType: null,
            Interfaces: Array.Empty<string>(),
            OverriddenMethod: null,
            OverriddenProperty: property.OverriddenProperty?.ToDisplayString(),
            ExplicitInterfaceImplementations: property.ExplicitInterfaceImplementations
                .Select(e => e.ToDisplayString()).ToArray());
    }
}
