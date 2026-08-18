using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Agents;

/// <summary>
/// API 接口设计 SubAgent
/// 职责: 根据功能模块和数据库设计，定义 RESTful API 接口契约
/// </summary>
public class ApiDesignAgent : ISubAgent, ITransient
{
    private readonly ILlmGatewayService _gateway;
    private readonly IKnowledgeIntegration _knowledge;
    private readonly ILogger<ApiDesignAgent> _logger;

    public string AgentName => "api_design";
    public string DisplayName => "API接口设计";

    public ApiDesignAgent(
        ILlmGatewayService gateway,
        IKnowledgeIntegration knowledge,
        ILogger<ApiDesignAgent> logger)
    {
        _gateway = gateway;
        _knowledge = knowledge;
        _logger = logger;
    }

    public async Task<SubAgentResult> ExecuteAsync(
        DetailedDesignContext context,
        IReadOnlyDictionary<string, SubAgentResult> previousResults,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var knowledge = await _knowledge.RetrieveRelevantAsync(
            $"API接口 {context.ProjectName}".Truncate(200),
            new List<string> { "api", "endpoint", "接口" },
            5, ct);

        var dbInfo = previousResults.GetValueOrDefault("database")?.Content ?? "";
        var moduleInfo = previousResults.GetValueOrDefault("functional_module")?.Content ?? "";

        var systemPrompt = """
你是一个 API 设计师 (JNPF DynamicApi 风格)。请设计 RESTful API 接口。

输出 JSON 格式:
{
  "services": [
    {
      "serviceName": "XxxService",
      "route": "/api/xxx",
      "description": "服务说明",
      "endpoints": [
        {
          "method": "GET|POST|PUT|DELETE",
          "path": "/api/xxx/action",
          "summary": "接口说明",
          "requestBody": { "type": "object", "properties": {} },
          "responseBody": { "type": "object", "properties": {} },
          "auth": "JWT",
          "tenantFilter": true
        }
      ]
    }
  ],
  "globalConventions": {
    "responseWrapper": "RESTfulResult<T>",
    "errorHandling": "Oops.Bah() 业务 / Oops.Oh() 系统",
    "jwtExpiry": "600"
  }
}
""";

        var userPrompt = $"""
## 项目名称
{context.ProjectName}

## 功能模块设计
{moduleInfo.Truncate(2000)}

## 数据库设计
{dbInfo.Truncate(2000)}

## 知识库参考
{string.Join("\n", knowledge.Select(k => $"- {k.Content}".Truncate(300)))}

请设计 API 接口。注意: 使用 IDynamicApiController + RESTfulResult 风格，禁止手工 Controller。
""";

        try
        {
            var response = await _gateway.ChatAsync(new ChatCompletionRequest
            {
                Messages = [new ChatMessage("system", systemPrompt), new ChatMessage("user", userPrompt)],
                Temperature = 0.2,
                MaxTokens = 4096,
                ResponseFormat = "json"
            }, ct);

            sw.Stop();
            return new SubAgentResult
            {
                AgentName = AgentName,
                IsSuccess = response.IsSuccess,
                Content = response.Content,
                DocumentTitle = "API接口设计",
                TokensUsed = response.TokensIn + response.TokensOut,
                LatencyMs = response.LatencyMs,
                Error = response.Error
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[{Agent}] 执行失败", AgentName);
            return new SubAgentResult { AgentName = AgentName, IsSuccess = false, Error = ex.Message };
        }
    }
}
