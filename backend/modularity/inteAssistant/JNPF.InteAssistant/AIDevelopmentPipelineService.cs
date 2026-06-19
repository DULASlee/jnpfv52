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
        ILogger<AIDevelopmentPipelineService> logger)
    {
        _pipelineEngine = pipelineEngine;
        _designOrchestrator = designOrchestrator;
        _llmGateway = llmGateway;
        _sandbox = sandbox;
        _db = sqlSugarClient;
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
        // 注意：后台任务不能复用请求作用域的 _db/_llmGateway（请求结束后 DI scope 销毁），
        //       必须从根 ServiceProvider 创建独立 scope 获取服务。
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
            // 注意：DeleteMark 可能为 null（Creator() 未设置该字段）或 0，用兼容查询
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
                // ChatStreamAsync 在出错时 yield return error 字符串（非 JSON，以 [ERROR] 开头）
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
    /// SSE 事件流 — 从 Channel 读取 LLM token，推送 {type:"chunk"}/{type:"done"}/{type:"error"}
    /// 对齐前端 useSSE.ts SSEMessage 契约。
    /// 通道不存在时短暂等待（最多 3 秒），容忍 /execute 与 /events 的时序竞态。
    /// </summary>
    [HttpGet("{pipelineId:long}/events")]
    public async Task GetPipelineEvents(long pipelineId, CancellationToken ct)
    {
        var response = App.HttpContext!.Response;
        response.ContentType = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";

        // 通道可能尚未创建（/execute 刚返回，后台任务还在排队），短暂轮询等待
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
        catch (OperationCanceledException)
        {
            // 客户端断开连接，正常退出
        }
        catch (ChannelClosedException)
        {
            // 通道已关闭，正常退出
        }
    }

    // ─── 确认阶段（人工审核）───

    /// <summary>
    /// 确认阶段（人工审核）
    /// </summary>
    [HttpPost("stage/{stageId:long}/confirm")]
    public async Task<StageResult> ConfirmStageAsync(
        long stageId, [FromBody] StageConfirmation confirmation)
    {
        // 兼容历史路由：前端当前传的是 pipelineId，后端引擎内部会按 pipelineId 优先解析。
        return await _pipelineEngine.ConfirmStageAsync(stageId, confirmation);
    }

    /// <summary>
    /// 回退到指定阶段（持久化）
    /// </summary>
    [HttpPost("{pipelineId:long}/rollback")]
    public async Task<StageResult> RollbackAsync(long pipelineId, [FromBody] RollbackRequest request)
    {
        var target = string.IsNullOrWhiteSpace(request.TargetStage) ? PipelineStage.Requirement : request.TargetStage;
        return await _pipelineEngine.RollbackAsync(pipelineId, target, request.Reason);
    }

    // ─── 获取流水线详情 ───

    /// <summary>
    /// 获取流水线详情
    /// </summary>
    [HttpGet("{pipelineId:long}")]
    public async Task<PipelineDetail> GetDetailAsync(long pipelineId)
    {
        return await _pipelineEngine.GetDetailAsync(pipelineId);
    }

    // ─── 流水线列表 ───

    /// <summary>
    /// 流水线列表
    /// </summary>
    [HttpGet("list")]
    public async Task<List<PipelineSummary>> ListAsync(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20)
    {
        return await _pipelineEngine.ListAsync(GetTenantId(), pageIndex, pageSize);
    }

    // ─── 执行详细设计（6 SubAgent 并行）───

    /// <summary>
    /// 执行详细设计（6 SubAgent 并行）
    /// </summary>
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

    /// <summary>
    /// 获取流水线 IR 版本快照
    /// </summary>
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

    /// <summary>
    /// 保存消息到 BASE_AI_PIPELINE_MESSAGE
    /// </summary>
    private async Task SaveMessageAsync(string pipelineId, string stage, string role, string content)
    {
        var msg = new AiPipelineMessageEntity
        {
            PipelineId = pipelineId,
            Stage = stage,
            Role = role,
            Content = content,
            Sequence = await GetNextSequenceAsync(pipelineId, stage),
            DeleteMark = 0  // 显式设置，Creator() 不会自动设此字段
        };
        msg.Creator();
        await _db.Insertable(msg).ExecuteCommandAsync();
    }

    /// <summary>
    /// 保存消息到 BASE_AI_PIPELINE_MESSAGE（指定 db 实例，用于后台任务）
    /// </summary>
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
            DeleteMark = 0  // 显式设置，Creator() 不会自动设此字段
        };
        msg.Creator();
        await db.Insertable(msg).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取下一个消息序号
    /// </summary>
    private async Task<int> GetNextSequenceAsync(string pipelineId, string stage)
    {
        return await GetNextSequenceAsync(_db, pipelineId, stage);
    }

    /// <summary>
    /// 获取下一个消息序号（指定 db 实例，用于后台任务）
    /// </summary>
    private static async Task<int> GetNextSequenceAsync(
        ISqlSugarClient db, string pipelineId, string stage)
    {
        var maxSeq = await db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId && x.Stage == stage)
            .MaxAsync(x => (int?)x.Sequence) ?? 0;
        return maxSeq + 1;
    }

    /// <summary>
    /// 从 LLM SSE JSON 中提取 token 文本（兼容 Anthropic / OpenAI 两种格式）
    /// </summary>
    private static string? ExtractToken(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Anthropic: {"type":"content_block_delta","delta":{"text":"..."}}
            if (root.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var text))
                return text.GetString();

            // OpenAI: {"choices":[{"delta":{"content":"..."}}]}
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("delta", out var delta2) &&
                    delta2.TryGetProperty("content", out var content))
                    return content.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 阶段系统提示词
    /// </summary>
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

    /// <summary>
    /// 写 SSE 事件到 HTTP 响应
    /// </summary>
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

    /// <summary>
    /// SSE 事件内部模型
    /// </summary>
    private record SseEvent(string Type, string? Data = null, string? Stage = null);
}

// ─── 请求 DTO ───

/// <summary>
/// 创建流水线请求（兼容前端 { requirement } 和标准 { name, userRequirement }）
/// </summary>
public record CreatePipelineInput
{
    /// <summary>用户需求（前端 AiChatPanel 发送此字段）</summary>
    public string? Requirement { get; init; }

    /// <summary>标准字段：流水线名称</summary>
    public string? Name { get; init; }

    /// <summary>标准字段：用户需求</summary>
    public string? UserRequirement { get; init; }
}

/// <summary>
/// 执行阶段请求（对齐前端 AiChatPanel 发送的 { message, stageName, provider }）
/// </summary>
public record ExecuteStageRequest
{
    /// <summary>阶段名称（requirement / architecture / design / development / delivery）</summary>
    public string StageName { get; init; } = "";

    /// <summary>用户消息内容</summary>
    public string? Message { get; init; }

    /// <summary>LLM 供应商代码（deepseek / mimo / openai / ollama）</summary>
    public string? Provider { get; init; }
}

public record RollbackRequest
{
    public string TargetStage { get; init; } = "";
    public string? Reason { get; init; }
}
