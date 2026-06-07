using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JNPF.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BypassOutboxAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "JNPF004";

    private static readonly LocalizableString Title = "[BypassOutbox] 必须注释说明理由";
    private static readonly LocalizableString MessageFormat = "[BypassOutbox] 特性必须附带注释说明绕过 Outbox 的业务理由";

    private const string Category = "Documentation";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true,
        helpLinkUri: "https://github.com/jnpf/docs/analyzers/JNPF004");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;

        var hasBypassOutbox = method.GetAttributes().Any(a =>
            a.AttributeClass?.Name == "BypassOutboxAttribute");

        if (!hasBypassOutbox)
            return;

        var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        if (syntax == null)
            return;

        var trivia = syntax.GetLeadingTrivia();
        var hasComment = trivia.Any(t =>
            t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
            t.IsKind(SyntaxKind.MultiLineCommentTrivia));

        if (!hasComment)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0]));
        }
    }
}
