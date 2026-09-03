namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H1: visibility + modifier facts read straight from Roslyn
/// (<c>DeclaredAccessibility</c>, <c>IsStatic</c>, …). Accessibility is a
/// plain string (e.g. "Public") so no Roslyn enum leaks into the Fact.
/// </summary>
public sealed record NativeVisibilityFacts(
    string Accessibility,
    bool IsStatic,
    bool IsAbstract,
    bool IsVirtual,
    bool IsOverride,
    bool IsSealed,
    bool IsReadOnly,
    bool IsConst,
    bool IsAsync,
    bool IsExtensionMethod);
