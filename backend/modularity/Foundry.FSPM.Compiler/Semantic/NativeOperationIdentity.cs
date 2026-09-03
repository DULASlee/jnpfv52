namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H3: single parameter fact. All strings are Roslyn display strings;
/// no Roslyn type objects.
/// </summary>
public sealed record NativeParameterFact(
    string Name,
    string ParameterType,
    string RefKind,
    bool IsOptional,
    string? DefaultValue,
    bool IsParams);

/// <summary>
/// P13-H3: complete operation identity projected from Roslyn
/// <c>IMethodSymbol</c> (or constructor). <c>StableId</c> reuses the
/// Phase 7 <c>FspmSymbolId</c> canonical form (DocId + assembly), so
/// overloads that differ only in signature already differ in identity.
/// </summary>
public sealed record NativeOperationIdentity(
    string ContainingType,
    string Name,
    int Arity,
    IReadOnlyList<NativeParameterFact> Parameters,
    string ReturnType,
    IReadOnlyList<string> GenericParameters,
    NativeSymbolKind Kind,
    string StableId);

/// <summary>
/// P13-H3: extension-method facts straight from Roslyn
/// (<c>IsExtensionMethod</c>, <c>ReducedFrom</c>, receiver type).
/// </summary>
public sealed record ExtensionMethodFacts(
    bool IsExtensionMethod,
    string ReducedFrom,
    string ReceiverType);
