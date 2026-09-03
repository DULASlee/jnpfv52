namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H1: native symbol classification. Direct mapping of Roslyn
/// <c>SymbolKind</c>; P13 never invents kinds.
/// </summary>
public enum NativeSymbolKind
{
    Unknown = 0,
    Namespace,
    Type,
    Property,
    Field,
    Method,
    Constructor,
    Parameter,
    Event,
    Indexer,
}

/// <summary>
/// P13-H1: native type-kind classification. Direct mapping of Roslyn
/// <c>TypeKind</c> for the shapes FSPM cares about.
/// </summary>
public enum NativeTypeKind
{
    Unknown = 0,
    Class,
    Struct,
    Record,
    RecordStruct,
    Interface,
    Enum,
    Delegate,
}
