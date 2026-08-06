import { computed, onUnmounted, ref, watch, type InjectionKey, type Ref } from 'vue';
import { message } from 'ant-design-vue';
import type { ConstraintCheckResult, ConstraintViolation, IrFragmentSnapshot, SseSkillProgressPayload } from '../types/ir';
import { checkIrConstraints } from '../api/studio/ir';
import {
  DESIGN_SKILL_IDS,
  getDesignStatus,
  getLlmBudget,
  normalizeDesignOrchestratorStatus,
  normalizeLlmBudgetInfo,
  runDesignOrchestrator,
  type DesignOrchestratorStatus,
  type DesignSkillPhaseStatus,
  type LlmBudgetInfo,
} from '../api/studio/designSkills';

export const DESIGN_SKILL_KEY: InjectionKey<ReturnType<typeof useDesignSkills>> = Symbol('designSkill');

const DESIGN_SKILL_SET = new Set<string>(Object.values(DESIGN_SKILL_IDS));

export const IR2_FRAGMENT_TYPES = ['IR2_Architecture', 'IR2_DDL', 'IR2_FormPageIR', 'IR2_SystemDesign'] as const;

function unwrap<T>(res: T | { data?: T }): T {
  return (res as { data?: T })?.data ?? (res as T);
}

function normalizeConstraintResult(raw: ConstraintCheckResult & Record<string, unknown>): ConstraintCheckResult {
  return {
    violations: (raw.violations ?? raw.Violations ?? []) as ConstraintViolation[],
    criticalCount: (raw.criticalCount ?? raw.CriticalCount ?? 0) as number,
    warningCount: (raw.warningCount ?? raw.WarningCount ?? 0) as number,
    passed: (raw.passed ?? raw.Passed ?? true) as boolean,
    eventAppended: (raw.eventAppended ?? raw.EventAppended) as boolean | undefined,
  };
}

export function notifyConstraintViolations(result: ConstraintCheckResult, silentPass = false) {
  if (result.criticalCount > 0) {
    message.error(`约束违规：${result.criticalCount} 条 critical`);
    result.violations
      .filter(v => (v.severity ?? (v as any).Severity) === 'critical')
      .slice(0, 3)
      .forEach(v => {
        const ruleId = v.ruleId ?? (v as any).RuleId;
        const msg = v.message ?? (v as any).Message;
        message.error(`[${ruleId}] ${msg}`, 4);
      });
    return;
  }

  if (result.warningCount > 0) {
    message.warning(`约束提示：${result.warningCount} 条 warning`);
    return;
  }

  if (!silentPass) {
    message.success('约束校验通过');
  }
}

export function parseConstraintPayload(payloadPreview?: string): ConstraintCheckResult | null {
  if (!payloadPreview) return null;
  try {
    const raw = JSON.parse(payloadPreview) as Record<string, unknown>;
    const violations =
      ((raw.violations ?? raw.Violations) as Array<Record<string, string>> | undefined)?.map(v => ({
        ruleId: v.ruleId ?? v.RuleId ?? '',
        severity: v.severity ?? v.Severity ?? 'warning',
        message: v.message ?? v.Message ?? '',
        fragmentType: v.fragmentType ?? v.FragmentType,
        fragmentId: v.fragmentId ?? v.FragmentId,
      })) ?? [];
    return {
      violations,
      criticalCount: Number(raw.criticalCount ?? raw.CriticalCount ?? 0),
      warningCount: Number(raw.warningCount ?? raw.WarningCount ?? 0),
      passed: Number(raw.criticalCount ?? raw.CriticalCount ?? 0) === 0,
    };
  } catch {
    return null;
  }
}

export function useDesignSkills(pipelineId: Ref<number>, snapshots: Ref<IrFragmentSnapshot[]>, refreshAll: () => Promise<void>) {
  const designLoading = ref(false);
  const statusLoading = ref(false);
  const budgetLoading = ref(false);
  const orchestratorStatus = ref<DesignOrchestratorStatus | null>(null);
  const budgetInfo = ref<LlmBudgetInfo | null>(null);
  const lastError = ref<string | null>(null);
  const skillProgress = ref<Record<string, SseSkillProgressPayload>>({});
  const violations = ref<ConstraintViolation[]>([]);
  const constraintCritical = ref(0);
  const constraintWarning = ref(0);
  const constraintChecking = ref(false);

  let pollTimer: ReturnType<typeof setInterval> | null = null;
  let abortController: AbortController | null = null;

  const projectId = computed(() => (pipelineId.value > 0 ? String(pipelineId.value) : ''));

  const ir1Stable = computed(() =>
    snapshots.value.some(
      s => (s.fragmentType === 'IR1_EventSpec' || s.fragmentId?.startsWith('eventspec:')) && (s.stabilityState === 'stable' || s.stabilityState === 'locked'),
    ),
  );

  /** 25 §6：优先用后端 status；未拉到 status 时回退 ir1Stable（兼容旧 pipeline） */
  const analysisFinalized = computed(() => orchestratorStatus.value?.analysisFinalized ?? ir1Stable.value);
  const hasEntityFields = computed(() => orchestratorStatus.value?.hasEntityFields ?? ir1Stable.value);
  const qualityGatePasses = computed(() => orchestratorStatus.value?.qualityGatePasses ?? true);
  const qualityCriticalCount = computed(() => orchestratorStatus.value?.qualityCriticalCount ?? 0);
  const qualityTotalScore = computed(() => orchestratorStatus.value?.qualityTotalScore ?? null);

  const ir2Snapshots = computed(() => snapshots.value.filter(s => IR2_FRAGMENT_TYPES.includes(s.fragmentType as (typeof IR2_FRAGMENT_TYPES)[number])));

  const designComplete = computed(
    () => orchestratorStatus.value?.designComplete || snapshots.value.some(s => s.fragmentType === 'IR2_SystemDesign' && s.stabilityState === 'locked'),
  );

  const canRunDesign = computed(() => {
    const gateOk =
      orchestratorStatus.value?.canRunDesign ??
      (analysisFinalized.value && hasEntityFields.value && qualityGatePasses.value);
    return gateOk && !designLoading.value && (budgetInfo.value?.canRunDesign ?? true);
  });

  const hasConstraintIssues = computed(() => constraintCritical.value > 0 || constraintWarning.value > 0);

  const phases = computed<DesignSkillPhaseStatus[]>(() => {
    if (orchestratorStatus.value?.phases?.length) return orchestratorStatus.value.phases;
    return [
      { skillId: DESIGN_SKILL_IDS.architect, phase: 'pending' },
      { skillId: DESIGN_SKILL_IDS.dbDesign, phase: 'pending' },
      { skillId: DESIGN_SKILL_IDS.uiDesign, phase: 'pending' },
      { skillId: DESIGN_SKILL_IDS.systemDesign, phase: 'pending' },
    ];
  });

  function stopPolling() {
    if (pollTimer) {
      clearInterval(pollTimer);
      pollTimer = null;
    }
  }

  async function loadStatus() {
    if (!pipelineId.value) return;
    statusLoading.value = true;
    try {
      const res = await getDesignStatus(pipelineId.value);
      const data = normalizeDesignOrchestratorStatus(unwrap(res) as unknown as Record<string, unknown>);
      orchestratorStatus.value = data;
      if (data.constraintCriticalCount != null) constraintCritical.value = data.constraintCriticalCount;
      if (data.constraintWarningCount != null) constraintWarning.value = data.constraintWarningCount;
    } catch {
      /* API 未就绪时静默 */
    } finally {
      statusLoading.value = false;
    }
  }

  async function loadBudget() {
    // R12：预算按真实 ProjectId 查（勿用 pipelineId 冒充）
    const pid = orchestratorStatus.value?.projectId || projectId.value;
    if (!pid) return;
    budgetLoading.value = true;
    try {
      const res = await getLlmBudget(pid);
      budgetInfo.value = normalizeLlmBudgetInfo(unwrap(res) as unknown as Record<string, unknown>);
    } catch {
      /* 后端未迁移时静默 */
    } finally {
      budgetLoading.value = false;
    }
  }

  async function refreshDesignContext() {
    await loadStatus();
    await loadBudget();
  }

  function startPolling() {
    stopPolling();
    pollTimer = setInterval(() => {
      void loadStatus();
      void loadBudget();
    }, 3000);
  }

  watch(pipelineId, () => {
    abortController?.abort();
    abortController = null;
    designLoading.value = false;
    orchestratorStatus.value = null;
    budgetInfo.value = null;
    skillProgress.value = {};
    lastError.value = null;
    violations.value = [];
    constraintCritical.value = 0;
    constraintWarning.value = 0;
    stopPolling();
    if (pipelineId.value > 0) {
      void refreshDesignContext();
    }
  });

  watch(designLoading, loading => {
    if (loading) startPolling();
    else stopPolling();
  });

  watch(orchestratorStatus, s => {
    if (!s || !designLoading.value) return;
    if (s.designComplete) {
      designLoading.value = false;
      void checkConstraints(true, true);
      return;
    }
    if ((s.constraintCriticalCount ?? 0) > 0) {
      designLoading.value = false;
      void checkConstraints(false, true);
      return;
    }
    const terminal = s.phases?.every(p => p.phase === 'stable' || p.phase === 'failed' || p.phase === 'completed');
    if (terminal && s.phases?.length) {
      designLoading.value = false;
      if (ir2Snapshots.value.length >= 3) void checkConstraints(true, true);
    }
  });

  onUnmounted(() => {
    stopPolling();
    abortController?.abort();
  });

  async function checkConstraints(persist = true, showToast = true): Promise<ConstraintCheckResult | null> {
    if (!pipelineId.value) return null;
    constraintChecking.value = true;
    try {
      const res = await checkIrConstraints(pipelineId.value, persist);
      const result = normalizeConstraintResult(unwrap(res) as ConstraintCheckResult & Record<string, unknown>);
      violations.value = result.violations;
      constraintCritical.value = result.criticalCount;
      constraintWarning.value = result.warningCount;
      if (showToast) notifyConstraintViolations(result, true);
      return result;
    } catch {
      return null;
    } finally {
      constraintChecking.value = false;
    }
  }

  function applyConstraintEvent(payloadPreview?: string) {
    const parsed = parseConstraintPayload(payloadPreview);
    if (!parsed) return;
    violations.value = parsed.violations;
    constraintCritical.value = parsed.criticalCount;
    constraintWarning.value = parsed.warningCount;
    notifyConstraintViolations(parsed);
  }

  function buildDesignBlockedMessage(status: DesignOrchestratorStatus | null): string {
    if (!status) return '无法读取设计门禁状态，请刷新后重试';
    if (status.designComplete) return '设计阶段已完成，无需重复启动';
    if (!status.analysisFinalized) return '需求分析尚未 Finalize，请先确认需求说明书';
    if (!status.hasEntityFields) return '实体字段尚未投影（ai_entity_field 为空），请先完成 Round 3 工程保障';
    if (!status.qualityGatePasses) {
      if ((status.qualityCriticalCount ?? 0) > 0)
        return `质量一致性存在 ${status.qualityCriticalCount} 条 CRITICAL，禁止启动设计`;
      return `质量门控未通过（总分 ${status.qualityTotalScore ?? '—'}，须≥60）`;
    }
    if (status.pmReviewGatePasses === false) {
      if (status.pmReviewScore != null && status.pmReviewScore > 0)
        return `PM 终评 ${status.pmReviewScore} 分（须≥85），请补充说明书或使用强制确认`;
      return 'PM 终评尚未通过，请先完成需求说明书确认';
    }
    if (budgetInfo.value?.canRunDesign === false) {
      const b = budgetInfo.value;
      const pct = b.tokenBudget > 0 ? ((b.tokenConsumed / b.tokenBudget) * 100).toFixed(1) : '—';
      return `LLM Token 预算不足（已用 ${b.tokenConsumed}/${b.tokenBudget}，${pct}%，须低于 95% 阈值 ${b.reserveThreshold}）`;
    }
    return '设计前置条件未满足（canRunDesign=false）';
  }

  async function runDesign(): Promise<boolean> {
    if (!pipelineId.value) return false;
    await refreshDesignContext();
    if (designComplete.value) return true;
    if (designLoading.value) return false;
    if (!canRunDesign.value) {
      const msg = buildDesignBlockedMessage(orchestratorStatus.value);
      lastError.value = msg;
      throw new Error(msg);
    }
    designLoading.value = true;
    lastError.value = null;
    abortController = new AbortController();
    try {
      await runDesignOrchestrator(pipelineId.value);
      startPolling();
      await Promise.all([refreshAll(), refreshDesignContext()]);
      return true;
    } catch (e: any) {
      designLoading.value = false;
      lastError.value = e?.response?.data?.msg ?? e?.message ?? '设计 Skill 启动失败';
      throw e;
    }
  }

  function handleSkillProgress(payload: SseSkillProgressPayload) {
    if (!payload.skillId || !DESIGN_SKILL_SET.has(payload.skillId)) return;

    skillProgress.value = {
      ...skillProgress.value,
      [payload.skillId]: payload,
    };

    if (payload.phase === 'running') {
      designLoading.value = true;
    }
    if (payload.phase === 'completed' || payload.phase === 'failed') {
      void refreshDesignContext();
      void refreshAll();
    }

    const allDone = [DESIGN_SKILL_IDS.architect, DESIGN_SKILL_IDS.dbDesign, DESIGN_SKILL_IDS.uiDesign].every(id => {
      const p = skillProgress.value[id]?.phase;
      return p === 'completed' || p === 'failed';
    });
    if (allDone && skillProgress.value[DESIGN_SKILL_IDS.systemDesign]?.phase === 'completed') {
      designLoading.value = false;
    }
  }

  function phaseForSkill(skillId: string): DesignSkillPhaseStatus['phase'] {
    const fromStatus = phases.value.find(p => p.skillId === skillId)?.phase;
    const fromSse = skillProgress.value[skillId]?.phase;
    if (fromSse === 'running' || fromSse === 'reason') return 'running';
    if (fromSse === 'failed') return 'failed';
    if (fromSse === 'completed') return fromStatus === 'stable' ? 'stable' : 'completed';
    return fromStatus ?? 'pending';
  }

  function skillLabel(skillId: string) {
    const map: Record<string, string> = {
      [DESIGN_SKILL_IDS.architect]: '架构',
      [DESIGN_SKILL_IDS.dbDesign]: '数据库',
      [DESIGN_SKILL_IDS.uiDesign]: 'UI',
      [DESIGN_SKILL_IDS.systemDesign]: '总体设计',
    };
    return map[skillId] ?? skillId;
  }

  return {
    DESIGN_SKILL_IDS,
    designLoading,
    statusLoading,
    budgetLoading,
    orchestratorStatus,
    budgetInfo,
    lastError,
    skillProgress,
    violations,
    constraintCritical,
    constraintWarning,
    constraintChecking,
    hasConstraintIssues,
    ir1Stable,
    analysisFinalized,
    hasEntityFields,
    qualityGatePasses,
    qualityCriticalCount,
    qualityTotalScore,
    ir2Snapshots,
    designComplete,
    canRunDesign,
    phases,
    loadStatus,
    loadBudget,
    refreshDesignContext,
    checkConstraints,
    applyConstraintEvent,
    buildDesignBlockedMessage,
    runDesign,
    handleSkillProgress,
    phaseForSkill,
    skillLabel,
  };
}
