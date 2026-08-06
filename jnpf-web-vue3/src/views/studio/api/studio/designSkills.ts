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
  /** @deprecated 设计启动以 analysisFinalized + hasEntityFields 为准 */
  ir1Stable: boolean;
  /** AnalysisCompleted.finalized=true（25 §6） */
  analysisFinalized?: boolean;
  hasEntityFields?: boolean;
  entityFieldCount?: number;
  /** 后端门禁：finalized ∧ 有实体字段 ∧ 质量门控 */
  canRunDesign?: boolean;
  hasQualityScore?: boolean;
  qualityTotalScore?: number | null;
  qualityCriticalCount?: number;
  qualityGatePasses?: boolean;
  pmReviewGatePasses?: boolean;
  pmReviewScore?: number | null;
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

/** 后端 JsonOptions PropertyNamingPolicy=null → PascalCase；统一为前端 camelCase */
export function normalizeDesignOrchestratorStatus(raw: Record<string, unknown>): DesignOrchestratorStatus {
  const phasesRaw = (raw.phases ?? raw.Phases ?? []) as Array<Record<string, unknown>>;
  return {
    pipelineId: (raw.pipelineId ?? raw.PipelineId ?? 0) as number,
    projectId: (raw.projectId ?? raw.ProjectId ?? '') as string,
    ir1Stable: (raw.ir1Stable ?? raw.Ir1Stable ?? false) as boolean,
    analysisFinalized: (raw.analysisFinalized ?? raw.AnalysisFinalized) as boolean | undefined,
    hasEntityFields: (raw.hasEntityFields ?? raw.HasEntityFields) as boolean | undefined,
    entityFieldCount: (raw.entityFieldCount ?? raw.EntityFieldCount) as number | undefined,
    canRunDesign: (raw.canRunDesign ?? raw.CanRunDesign) as boolean | undefined,
    hasQualityScore: (raw.hasQualityScore ?? raw.HasQualityScore) as boolean | undefined,
    qualityTotalScore: (raw.qualityTotalScore ?? raw.QualityTotalScore) as number | null | undefined,
    qualityCriticalCount: (raw.qualityCriticalCount ?? raw.QualityCriticalCount) as number | undefined,
    qualityGatePasses: (raw.qualityGatePasses ?? raw.QualityGatePasses) as boolean | undefined,
    pmReviewGatePasses: (raw.pmReviewGatePasses ?? raw.PmReviewGatePasses) as boolean | undefined,
    pmReviewScore: (raw.pmReviewScore ?? raw.PmReviewScore) as number | null | undefined,
    designComplete: (raw.designComplete ?? raw.DesignComplete ?? false) as boolean,
    phases: phasesRaw.map(
      (p): DesignSkillPhaseStatus => ({
        skillId: (p.skillId ?? p.SkillId ?? '') as string,
        phase: (p.phase ?? p.Phase ?? 'pending') as DesignSkillPhaseStatus['phase'],
        lastRunId: (p.lastRunId ?? p.LastRunId) as string | undefined,
        lastStatus: (p.lastStatus ?? p.LastStatus) as string | undefined,
      }),
    ),
    tokenConsumed: (raw.tokenConsumed ?? raw.TokenConsumed ?? 0) as number,
    tokenBudget: (raw.tokenBudget ?? raw.TokenBudget ?? 0) as number,
    budgetStatus: (raw.budgetStatus ?? raw.BudgetStatus ?? '') as string,
    constraintCriticalCount: (raw.constraintCriticalCount ?? raw.ConstraintCriticalCount) as number | undefined,
    constraintWarningCount: (raw.constraintWarningCount ?? raw.ConstraintWarningCount) as number | undefined,
  };
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

const DEFAULT_TOKEN_BUDGET = 5_000_000;

/** 后端可能 PascalCase；TokenBudget=0 时与 LlmBudgetApiService 对齐默认预算 */
export function normalizeLlmBudgetInfo(raw: Record<string, unknown>): LlmBudgetInfo {
  const tokenBudgetRaw = Number(raw.tokenBudget ?? raw.TokenBudget ?? 0);
  const tokenBudget = tokenBudgetRaw > 0 ? tokenBudgetRaw : DEFAULT_TOKEN_BUDGET;
  const tokenConsumed = Number(raw.tokenConsumed ?? raw.TokenConsumed ?? 0);
  const reserveThreshold = Number(
    raw.reserveThreshold ?? raw.ReserveThreshold ?? Math.floor(tokenBudget * 0.95),
  );
  const canRunRaw = raw.canRunDesign ?? raw.CanRunDesign;
  const canRunDesign =
    typeof canRunRaw === 'boolean' ? canRunRaw : tokenConsumed < reserveThreshold;
  return {
    projectId: String(raw.projectId ?? raw.ProjectId ?? ''),
    tenantId: (raw.tenantId ?? raw.TenantId) as string | undefined,
    tokenBudget,
    tokenConsumed,
    tokenRemaining: Number(raw.tokenRemaining ?? raw.TokenRemaining ?? Math.max(0, tokenBudget - tokenConsumed)),
    reserveThreshold,
    budgetStatus: String(raw.budgetStatus ?? raw.BudgetStatus ?? 'green'),
    canRunDesign,
    recentCalls: (raw.recentCalls ?? raw.RecentCalls) as LlmBudgetInfo['recentCalls'],
  };
}
