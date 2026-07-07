// 文件：Gates/GatePipeline.cs
// 命名空间：JNPF.InteAssistant.Gates
// 职责：需求门控管道（业务逻辑）— 含语义合格性校验（步骤4.5）

using JNPF.DependencyInjection;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 需求门控管道
///
/// 执行步骤：
///   0. 验证附件（格式/大小/数量）
///   1. 并行提取附件文本
///   2. 图片多模态提取
///   3. 合并文本（带来源标记）
///   4. 硬规则校验（纯格式检查）
///   4.5 语义合格性评估（★ 新增 — LLM 判断信息完整度）
///   5. 输出 GateResult
///
/// 修复的 4 个致命缺陷：
///   缺陷1 (Fail-Open)：SemanticFitnessValidator 统一 Fail-Closed
///   缺陷2 (JSON裸奔)：SemanticFitnessValidator.ExtractJson 三重防护
///   缺陷3 (Record写操作)：SemanticFitnessValidator.PostProcess 使用 with 表达式
///   缺陷4 (同步阻塞)：由 AIDevelopmentPipelineService 通过 BackgroundTaskRunner 异步化
/// </summary>
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
        object? gateContext = null,       // ★ 新增：可选上下文（扩展点，当前未使用）
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
                string text;
                if (!string.IsNullOrWhiteSpace(file.PreExtractedText))
                {
                    text = file.PreExtractedText;
                }
                else
                {
                    text = await _attachmentProcessor.ProcessAttachmentsAsync(new List<AttachmentFile> { file });
                }

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

        // ═══════════ 步骤4.5：语义合格性评估（★ 核心新增 — 缺陷1/2/3修复） ═══════════
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
            SemanticFitness = semanticResult  // ★ 携带语义评估结果，供 Stage 1 使用
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

    private static (List<AttachmentFile> valid, int blockedCount, List<string> warnings) ValidateAttachments(
        List<AttachmentFile> attachments, GatePipelineOptions options)
    {
        var valid = new List<AttachmentFile>();
        var warnings = new List<string>();
        int blockedCount = 0;
        long totalSize = 0;

        foreach (var file in attachments)
        {
            var ext = Path.GetExtension(file.FileName);

            if (options.BlockedExtensions.Contains(ext ?? "", StringComparer.OrdinalIgnoreCase))
            {
                blockedCount++;
                warnings.Add($"文件 {file.FileName} 格式不允许（{ext}）");
                continue;
            }

            if (!options.AllowedExtensions.Contains(ext ?? "", StringComparer.OrdinalIgnoreCase))
            {
                blockedCount++;
                warnings.Add($"文件 {file.FileName} 格式不在支持列表（{ext}）");
                continue;
            }

            if (file.Content.Length > options.MaxFileSizeBytes)
            {
                blockedCount++;
                warnings.Add($"文件 {file.FileName} 太大（{FormatSize(file.Content.Length)}）");
                continue;
            }

            if (totalSize + file.Content.Length > options.MaxTotalSizeBytes)
            {
                blockedCount++;
                warnings.Add("附件总大小超限");
                continue;
            }

            totalSize += file.Content.Length;
            valid.Add(file);
        }

        return (valid, blockedCount, warnings);
    }

    private static string FormatSize(long bytes) =>
        bytes >= 1024 * 1024
            ? $"{bytes / (1024.0 * 1024.0):F1}MB"
            : $"{bytes / 1024.0:F1}KB";
}
