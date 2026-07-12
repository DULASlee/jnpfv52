// StepRouter - 6 问清单 + 三级分层路由
// 根据 Step 1 提取的 events 复杂度,决定跑哪些步骤

import { SAOrchestrator } from './SAOrchestrator';
import { SAContext, ScopeOutput } from '../types';

export type EventComplexity = 'simple' | 'medium' | 'complex';

export interface StepDecision {
  runDFD: boolean;
  runBPM: boolean;
  runDict: boolean;
  runPSpec: boolean;
  runDecisionTable: boolean;
  runER: boolean;
  runStateMachine: boolean;
  runUI: boolean;
  reason: string;
}

/**
 * 根据事件复杂度决定跑哪些步骤
 * 这是"6 问清单"的核心逻辑
 */
export function decideSteps(complexity: EventComplexity, hasStateChange: boolean): StepDecision {
  switch (complexity) {
    case 'simple':
      return {
        runDFD: false,
        runBPM: false,
        runDict: false,
        runPSpec: false,
        runDecisionTable: false,
        runER: false,
        runStateMachine: false,
        runUI: true,  // 即使简单也要生成 UI(可能就是个按钮)
        reason: '简单事件:跳过 DFD/BPM/PSPEC/DT,只生成 UI',
      };
    case 'medium':
      return {
        runDFD: true,
        runBPM: true,
        runDict: true,
        runPSpec: true,
        runDecisionTable: false,  // 复杂规则才跑判定表
        runER: true,
        runStateMachine: hasStateChange,  // 有状态变化才跑 STD
        runUI: true,
        reason: '中等事件:跑 DFD/BPM/Dict/PSPEC/ER,按需跑 STD',
      };
    case 'complex':
      return {
        runDFD: true,
        runBPM: true,
        runDict: true,
        runPSpec: true,
        runDecisionTable: true,   // ★ 复杂事件必跑判定表
        runER: true,
        runStateMachine: true,
        runUI: true,
        reason: '复杂事件:跑全部 9 步',
      };
  }
}

/**
 * 跑 Step 1(总是先跑,作为整个流水线的入口)
 */
export async function runScopeStep(orchestrator: SAOrchestrator, ctx: SAContext): Promise<ScopeOutput> {
  return await orchestrator.runStepWithValidation(
    'ScopeAgent',
    'sa_scope',
    ctx,
    async (output) => orchestrator.saveScope(output, ctx),
    // Scope 自身没有 Validator(它是源头)
    // 但可以做"事件复杂度分布"的合理性检查
    (output) => {
      const errors = [];
      if (output.eventCount === 0) {
        errors.push({ code: 'SCOPE_NO_EVENTS', message: 'Scope 没提取到任何业务事件', severity: 'ERROR' as const });
      }
      if (output.systemBoundary.inScope.length === 0) {
        errors.push({ code: 'SCOPE_NO_INSCOPE', message: 'Scope 没定义 In Scope', severity: 'ERROR' as const });
      }
      return { passed: errors.length === 0, errors };
    }
  );
}
