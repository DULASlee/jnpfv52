// 文件：Gates/GatePipeline.cs
// 命名空间：JNPF.InteAssistant.Gates
// 职责：需求门控管道（业务逻辑）

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
/// 依赖：架构组件通过接口注入（IBackgroundTaskRunner、ITenantGuard）
///       业务组件直接注入（AttachmentProcessor、RequirementGateService）
/// </summary>
public sealed class GatePipeline : IGatePipeline, ITransient
{
    private readonly AttachmentProcessor _attachmentProcessor;
    private readonly RequirementGateService _gateService;
    private readonly ITenantGuard _tenantGuard;
    private readonly IOptionsMonitor<GatePipelineOptions> _optionsMonitor;
    private readonly ILogger<GatePipeline> _logger;

    public GatePipeline(
        AttachmentProcessor attachmentProcessor,
        RequirementGateService gateService,
        ITenantGuard tenantGuard,
        IOptionsMonitor<GatePipelineOptions> optionsMonitor,
        ILogger<GatePipeline> logger)
    {
        _attachmentProcessor = attachmentProcessor;
        _gateService = gateService;
        _tenantGuard = tenantGuard;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task<GateResult> ExecuteAsync(
        string userText,
        List<AttachmentFile> attachments,
        RequestContext ctx,
        string visionApiUrl = "",
        string visionApiKey = "",
        string visionModel = "",
        CancellationToken ct = default)
    {
        var options = _optionsMonitor.CurrentValue;
        var sw = Stopwatch.StartNew();
        var warnings = new List<string>();

        // 步骤0：验证附件
        var (validAttachments, blockedCount, validationWarnings) = ValidateAttachments(attachments, options);
        warnings.AddRange(validationWarnings);

        if (validAttachments.Count > options.MaxAttachmentCount)
            return GateResult.Fail($"附件数量超限（最多{options.MaxAttachmentCount}个）", warnings);

        // 步骤1：并行提取附件文本
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

        // 步骤2：图片多模态提取
        var imageFiles = validAttachments
            .Where(f => GateConstants.IsImageFile(f.FileName))
            .Where(f => !processingErrors.Contains(f.FileName))
            .ToList();

        if (imageFiles.Count > 0 && !string.IsNullOrWhiteSpace(visionApiKey))
        {
            try
            {
                var imageAnalysis = await _gateService.ExtractFromImages(imageFiles, visionApiUrl, visionApiKey, visionModel, ct);
                attachmentText += "\n\n" + imageAnalysis;
            }
            catch (OutOfMemoryException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "图片多模态分析失败");
                warnings.Add("图片多模态分析失败，已跳过");
            }
        }

        // 步骤3：合并文本
        var fullText = userText + attachmentText;

        // 步骤4：硬规则校验
        var actualAttachmentCount = validAttachments.Count - blockedCount;
        var hardRule = _gateService.ValidateHardRules(fullText, actualAttachmentCount);
        if (!hardRule.Passed)
            return GateResult.Fail(hardRule.Reason, warnings, hardRule.Hint);

        // 步骤5：输出
        sw.Stop();
        _logger.LogInformation("门控通过: 文字{TextLen}字, 附件{AttCount}个, 跳过{BlockedCount}个, 耗时{Ms}ms",
            userText.Length, actualAttachmentCount, blockedCount, sw.ElapsedMilliseconds);

        return new GateResult
        {
            Passed = true,
            MergedText = fullText,
            AttachmentText = attachmentText,
            AttachmentCount = actualAttachmentCount,
            BlockedCount = blockedCount,
            Warnings = warnings
        };
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
