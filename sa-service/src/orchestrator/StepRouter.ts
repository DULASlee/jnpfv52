// StepRouter - 6 问清单 + 三级分层路由
// 根据 Step 1 提取的 events 复杂度,决定跑哪些步骤

import { SAOrchestrator } from './SAOrchestrator';
import { SAContext, ScopeOutput, DictOutput, ValidationError } from './orchestrator-types';

export type EventComplexity = 'simple' | 'medium' | 'complex';
export type AssetLevel = 'PROJECT' | 'EVENT' | 'PROCESS';

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

export interface BusinessEvent {
  id: number;
  name: string;
  description: string;
  complexity: 'simple' | 'medium' | 'complex';
}

export interface TierDecision {
  assetLevel: AssetLevel;
  eventId: number;
  stepsToRun: string[];
  reason: string;
}

/**
 * 六问清单：判断事件属于 PROJECT / EVENT / PROCESS 哪一级
 */
export function classifyEvent(
  event: BusinessEvent,
  globalDict?: DictOutput,
): TierDecision {
  // 问题 1: 是否涉及新实体？→ 需要 Dict/ER
  // 问题 2: 是否涉及新数据流？→ 需要 DFD
  // 问题 3: 是否涉及状态扭转？→ 需要 STD
  // 问题 4: 是否涉及跨系统交互？→ 需要 BPM
  // 问题 5: 是否涉及复杂决策逻辑？→ 需要 DecisionTable
  // 问题 6: 是否涉及并发/事务边界？→ 需要 PSPEC

  if (event.complexity === 'complex') {
    // 复杂事件：跑完整 PROCESS 级流水线
    return {
      assetLevel: 'PROCESS',
      eventId: event.id,
      stepsToRun: ['PSpecAgent', 'DecisionTableAgent', 'StateMachineAgent', 'UIAgent'],
      reason: `复杂事件 "${event.name}"：需深度推演（PSPEC + 判定表 + 状态机 + UI）`,
    };
  }

  if (event.complexity === 'medium') {
    // 中等事件：增量扩展，跑 UI + 可能的 STD
    return {
      assetLevel: 'EVENT',
      eventId: event.id,
      stepsToRun: ['StateMachineAgent', 'UIAgent'],
      reason: `中等事件 "${event.name}"：增量扩展（状态机 + UI）`,
    };
  }

  // 简单事件：只跑 UI，字段从 Project 级字典取
  return {
    assetLevel: 'EVENT',
    eventId: event.id,
    stepsToRun: ['UIAgent'],
    reason: `简单事件 "${event.name}"：仅生成 UI 屏`,
  };
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
  return await orchestrator.runStepWithValidation<ScopeOutput>(
    'ScopeAgent',
    'sa_scope',
    ctx,
    async (output) => {
      const { id } = await orchestrator.db.saveScope(output, ctx);
      ctx.scopeId = id;
    },
    // Scope 自身没有 Validator(它是源头)
    // 但可以做"事件复杂度分布"的合理性检查
    (output) => {
      const errors: ValidationError[] = [];
      if (output.eventCount === 0) {
        errors.push({ code: 'SCOPE_NO_EVENTS', message: 'Scope 没提取到任何业务事件', severity: 'ERROR' });
      }
      if (!output.systemBoundary?.inScope || output.systemBoundary.inScope.length === 0) {
        errors.push({ code: 'SCOPE_NO_INSCOPE', message: 'Scope 没定义 In Scope', severity: 'ERROR' });
      }
      return { passed: errors.length === 0, errors };
    }
  );
}
