using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H2 entry points: <c>ExtractTypeShape(ITypeSymbol)</c> and generic
/// constraint extraction. Roslyn has already computed nullability,
/// constructed arguments, ranks and constraints — P13 only projects them
/// into <see cref="NativeTypeShape"/> strings. No generic inference is
/// reimplemented.
/// </summary>
public static class TypeShapeExtractor
{
    public static NativeTypeShape ExtractTypeShape(ITypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var kind = MapKind(type);
        var named = type as INamedTypeSymbol;

        var arguments = named is not null && named.IsGenericType
            ? named.TypeArguments.Select(a => a.ToDisplayString()).ToArray()
            : Array.Empty<string>();

        var tupleNames = named is not null && named.IsTupleType
            ? named.TupleElements.Select(e => e.Name ?? string.Empty).ToArray()
            : Array.Empty<string>();

        var array = type as IArrayTypeSymbol;
        var displayOriginal = named?.OriginalDefinition.ToDisplayString()
            ?? type.OriginalDefinition.ToDisplayString();

        return new NativeTypeShape(
            Kind: kind,
            OriginalDefinition: displayOriginal,
            TypeArguments: arguments,
            ContainingType: type.ContainingType?.ToDisplayString(),
            BaseType: (type as INamedTypeSymbol)?.BaseType?.ToDisplayString(),
            ArrayRank: array?.Rank ?? 0,
            ElementType: array?.ElementType.ToDisplayString(),
            TupleElementNames: tupleNames,
            NullableAnnotation: type.NullableAnnotation.ToString(),
            Arity: (type as INamedTypeSymbol)?.Arity ?? 0);
    }

    private static NativeTypeShapeKind MapKind(ITypeSymbol type) => type switch
    {
        IPointerTypeSymbol => NativeTypeShapeKind.Pointer,
        IArrayTypeSymbol => NativeTypeShapeKind.Array,
        ITypeParameterSymbol => NativeTypeShapeKind.TypeParameter,
        INamedTypeSymbol named when named.IsTupleType => NativeTypeShapeKind.Tuple,
        INamedTypeSymbol named when named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            => NativeTypeShapeKind.Nullable,
        INamedTypeSymbol named when named.IsGenericType && !SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, named)
            => NativeTypeShapeKind.ConstructedGeneric,
        INamedTypeSymbol => NativeTypeShapeKind.NamedType,
        _ => NativeTypeShapeKind.Unknown,
    };
}

/// <summary>
/// P13-H2: reads <c>ITypeParameterSymbol</c> constraint facts from Roslyn.
/// </summary>
public static class GenericConstraintExtractor
{
    public static GenericConstraintFacts Extract(ITypeParameterSymbol parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        var list = new List<string>();
        if (parameter.HasReferenceTypeConstraint) list.Add("class");
        if (parameter.HasValueTypeConstraint) list.Add("struct");
        if (parameter.HasNotNullConstraint) list.Add("notnull");
        if (parameter.HasUnmanagedTypeConstraint) list.Add("unmanaged");
        if (parameter.HasConstructorConstraint) list.Add("new()");

        // Roslyn 4.8 exposes variance via ReferenceTypeConstraint etc.;
        // type constraints come from ConstraintTypes (empty in 4.8 for
        // `where T : IShape`? No — ConstraintTypes carries them).
        foreach (var constraint in parameter.ConstraintTypes)
        {
            list.Add(constraint.ToDisplayString());
        }

        return new GenericConstraintFacts(list.ToArray());
    }
}
