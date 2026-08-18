using System.Security.Cryptography;
using System.Text;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Codegen.TemplateContext;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// A5 — 3 份请假 IR-2 样本 .vm 渲染 + Roslyn 语法门禁 + SHA256 快照。
/// </summary>
public static class TemplateRenderSamplesTests
{
    private static readonly string[] SampleFiles =
    {
        "leave-simple.json",
        "leave-with-flow.json",
        "leave-extended.json",
    };

    public static void RunAll()
    {
        var samplesDir = ResolveSamplesDir();
        var templateRoot = VmTemplateCatalog.ResolveDefaultTemplateRoot();
        var renderer = VmTemplateRenderer.CreateDefault(templateRoot);
        var builder = new TemplateContextBuilder();
        var hashManifest = LoadHashManifest(samplesDir);

        foreach (var sampleFile in SampleFiles)
        {
            var samplePath = Path.Combine(samplesDir, sampleFile);
            if (!File.Exists(samplePath))
                throw new FileNotFoundException($"缺少 IR-2 样本: {samplePath}");

            var context = builder.BuildFromSampleJson(samplePath);
            ValidateContext(context);

            foreach (var templateId in VmTemplateIds.LockedBackendTemplates)
            {
                var rendered = renderer.Render(templateId, context);
                if (string.IsNullOrWhiteSpace(rendered))
                    throw new InvalidOperationException($"{context.SampleId}/{templateId} 渲染结果为空");

                CodegenSyntaxValidator.EnsureValidSyntax(rendered, $"{context.SampleId}-{templateId}");

                var hash = ComputeSha256(rendered);
                var key = HashKey(context.SampleId, templateId);

                if (hashManifest.TryGetValue(key, out var expected) &&
                    !string.Equals(expected, hash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"渲染快照不匹配 {key}: expected={expected} actual={hash}");
                }

                if (!hashManifest.ContainsKey(key))
                    throw new InvalidOperationException(
                        $"expected-hashes.json 缺少条目 {key}（实际 hash={hash}，请更新 manifest）");
            }
        }

        Console.WriteLine("[A5] TemplateRenderSamples: 3 samples × 3 templates PASS");
    }

    public static string ResolveSamplesDirPublic() => ResolveSamplesDir();

    /// <summary>更新 expected-hashes.json（开发维护用）。</summary>
    public static int GenerateExpectedHashes()
    {
        var samplesDir = ResolveSamplesDir();
        var templateRoot = VmTemplateCatalog.ResolveDefaultTemplateRoot();
        var renderer = VmTemplateRenderer.CreateDefault(templateRoot);
        var builder = new TemplateContextBuilder();
        var manifest = new Dictionary<string, string>();

        foreach (var sampleFile in SampleFiles)
        {
            var context = builder.BuildFromSampleJson(Path.Combine(samplesDir, sampleFile));
            foreach (var templateId in VmTemplateIds.LockedBackendTemplates)
            {
                var rendered = renderer.Render(templateId, context);
                CodegenSyntaxValidator.EnsureValidSyntax(rendered, $"{context.SampleId}-{templateId}");
                manifest[HashKey(context.SampleId, templateId)] = ComputeSha256(rendered);
            }
        }

        var outPath = Path.Combine(ResolveSamplesSourceDir(), "expected-hashes.json");
        File.WriteAllText(outPath, System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
        }));
        Console.WriteLine($"[A5] Wrote {outPath} ({manifest.Count} entries)");
        return 0;
    }

    private static void ValidateContext(Ir2CodegenContext context)
    {
        if (string.IsNullOrWhiteSpace(context.NameSpace))
            throw new InvalidOperationException($"{context.SampleId}: NameSpace 为空");

        if (context.TableField.Count < 2)
            throw new InvalidOperationException($"{context.SampleId}: TableField 不足");

        if (context.ToViewModel() is null)
            throw new InvalidOperationException($"{context.SampleId}: ToViewModel 返回 null");
    }

    private static Dictionary<string, string> LoadHashManifest(string samplesDir)
    {
        // manifest 始终以源码目录为准（generate-hashes 写入处）；样本 JSON 可走 CopyToOutputDirectory
        var manifestPath = Path.Combine(ResolveSamplesSourceDir(), "expected-hashes.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"缺少 hash manifest: {manifestPath}");

        var json = File.ReadAllText(manifestPath);
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new InvalidOperationException("expected-hashes.json 解析失败");
    }

    private static string ResolveSamplesSourceDir()
    {
        var repo = VmTemplateCatalog.ResolveRepoRoot();
        return Path.Combine(repo, "backend", "tests", "JNPF.Tests.PhaseB", "TemplateRenderSamples");
    }

    private static string ResolveSamplesDir()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "TemplateRenderSamples"),
            Path.Combine(Directory.GetCurrentDirectory(), "TemplateRenderSamples"),
            Path.Combine(VmTemplateCatalog.ResolveRepoRoot(), "backend", "tests", "JNPF.Tests.PhaseB", "TemplateRenderSamples"),
        };

        foreach (var dir in candidates)
        {
            if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "leave-simple.json")))
                return dir;
        }

        throw new DirectoryNotFoundException("无法定位 TemplateRenderSamples 目录");
    }

    private static string HashKey(string sampleId, string templateId) =>
        $"{sampleId}::{templateId}";

    private static string ComputeSha256(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
