using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Agents;

/// <summary>
/// UI 界面设计 SubAgent
/// 职责: 根据功能模块和业务流程，设计前端页面布局和交互
/// </summary>
public class UIDesignAgent : ISubAgent, ITransient
{
    private readonly ILlmGatewayService _gateway;
    private readonly IKnowledgeIntegration _knowledge;
    private readonly ILogger<UIDesignAgent> _logger;

    public string AgentName => "ui_design";
    public string DisplayName => "UI界面设计";

    public UIDesignAgent(
        ILlmGatewayService gateway,
        IKnowledgeIntegration knowledge,
        ILogger<UIDesignAgent> logger)
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
            $"UI界面 {context.ProjectName}".Truncate(200),
            new List<string> { "ui", "page", "component", "界面" },
            5, ct);

        var moduleInfo = previousResults.GetValueOrDefault("functional_module")?.Content ?? "";

        var systemPrompt = """
你是一个 UI/UX 设计师。请设计系统的前端页面。

输出 JSON 格式:
{
  "pages": [
    {
      "path": "/views/module/PageName",
      "title": "页面标题",
      "type": "list|form|detail|dashboard",
      "components": ["BasicTable", "BasicForm", "BasicModal"],
      "layout": "布局描述",
      "dataSource": "数据来源 API",
      "interactions": [
        { "trigger": "点击新建", "action": "打开弹窗", "target": "BasicModal" }
      ]
    }
  ],
  "navigation": {
    "menuStructure": "菜单结构",
    "breadcrumbs": "面包屑"
  },
  "styleGuide": "Vue3 + Ant Design Vue + Less + WindiCSS"
}
""";

        var userPrompt = $"""
## 项目名称
{context.ProjectName}

## 需求
{context.Requirements}

## 功能模块设计
{moduleInfo.Truncate(2000)}

## 知识库参考
{string.Join("\n", knowledge.Select(k => $"- {k.Content}".Truncate(300)))}

请设计 UI 界面。注意使用 JNPF 标准组件 (BasicTable/BasicForm/BasicModal/jnpf-content-wrapper)。
""";

        try
        {
            var response = await _gateway.ChatAsync(new ChatCompletionRequest
            {
                Messages = [new ChatMessage("system", systemPrompt), new ChatMessage("user", userPrompt)],
                Temperature = 0.4,
                MaxTokens = 4096,
                ResponseFormat = "json"
            }, ct);

            sw.Stop();
            return new SubAgentResult
            {
                AgentName = AgentName,
                IsSuccess = response.IsSuccess,
                Content = response.Content,
                DocumentTitle = "UI界面设计",
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
