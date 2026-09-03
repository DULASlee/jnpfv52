namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H4: native type relationships, projected from Roslyn
/// (<c>BaseType</c>, <c>Interfaces</c>, <c>OverriddenMethod</c> /
/// <c>OverriddenProperty</c>, <c>ExplicitInterfaceImplementations</c>).
/// Fact extraction only — no business reasoning.
/// </summary>
public sealed record NativeTypeRelationships(
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    string? OverriddenMethod,
    string? OverriddenProperty,
    IReadOnlyList<string> ExplicitInterfaceImplementations);
