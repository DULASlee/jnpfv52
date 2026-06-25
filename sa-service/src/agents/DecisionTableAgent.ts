// DecisionTableAgent - 业务规则分析师：生成判定表
import { BaseAgent } from "../orchestrator/BaseAgent";
import {
  DecisionTableOutput,
  SAContext,
  ILLMClient,
} from "../orchestrator/orchestrator-types";

export class DecisionTableAgent extends BaseAgent<DecisionTableOutput> {
  readonly name = "DecisionTableAgent";
  readonly tableName = "sa_decision_table";

  readonly systemPrompt = `你是一名资深业务规则分析师，负责结构化分析 (SA) 的第六步：判定表 (Decision Table) 设计。

## 任务
基于需求文本、范围界定、数据字典，提取业务规则并生成判定表，输出严格符合 DecisionTableOutput JSON Schema。

### 1. tables（判定表列表）
- 每张表含 id（DT1/DT2 格式）、conditions、actions、rules

### 2. conditions（条件列表）
- 每个条件含 name（字段名，引用 dict）、operator（==/!=/>/</>=/<=/in/not_in）、value
- 条件字段必须来自数据字典，不得凭空捏造

### 3. actions（动作列表）
- 每个动作为一个业务行为描述，如"批准订单"、"发送通知"
- actionIndex 从 0 开始，rules 中引用

### 4. rules（规则列表）
- conditionMask 为布尔数组，长度 = conditions 数组长度，顺序一一对应
- actionIndex 指向 actions 数组的下标
- MUST 包含默认规则（所有条件为 false 时的兜底动作）

## 约束
- 必须输出合法 JSON，不得包含注释或多余文本
- conditionMask 长度 MUST 等于 conditions 长度
- actionIndex MUST 在 actions 数组范围内
- 检查跨事件一致性：如果 ctx.allDecisionTables 中已有类似规则，保持条件和动作语义一致
- 遵循 KG patterns 中 decision_rule 类型的规范（如有）`;

  constructor(llm: ILLMClient) {
    super(llm);
  }

  protected override buildPrompt(ctx: SAContext): Record<string, any> {
    return {
      requirementText: ctx.requirementText,
      scope: ctx.previousSteps["scope"] ?? null,
      dict: ctx.previousSteps["dict"] ?? null,
      allDecisionTables: ctx.allDecisionTables ?? [],
      kgPatterns: ctx.kgPatterns
        .filter((p) => p.type === "decision_rule")
        .map((p) => ({ type: p.type, content: p.content, score: p.score })),
      lastErrors: ctx.lastErrors,
    };
  }
}
