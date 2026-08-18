using System.Text.Json;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;

namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// Q2 可复现违规模板 — templates/_violations/{profileId}/
/// </summary>
public static class ArchGuardViolationProfiles
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<string> ListProfileIds()
    {
        var root = ResolveViolationsRoot();
        if (!Directory.Exists(root))
            return Array.Empty<string>();

        return Directory.GetDirectories(root)
            .Select(Path.GetFileName)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .Cast<string>()
            .ToList();
    }

    public static ArchGuardViolationProfile Load(string profileId)
    {
        var dir = Path.Combine(ResolveViolationsRoot(), profileId);
        var profilePath = Path.Combine(dir, "profile.json");
        if (!File.Exists(profilePath))
            throw new FileNotFoundException($"违规模板 profile 不存在: {profileId}", profilePath);

        var profile = JsonSerializer.Deserialize<ArchGuardViolationProfile>(
            File.ReadAllText(profilePath), JsonOptions)
            ?? throw new InvalidOperationException($"profile.json 解析失败: {profileId}");

        profile = profile with { ProfileId = profileId, ProfileDirectory = dir };
        return profile;
    }

    public static void ApplyToBackend(string profileId, string backendRoot)
    {
        var profile = Load(profileId);
        var dir = profile.ProfileDirectory;
        Directory.CreateDirectory(backendRoot);

        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            if (file.EndsWith("profile.json", StringComparison.OrdinalIgnoreCase))
                continue;

            var relative = Path.GetRelativePath(dir, file);
            var target = Path.Combine(backendRoot, relative);
            var targetDir = Path.GetDirectoryName(target)!;
            Directory.CreateDirectory(targetDir);
            File.Copy(file, target, overwrite: true);
        }

        foreach (var inject in profile.InjectFiles ?? Array.Empty<ArchGuardFileRef>())
        {
            CopyProfileFile(dir, inject.Source, backendRoot, inject.Target);
        }

        foreach (var overwrite in profile.OverwriteFiles ?? Array.Empty<ArchGuardFileRef>())
        {
            CopyProfileFile(dir, overwrite.Source, backendRoot, overwrite.Target);
        }
    }

    public static IrSnapshot ApplySnapshotPatches(string profileId, IrSnapshot snapshot)
    {
        var profile = Load(profileId);
        if (profile.SnapshotPatches == null || profile.SnapshotPatches.Count == 0)
            return snapshot;

        var fragments = snapshot.Fragments.ToList();
        foreach (var patch in profile.SnapshotPatches)
        {
            var idx = fragments.FindIndex(f =>
                string.Equals(f.FragmentType, patch.FragmentType, StringComparison.Ordinal));
            if (idx < 0)
                continue;

            var merged = MergePayloadJson(fragments[idx].Payload, patch.MergePayload);
            fragments[idx] = new IrSnapshotFragment
            {
                FragmentId = fragments[idx].FragmentId,
                FragmentType = fragments[idx].FragmentType,
                StabilityState = fragments[idx].StabilityState,
                Payload = merged,
                SaStepsCompleted = fragments[idx].SaStepsCompleted,
            };
        }

        return new IrSnapshot { Fragments = fragments };
    }

    public static string ResolveViolationsRoot()
    {
        var repoRoot = VmTemplateCatalog.ResolveRepoRoot();
        return Path.Combine(
            repoRoot,
            "backend",
            "modularity",
            "inteAssistant",
            "JNPF.InteAssistant",
            "Codegen",
            "templates",
            "_violations");
    }

    private static void CopyProfileFile(
        string profileDir, string sourceRelative, string backendRoot, string targetRelative)
    {
        var source = Path.Combine(profileDir, sourceRelative.Replace('/', Path.DirectorySeparatorChar));
        var target = Path.Combine(backendRoot, targetRelative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(source))
            throw new FileNotFoundException($"违规模板文件缺失: {sourceRelative}", source);

        var dir = Path.GetDirectoryName(target)!;
        Directory.CreateDirectory(dir);
        File.Copy(source, target, overwrite: true);
    }

    private static string MergePayloadJson(string existingJson, JsonElement? mergePayload)
    {
        if (mergePayload == null || mergePayload.Value.ValueKind == JsonValueKind.Null)
            return existingJson;

        using var baseDoc = JsonDocument.Parse(string.IsNullOrWhiteSpace(existingJson) ? "{}" : existingJson);
        var dict = new Dictionary<string, object?>();
        foreach (var prop in baseDoc.RootElement.EnumerateObject())
            dict[prop.Name] = prop.Value.Clone();

        foreach (var prop in mergePayload.Value.EnumerateObject())
            dict[prop.Name] = prop.Value.Clone();

        return JsonSerializer.Serialize(dict, JsonOptions);
    }
}

public sealed record ArchGuardViolationProfile
{
    public string ProfileId { get; init; } = string.Empty;
    public string ProfileDirectory { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<string> ExpectedRuleIds { get; init; } = Array.Empty<string>();
    public string ExpectedSeverity { get; init; } = "critical";
    public IReadOnlyList<ArchGuardFileRef>? InjectFiles { get; init; }
    public IReadOnlyList<ArchGuardFileRef>? OverwriteFiles { get; init; }
    public IReadOnlyList<ArchGuardSnapshotPatch>? SnapshotPatches { get; init; }
}

public sealed record ArchGuardFileRef
{
    public string Source { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
}

public sealed record ArchGuardSnapshotPatch
{
    public string FragmentType { get; init; } = string.Empty;
    public JsonElement? MergePayload { get; init; }
}
