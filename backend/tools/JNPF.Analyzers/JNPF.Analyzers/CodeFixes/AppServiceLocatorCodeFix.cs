using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace JNPF.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AppServiceLocatorCodeFix)), Shared]
public sealed class AppServiceLocatorCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AppServiceLocatorAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var invocation = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "使用构造函数注入替换",
                createChangedDocument: c => AddConstructorInjectionAsync(context.Document, root, invocation, c),
                equivalenceKey: nameof(AppServiceLocatorCodeFix)),
            diagnostic);
    }

    private static async Task<Document> AddConstructorInjectionAsync(
        Document document, SyntaxNode root, InvocationExpressionSyntax invocation, CancellationToken ct)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        var typeArg = memberAccess.Name is GenericNameSyntax genericName
            ? genericName.TypeArgumentList.Arguments.First().ToString()
            : null;

        if (typeArg == null) return document;

        var editor = await DocumentEditor.CreateAsync(document, ct);
        var parentClass = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (parentClass == null) return document;

        var fieldName = "_" + char.ToLower(typeArg[0]) + typeArg.Substring(1).Replace(".", "");

        editor.ReplaceNode(invocation,
            SyntaxFactory.IdentifierName(fieldName));

        return editor.GetChangedDocument();
    }
}
