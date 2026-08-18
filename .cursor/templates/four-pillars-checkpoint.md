# 四大支柱检查点（节点审批前必填）

> 对应机器文件：`.claude/pillar-claim-current.json`  
> 校验：`node .claude/hooks/pillar-claim-check.mjs`  
> **① 不可用纠偏项/编译绿/单测绿顶替。**

填写后运行校验；通过才可向用户提交「可审批」。

## JSON 字段说明

```json
{
  "schema": "pillar-claim-v1",
  "node": "SG2",
  "claimedAt": "2026-07-12T13:00:00+08:00",
  "pillar1_business": {
    "capability": "本节点业务能力一句话（对照 §0.11 / 25 号，非 bug 列表）",
    "userAction": "用户在界面/API 做什么",
    "deliverables": ["产物路径或名称"],
    "evidence": ["业务证据路径/说明，非仅 test pass"],
    "notBugfixOnly": true
  },
  "pillar2_data": {
    "writeModel": "IR / 表",
    "fieldSource": "ai_entity_field 或说明",
    "tripleKeyOk": true,
    "evidence": ["..."]
  },
  "pillar3_legacy": {
    "clearedOrNA": "已切断的旧路径 / N/A 理由",
    "evidence": ["..."]
  },
  "pillar4_xunit": {
    "command": "dotnet test ...",
    "result": "N/N 通过 或 UT-GAP 单号",
    "evidence": ["..."]
  },
  "agentAttestation": "我确认①描述的是业务功能本体，而非纠偏项顶替"
}
```
