using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace JNPF.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncVoidCodeFix)), Shared]
public sealed class AsyncVoidCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AsyncVoidAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "将 async void 改为 async Task",
                createChangedDocument: c => ChangeToAsyncTaskAsync(context.Document, root, methodDeclaration, c),
                equivalenceKey: nameof(AsyncVoidCodeFix)),
            diagnostic);
    }

    private static async Task<Document> ChangeToAsyncTaskAsync(
        Document document, SyntaxNode root, MethodDeclarationSyntax method, CancellationToken ct)
    {
        var newReturnType = SyntaxFactory.ParseTypeName("Task")
            .WithTrailingTrivia(SyntaxFactory.Space);

        var newMethod = method.WithReturnType(newReturnType);

        root = root.ReplaceNode(method, newMethod);

        var editor = await DocumentEditor.CreateAsync(document, ct);
        return editor.GetChangedDocument();
    }
}
