using System.Text.Json;
using JNPF.InteAssistant.Skills;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// P0 业务闭环：EventSpec 真实组装 + 需求说明书 Markdown 生成。
/// </summary>
public static class BusinessP0Tests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static void RunAll()
    {
        T1_EventSpecAssembler_ExtractsFieldsFromDictStep();
        T2_EventSpecAssembler_ExtractsRulesFromDecisionTable();
        T3_RequirementSpecDocument_ContainsEventSections();
        T4_RequirementTextHelper_TruncatesLongInput();
        T5_DesignDeliverableFormatter_ArchitectureMarkdown();
        T5b_DesignDeliverableFormatter_StringModules();
    }

    private static void T1_EventSpecAssembler_ExtractsFieldsFromDictStep()
    {
        var steps = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["CommandQuery"] = JsonSerializer.Deserialize<object>("""
                {
                  "elements": [
                    { "name": "leave_type", "type": "NVARCHAR(50)", "isRequired": true },
                    { "name": "start_date", "type": "DATETIME", "isRequired": true }
                  ]
                }
                """, JsonOptions)!,
        };

        var fields = EventSpecAssembler.ExtractConfirmedFields(steps);
        if (fields.Count != 2 || fields[0]["name"].ToString() != "leave_type")
            throw new Exception("T1 应从 CommandQuery 提取 2 个字段");
    }

    private static void T2_EventSpecAssembler_ExtractsRulesFromDecisionTable()
    {
        var steps = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["WorkflowSpec"] = JsonSerializer.Deserialize<object>("""
                {
                  "tables": [{
                    "id": "DT1",
                    "conditions": [{ "name": "days", "operator": "<=", "value": 3 }],
                    "rules": [{ "conditionMask": [true], "actionIndex": 0 }]
                  }]
                }
                """, JsonOptions)!,
        };

        var rules = EventSpecAssembler.ExtractBusinessRules(steps, "请假");
        if (rules.Count < 2)
            throw new Exception($"T2 应提取判定表规则，实际 {rules.Count}");
    }

    private static void T3_RequirementSpecDocument_ContainsEventSections()
    {
        var svc = new JNPF.InteAssistant.Studio.RequirementSpecDocumentService();
        var md = svc.BuildRequirementSpecMarkdown(42, "请假系统测试", new[]
        {
            new JNPF.InteAssistant.Entitys.Dto.Ir.IrFragmentSnapshotDto
            {
                FragmentId = "eventspec:BE-001",
                FragmentType = "EventSpec",
                Payload = """{"eventName":"请假申请","confirmedFields":[{"name":"id","type":"BIGINT","required":true,"source":"test"}],"previousSteps":{"DomainModel":{"scope":"test"}}}""",
                SaStepsCompleted = new[] { "DomainModel" },
            },
        });

        if (!md.Contains("系统需求分析说明书") || !md.Contains("BE-001") || !md.Contains("请假申请"))
            throw new Exception("T3 说明书应含标题与事件章节");
    }

    private static void T4_RequirementTextHelper_TruncatesLongInput()
    {
        var longText = new string('A', 30_000);
        var ctx = new SkillContext
        {
            RunId = "r",
            TenantId = "t",
            ProjectId = "p",
            PipelineId = 1,
            UserRequirement = longText,
        };
        var result = RequirementTextHelper.ForPmPrompt(ctx);
        if (result.Length >= longText.Length || !result.Contains("截断"))
            throw new Exception("T4 长文档应裁剪并标注截断");
    }

    private static void T5_DesignDeliverableFormatter_ArchitectureMarkdown()
    {
        var md = JNPF.InteAssistant.Studio.DesignDeliverableFormatter.BuildArchitectureMarkdown("""
            {"pattern":"layered","modules":[{"name":"Leave","responsibility":"请假模块"}],"candidates":[{"score":0.9}]}
            """);
        if (!md.Contains("03-architecture") || !md.Contains("layered") || !md.Contains("Leave"))
            throw new Exception("T5 架构说明书应含 pattern 与 modules");
    }

    private static void T5b_DesignDeliverableFormatter_StringModules()
    {
        var md = JNPF.InteAssistant.Studio.DesignDeliverableFormatter.BuildArchitectureMarkdown("""
            {"pattern":"layered","modules":["leave-application","approval"],"candidates":[{"name":"Layered","score":90}]}
            """);
        if (!md.Contains("leave-application") || !md.Contains("approval"))
            throw new Exception("T5b 字符串 modules 数组应落盘为表格行");
    }
}
