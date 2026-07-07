import { computed, ref, shallowRef, type InjectionKey, type Ref } from 'vue';
import type {
  IrEventRecord,
  IrFragmentSnapshot,
  IrProjectDiagnostics,
  IrRebuildResult,
  SimulateEventType,
  SseFragmentUpdatedPayload,
  SseIrEventPayload,
} from '../types/ir';
import { IR3_FRAGMENT_TYPES, IR3_RELEVANT_EVENT_TYPES } from '../types/ir';
import { getIrDiagnostics, getIrEvents, getIrSnapshotAtVersion, getIrSnapshots, getIrStability, rebuildIrProject, simulateIrEvent } from '../api/studio/ir';

export interface IrObservatoryContext {
  pipelineId: Ref<number>;
  events: Ref<IrEventRecord[]>;
  snapshots: Ref<IrFragmentSnapshot[]>;
  diagnostics: Ref<IrProjectDiagnostics | null>;
  loading: Ref<boolean>;
  connected: Ref<boolean>;
  error: Ref<string | null>;
  /** IR-3 事件到达时建议切换的观测台 Tab（如 ir3） */
  preferredObservatoryTab: Ref<string | null>;
  /** SSE 心跳时间戳（R1 30s ping） */
  lastHeartbeat: Ref<string | null>;
  /** 右侧观测台折叠（窄屏默认折叠） */
  panelCollapsed: Ref<boolean>;
  togglePanel: () => void;
  /** @deprecated 兼容旧调用，等同 togglePanel */
  drawerOpen: Ref<boolean>;
  toggleDrawer: () => void;
  setPipelineId: (id: number) => void;
  loadEvents: () => Promise<void>;
  loadSnapshots: () => Promise<void>;
  loadDiagnostics: () => Promise<void>;
  refreshAll: () => Promise<void>;
  onIrEvent: (payload: SseIrEventPayload) => void;
  onFragmentUpdated: (payload: SseFragmentUpdatedPayload) => void;
  onIr3PipelineEvent: (payload: SseIrEventPayload) => void;
  onSseHeartbeat: () => void;
  simulate: (eventType: SimulateEventType, extra?: { saStepName?: string; useInvalidPayload?: boolean }) => Promise<void>;
  simulateAllSaSteps: () => Promise<void>;
  rebuildProject: () => Promise<IrRebuildResult | null>;
}

export const IR_OBSERVATORY_KEY: InjectionKey<IrObservatoryContext> = Symbol('irObservatory');

export function useIrObservatory(): IrObservatoryContext {
  const pipelineId = ref(0);
  const events = ref<IrEventRecord[]>([]);
  const snapshots = ref<IrFragmentSnapshot[]>([]);
  const diagnostics = shallowRef<IrProjectDiagnostics | null>(null);
  const loading = ref(false);
  const connected = ref(false);
  const error = ref<string | null>(null);
  const preferredObservatoryTab = ref<string | null>(null);
  const lastHeartbeat = ref<string | null>(null);
  const panelCollapsed = ref(false);

  function togglePanel() {
    panelCollapsed.value = !panelCollapsed.value;
  }

  function toggleDrawer() {
    togglePanel();
  }

  const ir3EventTypeSet = new Set<string>(IR3_RELEVANT_EVENT_TYPES);

  function isIr3FragmentType(fragmentType?: string) {
    return !!fragmentType && (IR3_FRAGMENT_TYPES as readonly string[]).includes(fragmentType);
  }

  function isIr3RelevantEvent(payload: SseIrEventPayload) {
    return isIr3FragmentType(payload.fragmentType) || ir3EventTypeSet.has(payload.eventType);
  }

  async function hydrateIr3SnapshotPayload(fragmentId: string, version: number) {
    if (!pipelineId.value) return;
    try {
      const res = await getIrSnapshotAtVersion(pipelineId.value, fragmentId, version);
      const data = (res as any)?.data ?? res;
      const payload = data?.payload;
      if (payload == null) return;
      const idx = snapshots.value.findIndex(s => s.fragmentId === fragmentId);
      if (idx >= 0) {
        snapshots.value[idx] = { ...snapshots.value[idx], payload };
      }
    } catch {
      /* 后端未就绪或版本尚未投影 */
    }
  }

  function setPipelineId(id: number) {
    if (pipelineId.value === id) return;
    pipelineId.value = id;
    events.value = [];
    snapshots.value = [];
    diagnostics.value = null;
    error.value = null;
    if (id > 0) {
      void refreshAll();
    }
  }

  async function loadEvents() {
    if (!pipelineId.value) return;
    try {
      const res = await getIrEvents(pipelineId.value);
      events.value = Array.isArray(res) ? res : (res as any)?.data ?? [];
    } catch (e: any) {
      // API 未就绪时保持空态，不阻断页面
      if (e?.response?.status !== 404) {
        error.value = e?.message || '加载事件失败';
      }
    }
  }

  async function loadSnapshots() {
    if (!pipelineId.value) return;
    try {
      const res = await getIrSnapshots(pipelineId.value);
      snapshots.value = Array.isArray(res) ? res : (res as any)?.data ?? [];
    } catch {
      /* 后端未就绪 */
    }
  }

  async function loadDiagnostics() {
    if (!pipelineId.value) return;
    try {
      const res = await getIrDiagnostics(pipelineId.value);
      const data = (res as any)?.data ?? res ?? null;
      if (data) {
        diagnostics.value = {
          pipelineId: data.pipelineId ?? pipelineId.value,
          projectId: data.projectId,
          tenantId: data.tenantId,
          workspacePath: data.workspacePath,
          routeTable: data.routeTable,
          eventCount: data.eventCount,
          snapshotCount: data.snapshotCount,
          lastRebuild: data.lastRebuild,
        };
      }
    } catch {
      /* 后端未就绪 */
    }
  }

  async function refreshAll() {
    if (!pipelineId.value) return;
    loading.value = true;
    error.value = null;
    try {
      await Promise.all([loadEvents(), loadSnapshots(), loadDiagnostics()]);
    } finally {
      loading.value = false;
    }
  }

  function onIrEvent(payload: SseIrEventPayload) {
    connected.value = true;
    const exists = events.value.some(e => e.eventId === payload.eventId);
    if (!exists) {
      events.value = [payload as IrEventRecord, ...events.value];
    }
  }

  function onFragmentUpdated(payload: SseFragmentUpdatedPayload) {
    connected.value = true;
    const idx = snapshots.value.findIndex(s => s.fragmentId === payload.fragmentId);
    const prev = idx >= 0 ? snapshots.value[idx] : undefined;
    const next: IrFragmentSnapshot = {
      fragmentId: payload.fragmentId,
      fragmentType: payload.fragmentType,
      stabilityState: payload.stabilityState,
      currentVersion: payload.currentVersion,
      saStepsCompleted: payload.saStepsCompleted,
      payload: prev?.payload,
      updatedAt: prev?.updatedAt,
    };
    if (idx >= 0) {
      snapshots.value[idx] = { ...prev!, ...next };
    } else {
      snapshots.value = [...snapshots.value, next];
    }

    if (isIr3FragmentType(payload.fragmentType)) {
      preferredObservatoryTab.value = 'ir3';
      const needsPayload = !prev?.payload || prev.currentVersion !== payload.currentVersion;
      if (needsPayload) {
        void hydrateIr3SnapshotPayload(payload.fragmentId, payload.currentVersion);
      }
    }
  }

  function onIr3PipelineEvent(payload: SseIrEventPayload) {
    if (!isIr3RelevantEvent(payload)) return;
    connected.value = true;
    preferredObservatoryTab.value = 'ir3';
    void loadSnapshots();
  }

  async function simulate(eventType: SimulateEventType, extra?: { saStepName?: string; useInvalidPayload?: boolean }) {
    if (!pipelineId.value) {
      error.value = '请先发送消息创建流水线';
      return;
    }
    loading.value = true;
    error.value = null;
    try {
      const record = await simulateIrEvent(pipelineId.value, eventType, extra);
      const evt = (record as any)?.data ?? record;
      if (evt?.eventId) {
        onIrEvent(evt);
      } else {
        await loadEvents();
      }
      await Promise.all([loadSnapshots(), loadStability()]);
    } catch (e: any) {
      const msg = e?.response?.data?.msg ?? e?.message ?? '模拟事件失败';
      error.value = msg;
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function loadStability() {
    if (!pipelineId.value) return;
    try {
      await getIrStability(pipelineId.value);
    } catch {
      /* optional */
    }
  }

  async function simulateAllSaSteps() {
    const steps = [
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
    for (const step of steps) {
      await simulate('SA_Step_Completed', { saStepName: step });
    }
  }

  async function rebuildProject() {
    if (!pipelineId.value) return null;
    loading.value = true;
    error.value = null;
    try {
      const res = await rebuildIrProject(pipelineId.value);
      const result = (res as any)?.data ?? res;
      if (diagnostics.value) {
        diagnostics.value = { ...diagnostics.value, lastRebuild: result };
      }
      await Promise.all([loadSnapshots(), loadEvents()]);
      return result as IrRebuildResult;
    } catch (e: any) {
      error.value = e?.response?.data?.msg ?? e?.message ?? '投影重建失败';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function onSseHeartbeat() {
    connected.value = true;
    lastHeartbeat.value = new Date().toLocaleTimeString();
  }

  const drawerOpen = computed({
    get: () => !panelCollapsed.value,
    set: (v: boolean) => {
      panelCollapsed.value = !v;
    },
  });

  return {
    pipelineId,
    events,
    snapshots,
    diagnostics,
    loading,
    connected,
    error,
    preferredObservatoryTab,
    lastHeartbeat,
    panelCollapsed,
    togglePanel,
    drawerOpen,
    toggleDrawer,
    setPipelineId,
    loadEvents,
    loadSnapshots,
    loadDiagnostics,
    refreshAll,
    onIrEvent,
    onFragmentUpdated,
    onIr3PipelineEvent,
    onSseHeartbeat,
    simulate,
    simulateAllSaSteps,
    rebuildProject,
  };
}
