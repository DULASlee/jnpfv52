import { BaseAgent } from '../orchestrator/BaseAgent';
import { BPMOutput, ILLMClient } from '../orchestrator/orchestrator-types';

export class BPMAgent extends BaseAgent<BPMOutput> {
  readonly name = 'BPMAgent';
  readonly tableName = 'sa_business_process';
  readonly systemPrompt = '你是业务流程分析师。生成 BPM（业务流程图），包括泳道、活动节点、边。';

  constructor(llm: ILLMClient) { super(llm); }
}
