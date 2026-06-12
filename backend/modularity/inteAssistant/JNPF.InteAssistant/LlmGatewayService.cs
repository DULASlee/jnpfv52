using System.Diagnostics;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Interfaces;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// LLM 网关服务
/// 统一 LLM 调用入口，封装 provider 切换，每次调用写入 BASE_AI_CALL_LOG
/// </summary>
public class LlmGatewayService : ILlmGatewayService, ITransient
{
    /// <summary>
    /// AI 调用日志仓储
    /// </summary>
    private readonly ISqlSugarRepository<AiCallLogEntity> _logRepository;

    /// <summary>
    /// 初始化一个<see cref="LlmGatewayService"/>类型的新实例
    /// </summary>
    public LlmGatewayService(ISqlSugarRepository<AiCallLogEntity> logRepository)
    {
        _logRepository = logRepository;
    }

    /// <inheritdoc/>
    public async Task<string> ChatAsync(string prompt, string model = null)
    {
        model ??= "gpt-4o";
        var sw = Stopwatch.StartNew();

        // TODO: Phase 2 对接真实 LLM SDK
        // 当前为地桩实现，返回占位响应
        var responseText = $"[STUB] LLM response for prompt: {prompt[..Math.Min(prompt.Length, 100)]}";

        sw.Stop();

        // 写入调用日志
        await WriteCallLog(model, prompt, responseText, sw.ElapsedMilliseconds, 200);

        return responseText;
    }

    /// <inheritdoc/>
    public async Task<ProviderHealth> HealthCheckAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // TODO: Phase 2 对接真实 endpoint
            // 当前地桩：返回健康
            await Task.CompletedTask;
            sw.Stop();

            var health = new ProviderHealth
            {
                IsHealthy = true,
                Provider = "stub",
                LatencyMs = sw.ElapsedMilliseconds,
                Error = null
            };

            // 写入健康检查日志
            await WriteCallLog("health-check", "{}", health.IsHealthy.ToString(), sw.ElapsedMilliseconds, 200);

            return health;
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ProviderHealth
            {
                IsHealthy = false,
                Provider = "stub",
                LatencyMs = sw.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 写入 AI 调用日志
    /// </summary>
    private async Task WriteCallLog(string model, string requestBody, string responseBody, long latencyMs, int statusCode)
    {
        try
        {
            var log = new AiCallLogEntity
            {
                Model = model,
                RequestBody = requestBody,
                ResponseBody = responseBody,
                LatencyMs = latencyMs,
                StatusCode = statusCode,
            };
            log.Create();

            await _logRepository.AsInsertable(log).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        }
        catch
        {
            // 日志写入失败不应影响主流程
        }
    }
}
