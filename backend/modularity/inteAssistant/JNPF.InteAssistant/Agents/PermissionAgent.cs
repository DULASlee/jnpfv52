using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Agents;

/// <summary>
/// 权限管理设计 SubAgent
/// 职责: 根据功能模块和 UI 设计，规划角色权限矩阵
/// </summary>
public class PermissionAgent : ISubAgent, ITransient
{
    private readonly ILlmGatewayService _gateway;
    private readonly IKnowledgeIntegration _knowledge;
    private readonly ILogger<PermissionAgent> _logger;

    public string AgentName => "permission";
    public string DisplayName => "权限管理设计";

    public PermissionAgent(
        ILlmGatewayService gateway,
        IKnowledgeIntegration knowledge,
        ILogger<PermissionAgent> logger)
    {
        _gateway = gateway;
        _knowledge = knowledge;
        _logger = logger;
    }

    public async Task<SubAgentResult> ExecuteAsync(
        DetailedDesignContext context,
        Dictionary<string, SubAgentResult> previousResults,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var knowledge = await _knowledge.RetrieveRelevantAsync(
            $"权限管理 角色 {context.ProjectName}".Truncate(200),
            new List<string> { "permission", "role", "权限" },
            5, ct);

        var moduleInfo = previousResults.GetValueOrDefault("functional_module")?.Content ?? "";
        var uiInfo = previousResults.GetValueOrDefault("ui_design")?.Content ?? "";

        var systemPrompt = """
你是一个安全架构师。请设计系统的角色权限矩阵。

输出 JSON 格式:
{
  "roles": [
    {
      "name": "角色名",
      "code": "ROLE_CODE",
      "description": "角色描述",
      "permissions": [
        {
          "resource": "/api/xxx",
          "actions": ["GET", "POST"],
          "dataScope": "tenant|own|all"
        }
      ]
    }
  ],
  "permissionMatrix": {
    "说明": "角色 × 资源的权限矩阵"
  },
  "tenantIsolation": "多租户隔离策略"
}
""";

        var userPrompt = $"""
## 项目名称
{context.ProjectName}

## 功能模块设计
{moduleInfo.Truncate(2000)}

## UI设计
{uiInfo.Truncate(1000)}

## 知识库参考
{string.Join("\n", knowledge.Select(k => $"- {k.Content}".Truncate(300)))}

请设计权限管理方案。所有接口需考虑多租户隔离。
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
                DocumentTitle = "权限管理设计",
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
