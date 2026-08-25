using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JNPF.Audit.Scanner;

/// <summary>
/// Cyclomatic complexity walker — 同源口径复制自 JNPF.Analyzers/Complexity/CyclomaticComplexityWalker.cs
/// （JNPF009 门禁测量器；本工具为 S1-Final 审计一次性只读扫描器，复制以保持口径一致，零改动既有项目）。
/// </summary>
internal sealed class ComplexityWalker : CSharpSyntaxWalker
{
    private int _complexity = 1;

    private ComplexityWalker()
        : base(SyntaxWalkerDepth.Node)
    {
    }

    public static int Compute(SyntaxNode bodyOrExpressionBody)
    {
        if (bodyOrExpressionBody == null)
            return 1;

        var walker = new ComplexityWalker();
        walker.Visit(bodyOrExpressionBody);
        return walker._complexity;
    }

    public override void VisitIfStatement(IfStatementSyntax node)
    {
        _complexity++;
        base.VisitIfStatement(node);
    }

    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        _complexity++;
        base.VisitWhileStatement(node);
    }

    public override void VisitDoStatement(DoStatementSyntax node)
    {
        _complexity++;
        base.VisitDoStatement(node);
    }

    public override void VisitForStatement(ForStatementSyntax node)
    {
        _complexity++;
        base.VisitForStatement(node);
    }

    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        _complexity++;
        base.VisitForEachStatement(node);
    }

    public override void VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
    {
        _complexity++;
        base.VisitForEachVariableStatement(node);
    }

    public override void VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
    {
        _complexity++;
        base.VisitCaseSwitchLabel(node);
    }

    public override void VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
    {
        _complexity++;
        base.VisitCasePatternSwitchLabel(node);
    }

    public override void VisitCatchClause(CatchClauseSyntax node)
    {
        _complexity++;
        base.VisitCatchClause(node);
    }

    public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
    {
        _complexity++;
        base.VisitConditionalExpression(node);
    }

    public override void VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        if (node.IsKind(SyntaxKind.LogicalAndExpression) || node.IsKind(SyntaxKind.LogicalOrExpression)
            || node.IsKind(SyntaxKind.CoalesceExpression))
        {
            _complexity++;
        }

        base.VisitBinaryExpression(node);
    }

    public override void VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
    {
        _complexity++;
        base.VisitSwitchExpressionArm(node);
    }

    public override void VisitWhenClause(WhenClauseSyntax node)
    {
        _complexity++;
        base.VisitWhenClause(node);
    }

    public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        // Local functions are analyzed separately (JNPF009 同源口径); do not fold into enclosing method CC.
    }
}
