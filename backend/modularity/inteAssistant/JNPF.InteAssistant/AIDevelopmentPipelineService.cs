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
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Infrastructure.Security;
using JNPF.InteAssistant.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace JNPF.InteAssistant;

/// <summary>
/// AI 开发流水线 API — 完整的五阶段 AI 辅助开发平台
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "PipelineExecute", Order = 195)]
[Route("api/studio/pipeline/execute")]
public class AIDevelopmentPipelineService : IDynamicApiController, ITransient
{
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

    /// <summary>
    /// SSE 事件通道池：按 pipelineId 隔离。/execute 写入 token，/events 读取推送给前端。
    /// </summary>
    private static readonly ConcurrentDictionary<long, Channel<SseEvent>> _sseChannels = new();

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
        IGatePipeline gatePipeline)
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
        var name = string.IsNullOrWhiteSpace(input.Name)
            ? (requirement.Length > 50 ? requirement[..50] : requirement)
            : input.Name;
        if (string.IsNullOrWhiteSpace(name)) name = "未命名流水线";

        var request = new PipelineCreateRequest
        {
            Name = name,
            UserRequirement = requirement
        };

        var result = await _pipelineEngine.CreateAsync(request, tenantId, userId);

        // 落库 BASE_AI_PIPELINE（用 engine 的 pipelineId 作为主键，保持前后端 ID 一致）
        var entity = new AiPipelineEntity
        {
            Id = result.PipelineId.ToString(),
            Name = name,
            CurrentStage = PipelineStage.Requirement,
            Status = "draft",
            StartedTime = DateTime.Now,
            TenantId = tenantId.ToString()
        };
        entity.Create();
        await _db.Insertable(entity).ExecuteCommandAsync();

        // 初始化 AI 工作区目录
        try
        {
            StudioWorkspaceHelper.EnsureDirectories(tenantId.ToString(), result.PipelineId.ToString());
            _logger.LogInformation("工作区目录已创建: PipelineId={Id}", result.PipelineId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建工作区目录失败: PipelineId={Id}", result.PipelineId);
        }

        // 保存用户需求消息
        await SaveMessageAsync(result.PipelineId.ToString(), PipelineStage.Requirement, "user", requirement);

        _logger.LogInformation("流水线创建: Id={Id}, Name={Name}", result.PipelineId, name);
        return result;
    }

    // ─── 启动流水线 ───

    /// <summary>
    /// 启动流水线
    /// </summary>
    [HttpPost("{pipelineId:long}/start")]
    public async Task<PipelineResult> StartAsync(long pipelineId)
    {
        return await _pipelineEngine.StartAsync(pipelineId);
    }

    // ─── SA 门控（异步事件驱动 — 缺陷4修复）───

    /// <summary>
    /// SA 门控入口 — 异步事件驱动
    /// 前端提交 → 202 Accepted → 后台执行门控 → SSE 推送结果
    /// </summary>
    [HttpPost("{pipelineId:long}/sa-gate")]
    public async Task<object> ExecuteGateAsync(
        long pipelineId, [FromBody] SaGateRequest request)
    {
        var userText = request?.UserText ?? "";
        var tenantId = TenantResolver.Resolve();
        var userId = GetUserId();

        // Step 1: 读取已持久化的附件
        var attachments = await _db.Queryable<InteAssistantAttachment>()
            .Where(a => a.PipelineId == pipelineId.ToString() && a.ProcessStatus == 2)
            .ToListAsync();

        var attachmentFiles = new List<AttachmentFile>();
        foreach (var att in attachments)
        {
            if (!string.IsNullOrWhiteSpace(att.FileUrl))
            {
                attachmentFiles.Add(new AttachmentFile
                {
                    FileName = att.FileName,
                    Content = Array.Empty<byte>() // 已提取文本, 不再传原始内容
                });
            }
        }

        // Step 2: 创建 SSE 通道
        if (_sseChannels.TryRemove(pipelineId, out var oldChannel))
            oldChannel.Writer.TryComplete();

        var channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions
        {
            SingleReader = true, SingleWriter = true
        });
        _sseChannels[pipelineId] = channel;

        // Step 3: 后台异步执行门控 (BackgroundTaskRunner + SSE)
        var ctx = RequestContext.Capture(_httpContextAccessor);
        var visionConfig = _configuration.GetSection("MultimodalVision");
        var visionApiUrl = visionConfig["ApiUrl"] ?? "";
        var visionApiKey = visionConfig["ApiKey"] ?? "";
        var visionModel = visionConfig["Model"] ?? "";

        _taskRunner.Run(
            $"SA_Gate_{pipelineId}",
            async (bgCtx, bgCt) =>
            {
                using var sse = _senderFactory.Create(pipelineId.ToString(), channel);
                try
                {
                    // 通知前端: 门控开始
                    sse.TrySend("gate_started", "");

                    // 执行门控管道
                    var gateResult = await _gatePipeline.ExecuteAsync(
                        userText, attachmentFiles, ctx,
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
                        await SaveMessageAsync(pipelineId.ToString(), "gate", "system",
                            JsonSerializer.Serialize(gateResult));

                        // 自动流转到 requirement 阶段
                        await _pipelineEngine.ExecuteStageAsync(pipelineId, PipelineStage.Requirement);
                        sse.TrySend("stage_transition", PipelineStage.Requirement);
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
                    }

                    sse.Complete();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SA 门控异常: PipelineId={Id}", pipelineId);
                    sse.TrySend("gate_error", JsonSerializer.Serialize(new
                    {
                        message = "需求评估过程中发生异常，请重试。",
                        errorCode = "GATE_INTERNAL_ERROR"
                    }));
                    sse.Complete();
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
    /// 执行当前阶段 — 保存用户消息，启动后台 LLM 流式任务，立即返回。
    /// 前端随后通过 GET /events 读取 SSE 流式 token。
    /// </summary>
    [HttpPost("{pipelineId:long}/execute")]
    public async Task<StageResult> ExecuteStageAsync(
        long pipelineId, [FromBody] ExecuteStageRequest request)
    {
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
            await SaveMessageAsync(pipelineId.ToString(), stageName, "user", message);
        }

        // 2. 流转状态机
        var stageResult = await _pipelineEngine.ExecuteStageAsync(pipelineId, stageName);

        // development 阶段：写入 AI 开发上下文标记，激活 guard-write L4 白名单
        if (stageName == PipelineStage.Development)
        {
            var tenantId = TenantResolver.Resolve();
            StudioWorkspaceHelper.EnsureDirectories(tenantId.ToString(), pipelineId.ToString());
            StudioWorkspaceHelper.WriteAiDevContext(tenantId.ToString(), pipelineId.ToString());
            _logger.LogInformation("AI 开发上下文已激活: PipelineId={Id}", pipelineId);
        }

        // 3. 创建 SSE 通道（替换旧通道，支持重复执行）
        if (_sseChannels.TryRemove(pipelineId, out var oldChannel))
            oldChannel.Writer.TryComplete();

        var channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        _sseChannels[pipelineId] = channel;

        // 4. 启动后台 LLM 流式任务（BackgroundTaskRunner 自动捕获上下文 + 管理 CTS 生命周期）
        _taskRunner.Run(
            $"pipeline-{pipelineId}",
            async (ctx, ct) =>
            {
                using var sse = _senderFactory.Create(pipelineId.ToString(), channel);
                try
                {
                    await StreamLlmResponseAsync(
                        pipelineId, stageName, provider, authHeader, sse,
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
    private async Task StreamLlmResponseAsync(long pipelineId, string stageName, string provider, string authHeader, SseSender sse, List<AttachmentPayload>? requestAttachments, RequestContext ctx, CancellationToken ct)
    {
        // 创建独立 DI scope，确保 _db/_llmGateway 在请求结束后仍可用
        using var scope = App.RootServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var llmGateway = scope.ServiceProvider.GetRequiredService<ILlmGatewayService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AIDevelopmentPipelineService>>();

        var fullResponse = new StringBuilder();
        try
        {
            logger.LogInformation("LLM 流式任务开始: PipelineId={Id}, Stage={Stage}, Provider={Provider}",
                pipelineId, stageName, provider);

            // 读取历史消息（构建上下文）
            var history = await db.Queryable<AiPipelineMessageEntity>()
                .Where(x => x.PipelineId == pipelineId.ToString() && (x.DeleteMark == 0 || x.DeleteMark == null))
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

            // ═══════════════════════════════════════════════════
            // SA 流水线拦截：需求分析阶段走 SA Service
            // ═══════════════════════════════════════════════════
            if (false)  // TODO: 改回 stageName == "requirement" 即可恢复 SA
            {
                _logger.LogInformation("[SA] 需求分析阶段，调用 SA Service pipelineId={PipelineId}", pipelineId);

                // 从历史消息中提取用户需求文本
                var requirementText = chatMessages
                    .Where(m => m.Role == "user")
                    .Select(m => m.Content)
                    .LastOrDefault() ?? "";

                if (string.IsNullOrWhiteSpace(requirementText))
                {
                    sse.Error("未找到用户需求文本");
                return;
                }

                // 推送 SSE：SA 流水线开始
                sse.Thinking("正在启动 SA 结构化分析流水线...");

                try
                {
                    var saServiceUrl = _configuration.GetValue<string>("SA:ServiceUrl");
                    if (string.IsNullOrWhiteSpace(saServiceUrl))
                    {
                        logger.LogError("[SA] SA:ServiceUrl 未配置，无法调用 SA Service");
                        sse.Error("SA Service URL 未配置，请联系管理员设置 SA:ServiceUrl");
                return;
                    }

                    var httpClient = _httpClientFactory.CreateClient();
                    httpClient.Timeout = TimeSpan.FromMinutes(5);

                    // 从 pipeline 记录获取 tenantId 和行业信息
                    var pipeline = await db.Queryable<AiPipelineEntity>()
                        .FirstAsync(p => p.Id == pipelineId.ToString());
                    var tenantId = NormalizeTenantIdString(pipeline?.TenantId);
                    // 行业从配置获取，不再硬编码（可通过 AiPipelineEntity 扩展字段或 SA:DefaultIndustry 配置）
                    var industry = _configuration.GetValue<string>("SA:DefaultIndustry") ?? "general";

                    var saRequest = new
                    {
                        tenantId = tenantId,
                        projectId = pipelineId,
                        requirementText = requirementText,
                        userId = "system",
                        industry = industry,
                        authHeader = authHeader,
                        providerCode = string.IsNullOrWhiteSpace(provider) ? "deepseek" : provider
                    };

                    // 使用 HttpRequestMessage 避免 DefaultRequestHeaders 并发竞态
                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{saServiceUrl}/api/sa/run");
                    httpRequest.Content = JsonContent.Create(saRequest);
                    httpRequest.Headers.Add("X-Tenant-Id", tenantId);

                    sse.Thinking("正在执行 3-Tier SA 流水线（Scope → DFD → BPM → Dict → ER → STD）...");

                    var saResponse = await httpClient.SendAsync(httpRequest);

                    if (!saResponse.IsSuccessStatusCode)
                    {
                        var errorBody = await saResponse.Content.ReadAsStringAsync();
                        logger.LogError("[SA] SA Service 返回错误: {StatusCode} {Body}", saResponse.StatusCode, errorBody);
                        sse.Error($"SA Service 调用失败: {saResponse.StatusCode}");
                return;
                    }

                    var saResult = await saResponse.Content.ReadFromJsonAsync<SAResultDto>();

                    // 推送 Scope 结果
                    if (saResult?.Result?.Scope != null)
                    {
                        sse.Thinking("✅ 边界提取完成");
                    }

                    // 推送 DFD 结果
                    if (saResult?.Result?.Dfd != null)
                    {
                        sse.Thinking("✅ DFD 数据流图生成完成");
                    }

                    // 推送 BPM 结果
                    if (saResult?.Result?.Bpm != null)
                    {
                        sse.Thinking("✅ 业务流程图生成完成");
                    }

                    // 推送数据字典结果
                    if (saResult?.Result?.Dict != null)
                    {
                        sse.Thinking("✅ 数据字典生成完成");
                    }

                    // 推送 ER 图结果
                    if (saResult?.Result?.Er != null)
                    {
                        sse.Thinking("✅ ER 图生成完成");
                    }

                    // 推送状态机结果
                    if (saResult?.Result?.Std != null)
                    {
                        sse.Thinking("✅ 状态机生成完成");
                    }

                    // 推送完整 SA 结果作为 Markdown token
                    var saContent = FormatSAResultAsMarkdown(saResult);
                    fullResponse.Append(saContent);
                    await sse.TokenAsync(saContent, ct);

                    // 推送 IR 数据（结构化 JSON）
                    if (saResult?.Result != null)
                    {
                        var irJson = JsonSerializer.Serialize(saResult.Result);
                        sse.TrySend("ir", irJson);
                    }

                    // 推送阶段完成信号
                    sse.TrySend("stage_complete", "");

                    // 保存 assistant 消息到数据库
                    await SaveMessageAsync(db, pipelineId.ToString(), stageName, "assistant", saContent);

                    _logger.LogInformation("[SA] 需求分析完成 pipelineId={PipelineId}", pipelineId);
                    // SA 成功 → 推送完成并返回，不走 LLM 降级
                    sse.Complete();
                return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[SA] SA Service 调用异常，降级为纯 LLM 分析 pipelineId={PipelineId}", pipelineId);
                    sse.TrySend("info", "SA 结构化流水线暂不可用，降级为 LLM 直接分析...");
                    // 不 return，fallthrough 到下方 LLM 直接调用
                }
            }
            // ═══════════════════════════════════════════════════
            // LLM 直接调用（含 SA 失败降级路径）
            // ═══════════════════════════════════════════════════

            string? systemPrompt = null;

            // ═══════════════════════════════════════════════════
            // 需求门控：附件持久化 + 缓存 + 硬规则校验 + 成熟度评估
            // （仅 requirement 阶段触发，其他阶段走默认 SystemPrompt）
            // ═══════════════════════════════════════════════════
            if (true) // 门控在所有阶段生效（不含SA拦截块，SA走单独路径）
            {
                try
                {
                    var gateService = scope.ServiceProvider.GetRequiredService<RequirementGateService>();
                    var attachmentProcessor = scope.ServiceProvider.GetRequiredService<AttachmentProcessor>();
                    var http = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
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
                                fileUrl = $"{ctx.Scheme}://{ctx.Host}{fileUrl}";
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
                    _logger.LogInformation("附件处理完成: 文件数={Count}, 新解析={New}, 提取文本长度={Len}",
                        existingAttachments.Count, processedCount, attachmentText.Length);

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
                        var modeLabel = maturity.Mode switch
                        {
                            "explore" => "探索模式 — 需要补充更多信息",
                            "confirm" => "确认模式 — 需要确认部分细节",
                            "refine" => "精化模式 — 开始深度分析",
                            _ => maturity.Mode
                        };
                        sse.TrySend("info", $"\n\n> 📊 需求成熟度：{maturity.Score}/100（{modeLabel}）\n\n");
                        systemPrompt = gateService.GetSystemPrompt(maturity.Mode, maturity);
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
                                        imgUrl = $"{ctx.Scheme}://{ctx.Host}{imgUrl}";
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
                await SaveMessageAsync(db, pipelineId.ToString(), stageName, "assistant", fullResponse.ToString());
            }

            // development 阶段完成后：上传 generated/ 产物到沙箱
            if (stageName == PipelineStage.Development)
            {
                try
                {
                    var tenantId = TenantResolver.Resolve();
                    var (_, generatedDir, _, _) = StudioWorkspaceHelper.GetPipelineSubPaths(
                        tenantId.ToString(), pipelineId.ToString());
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
            _sseChannels.TryGetValue(pipelineId, out channel);
            if (channel == null) await Task.Delay(100, ct);
        }

        if (channel == null)
        {
            await WriteSseAsync(response, new SseEvent("error", "无活跃的流式任务，请先调用 POST /execute"));
            return;
        }

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            {
                await WriteSseAsync(response, evt);
                if (evt.Type is "done" or "error") break;
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
        return await _pipelineEngine.ConfirmStageAsync(stageId, confirmation);
    }

    [HttpPost("{pipelineId:long}/rollback")]
    public async Task<StageResult> RollbackAsync(long pipelineId, [FromBody] RollbackRequest request)
    {
        var target = string.IsNullOrWhiteSpace(request.TargetStage) ? PipelineStage.Requirement : request.TargetStage;
        return await _pipelineEngine.RollbackAsync(pipelineId, target, request.Reason);
    }

    // ─── 获取流水线详情 ───

    [HttpGet("{pipelineId:long}")]
    public async Task<PipelineDetail> GetDetailAsync(long pipelineId)
    {
        return await _pipelineEngine.GetDetailAsync(pipelineId);
    }

    // ─── 流水线列表 ───

    [HttpGet("list")]
    public async Task<List<PipelineSummary>> ListAsync(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20)
    {
        return await _pipelineEngine.ListAsync(TenantResolver.Resolve(), pageIndex, pageSize);
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

    private async Task SaveMessageAsync(string pipelineId, string stage, string role, string content)
    {
        var msg = new AiPipelineMessageEntity
        {
            PipelineId = pipelineId,
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
        ISqlSugarClient db, string pipelineId, string stage, string role, string content)
    {
        var msg = new AiPipelineMessageEntity
        {
            PipelineId = pipelineId,
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
        var maxSeq = await db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId && x.Stage == stage)
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
        var tenantId = TenantResolver.Resolve();
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .FirstAsync();

        if (pipeline == null)
            throw Oops.Bah($"流水线 {pipelineId} 不存在");

        try
        {
            var zipPath = StudioWorkspaceHelper.CreateDeliveryZip(
                tenantId.ToString(), pipelineId.ToString());

            // 清除 AI 开发上下文（退出 L4 白名单）
            StudioWorkspaceHelper.ClearAiDevContext();

            _logger.LogInformation("交付包已生成: PipelineId={Id}, Path={Path}", pipelineId, zipPath);

            return new
            {
                downloadUrl = $"/api/file/download?path={Uri.EscapeDataString(zipPath)}",
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
        var tenantId = TenantResolver.Resolve();
        var tenantIdStr = tenantId.ToString();
        var pipelineIdStr = pipelineId.ToString();

        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineIdStr)
            .FirstAsync();

        if (pipeline == null)
            throw Oops.Bah($"流水线 {pipelineId} 不存在");

        // 1. 获取工作区路径
        var (_, generatedDir, _, _) = StudioWorkspaceHelper.GetPipelineSubPaths(tenantIdStr, pipelineIdStr);

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
        var sandbox = await _sandbox.GetStatusAsync(sandboxId);

        if (sandbox == null || sandbox.Status == "destroyed" || sandbox.Status == "error")
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

            _logger.LogInformation("沙箱已创建用于预览: SandboxId={Id}, ContainerId={Cid}",
                sandboxId, sandbox.ContainerId);
        }

        // 5. 上传完整壳工程到沙箱
        var projectFiles = StudioWorkspaceHelper.ReadFilesFromDirectory(previewProjectDir);
        await _sandbox.UploadFilesAsync(sandboxId, projectFiles);

        _logger.LogInformation("壳工程已上传: SandboxId={Id}, Files={Count}", sandboxId, projectFiles.Count);

        // 6. 在沙箱内执行 npm install && vite dev
        var installCmd = "cd /app && npm install --prefer-offline 2>&1 | tail -5";
        var installResult = await _sandbox.ExecuteCommandAsync(sandboxId, installCmd);

        if (installResult.ExitCode != 0)
        {
            _logger.LogError("npm install 失败: SandboxId={Id}, Error={Error}", sandboxId, installResult.Error);
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
            throw Oops.Bah("Vite dev server 启动超时（30s）");

        // 7. 获取预览 URL
        var sandboxInfo = await _sandbox.GetSandboxInfoAsync(sandboxId);
        var previewUrl = sandboxInfo.PreviewUrl;

        // 8. SSE 推送 preview_ready
        if (_sseChannels.TryGetValue(pipelineId, out var channel))
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                previewUrl,
                sandboxId,
                status = "running"
            });
            channel.Writer.TryWrite(new SseEvent("preview_ready", payload));
        }

        _logger.LogInformation("预览就绪: PipelineId={Id}, Url={Url}", pipelineId, previewUrl);

        return new { previewUrl, sandboxId, status = "running" };
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
/// SA 门控请求
/// </summary>
public record SaGateRequest
{
    public string UserText { get; init; } = "";
    public string? Provider { get; init; }
}
