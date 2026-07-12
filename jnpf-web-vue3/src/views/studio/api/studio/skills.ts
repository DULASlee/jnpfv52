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

/** @deprecated 生产主路径请用 runRequirementAnalysis；保留仅供回归/兼容 */
export function runAnalystSkill(pipelineId: number, data?: { userRequirement?: string; providerCode?: string }) {
  return defHttp.post<SkillRunResult>({
    url: `/api/studio/skills/analyst/${pipelineId}/run`,
    data: data ?? {},
  });
}

/** 启动/续跑三轮需求分析编排器（27 号） */
export function runRequirementAnalysis(pipelineId: number, data?: { providerCode?: string; answers?: unknown }) {
  return defHttp.post<SkillRunResult>({
    url: `/api/studio/skills/requirement-analysis/${pipelineId}/run`,
    data: data ?? {},
  });
}

export function confirmSkeleton(pipelineId: number, data?: { autoRunAnalyst?: boolean }) {
  return defHttp.post<{ status: string; fragmentId: string; autoRunAnalyst?: boolean }>({
    url: `/api/studio/skills/pm/${pipelineId}/confirm-skeleton`,
    data: { autoRunAnalyst: data?.autoRunAnalyst ?? false },
  });
}

export function confirmRequirementSpec(pipelineId: number, data?: { autoRunDesign?: boolean; forceConfirm?: boolean }) {
  return defHttp.post<{ status: string; stage: string; autoRunDesign?: boolean }>({
    url: `/api/studio/skills/analyst/${pipelineId}/confirm-requirement-spec`,
    data: { autoRunDesign: data?.autoRunDesign ?? false, forceConfirm: data?.forceConfirm ?? false },
  });
}

export interface AmendmentPatch {
  operation: string;
  target: string;
  name: string;
  displayName?: string;
  type?: string;
  description?: string;
  required?: boolean;
  references?: string;
  scopeEventId?: string;
  from?: string;
  to?: string;
}

export interface AmendmentUnderstanding {
  features: string[];
  flows: string[];
  entitiesOrTables: string[];
  summaryMarkdown: string;
  severity: string;
  patches?: AmendmentPatch[];
}

export interface PmAmendProposeResult {
  proposalId: string;
  understanding: AmendmentUnderstanding;
}

export interface PmAmendApplyRequest {
  proposalId: string;
  understanding?: AmendmentUnderstanding;
  userMessage?: string;
  providerCode?: string;
}

export function proposeRequirementAmendment(pipelineId: number, data: { userMessage: string; providerCode?: string }) {
  return defHttp.post<PmAmendProposeResult>({
    url: `/api/studio/skills/requirement-analysis/${pipelineId}/amend/propose`,
    data,
  });
}

export function applyRequirementAmendment(pipelineId: number, data: PmAmendApplyRequest) {
  return defHttp.post<{ status: string; proposalId: string; deltaText?: string; nextAction?: string; reviewRefreshed?: boolean }>({
    url: `/api/studio/skills/requirement-analysis/${pipelineId}/amend/apply`,
    data,
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

/** 矩阵题子项——每行对应一个事件或实体，用户独立选择。 */
export interface MatrixSubItem {
  rowId: string;
  rowLabel: string;
  /** 用户选择的选项 ID（MATRIX_SINGLE 单选 / MATRIX_MULTI 需扩展为数组） */
  selectedOption?: string;
  /** 用户在文本框中的补充 */
  freeText?: string;
}

export interface ClarificationQuestion {
  id: string;
  text: string;
  /** single | multi | text（legacy，向后兼容；优先使用 questionFormat） */
  type: 'single' | 'multi' | 'text';
  /** 关键题（true 时硬门控：必须作答才能推进） */
  required?: boolean;
  options: ClarificationOption[];
  /** P9：为什么问这个问题（减少用户困惑，提高回答质量） */
  contextHint?: string;
  /** P9：合理默认值（option id），PM 能定的行业惯例自动设为默认值 */
  defaultOption?: string;
  /** P9：问题格式枚举：SINGLE | MULTI | MATRIX_SINGLE | MATRIX_MULTI */
  questionFormat?: 'SINGLE' | 'MULTI' | 'MATRIX_SINGLE' | 'MATRIX_MULTI';
  /** P9：矩阵子项（矩阵题专用：每行一个事件/实体，独立选择） */
  matrixSubItems?: MatrixSubItem[];
}

export interface ClarificationSet {
  setId: string;
  /** requirement | architecture | system-design | requirement-analysis-round1/2/3 */
  stage: string;
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
  /** P9：矩阵行作答（矩阵题专用） */
  matrixRowAnswers?: MatrixSubItem[];
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
  /** 澄清阶段：requirement | architecture | system-design | requirement-analysis-round* */
  stage: string;
  /** 作答后前端应执行的下一步动作 */
  nextAction:
    | 're-evaluate'
    | 'rerun-architect'
    | 'rerun-system-design-clarification'
    | 'continue-requirement-analysis'
    | 'none';
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
