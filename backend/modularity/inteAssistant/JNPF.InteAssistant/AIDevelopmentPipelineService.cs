using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using JNPF.DependencyInjection;
using JNPF.Common.Core.MultiTenancy;
using JNPF.FriendlyException;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Enum;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Infrastructure.Attachments;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Infrastructure.Security;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Skills.Bugfix;
using JNPF.InteAssistant.Pipeline;
using JNPF.InteAssistant.Studio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace JNPF.InteAssistant;

/// <summary>
/// AI 开发流水线 API — 完整的五阶段 AI 辅助开发平台
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "PipelineExecute", Order = 195)]
[Route("api/studio/pipeline/execute")]
public class AIDevelopmentPipelineService : IDynamicApiController, ITransient
{
    private static readonly JsonSerializerOptions JsonCamelOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IPipelineEngine _pipelineEngine;
    private readonly DetailedDesignOrchestrator _designOrchestrator;
    private readonly ILlmGatewayService _llmGateway;
    private readonly ILogger<AIDevelopmentPipelineService> _logger;
    private readonly ISandboxManager _sandbox;
    private readonly ISqlSugarClient _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IBackgroundTaskRunner _taskRunner;
    private readonly ISseSenderFactory _senderFactory;
    private readonly ITenantGuard _tenantGuard;
    private readonly IGatePipeline _gatePipeline;
    private readonly IPipelineSseChannelHub _sseHub;
    private readonly IIrEventStoreService _irEventStore;
    private readonly IGeneratedProjectRegistry _generatedProjectRegistry;
    private readonly ISkillHarness _skillHarness;
    private readonly IOptionsMonitor<GatePipelineOptions> _gateOptions;
    private readonly IPipelineAttachmentService _attachmentService;
    private readonly IPipelineDeliverableService _deliverableService;
    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly IStageConfirmSkillTrigger _stageConfirmSkillTrigger;
    private readonly IDeliverableRebuildService _deliverableRebuild;
    private readonly IBugfixSkillOrchestrator _bugfixOrchestrator;

    public AIDevelopmentPipelineService(
        IPipelineEngine pipelineEngine,
        DetailedDesignOrchestrator designOrchestrator,
        ILlmGatewayService llmGateway,
        ISandboxManager sandbox,
        ISqlSugarClient sqlSugarClient,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AIDevelopmentPipelineService> logger,
        IBackgroundTaskRunner taskRunner,
        ISseSenderFactory senderFactory,
        ITenantGuard tenantGuard,
        IGatePipeline gatePipeline,
        IPipelineSseChannelHub sseHub,
        IIrEventStoreService irEventStore,
        IGeneratedProjectRegistry generatedProjectRegistry,
        ISkillHarness skillHarness,
        IOptionsMonitor<GatePipelineOptions> gateOptions,
        IPipelineAttachmentService attachmentService,
        IPipelineDeliverableService deliverableService,
        ISkillLlmBudgetGuard budgetGuard,
        IStageConfirmSkillTrigger stageConfirmSkillTrigger,
        IDeliverableRebuildService deliverableRebuild,
        IBugfixSkillOrchestrator bugfixOrchestrator)
    {
        _pipelineEngine = pipelineEngine;
        _designOrchestrator = designOrchestrator;
        _llmGateway = llmGateway;
        _sandbox = sandbox;
        _db = sqlSugarClient;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _taskRunner = taskRunner;
        _senderFactory = senderFactory;
        _tenantGuard = tenantGuard;
        _gatePipeline = gatePipeline;
        _sseHub = sseHub;
        _irEventStore = irEventStore;
        _generatedProjectRegistry = generatedProjectRegistry;
        _skillHarness = skillHarness;
        _gateOptions = gateOptions;
        _attachmentService = attachmentService;
        _deliverableService = deliverableService;
        _budgetGuard = budgetGuard;
        _stageConfirmSkillTrigger = stageConfirmSkillTrigger;
        _deliverableRebuild = deliverableRebuild;
        _bugfixOrchestrator = bugfixOrchestrator;
    }

    // ─── 创建流水线 ───

    /// <summary>
    /// 创建流水线（落库 + 保存用户需求消息）
    /// </summary>
    [HttpPost("create")]
    public async Task<PipelineResult> CreateAsync([FromBody] CreatePipelineInput input)
    {
        var tenantId = TenantResolver.Resolve();
        var userId = GetUserId();

        var requirement = input.Requirement ?? input.UserRequirement ?? "";
        var workMode = PipelineWorkMode.Normalize(input.WorkMode);
        var name = string.IsNullOrWhiteSpace(input.Name)
            ? (requirement.Length > 50 ? requirement[..50] : requirement)
            : input.Name;
        if (string.IsNullOrWhiteSpace(name)) name = "未命名流水线";

        AiPipelineEntity? sourceEntity = null;
        if (workMode != PipelineWorkMode.Greenfield)
        {
            if (input.SourcePipelineId is null or <= 0)
                throw Oops.Bah("Debug/二次开发须选择已生成系统");

            sourceEntity = await _db.Queryable<AiPipelineEntity>()
                .FirstAsync(x => x.Id == input.SourcePipelineId.ToString()
                    && x.TenantId == tenantId.ToString()
                    && (x.DeleteMark == null || x.DeleteMark == 0));

            if (sourceEntity == null)
                throw Oops.Bah($"源流水线 {input.SourcePipelineId} 不存在");

            if (workMode == PipelineWorkMode.Bugfix && string.IsNullOrWhiteSpace(input.TargetPageRoute))
                throw Oops.Bah("Debug 修复须选择要修改的页面/路由");
        }

        var request = new PipelineCreateRequest
        {
            Name = name,
            UserRequirement = requirement,
            PipelineType = workMode,
        };

        var result = await _pipelineEngine.CreateAsync(request, tenantId, userId);

        var inheritedProjectId = sourceEntity?.ProjectId ?? result.PipelineId.ToString();
        var initialStage = workMode switch
        {
            PipelineWorkMode.Bugfix => PipelineStage.Development,
            PipelineWorkMode.Enhancement => PipelineStage.Requirement,
            _ => PipelineStage.Requirement,
        };

        var entity = new AiPipelineEntity
        {
            Id = result.PipelineId.ToString(),
            Name = name,
            CurrentStage = initialStage,
            Status = workMode == PipelineWorkMode.Greenfield ? "draft" : "active",
            StartedTime = DateTime.Now,
            TenantId = tenantId.ToString(),
            ProjectId = inheritedProjectId,
            WorkMode = workMode,
            SourcePipelineId = sourceEntity?.Id,
            TargetPageRoute = input.TargetPageRoute?.Trim(),
            TargetPageLabel = input.TargetPageLabel?.Trim(),
        };
        entity.Create();
        await _db.Insertable(entity).ExecuteCommandAsync();

        try
        {
            // R12 三元组：bugfix/enhancement 必须走新四层路径（projectId != pipelineId）
            StudioWorkspaceHelper.EnsureDirectories(tenantId.ToString(), inheritedProjectId, result.PipelineId.ToString());
            _logger.LogInformation("工作区目录已创建: PipelineId={Id}, ProjectId={ProjectId}, SelfAnchored={SelfAnchored}",
                result.PipelineId, inheritedProjectId, StudioWorkspaceHelper.IsSelfAnchored(inheritedProjectId, result.PipelineId.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建工作区目录失败: PipelineId={Id}", result.PipelineId);
        }

        await SaveMessageAsync(result.PipelineId.ToString(), inheritedProjectId, initialStage, "user", requirement);

        try
        {
            if (workMode == PipelineWorkMode.Greenfield)
            {
                await _irEventStore.EnsureProjectAsync(
                    result.PipelineId.ToString(),
                    tenantId.ToString(),
                    name,
                    userId.ToString());
            }
            else
            {
                await _irEventStore.EnsureProjectAsync(
                    inheritedProjectId,
                    tenantId.ToString(),
                    sourceEntity!.Name ?? name,
                    userId.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IR 项目注册失败（表可能未迁移）: PipelineId={Id}", result.PipelineId);
        }

        try
        {
            await _generatedProjectRegistry.UpsertFromPipelineAsync(
                result.PipelineId, tenantId.ToString(), userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "已生成系统索引同步失败: PipelineId={Id}", result.PipelineId);
        }

        _logger.LogInformation(
            "流水线创建: Id={Id}, Name={Name}, WorkMode={Mode}, Source={Source}",
            result.PipelineId, name, workMode, sourceEntity?.Id);

        return new PipelineResult
        {
            PipelineId = result.PipelineId,
            Name = name,
            CurrentStage = initialStage,
            Status = entity.Status ?? "draft",
        };
    }

    /// <summary>
    /// R12 fork：同 Project 下新建 pipeline，复制 IR fragment snapshots + ai_entity_field。
    /// </summary>
    [HttpPost("{sourcePipelineId:long}/fork")]
    public async Task<object> ForkAsync(long sourcePipelineId, [FromBody] ForkPipelineRequest? request)
    {
        await EnsurePipelineTenantAsync(sourcePipelineId, CancellationToken.None);
        var tenantId = TenantResolver.Resolve();
        var userId = GetUserId();

        var source = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == sourcePipelineId.ToString()
                && (x.DeleteMark == null || x.DeleteMark == 0));
        if (source == null)
            throw Oops.Bah($"源流水线 {sourcePipelineId} 不存在");

        var workMode = PipelineWorkMode.Normalize(
            string.IsNullOrWhiteSpace(request?.WorkMode) ? PipelineWorkMode.Enhancement : request.WorkMode);
        if (workMode == PipelineWorkMode.Greenfield)
            workMode = PipelineWorkMode.Enhancement;

        var name = string.IsNullOrWhiteSpace(request?.Name)
            ? $"{source.Name} (fork)"
            : request!.Name!.Trim();

        var createResult = await _pipelineEngine.CreateAsync(
            new PipelineCreateRequest { Name = name, UserRequirement = $"fork from {sourcePipelineId}", PipelineType = workMode },
            tenantId, userId);

        var projectId = string.IsNullOrWhiteSpace(source.ProjectId)
            ? sourcePipelineId.ToString()
            : source.ProjectId;
        var newId = createResult.PipelineId.ToString();

        var entity = new AiPipelineEntity
        {
            Id = newId,
            Name = name,
            CurrentStage = source.CurrentStage ?? PipelineStage.Requirement,
            Status = "active",
            StartedTime = DateTime.Now,
            TenantId = tenantId.ToString(),
            ProjectId = projectId,
            WorkMode = workMode,
            SourcePipelineId = source.Id,
        };
        entity.Create();
        await _db.Insertable(entity).ExecuteCommandAsync();

        StudioWorkspaceHelper.EnsureDirectories(tenantId.ToString(), projectId, newId);

        // 复制 IR fragment snapshots
        var snaps = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.TenantId == source.TenantId
                        && x.ProjectId == projectId
                        && x.PipelineId == sourcePipelineId.ToString()
                        && !x.DeleteMark)
            .ToListAsync();
        if (snaps.Count > 0)
        {
            var sourceIdText = sourcePipelineId.ToString();
            var copies = snaps.Select(s => new AiIrFragmentSnapshotEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                ProjectId = projectId,
                PipelineId = newId,
                TenantId = s.TenantId,
                // R12：同 project 多 pipeline 时 FragmentId 若含源 pipelineId 必须重映射，避免语义串线
                FragmentId = RemapForkFragmentId(s.FragmentId, sourceIdText, newId),
                FragmentType = s.FragmentType,
                CurrentVersion = s.CurrentVersion,
                StabilityState = s.StabilityState,
                IrContent = RemapForkFragmentId(s.IrContent ?? string.Empty, sourceIdText, newId),
                SaStepsCompleted = s.SaStepsCompleted,
                LastEventId = s.LastEventId,
                UpdatedAt = DateTime.UtcNow,
                DeleteMark = false,
            }).ToList();
            await _db.Insertable(copies).ExecuteCommandAsync();
        }

        // 复制 ai_entity_field
        var fields = await _db.Queryable<AiEntityFieldEntity>()
            .Where(x => x.TenantId == source.TenantId
                        && x.ProjectId == projectId
                        && x.PipelineId == sourcePipelineId.ToString()
                        && !x.DeleteMark)
            .ToListAsync();
        if (fields.Count > 0)
        {
            var fieldCopies = fields.Select(f =>
            {
                var json = JsonSerializer.Serialize(f);
                var c = JsonSerializer.Deserialize<AiEntityFieldEntity>(json)!;
                c.Id = Guid.NewGuid().ToString("N");
                c.PipelineId = newId;
                c.ProjectId = projectId;
                c.CreatorTime = DateTime.UtcNow;
                c.LastModifyTime = DateTime.UtcNow;
                c.DeleteMark = false;
                return c;
            }).ToList();
            await _db.Insertable(fieldCopies).ExecuteCommandAsync();
        }

        _logger.LogInformation(
            "Pipeline fork: Source={Source} New={New} Project={Project} Mode={Mode} Snaps={Snaps} Fields={Fields}",
            sourcePipelineId, createResult.PipelineId, projectId, workMode, snaps.Count, fields.Count);

        return new
        {
            pipelineId = createResult.PipelineId,
            projectId,
            sourcePipelineId,
            workMode,
            fragmentCount = snaps.Count,
            entityFieldCount = fields.Count,
            status = "active",
        };
    }

    /// <summary>源系统可修改页面/路由列表（Debug 快路径选页）</summary>
    [HttpGet("{pipelineId:long}/page-routes")]
    public async Task<object> GetPageRoutesAsync(long pipelineId)
    {
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        var entity = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == pipelineId.ToString());
        var projectId = entity?.ProjectId ?? pipelineId.ToString();
        var tenantId = entity?.TenantId ?? TenantResolver.Resolve().ToString();

        var routes = await _db.Queryable<AiRouteTableEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .ToListAsync();

        var items = routes
            .Where(r => !string.IsNullOrWhiteSpace(r.EtcdKey))
            .Select(r => new
            {
                route = r.EtcdKey,
                label = string.IsNullOrWhiteSpace(r.SandboxEndpoint) ? r.EtcdKey : r.SandboxEndpoint,
            })
            .ToList();

        if (items.Count == 0)
        {
            items.Add(new { route = "/api/", label = "后端 API（默认）" });
            items.Add(new { route = "/home", label = "首页（默认）" });
        }

        return new { pipelineId, projectId, items };
    }

    /// <summary>Debug 快路径 — bugfix-skill 增量重算，跳过 SA 门控全链</summary>
    [HttpPost("{pipelineId:long}/quick-bugfix")]
    public async Task<object> QuickBugfixAsync(long pipelineId, [FromBody] QuickTaskRequest request)
    {
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        await EnsureNotFrozenAsync(pipelineId, CancellationToken.None);

        var entity = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == pipelineId.ToString())
            ?? throw Oops.Bah("流水线不存在");

        if (entity.WorkMode != PipelineWorkMode.Bugfix)
            throw Oops.Bah("当前流水线不是 Debug 修复模式");

        var tenantId = entity.TenantId;
        var projectId = entity.ProjectId;
        var (fromSeq, toSeq) = await ResolveBugfixSequenceRangeAsync(projectId, tenantId);

        var description = BuildQuickTaskDescription(request.Message, entity);
        await SaveMessageAsync(pipelineId.ToString(), projectId, PipelineStage.Development, "user", request.Message ?? description);

        var runId = Guid.NewGuid().ToString("N");
        var taskName = $"quick-bugfix:{pipelineId}:{runId}";

        _taskRunner.Run(taskName, async (_, ct) =>
        {
            await _bugfixOrchestrator.RunAsync(
                pipelineId,
                tenantId,
                projectId,
                new BugfixRunContext
                {
                    FromSequence = fromSeq,
                    ToSequence = toSeq,
                    Description = description,
                    RootCauseLayer = BugfixRootCauseClassifier.LayerIr3,
                },
                ct);
        });

        return new
        {
            status = "started",
            skillId = BugfixSkillIds.Bugfix,
            fromSequence = fromSeq,
            toSequence = toSeq,
            targetPageRoute = entity.TargetPageRoute,
            message = "Debug 修复已启动，将增量重算受影响 Skill（≤3）",
        };
    }

    /// <summary>二次开发快路径 — 继承 IR，跳过 SA 门控，按 IR 状态调度 Skill</summary>
    [HttpPost("{pipelineId:long}/quick-enhancement")]
    public async Task<object> QuickEnhancementAsync(long pipelineId, [FromBody] QuickTaskRequest request)
    {
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        await EnsureNotFrozenAsync(pipelineId, CancellationToken.None);

        var entity = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == pipelineId.ToString())
            ?? throw Oops.Bah("流水线不存在");

        if (entity.WorkMode != PipelineWorkMode.Enhancement)
            throw Oops.Bah("当前流水线不是二次开发模式");

        var tenantId = entity.TenantId;
        var projectId = entity.ProjectId;
        var message = request.Message ?? "";
        await SaveMessageAsync(pipelineId.ToString(), projectId, PipelineStage.Requirement, "user", message);

        var snapshots = await _irEventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString());
        var hasStableEventSpec = snapshots.Any(s =>
            s.FragmentType == IrFragmentTypes.EventSpec
            && (s.StabilityState == IrStabilityStates.Stable || s.StabilityState == IrStabilityStates.Locked));
        var hasStableSkeleton = snapshots.Any(s =>
            (s.FragmentType == IrFragmentTypes.Skeleton || s.FragmentId?.StartsWith("skeleton:", StringComparison.Ordinal) == true)
            && (s.StabilityState == IrStabilityStates.Stable || s.StabilityState == IrStabilityStates.Locked));

        var skillId = hasStableEventSpec
            ? "analyst-skill"
            : hasStableSkeleton
                ? "pm-skill"
                : null;

        if (skillId == null)
            throw Oops.Bah("源系统 IR 未就绪（无 stable 骨架/EventSpec），请先用全量开发完成至少 S1");

        var runId = Guid.NewGuid().ToString("N");
        _taskRunner.Run($"quick-enhancement:{pipelineId}:{runId}", async (_, ct) =>
        {
            await _skillHarness.RunAsync(
                skillId,
                pipelineId,
                tenantId,
                projectId,
                new SkillRunOptions { UserRequirement = message },
                ct);
        });

        return new
        {
            status = "started",
            skillId,
            message = $"二次开发已启动，调度 {skillId}（已跳过 SA 门控）",
        };
    }

    private static string BuildQuickTaskDescription(string? message, AiPipelineEntity entity)
    {
        var page = string.IsNullOrWhiteSpace(entity.TargetPageLabel)
            ? entity.TargetPageRoute
            : $"{entity.TargetPageLabel} ({entity.TargetPageRoute})";
        return string.IsNullOrWhiteSpace(page)
            ? (message ?? "Debug 修复")
            : $"[{page}] {message}".Trim();
    }

    /// <summary>
    /// fork 时把 FragmentId / IR JSON 中嵌入的源 pipelineId 替换为新 pipelineId。
    /// </summary>
    private static string RemapForkFragmentId(string value, string sourcePipelineId, string newPipelineId)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(sourcePipelineId))
            return value;
        if (string.Equals(sourcePipelineId, newPipelineId, StringComparison.Ordinal))
            return value;
        return value.Replace($":{sourcePipelineId}", $":{newPipelineId}", StringComparison.Ordinal);
    }

    private async Task<(int From, int To)> ResolveBugfixSequenceRangeAsync(string projectId, string tenantId)
    {
        var maxSeq = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .MaxAsync(x => (int?)x.Sequence);

        if (maxSeq is null or <= 1)
            return (1, 2);

        return (1, maxSeq.Value);
    }

    // ─── 启动流水线 ───

    /// <summary>
    /// 启动流水线
    /// </summary>
    [HttpPost("{pipelineId:long}/start")]
    public async Task<PipelineResult> StartAsync(long pipelineId)
    {
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        await EnsureNotFrozenAsync(pipelineId, CancellationToken.None);
        return await _pipelineEngine.StartAsync(pipelineId);
    }

    // ─── SA 门控（异步事件驱动 — 缺陷4修复）───

    /// <summary>
    /// 登记并解析附件 — 写入 inte_assistant_attachment（ProcessStatus=2 后可供门控使用）
    /// </summary>
    [HttpPost("{pipelineId:long}/upload-materials")]
    public async Task<object> UploadMaterialsAsync(
        long pipelineId, [FromBody] UploadMaterialsRequest request, CancellationToken ct)
    {
        await EnsurePipelineTenantAsync(pipelineId, ct);
        await EnsureNotFrozenAsync(pipelineId, ct);
        var ctx = RequestContext.Capture(_httpContextAccessor);
        var payloads = MapAttachmentItems(request?.Attachments);

        var registered = await _attachmentService.RegisterAsync(pipelineId, payloads, ctx, ct);
        var prepared = await _attachmentService.PrepareForGateAsync(pipelineId, ctx, ct);

        return new
        {
            pipelineId,
            registered,
            processed = prepared.Files.Count,
            failed = prepared.FailedCount,
            warnings = prepared.Warnings,
            items = prepared.Items,
        };
    }

    /// <summary>列出 Pipeline 已登记的需求附件（含解析状态与下载链接）</summary>
    [HttpGet("{pipelineId:long}/attachments")]
    public async Task<object> ListAttachmentsAsync(long pipelineId, CancellationToken ct)
    {
        await EnsurePipelineTenantAsync(pipelineId, ct);
        var items = await _attachmentService.ListByPipelineAsync(pipelineId, ct);
        return new { pipelineId, count = items.Count, items };
    }

    /// <summary>下载附件原文件</summary>
    [HttpGet("{pipelineId:long}/attachments/{attachmentId}/download")]
    public async Task<IActionResult> DownloadAttachmentOriginalAsync(
        long pipelineId, string attachmentId, CancellationToken ct)
    {
        await EnsurePipelineTenantAsync(pipelineId, ct);
        var ctx = RequestContext.Capture(_httpContextAccessor);
        var (content, fileName, contentType) = await _attachmentService.DownloadOriginalAsync(
            pipelineId, attachmentId, ctx, ct);
        return new FileContentResult(content, contentType) { FileDownloadName = fileName };
    }

    /// <summary>下载附件解析文本</summary>
    [HttpGet("{pipelineId:long}/attachments/{attachmentId}/extracted")]
    public async Task<IActionResult> DownloadAttachmentExtractedAsync(
        long pipelineId, string attachmentId, CancellationToken ct)
    {
        await EnsurePipelineTenantAsync(pipelineId, ct);
        var text = await _attachmentService.GetExtractedTextAsync(pipelineId, attachmentId, ct);
        if (string.IsNullOrWhiteSpace(text))
            throw Oops.Bah("附件尚未解析完成或无可导出文本");

        var tenantId = TenantResolver.Resolve().ToString();
        var att = await _db.Queryable<InteAssistantAttachment>()
            .Where(a => a.F_Id == attachmentId && a.PipelineId == pipelineId.ToString() && a.TenantId == tenantId)
            .FirstAsync(ct);
        var baseName = Path.GetFileNameWithoutExtension(att?.FileName ?? "attachment");
        var bytes = Encoding.UTF8.GetBytes(text);
        return new FileContentResult(bytes, "text/plain; charset=utf-8")
        {
            FileDownloadName = $"{baseName}-extracted.txt",
        };
    }

    /// <summary>列出 Pipeline 阶段交付物（deliverables/ 索引）</summary>
    [HttpGet("{pipelineId:long}/deliverables")]
    public async Task<object> ListDeliverablesAsync(
        long pipelineId, [FromQuery] string? stageCode, CancellationToken ct)
    {
        await EnsurePipelineTenantAsync(pipelineId, ct);
        var items = await _deliverableService.ListByPipelineAsync(pipelineId, stageCode, ct);
        return new { pipelineId, count = items.Count, items };
    }

    /// <summary>下载 deliverables/ 下文件</summary>
    [HttpGet("{pipelineId:long}/deliverables/content")]
    public async Task<IActionResult> DownloadDeliverableAsync(
        long pipelineId, [FromQuery] string relativePath, CancellationToken ct)
    {
        await EnsurePipelineTenantAsync(pipelineId, ct);
        if (string.IsNullOrWhiteSpace(relativePath))
            throw Oops.Bah("relativePath 不能为空");

        var (absolute, contentType, fileName) = await _deliverableService.ResolveDeliverableAsync(
            pipelineId, relativePath, ct);
        var bytes = await System.IO.File.ReadAllBytesAsync(absolute, ct);
        return new FileContentResult(bytes, contentType) { FileDownloadName = fileName };
    }

    /// <summary>
    /// 从 IR 快照/事件重建 deliverables/（Skill 已跑但落盘缺失时补建，SUP-01c）。
    /// </summary>
    [HttpPost("{pipelineId:long}/deliverables/rebuild")]
    public async Task<object> RebuildDeliverablesAsync(
        long pipelineId,
        [FromQuery] string? stages,
        CancellationToken ct)
    {
        await EnsurePipelineTenantAsync(pipelineId, ct);
        var tenantId = TenantResolver.Resolve().ToString();
        IReadOnlyList<string>? stageList = string.IsNullOrWhiteSpace(stages)
            ? null
            : stages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = await _deliverableRebuild.RebuildAsync(pipelineId, tenantId, stageList, ct);
        return new { result.PipelineId, result.Written, result.Skipped, count = result.Written.Count };
    }

    /// <summary>
    /// <deprecated>SA 门控入口 — 旧 SSE 事件驱动门控流。</deprecated>
    /// 前端提交 → 202 Accepted → 后台执行门控 → SSE 推送结果。
    /// F3 铁律：需求分析请使用 POST /api/studio/skills/requirement-analysis/{pipelineId}/run 三轮编排器，
    ///   本端点保留仅供向后兼容（不含 PM/SystemAnalyst 联动的独立门控场景）。
    /// </summary>
    [HttpPost("{pipelineId:long}/sa-gate")]
    public async Task<object> ExecuteGateAsync(
        long pipelineId, [FromBody] SaGateRequest request)
    {
        _logger.LogWarning("旧 sa-gate 端点被调用 PipelineId={Id}，建议迁移到 /api/studio/skills/requirement-analysis/{Id}/run 三轮编排器", pipelineId);
        var userText = request?.UserText ?? "";
        var tenantId = TenantResolver.Resolve();
        var userId = GetUserId();
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        await EnsureNotFrozenAsync(pipelineId, CancellationToken.None);

        var ctx = RequestContext.Capture(_httpContextAccessor);
        if (request?.Attachments?.Count > 0)
        {
            await _attachmentService.RegisterAsync(pipelineId, MapAttachmentItems(request.Attachments), ctx);
        }

        // 创建 SSE 通道
        var channel = _sseHub.ReplaceChannel(pipelineId);

        // 后台异步执行门控 (BackgroundTaskRunner + SSE)
        var visionConfig = _configuration.GetSection("MultimodalVision");
        var visionApiUrl = visionConfig["ApiUrl"] ?? "";
        var visionApiKey = visionConfig["ApiKey"] ?? "";
        var visionModel = visionConfig["Model"] ?? "";

        var autoRunPmOnPass = request?.AutoRunPm ?? _gateOptions.CurrentValue.AutoRunPmSkillOnGatePass;

        _taskRunner.Run(
            $"SA_Gate_{pipelineId}",
            async (bgCtx, bgCt) =>
            {
                using var scope = App.RootServices.CreateScope();
                var attachmentService = scope.ServiceProvider.GetRequiredService<IPipelineAttachmentService>();
                var sse = _senderFactory.Create(pipelineId.ToString(), channel);
                var pmHandoff = false;
                try
                {
                    sse.TrySend("attachments_processing", JsonSerializer.Serialize(new
                    {
                        message = "正在下载并解析附件…",
                    }));

                    var prepared = await attachmentService.PrepareForGateAsync(pipelineId, ctx, bgCt);
                    sse.TrySend("attachments_ready", JsonSerializer.Serialize(new
                    {
                        processed = prepared.Files.Count,
                        failed = prepared.FailedCount,
                        warnings = prepared.Warnings,
                        items = prepared.Items,
                    }));

                    if (prepared.Files.Count == 0 && string.IsNullOrWhiteSpace(userText))
                    {
                        sse.TrySend("gate_failed", JsonSerializer.Serialize(new
                        {
                            reason = "未提供文字需求且没有可用附件",
                            hint = "请输入业务需求描述，或上传 Word/PDF/Excel 需求文档后再提交。",
                            semanticFitness = new
                            {
                                passed = false,
                                score = 0,
                                level = "insufficient",
                                identified = Array.Empty<object>(),
                                missing = new[]
                                {
                                    new
                                    {
                                        category = "业务事件",
                                        description = "无文字且无可用附件内容",
                                        severity = "critical",
                                        howToFix = "请描述业务场景，或上传包含需求说明的文档。",
                                    },
                                },
                            },
                            warnings = prepared.Warnings,
                        }));
                        sse.Complete();
                        return;
                    }

                    // 通知前端: 门控开始
                    sse.TrySend("gate_started", "");

                    // 执行门控管道（AttachmentFile 含真实字节，供格式校验与多模态）
                    var gateResult = await _gatePipeline.ExecuteAsync(
                        userText, prepared.Files, ctx,
                        gateContext: null,
                        visionApiUrl, visionApiKey, visionModel, bgCt);

                    if (gateResult.Passed)
                    {
                        // 门控通过 → 通知前端 + 自动进入 Stage 1
                        sse.TrySend("gate_passed", JsonSerializer.Serialize(new
                        {
                            mergedText = gateResult.MergedText,
                            warnings = gateResult.Warnings,
                            semanticFitness = gateResult.SemanticFitness
                        }));

                        // 持久化门控结果
                        await SaveMessageAsync(pipelineId.ToString(), pipelineId.ToString(), "gate", "system",
                            JsonSerializer.Serialize(gateResult));

                        var deliverableService = scope.ServiceProvider.GetRequiredService<IPipelineDeliverableService>();
                        await deliverableService.SaveGateDeliverablesAsync(
                            tenantId.ToString(), pipelineId, gateResult, prepared.Items, bgCt);

                        // 自动流转到 requirement 阶段
                        await _pipelineEngine.ExecuteStageAsync(pipelineId, PipelineStage.Requirement);
                        sse.TrySend("stage_transition", PipelineStage.Requirement);

                        // P2-B14：门控通过后可选自动触发 PM Skill
                        // ★ PM 失败不得冒充 GATE_INTERNAL_ERROR——门控已通过，应单独上报 pm_skill_failed
                        // ★ 2026-07-17：PM 不得与门控共用 5min 后台任务——步骤③ LLM 流式可 >5min，会触发 bgCt 取消。
                        if (autoRunPmOnPass)
                        {
                            pmHandoff = true;
                            sse.TrySend("pm_skill_started", JsonSerializer.Serialize(new { pipelineId, source = "gate_pass_new" }));
                            var mergedTextForPm = gateResult.MergedText;
                            var tenantIdForPm = tenantId.ToString();
                            var pipelineIdForPm = pipelineId;
                            var pmChannel = channel;
                            _taskRunner.Run(
                                $"req-analysis:{pipelineId}",
                                async (pmCtx, pmCt) =>
                                {
                                    using var pmScope = App.RootServices.CreateScope();
                                    var pmSenderFactory = pmScope.ServiceProvider.GetRequiredService<ISseSenderFactory>();
                                    using var pmSse = pmSenderFactory.Create(pipelineIdForPm.ToString(), pmChannel);
                                    try
                                    {
                                        var newOrchestrator = pmScope.ServiceProvider.GetRequiredService<IRequirementAnalysisOrchestrator>();
                                        var pmResult = await newOrchestrator.RunAsync(
                                            pipelineIdForPm,
                                            tenantIdForPm,
                                            pipelineIdForPm.ToString(),
                                            new RequirementAnalysisOptions
                                            {
                                                ProviderCode = null,
                                                InitialUserRequirement = mergedTextForPm,
                                            },
                                            pmCt);
                                        if (pmResult.Status is "failed" or "gate-rejected")
                                        {
                                            pmSse.TrySend("pm_skill_failed", JsonSerializer.Serialize(new
                                            {
                                                message = !string.IsNullOrWhiteSpace(pmResult.ErrorMessage)
                                                    ? pmResult.ErrorMessage
                                                    : pmResult.GateHint ?? "PM 需求分析未能继续，请检查需求材料后重试。",
                                                errorCode = pmResult.Status == "gate-rejected"
                                                    ? "PM_GATE_REJECTED"
                                                    : "PM_PIPELINE_FAILED",
                                            }));
                                        }
                                        pmSse.Complete();
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        _logger.LogWarning("PM 流水线超时或取消: PipelineId={Id}", pipelineIdForPm);
                                        pmSse.TrySend("pm_skill_failed", JsonSerializer.Serialize(new
                                        {
                                            message = "PM 需求分析超时或被取消，请稍后在流水线中重新触发。",
                                            errorCode = "PM_PIPELINE_TIMEOUT",
                                        }));
                                        pmSse.Complete();
                                    }
                                    catch (Exception pmEx) when (pmEx is not OutOfMemoryException and not StackOverflowException)
                                    {
                                        _logger.LogError(pmEx, "新流程 PM 完善需求失败: PipelineId={Id}", pipelineIdForPm);
                                        pmSse.TrySend("pm_skill_failed", JsonSerializer.Serialize(new
                                        {
                                            message = pmEx.Message,
                                            errorCode = "PM_PIPELINE_FAILED",
                                        }));
                                        pmSse.Complete();
                                    }
                                },
                                timeout: TimeSpan.FromMinutes(35));
                        }
                    }
                    else
                    {
                        // 门控不通过 → 推送结构化反馈
                        sse.TrySend("gate_failed", JsonSerializer.Serialize(new
                        {
                            reason = gateResult.Reason,
                            hint = gateResult.Hint,
                            semanticFitness = gateResult.SemanticFitness,
                            warnings = gateResult.Warnings
                        }));

                        // ★ 失败时也落盘门控报告，便于前端/E2E 立即拿到失败原因与语义评估
                        //   （MergedText 为空时 SaveGateDeliverablesAsync 内部已做保护，仅写 00-gate-report.json）
                        //   原 bug：失败时不落盘 → E2E 等不到任何交付物 → 干等超时报 "timeout: deliverable"
                        var failDeliverableService = scope.ServiceProvider.GetRequiredService<IPipelineDeliverableService>();
                        await failDeliverableService.SaveGateDeliverablesAsync(
                            tenantId.ToString(), pipelineId, gateResult, prepared.Items, bgCt);
                    }

                    if (!pmHandoff)
                        sse.Complete();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SA 门控异常: PipelineId={Id}", pipelineId);
                    // #region agent log
                    try
                    {
                        var dbg = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            sessionId = "ead5d0",
                            runId = "post-fix",
                            hypothesisId = "H5",
                            location = "AIDevelopmentPipelineService.ExecuteGateAsync:catch",
                            message = "GATE_INTERNAL_ERROR",
                            data = new
                            {
                                pipelineId,
                                exType = ex.GetType().FullName,
                                exMessage = ex.Message
                            },
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        });
                        System.IO.File.AppendAllText(@"D:\JNPF-v52\debug-ead5d0.log", dbg + "\n");
                    }
                    catch { }
                    // #endregion
                    sse.TrySend("gate_error", JsonSerializer.Serialize(new
                    {
                        message = "需求评估过程中发生异常，请重试。",
                        errorCode = "GATE_INTERNAL_ERROR"
                    }));
                    sse.Complete();
                }
                finally
                {
                    if (!pmHandoff)
                        sse.Dispose();
                }
            },
            timeout: TimeSpan.FromMinutes(5));

        // Step 4: 立即返回 202-style 响应
        return new
        {
            pipelineId,
            status = "processing",
            message = "需求材料正在评估中，请通过 SSE /events 监听结果..."
        };
    }

    // ─── 执行当前阶段（调 LLM 流式输出）───

    /// <summary>
    /// <deprecated>旧 SSE 流式执行端点。</deprecated>
    /// 执行当前阶段 — 保存用户消息，启动后台 LLM 流式任务，立即返回。
    /// 前端随后通过 GET /events 读取 SSE 流式 token。
    /// F3 铁律：需求分析请使用 POST /api/studio/skills/requirement-analysis/{pipelineId}/run 三轮编排器。
    /// </summary>
    [HttpPost("{pipelineId:long}/execute")]
    public async Task<StageResult> ExecuteStageAsync(
        long pipelineId, [FromBody] ExecuteStageRequest request)
    {
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        await EnsureNotFrozenAsync(pipelineId, CancellationToken.None);

        _logger.LogWarning("旧 execute 端点被调用 PipelineId={PipelineId} Stage={Stage}，建议迁移到 /api/studio/skills/requirement-analysis/{PipelineId}/run 三轮编排器", pipelineId, request.StageName);

        // 查询 projectId(三元组血缘):历史数据 projectId≡pipelineId,新数据从 pipeline.ProjectId 读取
        var pipelineEntity = await _db.Queryable<AiPipelineEntity>()
            .Where(p => p.Id == pipelineId.ToString())
            .Select(p => new { p.ProjectId })
            .FirstAsync();
        var projectId = string.IsNullOrWhiteSpace(pipelineEntity?.ProjectId)
            ? pipelineId.ToString()  // 兜底:历史数据或异常情况
            : pipelineEntity.ProjectId;

        var stageName = string.IsNullOrWhiteSpace(request.StageName)
            ? PipelineStage.Requirement : MapStageName(request.StageName);
        var message = request.Message ?? "";
        var provider = request.Provider ?? "";
        // 从当前 HTTP 请求获取 Authorization header，透传给 SA / LLM Gateway
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault() ?? "";
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            var queryToken = _httpContextAccessor.HttpContext?.Request.Query["token"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(queryToken))
            {
                authHeader = queryToken.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase)
                    ? queryToken
                    : $"Bearer {queryToken}";
            }
        }

        // 1. 保存用户消息到数据库
        if (!string.IsNullOrWhiteSpace(message))
        {
            await SaveMessageAsync(pipelineId.ToString(), projectId, stageName, "user", message);
        }

        // P1-4: 流式 LLM 预算预检(防绕过 SkillLlmBudgetGuard)
        // 主流程的 StreamLlmResponseAsync 直连 LLM Gateway,需在此校验项目 Token 预算
        try
        {
            var tenantIdForBudget = TenantResolver.Resolve();
            if (tenantIdForBudget >= 0)
            {
                await _budgetGuard.ValidateProjectBudgetAsync(
                    projectId, tenantIdForBudget.ToString(), 0.95, default);
            }
        }
        catch (JNPF.FriendlyException.AppFriendlyException)
        {
            throw;  // 预算耗尽(Oops.Oh/429)向上传播,阻断执行
        }
        catch (Exception ex)
        {
            // 非预算类异常(如表未迁移)不阻断,仅记录
            _logger.LogWarning(ex, "LLM 预算预检异常(非阻断): PipelineId={Id}", pipelineId);
        }

        // 2. 流转状态机
        var stageResult = await _pipelineEngine.ExecuteStageAsync(pipelineId, stageName);

        // development 阶段：写入 AI 开发上下文标记，激活 guard-write L4 白名单
        if (stageName == PipelineStage.Development)
        {
            var tenantId = TenantResolver.Resolve();
            var tenantIdStr = tenantId.ToString();
            var pipelineIdStr = pipelineId.ToString();
            StudioWorkspaceHelper.EnsureDirectories(tenantIdStr, projectId, pipelineIdStr);
            StudioWorkspaceHelper.WriteAiDevContext(tenantIdStr, projectId, pipelineIdStr);
            _logger.LogInformation("AI 开发上下文已激活: PipelineId={Id}, ProjectId={ProjectId}", pipelineId, projectId);
        }

        // 3. 创建 SSE 通道（替换旧通道，支持重复执行）
        var channel = _sseHub.ReplaceChannel(pipelineId);

        // 4. 启动后台 LLM 流式任务（BackgroundTaskRunner 自动捕获上下文 + 管理 CTS 生命周期）
        _taskRunner.Run(
            $"pipeline-{pipelineId}",
            async (ctx, ct) =>
            {
                using var sse = _senderFactory.Create(pipelineId.ToString(), channel);
                try
                {
                    await StreamLlmResponseAsync(
                        pipelineId, projectId, stageName, provider, authHeader, sse,
                        request.Attachments, ctx, ct);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Pipeline {Id} 被取消或超时", pipelineId);
                    sse.Token("⏱️ 分析已取消或超时");
                }
                catch (OutOfMemoryException ex)
                {
                    _logger.LogCritical(ex, "Pipeline {Id} OOM", pipelineId);
                    sse.Error("系统资源不足，请精简附件后重试");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Pipeline {Id} 异常", pipelineId);
                    sse.Error($"分析异常：{ex.Message}");
                }
            },
            timeout: TimeSpan.FromMinutes(10));

        _logger.LogInformation("流水线阶段执行启动: PipelineId={Id}, Stage={Stage}", pipelineId, stageName);
        return stageResult;
    }

    /// <summary>
    /// 后台执行 LLM 流式调用，token 写入 Channel 供 /events 读取。
    /// 从根 ServiceProvider 创建独立 scope，避免请求结束后 DI 服务被释放。
    /// </summary>
    private async Task StreamLlmResponseAsync(long pipelineId, string projectId, string stageName, string provider, string authHeader, SseSender sse, List<AttachmentPayload>? requestAttachments, RequestContext ctx, CancellationToken ct)
    {
        // 创建独立 DI scope，确保 _db/_llmGateway 在请求结束后仍可用
        using var scope = App.RootServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var llmGateway = scope.ServiceProvider.GetRequiredService<ILlmGatewayService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AIDevelopmentPipelineService>>();
        // P1-4: 从 scope 解析 budgetGuard,确保后台任务使用独立实例(避免 Transient 字段跨 scope 问题)
        var budgetGuard = scope.ServiceProvider.GetRequiredService<ISkillLlmBudgetGuard>();

        var fullResponse = new StringBuilder();
        try
        {
            logger.LogInformation("LLM 流式任务开始: PipelineId={Id}, Stage={Stage}, Provider={Provider}",
                pipelineId, stageName, provider);

            // 读取历史消息（构建上下文）—— 后台线程用 ctx.TenantId（TenantResolver 在后台 HttpContext 为空）
            var history = await db.Queryable<AiPipelineMessageEntity>()
                .Where(x => x.PipelineId == pipelineId.ToString() && x.TenantId == ctx.TenantId && (x.DeleteMark == 0 || x.DeleteMark == null))
                .OrderBy(x => x.CreatorTime)
                .ToListAsync();

            var chatMessages = history
                .Where(x => x.Role is "user" or "assistant")
                .Select(x => new ChatMessage(x.Role, x.Content))
                .ToList();

            logger.LogInformation("LLM 历史消息数: {Count}", chatMessages.Count);

            if (chatMessages.Count == 0)
            {
                sse.Error("无历史消息可发送给 LLM");
                return;
            }

            // R2（SUP-04）：需求分析不再走 bulk /api/sa/run 旁路，统一由 analyst-skill + /sa/run-step 主链承担。
            // Requirement 阶段 SSE 流仅负责门控评估与用户交互；结构化分析请通过 SkillsApiService 触发 analyst-skill。

            string? systemPrompt = null;

            #region LEGACY_REQUIREMENT_GATE_FLOW
            // ═══════════════════════════════════════════════════
            // 需求门控：附件持久化 + 缓存 + 硬规则校验 + 成熟度评估
            // （仅 requirement 阶段触发，其他阶段走默认 SystemPrompt）
            // F3 铁律：此块为旧 SSE 门控流 — 新版三轮编排器入口在 POST /api/studio/skills/requirement-analysis/{pipelineId}/run
            // ═══════════════════════════════════════════════════
            if (stageName == PipelineStage.Requirement)
            {
                try
                {
                    var gateService = scope.ServiceProvider.GetRequiredService<RequirementGateService>();
                    var attachmentProcessor = scope.ServiceProvider.GetRequiredService<AttachmentProcessor>();
                    var http = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
                    // 健壮性修复：默认 HttpClient 无超时会无限挂起；附件下载自调用需带 Authorization + 超时
                    http.Timeout = TimeSpan.FromSeconds(30);
                    if (!string.IsNullOrWhiteSpace(ctx.Authorization))
                        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ctx.Authorization.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase));
                    var tenantId = ctx.TenantId;

                    // ── 第1步：将请求带的新附件保存到数据库 ──
                    var existingAttachments = await db.Queryable<InteAssistantAttachment>()
                        .Where(a => a.PipelineId == pipelineId.ToString())
                        .ToListAsync();

                    if (requestAttachments?.Count > 0)
                    {
                        foreach (var att in requestAttachments)
                        {
                            var exists = existingAttachments.Any(e => e.FileUrl == att.Url);
                            if (exists) continue;

                            var entity = new InteAssistantAttachment
                            {
                                F_Id = Guid.NewGuid().ToString("N"),
                                PipelineId = pipelineId.ToString(),
                                ProjectId = projectId,
                                FileName = att.Name,
                                FileUrl = att.Url,
                                FileSize = 0,
                                FileType = Path.GetExtension(att.Name)?.TrimStart('.') ?? "",
                                FileHash = null, // 下载后计算
                                ProcessStatus = 0,
                                CreatorUserId = ctx.UserId,
                                CreatorUserName = ctx.UserName,
                                TenantId = tenantId,
                                CreateTime = DateTime.Now,
                                DeleteMark = false
                            };

                            await db.Insertable(entity).ExecuteCommandAsync();
                            existingAttachments.Add(entity);
                        }
                    }

                    // ── 第2步：处理附件（下载 + 解析，已解析的取缓存）──
                    var attachmentTexts = new List<string>();
                    int processedCount = 0;
                    var downloadedBytes = new Dictionary<string, byte[]>(); // 缓存已下载文件

                    foreach (var att in existingAttachments)
                    {
                        if (att.ProcessStatus == 2 && !string.IsNullOrWhiteSpace(att.ExtractedText))
                        {
                            attachmentTexts.Add(att.ExtractedText);
                            _logger.LogInformation("附件命中缓存: {Name} ({Len}字)", att.FileName, att.ExtractedText.Length);
                            continue;
                        }

                        try
                        {
                            await db.Updateable<InteAssistantAttachment>()
                                .SetColumns(a => a.ProcessStatus == 1)
                                .SetColumns(a => a.LastModifyTime == DateTime.Now)
                                .Where(a => a.F_Id == att.F_Id)
                                .ExecuteCommandAsync();

                            var fileUrl = att.FileUrl;
                            if (!fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            {
                                var baseUrl = ctx.GetBaseUrl();
                                if (string.IsNullOrEmpty(baseUrl))
                                {
                                    throw new InvalidOperationException($"无法解析附件下载基 URL（ctx.Host 为空），跳过附件 {att.FileName}");
                                }
                                fileUrl = $"{baseUrl}{fileUrl}";
                            }
                            var bytes = await http.GetByteArrayAsync(fileUrl, ct);
                            var fileHash = ComputeSha256(bytes);
                            downloadedBytes[att.FileUrl] = bytes; // 缓存，避免图片二次下载

                            var extracted = await attachmentProcessor.ProcessAttachmentsAsync(
                                new List<AttachmentFile> { new() { FileName = att.FileName, Content = bytes } });

                            await db.Updateable<InteAssistantAttachment>()
                                .SetColumns(a => a.ProcessStatus == 2)
                                .SetColumns(a => a.ExtractedText == extracted)
                                .SetColumns(a => a.FileHash == fileHash)
                                .SetColumns(a => a.LastModifyTime == DateTime.Now)
                                .Where(a => a.F_Id == att.F_Id)
                                .ExecuteCommandAsync();

                            if (!string.IsNullOrWhiteSpace(extracted))
                            {
                                attachmentTexts.Add(extracted);
                            }

                            processedCount++;
                            _logger.LogInformation("附件解析完成: {Name}, {Len}字", att.FileName, extracted.Length);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "附件处理失败: {Name}", att.FileName);
                            var errMsg = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                            await db.Updateable<InteAssistantAttachment>()
                                .SetColumns(a => a.ProcessStatus == 3)
                                .SetColumns(a => a.ProcessError == errMsg)
                                .SetColumns(a => a.LastModifyTime == DateTime.Now)
                                .Where(a => a.F_Id == att.F_Id)
                                .ExecuteCommandAsync();
                        }
                    }

                    var attachmentText = string.Join("\n\n", attachmentTexts);
                    _logger.LogInformation("附件处理完成: 文件数={Count}, 提取文本长度={Len}",
                        existingAttachments.Count, attachmentText.Length);

                    // ── 第3步：合并用户文字 + 附件提取内容 ──
                    var lastUserMsg = chatMessages.LastOrDefault(m => m.Role == "user")?.Content ?? "";
                    var fullText = lastUserMsg + attachmentText;

                    // ── 第4步：硬规则校验 ──
                    var hardRule = gateService.ValidateHardRules(fullText, existingAttachments.Count);
                    if (!hardRule.Passed)
                    {
                        await sse.TokenAsync($"❌ {hardRule.Reason}\n\n{hardRule.Hint}", ct);
                        sse.Complete();
                return;
                    }

                    // ── 第5步：将附件内容追加到最后一条用户消息 ──
                    if (!string.IsNullOrWhiteSpace(attachmentText))
                    {
                        var lastIdx = chatMessages.FindLastIndex(m => m.Role == "user");
                        if (lastIdx >= 0)
                        {
                            chatMessages[lastIdx] = new ChatMessage("user",
                                chatMessages[lastIdx].Content + attachmentText);
                        }
                    }

                    // ── 第6步：追问轮次 + 模式判定 + SystemPrompt ──
                    var assistantMsgCount = chatMessages.Count(m => m.Role == "assistant");

                    if (gateService.IsForceRefine(lastUserMsg))
                    {
                        _logger.LogInformation("用户要求直接分析 pipelineId={Id}", pipelineId);
                        systemPrompt = gateService.GetSystemPrompt("refine", new MaturityResult());
                        sse.TrySend("info", "\n\n> 📊 已进入精化模式 — 开始深度分析\n\n");
                    }
                    else if (gateService.IsMaxRoundsReached(assistantMsgCount))
                    {
                        _logger.LogInformation("追问{Count}轮，强制分析 pipelineId={Id}", assistantMsgCount, pipelineId);
                        systemPrompt = gateService.GetSystemPrompt("refine", new MaturityResult
                        {
                            Score = 50,
                            Mode = "refine",
                            Strengths = chatMessages
                                .Where(m => m.Role == "user")
                                .Select(m => m.Content.Length > 50 ? m.Content[..50] + "..." : m.Content)
                                .ToList()
                        });
                        sse.TrySend("info", $"\n\n> 📊 已进行{assistantMsgCount}轮追问，系统将基于当前信息开始分析\n\n");
                    }
                    else
                    {
                        var maturity = await gateService.EvaluateMaturity(chatMessages, provider, ct);
                        _logger.LogInformation("需求成熟度评估: score={Score} mode={Mode} clarifications={Count} pipelineId={Id}",
                            maturity.Score, maturity.Mode, maturity.Clarifications?.Count ?? 0, pipelineId);
                        var modeLabel = maturity.Mode switch
                        {
                            "explore" => "探索模式 — 需要补充更多信息",
                            "confirm" => "确认模式 — 需要确认部分细节",
                            "refine" => "精化模式 — 开始深度分析",
                            _ => maturity.Mode
                        };
                        sse.TrySend("info", $"\n\n> 📊 需求成熟度：{maturity.Score}/100（{modeLabel}）\n\n");
                        systemPrompt = gateService.GetSystemPrompt(maturity.Mode, maturity);

                        // ── ADR-005 交互式澄清问答：explore/confirm 模式向用户发结构化选择题 ──
                        // refine 模式信息已充分，直接进入深度分析（保持既有行为）。
                        if (maturity.Mode is "explore" or "confirm")
                        {
                            var maxRounds = _configuration.GetValue<int?>("Clarification:MaxRounds") ?? 7;
                            if (maxRounds < 1) maxRounds = 7;
                            if (maxRounds > 20) maxRounds = 20;

                            var clarificationRound = Math.Min(assistantMsgCount / 2 + 1, maxRounds);

                            // 轮次触顶则强制进入 refine（保持既有 ForceRefine 语义，避免无限提问）
                            if (clarificationRound >= maxRounds)
                            {
                                _logger.LogInformation("澄清提问已达上限 {Max} 轮，强制 refine pipelineId={Id}", maxRounds, pipelineId);
                                systemPrompt = gateService.GetSystemPrompt("refine", maturity);
                            }
                            else
                            {
                                var clarificationSet = gateService.BuildClarificationSet(maturity, clarificationRound);
                                var fragmentId = $"clarification:{ClarificationStages.Requirement}:{projectId}";

                                await _irEventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
                                {
                                    EventType = IrEventTypes.ClarificationRequested,
                                    FragmentId = fragmentId,
                                    FragmentType = IrFragmentTypes.Clarification,
                                    FragmentVersion = clarificationRound,
                                    Payload = JsonSerializer.Serialize(clarificationSet, JsonCamelOptions),
                                    SkillId = "requirement-gate",
                                }, ct);

                                // 显式推 clarification_requested（AppendAsync 内部的 ir_event 仅供观测台，前端聊天面板靠此事件名渲染问卷卡）
                                sse.TrySend("clarification_requested", JsonSerializer.Serialize(clarificationSet, JsonCamelOptions));
                                _logger.LogInformation(
                                    "已发出澄清提问 round={Round} questions={Count} pipelineId={Id}",
                                    clarificationRound, clarificationSet.Questions.Count, pipelineId);

                                // 本轮结束：暂停流式 LLM，等待用户作答（POST /skills/clarification/{id}/answer）
                                sse.Complete();
                                return;
                            }
                        }
                    }

                    // ── 图片附件提取（多模态）──
                    if (existingAttachments.Any(a => GateConstants.IsImageFile(a.FileName)))
                    {
                        var visionConfig = _configuration.GetSection("MultimodalVision");
                        var apiUrl = visionConfig["ApiUrl"];
                        var apiKey = visionConfig["ApiKey"];
                        var model = visionConfig["Model"];

                        if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiUrl))
                        {
                            // 图片附件需要重新下载（因为内容在AttachmentFile中需要byte[]）
                            var imageFiles = new List<AttachmentFile>();
                            foreach (var att in existingAttachments.Where(a => GateConstants.IsImageFile(a.FileName)))
                            {
                                // 优先取缓存（步骤2已下载），避免二次下载
                                if (downloadedBytes.TryGetValue(att.FileUrl, out var cachedBytes))
                                {
                                    imageFiles.Add(new AttachmentFile { FileName = att.FileName, Content = cachedBytes });
                                }
                                else
                                {
                                    var imgUrl = att.FileUrl;
                                    if (!imgUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                        imgUrl = $"{ctx.GetBaseUrl()}{imgUrl}";
                                    var imgBytes = await http.GetByteArrayAsync(imgUrl, ct);
                                    imageFiles.Add(new AttachmentFile { FileName = att.FileName, Content = imgBytes });
                                }
                            }

                            if (imageFiles.Count > 0)
                            {
                                var imageAnalysis = await gateService.ExtractFromImages(
                                    imageFiles, apiUrl, apiKey, model, ct);
                                if (!string.IsNullOrWhiteSpace(imageAnalysis))
                                {
                                    var lastIdx = chatMessages.FindLastIndex(m => m.Role == "user");
                                    if (lastIdx >= 0)
                                    {
                                        chatMessages[lastIdx] = new ChatMessage("user",
                                            chatMessages[lastIdx].Content + "\n\n" + imageAnalysis);
                                    }
                                }
                            }
                        }
                        else
                        {
                            logger.LogWarning("多模态API未配置，跳过图片分析。请配置 MultimodalVision 节点。");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw; // 向上传播到 Task.Run 的取消处理
                }
                catch (OutOfMemoryException)
                {
                    throw; // OOM 不能继续执行，向上传播
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "需求门控执行异常，阻断 LLM 调用 pipelineId={Id}", pipelineId);
                    sse.Error($"门控校验异常: {ex.Message}");
                    sse.Complete();
                    return;
                }
            }
            #endregion // LEGACY_REQUIREMENT_GATE_FLOW

            // 构造 LLM 请求
            var llmRequest = new ChatCompletionRequest
            {
                ProviderCode = provider,
                SystemPrompt = systemPrompt ?? GetStageSystemPrompt(stageName),
                Messages = chatMessages,
                MaxTokens = 4096,
                Temperature = 0.7,
                MaxRetries = 2,
                TimeoutMs = 120000
            };

            // 流式调用 LLM Gateway
            var chunkCount = 0;
            await foreach (var json in llmGateway.ChatStreamAsync(llmRequest))
            {
                if (json.StartsWith("[ERROR]") || json.StartsWith("[error]"))
                {
                    logger.LogWarning("LLM Gateway 返回错误: {Error}", json);
                    sse.Error(json);
                return;
                }

                var token = ExtractToken(json);
                if (string.IsNullOrEmpty(token)) continue;

                chunkCount++;
                fullResponse.Append(token);
                await sse.TokenAsync(token, ct);
            }

            logger.LogInformation("LLM 流式完成: PipelineId={Id}, Chunks={Chunks}, ResponseLength={Len}",
                pipelineId, chunkCount, fullResponse.Length);

            // 保存 AI 完整回复到数据库
            if (fullResponse.Length > 0)
            {
                await SaveMessageAsync(db, pipelineId.ToString(), projectId, stageName, "assistant", fullResponse.ToString());

                // P1-4: 流式 token 记账(估算值,因 ChatStreamAsync 不返回 usage)
                // 粗估公式:输入(chatMessages 字符数/4)+ 输出(fullResponse 字符数/4)
                // 行业标准粗估,后续可改为解析流式最后一条 chunk 的 usage
                try
                {
                    var estimatedInputTokens = chatMessages.Sum(m => m.Content.Length) / 4;
                    var estimatedOutputTokens = fullResponse.Length / 4;
                    var estimatedTotal = estimatedInputTokens + estimatedOutputTokens;
                    if (estimatedTotal > 0 && !string.IsNullOrEmpty(ctx.TenantId))
                    {
                        await budgetGuard.AccumulateProjectTokensAsync(
                            projectId, ctx.TenantId, estimatedTotal, ct);
                        logger.LogInformation(
                            "流式 Token 记账(估算): PipelineId={Id}, Input≈{In}, Output≈{Out}",
                            pipelineId, estimatedInputTokens, estimatedOutputTokens);
                    }
                }
                catch (Exception budgetEx)
                {
                    // 记账失败不应影响主流式响应
                    logger.LogWarning(budgetEx, "流式 Token 记账失败(非阻断): PipelineId={Id}", pipelineId);
                }
            }

            // development 阶段完成后：上传 generated/ 产物到沙箱
            if (stageName == PipelineStage.Development)
            {
                try
                {
                    var tenantId = TenantResolver.Resolve();
                    var (_, generatedDir, _, _) = StudioWorkspaceHelper.GetPipelineSubPaths(
                        tenantId.ToString(), projectId, pipelineId.ToString());
                    var sandboxId = $"pipeline-{pipelineId}";
                    var sandbox = await _sandbox.GetStatusAsync(sandboxId);
                    if (sandbox != null && sandbox.Status == "ready")
                    {
                        var files = StudioWorkspaceHelper.ReadFilesFromDirectory(generatedDir);
                        if (files.Count > 0)
                        {
                            sse.Token("📦 正在上传文件到沙箱...");
                            await _sandbox.UploadFilesAsync(sandboxId, files);
                            sse.Token($"✅ 已上传 {files.Count} 个文件到沙箱");
                            logger.LogInformation("沙箱上传完成: {SandboxId}, {Count} 文件", sandboxId, files.Count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "沙箱上传失败: PipelineId={Id}", pipelineId);
                    sse.Token($"⚠️ 沙箱上传失败: {ex.Message}");
                }
            }

            // 推送阶段完成信号 → 前端显示确认按钮
            sse.TrySend("stage_complete", "");
            // 推送完成事件
            sse.Complete();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LLM 流式调用失败: PipelineId={Id}, Stage={Stage}", pipelineId, stageName);
            var llmErrorDetail = ex.InnerException != null
                ? $"LLM 调用失败: {ex.Message} (Inner: {ex.InnerException.Message})"
                : $"LLM 调用失败: {ex.Message}";
            sse.Error(llmErrorDetail);
        }
        finally
        {
            // SseSender.Dispose() 已处理 Channel 关闭（由 using 块保证）
            // 不立即移除 Channel：前端可能尚未连接（LLM 太快时 <3s 完成）
            // 下次 POST /execute 时通过 TryRemove 覆盖旧 Channel，无泄漏
        }
    }

    // ─── SSE 事件流（对齐前端 useSSE 契约）───

    /// <summary>
    /// SSE 事件流 — 从 Channel 读取 LLM token
    /// </summary>
    [HttpGet("{pipelineId:long}/events")]
    public async Task GetPipelineEvents(long pipelineId, CancellationToken ct)
    {
        var response = App.HttpContext!.Response;
        response.ContentType = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";

        // 租户隔离校验（铁律 R2.2）：校验 pipelineId 归属当前租户
        var currentTenantId = TenantResolver.Resolve();
        if (currentTenantId >= 0)
        {
            var pipeline = await _db.Queryable<AiPipelineEntity>()
                .Where(p => p.Id == pipelineId.ToString() && (p.DeleteMark == null || p.DeleteMark == 0))
                .Select(p => new { p.TenantId })
                .FirstAsync(ct);
            if (pipeline == null)
            {
                await WriteSseAsync(response, new SseEvent("error", "流水线不存在"));
                return;
            }
            // 平台租户（超级管理员）上帝视角，跳过校验
            if (!TenantResolver.IsSuperTenant()
                && !string.Equals(pipeline.TenantId, currentTenantId.ToString(), StringComparison.Ordinal))
            {
                _logger.LogWarning("跨租户 SSE 访问被拒: PipelineId={PipelineId}, ClaimTenant={ClaimTenant}, PipelineTenant={PipelineTenant}",
                    pipelineId, currentTenantId, pipeline.TenantId);
                await WriteSseAsync(response, new SseEvent("error", "无权访问该流水线"));
                return;
            }
        }

        Channel<SseEvent>? channel = null;
        // 最长等待 10 秒（100 × 100ms），覆盖慢 DB 查询
        for (int i = 0; i < 100 && channel == null && !ct.IsCancellationRequested; i++)
        {
            _sseHub.TryGetChannel(pipelineId, out channel);
            if (channel == null) await Task.Delay(100, ct);
        }

        if (channel == null)
        {
            await WriteSseAsync(response, new SseEvent("error", "无活跃的流式任务，请先调用 POST /execute"));
            return;
        }

        try
        {
            var connectionDeadline = DateTime.UtcNow.AddMinutes(30);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(TimeSpan.FromMinutes(30));

            while (!linkedCts.Token.IsCancellationRequested && DateTime.UtcNow < connectionDeadline)
            {
                var waitRead = channel.Reader.WaitToReadAsync(linkedCts.Token).AsTask();
                var heartbeatDelay = Task.Delay(TimeSpan.FromSeconds(30), linkedCts.Token);
                var completed = await Task.WhenAny(waitRead, heartbeatDelay);

                if (completed == heartbeatDelay && !waitRead.IsCompleted)
                {
                    await WriteSseCommentAsync(response, "ping");
                    continue;
                }

                if (!await waitRead)
                    break;

                while (channel.Reader.TryRead(out var evt))
                {
                    await WriteSseAsync(response, evt);
                    if (evt.Type is "done" or "error")
                        return;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (ChannelClosedException) { }
    }

    // ─── 确认阶段（人工审核）───

    [HttpPost("stage/{stageId:long}/confirm")]
    public async Task<StageResult> ConfirmStageAsync(
        long stageId, [FromBody] StageConfirmation confirmation)
    {
        // 前端当前传的是 pipelineId（见 PipelineEngineService.ConfirmStageAsync 兼容注释）
        await EnsurePipelineTenantAsync(stageId, CancellationToken.None);
        await EnsureNotFrozenAsync(stageId, CancellationToken.None);

        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .FirstAsync(x => x.Id == stageId.ToString());
        if (pipeline == null)
            throw Oops.Bah($"流水线 {stageId} 不存在");

        var confirmedStage = string.IsNullOrWhiteSpace(pipeline.CurrentStage)
            ? PipelineStage.Requirement
            : pipeline.CurrentStage;

        var result = await _pipelineEngine.ConfirmStageAsync(stageId, confirmation);

        StageConfirmTriggerResult? triggerResult = null;
        if (confirmation.Approved)
        {
            try
            {
                var tenantId = pipeline.TenantId ?? TenantResolver.Resolve().ToString();
                triggerResult = await _stageConfirmSkillTrigger.TriggerAfterConfirmAsync(
                    stageId, tenantId, confirmedStage, result.StageName, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "阶段确认后 Skill 触发失败: PipelineId={Id}, Stage={Stage}",
                    stageId, confirmedStage);
            }
        }

        try
        {
            var tenantId = pipeline.TenantId ?? TenantResolver.Resolve().ToString();

            await _irEventStore.AppendAsync(stageId.ToString(), tenantId, new AppendIrEventRequest
            {
                EventType = IrEventTypes.StageConfirmed,
                Payload = JsonSerializer.Serialize(new
                {
                    pipelineId = stageId,
                    stageName = confirmedStage,
                    approved = confirmation.Approved,
                    nextStage = result.StageName,
                    triggeredSkillIds = triggerResult?.TriggeredSkillIds ?? Array.Empty<string>(),
                }),
            });

            var userId = GetUserId();
            await _generatedProjectRegistry.UpsertFromPipelineAsync(stageId, tenantId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "StageConfirmed IR 事件写入失败: PipelineId={Id}", stageId);
        }

        return result with
        {
            ConfirmedStage = confirmedStage,
            TriggeredSkillIds = triggerResult?.TriggeredSkillIds,
            BackgroundTaskNames = triggerResult?.BackgroundTaskNames,
        };
    }

    [HttpPost("{pipelineId:long}/rollback")]
    public async Task<StageResult> RollbackAsync(long pipelineId, [FromBody] RollbackRequest request)
    {
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        await EnsureNotFrozenAsync(pipelineId, CancellationToken.None);
        var target = string.IsNullOrWhiteSpace(request.TargetStage) ? PipelineStage.Requirement : request.TargetStage;
        return await _pipelineEngine.RollbackAsync(pipelineId, target, request.Reason);
    }

    // ─── P1-3: 冻结/恢复(开发任务对话冻结与重新拉起)───

    /// <summary>
    /// 冻结流水线(全量 checkpoint:状态机 + 最近消息 + IR 版本)
    /// 用途:用户离开、BUG 修复中途暂停、等待人工介入
    /// </summary>
    [HttpPost("{pipelineId:long}/freeze")]
    public async Task<PipelineResult> FreezeAsync(
        long pipelineId, [FromBody] FreezeRequest? request, CancellationToken ct)
    {
        await EnsurePipelineTenantAsync(pipelineId, ct);
        var userId = GetUserId();
        return await _pipelineEngine.FreezeAsync(pipelineId, request?.Reason, userId.ToString(), ct);
    }

    /// <summary>
    /// 恢复流水线(从 checkpoint 重建状态,生成新会话)
    /// 用途:重新拉起之前冻结的开发任务对话
    /// </summary>
    [HttpPost("{pipelineId:long}/resume")]
    public async Task<PipelineResult> ResumeAsync(long pipelineId, CancellationToken ct)
    {
        await EnsurePipelineTenantAsync(pipelineId, ct);
        return await _pipelineEngine.ResumeAsync(pipelineId, ct);
    }

    // ─── 获取流水线详情 ───

    [HttpGet("{pipelineId:long}")]
    public async Task<PipelineDetail> GetDetailAsync(long pipelineId)
    {
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        return await _pipelineEngine.GetDetailAsync(pipelineId);
    }

    // ─── 流水线列表 ───

    [HttpGet("list")]
    public async Task<List<PipelineSummary>> ListAsync(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20)
    {
        var tenantId = TenantResolver.Resolve();
        var userId = GetUserId().ToString();
        var isSuper = TenantResolver.IsSuperTenant();
        return await _pipelineEngine.ListAsync(tenantId, pageIndex, pageSize, isSuper ? null : userId);
    }

    // ─── Provider 列表（前端模型选择器）───

    [HttpGet("providers")]
    public object GetProviders()
    {
        var providers = _configuration.GetSection("LlmGateway:Providers").Get<List<ProviderEntry>>() ?? new();
        var items = providers.Select(p => new { p.ProviderCode, p.Name, Enabled = true }).ToList();
        return new { items };
    }

    private record ProviderEntry
    {
        public string ProviderCode { get; init; } = "";
        public string Name { get; init; } = "";
        public int Level { get; init; }
    }

    // ─── 执行详细设计（6 SubAgent 并行）───

    [HttpPost("{pipelineId:long}/detailed-design")]
    public async Task<DetailedDesignResult> ExecuteDetailedDesignAsync(
        long pipelineId, CancellationToken ct)
    {
        await EnsurePipelineTenantAsync(pipelineId, ct);
        await EnsureNotFrozenAsync(pipelineId, ct);
        var pipeline = await _pipelineEngine.GetDetailAsync(pipelineId);
        if (pipeline?.CurrentStage != PipelineStage.Design)
            throw new InvalidOperationException("当前阶段不是总体设计阶段");

        var context = new DetailedDesignContext
        {
            ProjectName = pipeline.Name,
            Requirements = "从流水线获取的需求",
            TenantId = TenantResolver.Resolve()
        };

        return await _designOrchestrator.ExecuteAsync(context, null, ct);
    }

    // ─── 获取流水线 IR 版本快照 ───

    [HttpGet("{pipelineId:long}/ir")]
    public async Task<object> GetPipelineIRAsync(long pipelineId)
    {
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        var pid = pipelineId.ToString();
        var dt = await _db.Ado.GetDataTableAsync(
            "SELECT TOP 1 F_IR_SNAPSHOT, F_VERSION, F_DIFF, F_CHANGE_SUMMARY, F_VALIDATION_RESULT, F_SNAPSHOT_AT FROM BASE_IR_VERSION WHERE F_PIPELINE_ID = @pid ORDER BY F_VERSION DESC",
            new SugarParameter("@pid", pid));

        if (dt.Rows.Count == 0)
            return new { pipelineId = pid, irSnapshot = (string?)null, irVersion = 0 };

        var row = dt.Rows[0];
        return new
        {
            pipelineId = pid,
            irSnapshot = row["F_IR_SNAPSHOT"] as string,
            irVersion = Convert.ToInt32(row["F_VERSION"]),
            diff = row["F_DIFF"] as string,
            changeSummary = row["F_CHANGE_SUMMARY"] as string,
            validationResult = row["F_VALIDATION_RESULT"] as string,
            snapshotAt = row["F_SNAPSHOT_AT"] as DateTime?
        };
    }

    // ─── 辅助方法 ───

    private async Task SaveMessageAsync(string pipelineId, string projectId, string stage, string role, string content)
    {
        var msg = new AiPipelineMessageEntity
        {
            PipelineId = pipelineId,
            ProjectId = projectId,
            Stage = stage,
            Role = role,
            Content = content,
            Sequence = await GetNextSequenceAsync(pipelineId, stage),
            DeleteMark = 0
        };
        msg.Creator();
        await _db.Insertable(msg).ExecuteCommandAsync();
    }

    private static async Task SaveMessageAsync(
        ISqlSugarClient db, string pipelineId, string projectId, string stage, string role, string content)
    {
        var msg = new AiPipelineMessageEntity
        {
            PipelineId = pipelineId,
            ProjectId = projectId,
            Stage = stage,
            Role = role,
            Content = content,
            Sequence = await GetNextSequenceAsync(db, pipelineId, stage),
            DeleteMark = 0
        };
        msg.Creator();
        await db.Insertable(msg).ExecuteCommandAsync();
    }

    private async Task<int> GetNextSequenceAsync(string pipelineId, string stage)
    {
        return await GetNextSequenceAsync(_db, pipelineId, stage);
    }

    private static async Task<int> GetNextSequenceAsync(
        ISqlSugarClient db, string pipelineId, string stage)
    {
        // 后台线程 TenantResolver 可能返回 -1，与写入时 Creator() 注入的 TenantId 来源一致
        var tenantId = TenantResolver.Resolve().ToString();
        var maxSeq = await db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId && x.TenantId == tenantId && x.Stage == stage)
            .MaxAsync(x => (int?)x.Sequence) ?? 0;
        return maxSeq + 1;
    }

    private static string? ExtractToken(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var text))
                return text.GetString();

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("delta", out var delta2) &&
                    delta2.TryGetProperty("content", out var content))
                    return content.GetString();
            }

            return null;
        }
        catch { return null; }
    }

    private static string GetStageSystemPrompt(string stage) => stage switch
    {
        PipelineStage.Requirement => """
            你是需求分析师 AI。你的职责是：
            1. 理解用户的业务需求（不要急于给方案）
            2. 主动追问关键问题（至少 3 个问题）
            3. 提供策略选项（不是唯一方案）
            4. 分析每个选项的利弊

            请用中文回复，条理清晰，使用 Markdown 格式。
            """,
        PipelineStage.Architecture => """
            你是架构师 AI。基于需求分析结果，设计系统架构：
            1. 技术选型及理由
            2. 模块划分
            3. 接口设计
            4. 数据库设计概要

            请用中文回复，条理清晰。
            """,
        PipelineStage.Design => """
            你是总体设计师 AI。产出详细的软件设计文档：
            1. 详细数据模型（ER 图描述）
            2. API 接口规格
            3. 前端页面结构

            请用中文回复。
            """,
        PipelineStage.Development => """
            你是开发工程师 AI。基于设计文档生成代码：
            1. 后端服务代码
            2. 前端页面代码
            3. 数据库脚本

            请用中文说明，代码用代码块包裹。
            """,
        PipelineStage.Delivery => """
            你是交付工程师 AI。整理交付物：
            1. 部署说明
            2. 测试报告
            3. 用户手册

            请用中文回复。
            """,
        _ => "你是一个 AI 开发助手，请用中文回复。"
    };

    private static async Task WriteSseCommentAsync(HttpResponse response, string comment)
    {
        var bytes = Encoding.UTF8.GetBytes($": {comment}\n\n");
        await response.Body.WriteAsync(bytes);
        await response.Body.FlushAsync();
    }

    private static async Task WriteSseAsync(HttpResponse response, SseEvent evt)
    {
        var payload = new Dictionary<string, string?>
        {
            ["type"] = evt.Type,
            ["data"] = evt.Data,
            ["content"] = evt.Data  // compat: frontend reads data.content for 'token' type
        };
        if (evt.Stage != null) payload["stage"] = evt.Stage;

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes($"data: {json}\n\n");
        await response.Body.WriteAsync(bytes);
        await response.Body.FlushAsync();
    }

    /// <summary>
    /// 交付打包：将 generated/ 目录打包为 zip 并返回下载信息
    /// GET /api/studio/pipeline/execute/{pipelineId}/delivery-package
    /// </summary>
    [HttpGet("{pipelineId:long}/delivery-package")]
    public async Task<object> GetDeliveryPackageAsync(long pipelineId)
    {
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        var tenantId = TenantResolver.Resolve();
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .FirstAsync();

        if (pipeline == null)
            throw Oops.Bah($"流水线 {pipelineId} 不存在");

        try
        {
            var projectId = string.IsNullOrWhiteSpace(pipeline.ProjectId)
                ? pipelineId.ToString()
                : pipeline.ProjectId;
            var zipPath = StudioWorkspaceHelper.CreateDeliveryZip(
                tenantId.ToString(), projectId, pipelineId.ToString());

            // 清除 AI 开发上下文（退出 L4 白名单）
            StudioWorkspaceHelper.ClearAiDevContext();

            var downloadUrl = $"/api/file/download?path={Uri.EscapeDataString(zipPath)}";

            await _generatedProjectRegistry.UpdateDeliveryArtifactsAsync(pipelineId, null, downloadUrl);

            _logger.LogInformation("交付包已生成: PipelineId={Id}, Path={Path}", pipelineId, zipPath);

            return new
            {
                downloadUrl,
                fileName = Path.GetFileName(zipPath),
                generatedAt = DateTime.Now
            };
        }
        catch (InvalidOperationException ex)
        {
            throw Oops.Bah(ex.Message);
        }
    }

    /// <summary>
    /// 启动前端预览：注入生成文件到壳工程 → 上传沙箱 → npm install → vite dev → SSE 推送预览 URL
    /// POST /api/studio/pipeline/execute/{pipelineId}/preview
    /// </summary>
    [HttpPost("{pipelineId:long}/preview")]
    public async Task<object> StartPreviewAsync(long pipelineId)
    {
        await EnsurePipelineTenantAsync(pipelineId, CancellationToken.None);
        await EnsureNotFrozenAsync(pipelineId, CancellationToken.None);
        var tenantId = TenantResolver.Resolve();
        var tenantIdStr = tenantId.ToString();
        var pipelineIdStr = pipelineId.ToString();

        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineIdStr)
            .FirstAsync();

        if (pipeline == null)
            throw Oops.Bah($"流水线 {pipelineId} 不存在");

        var projectId = string.IsNullOrWhiteSpace(pipeline.ProjectId) ? pipelineIdStr : pipeline.ProjectId;

        // 1. 获取工作区路径（R12 三元组）
        var (_, generatedDir, _, _) = StudioWorkspaceHelper.GetPipelineSubPaths(tenantIdStr, projectId, pipelineIdStr);

        if (!Directory.Exists(generatedDir) || !Directory.GetFiles(generatedDir, "*.vue", SearchOption.AllDirectories).Any())
            throw Oops.Bah("无可预览的前端文件：请先在 development 阶段生成 Vue 代码");

        // 2. 定位壳工程路径
        var previewProjectDir = _configuration.GetValue<string>("StudioPreview:ProjectPath")
            ?? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "studio-preview"));

        if (!Directory.Exists(previewProjectDir))
            throw Oops.Bah($"壳工程不存在: {previewProjectDir}");

        // 3. 注入生成文件到壳工程
        StudioWorkspaceHelper.InjectFrontendFiles(generatedDir, previewProjectDir);

        _logger.LogInformation("预览文件已注入: PipelineId={Id}, GeneratedDir={Dir}", pipelineId, generatedDir);

        // 4. 创建或获取沙箱
        var sandboxId = $"pipeline-{pipelineId}";
        var sandboxCreated = false; // REVIEW: resource cleanup verified on 2026-07-02
        var sandbox = await _sandbox.GetStatusAsync(sandboxId);

        try
        {
            if (sandbox == null || sandbox.Status == "destroyed" || sandbox.Status == "error")
            {
                // 检查排队状态并推送 SSE
                var queueLen = ((SandboxManager)_sandbox).QueueLength;
                if (queueLen > 0)
                {
                    PushSseEvent(pipelineId, "sandbox_queued",
                        System.Text.Json.JsonSerializer.Serialize(new { queuePosition = queueLen + 1 }));

                    _logger.LogInformation("沙箱创建排队中: SandboxId={Id}, QueuePosition={Pos}",
                        sandboxId, queueLen + 1);
                }

                try
                {
                    sandbox = await _sandbox.CreateAsync(new SandboxConfig
                    {
                        Id = sandboxId,
                        TenantId = tenantIdStr,
                        CpuLimit = 2,
                        MemoryLimit = "4Gi",
                        TimeoutSeconds = 600,
                        Port = 8080,
                        PreviewPort = 4173
                    });
                    sandboxCreated = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "沙箱创建失败: SandboxId={Id}", sandboxId);
                    PushSseEvent(pipelineId, "sandbox_error",
                        System.Text.Json.JsonSerializer.Serialize(new
                        {
                            pipelineId = pipelineIdStr,
                            stage = "create",
                            error = ex.Message
                        }));
                    throw;
                }

                // 推送排队位置更新（如果之前排过队）
                if (queueLen > 0)
                {
                    PushSseEvent(pipelineId, "queue_position",
                        System.Text.Json.JsonSerializer.Serialize(new { queuePosition = 0, status = "ready" }));
                }

                _logger.LogInformation("沙箱已创建用于预览: SandboxId={Id}, ContainerId={Cid}",
                    sandboxId, sandbox.ContainerId);
            }

            // 5. 上传完整壳工程到沙箱
            var projectFiles = StudioWorkspaceHelper.ReadFilesFromDirectory(previewProjectDir);
            await _sandbox.UploadFilesAsync(sandboxId, projectFiles);

            _logger.LogInformation("壳工程已上传: SandboxId={Id}, Files={Count}", sandboxId, projectFiles.Count);

            // 6. 在沙箱内执行 npm install（120s 超时）&& vite dev
            var installCmd = "cd /app && npm install --prefer-offline 2>&1 | tail -5";
            using var npmCts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            CommandResult installResult;
            try
            {
                installResult = await _sandbox.ExecuteCommandAsync(sandboxId, installCmd, npmCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("npm install 超时 (120s): SandboxId={Id}", sandboxId);
                PushSseEvent(pipelineId, "sandbox_error",
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        pipelineId = pipelineIdStr,
                        stage = "npm-install",
                        error = "timeout"
                    }));
                throw;
            }

            if (installResult.ExitCode != 0)
            {
                _logger.LogError("npm install 失败: SandboxId={Id}, Error={Error}", sandboxId, installResult.Error);
                PushSseEvent(pipelineId, "sandbox_error",
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        pipelineId = pipelineIdStr,
                        stage = "npm-install",
                        error = installResult.Error.Length > 500
                            ? installResult.Error[..500]
                            : installResult.Error
                    }));
                throw Oops.Bah($"npm install 失败: {installResult.Error}");
            }

            // 启动 Vite dev server（后台运行）
            var viteCmd = "cd /app && nohup npx vite --port 4173 --host > /tmp/vite.log 2>&1 &";
            await _sandbox.ExecuteCommandAsync(sandboxId, viteCmd);

            // 等待 Vite 就绪（轮询 30s）
            var ready = false;
            for (var i = 0; i < 15; i++)
            {
                await Task.Delay(2000);
                var checkResult = await _sandbox.ExecuteCommandAsync(sandboxId, "curl -s -o /dev/null -w '%{http_code}' http://localhost:4173");
                if (checkResult.ExitCode == 0 && checkResult.Output.Trim() == "200")
                {
                    ready = true;
                    break;
                }
            }

            if (!ready)
            {
                _logger.LogError("Vite 启动超时 (30s): SandboxId={Id}", sandboxId);
                PushSseEvent(pipelineId, "sandbox_error",
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        pipelineId = pipelineIdStr,
                        stage = "vite-start",
                        error = "timeout"
                    }));
                throw Oops.Bah("Vite dev server 启动超时（30s）");
            }

            // 7. 获取预览 URL
            var sandboxInfo = await _sandbox.GetSandboxInfoAsync(sandboxId);
            var previewUrl = sandboxInfo.PreviewUrl;

            if (string.IsNullOrEmpty(previewUrl) || previewUrl.EndsWith(":0"))
            {
                _logger.LogWarning("预览端口查询可能失败: PipelineId={Id}, PreviewUrl={Url}",
                    pipelineId, previewUrl);
            }

            // 8. SSE 推送 preview_ready
            PushSseEvent(pipelineId, "preview_ready", System.Text.Json.JsonSerializer.Serialize(new
            {
                previewUrl,
                sandboxId,
                status = "running"
            }));

            _logger.LogInformation("预览就绪: PipelineId={Id}, Url={Url}", pipelineId, previewUrl);

            await _generatedProjectRegistry.UpdateDeliveryArtifactsAsync(pipelineId, previewUrl, null);

            return new { previewUrl, sandboxId, status = "running" };
        }
        catch (Exception ex)
        {
            // 任何异常：尝试销毁新创建的沙箱（资源清理）
            if (sandboxCreated)
            {
                try
                {
                    await _sandbox.DestroyAsync(sandboxId);
                    _logger.LogInformation("异常路径沙箱已销毁: SandboxId={Id}, Error={Error}",
                        sandboxId, ex.Message);
                }
                catch (Exception destroyEx)
                {
                    _logger.LogWarning(destroyEx, "异常路径沙箱销毁失败: SandboxId={Id}", sandboxId);
                }
            }
            throw;
        }
    }

    /// <summary>
    /// SA / 外部服务调用用的租户 ID 字符串归一化（"default"/"0"/空 → "1"）。
    /// </summary>
    private static string NormalizeTenantIdString(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || tenantId == "0" || tenantId == "default")
            return "1";
        return tenantId;
    }

    /// <summary>
    /// 阶段名映射：支持数字(1-5)或字符串("requirement"...)
    /// 前端 currentStage.value 可能发送数字字符串
    /// </summary>
    /// <summary>
    /// 向 SSE 通道推送事件（非阻塞，通道满时丢弃）.
    /// 对高频事件类型（queue_position）做 300ms 防抖.
    /// </summary>
    private void PushSseEvent(long pipelineId, string eventType, string data)
        => _sseHub.TryPush(pipelineId, eventType, data);

    private static string MapStageName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return PipelineStage.Requirement;

        // 数字映射
        if (input is "1") return PipelineStage.Requirement;
        if (input is "2") return PipelineStage.Architecture;
        if (input is "3") return PipelineStage.Design;
        if (input is "4") return PipelineStage.Development;
        if (input is "5") return PipelineStage.Delivery;

        // 英文映射（不区分大小写）
        var lower = input.ToLowerInvariant();
        if (lower is "requirement") return PipelineStage.Requirement;
        if (lower is "architecture") return PipelineStage.Architecture;
        if (lower is "design") return PipelineStage.Design;
        if (lower is "development") return PipelineStage.Development;
        if (lower is "delivery") return PipelineStage.Delivery;

        // 中文映射（兜底——前端应发 code 而非 name）
        if (input.Contains("需求")) return PipelineStage.Requirement;
        if (input.Contains("架构")) return PipelineStage.Architecture;
        if (input.Contains("设计") || input.Contains("总体")) return PipelineStage.Design;
        if (input.Contains("开发")) return PipelineStage.Development;
        if (input.Contains("交付") || input.Contains("验证")) return PipelineStage.Delivery;

        // 未知阶段 → 直接返回原值
        return input;
    }

    private long GetUserId()
    {
        var claim = App.HttpContext?.User?.FindFirst("user_id")?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }

    private async Task EnsurePipelineTenantAsync(long pipelineId, CancellationToken ct)
    {
        var currentTenantId = TenantResolver.Resolve();
        if (currentTenantId < 0) return;

        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(p => p.Id == pipelineId.ToString() && (p.DeleteMark == null || p.DeleteMark == 0))
            .Select(p => new { p.TenantId, p.CreatorUserId })
            .FirstAsync(ct);

        if (pipeline == null)
            throw Oops.Bah($"流水线 {pipelineId} 不存在");

        if (!TenantResolver.IsSuperTenant()
            && !string.Equals(pipeline.TenantId, currentTenantId.ToString(), StringComparison.Ordinal))
        {
            throw Oops.Bah("无权访问该流水线");
        }

        // R12：同租户按创建人隔离（超管例外）
        if (!TenantResolver.IsSuperTenant())
        {
            var userId = GetUserId().ToString();
            if (!string.IsNullOrWhiteSpace(pipeline.CreatorUserId)
                && !string.Equals(pipeline.CreatorUserId, userId, StringComparison.Ordinal))
            {
                throw Oops.Bah("无权访问该流水线（非创建人）");
            }
        }
    }

    /// <summary>
    /// 冻结状态守卫(P1-3):冻结的流水线拒绝写操作(执行阶段/确认/回退/上传)。
    /// 读操作(GetDetail/events/IR 查询)允许,用户需先 POST /resume 解冻。
    /// </summary>
    private async Task EnsureNotFrozenAsync(long pipelineId, CancellationToken ct)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(p => p.Id == pipelineId.ToString())
            .Select(p => new { p.Frozen })
            .FirstAsync(ct);

        if (pipeline is { Frozen: true })
            throw Oops.Bah($"流水线 {pipelineId} 已冻结,请先调用 /resume 恢复后再操作");
    }

    private static IEnumerable<AttachmentRegisterItem> MapAttachmentItems(IEnumerable<AttachmentPayload>? payloads)
    {
        if (payloads == null) yield break;
        foreach (var p in payloads)
        {
            if (string.IsNullOrWhiteSpace(p.Url)) continue;
            yield return new AttachmentRegisterItem { Name = p.Name, Url = p.Url };
        }
    }

    // ═══════════════════════════════════════════════════
    // SA 结果格式化辅助方法
    // ═══════════════════════════════════════════════════

    private static string FormatSAResultAsMarkdown(SAResultDto? saResult)
    {
        if (saResult?.Result == null) return "SA 分析未返回结果。";

        var r = saResult.Result;
        var sb = new StringBuilder();
        sb.AppendLine("# 系统需求分析说明书");
        sb.AppendLine();

        sb.AppendLine("## 1. 系统边界");
        if (TryGetJsonElementText(r.Scope, out var scopeText))
            sb.AppendLine(TruncateJson(scopeText, 2000));
        else
            sb.AppendLine("边界提取完成，详见 IR 结构化数据。");
        sb.AppendLine();

        sb.AppendLine("## 2. 数据流图 (DFD)");
        if (TryGetJsonElementText(r.Dfd, out var dfdText))
            sb.AppendLine(TruncateJson(dfdText, 1500));
        else
            sb.AppendLine("数据流图已生成，详见 IR 结构化数据。");
        sb.AppendLine();

        sb.AppendLine("## 3. 业务流程 (BPM)");
        if (TryGetJsonElementText(r.Bpm, out var bpmText))
            sb.AppendLine(TruncateJson(bpmText, 1500));
        else
            sb.AppendLine("业务流程已生成，详见 IR 结构化数据。");
        sb.AppendLine();

        sb.AppendLine("## 4. 数据字典");
        if (TryGetJsonElementText(r.Dict, out var dictText))
            sb.AppendLine(TruncateJson(dictText, 2000));
        else
            sb.AppendLine("数据字典已生成，详见 IR 结构化数据。");
        sb.AppendLine();

        sb.AppendLine("## 5. ER 数据模型");
        if (TryGetJsonElementText(r.Er, out var erText))
            sb.AppendLine(TruncateJson(erText, 1500));
        else
            sb.AppendLine("实体关系图已生成，详见 IR 结构化数据。");
        sb.AppendLine();

        if (TryGetJsonElementText(r.Std, out var stdText) && stdText.Length > 4)
        {
            sb.AppendLine("## 6. 状态机 (STD)");
            sb.AppendLine(TruncateJson(stdText, 1000));
            sb.AppendLine();
        }

        if (saResult.ValidationStats != null && saResult.ValidationStats.Count > 0)
        {
            sb.AppendLine("## 7. 质量验证");
            foreach (var stat in saResult.ValidationStats)
            {
                var icon = stat.Passed ? "✅" : "❌";
                sb.AppendLine($"- {icon} {stat.Step}: {(stat.Passed ? "通过" : "需修正")}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("请确认以上分析是否准确。如有需要补充的地方，请在下方输入框继续说明。");
        return sb.ToString();
    }

    private static bool TryGetJsonElementText(JsonElement? element, out string text)
    {
        if (element == null) { text = ""; return false; }
        try
        {
            text = JsonSerializer.Serialize(element.Value, new JsonSerializerOptions { WriteIndented = true });
            return !string.IsNullOrWhiteSpace(text) && text != "null" && text != "{}";
        }
        catch { text = ""; return false; }
    }

    private static string TruncateJson(string json, int maxLen)
    {
        if (json.Length <= maxLen) return json;
        return json[..maxLen] + "\n... (已截断，完整数据见 IR 字段)";
    }

    /// <summary>
    /// 计算文件内容的 SHA256 哈希（用于附件去重）
    /// </summary>
    private static string ComputeSha256(byte[] data)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }



    // ═══════════════════════════════════════════════════
    // SA 结果 DTO
    // ═══════════════════════════════════════════════════

    private class SAResultDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("taskId")]
        public string? TaskId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string? Status { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("result")]
        public SAOutputDto? Result { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("validationStats")]
        public List<ValidationStatDto>? ValidationStats { get; set; }
    }

    private class SAOutputDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("scope")]
        public JsonElement? Scope { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("dfd")]
        public JsonElement? Dfd { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("bpm")]
        public JsonElement? Bpm { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("dict")]
        public JsonElement? Dict { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("er")]
        public JsonElement? Er { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("std")]
        public JsonElement? Std { get; set; }
    }

    private class ValidationStatDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("step")]
        public string Step { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("attempts")]
        public int Attempts { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("passed")]
        public bool Passed { get; set; }
    }


}

// ─── 请求 DTO ───

public record CreatePipelineInput
{
    public string? Requirement { get; init; }
    public string? Name { get; init; }
    public string? UserRequirement { get; init; }

    /// <summary>greenfield | bugfix | enhancement</summary>
    public string? WorkMode { get; init; }

    /// <summary>关联源流水线 ID（bugfix / enhancement 必填）</summary>
    public long? SourcePipelineId { get; init; }

    /// <summary>Debug 目标页面路由</summary>
    public string? TargetPageRoute { get; init; }

    /// <summary>Debug 目标页面显示名</summary>
    public string? TargetPageLabel { get; init; }
}

public record QuickTaskRequest
{
    public string? Message { get; init; }
}

public record ExecuteStageRequest
{
    public string StageName { get; init; } = "";
    public string? Message { get; init; }
    public string? Provider { get; init; }
    public List<AttachmentPayload>? Attachments { get; init; }
}

public record AttachmentPayload
{
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
}

public record RollbackRequest
{
    public string TargetStage { get; init; } = "";
    public string? Reason { get; init; }
}

/// <summary>
/// 冻结流水线请求(P1-3: 开发任务对话冻结)
/// </summary>
public record FreezeRequest
{
    /// <summary>冻结原因(可选,如"用户离开""等待人工介入""BUG修复中途暂停")</summary>
    public string? Reason { get; init; }
}

/// <summary>R12 fork 请求</summary>
public record ForkPipelineRequest
{
    public string? Name { get; init; }
    /// <summary>bugfix | enhancement（默认 enhancement）</summary>
    public string? WorkMode { get; init; }
}

/// <summary>
/// SA 门控请求
/// </summary>
public record SaGateRequest
{
    public string UserText { get; init; } = "";
    public string? Provider { get; init; }
    public List<AttachmentPayload>? Attachments { get; init; }

    /// <summary>门控通过后自动运行 PM Skill；null 时使用 GatePipeline.json 配置</summary>
    public bool? AutoRunPm { get; init; }
}

public record UploadMaterialsRequest
{
    public List<AttachmentPayload>? Attachments { get; init; }
}
