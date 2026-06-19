import { BaseAgent } from '../orchestrator/BaseAgent';
import { EROutput, ILLMClient } from '../orchestrator/orchestrator-types';

export class ERAgent extends BaseAgent<EROutput> {
  readonly name = 'ERAgent';
  readonly tableName = 'sa_er';
  readonly systemPrompt = '你是数据建模分析师。生成 ER 图，包括实体、列、关系。';

  constructor(llm: ILLMClient) { super(llm); }
}
