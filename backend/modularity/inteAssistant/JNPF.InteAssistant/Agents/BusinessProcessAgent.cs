using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Agents;

/// <summary>
/// 业务流程设计 SubAgent
/// 职责: 根据功能模块设计，规划系统的端到端业务流程
/// </summary>
public class BusinessProcessAgent : ISubAgent, ITransient
{
    private readonly ILlmGatewayService _gateway;
    private readonly IKnowledgeIntegration _knowledge;
    private readonly ILogger<BusinessProcessAgent> _logger;

    public string AgentName => "business_process";
    public string DisplayName => "业务流程设计";

    public BusinessProcessAgent(
        ILlmGatewayService gateway,
        IKnowledgeIntegration knowledge,
        ILogger<BusinessProcessAgent> logger)
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
            $"业务流程 {context.ProjectName}".Truncate(200),
            new List<string> { "workflow", "流程" },
            5, ct);

        var systemPrompt = """
你是一个业务流程分析师。请设计系统的端到端业务流程。

输出 JSON 格式:
{
  "processes": [
    {
      "name": "流程名",
      "description": "流程描述",
      "steps": ["步骤1", "步骤2"],
      "roles": ["参与角色"],
      "conditions": "触发条件",
      "exceptions": "异常处理"
    }
  ],
  "stateMachine": "状态机设计",
  "integrationPoints": "集成点"
}
""";

        var userPrompt = $"""
## 项目名称
{context.ProjectName}

## 需求
{context.Requirements}

## 知识库参考
{string.Join("\n", knowledge.Select(k => $"- {k.Content}".Truncate(300)))}

请设计端到端业务流程。
""";

        try
        {
            var response = await _gateway.ChatAsync(new ChatCompletionRequest
            {
                Messages = [new ChatMessage("system", systemPrompt), new ChatMessage("user", userPrompt)],
                Temperature = 0.3,
                MaxTokens = 4096,
                ResponseFormat = "json"
            }, ct);

            sw.Stop();
            return new SubAgentResult
            {
                AgentName = AgentName,
                IsSuccess = response.IsSuccess,
                Content = response.Content,
                DocumentTitle = "业务流程设计",
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
