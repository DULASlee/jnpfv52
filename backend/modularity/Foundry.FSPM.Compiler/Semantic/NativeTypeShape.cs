namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H2: native type-shape discriminator. Every value maps 1:1 from a
/// Roslyn <c>ITypeSymbol</c> subtype; P13 adds no synthetic shapes.
/// </summary>
public enum NativeTypeShapeKind
{
    Unknown = 0,
    NamedType,
    ConstructedGeneric,
    TypeParameter,
    Array,
    Pointer,
    Tuple,
    Nullable,
}

/// <summary>
/// P13-H2: immutable native type shape. All fields are plain strings /
/// string lists — no Roslyn type objects leak into the Fact.
/// <list type="bullet">
/// <item>OriginalDefinition: e.g. "System.Collections.Generic.List&lt;T&gt;" or "SemanticGolden.Shapes.ShapeUser".</item>
/// <item>TypeArguments: display strings of constructed arguments, nullability preserved ("ShapeUser?").</item>
/// <item>NullableAnnotation: "Annotated" / "NotAnnotated" / "Oblivious" / "None".</item>
/// </list>
/// </summary>
public sealed record NativeTypeShape(
    NativeTypeShapeKind Kind,
    string OriginalDefinition,
    IReadOnlyList<string> TypeArguments,
    string? ContainingType,
    string? BaseType,
    int ArrayRank,
    string? ElementType,
    IReadOnlyList<string> TupleElementNames,
    string NullableAnnotation);

/// <summary>
/// P13-H2: generic constraint facts as Roslyn-reported display strings
/// ("class", "struct", "notnull", "unmanaged", "new()", type names).
/// </summary>
public sealed record GenericConstraintFacts(
    IReadOnlyList<string> Constraints);
