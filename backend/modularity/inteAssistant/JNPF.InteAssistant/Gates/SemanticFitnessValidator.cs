// 文件：Gates/SemanticFitnessValidator.cs
// 命名空间：JNPF.InteAssistant.Gates
// 职责：语义合格性校验器 — 判断用户提交的需求材料是否包含足够信息支撑 SA 流水线

using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 语义合格性校验器
///
/// 职责：判断用户提交的需求材料是否包含足够信息支撑 SA 流水线
/// 策略：Fail-Closed（任何异常都拒绝放行，绝不允许垃圾数据进入 SA 九步）
/// 依赖：ILlmGatewayService（语义分析） + GatePipelineOptions（阈值配置）
///
/// 修正的 4 个致命缺陷：
///   缺陷1 (Fail-Open)：所有 catch 路径统一返回 FailClosed → Passed=false
///   缺陷2 (JSON裸奔)：ExtractJson 三重防护（markdown剥离 + 大括号提取 + 尾逗号修复）
///   缺陷3 (Record写操作)：PostProcess 使用 .ToList() 拷贝 + with 表达式保持不可变性
///   缺陷4 (同步阻塞)：由调用方 AIDevelopmentPipelineService 通过 BackgroundTaskRunner 解决
/// </summary>
public class SemanticFitnessValidator : ITransient
{
    private readonly ILlmGatewayService _llmGateway;
    private readonly ILogger<SemanticFitnessValidator> _logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,          // LLM 经常输出尾逗号
        ReadCommentHandling = JsonCommentHandling.Skip // LLM 偶尔输出注释
    };

    static SemanticFitnessValidator()
    {
        // LLM prompt 要求 sufficient|partial|insufficient（小写），须 CamelCase 反序列化
        s_jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
    }

    public SemanticFitnessValidator(
        ILlmGatewayService llmGateway,
        ILogger<SemanticFitnessValidator> logger)
    {
        _llmGateway = llmGateway;
        _logger = logger;
    }

    /// <summary>
    /// 评估需求材料的语义合格性
    /// </summary>
    public async Task<SemanticFitnessResult> EvaluateAsync(
        string text, GatePipelineOptions options, CancellationToken ct = default)
    {
        try
        {
            var systemPrompt = BuildSystemPrompt(options);

            var response = await _llmGateway.ChatAsync(new ChatCompletionRequest
            {
                ProviderCode = options.SemanticProvider,
                SystemPrompt = systemPrompt,
                Messages = new List<ChatMessage> { new() { Role = "user", Content = text } },
                MaxTokens = 3000,
                Temperature = 0.1,
                ResponseFormat = "json",
                // ★ 收紧重试预算：语义评估是门控前置步骤，不应吞掉整个 5min 后台预算
                //   单次 30s × 1 次（无 fallback 自循环）= 最坏 30s 内 fail-closed 返回
                //   原值 MaxRetries=2 + TimeoutMs=45000 在 LLM 故障时最坏耗 ~3min，逼近后台超时
                MaxRetries = 1,
                TimeoutMs = 30000
            }, ct);

            if (!response.IsSuccess)
            {
                _logger.LogWarning("语义评估 LLM 调用失败: {Error}", response.Error);
                // #region agent log
                try
                {
                    var dbg = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        sessionId = "ead5d0",
                        runId = "gate-llm",
                        hypothesisId = "H3",
                        location = "SemanticFitnessValidator.EvaluateAsync:fail",
                        message = "GATE_LLM_ERR",
                        data = new
                        {
                            provider = options.SemanticProvider,
                            error = response.Error,
                            modelUsed = response.ModelUsed,
                            latencyMs = response.LatencyMs
                        },
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    System.IO.File.AppendAllText(@"D:\JNPF-v52\debug-ead5d0.log", dbg + "\n");
                }
                catch { }
                // #endregion
                return FailClosed("需求评估服务暂时不可用，请稍后重试。", "GATE_LLM_ERR");
            }

            // 宽容 JSON 提取（缺陷2修复）
            string json;
            try
            {
                json = ExtractJson(response.Content);
            }
            catch (JsonException ex)
            {
                // LLM 输出截断/格式异常（JsonReaderException 等）→ fail-closed，避免 GATE_UNEXPECTED
                _logger.LogWarning("JSON 提取失败（LLM 输出可能截断）: {Message}", ex.Message);
                return FailClosed("需求评估结果格式异常，请稍后重试。", "GATE_JSON_ERR");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("JSON 提取失败: {Message}", ex.Message);
                return FailClosed("需求评估结果格式异常，请稍后重试。", "GATE_JSON_ERR");
            }

            // 反序列化 + 结构校验
            SemanticFitnessResult result;
            try
            {
                result = DeserializeAndValidate(json);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("JSON 结构校验失败: {Message}", ex.Message);
                return FailClosed("需求评估结果结构异常，请稍后重试。", "GATE_SCHEMA_ERR");
            }

            // 硬阈值覆盖（缺陷3修复 — 通过 with 表达式保持不可变性）
            return PostProcess(result, options);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("语义评估被取消");
            return FailClosed("需求评估超时，请稍后重试。", "GATE_TIMEOUT");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "语义评估未预期异常: {Message}", ex.Message);
            return FailClosed("需求评估服务异常，请稍后重试。", "GATE_UNEXPECTED");
        }
    }

    // ═══════════════════════════════════════════
    // Prompt 构建
    // ═══════════════════════════════════════════

    private static string BuildSystemPrompt(GatePipelineOptions options)
    {
        return $$"""
        你是需求材料合格性评估器。判断用户提交的材料是否包含足够信息来支撑后续的系统需求分析。

        【最低合格标准——必须同时满足】
        1. 至少{{options.MinBusinessEvents}}个明确的业务事件（业务术语描述，如"工人提交工序报工"，不是CRUD动词"新增记录"）
        2. 至少{{options.MinRoles}}个具体角色（具体岗位如"车间工人"，不是泛称"管理员"）
        3. 至少{{options.MinDataEntities}}个数据实体
        4. 每个实体至少{{options.MinFieldsPerEntity}}个可推测的字段

        【评估规则】
        - 业务事件必须是业务动作，不是系统操作
        - 角色必须具体到岗位，不是泛称
        - 字段可以从上下文合理推断（如提到"工单"可推断工单号、数量、状态等）
        - 表格的列头可直接作为字段来源
        - 截图中识别出的界面元素可作为字段来源

        【输出格式——严格JSON，不要输出任何其他内容】
        {
          "passed": true或false,
          "score": 0到100的数字,
          "level": "sufficient|partial|insufficient",
          "identified": [
            {"category": "业务事件|角色|数据实体|字段|流程", "description": "描述", "evidence": "原文证据"}
          ],
          "missing": [
            {"category": "类别", "description": "描述", "severity": "critical或warning", "howToFix": "具体的修复建议，要给出示例"}
          ],
          "nextStepGuidance": "整体改进建议"
        }
        """;
    }

    // ═══════════════════════════════════════════
    // JSON 提取（宽容模式 — 缺陷2修复）
    // ═══════════════════════════════════════════

    /// <summary>
    /// 宽容提取：从 LLM 原始输出中提取 JSON
    /// 处理：markdown 包裹、前后文字、截断
    /// </summary>
    private static string ExtractJson(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            throw new InvalidOperationException("LLM 返回空内容");

        var cleaned = rawContent.Trim();

        // Step 1: 去掉 markdown 代码块包裹
        if (cleaned.StartsWith("```json"))
            cleaned = cleaned[7..];
        else if (cleaned.StartsWith("```"))
            cleaned = cleaned[3..];
        if (cleaned.EndsWith("```"))
            cleaned = cleaned[..^3];
        cleaned = cleaned.Trim();

        // Step 2: 提取第一个 { 到最后一个 } 的内容
        var start = cleaned.IndexOf('{');
        var end = cleaned.LastIndexOf('}');

        if (start < 0 || end <= start)
            throw new InvalidOperationException(
                $"LLM 返回内容中未找到有效 JSON: {cleaned[..Math.Min(200, cleaned.Length)]}");

        var candidate = cleaned[start..(end + 1)];

        // Step 3: 预校验——确保是合法 JSON
        try
        {
            using var doc = JsonDocument.Parse(candidate);
            return candidate;
        }
        catch (JsonException)
        {
            // 尝试修复常见问题：尾逗号（最常见的 LLM 错误）
            var fixed_ = candidate
                .Replace(",\n}", "\n}")
                .Replace(",\r\n}", "\r\n}")
                .Replace(",}", "}");

            using var doc = JsonDocument.Parse(fixed_);
            return fixed_;
        }
    }

    // ═══════════════════════════════════════════
    // 反序列化 + 结构校验（缺陷2修复续）
    // ═══════════════════════════════════════════

    private static SemanticFitnessResult DeserializeAndValidate(string json)
    {
        var result = JsonSerializer.Deserialize<SemanticFitnessResult>(json, s_jsonOptions)
            ?? throw new InvalidOperationException("JSON 反序列化返回 null");

        // 校验核心字段完整性
        if (result.Identified == null)
            throw new InvalidOperationException("JSON 缺少 identified 字段");
        if (result.Missing == null)
            throw new InvalidOperationException("JSON 缺少 missing 字段");
        if (result.Score < 0 || result.Score > 100)
            throw new InvalidOperationException($"score 超出范围: {result.Score}");
        // 空 JSON（如仅含 passed:true）——三条核心字段全空 = 结构性错误
        if (result.Score == 0 && result.Identified.Count == 0 && result.Missing.Count == 0)
            throw new InvalidOperationException("JSON 缺少评估数据（identified/missing/score 均为空）");

        return result;
    }

    // ═══════════════════════════════════════════
    // 硬阈值覆盖（缺陷3修复 — 不可变性安全）
    // ═══════════════════════════════════════════

    /// <summary>
    /// 后处理：硬阈值覆盖 + 最终判定
    /// 严格保持不可变性，所有修改都通过 with 表达式创建新副本
    /// </summary>
    private static SemanticFitnessResult PostProcess(SemanticFitnessResult raw, GatePipelineOptions options)
    {
        var missing = raw.Missing.ToList(); // 拷贝为可变 List（缺陷3修复）
        var passed = raw.Passed;
        var level = raw.Level;

        // 硬阈值1：至少 1 个业务事件
        if (!raw.Identified.Any(e => e.Category == "业务事件"))
        {
            passed = false;
            level = FitnessLevel.Insufficient;
            if (!missing.Any(m => m.Category == "业务事件"))
            {
                missing.Add(new MissingElement
                {
                    Category = "业务事件",
                    Description = "未能识别到任何业务事件",
                    Severity = "critical",
                    HowToFix = "请在需求描述中明确说明您要管理的业务场景。例如：'车间工人完成一道工序后，需要向系统提交报工记录，包括完成数量和质量情况。'"
                });
            }
        }

        // 硬阈值2：至少 1 个角色
        if (!raw.Identified.Any(e => e.Category == "角色"))
        {
            passed = false;
            level = FitnessLevel.Insufficient;
            if (!missing.Any(m => m.Category == "角色"))
            {
                missing.Add(new MissingElement
                {
                    Category = "角色",
                    Description = "未能识别到任何参与角色",
                    Severity = "critical",
                    HowToFix = "请说明系统中有哪些角色。例如：'车间工人负责报工，车间主任负责审核，质检员负责质量检验。'"
                });
            }
        }

        // 硬阈值3：至少 1 个数据实体
        if (!raw.Identified.Any(e => e.Category == "数据实体"))
        {
            passed = false;
            level = FitnessLevel.Insufficient;
            if (!missing.Any(m => m.Category == "数据实体"))
            {
                missing.Add(new MissingElement
                {
                    Category = "数据实体",
                    Description = "未能识别到任何数据实体",
                    Severity = "critical",
                    HowToFix = "请说明系统需要管理哪些数据。例如：'系统需要管理工单、报工记录、员工信息、设备信息等。'"
                });
            }
        }

        // 硬阈值4：分数过低
        if (raw.Score < options.SemanticMinScore)
        {
            passed = false;
            level = FitnessLevel.Insufficient;
        }

        // ★ 关键：通过 with 表达式创建新副本，维持不可变性（缺陷3修复）
        return raw with
        {
            Passed = passed,
            Level = level,
            Missing = missing
        };
    }

    // ═══════════════════════════════════════════
    // Fail-Closed 降级（缺陷1修复）
    // ═══════════════════════════════════════════

    /// <summary>
    /// Fail-Closed：所有异常情况统一返回不合格
    /// 绝不允许垃圾数据进入 SA 流水线
    /// </summary>
    private static SemanticFitnessResult FailClosed(string message, string errorCode)
    {
        return new SemanticFitnessResult
        {
            Passed = false,
            Score = 0,
            Level = FitnessLevel.Insufficient,
            Identified = new List<IdentifiedElement>(),
            Missing = new List<MissingElement>
            {
                new MissingElement
                {
                    Category = "系统",
                    Description = $"评估服务异常 ({errorCode})",
                    Severity = "critical",
                    HowToFix = message
                }
            },
            NextStepGuidance = $"{message}\n错误代码: {errorCode}"
        };
    }
}
