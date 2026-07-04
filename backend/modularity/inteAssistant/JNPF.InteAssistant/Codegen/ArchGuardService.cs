using System.Text.Json;
using System.Text.RegularExpressions;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Codegen;

public interface IArchGuardService
{
    ArchGuardScanResult Scan(string tenantId, string projectId, IrSnapshot? snapshot = null);

    Task<ArchGuardScanResult> ScanAndPersistAsync(
        string projectId,
        string tenantId,
        IrSnapshot? snapshot,
        string? skillId,
        CancellationToken ct = default);
}

/// <summary>
/// 阶段四 P4-B04a — 正则/路径 MVP，规则唯一来源 arch-guard-rules.yaml。
/// </summary>
public sealed class ArchGuardService : IArchGuardService, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IIrEventStoreService _eventStore;
    private readonly ILogger<ArchGuardService> _logger;

    public ArchGuardService(IIrEventStoreService eventStore, ILogger<ArchGuardService> logger)
    {
        _eventStore = eventStore;
        _logger = logger;
    }

    public ArchGuardScanResult Scan(string tenantId, string projectId, IrSnapshot? snapshot = null)
    {
        var backendRoot = CodegenWorkspacePaths.ResolveBackendRoot(tenantId, projectId);
        var violations = new List<ArchGuardViolation>();

        foreach (var rule in ArchGuardRulesLoader.GetOrderedRules())
        {
            violations.AddRange(EvaluateRule(rule, backendRoot, tenantId, projectId, snapshot));
        }

        return new ArchGuardScanResult { Violations = violations };
    }

    public async Task<ArchGuardScanResult> ScanAndPersistAsync(
        string projectId,
        string tenantId,
        IrSnapshot? snapshot,
        string? skillId,
        CancellationToken ct = default)
    {
        var result = Scan(tenantId, projectId, snapshot);
        if (result.Violations.Count == 0)
            return result;

        var payload = ArchGuardManifestBuilder.BuildArchViolationPayload(projectId, result);
        await _eventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.ArchViolationDetected,
            FragmentId = $"arch-report:{projectId}",
            FragmentType = IrFragmentTypes.ArchReport,
            FragmentVersion = 1,
            Payload = payload,
            SkillId = skillId ?? "arch-guard",
        }, ct);

        _logger.LogWarning(
            "ArchViolationDetected project={ProjectId} critical={Critical} warning={Warning}",
            projectId,
            result.CriticalCount,
            result.WarningCount);

        return new ArchGuardScanResult
        {
            Violations = result.Violations,
            EventAppended = true,
        };
    }

    private static IEnumerable<ArchGuardViolation> EvaluateRule(
        ArchGuardRuleDefinition rule,
        string backendRoot,
        string tenantId,
        string projectId,
        IrSnapshot? snapshot)
    {
        if (rule.Detect == null)
            yield break;

        var detectType = rule.Detect.Type ?? string.Empty;
        switch (detectType)
        {
            case "path-convention":
                foreach (var v in EvaluatePathConvention(rule, backendRoot))
                    yield return v;
                break;
            case "regex":
                foreach (var file in ArchGuardPathMatcher.EnumerateTargetFiles(backendRoot, rule.Targets))
                {
                    var content = File.ReadAllText(file);
                    foreach (var v in EvaluateRegex(rule, ToDisplayPath(file, backendRoot, tenantId, projectId), content))
                        yield return v;
                }

                if (ArchGuardPathMatcher.MergeFragmentType(rule.Targets) != null && snapshot != null)
                {
                    foreach (var v in EvaluateRegexOnFragment(rule, snapshot))
                        yield return v;
                }

                break;
            case "regex-all-required":
                foreach (var file in ArchGuardPathMatcher.EnumerateTargetFiles(backendRoot, rule.Targets))
                {
                    var content = File.ReadAllText(file);
                    if (!EvaluateRegexAllRequired(rule.Detect, content))
                    {
                        yield return BuildViolation(
                            rule,
                            ToDisplayPath(file, backendRoot, tenantId, projectId),
                            match: null);
                    }
                }

                break;
            case "regex-negative":
                foreach (var file in ArchGuardPathMatcher.EnumerateTargetFiles(backendRoot, rule.Targets))
                {
                    if (ShouldSkipFile(file, rule.Detect.SkipGlob, backendRoot))
                        continue;

                    var content = File.ReadAllText(file);
                    foreach (var v in EvaluateRegexNegative(rule, ToDisplayPath(file, backendRoot, tenantId, projectId), content))
                        yield return v;
                }

                break;
            case "sibling-file-missing":
                foreach (var file in ArchGuardPathMatcher.EnumerateTargetFiles(backendRoot, rule.Targets))
                {
                    foreach (var v in EvaluateSiblingMissing(rule, file, backendRoot, tenantId, projectId))
                        yield return v;
                }

                break;
        }
    }

    private static IEnumerable<ArchGuardViolation> EvaluatePathConvention(
        ArchGuardRuleDefinition rule,
        string backendRoot)
    {
        var detect = rule.Detect!;
        var forbidden = ExpandStringList(detect.ForbiddenPatterns);
        var allowed = ExpandStringList(detect.AllowedExceptions);

        foreach (var file in ArchGuardPathMatcher.EnumerateTargetFiles(backendRoot, rule.Targets))
        {
            var relative = Path.GetRelativePath(backendRoot, file).Replace('\\', '/');
            if (!forbidden.Any(f => ArchGuardPathMatcher.MatchesGlob(relative, ArchGuardPathMatcher.NormalizeBackendGlob(f))))
                continue;

            if (allowed.Any(a => ArchGuardPathMatcher.MatchesGlob(relative, ArchGuardPathMatcher.NormalizeBackendGlob(a))))
                continue;

            if (!string.IsNullOrEmpty(detect.ChannelCAllowedSuffix)
                && file.EndsWith(detect.ChannelCAllowedSuffix, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return BuildViolation(rule, relative, match: Path.GetFileName(file));
        }
    }

    private static IEnumerable<ArchGuardViolation> EvaluateRegex(
        ArchGuardRuleDefinition rule,
        string displayPath,
        string content)
    {
        var pattern = rule.Detect!.Pattern;
        if (string.IsNullOrWhiteSpace(pattern))
            yield break;

        var options = BuildRegexOptions(rule.Detect.Flags);
        var match = Regex.Match(content, pattern, options);
        if (match.Success)
        {
            yield return BuildViolation(rule, displayPath, match.Value);
        }
    }

    private static IEnumerable<ArchGuardViolation> EvaluateRegexOnFragment(
        ArchGuardRuleDefinition rule,
        IrSnapshot snapshot)
    {
        var fragmentType = ArchGuardPathMatcher.MergeFragmentType(rule.Targets);
        if (string.IsNullOrWhiteSpace(fragmentType))
            yield break;

        var fragment = snapshot.Find(fragmentType, IrStabilityStates.Stable)
            ?? snapshot.Find(fragmentType);
        if (fragment == null)
            yield break;

        var ddlText = ExtractFragmentText(fragment.Payload);
        if (string.IsNullOrWhiteSpace(ddlText))
            yield break;

        foreach (var v in EvaluateRegex(rule, fragmentType, ddlText))
        {
            yield return new ArchGuardViolation
            {
                RuleId = v.RuleId,
                Severity = v.Severity,
                Message = v.Message,
                FilePath = fragment.FragmentId,
                Match = v.Match,
            };
        }
    }

    private static bool EvaluateRegexAllRequired(ArchGuardDetectSection detect, string content)
    {
        var patterns = ExpandStringList(detect.Patterns);
        if (patterns.Count == 0)
            return true;

        var requireAny = detect.RequireAny ?? false;
        if (requireAny)
            return patterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase | RegexOptions.Compiled));

        return patterns.All(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase | RegexOptions.Compiled));
    }

    private static IEnumerable<ArchGuardViolation> EvaluateRegexNegative(
        ArchGuardRuleDefinition rule,
        string displayPath,
        string content)
    {
        var detect = rule.Detect!;
        var trigger = detect.WhenFileContains;
        if (!string.IsNullOrWhiteSpace(trigger) && !Regex.IsMatch(content, trigger, RegexOptions.IgnoreCase))
            yield break;

        if (!string.IsNullOrWhiteSpace(detect.MustContain)
            && !content.Contains(detect.MustContain, StringComparison.Ordinal))
        {
            yield return BuildViolation(rule, displayPath, match: detect.MustContain);
            yield break;
        }

        var mustAlso = ExpandStringList(detect.MustAlsoContainAny);
        if (mustAlso.Count > 0
            && !string.IsNullOrWhiteSpace(trigger)
            && Regex.IsMatch(content, trigger, RegexOptions.IgnoreCase)
            && !mustAlso.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase)))
        {
            yield return BuildViolation(rule, displayPath, match: trigger);
        }
    }

    private static IEnumerable<ArchGuardViolation> EvaluateSiblingMissing(
        ArchGuardRuleDefinition rule,
        string filePath,
        string backendRoot,
        string tenantId,
        string projectId)
    {
        var detect = rule.Detect!;
        var suffix = detect.StripSuffix ?? ".custom.cs";
        if (!filePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            yield break;

        var siblingPattern = detect.SiblingPattern ?? "{basename}.cs";
        var basename = Path.GetFileNameWithoutExtension(filePath);
        if (basename.EndsWith(".custom", StringComparison.OrdinalIgnoreCase))
            basename = basename[..^".custom".Length];

        var expectedName = siblingPattern.Replace("{basename}", basename, StringComparison.Ordinal);
        var siblingPath = Path.Combine(Path.GetDirectoryName(filePath)!, expectedName);
        if (File.Exists(siblingPath))
            yield break;

        yield return BuildViolation(
            rule,
            ToDisplayPath(filePath, backendRoot, tenantId, projectId),
            match: expectedName);
    }

    private static bool ShouldSkipFile(string filePath, string? skipGlob, string backendRoot)
    {
        if (string.IsNullOrWhiteSpace(skipGlob))
            return false;

        var relative = Path.GetRelativePath(backendRoot, filePath).Replace('\\', '/');
        return ArchGuardPathMatcher.MatchesGlob(relative, ArchGuardPathMatcher.NormalizeBackendGlob(skipGlob));
    }

    private static ArchGuardViolation BuildViolation(
        ArchGuardRuleDefinition rule,
        string filePath,
        string? match)
    {
        var message = rule.Message
            .Replace("{filePath}", filePath, StringComparison.Ordinal)
            .Replace("{match}", match ?? string.Empty, StringComparison.Ordinal)
            .Replace("{expectedSibling}", match ?? string.Empty, StringComparison.Ordinal);

        return new ArchGuardViolation
        {
            RuleId = rule.Id,
            Severity = rule.Severity,
            Message = message,
            FilePath = filePath,
            Match = match,
        };
    }

    private static string ToDisplayPath(string absolutePath, string backendRoot, string tenantId, string projectId)
    {
        var relative = Path.GetRelativePath(backendRoot, absolutePath).Replace('\\', '/');
        return $"{CodegenWorkspacePaths.ToArtifactRootRelative(tenantId, projectId)}/{relative}";
    }

    private static RegexOptions BuildRegexOptions(string? flags)
    {
        var options = RegexOptions.Compiled;
        if (!string.IsNullOrWhiteSpace(flags) && flags.Contains("ignoreCase", StringComparison.OrdinalIgnoreCase))
            options |= RegexOptions.IgnoreCase;
        return options;
    }

    private static List<string> ExpandStringList(YamlStringOrList? value)
    {
        return value?.Values.Where(x => x.Length > 0).ToList() ?? new List<string>();
    }

    private static List<string> ExpandPathGlobs(YamlStringOrList? pathGlob) => ExpandStringList(pathGlob);

    private static string ExtractFragmentText(object? payload)
    {
        if (payload == null)
            return string.Empty;

        if (payload is string str)
        {
            if (str.TrimStart().StartsWith('{'))
            {
                try
                {
                    using var doc = JsonDocument.Parse(str);
                    if (doc.RootElement.TryGetProperty("ddl", out var ddl))
                        return ddl.GetString() ?? str;
                    if (doc.RootElement.TryGetProperty("DDL", out var ddlUpper))
                        return ddlUpper.GetString() ?? str;
                }
                catch
                {
                    return str;
                }
            }

            return str;
        }

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}

public static class ArchGuardManifestBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string BuildArchViolationPayload(string projectId, ArchGuardScanResult result)
    {
        var payload = new
        {
            context = "https://schema.jnpf.ai/ir/v1",
            id = $"arch-report:{projectId}",
            checkedAt = DateTime.UtcNow.ToString("O"),
            criticalCount = result.CriticalCount,
            warningCount = result.WarningCount,
            violations = result.Violations.Select(v => new
            {
                v.RuleId,
                v.Severity,
                v.Message,
                v.FilePath,
                v.Match,
            }),
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
