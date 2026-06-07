using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JNPF.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DirectSqlSugarAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "JNPF005";

    private static readonly LocalizableString Title = "建议通过 Repository 访问数据库";
    private static readonly LocalizableString MessageFormat = "构造函数直接注入 ISqlSugarClient，建议改用 ISqlSugarRepository<T> 访问数据库";

    private const string Category = "Design";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true,
        helpLinkUri: "https://github.com/jnpf/docs/analyzers/JNPF005");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeConstructor, SymbolKind.Method);
    }

    private void AnalyzeConstructor(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.MethodKind != MethodKind.Constructor)
            return;

        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type.Name == "ISqlSugarClient")
            {
                var location = parameter.Locations.FirstOrDefault();
                if (location != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, location));
                }
            }
        }
    }
}
