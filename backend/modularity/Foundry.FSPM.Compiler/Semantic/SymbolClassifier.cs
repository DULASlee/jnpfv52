using Microsoft.CodeAnalysis;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H1: one classification verdict for a real Roslyn symbol.
/// </summary>
public sealed record SymbolClassification(
    NativeSymbolKind Kind,
    NativeTypeKind TypeKind,
    NativeVisibilityFacts Visibility);

/// <summary>
/// P13-H1 entry point: <c>ISymbol → SymbolClassification + VisibilityFacts +
/// CompilationContext</c>. Every field is read from Roslyn; access decisions
/// use Roslyn's own <c>DeclaredAccessibility</c> — P13 implements no C#
/// visibility algorithm.
/// </summary>
public static class SymbolClassifier
{
    public static SymbolClassification Classify(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        return new SymbolClassification(
            MapKind(symbol),
            MapTypeKind(symbol),
            new NativeVisibilityFacts(
                Accessibility: symbol.DeclaredAccessibility.ToString(),
                IsStatic: symbol.IsStatic,
                IsAbstract: symbol.IsAbstract,
                IsVirtual: symbol.IsVirtual,
                IsOverride: symbol.IsOverride,
                IsSealed: symbol.IsSealed,
                IsReadOnly: IsReadOnly(symbol),
                IsConst: symbol is IFieldSymbol { IsConst: true },
                IsAsync: symbol is IMethodSymbol { IsAsync: true },
                IsExtensionMethod: symbol is IMethodSymbol { IsExtensionMethod: true }));
    }

    private static NativeSymbolKind MapKind(ISymbol symbol) => symbol.Kind switch
    {
        SymbolKind.Namespace => NativeSymbolKind.Namespace,
        SymbolKind.NamedType => NativeSymbolKind.Type,
        SymbolKind.Property => symbol is IPropertySymbol { IsIndexer: true }
            ? NativeSymbolKind.Indexer
            : NativeSymbolKind.Property,
        SymbolKind.Field => NativeSymbolKind.Field,
        SymbolKind.Method => symbol is IMethodSymbol { MethodKind: MethodKind.Constructor }
            ? NativeSymbolKind.Constructor
            : NativeSymbolKind.Method,
        SymbolKind.Parameter => NativeSymbolKind.Parameter,
        SymbolKind.Event => NativeSymbolKind.Event,
        _ => NativeSymbolKind.Unknown,
    };

    private static NativeTypeKind MapTypeKind(ISymbol symbol)
    {
        if (symbol is not INamedTypeSymbol type)
        {
            return NativeTypeKind.Unknown;
        }

        return type.TypeKind switch
        {
            TypeKind.Class => type.IsRecord ? NativeTypeKind.Record : NativeTypeKind.Class,
            TypeKind.Struct => type.IsRecord ? NativeTypeKind.RecordStruct : NativeTypeKind.Struct,
            TypeKind.Interface => NativeTypeKind.Interface,
            TypeKind.Enum => NativeTypeKind.Enum,
            TypeKind.Delegate => NativeTypeKind.Delegate,
            _ => NativeTypeKind.Unknown,
        };
    }

    private static bool IsReadOnly(ISymbol symbol) => symbol switch
    {
        IFieldSymbol f => f.IsReadOnly,
        IPropertySymbol p => p.IsReadOnly,
        _ => false,
    };
}
