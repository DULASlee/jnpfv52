using JNPF.InteAssistant.Sa;
using Microsoft.Extensions.Logging.Abstractions;
using JNPF.InteAssistant.Skills;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// SaNineViewCompiler — 预分析 → SA 九步视图（无 LLM）。
/// </summary>
public static class SaNineViewCompilerTests
{
    private const string LeaveSkeletonJson = """
        {
          "businessEvents": [
            { "eventId": "EV-001", "eventName": "提交请假申请", "complexityHint": "中等", "dependsOn": [] },
            { "eventId": "EV-005", "eventName": "审批任务处理", "complexityHint": "复杂", "dependsOn": ["EV-001"] }
          ],
          "entityDrafts": [
            {
              "entityName": "LeaveRequest",
              "tableName": "OA_LEAVE_REQUEST",
              "fields": [
                { "name": "requestId", "type": "String", "required": true },
                { "name": "employeeId", "type": "String", "required": true },
                { "name": "leaveType", "type": "String", "required": true },
                { "name": "startTime", "type": "DateTime", "required": true },
                { "name": "duration", "type": "Decimal", "required": false },
                { "name": "status", "type": "String", "required": true }
              ]
            }
          ],
          "businessRules": [
            { "ruleId": "AR-005-1", "scope": "EV-005", "description": "≤3天部门主管审批；>3天需HR审批" }
          ],
          "stateTransitions": [
            { "entity": "LeaveRequest", "from": "Draft", "to": "Submitted", "trigger": "EV-001" },
            { "entity": "LeaveRequest", "from": "Submitted", "to": "Approved", "trigger": "EV-005" }
          ]
        }
        """;

    public static void RunAll()
    {
        T1_Compile_FastAndNineStepsPerEvent();
        T2_Compile_ExtractsFieldsNotDefaultPkOnly();
        T3_Compile_ComplexEventHasWorkflowAndPspec();
        T4_Compile_EventSpecAssemblerConsumesOutput();
    }

    private static void T1_Compile_FastAndNineStepsPerEvent()
    {
        var compiler = new SaNineViewCompiler(NullLogger<SaNineViewCompiler>.Instance);
        var result = compiler.CompileFromSkeletonJson(LeaveSkeletonJson, "员工请假系统");

        if (result.EventResults.Count != 2)
            throw new Exception($"T1 应有 2 个事件，实际 {result.EventResults.Count}");

        if (result.CompileDurationMs > 500)
            throw new Exception($"T1 编译应毫秒级，实际 {result.CompileDurationMs}ms");

        foreach (var ev in result.EventResults)
        {
            foreach (var step in SaStepNames.All)
            {
                if (!ev.Steps.ContainsKey(step))
                    throw new Exception($"T1 事件 {ev.EventId} 缺步骤 {step}");
            }
        }
    }

    private static void T2_Compile_ExtractsFieldsNotDefaultPkOnly()
    {
        var compiler = new SaNineViewCompiler(NullLogger<SaNineViewCompiler>.Instance);
        var result = compiler.CompileFromSkeletonJson(LeaveSkeletonJson);
        var ev = result.EventResults.First(e => e.EventId == "EV-001");
        var meta = new AnalystSkillService.BusinessEventMeta(ev.EventId, ev.EventName, ev.Complexity);
        var payload = EventSpecAssembler.BuildPayloadJson(ev.EventId, meta, ev.Steps);

        using var doc = System.Text.Json.JsonDocument.Parse(payload);
        var count = doc.RootElement.GetProperty("confirmedFields").GetArrayLength();
        if (count < 4)
            throw new Exception($"T2 应提取 ≥4 字段，实际 {count}");

        var names = doc.RootElement.GetProperty("confirmedFields").EnumerateArray()
            .Select(f => f.GetProperty("name").GetString()).ToList();
        if (names.Count == 1 && names[0] == "id")
            throw new Exception("T2 不应仅 default-pk");
    }

    private static void T3_Compile_ComplexEventHasWorkflowAndPspec()
    {
        var compiler = new SaNineViewCompiler(NullLogger<SaNineViewCompiler>.Instance);
        var result = compiler.CompileFromSkeletonJson(LeaveSkeletonJson);
        var ev = result.EventResults.First(e => e.EventId == "EV-005");

        if (!ev.Steps.ContainsKey(SaStepNames.WorkflowSpec)
            || !ev.Steps.ContainsKey(SaStepNames.IntegrationPoints))
            throw new Exception("T3 complex 事件应含 WorkflowSpec 与 IntegrationPoints");

        var meta = new AnalystSkillService.BusinessEventMeta(ev.EventId, ev.EventName, ev.Complexity);
        var payload = EventSpecAssembler.BuildPayloadJson(ev.EventId, meta, ev.Steps);
        if (!payload.Contains("WorkflowSpec") && !payload.Contains("tables"))
            throw new Exception("T3 payload 应含判定表结构");
    }

    private static void T4_Compile_EventSpecAssemblerConsumesOutput()
    {
        var compiler = new SaNineViewCompiler(NullLogger<SaNineViewCompiler>.Instance);
        var result = compiler.CompileFromSkeletonJson(LeaveSkeletonJson);
        var ev = result.EventResults[0];
        var meta = new AnalystSkillService.BusinessEventMeta(ev.EventId, ev.EventName, ev.Complexity);

        var payload = EventSpecAssembler.BuildPayloadJson(ev.EventId, meta, ev.Steps);
        if (!payload.Contains("leave_type") && !payload.Contains("request_id") && !payload.Contains("requestId"))
            throw new Exception("T4 EventSpec payload 应含骨架字段名");

        if (!payload.Contains("saStepsCompleted"))
            throw new Exception("T4 payload 应含 saStepsCompleted");
    }
}
