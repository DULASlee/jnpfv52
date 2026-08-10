using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JNPF.Analyzers;

/// <summary>
/// JNPF009: Incremental cyclomatic-complexity gate (threshold 30) with complexity-baseline.json exemptions.
/// New / unlisted methods with CC ≥ threshold → error; baselined methods fail only if CC exceeds maxComplexity.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComplexityAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "JNPF009";

    private static readonly LocalizableString Title = "方法圈复杂度超过门禁阈值";
    private static readonly LocalizableString MessageFormat =
        "方法 '{0}' 圈复杂度为 {1}，超过阈值 {2}（基线上限 {3}）。请拆分方法或更新 complexity-baseline.json（只许下降）";
    private static readonly LocalizableString Description =
        "W0 复杂度止损：新增/升高 CC≥30 的方法必须先测后拆；存量见 complexity-baseline.json。";

    private const string Category = "Maintainability";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/jnpf/docs/analyzers/JNPF009");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var baseline = ComplexityBaseline.Load(start.Options.AdditionalFiles);
            start.RegisterSyntaxNodeAction(
                ctx => AnalyzeMethod(ctx, baseline),
                SyntaxKind.MethodDeclaration);
            start.RegisterSyntaxNodeAction(
                ctx => AnalyzeLocalFunction(ctx, baseline),
                SyntaxKind.LocalFunctionStatement);
        });
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, ComplexityBaseline baseline)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        if (method.Body is null && method.ExpressionBody is null)
            return;

        // Skip abstract / extern / partial without body already handled
        var cc = CyclomaticComplexityWalker.Compute(method);
        ReportIfNeeded(context, method.Identifier.GetLocation(), method.Identifier.Text, cc, baseline,
            context.Node.SyntaxTree.FilePath);
    }

    private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context, ComplexityBaseline baseline)
    {
        var local = (LocalFunctionStatementSyntax)context.Node;
        var cc = CyclomaticComplexityWalker.Compute(local);
        ReportIfNeeded(context, local.Identifier.GetLocation(), local.Identifier.Text, cc, baseline,
            context.Node.SyntaxTree.FilePath);
    }

    private static void ReportIfNeeded(
        SyntaxNodeAnalysisContext context,
        Location location,
        string methodName,
        int cc,
        ComplexityBaseline baseline,
        string filePath)
    {
        var threshold = baseline.Threshold > 0 ? baseline.Threshold : 30;

        // Stock severe methods (check02 seed): exempt while listed.
        // W0 hard gate = block new/unlisted methods with CC ≥ threshold.
        // maxComplexity retained for quarterly reduction / future tighten (fail if CC > max).
        if (baseline.TryGetMaxComplexity(filePath, methodName, out _))
            return;

        if (cc < threshold)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule, location, methodName, cc, threshold, "无基线"));
    }
}
