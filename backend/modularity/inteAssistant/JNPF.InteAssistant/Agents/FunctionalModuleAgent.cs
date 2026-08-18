using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JNPF.InteAssistant.Agents;

/// <summary>
/// 功能模块设计 SubAgent
/// 职责: 根据需求分析产出，设计系统的功能模块划分
/// </summary>
public class FunctionalModuleAgent : ISubAgent, ITransient
{
    private readonly ILlmGatewayService _gateway;
    private readonly IKnowledgeIntegration _knowledge;
    private readonly ILogger<FunctionalModuleAgent> _logger;

    public string AgentName => "functional_module";
    public string DisplayName => "功能模块设计";

    public FunctionalModuleAgent(
        ILlmGatewayService gateway,
        IKnowledgeIntegration knowledge,
        ILogger<FunctionalModuleAgent> logger)
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

        // 检索相关知识
        var knowledge = await _knowledge.RetrieveRelevantAsync(
            $"功能模块 {context.ProjectName} {context.Requirements}".Truncate(200),
            new List<string> { "module", "功能模块" },
            5, ct);

        var systemPrompt = """
你是一个资深系统架构师，擅长模块化设计。
请根据需求分析，设计系统的功能模块划分。

输出 JSON 格式:
{
  "modules": [
    {
      "name": "模块名",
      "label": "中文名",
      "description": "模块职责",
      "entities": ["实体列表"],
      "pages": ["页面列表"],
      "dependencies": ["依赖模块"]
    }
  ],
  "moduleRelationships": "模块关系说明",
  "designRationale": "设计理由"
}
""";

        var userPrompt = $"""
## 项目名称
{context.ProjectName}

## 需求
{context.Requirements}

## 架构设计
{context.Architecture}

## 相关知识库参考
{string.Join("\n", knowledge.Select(k => $"- [{k.NodeType}] {k.Content}".Truncate(300)))}

请设计功能模块划分。
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
                DocumentTitle = "功能模块设计",
                TokensUsed = response.TokensIn + response.TokensOut,
                LatencyMs = response.LatencyMs,
                Error = response.Error
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[{Agent}] 执行失败", AgentName);
            return new SubAgentResult
            {
                AgentName = AgentName,
                IsSuccess = false,
                Error = ex.Message,
                LatencyMs = (int)sw.ElapsedMilliseconds
            };
        }
    }
}

/// <summary>
/// 字符串截断扩展
/// </summary>
internal static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
