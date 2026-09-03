using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Security.Cryptography;
using System.Text;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H5 entry point: mints the four separated identities from real
/// Roslyn state. Rules (chief §十五 frozen):
/// <list type="bullet">
/// <item>CompilationIdentity: project + reference set + options + document set + snapshot id.</item>
/// <item>NativeBindingIdentity: assembly-qualified DocId of the CURRENT binding.</item>
/// <item>LogicalSemanticIdentity: assembly + namespace + containing type + member + kind — shape-free.</item>
/// <item>SemanticFingerprint: SHA-256 over kind + full signature + type shape + constraints + nullability.</item>
/// </list>
/// </summary>
public static class SemanticIdentityMint
{
    public static CompilationIdentity MintCompilationIdentity(
        Compilation compilation, string projectName, IReadOnlyList<string> documentPaths, string snapshotId)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        var references = compilation.References
            .Select(r => r.Display ?? r.GetType().Name)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        var language = compilation is CSharpCompilation cs
            ? cs.LanguageVersion.ToString()
            : compilation.Language;

        return new CompilationIdentity(
            ProjectName: projectName,
            AssemblyName: compilation.AssemblyName ?? "<unknown>",
            ReferenceDisplayNames: references,
            OptimizationLevel: compilation.Options.OptimizationLevel.ToString(),
            LanguageVersion: language,
            DocumentPaths: documentPaths.OrderBy(p => p, StringComparer.Ordinal).ToArray(),
            SnapshotId: snapshotId);
    }

    public static NativeBindingIdentity MintBindingIdentity(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var declarationId = DocumentationCommentId.CreateDeclarationId(symbol)
            ?? throw new InvalidOperationException(
                $"Symbol '{symbol.ToDisplayString()}' has no stable DocumentationCommentId.");
        var assembly = symbol.ContainingAssembly?.Name
            ?? throw new InvalidOperationException(
                $"Symbol '{symbol.ToDisplayString()}' has no containing assembly.");

        return new NativeBindingIdentity(assembly, declarationId);
    }

    public static LogicalSemanticIdentity MintLogicalIdentity(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        string? containingType = symbol.ContainingType?.ToDisplayString();
        string memberName;
        string memberKind = symbol.Kind.ToString();
        string ns;

        if (symbol is INamedTypeSymbol type)
        {
            ns = type.ContainingNamespace.ToDisplayString();
            containingType = null;
            // Name, not MetadataName: backtick arity suffixes (`1) are a
            // binding detail, not logical identity. Overloads of one
            // operation intentionally share the logical node; the
            // SemanticFingerprint distinguishes them.
            memberName = type.Name;
        }
        else
        {
            ns = symbol.ContainingNamespace.ToDisplayString();
            memberName = symbol.Name;
            containingType = symbol.ContainingType?.MetadataName;
        }

        return new LogicalSemanticIdentity(
            AssemblyName: symbol.ContainingAssembly?.Name ?? "<unknown>",
            Namespace: ns,
            ContainingTypeName: containingType ?? string.Empty,
            MemberName: memberName,
            MemberKind: memberKind);
    }

    public static SemanticFingerprint MintFingerprint(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var builder = new StringBuilder();
        builder.Append(symbol.Kind).Append('|');
        builder.Append(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append('|');

        switch (symbol)
        {
            case IMethodSymbol method:
                builder.Append(method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append('|');
                builder.Append(method.ReturnType.NullableAnnotation).Append('|');
                foreach (var parameter in method.Parameters)
                {
                    builder.Append(parameter.RefKind).Append(':');
                    builder.Append(parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(':');
                    builder.Append(parameter.NullableAnnotation).Append(';');
                }

                builder.Append("arity=").Append(method.Arity).Append('|');
                foreach (var typeParameter in method.TypeParameters)
                {
                    builder.Append("typeparam:").Append(typeParameter.Name).Append(':');
                    foreach (var constraint in GenericConstraintExtractor.Extract(typeParameter).Constraints)
                    {
                        builder.Append(constraint).Append(',');
                    }

                    builder.Append(';');
                }

                break;
            case IPropertySymbol property:
                builder.Append(property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append('|');
                builder.Append(property.NullableAnnotation);
                break;
            case IFieldSymbol field:
                builder.Append(field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append('|');
                builder.Append(field.NullableAnnotation);
                break;
            case INamedTypeSymbol type:
                builder.Append(type.TypeKind).Append('|');
                builder.Append(type.IsRecord).Append('|');
                foreach (var argument in type.TypeArguments)
                {
                    builder.Append(argument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(';');
                }

                break;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return new SemanticFingerprint(Convert.ToHexString(hash));
    }
}
