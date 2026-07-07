import { defHttp } from '/@/utils/http/axios';

export interface DeveloperOrchestratorStatus {
  pipelineId: number;
  projectId: string;
  designLocked: boolean;
  codegenDraft: boolean;
  sandboxBuildPassed: boolean;
  archGuardPassed: boolean;
  archWarningCount: number;
  codegenStability: string;
  lastDeveloperRunId?: string;
  lastDeveloperStatus?: string;
}

export interface DeveloperRunResult {
  runId: string;
  pipelineId: number;
  status: string;
  message?: string;
}

export interface DeployRunResult {
  runId: string;
  skillId: string;
  pipelineId: number;
  status: string;
  message?: string;
}

export const DEVELOPER_SKILL_ID = 'developer-skill';
export const TESTER_SKILL_ID = 'tester-skill';
export const DEPLOY_SKILL_ID = 'deploy-skill';

export function runDeveloperOrchestrator(pipelineId: number, data?: { providerCode?: string }) {
  return defHttp.post<DeveloperRunResult>({
    url: `/api/studio/skills/developer/${pipelineId}/run`,
    data: data ?? {},
  });
}

export function getDeveloperStatus(pipelineId: number) {
  return defHttp.get<DeveloperOrchestratorStatus>({
    url: `/api/studio/skills/developer/${pipelineId}/status`,
  });
}

export function runDeploySkill(pipelineId: number, data?: { providerCode?: string }) {
  return defHttp.post<DeployRunResult>({
    url: `/api/studio/skills/deploy/${pipelineId}/run`,
    data: data ?? {},
  });
}
