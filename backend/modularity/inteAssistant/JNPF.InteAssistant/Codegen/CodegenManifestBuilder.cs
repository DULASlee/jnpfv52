using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JNPF.InteAssistant.Codegen.TemplateContext;
using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// 构建 IR-3 <c>CodeGenerated</c> payload（A1 §6）。
/// </summary>
public static class CodegenManifestBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static string ComputeSha256(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static IReadOnlyList<TemplateVersionEntry> BuildTemplateVersions(
        Ir2CodegenContext context,
        IReadOnlyDictionary<string, string> renderedByTemplateId)
    {
        var list = new List<TemplateVersionEntry>();
        foreach (var templateId in VmTemplateIds.LockedBackendTemplates)
        {
            if (!renderedByTemplateId.TryGetValue(templateId, out var content))
                continue;

            list.Add(new TemplateVersionEntry
            {
                TemplateId = templateId,
                Sha256 = ComputeSha256(content),
                RenderedPath = CodegenArtifactPaths.ToRelativePath(templateId, context.ClassName)
                    .Replace('\\', '/'),
            });
        }

        return list;
    }

    public static string BuildCodeGeneratedPayload(
        string tenantId,
        string projectId,
        Ir2CodegenContext context,
        IReadOnlyList<TemplateVersionEntry> templateVersions,
        bool syntaxPassed,
        bool sandboxBuildPassed = false,
        string? sandboxNote = null)
    {
        var payload = new
        {
            context = "https://schema.jnpf.ai/ir/v1",
            id = $"codegen:{projectId}",
            stabilityState = IrStabilityStates.Draft,
            artifactRoot = CodegenWorkspacePaths.ToArtifactRootRelative(tenantId, projectId),
            nameSpace = context.NameSpace,
            className = context.ClassName,
            templateProfileId = context.TemplateProfileId,
            templateVersions = templateVersions.Select(t => new
            {
                templateId = t.TemplateId,
                sha256 = t.Sha256,
                renderedPath = t.RenderedPath,
            }),
            syntaxCheck = new
            {
                passed = syntaxPassed,
                validator = "CodegenSyntaxValidator",
            },
            sandboxBuild = new
            {
                passed = sandboxBuildPassed,
                note = sandboxNote ?? (sandboxBuildPassed ? null : "promote 前由 CodeSandboxService 更新"),
            },
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string BuildCodegenFailedPayload(
        string projectId,
        CodeSandboxBuildResult result,
        string? fragmentId = null)
    {
        var payload = new
        {
            context = "https://schema.jnpf.ai/ir/v1",
            id = fragmentId ?? $"codegen:{projectId}",
            stabilityState = IrStabilityStates.Invalidated,
            phase = result.Phase,
            exitCode = result.ExitCode,
            errorMessage = result.ErrorMessage,
            stderr = Truncate(result.StandardError, 2000),
            elapsedMs = (int)result.Elapsed.TotalMilliseconds,
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string BuildCodegenBuildValidatedPayload(
        string projectId,
        CodeSandboxBuildResult result,
        string? fragmentId = null)
    {
        var payload = new
        {
            context = "https://schema.jnpf.ai/ir/v1",
            id = fragmentId ?? $"codegen:{projectId}",
            sandboxBuild = new
            {
                passed = true,
                phase = result.Phase,
                elapsedMs = (int)result.Elapsed.TotalMilliseconds,
            },
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string BuildCodeGeneratedStablePromotedPayload(
        string projectId,
        CodeSandboxBuildResult sandbox,
        ArchGuardScanResult archResult,
        string? fragmentId = null)
    {
        var payload = new
        {
            context = "https://schema.jnpf.ai/ir/v1",
            id = fragmentId ?? $"codegen:{projectId}",
            stabilityState = IrStabilityStates.Stable,
            promotedAt = DateTime.UtcNow.ToString("O"),
            promotionGate = new
            {
                sandboxBuild = true,
                sandboxElapsedMs = (int)sandbox.Elapsed.TotalMilliseconds,
                archGuardCritical = archResult.CriticalCount,
                archGuardWarnings = archResult.WarningCount,
            },
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    /// <summary>合并 IR-3 增量 patch（CodegenBuildValidated / CodeGeneratedStablePromoted）。</summary>
    public static string MergeIr3Payload(string existingJson, string patchJson)
    {
        using var existingDoc = JsonDocument.Parse(string.IsNullOrWhiteSpace(existingJson) ? "{}" : existingJson);
        using var patchDoc = JsonDocument.Parse(patchJson);
        return MergeJsonObjects(existingDoc.RootElement, patchDoc.RootElement);
    }

    private static string MergeJsonObjects(JsonElement baseEl, JsonElement patchEl)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var prop in baseEl.EnumerateObject())
            {
                if (patchEl.TryGetProperty(prop.Name, out _))
                    continue;
                prop.WriteTo(writer);
            }

            foreach (var prop in patchEl.EnumerateObject())
                prop.WriteTo(writer);

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string? Truncate(string? text, int maxLen)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLen)
            return text;
        return text[..maxLen];
    }

    public sealed class TemplateVersionEntry
    {
        public required string TemplateId { get; init; }
        public required string Sha256 { get; init; }
        public required string RenderedPath { get; init; }
    }
}
