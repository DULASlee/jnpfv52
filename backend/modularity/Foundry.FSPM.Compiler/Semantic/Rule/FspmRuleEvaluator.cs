using Model = Foundry.FSPM.SemanticModel;

namespace Foundry.FSPM.Compiler.Semantic.Rule;

/// <summary>
/// P14-04: evaluates generic rules against an already-bound model.
/// Identity-keyed only (LogicalId match); no name inference, no Roslyn,
/// no mutation. Missing entries fail with a named reason; every decision
/// carries subject/target identities, fingerprints, and the anchor of the
/// first addressable participant. Model.* qualification is deliberate:
/// this namespace nests under Compiler.Semantic, which owns legacy
/// same-named records the rule layer must never touch.
/// </summary>
public static class FspmRuleEvaluator
{
    public static Model.FspmRuleDecision Evaluate(Model.FspmRuleContext context, Model.FspmRule rule)
    {
        return rule.Kind switch
        {
            Model.FspmRuleKind.Required => EvaluatePresence(context, rule, mustExist: true),
            Model.FspmRuleKind.Forbidden => EvaluatePresence(context, rule, mustExist: false),
            Model.FspmRuleKind.Allowed => EvaluateAllowed(context, rule),
            Model.FspmRuleKind.ExactlyOne => EvaluateCount(context, rule, exact: 1),
            Model.FspmRuleKind.AtLeastOne => EvaluateCount(context, rule, exact: -1),
            Model.FspmRuleKind.TypeCompatible => EvaluateTypeCompatible(context, rule),
            Model.FspmRuleKind.OperationCompatible => EvaluateOperationCompatible(context, rule),
            _ => Fail(rule, "", "", "", "", null, $"unknown rule kind '{rule.Kind}'"),
        };
    }

    private static Model.FspmRuleDecision EvaluatePresence(
        Model.FspmRuleContext context, Model.FspmRule rule, bool mustExist)
    {
        var subject = rule.Targets.Count > 0 ? rule.Targets[0] : "";
        var found = FindAny(context.Model, subject);
        var present = found is not null;
        var passed = present == mustExist;
        var reason = mustExist
            ? (present ? $"subject '{subject}' is present" : $"subject '{subject}' is missing")
            : (present ? $"subject '{subject}' is present but forbidden" : $"subject '{subject}' is absent");
        return new Model.FspmRuleDecision(
            rule.Id, passed, reason,
            subject, found?.Fingerprint ?? "",
            subject, found?.Fingerprint ?? "",
            found?.Anchor);
    }

    private static Model.FspmRuleDecision EvaluateAllowed(
        Model.FspmRuleContext context, Model.FspmRule rule)
    {
        var subject = rule.Targets.Count > 0 ? rule.Targets[0] : "";
        var found = FindAny(context.Model, subject);
        var reason = found is not null
            ? $"subject '{subject}' is present (allowed)"
            : $"subject '{subject}' is absent (allowed)";
        return new Model.FspmRuleDecision(
            rule.Id, true, reason,
            subject, found?.Fingerprint ?? "",
            subject, found?.Fingerprint ?? "",
            found?.Anchor);
    }

    private static Model.FspmRuleDecision EvaluateCount(
        Model.FspmRuleContext context, Model.FspmRule rule, int exact)
    {
        var hits = rule.Targets.Where(t => FindAny(context.Model, t) is not null).ToList();
        var missing = rule.Targets.Except(hits).ToList();
        bool passed;
        string reason;
        if (exact >= 0)
        {
            passed = hits.Count == exact;
            reason = passed
                ? $"exactly {exact} of {rule.Targets.Count} candidates present ('{hits[0]}')"
                : $"expected exactly {exact} of {rule.Targets.Count} candidates present, found {hits.Count}"
                    + (missing.Count > 0 ? $"; missing: {string.Join(",", missing)}" : "");
        }
        else
        {
            passed = hits.Count >= 1;
            reason = passed
                ? $"at least one of {rule.Targets.Count} candidates present ('{hits[0]}')"
                : $"none of {rule.Targets.Count} candidates present; missing: {string.Join(",", missing)}";
        }
        var anchor = hits.Count > 0 ? FindAny(context.Model, hits[0])?.Anchor : null;
        return new Model.FspmRuleDecision(
            rule.Id, passed, reason,
            string.Join(",", hits), "",
            string.Join(",", rule.Targets), "",
            anchor);
    }

    private static Model.FspmRuleDecision EvaluateTypeCompatible(
        Model.FspmRuleContext context, Model.FspmRule rule)
    {
        var subject = rule.Targets.Count > 0 ? rule.Targets[0] : "";
        var member = FindMember(context.Model, subject);
        if (member is null)
            return Fail(rule, subject, "", subject, "", null, $"member '{subject}' is missing");
        var passed = string.Equals(member.Type, rule.Expected, StringComparison.Ordinal);
        var reason = passed
            ? $"member '{subject}' has type '{member.Type}' matching expected '{rule.Expected}'"
            : $"member '{subject}' has type '{member.Type}', expected '{rule.Expected}'";
        return new Model.FspmRuleDecision(
            rule.Id, passed, reason,
            subject, member.Fingerprint,
            subject, member.Fingerprint,
            member.Anchor);
    }

    private static Model.FspmRuleDecision EvaluateOperationCompatible(
        Model.FspmRuleContext context, Model.FspmRule rule)
    {
        var subject = rule.Targets.Count > 0 ? rule.Targets[0] : "";
        var operation = FindOperation(context.Model, subject);
        if (operation is null)
            return Fail(rule, subject, "", subject, "", null, $"operation '{subject}' is missing");
        var passed = string.Equals(operation.ReturnType, rule.Expected, StringComparison.Ordinal);
        var reason = passed
            ? $"operation '{subject}' returns '{operation.ReturnType}' matching expected '{rule.Expected}'"
            : $"operation '{subject}' returns '{operation.ReturnType}', expected '{rule.Expected}'";
        return new Model.FspmRuleDecision(
            rule.Id, passed, reason,
            subject, operation.Fingerprint,
            subject, operation.Fingerprint,
            operation.Anchor);
    }

    private static Model.FspmRuleDecision Fail(
        Model.FspmRule rule, string subjectIdentity, string subjectFingerprint,
        string targetIdentity, string targetFingerprint,
        Model.FspmSemanticAnchor? anchor, string reason)
        => new(rule.Id, false, reason, subjectIdentity, subjectFingerprint, targetIdentity, targetFingerprint, anchor);

    private sealed record Entry(string LogicalId, string Fingerprint, Model.FspmSemanticAnchor Anchor);

    private static Entry? FindAny(Model.FspmSemanticModel model, string logicalId)
    {
        if (string.IsNullOrEmpty(logicalId)) return null;
        var type = model.Types.FirstOrDefault(t => t.Identity.LogicalId == logicalId);
        if (type is not null) return new Entry(logicalId, type.Fingerprint, type.Anchor);
        var member = FindMember(model, logicalId);
        if (member is not null) return new Entry(logicalId, member.Fingerprint, member.Anchor);
        var operation = FindOperation(model, logicalId);
        if (operation is not null) return new Entry(logicalId, operation.Fingerprint, operation.Anchor);
        return null;
    }

    private static Model.FspmSemanticMember? FindMember(Model.FspmSemanticModel model, string logicalId)
        => model.Members.FirstOrDefault(m => m.Identity.LogicalId == logicalId);

    private static Model.FspmSemanticOperation? FindOperation(Model.FspmSemanticModel model, string logicalId)
        => model.Operations.FirstOrDefault(o => o.Identity.LogicalId == logicalId);
}
