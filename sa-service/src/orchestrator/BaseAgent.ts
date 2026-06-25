// BaseAgent - 所有 SA Agent 的基类
// 9 个 Agent(Scope/DFD/BPM/Dict/PSpec/DecisionTable/ER/StateMachine/UI)都继承这个

import { ILLMClient, SAContext } from './orchestrator-types';

export abstract class BaseAgent<TOutput> {
  abstract readonly name: string;
  abstract readonly tableName: string;
  abstract readonly systemPrompt: string;

  /** 各 Agent 可覆盖的温度（不同步骤可能需要不同创造性） */
  protected temperature: number = 0.1;

  constructor(protected llm: ILLMClient) {}

  /**
   * 生成 SA 步骤产出
   * 子类可以覆盖 buildPrompt 来定制上下文
   */
  async generate(ctx: SAContext): Promise<TOutput> {
    const prompt = this.buildPrompt(ctx);

    const result = await this.llm.generate({
      systemPrompt: this.systemPrompt,
      context: prompt,
      lastErrors: ctx.lastErrors,
      temperature: this.temperature,  // Agent 级可覆盖温度
    });

    return result as TOutput;
  }

  /**
   * 构造 LLM 输入(注入上一步产出 + KG/DM + 错误回灌)
   * 子类可覆盖
   */
  protected buildPrompt(ctx: SAContext): Record<string, any> {
    return {
      requirement: ctx.requirementText,
      eventDescription: ctx.eventDescription,
      previousSteps: ctx.previousSteps,
      kgPatterns: ctx.kgPatterns.map(p => ({
        type: p.type,
        content: p.content,
        score: p.score,
      })),
      domainModel: ctx.domainModel,
      lastErrors: ctx.lastErrors,
    };
  }
}
