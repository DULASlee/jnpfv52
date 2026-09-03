using Foundry.FSPM.Compiler.Lexer;
using Foundry.FSPM.Compiler.Parser;
using Foundry.FSPM.Compiler.Syntax;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Phase 12 positive matrix (P12-07 T01-T06): real source strings in,
// observable AST out. Native expressions stay opaque (P12-04).
public sealed class ConstructionParserTests
{
    private static FspmConstructionParseResult Parse(string source)
    {
        var tokens = FspmLexer.Lex(source);
        return new FspmConstructionParser().Parse(tokens, source);
    }

    [Fact]
    public void T01_EmptyPage_ParsesWithZeroDiagnostics()
    {
        var result = Parse("page UserManagement {\n}");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        var page = Assert.Single(result.Document.Pages);
        Assert.Equal("UserManagement", page.Name);
        Assert.Empty(page.Forms);
    }

    [Fact]
    public void T02_PageWithEmptyForm_Parses()
    {
        var result = Parse("page UserManagement {\n    form UserForm {\n    }\n}");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        var form = Assert.Single(Assert.Single(result.Document.Pages).Forms);
        Assert.Equal("UserForm", form.Name);
        Assert.Empty(form.Bindings);
    }

    [Fact]
    public void T03_FormWithEntityBinding_Parses()
    {
        var result = Parse("page UserManagement {\n    form UserForm {\n        entity User\n    }\n}");

        Assert.True(result.Succeeded);
        var binding = Assert.Single(Assert.Single(Assert.Single(result.Document.Pages).Forms).Bindings);
        var entity = Assert.IsType<FspmEntityBindingSyntax>(binding);
        Assert.Equal("User", entity.Expression.Text);
    }

    [Fact]
    public void T04_FormWithFieldBinding_KeepsNativeExpression()
    {
        var result = Parse("page UserManagement {\n    form UserForm {\n        field User.Name\n    }\n}");

        Assert.True(result.Succeeded);
        var binding = Assert.Single(Assert.Single(Assert.Single(result.Document.Pages).Forms).Bindings);
        var field = Assert.IsType<FspmFieldBindingSyntax>(binding);
        Assert.Equal("User.Name", field.Expression.Text);
    }

    [Fact]
    public void T05_FormWithSubmitBinding_ParsesNameAndTarget()
    {
        var result = Parse("page UserManagement {\n    form UserForm {\n        submit Create -> User.Create\n    }\n}");

        Assert.True(result.Succeeded);
        var binding = Assert.Single(Assert.Single(Assert.Single(result.Document.Pages).Forms).Bindings);
        var submit = Assert.IsType<FspmSubmitBindingSyntax>(binding);
        Assert.Equal("Create", submit.Name);
        Assert.Equal("User.Create", submit.Target.Text);
    }

    [Fact]
    public void T06_FullGolden_ParsesExactStructure()
    {
        var source =
            "page UserManagement {\n" +
            "\n" +
            "    form UserForm {\n" +
            "\n" +
            "        entity User\n" +
            "\n" +
            "        field User.Name\n" +
            "        field User.PhoneNumber\n" +
            "        field User.Address\n" +
            "\n" +
            "        submit Create -> User.Create\n" +
            "    }\n" +
            "}";

        var result = Parse(source);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);

        var page = Assert.Single(result.Document.Pages);
        Assert.Equal("UserManagement", page.Name);
        var form = Assert.Single(page.Forms);
        Assert.Equal("UserForm", form.Name);
        Assert.Equal(5, form.Bindings.Count);

        Assert.IsType<FspmEntityBindingSyntax>(form.Bindings[0]);
        var f1 = Assert.IsType<FspmFieldBindingSyntax>(form.Bindings[1]);
        var f2 = Assert.IsType<FspmFieldBindingSyntax>(form.Bindings[2]);
        var f3 = Assert.IsType<FspmFieldBindingSyntax>(form.Bindings[3]);
        var submit = Assert.IsType<FspmSubmitBindingSyntax>(form.Bindings[4]);

        Assert.Equal("User.Name", f1.Expression.Text);
        Assert.Equal("User.PhoneNumber", f2.Expression.Text);
        Assert.Equal("User.Address", f3.Expression.Text);
        Assert.Equal("Create", submit.Name);
        Assert.Equal("User.Create", submit.Target.Text);
    }

    [Fact]
    public void FieldBinding_SpanCoversExactSourceSlice()
    {
        var source = "page P {\n    form F {\n        field User.PhoneNumber\n    }\n}";

        var result = Parse(source);

        Assert.True(result.Succeeded);
        var field = Assert.IsType<FspmFieldBindingSyntax>(
            Assert.Single(Assert.Single(Assert.Single(result.Document.Pages).Forms).Bindings));
        Assert.Equal("field User.PhoneNumber", source.Substring(field.Start, field.Length));
        Assert.Equal("User.PhoneNumber",
            source.Substring(field.Expression.Start, field.Expression.Length));
    }
}
