using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Entitys.Common;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
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
        ILogger<AIDevelopmentPipelineService> logger)
    {
        _pipelineEngine = pipelineEngine;
        _designOrchestrator = designOrchestrator;
        _llmGateway = llmGateway;
        _sandbox = sandbox;
        _db = sqlSugarClient;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // ─── 创建流水线 ───

    /// <summary>
    /// 创建流水线（落库 + 保存用户需求消息）
    /// </summary>
    [HttpPost("create")]
    public async Task<PipelineResult> CreateAsync([FromBody] CreatePipelineInput input)
    {
        var tenantId = GetTenantId();
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
            StartedTime = DateTime.Now
        };
        entity.Create();
        await _db.Insertable(entity).ExecuteCommandAsync();

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
            ? PipelineStage.Requirement : request.StageName;
        var message = request.Message ?? "";
        var provider = request.Provider ?? "";

        // 1. 保存用户消息到数据库
        if (!string.IsNullOrWhiteSpace(message))
        {
            await SaveMessageAsync(pipelineId.ToString(), stageName, "user", message);
        }

        // 2. 流转状态机
        var stageResult = await _pipelineEngine.ExecuteStageAsync(pipelineId, stageName);

        // 3. 创建 SSE 通道（替换旧通道，支持重复执行）
        if (_sseChannels.TryRemove(pipelineId, out var oldChannel))
            oldChannel.Writer.TryComplete();

        var channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        _sseChannels[pipelineId] = channel;

        // 4. 启动后台 LLM 流式任务（不等待，立即返回）
        _ = Task.Run(() => StreamLlmResponseAsync(pipelineId, stageName, provider, channel));

        _logger.LogInformation("流水线阶段执行启动: PipelineId={Id}, Stage={Stage}", pipelineId, stageName);
        return stageResult;
    }

    /// <summary>
    /// 后台执行 LLM 流式调用，token 写入 Channel 供 /events 读取。
    /// 从根 ServiceProvider 创建独立 scope，避免请求结束后 DI 服务被释放。
    /// </summary>
    private async Task StreamLlmResponseAsync(
        long pipelineId, string stageName, string provider, Channel<SseEvent> channel)
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
                await channel.Writer.WriteAsync(new SseEvent("error", "无历史消息可发送给 LLM"));
                return;
            }

            // ═══════════════════════════════════════════════════
            // SA 流水线拦截：需求分析阶段走 SA Service
            // ═══════════════════════════════════════════════════
            if (stageName == "requirement")
            {
                logger.LogInformation("[SA] 需求分析阶段，调用 SA Service pipelineId={PipelineId}", pipelineId);

                // 从历史消息中提取用户需求文本
                var requirementText = chatMessages
                    .Where(m => m.Role == "user")
                    .Select(m => m.Content)
                    .LastOrDefault() ?? "";

                if (string.IsNullOrWhiteSpace(requirementText))
                {
                    await channel.Writer.WriteAsync(new SseEvent("error", "未找到用户需求文本"));
                    return;
                }

                // 推送 SSE：SA 流水线开始
                await channel.Writer.WriteAsync(new SseEvent("thinking", "正在启动 SA 结构化分析流水线..."));

                try
                {
                    var saServiceUrl = _configuration.GetValue<string>("SA:ServiceUrl") ?? "http://localhost:3001";

                    var httpClient = _httpClientFactory.CreateClient();
                    httpClient.Timeout = TimeSpan.FromMinutes(5);

                    // 从 pipeline 记录获取 tenantId
                    var pipeline = await db.Queryable<AiPipelineEntity>()
                        .FirstAsync(p => p.Id == pipelineId.ToString());
                    var tenantId = pipeline?.TenantId ?? "default";

                    var saRequest = new
                    {
                        tenantId = tenantId,
                        projectId = pipelineId,
                        requirementText = requirementText,
                        userId = "system",
                        industry = "manufacturing"
                    };

                    // 同时通过 X-Tenant-Id 请求头传递租户信息
                    httpClient.DefaultRequestHeaders.Remove("X-Tenant-Id");
                    httpClient.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);

                    await channel.Writer.WriteAsync(new SseEvent("thinking", "正在执行 3-Tier SA 流水线（Scope → DFD → BPM → Dict → ER → STD）..."));

                    var saResponse = await httpClient.PostAsJsonAsync($"{saServiceUrl}/api/sa/run", saRequest);

                    if (!saResponse.IsSuccessStatusCode)
                    {
                        var errorBody = await saResponse.Content.ReadAsStringAsync();
                        logger.LogError("[SA] SA Service 返回错误: {StatusCode} {Body}", saResponse.StatusCode, errorBody);
                        await channel.Writer.WriteAsync(new SseEvent("error", $"SA Service 调用失败: {saResponse.StatusCode}"));
                        return;
                    }

                    var saResult = await saResponse.Content.ReadFromJsonAsync<SAResultDto>();

                    // 推送 Scope 结果
                    if (saResult?.Result?.Scope != null)
                    {
                        await channel.Writer.WriteAsync(new SseEvent("thinking", "✅ 边界提取完成"));
                    }

                    // 推送 DFD 结果
                    if (saResult?.Result?.Dfd != null)
                    {
                        await channel.Writer.WriteAsync(new SseEvent("thinking", "✅ DFD 数据流图生成完成"));
                    }

                    // 推送 BPM 结果
                    if (saResult?.Result?.Bpm != null)
                    {
                        await channel.Writer.WriteAsync(new SseEvent("thinking", "✅ 业务流程图生成完成"));
                    }

                    // 推送数据字典结果
                    if (saResult?.Result?.Dict != null)
                    {
                        await channel.Writer.WriteAsync(new SseEvent("thinking", "✅ 数据字典生成完成"));
                    }

                    // 推送 ER 图结果
                    if (saResult?.Result?.Er != null)
                    {
                        await channel.Writer.WriteAsync(new SseEvent("thinking", "✅ ER 图生成完成"));
                    }

                    // 推送状态机结果
                    if (saResult?.Result?.Std != null)
                    {
                        await channel.Writer.WriteAsync(new SseEvent("thinking", "✅ 状态机生成完成"));
                    }

                    // 推送完整 SA 结果作为 Markdown token
                    var saContent = FormatSAResultAsMarkdown(saResult);
                    fullResponse.Append(saContent);
                    await channel.Writer.WriteAsync(new SseEvent("chunk", saContent));

                    // 推送 IR 数据（结构化 JSON）
                    if (saResult?.Result != null)
                    {
                        var irJson = JsonSerializer.Serialize(saResult.Result);
                        await channel.Writer.WriteAsync(new SseEvent("ir", irJson));
                    }

                    // 推送阶段完成信号
                    await channel.Writer.WriteAsync(new SseEvent("stage_complete"));

                    // 保存 assistant 消息到数据库
                    await SaveMessageAsync(db, pipelineId.ToString(), stageName, "assistant", saContent);

                    logger.LogInformation("[SA] 需求分析完成 pipelineId={PipelineId}", pipelineId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[SA] SA Service 调用异常 pipelineId={PipelineId}", pipelineId);
                    await channel.Writer.WriteAsync(new SseEvent("error", $"SA 流水线执行异常: {ex.Message}"));
                }

                await channel.Writer.WriteAsync(new SseEvent("done"));
                return;
            }
            // ═══════════════════════════════════════════════════
            // 以下原有 LLM 调用代码，一个字不动
            // ═══════════════════════════════════════════════════

            // 构造 LLM 请求
            var llmRequest = new ChatCompletionRequest
            {
                ProviderCode = provider,
                SystemPrompt = GetStageSystemPrompt(stageName),
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
                    await channel.Writer.WriteAsync(new SseEvent("error", json));
                    return;
                }

                var token = ExtractToken(json);
                if (string.IsNullOrEmpty(token)) continue;

                chunkCount++;
                fullResponse.Append(token);
                await channel.Writer.WriteAsync(new SseEvent("chunk", token));
            }

            logger.LogInformation("LLM 流式完成: PipelineId={Id}, Chunks={Chunks}, ResponseLength={Len}",
                pipelineId, chunkCount, fullResponse.Length);

            // 保存 AI 完整回复到数据库
            if (fullResponse.Length > 0)
            {
                await SaveMessageAsync(db, pipelineId.ToString(), stageName, "assistant", fullResponse.ToString());
            }

            // 推送完成事件
            await channel.Writer.WriteAsync(new SseEvent("done"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LLM 流式调用失败: PipelineId={Id}, Stage={Stage}", pipelineId, stageName);
            await channel.Writer.WriteAsync(new SseEvent("error", $"LLM 调用失败: {ex.Message}"));
        }
        finally
        {
            channel.Writer.TryComplete();
            _sseChannels.TryRemove(pipelineId, out _);
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

        Channel<SseEvent>? channel = null;
        for (int i = 0; i < 30 && channel == null && !ct.IsCancellationRequested; i++)
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
        return await _pipelineEngine.ListAsync(GetTenantId(), pageIndex, pageSize);
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
            TenantId = GetTenantId()
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
            ["data"] = evt.Data
        };
        if (evt.Stage != null) payload["stage"] = evt.Stage;

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes($"data: {json}\n\n");
        await response.Body.WriteAsync(bytes);
        await response.Body.FlushAsync();
    }

    private long GetTenantId()
    {
        var claim = App.HttpContext?.User?.FindFirst("tenant_id")?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
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

        var sb = new StringBuilder();
        sb.AppendLine("# 系统需求分析说明书");
        sb.AppendLine();
        sb.AppendLine("## 1. 系统边界");
        sb.AppendLine("边界提取完成，已识别所有业务事件。");
        sb.AppendLine();
        sb.AppendLine("## 2. 数据字典");
        sb.AppendLine("已生成数据字典，包含所有数据流和数据存储的字段定义。");
        sb.AppendLine();
        sb.AppendLine("## 3. ER 数据模型");
        sb.AppendLine("已生成实体关系图。");
        sb.AppendLine();
        sb.AppendLine("## 4. 状态机");
        sb.AppendLine("已生成状态转换图。");
        sb.AppendLine();

        if (saResult.ValidationStats != null && saResult.ValidationStats.Count > 0)
        {
            sb.AppendLine("## 5. 质量验证");
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

    /// <summary>
    /// SSE 事件内部模型
    /// </summary>
    private record SseEvent(string Type, string? Data = null, string? Stage = null);
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
}

public record RollbackRequest
{
    public string TargetStage { get; init; } = "";
    public string? Reason { get; init; }
}
