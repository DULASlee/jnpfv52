import { BaseAgent } from '../orchestrator/BaseAgent';
import { DictOutput, ILLMClient } from '../orchestrator/orchestrator-types';

export class DictAgent extends BaseAgent<DictOutput> {
  readonly name = 'DictAgent';
  readonly tableName = 'sa_data_dictionary';
  readonly systemPrompt = '你是数据字典分析师。生成数据字典，包括字段元素、数据流、数据存储。';

  constructor(llm: ILLMClient) { super(llm); }
}
