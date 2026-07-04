import { computed, ref, watch, type InjectionKey, type Ref } from 'vue';
import type { IrFragmentSnapshot } from '../types/ir';
import { runAnalystSkill, type SkillRunResult } from '../api/studio/skills';

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

export function useAnalystSkill(pipelineId: Ref<number>, snapshots: Ref<IrFragmentSnapshot[]>, refreshAll: () => Promise<void>) {
  const analystLoading = ref(false);
  const analysisCompleted = ref(false);
  let abortController: AbortController | null = null;

  watch(pipelineId, () => {
    abortController?.abort();
    abortController = null;
    analystLoading.value = false;
    analysisCompleted.value = false;
  });

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
      const res = await runAnalystSkill(pipelineId.value);
      await refreshAll();
      return res;
    } finally {
      analystLoading.value = false;
      abortController = null;
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
    eventProgressList,
    totalProgress,
    runAnalyst,
    markAnalysisCompleted,
    handleSkillProgress,
  };
}
