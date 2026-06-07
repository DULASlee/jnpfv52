using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JNPF.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CreateScopeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "JNPF003";

    private static readonly LocalizableString Title = "禁止直接创建 ServiceScope";
    private static readonly LocalizableString MessageFormat = "禁止直接调用 CreateScope()，应使用构造函数注入的 Scoped 服务管理生命周期";
    private static readonly LocalizableString Description = "手动创建 Scope 绕过 DI 容器生命周期管理，可能导致资源泄漏。";

    private const string Category = "Design";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true,
        description: Description,
        helpLinkUri: "https://github.com/jnpf/docs/analyzers/JNPF003");

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

        if (memberAccess.Name.Identifier.Text == "CreateScope")
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }
}
