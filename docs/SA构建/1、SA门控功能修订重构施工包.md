# SA门控代码审核——D爷致命缺陷修正 + 工程师执行手册

---

## 一、对D爷4个致命缺陷的逐条确认与修正

### 致命缺陷1：Fail-Open降级策略

**D爷判定**：语义评估失败时放行所有请求，垃圾数据进入SA流水线。

**确认**：D爷说得完全对。这是企业级系统的安全红线。在金融、制造、医疗等场景中，"不可靠时拒绝服务"是铁律。Fail-Open的后果是：LLM网关一旦超时（概率不低，尤其在高峰期），所有"我要做个管理系统"级别的垃圾输入全部灌入SA九步，触发后续Agent幻觉风暴，污染知识图谱，修复成本极高。

**修正后的完整降级策略**：

```csharp
// SemanticFitnessValidator.cs 中的异常处理

public async Task<SemanticFitnessResult> EvaluateAsync(
    string text, GatePipelineOptions options, CancellationToken ct = default)
{
    try
    {
        var response = await _llmGateway.ChatAsync(request, ct);
        
        if (!response.IsSuccess)
        {
            _logger.LogWarning("语义评估LLM调用失败: {Error}", response.Error);
            return FailClosed("需求评估服务暂时不可用，请稍后重试。", "GATE_LLM_ERR");
        }

        var json = ExtractJson(response.Content);
        var result = JsonSerializer.Deserialize<SemanticFitnessResult>(json, s_jsonOptions);

        // 反序列化后必须校验核心字段完整性
        if (result == null || result.Identified == null || result.Missing == null)
        {
            _logger.LogWarning("LLM返回的JSON结构不符合契约");
            return FailClosed("需求评估结果解析异常，请稍后重试。", "GATE_PARSE_ERR");
        }

        // 硬阈值覆盖
        return PostProcess(result, options);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        _logger.LogWarning("语义评估被取消");
        return FailClosed("需求评估超时，请稍后重试。", "GATE_TIMEOUT");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "语义评估异常");
        return FailClosed("需求评估服务异常，请稍后重试。", "GATE_UNEXPECTED");
    }
}

/// <summary>
/// Fail-Closed：所有异常情况统一返回不合格
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
```

---

### 致命缺陷2：LLM JSON解析裸奔

**D爷判定**：LLM即使配置了json模式，仍有概率输出Markdown包裹、首尾幻觉字符、截断JSON，直接Deserialize会抛JsonException，触发Fail-Open（缺陷1的连锁反应）。

**确认**：D爷说得对。这个问题在生产环境中确实频繁发生，尤其是DeepSeek模型经常在JSON输出前后附加解释性文字。

**修正后的完整JSON解析链路**：

```csharp
// SemanticFitnessValidator.cs 中的JSON解析

private static readonly JsonSerializerOptions s_jsonOptions = new()
{
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,          // LLM经常输出尾逗号
    ReadCommentHandling = JsonCommentHandling.Skip, // LLM偶尔输出注释
};

/// <summary>
/// 宽容提取：从LLM原始输出中提取JSON
/// 处理：markdown包裹、前后文字、截断
/// </summary>
private static string ExtractJson(string rawContent)
{
    if (string.IsNullOrWhiteSpace(rawContent))
        throw new InvalidOperationException("LLM返回空内容");

    // Step 1: 去掉markdown代码块包裹
    var cleaned = rawContent.Trim();
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
        throw new InvalidOperationException($"LLM返回内容中未找到有效JSON: {cleaned[..Math.Min(200, cleaned.Length)]}");

    var candidate = cleaned[start..(end + 1)];

    // Step 3: 预校验——确保是合法JSON
    try
    {
        using var doc = JsonDocument.Parse(candidate);
        return candidate;
    }
    catch (JsonException ex)
    {
        // 尝试修复常见问题：单引号、尾逗号、未转义换行
        var fixed_ = candidate
            .Replace("'", "\"")
            .Replace(",\n}", "\n}")
            .Replace(",\r\n}", "\r\n}")
            .Replace(",}", "}");

        try
        {
            using var doc = JsonDocument.Parse(fixed_);
            return fixed_;
        }
        catch
        {
            throw new InvalidOperationException($"LLM返回的JSON无法解析: {ex.Message}");
        }
    }
}

/// <summary>
/// 反序列化后的结构校验
/// </summary>
private static SemanticFitnessResult DeserializeAndValidate(string json, GatePipelineOptions options)
{
    var result = JsonSerializer.Deserialize<SemanticFitnessResult>(json, s_jsonOptions)
        ?? throw new InvalidOperationException("JSON反序列化返回null");

    // 校验核心字段完整性
    if (result.Identified == null)
        throw new InvalidOperationException("JSON缺少identified字段");
    if (result.Missing == null)
        throw new InvalidOperationException("JSON缺少missing字段");
    if (result.Score < 0 || result.Score > 100)
        throw new InvalidOperationException($"score超出范围: {result.Score}");

    return result;
}
```

---

### 致命缺陷3：不可变Record的写操作陷阱

**D爷判定**：`SemanticFitnessResult` 定义为 `record`，属性默认 `init` 只读。`result.Missing.Add(...)` 会抛 `NotSupportedException`。

**确认**：D爷说得完全正确。这是C# record语法的常见陷阱。

**修正后的硬阈值覆盖逻辑**：

```csharp
/// <summary>
/// 后处理：硬阈值覆盖 + 最终判定
/// 严格保持不可变性，所有修改都通过 with 表达式创建新副本
/// </summary>
private SemanticFitnessResult PostProcess(SemanticFitnessResult raw, GatePipelineOptions options)
{
    var identified = raw.Identified;
    var missing = raw.Missing.ToList(); // 拷贝为可变List
    var passed = raw.Passed;
    var level = raw.Level;

    // 硬阈值1：至少1个业务事件
    if (!identified.Any(e => e.Category == "业务事件"))
    {
        passed = false;
        level = FitnessLevel.Insufficient;
        missing.Add(new MissingElement
        {
            Category = "业务事件",
            Description = "未能识别到任何业务事件",
            Severity = "critical",
            HowToFix = "请在需求描述中明确说明您要管理的业务场景。例如：'车间工人完成一道工序后，需要向系统提交报工记录，包括完成数量和质量情况。'"
        });
    }

    // 硬阈值2：至少1个角色
    if (!identified.Any(e => e.Category == "角色"))
    {
        passed = false;
        level = FitnessLevel.Insufficient;
        missing.Add(new MissingElement
        {
            Category = "角色",
            Description = "未能识别到任何参与角色",
            Severity = "critical",
            HowToFix = "请说明系统中有哪些角色在使用系统。例如：'车间工人负责报工，车间主任负责审核，质检员负责质量检验。'"
        });
    }

    // 硬阈值3：至少1个数据实体
    if (!identified.Any(e => e.Category == "数据实体"))
    {
        passed = false;
        level = FitnessLevel.Insufficient;
        missing.Add(new MissingElement
        {
            Category = "数据实体",
            Description = "未能识别到任何数据实体",
            Severity = "critical",
            HowToFix = "请说明系统需要管理哪些数据。例如：'系统需要管理工单、报工记录、员工信息、设备信息等。'"
        });
    }

    // 硬阈值4：分数过低
    if (raw.Score < options.SemanticMinScore)
    {
        passed = false;
        level = FitnessLevel.Insufficient;
    }

    // 整体替换，维持不可变性
    return raw with
    {
        Passed = passed,
        Level = level,
        Missing = missing  // 新的List替换原有的
    };
}
```

---

### 致命缺陷4：门控同步阻塞导致HTTP超时

**D爷判定**：门控涉及附件提取（IO密集）、多模态图片分析（网络调用）、LLM语义评估（网络调用），总耗时极可能超过30秒，导致Nginx 504超时。

**确认**：D爷说得对。当前 `GatePipeline.ExecuteAsync` 是同步阻塞HTTP请求的。附件多、图片大、LLM慢的时候，60秒都打不住。

**修正方案：异步事件驱动，复用已有的BackgroundTaskRunner和SSE通道**

```csharp
// AIDevelopmentPipelineService.cs 中的集成层改造

/// <summary>
/// SA门控入口——改为异步事件驱动
/// 前端提交 → 立即返回202 → 后台执行门控 → SSE推送结果
/// </summary>
public async Task<IActionResult> ExecuteGateAsync(
    long pipelineId, string userText, List<AttachmentFile> attachments, 
    RequestContext ctx, CancellationToken ct)
{
    // Step 1: 持久化附件到DB（同步，必须先存再异步处理）
    var materialId = await PersistAttachmentsAsync(pipelineId, attachments, ctx, ct);

    // Step 2: 立即返回202 Accepted
    var response = new
    {
        pipelineId,
        materialId,
        status = "processing",
        message = "需求材料正在评估中，请等待结果..."
    };

    // Step 3: 后台异步执行门控
    var channel = $"pipeline-{pipelineId}";
    _backgroundTaskRunner.Run(
        $"SA_Gate_{pipelineId}",
        async (bgCtx, bgCt) =>
        {
            using var sse = _sseSenderFactory.Create(pipelineId.ToString(), channel);
            
            try
            {
                // 通知前端：门控开始
                await sse.SendAsync(new { type = "gate_started", timestamp = DateTime.UtcNow });

                // 执行门控（附件提取+图片分析+硬规则+语义评估）
                var gateResult = await _gatePipeline.ExecuteAsync(
                    userText, attachments, ctx,
                    visionApiUrl, visionApiKey, visionModel, bgCt);

                if (gateResult.Passed)
                {
                    // 门控通过 → 通知前端
                    await sse.SendAsync(new
                    {
                        type = "gate_passed",
                        mergedText = gateResult.MergedText,
                        warnings = gateResult.Warnings,
                        semanticFitness = gateResult.SemanticFitness
                    });

                    // ★ 自动进入Stage 1（骨架预分析）
                    await TransitionToStage1Async(pipelineId, gateResult, bgCtx, bgCt);
                }
                else
                {
                    // 门控不通过 → 推送详细反馈
                    await sse.SendAsync(new
                    {
                        type = "gate_failed",
                        reason = gateResult.Reason,
                        hint = gateResult.Hint,
                        semanticFitness = gateResult.SemanticFitness,
                        warnings = gateResult.Warnings
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SA门控执行异常: PipelineId={PipelineId}", pipelineId);
                await sse.SendAsync(new
                {
                    type = "gate_error",
                    message = "需求评估过程中发生异常，请重试。",
                    errorCode = "GATE_INTERNAL_ERROR"
                });
            }
        },
        timeout: TimeSpan.FromMinutes(5)); // 门控总超时5分钟

    return new AcceptedResult();
}
```

**前端SSE监听改造**：

```typescript
// submit-requirement.vue 中的SSE监听

const eventSource = new EventSource(`/api/ai/pipeline/${pipelineId}/events`);

eventSource.addEventListener('gate_started', (e) => {
    status.value = 'processing';
    statusText.value = '正在评估需求材料...';
});

eventSource.addEventListener('gate_passed', (e) => {
    const data = JSON.parse(e.data);
    status.value = 'passed';
    statusText.value = '需求材料评估通过，正在进入需求分析...';
    // 自动进入Stage 1，无需用户操作
});

eventSource.addEventListener('gate_failed', (e) => {
    const data = JSON.parse(e.data);
    status.value = 'failed';
    // 展示结构化反馈
    gateResult.value = data.semanticFitness;
    // 显示：已识别要素 + 缺失要素 + 具体改进建议
});

eventSource.addEventListener('gate_error', (e) => {
    const data = JSON.parse(e.data);
    status.value = 'error';
    errorMessage.value = data.message;
    errorCode.value = data.errorCode;
});
```

---

## 二、附加修正：Excel表头检测Bug

D爷指出 `worksheet.Cells[1, 1].Merge` 判断不严谨。修正如下：

```csharp
// AttachmentProcessor.cs 中的Excel提取

private string ExtractExcel(byte[] content)
{
    using var stream = new MemoryStream(content);
    using var package = new ExcelPackage(stream);
    var workbook = package.Workbook;

    if (workbook?.Worksheets == null || workbook.Worksheets.Count == 0)
        return "";

    var result = new StringBuilder();

    foreach (var worksheet in workbook.Worksheets)
    {
        result.AppendLine($"【Sheet: {worksheet.Name}】");

        if (worksheet.Dimension == null)
        {
            result.AppendLine("（空表）");
            continue;
        }

        var colCount = Math.Min(worksheet.Dimension.End.Column, 20);

        // ★ 表头行检测：比较第1行和第2行的非空单元格数量
        int headerRow = DetectHeaderRow(worksheet, colCount);

        var rowCount = Math.Min(worksheet.Dimension.End.Row, headerRow + 50);

        // 表头
        var headers = new List<string>();
        for (int col = 1; col <= colCount; col++)
        {
            var val = worksheet.Cells[headerRow, col]?.Text?.Trim();
            headers.Add(string.IsNullOrWhiteSpace(val) ? $"列{col}" : val);
        }
        result.AppendLine(string.Join(" | ", headers));
        result.AppendLine(new string('-', headers.Sum(h => h.Length) + headers.Count * 3));

        // 数据行
        for (int row = headerRow + 1; row <= rowCount; row++)
        {
            var cells = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                cells.Add(worksheet.Cells[row, col]?.Text?.Trim() ?? "");
            }
            result.AppendLine(string.Join(" | ", cells));
        }

        if (worksheet.Dimension.End.Row > headerRow + 50)
        {
            result.AppendLine($"... 共{worksheet.Dimension.End.Row}行，仅显示前50行");
        }
        result.AppendLine();
    }

    return result.ToString();
}

/// <summary>
/// 检测表头行号
/// 规则：如果第1行只有≤1个非空单元格，且第2行有多个非空单元格，则第2行是表头
/// </summary>
private static int DetectHeaderRow(ExcelWorksheet worksheet, int colCount)
{
    if (worksheet.Dimension.End.Row < 2)
        return 1; // 只有一行，就当表头

    int row1NonEmpty = CountNonEmptyCells(worksheet, 1, colCount);
    int row2NonEmpty = CountNonEmptyCells(worksheet, 2, colCount);

    // 第1行几乎为空（标题行），第2行有多个单元格（真正的表头）
    if (row1NonEmpty <= 1 && row2NonEmpty > 1)
        return 2;

    return 1;
}

private static int CountNonEmptyCells(ExcelWorksheet worksheet, int row, int colCount)
{
    int count = 0;
    for (int col = 1; col <= colCount; col++)
    {
        if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col]?.Text))
            count++;
    }
    return count;
}
```

---

## 三、完整的修正后门控管道（终版）

```csharp
// GatePipeline.cs — 终版（含全部修正）

public sealed class GatePipeline : IGatePipeline, ITransient
{
    private readonly AttachmentProcessor _attachmentProcessor;
    private readonly RequirementGateService _gateService;
    private readonly SemanticFitnessValidator _semanticValidator;  // ★ 新增
    private readonly ITenantGuard _tenantGuard;
    private readonly IOptionsMonitor<GatePipelineOptions> _optionsMonitor;
    private readonly ILogger<GatePipeline> _logger;

    public GatePipeline(
        AttachmentProcessor attachmentProcessor,
        RequirementGateService gateService,
        SemanticFitnessValidator semanticValidator,  // ★ 新增
        ITenantGuard tenantGuard,
        IOptionsMonitor<GatePipelineOptions> optionsMonitor,
        ILogger<GatePipeline> logger)
    {
        _attachmentProcessor = attachmentProcessor;
        _gateService = gateService;
        _semanticValidator = semanticValidator;
        _tenantGuard = tenantGuard;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task<GateResult> ExecuteAsync(
        string userText,
        List<AttachmentFile> attachments,
        RequestContext ctx,
        GateContext? gateContext = null,       // ★ 新增：可选上下文
        string visionApiUrl = "",
        string visionApiKey = "",
        string visionModel = "",
        CancellationToken ct = default)
    {
        var options = _optionsMonitor.CurrentValue;
        var sw = Stopwatch.StartNew();
        var warnings = new List<string>();

        // ═══════════ 步骤0：验证附件 ═══════════
        var (validAttachments, blockedCount, validationWarnings) = ValidateAttachments(attachments, options);
        warnings.AddRange(validationWarnings);

        if (validAttachments.Count > options.MaxAttachmentCount)
            return GateResult.Fail($"附件数量超限（最多{options.MaxAttachmentCount}个）", warnings);

        // ═══════════ 步骤1：并行提取附件文本 ═══════════
        var attachmentTexts = new ConcurrentBag<string>();
        var processingErrors = new ConcurrentBag<string>();
        var semaphore = new SemaphoreSlim(options.MaxConcurrentFiles);

        var tasks = validAttachments.Select(async file =>
        {
            await semaphore.WaitAsync(ct);
            using var fileCts = new CancellationTokenSource(options.PerFileTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(fileCts.Token, ct);

            try
            {
                var text = await _attachmentProcessor.ProcessAttachmentsAsync(new List<AttachmentFile> { file });
                if (!string.IsNullOrWhiteSpace(text))
                    attachmentTexts.Add(text);
            }
            catch (OperationCanceledException) when (fileCts.IsCancellationRequested)
            {
                processingErrors.Add(file.FileName);
                warnings.Add($"文件 {file.FileName} 处理超时");
            }
            catch (OutOfMemoryException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "文件处理失败: {Name}", file.FileName);
                processingErrors.Add(file.FileName);
                warnings.Add($"文件 {file.FileName} 处理失败");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        blockedCount += processingErrors.Count;
        var attachmentText = string.Join("\n\n", attachmentTexts);

        // ═══════════ 步骤2：图片多模态提取 ═══════════
        var imageFiles = validAttachments
            .Where(f => GateConstants.IsImageFile(f.FileName))
            .Where(f => !processingErrors.Contains(f.FileName))
            .ToList();

        var imageAnalysisFailed = 0;
        if (imageFiles.Count > 0 && !string.IsNullOrWhiteSpace(visionApiKey))
        {
            try
            {
                var imageAnalysis = await _gateService.ExtractFromImages(
                    imageFiles, visionApiUrl, visionApiKey, visionModel, ct);
                attachmentText += "\n\n" + imageAnalysis;
            }
            catch (OutOfMemoryException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "图片多模态分析失败");
                imageAnalysisFailed = imageFiles.Count;
                warnings.Add($"图片多模态分析失败（{imageFiles.Count}张），已跳过");
            }
        }

        // ★ 全部图片失败的特殊处理
        if (imageFiles.Count > 0 && imageAnalysisFailed == imageFiles.Count && string.IsNullOrWhiteSpace(userText))
        {
            warnings.Add($"全部{imageFiles.Count}张图片处理失败，且无文字描述。如果图片包含关键需求信息，请重新上传或用文字描述。");
        }

        // ═══════════ 步骤3：合并文本（带来源标记） ═══════════
        var fullText = BuildMergedText(userText, attachmentText);

        // ═══════════ 步骤4：硬规则校验 ═══════════
        var actualAttachmentCount = validAttachments.Count - blockedCount;
        var hardRule = _gateService.ValidateHardRules(fullText, actualAttachmentCount);
        if (!hardRule.Passed)
            return GateResult.Fail(hardRule.Reason, warnings, hardRule.Hint);

        // ═══════════ 步骤4.5：语义合格性评估（★ 核心新增） ═══════════
        var semanticResult = await _semanticValidator.EvaluateAsync(fullText, options, ct);
        if (!semanticResult.Passed)
        {
            sw.Stop();
            _logger.LogInformation("门控语义不合格: Score={Score}, 耗时{Ms}ms",
                semanticResult.Score, sw.ElapsedMilliseconds);
            return GateResult.SemanticallyUnfit(semanticResult, warnings);
        }

        // ═══════════ 步骤5：输出 ═══════════
        sw.Stop();
        _logger.LogInformation("门控通过: Score={Score}, 文字{TextLen}字, 附件{AttCount}个, 耗时{Ms}ms",
            semanticResult.Score, userText.Length, actualAttachmentCount, sw.ElapsedMilliseconds);

        return new GateResult
        {
            Passed = true,
            MergedText = fullText,
            AttachmentText = attachmentText,
            AttachmentCount = actualAttachmentCount,
            BlockedCount = blockedCount,
            Warnings = warnings,
            SemanticFitness = semanticResult  // ★ 携带语义评估结果，供Stage 1使用
        };
    }

    /// <summary>
    /// 合并文本，带来源标记
    /// </summary>
    private static string BuildMergedText(string userText, string attachmentText)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(userText))
            parts.Add($"【用户输入】\n{userText}");

        if (!string.IsNullOrWhiteSpace(attachmentText))
            parts.Add($"【附件提取内容】{attachmentText}");

        return string.Join("\n\n", parts);
    }

    // ValidateAttachments 方法保持不变...
}
```

---

## 四、修正后的DTO定义（终版）

```csharp
// Gates/GateResult.cs — 终版

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 门控执行结果（不可变 record）
/// </summary>
public sealed record GateResult
{
    public bool Passed { get; init; }
    public string Reason { get; init; } = "";
    public string Hint { get; init; } = "";
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public string MergedText { get; init; } = "";
    public string AttachmentText { get; init; } = "";
    public int AttachmentCount { get; init; }
    public int BlockedCount { get; init; }
    
    /// <summary>语义评估结果（门控通过时也携带，供Stage 1骨架提取使用）</summary>
    public SemanticFitnessResult? SemanticFitness { get; init; }

    public static GateResult Fail(string reason, List<string>? warnings = null, string hint = "") =>
        new()
        {
            Passed = false,
            Reason = reason,
            Hint = hint,
            Warnings = warnings?.AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>()
        };

    /// <summary>语义不合格的工厂方法</summary>
    public static GateResult SemanticallyUnfit(SemanticFitnessResult fitness, List<string>? warnings = null) =>
        new()
        {
            Passed = false,
            SemanticFitness = fitness,
            Reason = fitness.BuildSummary(),
            Hint = fitness.BuildGuidance(),
            Warnings = warnings?.AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>()
        };
}

/// <summary>
/// 语义合格性评估结果（不可变 record）
/// </summary>
public sealed record SemanticFitnessResult
{
    public bool Passed { get; init; }
    public double Score { get; init; }
    public FitnessLevel Level { get; init; } = FitnessLevel.Insufficient;
    public List<IdentifiedElement> Identified { get; init; } = new();
    public List<MissingElement> Missing { get; init; } = new();
    public string NextStepGuidance { get; init; } = "";

    public string BuildSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"需求材料评估结果：{Level}（评分 {Score:F0}/100）");

        if (Identified.Count > 0)
        {
            sb.AppendLine("\n✅ 已识别的要素：");
            foreach (var item in Identified)
                sb.AppendLine($"  - {item.Category}：{item.Description}");
        }

        var criticalMissing = Missing.Where(m => m.Severity == "critical").ToList();
        if (criticalMissing.Count > 0)
        {
            sb.AppendLine("\n❌ 缺失的关键要素（必须补充）：");
            foreach (var item in criticalMissing)
                sb.AppendLine($"  - {item.Category}：{item.Description}");
        }

        return sb.ToString();
    }

    public string BuildGuidance()
    {
        if (!string.IsNullOrEmpty(NextStepGuidance))
            return NextStepGuidance;

        var criticalMissing = Missing.Where(m => m.Severity == "critical").ToList();
        if (criticalMissing.Count == 0)
            return "请补充缺失要素后重新提交。";

        var sb = new StringBuilder();
        sb.AppendLine("请根据以下建议补充需求材料：\n");
        for (int i = 0; i < criticalMissing.Count; i++)
        {
            sb.AppendLine($"{i + 1}. 【{criticalMissing[i].Category}】{criticalMissing[i].HowToFix}");
        }
        return sb.ToString();
    }
}

public enum FitnessLevel
{
    /// <summary>足够：至少1个完整业务事件+角色+实体+5字段</summary>
    Sufficient,
    /// <summary>部分：有部分内容但不完整</summary>
    Partial,
    /// <summary>不足：几乎无法提取有效信息</summary>
    Insufficient
}

/// <summary>已识别的要素</summary>
public sealed record IdentifiedElement
{
    public string Category { get; init; } = "";      // 业务事件/角色/数据实体/字段/流程
    public string Description { get; init; } = "";
    public string Evidence { get; init; } = "";       // 从原文中提取的证据
}

/// <summary>缺失的要素</summary>
public sealed record MissingElement
{
    public string Category { get; init; } = "";
    public string Description { get; init; } = "";
    public string Severity { get; init; } = "critical";  // critical / warning
    public string HowToFix { get; init; } = "";           // 具体的修复建议
}
```

---

## 五、修正后的SemanticFitnessValidator（终版）

```csharp
// Gates/SemanticFitnessValidator.cs — 终版

using JNPF.DependencyInjection;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 语义合格性校验器
/// 
/// 职责：判断用户提交的需求材料是否包含足够信息支撑SA流水线
/// 策略：Fail-Closed（任何异常都拒绝放行）
/// 依赖：ILlmGatewayService（语义分析） + GatePipelineOptions（阈值配置）
/// </summary>
public class SemanticFitnessValidator : ITransient
{
    private readonly ILlmGatewayService _llmGateway;
    private readonly ILogger<SemanticFitnessValidator> _logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

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
                MaxTokens = 1500,
                Temperature = 0.1,
                ResponseFormat = "json",
                MaxRetries = 2,
                TimeoutMs = 45000
            }, ct);

            if (!response.IsSuccess)
            {
                _logger.LogWarning("语义评估LLM调用失败: {Error}", response.Error);
                return FailClosed("需求评估服务暂时不可用，请稍后重试。", "GATE_LLM_ERR");
            }

            // 宽容JSON提取
            string json;
            try
            {
                json = ExtractJson(response.Content);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("JSON提取失败: {Message}", ex.Message);
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
                _logger.LogWarning("JSON结构校验失败: {Message}", ex.Message);
                return FailClosed("需求评估结果结构异常，请稍后重试。", "GATE_SCHEMA_ERR");
            }

            // 硬阈值覆盖
            return PostProcess(result, options);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("语义评估被取消");
            return FailClosed("需求评估超时，请稍后重试。", "GATE_TIMEOUT");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "语义评估未预期异常");
            return FailClosed("需求评估服务异常，请稍后重试。", "GATE_UNEXPECTED");
        }
    }

    // ═══════════════════════════════════════════
    // Prompt 构建
    // ═══════════════════════════════════════════

    private static string BuildSystemPrompt(GatePipelineOptions options)
    {
        return $"""
        你是需求材料合格性评估器。判断用户提交的材料是否包含足够信息来支撑后续的系统需求分析。

        【最低合格标准——必须同时满足】
        1. 至少{options.MinBusinessEvents}个明确的业务事件（业务术语描述，如"工人提交工序报工"，不是CRUD动词"新增记录"）
        2. 至少{options.MinRoles}个具体角色（具体岗位如"车间工人"，不是泛称"管理员"）
        3. 至少{options.MinDataEntities}个数据实体
        4. 每个实体至少{options.MinFieldsPerEntity}个可推测的字段

        【评估规则】
        - 业务事件必须是业务动作，不是系统操作
        - 角色必须具体到岗位，不是泛称
        - 字段可以从上下文合理推断（如提到"工单"可推断工单号、数量、状态等）
        - 表格的列头可直接作为字段来源
        - 截图中识别出的界面元素可作为字段来源

        【输出格式——严格JSON，不要输出任何其他内容】
        {{
          "passed": true或false,
          "score": 0到100的数字,
          "level": "sufficient|partial|insufficient",
          "identified": [
            {{"category": "业务事件|角色|数据实体|字段|流程", "description": "描述", "evidence": "原文证据"}}
          ],
          "missing": [
            {{"category": "类别", "description": "描述", "severity": "critical或warning", "howToFix": "具体的修复建议，要给出示例"}}
          ],
          "nextStepGuidance": "整体改进建议"
        }}
        """;
    }

    // ═══════════════════════════════════════════
    // JSON 提取（宽容模式）
    // ═══════════════════════════════════════════

    private static string ExtractJson(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            throw new InvalidOperationException("LLM返回空内容");

        var cleaned = rawContent.Trim();

        // 去掉markdown代码块包裹
        if (cleaned.StartsWith("```json"))
            cleaned = cleaned[7..];
        else if (cleaned.StartsWith("```"))
            cleaned = cleaned[3..];
        if (cleaned.EndsWith("```"))
            cleaned = cleaned[..^3];
        cleaned = cleaned.Trim();

        // 提取第一个 { 到最后一个 }
        var start = cleaned.IndexOf('{');
        var end = cleaned.LastIndexOf('}');

        if (start < 0 || end <= start)
            throw new InvalidOperationException(
                $"LLM返回内容中未找到有效JSON: {cleaned[..Math.Min(200, cleaned.Length)]}");

        var candidate = cleaned[start..(end + 1)];

        // 预校验
        try
        {
            using var doc = JsonDocument.Parse(candidate);
            return candidate;
        }
        catch (JsonException)
        {
            // 尝试修复常见问题
            var fixed_ = candidate
                .Replace(",\n}", "\n}")
                .Replace(",\r\n}", "\r\n}")
                .Replace(",}", "}");

            using var doc = JsonDocument.Parse(fixed_);
            return fixed_;
        }
    }

    // ═══════════════════════════════════════════
    // 反序列化 + 结构校验
    // ═══════════════════════════════════════════

    private static SemanticFitnessResult DeserializeAndValidate(string json)
    {
        var result = JsonSerializer.Deserialize<SemanticFitnessResult>(json, s_jsonOptions)
            ?? throw new InvalidOperationException("JSON反序列化返回null");

        if (result.Identified == null)
            throw new InvalidOperationException("JSON缺少identified字段");
        if (result.Missing == null)
            throw new InvalidOperationException("JSON缺少missing字段");
        if (result.Score < 0 || result.Score > 100)
            throw new InvalidOperationException($"score超出范围: {result.Score}");

        return result;
    }

    // ═══════════════════════════════════════════
    // 硬阈值覆盖（不可变性安全）
    // ═══════════════════════════════════════════

    private SemanticFitnessResult PostProcess(SemanticFitnessResult raw, GatePipelineOptions options)
    {
        var missing = raw.Missing.ToList(); // 拷贝为可变List
        var passed = raw.Passed;
        var level = raw.Level;

        // 硬阈值1：至少1个业务事件
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

        // 硬阈值2：至少1个角色
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

        // 硬阈值3：至少1个数据实体
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

        // ★ 关键：通过 with 表达式创建新副本，维持不可变性
        return raw with
        {
            Passed = passed,
            Level = level,
            Missing = missing
        };
    }

    // ═══════════════════════════════════════════
    // Fail-Closed 降级
    // ═══════════════════════════════════════════

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
```

---

## 六、修正后的上下游衔接（终版）

### 6.1 上游：前端提交 → 门控执行

```
前端 MaterialUploader.vue
    │ POST /api/ai/pipeline/{id}/upload-materials
    ▼
后端 持久化附件到DB
    │ 返回 { materialId }
    ▼
前端 POST /api/ai/pipeline/{id}/sa-gate
    │ 后端返回 202 Accepted + { pipelineId, status: "processing" }
    ▼
前端 开启SSE监听 /api/ai/pipeline/{id}/events
    │
    ├── gate_started → 显示"正在评估..."
    ├── gate_passed → 自动进入Stage 1
    │     payload: { mergedText, warnings, semanticFitness }
    ├── gate_failed → 显示结构化反馈
    │     payload: { reason, hint, semanticFitness: { identified[], missing[] } }
    └── gate_error → 显示错误信息+错误代码
```

### 6.2 下游：门控通过 → Stage 1骨架提取

```csharp
// GateResult.Passed = true后，自动触发Stage 1

private async Task TransitionToStage1Async(
    long pipelineId, GateResult gateResult, RequestContext ctx, CancellationToken ct)
{
    // 门控已识别的要素作为骨架提取的"种子输入"
    var seedInput = new
    {
        fullText = gateResult.MergedText,
        // ★ 从语义评估中提取已识别要素，避免Stage 1重复识别
        identifiedEvents = gateResult.SemanticFitness?.Identified
            .Where(e => e.Category == "业务事件")
            .Select(e => e.Description)
            .ToList() ?? new List<string>(),
        identifiedRoles = gateResult.SemanticFitness?.Identified
            .Where(e => e.Category == "角色")
            .Select(e => e.Description)
            .ToList() ?? new List<string>(),
        identifiedEntities = gateResult.SemanticFitness?.Identified
            .Where(e => e.Category == "数据实体")
            .Select(e => e.Description)
            .ToList() ?? new List<string>()
    };

    // 持久化门控结果到 BASE_AI_PIPELINE_MESSAGE
    await SavePipelineMessageAsync(pipelineId, 0, "system", 
        JsonSerializer.Serialize(gateResult), "gate_result", ct);

    // 启动Stage 1骨架提取
    await _skeletonExtractionService.StartAsync(pipelineId, seedInput, ctx, ct);
}
```

### 6.3 回退：SA九步驳回 → 回到事件精炼

门控产出的 `MergedText` 和 `SemanticFitness` 已持久化到 `BASE_AI_PIPELINE_MESSAGE`。当后续SA九步被驳回时，可以直接读取这些数据作为重新分析的输入，无需用户重新提交。

---

## 七、TDD红绿测试计划（终版）

### 7.1 单元测试：SemanticFitnessValidatorTests.cs

```csharp
public class SemanticFitnessValidatorTests
{
    // ════════ 绿灯（应该通过） ════════

    [Fact]
    public async Task 详细MES需求_应该通过()
    {
        // 输入：完整的MES报工需求
        var input = @"我们是汽车零部件工厂，需要一个报工管理系统。
            工人完成工序后扫描工单号，输入完成数量和不良品数量。
            车间主任审核报工记录，质检员处理不良品。
            系统需要管理：工单、工序、报工记录、员工、设备。
            字段包括：工单号、工序名称、报工数量、不良品数量、设备编号、操作员工号、报工时间、审核状态。";
        
        var result = await _validator.EvaluateAsync(input, _options, CancellationToken.None);
        
        Assert.True(result.Passed);
        Assert.True(result.Identified.Any(e => e.Category == "业务事件"));
        Assert.True(result.Identified.Any(e => e.Category == "角色"));
        Assert.True(result.Identified.Any(e => e.Category == "数据实体"));
        Assert.True(result.Score >= 60);
    }

    [Fact]
    public async Task Excel含表头_应该识别为字段()
    {
        // 输入：简短文字 + Excel附件（表头：产品编号/名称/规格/单价/库存量/安全库存/供应商）
        var input = "管理我们的产品库存";
        // Mock LLM返回：识别到数据实体"产品"，字段7个
        
        var result = await _validator.EvaluateAsync(input, _options, CancellationToken.None);
        
        Assert.True(result.Passed);
        Assert.True(result.Identified.Any(e => e.Category == "数据实体"));
    }

    // ════════ 红灯（应该不通过） ════════

    [Fact]
    public async Task 仅写管理系统_应该不通过()
    {
        var input = "我要做个管理系统";
        
        var result = await _validator.EvaluateAsync(input, _options, CancellationToken.None);
        
        Assert.False(result.Passed);
        Assert.True(result.Missing.Any(m => m.Category == "业务事件" && m.Severity == "critical"));
        Assert.False(string.IsNullOrEmpty(result.Missing.First(m => m.Category == "业务事件").HowToFix));
    }

    [Fact]
    public async Task 有角色无事件_应该不通过()
    {
        var input = "我想做一个仓库管理系统，仓库管理员负责入库和出库。";
        
        var result = await _validator.EvaluateAsync(input, _options, CancellationToken.None);
        
        // 有角色"仓库管理员"，但可能缺数据实体和字段
        // 至少要有一个critical missing
        Assert.True(result.Missing.Any(m => m.Severity == "critical"));
        Assert.False(string.IsNullOrEmpty(result.BuildGuidance()));
    }

    [Fact]
    public async Task LLM返回_通过但无业务事件_硬阈值覆盖()
    {
        // Mock LLM返回 passed=true, 但identified中没有"业务事件"
        _mockLlm.Setup(x => x.ChatAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse 
            { 
                IsSuccess = true, 
                Content = """{"passed":true,"score":65,"identified":[{"category":"角色","description":"管理员"}],"missing":[]}"""
            });
        
        var result = await _validator.EvaluateAsync("test input", _options, CancellationToken.None);
        
        // 硬阈值覆盖：虽然LLM说通过，但没有业务事件
        Assert.False(result.Passed);
        Assert.True(result.Missing.Any(m => m.Category == "业务事件"));
    }

    // ════════ Fail-Closed 测试 ════════

    [Fact]
    public async Task LLM调用失败_应该FailClosed()
    {
        _mockLlm.Setup(x => x.ChatAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = false, Error = "timeout" });
        
        var result = await _validator.EvaluateAsync("test", _options, CancellationToken.None);
        
        Assert.False(result.Passed);
        Assert.Contains("GATE_LLM_ERR", result.NextStepGuidance);
    }

    [Fact]
    public async Task LLM返回乱码JSON_应该FailClosed()
    {
        _mockLlm.Setup(x => x.ChatAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Content = "这不是JSON" });
        
        var result = await _validator.EvaluateAsync("test", _options, CancellationToken.None);
        
        Assert.False(result.Passed);
        Assert.Contains("GATE_JSON_ERR", result.NextStepGuidance);
    }

    [Fact]
    public async Task LLM返回JSON缺少字段_应该FailClosed()
    {
        _mockLlm.Setup(x => x.ChatAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse { IsSuccess = true, Content = """{"passed":true}"""); // 缺少identified和missing
        
        var result = await _validator.EvaluateAsync("test", _options, CancellationToken.None);
        
        Assert.False(result.Passed);
        Assert.Contains("GATE_SCHEMA_ERR", result.NextStepGuidance);
    }

    [Fact]
    public async Task CancellationToken取消_应该FailClosed()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        var result = await _validator.EvaluateAsync("test", _options, cts.Token);
        
        Assert.False(result.Passed);
        Assert.Contains("GATE_TIMEOUT", result.NextStepGuidance);
    }
}
```

### 7.2 集成测试：GatePipelineIntegrationTests.cs

```csharp
public class GatePipelineIntegrationTests
{
    [Fact]
    public async Task 完整管道_文档加图片_语义评估通过()
    {
        // 3个docx + 2个png + 详细用户文字
        // 验证：附件提取成功 → 合并文本 → 硬规则通过 → 语义评估通过
    }

    [Fact]
    public async Task 完整管道_垃圾输入_硬规则拦截()
    {
        // "test" + 0附件
        // 验证：硬规则阶段就拦截，不调用语义评估
    }

    [Fact]
    public async Task 完整管道_空洞输入_语义拦截()
    {
        // "我要做个管理系统" + 0附件
        // 验证：硬规则通过 → 语义评估不通过 → GateResult.SemanticallyUnfit
        // 验证：SemanticFitness.Missing 包含具体缺失项和 HowToFix
    }

    [Fact]
    public async Task 完整管道_附件损坏_其他附件正常()
    {
        // 2个正常docx + 1个损坏docx + 1个exe（被阻止）
        // 验证：正常附件提取成功，损坏附件记warning，exe被blocked
    }

    [Fact]
    public async Task 完整管道_图片全部失败_警告用户()
    {
        // 用户文字很少 + 3张图片（Vision API失败）
        // 验证：warning包含"全部3张图片处理失败"
    }
}
```

### 7.3 E2E测试：SAGateE2ETests.cs

```csharp
public class SAGateE2ETests
{
    [Fact]
    public async Task 场景1_合格材料_通过门控进入Stage1()
    {
        // POST /api/ai/pipeline/{id}/sa-gate
        // 输入：详细MES需求
        // 验证：202 Accepted → SSE gate_passed → 自动触发Stage 1
    }

    [Fact]
    public async Task 场景2_不合格材料_门控拦截_返回结构化反馈()
    {
        // POST /api/ai/pipeline/{id}/sa-gate
        // 输入："我要做个系统"
        // 验证：SSE gate_failed → response包含 identified[] + missing[] + howToFix
        // 验证：howToFix不为空且内容具体
    }

    [Fact]
    public async Task 场景3_部分合格_门控拦截_保留已识别要素()
    {
        // 输入："管理仓库，管理员入库出库"
        // 验证：identified包含角色"仓库管理员"，missing包含数据实体
    }

    [Fact]
    public async Task 场景4_LLM服务不可用_FailClosed()
    {
        // Mock LLM网关超时
        // 验证：gate_error → 错误代码 GATE_LLM_ERR → 不放行
    }

    [Fact]
    public async Task 场景5_多租户隔离()
    {
        // 租户A提交材料 → 门控结果只能租户A看到
        // 验证：TenantGuard 正确注入 TenantId
    }
}
```

---

## 八、工程师执行检查清单（终版）

### Phase 1：核心文件开发（Day 1-2）

- [ ] 新建 `Gates/SemanticFitnessValidator.cs`（~200行）
  - [ ] Fail-Closed降级策略（所有catch返回FailClosed）
  - [ ] 宽容JSON提取（ExtractJson，去markdown包裹+取首尾大括号）
  - [ ] 反序列化+结构校验（DeserializeAndValidate）
  - [ ] 硬阈值覆盖（PostProcess，通过`with`表达式保持不可变性）
  - [ ] BuildSystemPrompt（注入GatePipelineOptions配置）
- [ ] 修改 `Gates/GateResult.cs`（新增SemanticFitness字段 + SemanticallyUnfit工厂方法）
- [ ] 新增DTO：SemanticFitnessResult / IdentifiedElement / MissingElement / FitnessLevel
- [ ] 修改 `Gates/GatePipelineOptions.cs`（新增SemanticMinScore/MinBusinessEvents/MinRoles/MinDataEntities/MinFieldsPerEntity/SemanticProvider）

### Phase 2：管道集成（Day 3）

- [ ] 修改 `Gates/GatePipeline.cs`
  - [ ] 注入 `SemanticFitnessValidator`
  - [ ] 步骤3合并文本改为带来源标记
  - [ ] 插入步骤4.5语义评估
  - [ ] 步骤2图片全部失败时的特殊处理
- [ ] 修改 `Gates/IGatePipeline.cs`（新增可选 `GateContext?` 参数）

### Phase 3：集成层切换（Day 4）

- [ ] 修改 `AIDevelopmentPipelineService.cs`
  - [ ] 门控改为异步事件驱动（BackgroundTaskRunner + SSE推送）
  - [ ] 前端提交 → 202 Accepted → 后台执行 → SSE推送结果
  - [ ] 门控通过 → 自动触发Stage 1（注入SemanticFitness.Identified作为种子）
  - [ ] 门控结果持久化到 BASE_AI_PIPELINE_MESSAGE

### Phase 4：TDD红绿测试（Day 5-6）

- [ ] 新建 `Tests/Gates/SemanticFitnessValidatorTests.cs`（8个用例）
  - [ ] 绿灯：详细MES需求通过 / Excel含表头通过
  - [ ] 红灯：仅写管理系统不通过 / 有角色无事件不通过
  - [ ] 硬阈值：LLM说通过但无业务事件→覆盖
  - [ ] Fail-Closed：LLM失败 / 乱码JSON / 缺字段JSON / 取消
- [ ] 新建 `Tests/Gates/GatePipelineIntegrationTests.cs`（5个用例）
- [ ] 新建 `Tests/E2E/SAGateE2ETests.cs`（5个用例）

### Phase 5：联调验收（Day 7）

- [ ] 前端对接SSE事件（gate_started/gate_passed/gate_failed/gate_error）
- [ ] 前端SAGateResult.vue对接结构化反馈（identified[]+missing[]+howToFix）
- [ ] 多租户隔离验证
- [ ] 性能验证（附件提取+语义评估 < 2分钟）

### Phase 6：代码审核提交（Day 8）

- [ ] 确认零干扰：只修改 Gates命名空间 + AIDevelopmentPipelineService集成点
- [ ] 确认所有DTO为不可变record + `with`表达式修改
- [ ] 确认所有Fail-Closed降级
- [ ] 确认所有测试通过
- [ ] 提交首席架构师审核

---

## 九、最终文件清单

| 操作     | 文件                                           | 行数       | 说明                            |
| -------- | ---------------------------------------------- | ---------- | ------------------------------- |
| **新建** | `Gates/SemanticFitnessValidator.cs`            | ~200       | 语义合格性校验器（核心）        |
| **修改** | `Gates/GateResult.cs`                          | +60        | 新增SemanticFitness字段+DTO定义 |
| **修改** | `Gates/GatePipelineOptions.cs`                 | +20        | 新增语义评估配置项              |
| **修改** | `Gates/GatePipeline.cs`                        | +25        | 注入校验器+步骤4.5+文本合并改进 |
| **修改** | `Gates/IGatePipeline.cs`                       | +5         | 新增GateContext可选参数         |
| **修改** | `AIDevelopmentPipelineService.cs`              | +30        | 异步事件驱动+Stage1衔接         |
| **新建** | `Tests/Gates/SemanticFitnessValidatorTests.cs` | ~150       | 8个红绿测试                     |
| **新建** | `Tests/Gates/GatePipelineIntegrationTests.cs`  | ~100       | 5个集成测试                     |
| **新建** | `Tests/E2E/SAGateE2ETests.cs`                  | ~120       | 5个E2E测试                      |
| **总计** |                                                | **~710行** | 1个新文件+5处修改+3个测试文件   |

**D爷指出的4个致命缺陷全部修正。工程师按此清单执行，SA门控可以作为企业级"铁闸"上线。**