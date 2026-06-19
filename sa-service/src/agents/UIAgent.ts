import { BaseAgent } from '../orchestrator/BaseAgent';
import { UIOutput, ILLMClient } from '../orchestrator/orchestrator-types';

export class UIAgent extends BaseAgent<UIOutput> {
  readonly name = 'UIAgent';
  readonly tableName = 'sa_ui';
  readonly systemPrompt = '你是 UI 设计分析师。生成 UI 原型，包括页面、字段、控件类型。';

  constructor(llm: ILLMClient) { super(llm); }
}
