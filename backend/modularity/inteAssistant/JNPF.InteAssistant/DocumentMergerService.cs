using JNPF.DependencyInjection;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace JNPF.InteAssistant;

/// <summary>
/// 文档合并器
/// 将 6 个 SubAgent 的并行产出合并为完整的详细设计说明书
/// </summary>
public class DocumentMergerService : IDocumentMerger, ITransient
{
    private readonly ILogger<DocumentMergerService> _logger;

    /// <summary>
    /// 各 Agent 的章节顺序（决定合并后的章节排列）
    /// </summary>
    private static readonly Dictionary<string, int> ChapterOrder = new()
    {
        ["functional_module"] = 1,
        ["business_process"] = 2,
        ["database"] = 3,
        ["ui_design"] = 4,
        ["permission"] = 5,
        ["api_design"] = 6,
    };

    public DocumentMergerService(ILogger<DocumentMergerService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<MergedDocument> MergeAsync(
        string projectName,
        IReadOnlyDictionary<string, SubAgentResult> subAgentResults,
        CancellationToken ct = default)
    {
        var chapters = new Dictionary<string, string>();
        var fullContent = new StringBuilder();

        // 文档头
        fullContent.AppendLine($"# 《{projectName} 系统详细设计说明书》");
        fullContent.AppendLine();
        fullContent.AppendLine($"> 由 AI 详细设计编排器自动生成");
        fullContent.AppendLine($"> 生成时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        fullContent.AppendLine($"> Agent 数量: {subAgentResults.Count}");
        fullContent.AppendLine();

        // 按章节顺序排列
        var ordered = subAgentResults
            .Select(kvp => new
            {
                Order = ChapterOrder.GetValueOrDefault(kvp.Key, 99),
                Name = kvp.Key,
                Result = kvp.Value
            })
            .OrderBy(x => x.Order);

        int chapterNum = 1;
        foreach (var item in ordered)
        {
            if (!item.Result.IsSuccess)
                continue;

            var chapterTitle = GetChapterTitle(item.Name);
            var chapterContent = item.Result.Content;

            chapters[chapterTitle] = chapterContent;
            fullContent.AppendLine($"## 第{chapterNum}章 · {chapterTitle}");
            fullContent.AppendLine();
            fullContent.AppendLine(chapterContent);
            fullContent.AppendLine();

            chapterNum++;
        }

        // 文档尾
        fullContent.AppendLine("---");
        fullContent.AppendLine($"**本文档由 JNPF-AI 详细设计编排器自动生成**");
        fullContent.AppendLine($"**Agent 执行统计**: {subAgentResults.Count(r => r.Value.IsSuccess)}/{subAgentResults.Count} 成功");

        _logger.LogInformation("文档合并完成: {Project}, {Chapters} 章节",
            projectName, chapters.Count);

        var storagePath = Path.Combine(
            Path.GetTempPath(),
            $"jnpf-design-{projectName}-{DateTime.UtcNow:yyyyMMddHHmmss}.md");

        // 异步写入磁盘（非阻塞）
        _ = File.WriteAllTextAsync(storagePath, fullContent.ToString(), ct);

        return Task.FromResult(new MergedDocument
        {
            Title = $"《{projectName} 系统详细设计说明书》",
            FullContent = fullContent.ToString(),
            Chapters = chapters,
            StoragePath = storagePath
        });
    }

    private static string GetChapterTitle(string agentName) => agentName switch
    {
        "functional_module" => "功能模块设计",
        "business_process" => "业务流程设计",
        "database" => "数据库设计",
        "ui_design" => "UI界面设计",
        "permission" => "权限管理设计",
        "api_design" => "API接口设计",
        _ => agentName
    };
}
