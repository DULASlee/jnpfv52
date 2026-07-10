import { computed, ref, watch, type InjectionKey, type Ref } from 'vue';
import type { IrEventRecord, IrFragmentSnapshot } from '../types/ir';
import { confirmRequirementSpec, runRequirementAnalysis, type SkillRunResult } from '../api/studio/skills';

export interface EventSaProgress {
  eventId: string;
  fragmentId: string;
  completedSteps: string[];
  percent: number;
  isStable: boolean;
}

export const ANALYST_SKILL_KEY: InjectionKey<ReturnType<typeof useAnalystSkill>> = Symbol('analystSkill');

const SA_STEPS = [
  'DomainModel',
  'AggregateDesign',
  'EventCatalog',
  'CommandQuery',
  'IntegrationPoints',
  'WorkflowSpec',
  'UISpec',
  'DataModel',
  'DeliveryChecklist',
];

function isS2StageConfirmed(events: IrEventRecord[]) {
  return events.some(e => {
    if (e.eventType !== 'StageConfirmed') return false;
    try {
      const raw = e.payloadPreview ?? '';
      const p = typeof raw === 'string' && raw.startsWith('{') ? JSON.parse(raw) : { stage: raw };
      return p?.stage === 'S2';
    } catch {
      return false;
    }
  });
}

export function useAnalystSkill(pipelineId: Ref<number>, snapshots: Ref<IrFragmentSnapshot[]>, refreshAll: () => Promise<void>, events?: Ref<IrEventRecord[]>) {
  const analystLoading = ref(false);
  const analysisCompleted = ref(false);
  const confirmLoading = ref(false);
  let abortController: AbortController | null = null;

  watch(pipelineId, () => {
    abortController?.abort();
    abortController = null;
    analystLoading.value = false;
    analysisCompleted.value = false;
    confirmLoading.value = false;
  });

  const s2Confirmed = computed(() => (events?.value ? isS2StageConfirmed(events.value) : false));

  const needsRequirementSpecConfirmation = computed(() => analysisCompleted.value && !s2Confirmed.value);

  const eventProgressList = computed<EventSaProgress[]>(() => {
    const specs = snapshots.value.filter(s => s.fragmentType === 'IR1_EventSpec' || s.fragmentId?.startsWith('eventspec:'));
    return specs.map(snap => {
      const eventId = snap.fragmentId?.replace(/^eventspec:/, '') ?? snap.fragmentId;
      const completed = snap.saStepsCompleted ?? [];
      return {
        eventId,
        fragmentId: snap.fragmentId,
        completedSteps: completed,
        percent: Math.round((completed.length / SA_STEPS.length) * 100),
        isStable: snap.stabilityState === 'stable' || snap.stabilityState === 'locked',
      };
    });
  });

  const totalProgress = computed(() => {
    if (eventProgressList.value.length === 0) return 0;
    const sum = eventProgressList.value.reduce((acc, e) => acc + e.percent, 0);
    return Math.round(sum / eventProgressList.value.length);
  });

  async function runAnalyst(): Promise<SkillRunResult | null> {
    if (!pipelineId.value || analystLoading.value) return null;
    analystLoading.value = true;
    abortController = new AbortController();
    try {
      const res = await runRequirementAnalysis(pipelineId.value);
      await refreshAll();
      return res;
    } finally {
      analystLoading.value = false;
      abortController = null;
    }
  }

  async function confirmAndProceed(autoRunDesign = true): Promise<void> {
    if (!pipelineId.value || confirmLoading.value) return;
    confirmLoading.value = true;
    try {
      await confirmRequirementSpec(pipelineId.value, { autoRunDesign });
      await refreshAll();
    } finally {
      confirmLoading.value = false;
    }
  }

  function markAnalysisCompleted() {
    analysisCompleted.value = true;
    analystLoading.value = false;
  }

  function handleSkillProgress(payload: { skillId?: string; phase?: string; percent?: number; message?: string }) {
    if (payload.skillId === 'analyst-skill') {
      analystLoading.value = payload.phase !== 'completed' && payload.phase !== 'failed';
    }
  }

  return {
    SA_STEPS,
    analystLoading,
    analysisCompleted,
    confirmLoading,
    s2Confirmed,
    needsRequirementSpecConfirmation,
    eventProgressList,
    totalProgress,
    runAnalyst,
    confirmAndProceed,
    markAnalysisCompleted,
    handleSkillProgress,
  };
}
