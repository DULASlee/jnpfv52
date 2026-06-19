// StateMachineAgent - 状态机分析师：生成实体状态机（状态、转换、触发条件）
import { BaseAgent } from '../orchestrator/BaseAgent';
import { StateMachineOutput, SAContext, ILLMClient } from '../orchestrator/orchestrator-types';

export class StateMachineAgent extends BaseAgent<StateMachineOutput> {
  readonly name = 'StateMachineAgent';
  readonly tableName = 'sa_state_machine';

  readonly systemPrompt = `你是一名资深状态机分析师，负责结构化分析 (SA) 的第八步：状态机设计。

## 任务
基于数据字典 (dict) 和业务流程 (bpm)，为具有状态转换的实体生成状态机模型，输出严格符合 StateMachineOutput JSON Schema。

### 1. stateMachines（状态机列表）
- 仅对具有生命周期状态的实体生成状态机（如订单、工单、审批单等）
- 纯 CRUD 实体（无状态流转）不需要生成状态机

### 2. 每个状态机包含
- entity：实体名（PascalCase，必须与 dict 中的 dataStore 名称一致）
- states：状态列表（字符串数组），覆盖实体完整生命周期
  - 必须包含起始状态（如 Draft/草稿）和终态（如 Completed/已归档）
  - 状态名使用英文 PascalCase
- transitions：转换列表，每个转换包含
  - from：源状态
  - to：目标状态
  - trigger：触发条件（业务事件或用户操作，如 Submit、Approve、Reject）
  - trigger 描述应与 bpm 中的 activityNode 对应

### 3. 状态覆盖原则
- 正向流程：Draft → Submitted → Approved → Completed
- 异常流程：必须包含驳回 (Rejected)、撤回 (Withdrawn)、作废 (Cancelled) 等分支
- 每个状态至少有一条入边和一条出边（起始状态和终态除外）

## 约束
- 必须输出合法 JSON，不得包含注释或多余文本
- 状态名不得使用中文
- 转换不得形成死循环（A→B→A 除外，这是合法的驳回重提交）
- bpm 中的 activityNode 是触发条件的主要来源`;

  constructor(llm: ILLMClient) { super(llm); }

  protected override buildPrompt(ctx: SAContext): Record<string, any> {
    return {
      requirementText: ctx.requirementText,
      scope: ctx.previousSteps['scope'] ?? null,
      dict: ctx.previousSteps['dict'] ?? null,
      bpm: ctx.previousSteps['bpm'] ?? null,
      kgPatterns: ctx.kgPatterns
        .filter(p => p.type === 'state_machine')
        .map(p => ({ type: p.type, content: p.content, score: p.score })),
      lastErrors: ctx.lastErrors,
    };
  }
}
