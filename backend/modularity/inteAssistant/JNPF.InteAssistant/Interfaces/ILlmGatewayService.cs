using JNPF.InteAssistant.Entitys.Dto.InteAssistant;

namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// LLM 网关服务接口
/// 统一 LLM 调用入口，封装 provider 切换，每次调用写入 BASE_AI_CALL_LOG
/// 对齐前端 src/core/ai/llm/types.ts LLMGateway
/// </summary>
public interface ILlmGatewayService
{
    /// <summary>
    /// 执行聊天补全（旧接口，保留向后兼容）
    /// </summary>
    [Obsolete("Use ChatAsync(ChatCompletionRequest, CancellationToken) instead.")]
    Task<string> ChatAsync(string prompt, string? model = null);

    /// <summary>
    /// 健康检查（旧接口，保留向后兼容）
    /// </summary>
    [Obsolete("Use HealthCheckAsync(string, CancellationToken) instead.")]
    Task<ProviderHealth> HealthCheckAsync();

    // ─── 新接口（对齐前端 LLMGateway）───

    /// <summary>
    /// 执行聊天补全（完整参数）
    /// </summary>
    /// <param name="request">聊天请求</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>聊天响应</returns>
    Task<ChatCompletionResponse> ChatAsync(ChatCompletionRequest request, CancellationToken ct = default);

    /// <summary>
    /// 执行聊天补全（流式输出）
    /// 返回 SSE 流，每条 yield 增量文本
    /// </summary>
    /// <param name="request">聊天请求</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>流式文本块</returns>
    IAsyncEnumerable<string> ChatStreamAsync(ChatCompletionRequest request, CancellationToken ct = default);

    /// <summary>
    /// 健康检查（指定 Provider）
    /// </summary>
    /// <param name="providerCode">Provider 代码</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>true=可用</returns>
    Task<bool> HealthCheckAsync(string providerCode, CancellationToken ct = default);

    /// <summary>
    /// 获取 Provider 信息
    /// </summary>
    /// <param name="providerCode">Provider 代码</param>
    /// <returns>Provider 信息</returns>
    Task<ProviderInfo> GetProviderInfoAsync(string providerCode);

    /// <summary>
    /// 27 号 §7.2：按任务类型解析超时（毫秒）。
    /// 读取 AI:TimeoutMs:{skillId} 配置，无匹配回退 AI:TimeoutMs:Default（默认 60_000）。
    /// </summary>
    /// <param name="skillId">Skill 标识</param>
    /// <returns>超时毫秒数</returns>
    int ResolveTimeoutMs(string skillId);

    /// <summary>
    /// 27 号 §7.3：按任务（skillId）路由 Provider。
    /// 读取 AI:ProviderRouting 配置表，将不同 Skill 路由到不同 Provider（如 PM→deepseek，门控→mimo）。
    /// 无匹配项时回退默认 Provider。
    /// </summary>
    /// <param name="skillId">Skill 标识（如 "pm-skill"、"analyst-skill"）</param>
    /// <returns>Provider 代码</returns>
    string ResolveProvider(string skillId);

    /// <summary>
    /// Tree-of-Thought 多路候选生成（施工包 21 §3.5）：
    /// 同一 prompt 按温度梯度并行发 N 路 ChatAsync，每路独立审计入 BASE_AI_CALL_LOG。
    /// 只生成候选不做裁决；全部分支失败时 IsSuccess=false，禁止降级编造内容。
    /// </summary>
    /// <param name="request">ToT 请求（分支数、温度梯度）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>候选集合</returns>
    Task<TreeSearchResult> TreeSearchAsync(TreeSearchRequest request, CancellationToken ct = default);
}
