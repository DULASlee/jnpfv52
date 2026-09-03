namespace Foundry.FSPM.Compiler.Diagnostics;

/// <summary>
/// Diagnostic codes (Phase 5 — 施工包 §17).
/// Phase 4 (Parser) uses FSPM001-FSPM004 only.
/// FSPM101+ reserved for Phase 8 Binders (NOT emitted by Parser).
/// FSPM111+ reserved for Phase 8 ambiguous resolution (NOT emitted by Parser).
/// </summary>
public static class FspmDiagnosticCodes
{
    // ===== Phase 4 Parser =====
    public const string UnexpectedToken = "FSPM001";
    public const string MissingIdentifier = "FSPM002";
    public const string MissingDot = "FSPM003";
    public const string DuplicateDeclaration = "FSPM004";

    // ===== Phase 12 Construction Parser (emitted by FspmConstructionParser only) =====
    public const string MissingExpression = "FSPM005";
    public const string MissingBrace = "FSPM006";
    public const string InvalidNesting = "FSPM007";
    public const string MissingArrow = "FSPM008";

    // ===== Phase 8 Binders (emitted by Binding/EntityBinder etc.) =====
    public const string EntityNotFound = "FSPM101";
    public const string PropertyNotFound = "FSPM102";
    public const string OperationNotFound = "FSPM103";
    public const string InvalidOperationSignature = "FSPM104";

    public const string AmbiguousEntity = "FSPM111";
    public const string AmbiguousProperty = "FSPM112";
    public const string AmbiguousOperation = "FSPM113";
}
