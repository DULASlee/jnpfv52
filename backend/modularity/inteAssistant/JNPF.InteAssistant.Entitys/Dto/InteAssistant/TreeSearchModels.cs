namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// Tree-of-Thought 多路候选生成请求（施工包 21 §3.5）。
/// 同一 prompt 按温度梯度并行发 N 路，每路独立审计入 BASE_AI_CALL_LOG。
/// 网关只负责生成候选，不做裁决——打分/剪枝由技能用 MCP 工具完成。
/// </summary>
public record TreeSearchRequest
{
    /// <summary>供应商代码，空则用默认 Provider。</summary>
    public string ProviderCode { get; init; } = "";

    public string? ModelCode { get; init; }

    public string? SystemPrompt { get; init; }

    public List<ChatMessage> Messages { get; init; } = new();

    /// <summary>并行分支数（2-6，越界自动收敛）。</summary>
    public int BranchCount { get; init; } = 3;

    /// <summary>首分支温度，后续分支按 TemperatureStep 递增。</summary>
    public double BaseTemperature { get; init; } = 0.3;

    public double TemperatureStep { get; init; } = 0.35;

    /// <summary>响应格式 ("text", "json")。</summary>
    public string? ResponseFormat { get; init; }

    public int MaxTokens { get; init; } = 4096;

    public int TimeoutMs { get; init; } = 120000;
}

/// <summary>单路候选结果。</summary>
public record TreeSearchCandidate
{
    public int BranchIndex { get; init; }

    public double Temperature { get; init; }

    public bool IsSuccess { get; init; }

    public string Content { get; init; } = "";

    public string ModelUsed { get; init; } = "";

    public int TokensIn { get; init; }

    public int TokensOut { get; init; }

    public int LatencyMs { get; init; }

    public string? Error { get; init; }
}

/// <summary>
/// ToT 生成结果。IsSuccess=false 表示全部分支失败——
/// 调用方必须以失败态处理，禁止降级编造内容（红线 RL-1）。
/// </summary>
public record TreeSearchResult
{
    public bool IsSuccess { get; init; }

    public List<TreeSearchCandidate> Candidates { get; init; } = new();

    /// <summary>全败时的聚合错误。</summary>
    public string? Error { get; init; }

    /// <summary>成功候选（快捷访问）。</summary>
    public IEnumerable<TreeSearchCandidate> Succeeded => Candidates.Where(c => c.IsSuccess);
}
