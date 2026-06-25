/**
 * Prompt 模板单元测试
 */

import { describe, it, expect } from 'vitest';
import { REQUIREMENT_ANALYST_PROMPT, ARCHITECT_PROMPT, UI_UX_DESIGNER_PROMPT, DATABASE_DESIGNER_PROMPT, getAllTemplates, getTemplate } from '../llm/prompts';
import { BaseAgent, type AgentContext } from '../agents/base';
import { MockLLMGateway } from './mock-llm';

// 用于测试 buildSystemPrompt 的具体子类
class TestAgent extends BaseAgent {
  constructor() {
    super(new MockLLMGateway(), REQUIREMENT_ANALYST_PROMPT);
  }
  // 暴露 protected 方法
  public testBuildPrompt(context: AgentContext): string {
    return this.buildSystemPrompt(context);
  }
}

describe('Prompt 模板', () => {
  it('REQUIREMENT_ANALYST_PROMPT 变量定义完整', () => {
    const tpl = REQUIREMENT_ANALYST_PROMPT;
    expect(tpl.variables.length).toBe(3);
    expect(tpl.variables.some(v => v.name === 'domains')).toBe(true);
    expect(tpl.variables.some(v => v.name === 'domainPatterns')).toBe(true);
    expect(tpl.variables.some(v => v.name === 'technicalConstraints')).toBe(true);
  });

  it('ARCHITECT_PROMPT 变量定义完整', () => {
    const tpl = ARCHITECT_PROMPT;
    expect(tpl.variables.some(v => v.name === 'eab')).toBe(true);
  });

  it('UI_UX_DESIGNER_PROMPT 变量定义完整', () => {
    const tpl = UI_UX_DESIGNER_PROMPT;
    expect(tpl.variables.some(v => v.name === 'designDNA')).toBe(true);
    expect(tpl.variables.some(v => v.name === 'availableComponents')).toBe(true);
  });

  it('DATABASE_DESIGNER_PROMPT 无变量', () => {
    const tpl = DATABASE_DESIGNER_PROMPT;
    expect(tpl.variables.length).toBe(0);
  });

  it('getAllTemplates 返回 4 个模板', () => {
    const all = getAllTemplates();
    expect(all.size).toBe(4);
  });

  it('getTemplate 按 ID 查找', () => {
    const tpl = getTemplate('architect');
    expect(tpl).toBeDefined();
    expect(tpl!.name).toBe('架构师');
  });

  it('getTemplate 不存在的 ID 返回 undefined', () => {
    expect(getTemplate('nonexistent')).toBeUndefined();
  });
});

describe('buildSystemPrompt', () => {
  it('正确填充变量占位符', () => {
    const agent = new TestAgent();
    const context: AgentContext = {
      domains: '教育,医疗',
      domainPatterns: '[{"name":"学生管理"}]',
      technicalConstraints: '自定义约束',
    };
    const prompt = agent.testBuildPrompt(context);

    expect(prompt).toContain('教育,医疗');
    expect(prompt).toContain('[{"name":"学生管理"}]');
    expect(prompt).toContain('自定义约束');
    expect(prompt).not.toContain('{{domains}}');
    expect(prompt).not.toContain('{{domainPatterns}}');
    expect(prompt).not.toContain('{{technicalConstraints}}');
  });

  it('变量缺失时使用默认值', () => {
    const agent = new TestAgent();
    const prompt = agent.testBuildPrompt({});

    // domains 有默认值 "通用业务"
    expect(prompt).toContain('通用业务');
    // technicalConstraints 有默认值
    expect(prompt).toContain('JNPF 低代码平台');
  });
});
