/**
 * 智能体基类
 *
 * 所有智能体（需求分析师、架构师、UI/UX 设计师、数据库设计师）的抽象基类。
 * 提供：
 *   - LLM 调用封装（execute / executeStream）
 *   - Prompt 变量填充（buildSystemPrompt）
 *   - 响应解析（parseResponse — 3 种 JSON 格式）
 *   - 置信度评估（evaluateConfidence）
 *
 * 子类只需定义 AgentResponse 的具体类型并调用 execute<T>()。
 *
 * @version 1.0.0
 * @module ai/agents/base
 */

import type { LLMGateway, ChatResponse } from '../llm/types';
import type { PromptTemplate } from '../llm/prompts';

// ============================================================
// 上下文类型
// ============================================================

/** 智能体执行上下文（供 resolveVariable 使用） */
export interface AgentContext {
  /** 对话历史 */
  messages?: Array<{ role: 'system' | 'user' | 'assistant'; content: string }>;
  /** DKEE 知识图谱数据 */
  knowledgeGraph?: Record<string, unknown>;
  /** 企业架构基线（EAB）快照 */
  eab?: Record<string, unknown>;
  /** 当前 IR（架构师产出后，UI/DB 智能体继续加工） */
  currentIR?: Record<string, unknown>;
  /** 当前业务领域 */
  domain?: string;
  /** 扩展字段 */
  [key: string]: unknown;
}

/** 智能体响应（泛型，子类指定具体 T） */
export interface AgentResponse<T = unknown> {
  /** 结构化数据 */
  data: T;
  /** LLM 原始文本（调试用） */
  rawText: string;
  /** Token 用量 */
  usage: ChatResponse['usage'];
  /** 请求延迟（毫秒） */
  latency: number;
  /** 响应置信度（0-1），< 0.6 建议人工审核 */
  confidence: number;
}

// ============================================================
// BaseAgent
// ============================================================

export abstract class BaseAgent {
  protected llm: LLMGateway;
  protected template: PromptTemplate;

  constructor(llm: LLMGateway, template: PromptTemplate) {
    this.llm = llm;
    this.template = template;
  }

  // ============================================================
  // 核心方法
  // ============================================================

  /**
   * 执行智能体（非流式）。
   *
   * 流程：buildSystemPrompt → llm.chat → parseResponse → evaluateConfidence
   *
   * @param userInput - 用户输入
   * @param context - 执行上下文
   * @returns AgentResponse<T>
   */
  protected async execute<T>(userInput: string, context: AgentContext): Promise<AgentResponse<T>> {
    const systemPrompt = this.buildSystemPrompt(context);

    const response = await this.llm.chat({
      messages: [{ role: 'system', content: systemPrompt }, ...(context.messages ?? []), { role: 'user', content: userInput }],
      responseFormat: 'json',
    });

    const data = this.parseResponse<T>(response.content);
    const confidence = this.evaluateConfidence(data, response);

    return {
      data,
      rawText: response.content,
      usage: response.usage,
      latency: response.latency,
      confidence,
    };
  }

  /**
   * 执行智能体（流式）。
   *
   * 收集完整响应后解析，但实时 yield 增量文本给调用方展示"AI 思考过程"。
   *
   * @param userInput - 用户输入
   * @param context - 执行上下文
   * @returns 异步生成器
   */
  protected async *executeStream(userInput: string, context: AgentContext): AsyncGenerator<string, void, undefined> {
    const systemPrompt = this.buildSystemPrompt(context);

    let fullContent = '';

    for await (const chunk of this.llm.chatStream({
      messages: [{ role: 'system', content: systemPrompt }, ...(context.messages ?? []), { role: 'user', content: userInput }],
    })) {
      fullContent += chunk;
      yield chunk;
    }

    const data = this.parseResponse<unknown>(fullContent);
    const confidence = this.evaluateConfidence(data, {
      content: fullContent,
      usage: { promptTokens: 0, completionTokens: 0, totalTokens: 0 },
      model: '',
      provider: '',
      latency: 0,
    });

    yield `\n\n---\n**置信度:** ${(confidence * 100).toFixed(0)}%`;

    // 最终产出
    const finalResponse: AgentResponse<unknown> = {
      data,
      rawText: fullContent,
      usage: { promptTokens: 0, completionTokens: 0, totalTokens: 0 },
      latency: 0,
      confidence,
    };

    // 通过 hack 返回最终对象（generator 不能 return 值给 for-await 消费方，
    // 调用方通过 lastValue 模式获取）
    yield `\n__AGENT_RESPONSE__${JSON.stringify(finalResponse)}`;
  }

  // ============================================================
  // Prompt 构建
  // ============================================================

  /**
   * 构建 System Prompt。
   *
   * 扫描模板中的 {{variableName}} 占位符，
   * 调用 resolveVariable 获取实际值后替换。
   *
   * @param context - 执行上下文
   * @returns 填充后的 System Prompt
   */
  protected buildSystemPrompt(context: AgentContext): string {
    let result = this.template.template;

    for (const variable of this.template.variables) {
      const placeholder = `{{${variable.name}}}`;
      if (result.includes(placeholder)) {
        const value = this.resolveVariable(variable.name, context);
        result = result.replaceAll(placeholder, value);
      }
    }

    return result;
  }

  /**
   * 解析变量值。
   *
   * 优先级：
   *   1. AgentContext 中直接匹配的 key
   *   2. PromptVariable.defaultValue
   *   3. 空字符串
   *
   * @param name - 变量名
   * @param context - 执行上下文
   * @returns 变量值字符串
   */
  protected resolveVariable(name: string, context: AgentContext): string {
    // 尝试从上下文直接取值
    const value = context[name];
    if (value !== undefined && value !== null) {
      if (typeof value === 'string') return value;
      return JSON.stringify(value);
    }

    // fallback 到 PromptTemplate 变量的默认值
    const variable = this.template.variables.find(v => v.name === name);
    if (variable?.defaultValue !== undefined) {
      return variable.defaultValue;
    }

    return '';
  }

  // ============================================================
  // 响应解析（3 种格式）
  // ============================================================

  /**
   * 解析 LLM 响应为结构化数据。
   *
   * 依次尝试三种格式：
   *   1. 纯 JSON — 直接 JSON.parse
   *   2. Markdown 包裹 — 提取 ```json ... ``` 块
   *   3. 混合文本 — 提取第一个 {...} 块
   *
   * 全部失败则抛出异常。
   *
   * @param content - LLM 原始响应内容
   * @returns 解析后的数据
   * @throws 无法提取 JSON 时抛出
   */
  protected parseResponse<T>(content: string): T {
    const trimmed = content.trim();

    // 尝试 1：纯 JSON
    if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
      try {
        return JSON.parse(trimmed) as T;
      } catch {
        // 继续尝试其他格式
      }
    }

    // 尝试 2：Markdown 包裹的 JSON
    const jsonBlockMatch = trimmed.match(/```(?:json)?\s*([\s\S]*?)```/);
    if (jsonBlockMatch) {
      try {
        return JSON.parse(jsonBlockMatch[1].trim()) as T;
      } catch {
        // 继续尝试
      }
    }

    // 尝试 3：混合文本中的第一个完整 JSON 对象
    const firstBrace = trimmed.indexOf('{');
    if (firstBrace !== -1) {
      // 从第一个 { 开始，追踪嵌套层级找到匹配的 }
      let depth = 0;
      let endIndex = -1;
      for (let i = firstBrace; i < trimmed.length; i++) {
        if (trimmed[i] === '{') depth++;
        if (trimmed[i] === '}') {
          depth--;
          if (depth === 0) {
            endIndex = i;
            break;
          }
        }
      }
      if (endIndex !== -1) {
        const jsonCandidate = trimmed.slice(firstBrace, endIndex + 1);
        try {
          return JSON.parse(jsonCandidate) as T;
        } catch {
          // 最后尝试失败
        }
      }
    }

    throw new Error(`[BaseAgent] 无法从 LLM 响应中提取 JSON。响应前 200 字符: ${trimmed.slice(0, 200)}`);
  }

  // ============================================================
  // 置信度评估
  // ============================================================

  /**
   * 评估响应置信度（0-1）。
   *
   * 基础分 0.7。
   * + 0.1：响应长度 > 50 字符
   * + 0.1：数据包含 3+ 个 key
   * 上限 1.0。
   *
   * @param data - 解析后的数据
   * @param _response - LLM 原始响应（保留，后续扩展用）
   * @returns 置信度分数
   */
  protected evaluateConfidence(data: unknown, _response: ChatResponse): number {
    let score = 0.7;

    // 数据完整性
    if (data !== null && typeof data === 'object') {
      const keys = Object.keys(data as Record<string, unknown>);
      if (keys.length >= 3) {
        score += 0.1;
      }
    }

    // 上限
    return Math.min(score, 1.0);
  }
}
