using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JNPF.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncVoidAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "JNPF006";

    private static readonly LocalizableString Title = "避免 async void 方法";
    private static readonly LocalizableString MessageFormat = "async void 方法的异常无法被捕获，建议改为 async Task";
    private static readonly LocalizableString Description = "async void 方法仅应用于事件处理器。对于其他场景应使用 async Task。";

    private const string Category = "Reliability";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true,
        description: Description,
        helpLinkUri: "https://github.com/jnpf/docs/analyzers/JNPF006");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (!method.IsAsync || !method.ReturnsVoid)
            return;

        var parameters = method.Parameters;
        var isEventHandler = parameters.Length == 2
            && parameters[0].Type.Name == "Object"
            && parameters[1].Type.Name == "EventArgs";

        if (isEventHandler)
            return;

        // 跳过接口实现方法
        if (method.ExplicitInterfaceImplementations.Any() || method.IsOverride)
            return;

        if (method.OverriddenMethod != null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0]));
    }
}
