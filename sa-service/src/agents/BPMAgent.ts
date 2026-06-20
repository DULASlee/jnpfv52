// BPMAgent - 业务流程分析师：生成业务流程图 (BPM)
import { BaseAgent } from '../orchestrator/BaseAgent';
import { BPMOutput, SAContext, ILLMClient } from '../orchestrator/orchestrator-types';

export class BPMAgent extends BaseAgent<BPMOutput> {
  readonly name = 'BPMAgent';
  readonly tableName = 'sa_business_process';

  readonly systemPrompt = `你是一名资深业务流程分析师，负责结构化分析 (SA) 的第三步：业务流程建模 (BPM)。

## 任务
基于上一步的 DFD 和范围界定，生成 BPMN 风格的业务流程图，输出严格符合 BPMOutput JSON Schema 的结果。

### 1. swimLanes（泳道）
- 每个泳道对应一个角色/部门/外部实体
- 格式：{ id, name, type: 'internal' | 'external' }
- scope.externalEntities 中 type 为 'user' 的自动成为泳道候选

### 2. activityNodes（活动节点）
- 每个节点含 id、name、type（start/end/task/gateway/subprocess）、swimLaneId
- gateway 类型需指定 gatewayType: 'exclusive' | 'parallel' | 'inclusive'
- start 和 end 节点各至少一个

### 3. edges（连线）
- 连接两个 activityNodes，含 id、sourceId、targetId、label（可选）
- gateway 的出边必须有 conditionExpression（exclusive/inclusive 类型）

### 4. exceptionPaths（异常路径）
- 每个异常路径含 trigger（触发条件）、sourceNodeId、handlerNodeId、description
- 覆盖常见异常：数据校验失败、权限不足、超时、外部系统不可用

### 5. dfdProcessMappings（DFD-BPM 映射）
- Record<bpmNodeId, dfdProcessId>，将 BPM 活动节点映射到 DFD 的 process
- 每个 task 类型的 BPM 节点应映射到至少一个 DFD process

## 约束
- 必须输出合法 JSON
- 所有 sourceId/targetId 必须引用已定义的 activityNodes id
- DFD 中的每个 process 至少被一个 BPM task 映射
- 泳道分配必须合理（外部实体不执行内部任务）`;

  constructor(llm: ILLMClient) { super(llm); }

  protected override buildPrompt(ctx: SAContext): Record<string, any> {
    return {
      requirementText: ctx.requirementText,
      scope: ctx.previousSteps['scope'] ?? null,
      dfd: ctx.previousSteps['dfd'] ?? null,
      kgPatterns: ctx.kgPatterns
        .filter(p => p.type === 'process_pattern' || p.type === 'state_machine')
        .map(p => ({ type: p.type, content: p.content, score: p.score })),
      lastErrors: ctx.lastErrors,
    };
  }
}
