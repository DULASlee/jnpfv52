import { defHttp } from '/@/utils/http/axios';

export interface DesignSkillPhaseStatus {
  skillId: string;
  phase: 'pending' | 'running' | 'completed' | 'stable' | 'failed';
  lastRunId?: string;
  lastStatus?: string;
}

export interface DesignOrchestratorStatus {
  pipelineId: number;
  projectId: string;
  ir1Stable: boolean;
  designComplete: boolean;
  phases: DesignSkillPhaseStatus[];
  tokenConsumed: number;
  tokenBudget: number;
  budgetStatus: string;
  constraintCriticalCount?: number;
  constraintWarningCount?: number;
}

export interface DesignRunResult {
  runId: string;
  pipelineId: number;
  status: string;
  message?: string;
}

export interface LlmBudgetInfo {
  projectId: string;
  tenantId?: string;
  tokenBudget: number;
  tokenConsumed: number;
  tokenRemaining: number;
  reserveThreshold: number;
  budgetStatus: string;
  canRunDesign: boolean;
  recentCalls?: Array<{
    runId?: string;
    skillId?: string;
    model?: string;
    promptTokens?: number;
    completionTokens?: number;
    latencyMs?: number;
    creatorTime?: string;
  }>;
}

export const DESIGN_SKILL_IDS = {
  architect: 'architect-skill',
  dbDesign: 'db-design-skill',
  uiDesign: 'ui-design-skill',
  systemDesign: 'system-design-skill',
} as const;

export function runDesignOrchestrator(pipelineId: number, data?: { providerCode?: string }) {
  return defHttp.post<DesignRunResult>({
    url: `/api/studio/skills/design/${pipelineId}/run`,
    data: data ?? {},
  });
}

export function getDesignStatus(pipelineId: number) {
  return defHttp.get<DesignOrchestratorStatus>({
    url: `/api/studio/skills/design/${pipelineId}/status`,
  });
}

export function runArchitectSkill(pipelineId: number, data?: { providerCode?: string }) {
  return defHttp.post<DesignRunResult>({
    url: `/api/studio/skills/architect/${pipelineId}/run`,
    data: data ?? {},
  });
}

export function runDbDesignSkill(pipelineId: number, data?: { providerCode?: string }) {
  return defHttp.post<DesignRunResult>({
    url: `/api/studio/skills/db-design/${pipelineId}/run`,
    data: data ?? {},
  });
}

export function runUiDesignSkill(pipelineId: number, data?: { providerCode?: string }) {
  return defHttp.post<DesignRunResult>({
    url: `/api/studio/skills/ui-design/${pipelineId}/run`,
    data: data ?? {},
  });
}

export function runSystemDesignSkill(pipelineId: number, data?: { providerCode?: string }) {
  return defHttp.post<DesignRunResult>({
    url: `/api/studio/skills/system-design/${pipelineId}/run`,
    data: data ?? {},
  });
}

/** ADR-005 P3：总体设计澄清 Skill（两阶段，提问 + 阶段二约束引擎锁定）。 */
export function runSystemDesignClarificationSkill(pipelineId: number, data?: { providerCode?: string }) {
  return defHttp.post<DesignRunResult>({
    url: `/api/studio/skills/system-design-clarification/${pipelineId}/run`,
    data: data ?? {},
  });
}

export function getLlmBudget(projectId: string) {
  return defHttp.get<LlmBudgetInfo>({
    url: `/api/studio/llm/budget/${projectId}`,
  });
}
