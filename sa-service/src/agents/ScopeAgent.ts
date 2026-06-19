import { BaseAgent } from '../orchestrator/BaseAgent';
import { ScopeOutput, ILLMClient } from '../orchestrator/orchestrator-types';

export class ScopeAgent extends BaseAgent<ScopeOutput> {
  readonly name = 'ScopeAgent';
  readonly tableName = 'sa_scope';
  readonly systemPrompt = '你是需求分析师。从客户需求中提取系统边界、外部实体和业务事件。';

  constructor(llm: ILLMClient) { super(llm); }
}
