using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JNPF.Analyzers;

/// <summary>
/// 需求分析子链守卫分析器（req-analysis-iron-law.md 禁令编译期检测）。
/// JNPF007: 禁止定义废止模块类（ScannerValidator / EventDependencyBuilder / PSpecEnhancer 等）。
/// JNPF008: 禁止给 ClarificationQuestion.QuestionFormat 赋值 "SINGLE"（非 MATRIX_SINGLE）。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RequirementAnalysisGuardAnalyzer : DiagnosticAnalyzer
{
    // JNPF007 — 废止模块类定义检测（25 §0.2 / req-analysis-iron-law.md 禁令七）
    public const string ProhibitedClassId = "JNPF007";
    private static readonly LocalizableString ProhibitedClassTitle = "禁止定义废止模块类";
    private static readonly LocalizableString ProhibitedClassMessage =
        "类 '{0}' 已被 25 号 §0.2 废止，禁止再定义（req-analysis-iron-law.md 禁令七）";

    private const string ProhibitedClassCategory = "RequirementAnalysis";

    private static readonly DiagnosticDescriptor ProhibitedClassRule = new(
        ProhibitedClassId, ProhibitedClassTitle, ProhibitedClassMessage, ProhibitedClassCategory,
        DiagnosticSeverity.Error, true,
        helpLinkUri: "https://github.com/jnpf/docs/analyzers/JNPF007");

    // JNPF008 — 普通 SINGLE 题型赋值检测（25 红线1 / 31 D-E）
    public const string SingleFormatId = "JNPF008";
    private static readonly LocalizableString SingleFormatTitle = "禁止普通 SINGLE 题型";
    private static readonly LocalizableString SingleFormatMessage =
        "QuestionFormat = \"SINGLE\" 违反题型约束（25 红线1 / 31 D-E）；仅允许 MULTI / MATRIX_SINGLE / MATRIX_MULTI";

    private const string SingleFormatCategory = "RequirementAnalysis";

    private static readonly DiagnosticDescriptor SingleFormatRule = new(
        SingleFormatId, SingleFormatTitle, SingleFormatMessage, SingleFormatCategory,
        DiagnosticSeverity.Error, true,
        helpLinkUri: "https://github.com/jnpf/docs/analyzers/JNPF008");

    // 25 §0.2 废止类清单
    private static readonly HashSet<string> ProhibitedClassNames = new()
    {
        "ScannerValidator",
        "EventDependencyBuilder",
        "PSpecEnhancer",
        "DecisionTableEnhancer",
        "NoopEnhancer",
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ProhibitedClassRule, SingleFormatRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        // 检测字符串字面量 "SINGLE"（覆盖普通赋值 + 匿名对象初始化器 + 命名参数）
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
    }

    /// <summary>
    /// JNPF007: 检测类/接口/record 定义命中废止清单。
    /// </summary>
    private void AnalyzeType(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;
        if (ProhibitedClassNames.Contains(typeSymbol.Name))
        {
            var location = typeSymbol.Locations.FirstOrDefault();
            if (location != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(ProhibitedClassRule, location, typeSymbol.Name));
            }
        }
        // 接口也检测（ISaStepEnhancer 等）
        if (typeSymbol.Name is "ISaStepEnhancer" or "ISaStepContext")
        {
            var location = typeSymbol.Locations.FirstOrDefault();
            if (location != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(ProhibitedClassRule, location, typeSymbol.Name));
            }
        }
    }

    /// <summary>
    /// JNPF008: 检测 QuestionFormat = "SINGLE" 赋值（非模式匹配、非比较）。
    /// 只在"赋值表达式右侧"或"匿名对象成员初始化器右侧"触发，排除 is/case 模式匹配和 == 比较。
    /// </summary>
    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var literal = (Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax)context.Node;
        var tokenText = literal.Token.ValueText;

        // 精确匹配 "SINGLE"（排除 MATRIX_SINGLE / MATRIX_MULTI）
        if (!string.Equals(tokenText, "SINGLE", System.StringComparison.Ordinal))
            return;

        // 只在赋值右侧触发（排除模式匹配 is/case 和 == 比较）
        var parent = literal.Parent;
        // 赋值表达式：a.QuestionFormat = "SINGLE"
        if (parent is Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax assignment
            && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && assignment.Right == literal
            && assignment.Left.ToString().Contains("QuestionFormat"))
        {
            context.ReportDiagnostic(Diagnostic.Create(SingleFormatRule, literal.GetLocation()));
            return;
        }
        // 匿名对象成员初始化器：new { QuestionFormat = "SINGLE" }
        if (parent is Microsoft.CodeAnalysis.CSharp.Syntax.AnonymousObjectMemberDeclaratorSyntax anonDecl
            && anonDecl.NameEquals?.Name?.ToString() == "QuestionFormat")
        {
            context.ReportDiagnostic(Diagnostic.Create(SingleFormatRule, literal.GetLocation()));
            return;
        }
        // 命名参数：M(QuestionFormat: "SINGLE")
        if (parent is Microsoft.CodeAnalysis.CSharp.Syntax.NameColonSyntax nameColon
            && nameColon.Name?.ToString() == "QuestionFormat")
        {
            context.ReportDiagnostic(Diagnostic.Create(SingleFormatRule, literal.GetLocation()));
            return;
        }
        // 其他上下文（模式匹配 is "SINGLE"、比较 == "SINGLE"、switch case）不触发
    }
}
