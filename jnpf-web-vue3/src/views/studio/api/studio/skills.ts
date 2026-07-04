import { defHttp } from '/@/utils/http/axios';

export interface SkillRunResult {
  runId: string;
  skillId: string;
  pipelineId: number;
  status: string;
  message?: string;
}

export interface SkillRunRecord {
  runId: string;
  skillId: string;
  status: string;
  startedAt: string;
  completedAt?: string;
  tokenConsumed: number;
  errorMessage?: string;
}

export function runPmSkill(pipelineId: number, data?: { userRequirement?: string; providerCode?: string }) {
  return defHttp.post<SkillRunResult>({
    url: `/api/studio/skills/pm/${pipelineId}/run`,
    data: data ?? {},
  });
}

export function runAnalystSkill(pipelineId: number, data?: { userRequirement?: string; providerCode?: string }) {
  return defHttp.post<SkillRunResult>({
    url: `/api/studio/skills/analyst/${pipelineId}/run`,
    data: data ?? {},
  });
}

export function confirmSkeleton(pipelineId: number, data?: { autoRunAnalyst?: boolean }) {
  return defHttp.post<{ status: string; fragmentId: string; autoRunAnalyst?: boolean }>({
    url: `/api/studio/skills/pm/${pipelineId}/confirm-skeleton`,
    data: { autoRunAnalyst: data?.autoRunAnalyst ?? false },
  });
}

export function listSkillRuns(pipelineId: number) {
  return defHttp.get<SkillRunRecord[]>({
    url: `/api/studio/skills/${pipelineId}/runs`,
  });
}

export function listSeedTemplates(params?: { keyword?: string; industry?: string }) {
  return defHttp.get<{ total: number; items: unknown[] }>({
    url: '/api/studio/skills/seed/templates',
    params,
  });
}
