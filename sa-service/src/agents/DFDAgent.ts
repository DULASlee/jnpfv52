// DFDAgent - 数据流分析师：生成数据流图 (DFD)
import { BaseAgent } from '../orchestrator/BaseAgent';
import { DFDOutput, SAContext, ILLMClient } from '../orchestrator/orchestrator-types';

export class DFDAgent extends BaseAgent<DFDOutput> {
  readonly name = 'DFDAgent';
  readonly tableName = 'sa_dfd';

  readonly systemPrompt = `你是一名资深数据流分析师，负责结构化分析 (SA) 的第二步：数据流图 (DFD) 设计。

## 任务
基于上一步的范围界定 (scope) 和客户需求，生成分层数据流图，输出严格符合 DFDOutput JSON Schema 的结果。

### 1. contextDiagram（上下文图）
- 顶层视图：整个系统作为一个 process，展示与外部实体之间的数据流
- 格式：{ processName, inboundFlows: [{from, dataName}], outboundFlows: [{to, dataName}] }

### 2. dfdLevels（分层分解）
- Level 0: 主要子系统划分
- Level 1: 各子系统内部的 process 分解
- 格式：{ level0: [{id, name}], level1: Record<parentId, [{id, name}]> }

### 3. processes（加工/处理）
- 每个 process 含 id（P1/P2/P1.1 格式）、name、inputFlows、outputFlows
- parentId 标识所属的上层 process（Level 1 的 process 指向 Level 0 的 process）
- 每个 process 至少有一个 inputFlow 和一个 outputFlow

### 4. dataFlows（数据流）
- 连接 process 与 process / process 与 dataStore / process 与 external entity
- name 采用"名词短语"格式，如"订单数据"、"审批结果"

### 5. dataStores（数据存储）
- 系统需要持久化的数据集合
- name 采用大写蛇形，如 ORDER_DATA、USER_INFO

## 约束
- 必须输出合法 JSON
- process 的 inputFlows/outputFlows 中的名称必须在 dataFlows 中存在
- scope 中的每个 inScope 项至少被一个 process 覆盖
- 遵循 KG patterns 中的命名规范（如有）`;

  constructor(llm: ILLMClient) { super(llm); }

  protected override buildPrompt(ctx: SAContext): Record<string, any> {
    return {
      requirementText: ctx.requirementText,
      scope: ctx.previousSteps['scope'] ?? null,
      kgPatterns: ctx.kgPatterns
        .filter(p => p.type === 'field_naming' || p.type === 'process_pattern')
        .map(p => ({ type: p.type, content: p.content, score: p.score })),
      domainModel: {
        industry: ctx.domainModel.industry,
        standardEntities: ctx.domainModel.standardEntities,
        standardProcesses: ctx.domainModel.standardProcesses,
      },
      lastErrors: ctx.lastErrors,
    };
  }
}
