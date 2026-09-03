using Foundry.FSPM.Compiler.Diagnostics;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// Phase 5 Diagnostic tests (施工包 §16-§17).
/// Verifies FspmDiagnostic shape, codes constant values, and severity enum.
/// </summary>
public sealed class DiagnosticTests
{
    [Fact]
    public void Severity_Enum_Has_Info_Warning_Error()
    {
        Assert.Equal(0, (int)FspmDiagnosticSeverity.Info);
        Assert.Equal(1, (int)FspmDiagnosticSeverity.Warning);
        Assert.Equal(2, (int)FspmDiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData(FspmDiagnosticSeverity.Info, "info")]
    [InlineData(FspmDiagnosticSeverity.Warning, "warning")]
    [InlineData(FspmDiagnosticSeverity.Error, "error")]
    public void Severity_Enum_ToString_RoundTrips(FspmDiagnosticSeverity sev, string expected)
    {
        Assert.Equal(expected, sev.ToString(), ignoreCase: true);
    }

    [Fact]
    public void DiagnosticCodes_FSPM001_Is_UnexpectedToken()
    {
        Assert.Equal("FSPM001", FspmDiagnosticCodes.UnexpectedToken);
    }

    [Fact]
    public void DiagnosticCodes_FSPM002_Is_MissingIdentifier()
    {
        Assert.Equal("FSPM002", FspmDiagnosticCodes.MissingIdentifier);
    }

    [Fact]
    public void DiagnosticCodes_FSPM003_Is_MissingDot()
    {
        Assert.Equal("FSPM003", FspmDiagnosticCodes.MissingDot);
    }

    [Fact]
    public void DiagnosticCodes_FSPM004_Is_DuplicateDeclaration()
    {
        Assert.Equal("FSPM004", FspmDiagnosticCodes.DuplicateDeclaration);
    }

    [Fact]
    public void DiagnosticCodes_Phase8Codes_Exist_ButNotYetEmittedByParser()
    {
        // FSPM101+ are reserved for Phase 8 Binders. They exist so callers
        // can reference them, but Parser does NOT emit them yet.
        Assert.Equal("FSPM101", FspmDiagnosticCodes.EntityNotFound);
        Assert.Equal("FSPM102", FspmDiagnosticCodes.PropertyNotFound);
        Assert.Equal("FSPM103", FspmDiagnosticCodes.OperationNotFound);
        Assert.Equal("FSPM111", FspmDiagnosticCodes.AmbiguousEntity);
        Assert.Equal("FSPM112", FspmDiagnosticCodes.AmbiguousProperty);
        Assert.Equal("FSPM113", FspmDiagnosticCodes.AmbiguousOperation);
    }

    [Fact]
    public void Diagnostic_Record_Carries_AllFields()
    {
        var d = new FspmDiagnostic(
            Code: "FSPM001",
            Severity: FspmDiagnosticSeverity.Error,
            Message: "test message",
            Line: 3,
            Column: 7);

        Assert.Equal("FSPM001", d.Code);
        Assert.Equal(FspmDiagnosticSeverity.Error, d.Severity);
        Assert.Equal("test message", d.Message);
        Assert.Equal(3, d.Line);
        Assert.Equal(7, d.Column);
    }

    [Fact]
    public void Diagnostic_Has_ValueEquality()
    {
        var a = new FspmDiagnostic("FSPM001", FspmDiagnosticSeverity.Error, "msg", 1, 1);
        var b = new FspmDiagnostic("FSPM001", FspmDiagnosticSeverity.Error, "msg", 1, 1);
        var c = new FspmDiagnostic("FSPM002", FspmDiagnosticSeverity.Error, "msg", 1, 1);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
