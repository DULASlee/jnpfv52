using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JNPF.Tests.Agent;

/// <summary>
/// v5 — Real Roslyn AST-based targeted contract repairer.
///
/// Chief Architect B2 fixes:
///   - NO Regex on method text (previous v4 approach was `Regex.Replace` on
///     `method.ToFullString()`, which was Roslyn-locator + Regex-mutation,
///     NOT Roslyn-mutation).
///   - NO `NormalizeWhitespace` on whole tree (previous v4 reformatted
///     entire file, destroying trivia outside target method).
///   - NO regex-escape double-escape (previous v4 hand-wrote escape
///     sequences AND called Regex.Escape, leading to non-matches).
///
/// Strategy: parse → locate target method → find smallest descendant
/// ExpressionSyntax/StatementSyntax whose ToFullString contains the target
/// text → ReplaceNode with a Roslyn-parsed replacement that has trivia
/// preserved from the original node.
/// </summary>
public sealed class TargetedContractRepairer
{
    public IReadOnlyList<ContractViolation> Diagnose(string filePath)
    {
        var source = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var violations = new List<ContractViolation>();

        // Contract: Query Semantics — taskId filter in Where clause
        if (!source.Contains("a.TaskId == input.taskId"))
        {
            violations.Add(new ContractViolation(
                ContractName: "QuerySemantics.TaskFilter",
                Severity: Severity.Critical,
                DiagnosisMessage: "Where clause missing taskId filter in BuildListQuery",
                TargetMethod: "BuildListQuery",
                TargetSyntaxText: ".Where((a, b) => a.DeleteMark == null)",
                ReplacementSyntaxText: ".Where((a, b) => a.TaskId == input.taskId && a.DeleteMark == null)"));
        }

        // Contract: Soft Delete — 3 DeleteMark filters
        var dmCount = System.Text.RegularExpressions.Regex.Matches(source, @"DeleteMark\s*==\s*null").Count;
        if (dmCount < 3)
        {
            violations.Add(new ContractViolation(
                ContractName: "SoftDelete.ThreeFilters",
                Severity: Severity.Critical,
                DiagnosisMessage: $"Soft delete filter count decreased (expected 3, found {dmCount})",
                TargetMethod: "GetInfo",
                TargetSyntaxText: ".GetFirstAsync(x => x.Id == id)",
                ReplacementSyntaxText: ".GetFirstAsync(x => x.Id == id && x.DeleteMark == null)"));
        }

        // Contract: Entity Lifecycle — Creator
        if (!source.Contains("CallEntityMethod(m => m.Creator())"))
        {
            violations.Add(new ContractViolation(
                ContractName: "EntityLifecycle.Creator",
                Severity: Severity.Critical,
                DiagnosisMessage: "Entity Creator() lifecycle hook missing",
                TargetMethod: "Create",
                TargetSyntaxText: ".AsInsertable(entity).ExecuteCommandAsync()",
                ReplacementSyntaxText: ".AsInsertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync()"));
        }

        // Contract: Entity Lifecycle — LastModify
        if (!source.Contains("CallEntityMethod(m => m.LastModify())"))
        {
            violations.Add(new ContractViolation(
                ContractName: "EntityLifecycle.LastModify",
                Severity: Severity.Critical,
                DiagnosisMessage: "Entity LastModify() lifecycle hook missing",
                TargetMethod: "Update",
                TargetSyntaxText: ".AsUpdateable(entity).IgnoreColumns",
                ReplacementSyntaxText: ".AsUpdateable(entity).CallEntityMethod(m => m.LastModify()).IgnoreColumns"));
        }

        // Contract: Entity Lifecycle — Delete
        if (!source.Contains("CallEntityMethod(m => m.Delete())"))
        {
            violations.Add(new ContractViolation(
                ContractName: "EntityLifecycle.Delete",
                Severity: Severity.Critical,
                DiagnosisMessage: "Entity Delete() lifecycle hook missing",
                TargetMethod: "Delete",
                TargetSyntaxText: ".AsUpdateable(entity).UpdateColumns",
                ReplacementSyntaxText: ".AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns"));
        }

        // Contract: User Context — _userManager.UserId in isDel IIF
        if (!source.Contains("_userManager.UserId"))
        {
            violations.Add(new ContractViolation(
                ContractName: "UserContext.IsDelLogic",
                Severity: Severity.Critical,
                DiagnosisMessage: "User context (UserId) not used — isDel logic broken",
                TargetMethod: "BuildListQuery",
                TargetSyntaxText: "isDel = SqlFunc.IIF(false, false)",
                ReplacementSyntaxText: "isDel = SqlFunc.IIF(a.CreatorUserId == _userManager.UserId, true, false)"));
        }

        return violations;
    }

    /// <summary>
    /// Generate repair using Roslyn ReplaceNode.
    /// Surgical: smallest containing SyntaxNode containing the target text is replaced.
    /// Trivia from the original node is preserved on the replacement.
    /// NO NormalizeWhitespace — file outside target method keeps original formatting.
    /// </summary>
    public TargetedRepair GenerateRepair(string filePath, ContractViolation v)
    {
        var sourceCode = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();

        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == v.TargetMethod)
            ?? throw new InvalidOperationException($"Method {v.TargetMethod} not found in {filePath}");

        // Locate the smallest descendant ExpressionSyntax/StatementSyntax whose
        // text contains the target marker.
        var candidates = method.DescendantNodes()
            .Where(n => n is ExpressionSyntax || n is StatementSyntax)
            .Where(n => n.ToFullString().Contains(v.TargetSyntaxText, StringComparison.Ordinal))
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException(
                $"Pattern '{v.TargetSyntaxText}' not found in method {v.TargetMethod}.");
        }

        var targetNode = candidates.OrderBy(n => n.Span.Length).First();
        var originalText = targetNode.ToFullString();
        var newText = originalText.Replace(v.TargetSyntaxText, v.ReplacementSyntaxText);

        // Build replacement via Roslyn (NOT Regex). Preserve trivia from original.
        SyntaxNode replacementNode;
        if (targetNode is StatementSyntax)
        {
            replacementNode = SyntaxFactory.ParseStatement(newText).WithTriviaFrom(targetNode);
        }
        else
        {
            replacementNode = SyntaxFactory.ParseExpression(newText).WithTriviaFrom(targetNode);
        }

        var newRoot = root.ReplaceNode(targetNode, replacementNode);

        // [B2] NO NormalizeWhitespace — file outside the replaced node keeps original
        // formatting/trivia. The only mutation visible in the diff is the target text.
        var newContent = newRoot.ToFullString();

        var startLine = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = method.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        return new TargetedRepair(
            NewContent: newContent,
            StartLine: startLine,
            EndLine: endLine,
            TargetSyntaxText: v.TargetSyntaxText,
            ReplacementSyntaxText: v.ReplacementSyntaxText,
            Description: $"Restore {v.ContractName} in {v.TargetMethod}()");
    }

    public void ApplyRepair(string filePath, TargetedRepair repair)
    {
        File.WriteAllText(filePath, repair.NewContent);
    }
}

public sealed record ContractViolation(
    string ContractName,
    Severity Severity,
    string DiagnosisMessage,
    string TargetMethod,
    string TargetSyntaxText,
    string ReplacementSyntaxText);

public sealed record TargetedRepair(
    string NewContent,
    int StartLine,
    int EndLine,
    string TargetSyntaxText,
    string ReplacementSyntaxText,
    string Description);

public enum Severity { Critical, Warning, Info }