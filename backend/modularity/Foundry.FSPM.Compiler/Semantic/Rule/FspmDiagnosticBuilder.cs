using Model = Foundry.FSPM.SemanticModel;

namespace Foundry.FSPM.Compiler.Semantic.Rule;

/// <summary>
/// P14-04: decision-to-diagnostic projection. Codes are stable per rule
/// kind; Passed maps to Info, Failed to Error; the decision Reason is
/// preserved verbatim so the chain survives projection.
/// </summary>
public static class FspmDiagnosticBuilder
{
    public static string CodeFor(Model.FspmRuleKind kind) => kind switch
    {
        Model.FspmRuleKind.Required => "RUL-REQ",
        Model.FspmRuleKind.Forbidden => "RUL-FRBD",
        Model.FspmRuleKind.Allowed => "RUL-ALLOW",
        Model.FspmRuleKind.ExactlyOne => "RUL-ONE",
        Model.FspmRuleKind.AtLeastOne => "RUL-ANY",
        Model.FspmRuleKind.TypeCompatible => "RUL-TYPE",
        Model.FspmRuleKind.OperationCompatible => "RUL-OP",
        _ => "RUL-UNK",
    };

    public static Model.FspmDiagnostic FromDecision(Model.FspmRule rule, Model.FspmRuleDecision decision)
    {
        var severity = decision.Passed ? "Info" : "Error";
        var message = decision.Passed
            ? $"rule '{rule.Id}' passed: {decision.Reason}"
            : $"rule '{rule.Id}' failed: {decision.Reason}";
        return new Model.FspmDiagnostic(CodeFor(rule.Kind), severity, message, decision.Reason, decision.Anchor);
    }
}
