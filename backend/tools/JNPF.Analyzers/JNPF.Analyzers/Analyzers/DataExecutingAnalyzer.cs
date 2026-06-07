using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JNPF.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataExecutingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "JNPF002";

    private static readonly LocalizableString Title = "禁止直接赋值 DataExecuting";
    private static readonly LocalizableString MessageFormat = "禁止直接覆盖 DataExecuting，应使用 ConfigureGlobalDataExecuting 统一配置";
    private static readonly LocalizableString Description = "直接赋值 db.Aop.DataExecuting = handler 会覆盖全局配置，应使用 += 累加或 ConfigureGlobalDataExecuting。";

    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true,
        description: Description,
        helpLinkUri: "https://github.com/jnpf/docs/analyzers/JNPF002");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
    }

    private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;

        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name.Identifier.Text != "DataExecuting")
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.GetLocation()));
    }
}
