using Foundry.FSPM.Compiler.Syntax;
using System.Reflection;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// Phase 3 AST / Syntax Model tests (施工包 §10-§14).
/// Hard constraint: AST nodes must NOT contain Semantic / Symbol references.
/// These tests construct AST nodes directly (no Parser in Phase 3 yet).
/// </summary>
public sealed class SyntaxTests
{
    private static readonly string[] ForbiddenSemanticMembers =
    {
        "Symbol", "Symbols", "BoundSymbol", "Binding",
        "Semantic", "Semantics", "SemanticModel",
        "Resolved", "Resolution", "ResolutionState",
        "Found", "INamedTypeSymbol", "IPropertySymbol", "IMethodSymbol",
        "ContainingType", "ContainingSymbol", "ReturnType",
    };

    // ===== Hierarchy =====

    [Fact]
    public void EntityDeclaration_InheritsFrom_FspmSyntaxNode()
    {
        var node = new FspmEntityDeclarationSyntax("User", 0, 11, 1, 1);
        Assert.IsAssignableFrom<FspmSyntaxNode>(node);
    }

    [Fact]
    public void PropertyDeclaration_InheritsFrom_FspmSyntaxNode()
    {
        var node = new FspmPropertyDeclarationSyntax("User", "UserName", 0, 22, 1, 1);
        Assert.IsAssignableFrom<FspmSyntaxNode>(node);
    }

    [Fact]
    public void OperationDeclaration_InheritsFrom_FspmSyntaxNode()
    {
        var node = new FspmOperationDeclarationSyntax("User", "Login", 0, 19, 1, 1);
        Assert.IsAssignableFrom<FspmSyntaxNode>(node);
    }

    [Fact]
    public void CompilationUnit_InheritsFrom_FspmSyntaxNode()
    {
        var unit = new FspmCompilationUnitSyntax(
            Array.Empty<FspmSyntaxNode>(),
            0, 0, 1, 1);
        Assert.IsAssignableFrom<FspmSyntaxNode>(unit);
    }

    // ===== Field accessibility =====

    [Fact]
    public void EntityDeclaration_Exposes_Name_Start_Length_Line_Column()
    {
        var node = new FspmEntityDeclarationSyntax(
            Name: "User",
            Start: 0, Length: 11, Line: 1, Column: 1);

        Assert.Equal("User", node.Name);
        Assert.Equal(0, node.Start);
        Assert.Equal(11, node.Length);
        Assert.Equal(1, node.Line);
        Assert.Equal(1, node.Column);
    }

    [Fact]
    public void PropertyDeclaration_Exposes_EntityName_PropertyName_Position()
    {
        var node = new FspmPropertyDeclarationSyntax(
            EntityName: "User",
            PropertyName: "UserName",
            Start: 0, Length: 22, Line: 1, Column: 1);

        Assert.Equal("User", node.EntityName);
        Assert.Equal("UserName", node.PropertyName);
        Assert.Equal(0, node.Start);
        Assert.Equal(22, node.Length);
        Assert.Equal(1, node.Line);
        Assert.Equal(1, node.Column);
    }

    [Fact]
    public void OperationDeclaration_Exposes_EntityName_OperationName_Position()
    {
        var node = new FspmOperationDeclarationSyntax(
            EntityName: "User",
            OperationName: "Login",
            Start: 0, Length: 19, Line: 1, Column: 1);

        Assert.Equal("User", node.EntityName);
        Assert.Equal("Login", node.OperationName);
        Assert.Equal(0, node.Start);
        Assert.Equal(19, node.Length);
        Assert.Equal(1, node.Line);
        Assert.Equal(1, node.Column);
    }

    // ===== Source position traceability (Hard Constraint) =====

    [Fact]
    public void EntityDeclaration_OnLine2_HasCorrectLineAndColumn()
    {
        var node = new FspmEntityDeclarationSyntax(
            "User",
            Start: 1, Length: 11, Line: 2, Column: 1);

        Assert.Equal(2, node.Line);
        Assert.Equal(1, node.Column);
    }

    [Fact]
    public void PropertyDeclaration_OnLine3_HasCorrectLineAndColumn()
    {
        var node = new FspmPropertyDeclarationSyntax(
            "Order", "Id",
            Start: 50, Length: 16, Line: 3, Column: 5);

        Assert.Equal(3, node.Line);
        Assert.Equal(5, node.Column);
        Assert.Equal(50, node.Start);
        Assert.Equal(16, node.Length);
    }

    [Fact]
    public void SliceOfSource_ReproducesDeclarationText()
    {
        const string source = "property User.PhoneNumber";
        var node = new FspmPropertyDeclarationSyntax(
            "User", "PhoneNumber",
            Start: 0, Length: source.Length, Line: 1, Column: 1);

        var slice = source.Substring(node.Start, node.Length);
        Assert.Equal("property User.PhoneNumber", slice);
    }

    // ===== CompilationUnit aggregation =====

    [Fact]
    public void CompilationUnit_AggregatesDeclarations()
    {
        var decls = new FspmSyntaxNode[]
        {
            new FspmEntityDeclarationSyntax("User", 0, 11, 1, 1),
            new FspmPropertyDeclarationSyntax("User", "UserName", 12, 21, 2, 1),
            new FspmOperationDeclarationSyntax("User", "Login", 34, 18, 3, 1),
        };

        var unit = new FspmCompilationUnitSyntax(
            decls, Start: 0, Length: 52, Line: 1, Column: 1);

        Assert.Equal(3, unit.Declarations.Count);
        Assert.IsType<FspmEntityDeclarationSyntax>(unit.Declarations[0]);
        Assert.IsType<FspmPropertyDeclarationSyntax>(unit.Declarations[1]);
        Assert.IsType<FspmOperationDeclarationSyntax>(unit.Declarations[2]);
    }

    [Fact]
    public void CompilationUnit_CanBeEmpty()
    {
        var unit = new FspmCompilationUnitSyntax(
            Array.Empty<FspmSyntaxNode>(), 0, 0, 1, 1);

        Assert.Empty(unit.Declarations);
    }

    // ===== Record semantics =====

    [Fact]
    public void EntityDeclaration_HasValueEquality()
    {
        var a = new FspmEntityDeclarationSyntax("User", 0, 11, 1, 1);
        var b = new FspmEntityDeclarationSyntax("User", 0, 11, 1, 1);
        var c = new FspmEntityDeclarationSyntax("Order", 0, 12, 1, 1);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void CompilationUnit_HasValueEquality_WhenDeclarationsArrayIsSameInstance()
    {
        // Note: record equality on IReadOnlyList<T> uses reference equality on the
        // list itself. Sharing the same array instance is therefore a valid equality
        // scenario. Element-wise equality of different list instances is intentionally
        // NOT a feature of the record type in Phase 3.
        var sharedDecls = new FspmSyntaxNode[] { new FspmEntityDeclarationSyntax("User", 0, 11, 1, 1) };

        var a = new FspmCompilationUnitSyntax(sharedDecls, 0, 11, 1, 1);
        var b = new FspmCompilationUnitSyntax(sharedDecls, 0, 11, 1, 1);

        Assert.Equal(a, b);
    }

    // ===== Hard architectural constraint: NO Semantic / Symbol references =====

    [Theory]
    [InlineData(typeof(FspmEntityDeclarationSyntax))]
    [InlineData(typeof(FspmPropertyDeclarationSyntax))]
    [InlineData(typeof(FspmOperationDeclarationSyntax))]
    [InlineData(typeof(FspmCompilationUnitSyntax))]
    public void Node_MustNotExposeAnySymbolOrSemanticField(Type nodeType)
    {
        // Phase 3 hard constraint: AST 不得提前变成 Semantic Model.
        // Any field/property whose name suggests Symbol/Binding/Semantic/Resolved/Found/Type
        // is a hard architectural violation that must be rejected here.
        var members = nodeType
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var f in ForbiddenSemanticMembers)
        {
            Assert.False(
                members.Contains(f),
                $"AST node {nodeType.Name} exposes forbidden member: {f}. " +
                $"Phase 3 hard constraint: AST must NOT contain Semantic / Symbol references.");
        }
    }

    [Fact]
    public void EntityDeclaration_ExposesAllDocumentedUserFacingFields()
    {
        // Positive counterpart of Node_MustNotExposeAnySymbolOrSemanticField.
        // We assert the documented user-facing fields are present, without
        // requiring exhaustive match on record-generated members.
        var entityMembers = typeof(FspmEntityDeclarationSyntax)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        var requiredUserFacing = new[] { "Name", "Start", "Length", "Line", "Column" };
        foreach (var f in requiredUserFacing)
        {
            Assert.True(
                entityMembers.Contains(f),
                $"FspmEntityDeclarationSyntax must expose user-facing field: {f}");
        }
    }

    // ===== Negative tests: bad construction inputs =====

    [Fact]
    public void EntityDeclaration_AcceptsNullName_AsCompilerSafetyNet()
    {
        // Phase 3 invariant: AST nodes are data carriers; they do not validate Name.
        // Validation belongs to Phase 5 Diagnostics, not to Syntax.
        var node = new FspmEntityDeclarationSyntax(null!, 0, 11, 1, 1);
        Assert.Null(node.Name);
    }

    [Fact]
    public void PropertyDeclaration_AcceptsNullNames_AsCompilerSafetyNet()
    {
        var node = new FspmPropertyDeclarationSyntax(null!, null!, 0, 22, 1, 1);
        Assert.Null(node.EntityName);
        Assert.Null(node.PropertyName);
    }
}
