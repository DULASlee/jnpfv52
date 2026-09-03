using Foundry.FSPM.Compiler.Compiler;
using Foundry.FSPM.Compiler.Syntax;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// G13-LEXER-LOC-01: verification only (no lexer rework). LF vs CRLF must
// produce identical AST; unterminated strings (+LF/+CRLF) must surface as
// diagnostics with real Line/Column/Span — never raw exceptions.
public sealed class LexerLocationVerificationTests
{
    private const string GoldenLf =
        "page UserManagement {\n" +
        "    form UserForm {\n" +
        "        entity User\n" +
        "        field User.PhoneNumber\n" +
        "        submit Create -> User.Create\n" +
        "    }\n" +
        "}";

    private static string Fingerprint(FspmConstructionDocumentSyntax document)
    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (var page in document.Pages)
        {
            parts.Add("P:" + page.Name);
            foreach (var form in page.Forms)
            {
                parts.Add("F:" + form.Name);
                foreach (var binding in form.Bindings)
                {
                    parts.Add(binding switch
                    {
                        FspmEntityBindingSyntax e => "E:" + e.Expression.Text,
                        FspmFieldBindingSyntax f => "FL:" + f.Expression.Text,
                        FspmSubmitBindingSyntax s => "S:" + s.Name + "->" + s.Target.Text,
                        _ => "?:" + binding.GetType().Name,
                    });
                }
            }
        }

        return string.Join(";", parts);
    }

    [Fact]
    public void Lf_And_Crlf_Produce_Identical_Ast()
    {
        var crlf = GoldenLf.Replace("\n", "\r\n");

        var lfResult = FspmConstructionCompiler.Parse(GoldenLf);
        var crlfResult = FspmConstructionCompiler.Parse(crlf);

        Assert.True(lfResult.Succeeded);
        Assert.True(crlfResult.Succeeded);
        Assert.Equal(Fingerprint(lfResult.Document), Fingerprint(crlfResult.Document));
    }

    [Fact]
    public void UnterminatedString_With_Lf_Yields_Diagnostic_With_Location()
    {
        var result = FspmConstructionCompiler.Parse(
            "page \"UserManagement\n    form F {\n}");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
        var diagnostic = result.Diagnostics[0];
        Assert.True(diagnostic.Line >= 1);
        Assert.True(diagnostic.Column >= 1);
    }

    [Fact]
    public void UnterminatedString_With_Crlf_Yields_Diagnostic_With_Location()
    {
        var result = FspmConstructionCompiler.Parse(
            "page \"UserManagement\r\n    form F {\r\n}");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
        var diagnostic = result.Diagnostics[0];
        Assert.True(diagnostic.Line >= 1);
        Assert.True(diagnostic.Column >= 1);
    }

    [Fact]
    public void MalformedSource_Diagnostic_Carries_Span()
    {
        var result = FspmConstructionCompiler.Parse("page P {\n    entity User\n}");

        Assert.False(result.Succeeded);
        var diagnostic = result.Diagnostics[0];
        Assert.True(diagnostic.Start >= 0);
        Assert.True(diagnostic.Length > 0);
    }
}
