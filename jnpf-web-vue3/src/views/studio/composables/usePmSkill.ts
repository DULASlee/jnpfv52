import { computed, ref, watch, type InjectionKey, type Ref } from 'vue';
import type { IrFragmentSnapshot } from '../types/ir';
import { confirmSkeleton, runPmSkill, type SkillRunResult } from '../api/studio/skills';

export const PM_SKILL_KEY: InjectionKey<ReturnType<typeof usePmSkill>> = Symbol('pmSkill');

export function usePmSkill(pipelineId: Ref<number>, snapshots: Ref<IrFragmentSnapshot[]>, refreshAll: () => Promise<void>) {
  const pmLoading = ref(false);
  const confirmLoading = ref(false);
  let abortController: AbortController | null = null;

  const skeletonSnapshot = computed(() => snapshots.value.find(s => s.fragmentType === 'IR0_Skeleton' || s.fragmentId?.startsWith('skeleton:')));

  const needsConfirmation = computed(() => !!skeletonSnapshot.value && skeletonSnapshot.value.stabilityState === 'draft');

  const isSkeletonStable = computed(() => skeletonSnapshot.value?.stabilityState === 'stable' || skeletonSnapshot.value?.stabilityState === 'locked');

  watch(pipelineId, () => {
    abortController?.abort();
    abortController = null;
    pmLoading.value = false;
    confirmLoading.value = false;
  });

  async function runPm(): Promise<SkillRunResult | null> {
    if (!pipelineId.value || pmLoading.value) return null;
    pmLoading.value = true;
    abortController = new AbortController();
    try {
      const res = await runPmSkill(pipelineId.value);
      await refreshAll();
      return res;
    } finally {
      pmLoading.value = false;
      abortController = null;
    }
  }

  async function confirmAndProceed(autoRunAnalyst = true): Promise<void> {
    if (!pipelineId.value || confirmLoading.value) return;
    confirmLoading.value = true;
    try {
      // 后端 AutoRunAnalyst=true 时调度 requirement-analysis（非旧 analyst-skill）
      await confirmSkeleton(pipelineId.value, { autoRunAnalyst });
      await refreshAll();
    } finally {
      confirmLoading.value = false;
    }
  }

  return {
    pmLoading,
    confirmLoading,
    skeletonSnapshot,
    needsConfirmation,
    isSkeletonStable,
    runPm,
    confirmAndProceed,
  };
}
