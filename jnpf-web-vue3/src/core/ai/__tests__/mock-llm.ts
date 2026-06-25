/**
 * Mock LLM 网关实现
 *
 * 用于智能体和 Prompt 单元测试，不真实调用 LLM API。
 * 支持预设响应用于验证解析逻辑。
 *
 * @version 1.0.0
 * @module ai/__tests__/mock-llm
 */

import type { LLMGateway, ChatRequest, ChatResponse } from '../llm/types';

export class MockLLMGateway implements LLMGateway {
  private responses = new Map<string, string>();
  private callLog: ChatRequest[] = [];
  private providerInfo = { provider: 'mock', model: 'mock-model' };

  /**
   * 设置预设响应。
   *
   * @param promptPattern - 匹配关键字（用户消息中包含此关键字时返回）
   * @param response - 预设的响应内容
   */
  setResponse(promptPattern: string, response: string): void {
    this.responses.set(promptPattern, response);
  }

  /** 设置 providerInfo */
  setProviderInfo(info: { provider: string; model: string }): void {
    this.providerInfo = info;
  }

  async chat(request: ChatRequest): Promise<ChatResponse> {
    this.callLog.push(request);
    const lastMsg = request.messages[request.messages.length - 1];

    // 匹配预设响应
    for (const [pattern, response] of this.responses) {
      if (lastMsg.content.includes(pattern)) {
        return {
          content: response,
          usage: { promptTokens: 100, completionTokens: 200, totalTokens: 300 },
          model: this.providerInfo.model,
          provider: this.providerInfo.provider,
          latency: 10,
        };
      }
    }

    // 默认空响应
    return {
      content: '{}',
      usage: { promptTokens: 0, completionTokens: 0, totalTokens: 0 },
      model: this.providerInfo.model,
      provider: this.providerInfo.provider,
      latency: 0,
    };
  }

  async *chatStream(): AsyncGenerator<string, void, undefined> {
    yield '{}';
  }

  async healthCheck(): Promise<boolean> {
    return true;
  }

  getProviderInfo(): { provider: string; model: string } {
    return this.providerInfo;
  }

  /** 获取调用记录 */
  getCallLog(): ChatRequest[] {
    return [...this.callLog];
  }

  /** 清空调用记录 */
  clearCallLog(): void {
    this.callLog = [];
  }
}
