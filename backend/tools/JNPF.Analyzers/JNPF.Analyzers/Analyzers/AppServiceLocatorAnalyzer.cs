using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JNPF.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AppServiceLocatorAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "JNPF001";

    private static readonly LocalizableString Title = "禁止使用 Service Locator 模式";
    private static readonly LocalizableString MessageFormat = "禁止直接调用 App.GetService<T>() 或 App.GetRequiredService<T>()，请通过构造函数注入";
    private static readonly LocalizableString Description = "Service Locator 反模式隐藏依赖关系，使单元测试困难。应使用构造函数注入。";

    private const string Category = "Design";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true,
        description: Description,
        helpLinkUri: "https://github.com/jnpf/docs/analyzers/JNPF001");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName != "GetService" && methodName != "GetRequiredService")
            return;

        if (memberAccess.Expression is not IdentifierNameSyntax identifier)
            return;

        if (identifier.Identifier.Text == "App")
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }
}
