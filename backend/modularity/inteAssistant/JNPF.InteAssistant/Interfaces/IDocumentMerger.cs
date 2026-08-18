namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// 文档合并器
/// 将 6 个 SubAgent 的并行产出合并为完整的详细设计说明书
/// </summary>
public interface IDocumentMerger
{
    /// <summary>
    /// 合并多个 Agent 产出为一份完整文档
    /// </summary>
    /// <param name="projectName">项目名称</param>
    /// <param name="subAgentResults">各 Agent 的执行结果</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>合并后的文档</returns>
    Task<MergedDocument> MergeAsync(
        string projectName,
        IReadOnlyDictionary<string, SubAgentResult> subAgentResults,
        CancellationToken ct = default);
}

/// <summary>
/// 合并后的文档
/// </summary>
public record MergedDocument
{
    /// <summary>
    /// 完整 Markdown 内容
    /// </summary>
    public string FullContent { get; init; } = "";

    /// <summary>
    /// 按章节分割的内容（章节名 → 内容）
    /// </summary>
    public Dictionary<string, string> Chapters { get; init; } = new();

    /// <summary>
    /// 文档存储路径
    /// </summary>
    public string StoragePath { get; init; } = "";

    /// <summary>
    /// 文档标题
    /// </summary>
    public string Title { get; init; } = "";
}
