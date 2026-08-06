import { computed, ref, watch, type InjectionKey, type Ref } from 'vue';
import { message } from 'ant-design-vue';
import type { IrFragmentSnapshot, SseSkillProgressPayload } from '../types/ir';
import {
  DEPLOY_SKILL_ID,
  DEVELOPER_SKILL_ID,
  getDeveloperStatus,
  runDeploySkill,
  runDeveloperOrchestrator,
  TESTER_SKILL_ID,
  type DeveloperOrchestratorStatus,
} from '../api/studio/developerSkills';

export const DEVELOPER_SKILL_KEY: InjectionKey<ReturnType<typeof useDeveloperSkill>> = Symbol('developerSkill');

const DEV_SKILL_SET = new Set<string>([DEVELOPER_SKILL_ID, TESTER_SKILL_ID, DEPLOY_SKILL_ID]);

export const IR3_FRAGMENT_TYPES = ['IR3_GeneratedCode', 'IR3_ArchReport', 'IR3_TestSuite'] as const;

function unwrap<T>(res: T | { data?: T }): T {
  return (res as { data?: T })?.data ?? (res as T);
}

export function useDeveloperSkill(pipelineId: Ref<number>, snapshots: Ref<IrFragmentSnapshot[]>, refreshAll: () => Promise<void>) {
  const developerLoading = ref(false);
  const deployLoading = ref(false);
  const statusLoading = ref(false);
  const orchestratorStatus = ref<DeveloperOrchestratorStatus | null>(null);
  const skillProgress = ref<Record<string, SseSkillProgressPayload>>({});
  const lastError = ref<string | null>(null);
  let pollTimer: ReturnType<typeof setInterval> | null = null;

  const ir3Snapshots = computed(() => snapshots.value.filter(s => IR3_FRAGMENT_TYPES.includes(s.fragmentType as (typeof IR3_FRAGMENT_TYPES)[number])));

  const codegenStable = computed(() =>
    snapshots.value.some(s => s.fragmentType === 'IR3_GeneratedCode' && (s.stabilityState === 'stable' || s.stabilityState === 'locked')),
  );

  const testSuiteReady = computed(() => snapshots.value.some(s => s.fragmentType === 'IR3_TestSuite'));

  const canRunDeveloper = computed(() => pipelineId.value > 0 && !developerLoading.value);

  watch(pipelineId, () => {
    stopPolling();
    developerLoading.value = false;
    deployLoading.value = false;
    orchestratorStatus.value = null;
    skillProgress.value = {};
    lastError.value = null;
  });

  function stopPolling() {
    if (pollTimer) {
      clearInterval(pollTimer);
      pollTimer = null;
    }
  }

  function startPolling() {
    stopPolling();
    pollTimer = setInterval(() => void loadStatus(), 4000);
  }

  async function loadStatus() {
    if (!pipelineId.value) return;
    statusLoading.value = true;
    try {
      orchestratorStatus.value = unwrap(await getDeveloperStatus(pipelineId.value));
    } catch {
      orchestratorStatus.value = null;
    } finally {
      statusLoading.value = false;
    }
  }

  async function runDeveloper(): Promise<boolean> {
    if (!canRunDeveloper.value) return false;
    developerLoading.value = true;
    lastError.value = null;
    try {
      await runDeveloperOrchestrator(pipelineId.value);
      startPolling();
      await Promise.all([refreshAll(), loadStatus()]);
      message.success('自动交付链已启动（代码生成 → 测试 → 部署），请关注进度提示');
      return true;
    } catch (e: any) {
      lastError.value = e?.response?.data?.msg ?? e?.message ?? '开发 Skill 启动失败';
      message.error(lastError.value);
      return false;
    } finally {
      developerLoading.value = false;
    }
  }

  async function runDeploy(): Promise<boolean> {
    if (!pipelineId.value || deployLoading.value) return false;
    deployLoading.value = true;
    lastError.value = null;
    try {
      await runDeploySkill(pipelineId.value);
      startPolling();
      await refreshAll();
      message.success('部署 Skill 已启动');
      return true;
    } catch (e: any) {
      lastError.value = e?.response?.data?.msg ?? e?.message ?? '部署 Skill 启动失败';
      message.error(lastError.value);
      return false;
    } finally {
      deployLoading.value = false;
    }
  }

  function handleSkillProgress(payload: SseSkillProgressPayload) {
    if (!payload.skillId || !DEV_SKILL_SET.has(payload.skillId)) return;
    skillProgress.value = { ...skillProgress.value, [payload.skillId]: payload };

    const inFlight = payload.phase === 'running' || payload.phase === 'reason';
    if (inFlight) {
      // 自动交付链：任一环节运行中都保持 loading，避免用户以为卡住
      developerLoading.value = true;
      deployLoading.value = payload.skillId === DEPLOY_SKILL_ID;
    }

    if (payload.phase === 'completed' || payload.phase === 'failed' || payload.phase === 'aborted') {
      void refreshAll();
      void loadStatus();
      if (payload.skillId === DEPLOY_SKILL_ID || (payload.percent ?? 0) >= 100) {
        developerLoading.value = false;
        deployLoading.value = false;
        stopPolling();
      } else if (payload.phase === 'failed' || payload.phase === 'aborted') {
        developerLoading.value = false;
        deployLoading.value = false;
      }
    }
  }

  return {
    DEVELOPER_SKILL_ID,
    TESTER_SKILL_ID,
    DEPLOY_SKILL_ID,
    developerLoading,
    deployLoading,
    statusLoading,
    orchestratorStatus,
    skillProgress,
    lastError,
    ir3Snapshots,
    codegenStable,
    testSuiteReady,
    canRunDeveloper,
    loadStatus,
    runDeveloper,
    runDeploy,
    handleSkillProgress,
    stopPolling,
  };
}
