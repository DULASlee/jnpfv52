/** IR 观测台类型（阶段一 P1） */

export interface IrEventRecord {
  eventId: string;
  eventType: string;
  fragmentId?: string;
  fragmentType?: string;
  fragmentVersion: number;
  skillId?: string;
  saStepName?: string;
  createdAt: string;
  payloadPreview?: string;
}

/** IR-3 片段类型（与后端 IrFragmentTypes 对齐） */
export const IR3_FRAGMENT_TYPES = ['IR3_GeneratedCode', 'IR3_ArchReport', 'IR3_TestSuite'] as const;

/** 触发 IR-3 Tab 刷新的 SSE 事件类型 */
export const IR3_RELEVANT_EVENT_TYPES = [
  'CodeGenerated',
  'CodegenFailed',
  'CodegenBuildValidated',
  'CodeGeneratedStablePromoted',
  'ArchViolationDetected',
  'DeveloperSkillCompleted',
  'TestSuiteGenerated',
  'TesterSkillCompleted',
] as const;

export type IrStabilityState = 'draft' | 'in-progress' | 'stable' | 'locked' | 'invalidated';

export interface IrFragmentSnapshot {
  fragmentId: string;
  fragmentType: string;
  stabilityState: IrStabilityState;
  currentVersion: number;
  saStepsCompleted?: string[];
  payload?: unknown;
  updatedAt?: string;
}

export interface IrProjectDiagnostics {
  pipelineId: number;
  projectId?: string;
  tenantId?: string;
  routeTable?: Array<{ path: string; target: string }>;
  workspacePath?: string;
  eventCount?: number;
  snapshotCount?: number;
  lastRebuild?: IrRebuildResult;
}

export interface IrRebuildResult {
  eventCount: number;
  fragmentCount: number;
  elapsedMs: number;
  passedPerformanceGate?: boolean | null;
}

export interface IrStabilityStatus {
  fragmentId: string;
  fragmentType: string;
  stabilityState: IrFragmentSnapshot['stabilityState'];
  saStepsCompleted: string[];
  requiredSteps: number;
  completedCount: number;
  isStable: boolean;
}

export interface SseIrEventPayload {
  eventId: string;
  eventType: string;
  fragmentId?: string;
  fragmentType?: string;
  fragmentVersion: number;
  skillId?: string;
  saStepName?: string;
  createdAt: string;
  payloadPreview: string;
}

export interface SseFragmentUpdatedPayload {
  fragmentId: string;
  fragmentType: string;
  stabilityState: IrFragmentSnapshot['stabilityState'];
  currentVersion: number;
  saStepsCompleted?: string[];
}

export interface SseSkillProgressPayload {
  skillId: 'pm-skill' | 'analyst-skill' | 'architect-skill' | 'db-design-skill' | 'ui-design-skill' | 'system-design-skill' | string;
  runId?: string;
  phase: string;
  eventId?: string;
  saStepName?: string;
  percent: number;
  message: string;
  code?: string;
}

export interface SseAnalysisCompletedPayload {
  projectId: string;
  eventSpecCount: number;
  allStable: boolean;
}

export interface ConstraintViolation {
  ruleId: string;
  severity: 'critical' | 'warning' | string;
  message: string;
  fragmentType?: string;
  fragmentId?: string;
}

export interface ConstraintCheckResult {
  violations: ConstraintViolation[];
  criticalCount: number;
  warningCount: number;
  passed: boolean;
  eventAppended?: boolean;
}

export type SimulateEventType = 'SkeletonCreated' | 'SA_Step_Completed' | 'EventSpecRevised';
