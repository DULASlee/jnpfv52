using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Codegen.TemplateContext;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 阶段四 P4-B01a — developer-skill：IR-2 → .vm 渲染 → workspace 落盘 → CodeGenerated draft。
/// </summary>
public sealed class DeveloperSkillService : IBaseSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly TemplateContextBuilder _contextBuilder;
    private readonly CodegenWorkspaceWriter _workspaceWriter;
    private readonly ISystemDesignLockedCompletenessGate _completenessGate;
    private readonly ILogger<DeveloperSkillService> _logger;
    private readonly string _templateRoot;

    public DeveloperSkillService(
        TemplateContextBuilder contextBuilder,
        CodegenWorkspaceWriter workspaceWriter,
        ISystemDesignLockedCompletenessGate completenessGate,
        ILogger<DeveloperSkillService> logger)
    {
        _contextBuilder = contextBuilder;
        _workspaceWriter = workspaceWriter;
        _completenessGate = completenessGate;
        _logger = logger;
        _templateRoot = VmTemplateCatalog.ResolveDefaultTemplateRoot();
    }

    public string SkillId => DevelopmentSkillIds.Developer;
    public string Version => "1.0.0-d4a";

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[]
        {
            IrFragmentTypes.Architecture,
            IrFragmentTypes.DDL,
            IrFragmentTypes.FormPageIR,
            IrFragmentTypes.SystemDesign,
        },
        RequiredStability = IrStabilityStates.Stable,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[]
        {
            IrEventTypes.CodeGenerated,
            IrEventTypes.DeveloperSkillCompleted,
        },
    };

    public async Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        _ = ct;
        return await _completenessGate.ValidateAsync(snapshot, ct);
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        Ir2CodegenContext codegenContext;
        try
        {
            codegenContext = _contextBuilder.BuildFromSkillContext(context);
        }
        catch (TemplateContextBuildException ex)
        {
            throw Oops.Bah(ex.Message);
        }

        var renderer = VmTemplateRenderer.CreateDefault(_templateRoot);
        var rendered = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var templateId in VmTemplateIds.LockedBackendTemplates)
        {
            ct.ThrowIfCancellationRequested();
            var content = renderer.Render(templateId, codegenContext);
            if (string.IsNullOrWhiteSpace(content))
                throw Oops.Bah($"模板 {templateId} 渲染结果为空");

            CodegenSyntaxValidator.EnsureValidSyntax(content, $"{codegenContext.ClassName}-{templateId}");
            rendered[templateId] = content;
        }

        var backendRoot = CodegenWorkspacePaths.ResolveBackendRoot(context.TenantId, context.ProjectId);
        _workspaceWriter.WriteGenerated(backendRoot, codegenContext, rendered);

        _logger.LogInformation(
            "Developer skill 落盘完成 project={ProjectId} path={Path} templates={Count}",
            context.ProjectId,
            backendRoot,
            rendered.Count);

        var templateVersions = CodegenManifestBuilder.BuildTemplateVersions(codegenContext, rendered);
        var fragmentId = $"codegen:{context.ProjectId}";
        var payload = CodegenManifestBuilder.BuildCodeGeneratedPayload(
            context.TenantId,
            context.ProjectId,
            codegenContext,
            templateVersions,
            syntaxPassed: true);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.CodeGenerated,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.GeneratedCode,
            FragmentVersion = 1,
            Payload = payload,
            SkillId = SkillId,
        };

        var completedPayload = JsonSerializer.Serialize(new
        {
            context = "https://schema.jnpf.ai/ir/v1",
            projectId = context.ProjectId,
            artifactRoot = CodegenWorkspacePaths.ToArtifactRootRelative(context.TenantId, context.ProjectId),
            templateCount = templateVersions.Count,
            channel = "A/B",
        }, JsonOptions);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.DeveloperSkillCompleted,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.GeneratedCode,
            FragmentVersion = 1,
            Payload = completedPayload,
            SkillId = SkillId,
        };

        await Task.CompletedTask;
    }

    public Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events,
        CancellationToken ct = default)
    {
        _ = ct;
        if (events.Count != 2)
            return Task.FromResult(SkillValidationResult.Fail("developer-skill 必须产出 CodeGenerated + DeveloperSkillCompleted"));

        if (events[0].EventType != IrEventTypes.CodeGenerated
            || events[1].EventType != IrEventTypes.DeveloperSkillCompleted)
        {
            return Task.FromResult(SkillValidationResult.Fail("事件顺序或类型不正确"));
        }

        if (events[0].FragmentType != IrFragmentTypes.GeneratedCode)
            return Task.FromResult(SkillValidationResult.Fail("CodeGenerated 须绑定 IR3_GeneratedCode"));

        try
        {
            using var doc = JsonDocument.Parse(events[0].Payload);
            var root = doc.RootElement;
            if (!root.TryGetProperty("stabilityState", out var st)
                || st.GetString() != IrStabilityStates.Draft)
            {
                return Task.FromResult(SkillValidationResult.Fail("CodeGenerated payload 须 stabilityState=draft"));
            }

            if (!root.TryGetProperty("templateVersions", out var tv) || tv.GetArrayLength() == 0)
                return Task.FromResult(SkillValidationResult.Fail("CodeGenerated 缺少 templateVersions"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(SkillValidationResult.Fail($"CodeGenerated payload JSON 无效: {ex.Message}"));
        }

        return Task.FromResult(SkillValidationResult.Ok());
    }
}
