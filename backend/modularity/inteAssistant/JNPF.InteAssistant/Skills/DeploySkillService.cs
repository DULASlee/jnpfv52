using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Studio;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 阶段六 P5-B03 — deploy-skill：沙箱预览 + 源码 ZIP + DeploymentVerified 事件。
/// </summary>
public sealed class DeploySkillService : IBaseSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IPipelineDeliveryCoordinator _deliveryCoordinator;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<DeploySkillService> _logger;

    public DeploySkillService(
        IPipelineDeliveryCoordinator deliveryCoordinator,
        ISqlSugarClient db,
        ILogger<DeploySkillService> logger)
    {
        _deliveryCoordinator = deliveryCoordinator;
        _db = db;
        _logger = logger;
    }

    public string SkillId => DeploySkillIds.Deploy;
    public string Version { get; } = "1.0.0-p5b03";

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes =
        [
            IrFragmentTypes.GeneratedCode,
            IrFragmentTypes.TestSuite,
        ],
        RequiredStability = IrStabilityStates.Stable,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes =
        [
            IrEventTypes.DeploymentVerified,
            IrEventTypes.DeploymentFailed,
        ],
    };

    public Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        _ = ct;
        var codegen = snapshot.Find(IrFragmentTypes.GeneratedCode, IrStabilityStates.Stable);
        if (codegen == null)
            return Task.FromResult(SkillValidationResult.Fail("IR3_GeneratedCode 须 stable 后才可运行 deploy-skill"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var fragmentId = $"deployment:{context.ProjectId}";
        string? previewUrl = null;
        string? downloadUrl = null;
        string? errorMessage = null;

        try
        {
            await _deliveryCoordinator.RunPreviewAndPackageAsync(context.PipelineId, context.TenantId, ct);
            var row = await _db.Queryable<GeneratedProjectEntity>()
                .FirstAsync(x => x.F_Id == context.PipelineId, ct);
            previewUrl = row?.F_SandboxUrl;
            downloadUrl = row?.F_SourceZipUrl;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogError(ex, "deploy-skill 交付失败 pipeline={PipelineId}", context.PipelineId);
        }

        if (!string.IsNullOrWhiteSpace(errorMessage) || string.IsNullOrWhiteSpace(downloadUrl))
        {
            yield return new AppendIrEventRequest
            {
                EventType = IrEventTypes.DeploymentFailed,
                FragmentId = fragmentId,
                FragmentType = IrFragmentTypes.GeneratedCode,
                Payload = JsonSerializer.Serialize(new
                {
                    projectId = context.ProjectId,
                    pipelineId = context.PipelineId,
                    error = errorMessage ?? "交付包未生成",
                    previewUrl,
                    downloadUrl,
                }, JsonOptions),
            };
            throw Oops.Bah(errorMessage ?? "部署交付失败：无 downloadUrl");
        }

        // 记录部署验证时间（阶段五 P5-B03 DDL）
        await _db.Updateable<AiProjectEntity>()
            .SetColumns(x => x.DeploymentVerifiedAt == DateTime.UtcNow)
            .SetColumns(x => x.LastModifyTime == DateTime.UtcNow)
            .Where(x => x.Id == context.ProjectId)
            .ExecuteCommandAsync(ct);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.DeploymentVerified,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.GeneratedCode,
            Payload = JsonSerializer.Serialize(new
            {
                projectId = context.ProjectId,
                pipelineId = context.PipelineId,
                previewUrl,
                downloadUrl,
                // H4: 凭据不应写入 IR 事件存储 — 移除 defaultCredentials
                verifiedAt = DateTime.UtcNow.ToString("O"),
            }, JsonOptions),
        };
    }

    public Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        _ = ct;
        if (events.Any(e => e.EventType == IrEventTypes.DeploymentVerified))
            return Task.FromResult(SkillValidationResult.Ok());

        if (events.Any(e => e.EventType == IrEventTypes.DeploymentFailed))
            return Task.FromResult(SkillValidationResult.Fail("DeploymentFailed 已记录"));

        return Task.FromResult(SkillValidationResult.Fail("缺少 DeploymentVerified 事件"));
    }
}
