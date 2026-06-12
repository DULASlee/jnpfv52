using JNPF.InteAssistant.Entitys.Dto.InteAssistant;

namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// LLM 网关服务接口
/// 统一 LLM 调用入口，封装 provider 切换
/// </summary>
public interface ILlmGatewayService
{
    /// <summary>
    /// 执行聊天补全
    /// </summary>
    /// <param name="prompt">用户 prompt</param>
    /// <param name="model">模型名称（可选，默认使用配置的默认模型）</param>
    /// <returns>LLM 响应文本</returns>
    Task<string> ChatAsync(string prompt, string model = null);

    /// <summary>
    /// 健康检查
    /// </summary>
    /// <returns>Provider 连通性结果</returns>
    Task<ProviderHealth> HealthCheckAsync();
}
