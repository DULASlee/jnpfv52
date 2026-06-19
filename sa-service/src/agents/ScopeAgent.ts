// ScopeAgent - 需求分析师：提取系统边界、外部实体、业务事件
import { BaseAgent } from '../orchestrator/BaseAgent';
import { ScopeOutput, SAContext, ILLMClient } from '../orchestrator/orchestrator-types';

export class ScopeAgent extends BaseAgent<ScopeOutput> {
  readonly name = 'ScopeAgent';
  readonly tableName = 'sa_scope';

  readonly systemPrompt = `你是一名资深需求分析师，负责结构化分析 (SA) 流程的第一步：范围界定。

## 任务
从客户需求文本中提取以下三类信息，输出严格符合 ScopeOutput JSON Schema 的结果：

### 1. systemBoundary（系统边界）
- inScope: 本次系统需要实现的功能模块列表
- outOfScope: 明确不属于本期范围的需求（防止范围蔓延）

### 2. externalEntities（外部实体）
- 每个实体包含 name、type（用户/系统/设备/第三方服务）、description
- 外部实体是与本系统交互但不受本系统控制的参与者

### 3. businessEvents（业务事件）
- 每个事件包含 id（从 1 递增）、name、description、complexity（simple/medium/complex）
- 复杂度判定：simple = 单步骤 CRUD；medium = 多步骤但无分支；complex = 含条件分支或跨模块协调

## 约束
- 必须输出合法 JSON，不得包含注释或多余文本
- 如果 kgPatterns 包含 field_naming 类型，遵循其命名规范
- 如果 domainModel 提供了 standardEntities，优先复用其中的实体名称
- 业务事件数量 eventCount 必须等于 businessEvents 数组长度`;

  constructor(llm: ILLMClient) { super(llm); }

  protected override buildPrompt(ctx: SAContext): Record<string, any> {
    return {
      requirementText: ctx.requirementText,
      kgPatterns: ctx.kgPatterns
        .filter(p => p.type === 'field_naming' || p.type === 'process_pattern')
        .map(p => ({ type: p.type, content: p.content, score: p.score })),
      domainModel: {
        industry: ctx.domainModel.industry,
        standardEntities: ctx.domainModel.standardEntities,
      },
      lastErrors: ctx.lastErrors,
    };
  }
}
