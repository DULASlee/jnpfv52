import { defHttp } from '/@/utils/http/axios';

/**
 * 阶段七 P7-E04/P7-E03 前端 API 封装。
 * 文档：15、全链条第七阶段开发计划.md §6.4 §6.5
 */

// ─── 质量榜（P7-E04）───

export interface QualityBoardItem {
  skillId: string;
  totalRuns: number;
  successCount: number;
  failCount: number;
  successRate: number;
  avgTokens: number;
  lastRunAt: string;
  /** 质量等级 A/B/C/D（↔ green/yellow/red/fuse） */
  grade: string;
}

export interface QualityBoardResult {
  items: QualityBoardItem[];
  sinceDays: number;
  totalSkills: number;
  overallSuccessRate: number;
}

export function getSkillQualityBoard(sinceDays = 30) {
  return defHttp.get<QualityBoardResult>({
    url: '/api/studio/skill-quality',
    params: { sinceDays },
  });
}

// ─── 人工抽检（P7-E03）───

export interface SkillReview {
  f_Id: number;
  f_SkillRunId: string;
  f_SkillId: string;
  f_Score: number;
  f_Verdict: string;
  f_Comment?: string;
  f_ReviewerId?: number;
  f_ReviewerName?: string;
  f_CreatorTime: string;
}

export interface InterRaterStats {
  reviewerCount: number;
  meanScore: number;
  stdDev: number;
  passCount: number;
  failCount: number;
  majorityVerdict: string;
  isDisputed: boolean;
}

export interface ReviewListResult {
  items: SkillReview[];
  total: number;
  stats: InterRaterStats;
}

export function getSkillReviews(skillRunId: string) {
  return defHttp.get<ReviewListResult>({
    url: `/api/studio/skill-review/${skillRunId}`,
  });
}

export function submitSkillReview(data: {
  skillRunId: string;
  score: number;
  comment?: string;
  evalRunId?: number;
}) {
  return defHttp.post<{ ok: boolean; reviewId: number; verdict: string }>({
    url: '/api/studio/skill-review',
    data,
  });
}

// ─── Eval Pipeline（P7-E01）───

export interface LayerResult {
  passed: boolean;
  metric: string;
  warnings?: string[];
  elapsedMs?: number;
}

export interface EvalRunDetail {
  id: number;
  setId: number;
  caseId?: number;
  status: string;
  runAt: string;
  tenantId: string;
  projectId: string;
  pipelineId: string;
  overallPassed?: boolean;
  judgeKappa?: number;
  consistency?: number;
  layerResults?: {
    l1?: LayerResult;
    l2?: LayerResult;
    l3?: LayerResult;
    l4?: LayerResult;
  };
}

export function getEvalRun(runId: number) {
  return defHttp.get<EvalRunDetail>({
    url: `/api/studio/eval/run/${runId}`,
  });
}

// ─── Judge 校准（P7-E02）───

export interface CalibrationReport {
  status: string; // trusted / untrusted / insufficient_samples
  kappa?: number;
  sampleCount: number;
  agreeCount: number;
  disagreeCount: number;
  recommendAction: string;
  calibratedAt?: string;
}

export function getJudgeCalibration(minSamples = 10) {
  return defHttp.get<CalibrationReport>({
    url: '/api/studio/eval/calibration',
    params: { minSamples },
  });
}
