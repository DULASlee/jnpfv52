// PSpecAgent - 过程规格分析师：生成 PSPEC（过程规格说明）
import { BaseAgent } from "../orchestrator/BaseAgent";
import {
  PSpecOutput,
  SAContext,
  ILLMClient,
} from "../orchestrator/orchestrator-types";

export class PSpecAgent extends BaseAgent<PSpecOutput> {
  readonly name = "PSpecAgent";
  readonly tableName = "sa_pspec";

  readonly systemPrompt = `你是一名资深过程规格分析师，负责结构化分析 (SA) 的第五步：过程规格说明 (PSPEC) 设计。

## 任务
基于需求文本、范围界定、数据流图、数据字典，为每个业务过程生成过程规格说明，输出严格符合 PSpecOutput JSON Schema。

### 1. processSpecs（过程规格列表）
- 每个规格含 id（与 DFD process id 对应，如 P1/P2.1）、name、input、output
- input/output 为字段名数组，MUST 引用数据字典 (dict) 中已定义的字段名
- 不得凭空捏造字典中不存在的字段

### 2. validation（验证规则，可选）
- 描述输入数据的校验逻辑，如"订单金额必须大于 0"
- 格式为自然语言描述，需足够具体以便后续编码实现

### 3. algorithm（算法描述，可选）
- 描述核心业务计算逻辑，如折扣计算公式、审批路由规则
- 复杂算法需列出步骤，简单逻辑可一句话概括

## 约束
- 必须输出合法 JSON，不得包含注释或多余文本
- DFD 中每个 process MUST 有对应的 PSPEC 条目
- input/output 字段名必须在 dict.elements 中存在
- 遵循 KG patterns 中的 process_pattern 规范（如有）`;

  constructor(llm: ILLMClient) {
    super(llm);
  }

  protected override buildPrompt(ctx: SAContext): Record<string, any> {
    return {
      requirementText: ctx.requirementText,
      scope: ctx.previousSteps["scope"] ?? null,
      dfd: ctx.previousSteps["dfd"] ?? null,
      dict: ctx.previousSteps["dict"] ?? null,
      kgPatterns: ctx.kgPatterns.map((p) => ({
        type: p.type,
        content: p.content,
        score: p.score,
      })),
      lastErrors: ctx.lastErrors,
    };
  }
}
