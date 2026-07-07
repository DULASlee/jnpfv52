using JNPF.InteAssistant.Sa;
using JNPF.InteAssistant.Skills;

const string skeleton = """
{
  "businessEvents": [
    { "eventId": "EV-001", "eventName": "提交请假申请", "complexityHint": "中等" },
    { "eventId": "EV-005", "eventName": "审批任务处理", "complexityHint": "复杂", "dependsOn": ["EV-001"] }
  ],
  "entityDrafts": [{
    "entityName": "LeaveRequest",
    "fields": [
      { "name": "requestId", "type": "String", "required": true },
      { "name": "employeeId", "type": "String", "required": true },
      { "name": "leaveType", "type": "String", "required": true },
      { "name": "status", "type": "String", "required": true }
    ]
  }],
  "businessRules": [
    { "ruleId": "AR-005", "scope": "EV-005", "description": "≤3天主管审批" }
  ]
}
""";

var compiler = new SaNineViewCompiler();
var result = compiler.CompileFromSkeletonJson(skeleton, "员工请假");
Console.WriteLine($"events={result.EventResults.Count} compileMs={result.CompileDurationMs} hash={result.BundleHash}");

var ev = result.EventResults[0];
var meta = new AnalystSkillService.BusinessEventMeta(ev.EventId, ev.EventName, ev.Complexity);
var payload = EventSpecAssembler.BuildPayloadJson(ev.EventId, meta, ev.Steps);
using var doc = System.Text.Json.JsonDocument.Parse(payload);
var fieldCount = doc.RootElement.GetProperty("confirmedFields").GetArrayLength();
Console.WriteLine($"EV-001 fields={fieldCount} steps={ev.Steps.Count}");
Console.WriteLine(fieldCount >= 4 && result.CompileDurationMs < 500 ? "SMOKE PASS" : "SMOKE FAIL");
