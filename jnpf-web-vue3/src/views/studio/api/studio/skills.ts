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

export function confirmRequirementSpec(pipelineId: number, data?: { autoRunDesign?: boolean }) {
  return defHttp.post<{ status: string; stage: string; autoRunDesign?: boolean }>({
    url: `/api/studio/skills/analyst/${pipelineId}/confirm-requirement-spec`,
    data: { autoRunDesign: data?.autoRunDesign ?? false },
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

// ════════════════════════════════════════════════════════════════
// ADR-005 交互式澄清问答
//
// 后端契约：backend/.../Entitys/Dto/Ir/ClarificationDtos.cs
// 字段须与 C# record 保持一致（camelCase 由后端 JsonSerializer camelCase 序列化）
// ════════════════════════════════════════════════════════════════

export interface ClarificationOption {
  id: string;
  label: string;
  /** 是否为"其他"项（true 时展开文本输入框） */
  freeText?: boolean;
}

export interface ClarificationQuestion {
  id: string;
  text: string;
  /** single | multi | text */
  type: 'single' | 'multi' | 'text';
  /** 关键题（true 时硬门控：必须作答才能推进） */
  required?: boolean;
  options: ClarificationOption[];
}

export interface ClarificationSet {
  setId: string;
  /** requirement | architecture | system-design */
  stage: 'requirement' | 'architecture' | 'system-design';
  round: number;
  title: string;
  intro: string;
  questions: ClarificationQuestion[];
  allowSkipNonCritical?: boolean;
}

export interface ClarificationAnswer {
  questionId: string;
  optionIds: string[];
  freeText?: string;
}

export interface AnswerClarificationRequest {
  setId: string;
  answers: ClarificationAnswer[];
  skippedQuestionIds?: string[];
  /** 逃生口：全部跳过直接分析 */
  skipAll?: boolean;
}

export interface AnswerClarificationResult {
  status: string;
  setId: string;
  fragmentId: string;
  stabilityState: string;
  triggerNextRound: boolean;
  /** 澄清阶段：requirement | architecture | system-design */
  stage: 'requirement' | 'architecture' | 'system-design';
  /** 作答后前端应执行的下一步动作 */
  nextAction: 're-evaluate' | 'rerun-architect' | 'rerun-system-design-clarification' | 'none';
}

/**
 * 提交一轮澄清问答的答案。
 * 后端对 required 题做硬门控：未作答返回 400（Oops.Bah）。
 */
export function answerClarification(pipelineId: number, data: AnswerClarificationRequest) {
  return defHttp.post<AnswerClarificationResult>({
    url: `/api/studio/skills/clarification/${pipelineId}/answer`,
    data,
  });
}
