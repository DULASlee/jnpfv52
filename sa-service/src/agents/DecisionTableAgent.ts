import { BaseAgent } from '../orchestrator/BaseAgent';
import { DecisionTableOutput, ILLMClient } from '../orchestrator/orchestrator-types';

export class DecisionTableAgent extends BaseAgent<DecisionTableOutput> {
  readonly name = 'DecisionTableAgent';
  readonly tableName = 'sa_decision_table';
  readonly systemPrompt = '你是业务规则分析师。生成判定表，包括条件、动作、规则。';

  constructor(llm: ILLMClient) { super(llm); }
}
