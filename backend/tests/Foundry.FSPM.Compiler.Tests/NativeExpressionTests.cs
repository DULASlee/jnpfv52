using Foundry.FSPM.Compiler.Lexer;
using Foundry.FSPM.Compiler.Parser;
using Foundry.FSPM.Compiler.Syntax;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Phase 12 native-expression boundary (P12-08): each expression must cross
// the Parser as one opaque FspmNativeExpressionSyntax with verbatim text.
// The Parser MUST NOT split receiver/member/arguments (no '.' analysis).
public sealed class NativeExpressionTests
{
    private static string FieldExpression(string expression)
    {
        var source = $"page P {{\n    form F {{\n        field {expression}\n    }}\n}}";
        var result = new FspmConstructionParser().Parse(FspmLexer.Lex(source), source);

        Assert.True(result.Succeeded);
        var field = Assert.IsType<FspmFieldBindingSyntax>(
            Assert.Single(Assert.Single(Assert.Single(result.Document.Pages).Forms).Bindings));
        return field.Expression.Text;
    }

    [Theory]
    [InlineData("User.Name")]
    [InlineData("User.PhoneNumber")]
    [InlineData("User.Address.City")]
    [InlineData("User.Create(user)")]
    [InlineData("User.Update(user)")]
    [InlineData("obj?.Property")]
    [InlineData("obj[0].Property")]
    [InlineData("((User)obj).PhoneNumber")]
    public void NativeExpression_CrossesBoundaryVerbatim(string expression)
    {
        Assert.Equal(expression, FieldExpression(expression));
    }

    [Fact]
    public void NativeExpression_IsSingleOpaqueNode_NotSplit()
    {
        var source = "page P {\n    form F {\n        field User.PhoneNumber\n    }\n}";
        var result = new FspmConstructionParser().Parse(FspmLexer.Lex(source), source);

        Assert.True(result.Succeeded);
        var field = Assert.IsType<FspmFieldBindingSyntax>(
            Assert.Single(Assert.Single(Assert.Single(result.Document.Pages).Forms).Bindings));

        // Exactly one child expression node; receiver/member are NOT separate nodes.
        Assert.IsType<FspmNativeExpressionSyntax>(field.Expression);
        Assert.Equal("User.PhoneNumber", field.Expression.Text);
    }

    [Fact]
    public void SubmitTarget_WithCallArguments_StaysOpaque()
    {
        var source = "page P {\n    form F {\n        submit Create -> User.Create(user)\n    }\n}";
        var result = new FspmConstructionParser().Parse(FspmLexer.Lex(source), source);

        Assert.True(result.Succeeded);
        var submit = Assert.IsType<FspmSubmitBindingSyntax>(
            Assert.Single(Assert.Single(Assert.Single(result.Document.Pages).Forms).Bindings));
        Assert.Equal("Create", submit.Name);
        Assert.Equal("User.Create(user)", submit.Target.Text);
    }
}
