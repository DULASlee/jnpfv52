using Foundry.FSPM.Compiler.Compiler;
using Foundry.FSPM.Compiler.Syntax;
using System.Linq;
using System.Text;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Phase 12 black-box (P12-14) + determinism (P12-21): only the public
// facade is exercised (Input Source → ConstructionCompiler → Observable
// AST). Fingerprint is a test-local debug projection, not a second model.
public sealed class ConstructionBlackBoxTests
{
    private const string Golden =
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

    private static string Fingerprint(FspmConstructionDocumentSyntax document)
    {
        var sb = new StringBuilder();
        foreach (var page in document.Pages)
        {
            sb.Append("P:").Append(page.Name).Append(';');
            foreach (var form in page.Forms)
            {
                sb.Append("F:").Append(form.Name).Append(';');
                foreach (var binding in form.Bindings)
                {
                    switch (binding)
                    {
                        case FspmEntityBindingSyntax e:
                            sb.Append("E:").Append(e.Expression.Text).Append(';');
                            break;
                        case FspmFieldBindingSyntax f:
                            sb.Append("FL:").Append(f.Expression.Text).Append(';');
                            break;
                        case FspmSubmitBindingSyntax s:
                            sb.Append("S:").Append(s.Name).Append("->").Append(s.Target.Text).Append(';');
                            break;
                        default:
                            sb.Append("?:").Append(binding.GetType().Name).Append(';');
                            break;
                    }
                }
            }
        }

        return sb.ToString();
    }

    private static string DiagnosticFingerprint(
        System.Collections.Generic.IReadOnlyList<Foundry.FSPM.Compiler.Diagnostics.FspmDiagnostic> diagnostics)
    {
        return string.Join(";", diagnostics.Select(d => $"{d.Code}@{d.Line}:{d.Column}"));
    }

    [Fact]
    public void BlackBox_GoldenSource_YieldsExpectedStructure()
    {
        var result = FspmConstructionCompiler.Parse(Golden);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(
            "P:UserManagement;F:UserForm;E:User;FL:User.Name;FL:User.PhoneNumber;FL:User.Address;S:Create->User.Create;",
            Fingerprint(result.Document));
    }

    [Fact]
    public void BlackBox_MalformedSource_YieldsDiagnostic_NotException()
    {
        var result = FspmConstructionCompiler.Parse("page UserManagement {");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void BlackBox_NestedForm_YieldsDiagnostic()
    {
        var result = FspmConstructionCompiler.Parse(
            "page P {\n    form Outer {\n        form Inner {\n        }\n    }\n}");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void BlackBox_MultipleFieldsAndSubmits_AllRecorded()
    {
        var source =
            "page P {\n    form F {\n" +
            "        field A.X\n        field A.Y\n" +
            "        submit S1 -> A.M1\n        submit S2 -> A.M2\n    }\n}";

        var result = FspmConstructionCompiler.Parse(source);

        Assert.True(result.Succeeded);
        Assert.Equal(
            "P:P;F:F;FL:A.X;FL:A.Y;S:S1->A.M1;S:S2->A.M2;",
            Fingerprint(result.Document));
    }

    [Fact]
    public void BlackBox_NativeExpression_PassesThrough()
    {
        var result = FspmConstructionCompiler.Parse(
            "page P {\n    form F {\n        field ((User)obj).PhoneNumber\n    }\n}");

        Assert.True(result.Succeeded);
        Assert.Contains("FL:((User)obj).PhoneNumber;", Fingerprint(result.Document));
    }

    [Fact]
    public void BlackBox_IllegalCharacter_YieldsDiagnostic_NotThrow()
    {
        // NOTE: '#' starts a line comment in this language (skipped by design),
        // so '$' is used as the genuinely illegal character here.
        var result = FspmConstructionCompiler.Parse("page P {\n    form F {\n        field A$B\n    }\n}");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Determinism_SameSource_ParsedTenTimes_IdenticalFingerprints()
    {
        var first = Fingerprint(FspmConstructionCompiler.Parse(Golden).Document);
        var firstDiag = DiagnosticFingerprint(FspmConstructionCompiler.Parse(Golden).Diagnostics);

        for (var i = 0; i < 9; i++)
        {
            var result = FspmConstructionCompiler.Parse(Golden);
            Assert.Equal(first, Fingerprint(result.Document));
            Assert.Equal(firstDiag, DiagnosticFingerprint(result.Diagnostics));
        }
    }

    [Fact]
    public void Determinism_MalformedSource_ParsedTenTimes_IdenticalDiagnostics()
    {
        const string bad = "page P {\n    entity User\n}";
        var first = DiagnosticFingerprint(FspmConstructionCompiler.Parse(bad).Diagnostics);

        for (var i = 0; i < 9; i++)
        {
            Assert.Equal(first, DiagnosticFingerprint(FspmConstructionCompiler.Parse(bad).Diagnostics));
        }
    }
}
