import { BaseAgent } from '../orchestrator/BaseAgent';
import { StateMachineOutput, ILLMClient } from '../orchestrator/orchestrator-types';

export class StateMachineAgent extends BaseAgent<StateMachineOutput> {
  readonly name = 'StateMachineAgent';
  readonly tableName = 'sa_state_machine';
  readonly systemPrompt = '你是状态机分析师。生成状态机，包括状态、转换、触发条件。';

  constructor(llm: ILLMClient) { super(llm); }
}
