import { BaseAgent } from '../orchestrator/BaseAgent';
import { PSpecOutput, ILLMClient } from '../orchestrator/orchestrator-types';

export class PSpecAgent extends BaseAgent<PSpecOutput> {
  readonly name = 'PSpecAgent';
  readonly tableName = 'sa_pspec';
  readonly systemPrompt = '你是过程规格分析师。生成 PSPEC（过程规格说明），包括输入、输出、验证、算法。';

  constructor(llm: ILLMClient) { super(llm); }
}
