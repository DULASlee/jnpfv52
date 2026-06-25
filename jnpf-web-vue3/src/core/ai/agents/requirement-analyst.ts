/**
 * 需求分析师智能体
 *
 * 接收模糊的自然语言需求，输出结构化的需求分析文档。
 * 支持首轮分析 + 多轮追问深化。
 *
 * @version 1.0.0
 * @module ai/agents/requirement-analyst
 */

import type { LLMGateway } from '../llm/types';
import { REQUIREMENT_ANALYST_PROMPT } from '../llm/prompts';
import { BaseAgent, type AgentContext, type AgentResponse } from './base';

// ============================================================
// 输出类型
// ============================================================

export interface RequirementAnalysis {
  /** 需求理解概述 */
  understanding: string;
  /** 追问列表（需要用户澄清的问题） */
  questions: string[];
  /** 领域模型 */
  proposedDomainModel: {
    entities: Array<{
      name: string;
      fields: Array<{ name: string; type: string }>;
    }>;
    relationships: Array<{
      from: string;
      to: string;
      type: 'one-to-many' | 'many-to-many' | 'one-to-one';
    }>;
    businessRules: Array<{
      name: string;
      condition: string;
      action: string;
    }>;
  };
  /** 策略选项 */
  strategies: Array<{
    name: string;
    description: string;
    pros: string[];
    cons: string[];
    impact: string;
  }>;
  /** 用户故事 */
  userStories: Array<{
    role: string;
    action: string;
    goal: string;
    acceptance: string;
  }>;
  /** 隐含需求 */
  implicitRequirements: string[];
  /** 风险点 */
  risks: string[];
}

// ============================================================
// RequirementAnalystAgent
// ============================================================

export class RequirementAnalystAgent extends BaseAgent {
  constructor(llm: LLMGateway) {
    super(llm, REQUIREMENT_ANALYST_PROMPT);
  }

  /**
   * 分析用户需求。
   *
   * 首轮分析：LLM 理解需求 → 输出领域模型 + 策略 + 用户故事。
   * 如果 LLM 判定需求不清晰，questions 字段会包含追问。
   *
   * @param userInput - 用户输入的自然语言需求
   * @param context - 执行上下文（可传入 domains, domainPatterns）
   * @returns 结构化需求分析
   */
  async analyze(userInput: string, context: AgentContext = {}): Promise<AgentResponse<RequirementAnalysis>> {
    return this.execute<RequirementAnalysis>(userInput, context);
  }

  /**
   * 追问深化。
   *
   * 用户回答第一轮问题后，将上下文传入进行深入分析。
   * 自动将前一轮分析作为 assistant 消息传入。
   *
   * @param userAnswers - 用户对追问的回答（问题→答案映射）
   * @param previousAnalysis - 前一轮分析结果
   * @param context - 执行上下文
   * @returns 更新后的需求分析
   */
  async followUp(
    userAnswers: Record<string, string>,
    previousAnalysis: RequirementAnalysis,
    context: AgentContext = {},
  ): Promise<AgentResponse<RequirementAnalysis>> {
    // 构建对话历史
    const answersText = Object.entries(userAnswers)
      .map(([q, a]) => `Q: ${q}\nA: ${a}`)
      .join('\n\n');

    const userInput = `基于前面的需求分析，我补充以下信息：\n\n${answersText}\n\n请更新需求分析结果。`;

    return this.analyze(userInput, {
      ...context,
      messages: [
        {
          role: 'assistant',
          content: `上一轮分析结果：\n${JSON.stringify(previousAnalysis, null, 2)}`,
        },
      ],
    });
  }
}
