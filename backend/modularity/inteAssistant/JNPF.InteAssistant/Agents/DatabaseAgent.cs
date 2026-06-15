using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Agents;

/// <summary>
/// 数据库设计 SubAgent
/// 职责: 根据功能模块设计，产出数据库表结构设计 (DDL)
/// </summary>
public class DatabaseAgent : ISubAgent, ITransient
{
    private readonly ILlmGatewayService _gateway;
    private readonly IKnowledgeIntegration _knowledge;
    private readonly ILogger<DatabaseAgent> _logger;

    public string AgentName => "database";
    public string DisplayName => "数据库设计";

    public DatabaseAgent(
        ILlmGatewayService gateway,
        IKnowledgeIntegration knowledge,
        ILogger<DatabaseAgent> logger)
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
            $"数据库设计 表结构 {context.ProjectName}".Truncate(200),
            new List<string> { "database", "entity", "表" },
            5, ct);

        // 获取批次1中功能模块和业务流程的产出
        var moduleInfo = previousResults.GetValueOrDefault("functional_module")?.Content ?? "";
        var processInfo = previousResults.GetValueOrDefault("business_process")?.Content ?? "";

        var systemPrompt = """
你是一个数据库架构师。请根据功能模块和业务流程设计，产出完整的数据库表结构。

输出 JSON 格式:
{
  "tables": [
    {
      "tableName": "BASE_XXX",
      "description": "表说明",
      "columns": [
        {
          "name": "F_XXX",
          "type": "NVARCHAR(50)",
          "nullable": false,
          "description": "字段说明",
          "isPrimaryKey": false,
          "isForeignKey": false,
          "isTenantId": false
        }
      ],
      "indexes": [
        { "name": "IDX_XXX", "columns": ["F_XXX"], "unique": false }
      ]
    }
  ],
  "relationships": [
    { "from": "TABLE_A.F_ID", "to": "TABLE_B.F_REF_ID", "type": "1:N" }
  ],
  "namingConventions": "F_前缀，大写下划线，租户ID必含"
}
""";

        var userPrompt = $"""
## 项目名称
{context.ProjectName}

## 需求
{context.Requirements}

## 功能模块设计
{moduleInfo.Truncate(2000)}

## 业务流程设计
{processInfo.Truncate(2000)}

## 知识库参考
{string.Join("\n", knowledge.Select(k => $"- {k.Content}".Truncate(300)))}

请设计数据库表结构。注意：所有业务表必须包含 F_TENANT_ID 字段。
""";

        try
        {
            var response = await _gateway.ChatAsync(new ChatCompletionRequest
            {
                Messages = [new ChatMessage("system", systemPrompt), new ChatMessage("user", userPrompt)],
                Temperature = 0.2,
                MaxTokens = 8192,
                ResponseFormat = "json"
            }, ct);

            sw.Stop();
            return new SubAgentResult
            {
                AgentName = AgentName,
                IsSuccess = response.IsSuccess,
                Content = response.Content,
                DocumentTitle = "数据库设计",
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
