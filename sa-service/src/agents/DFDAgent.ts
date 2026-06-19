import { BaseAgent } from '../orchestrator/BaseAgent';
import { DFDOutput, ILLMClient } from '../orchestrator/orchestrator-types';

export class DFDAgent extends BaseAgent<DFDOutput> {
  readonly name = 'DFDAgent';
  readonly tableName = 'sa_dfd';
  readonly systemPrompt = '你是数据流分析师。生成 DFD（数据流图），包括 process、flow、data store。';

  constructor(llm: ILLMClient) { super(llm); }
}
