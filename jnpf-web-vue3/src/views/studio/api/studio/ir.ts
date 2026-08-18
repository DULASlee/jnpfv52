/**
 * IR 观测台 REST API（阶段一 P1）
 */
import { defHttp } from '/@/utils/http/axios';
import type {
  IrEventRecord,
  IrFragmentSnapshot,
  IrProjectDiagnostics,
  IrRebuildResult,
  IrStabilityStatus,
  SimulateEventType,
  ConstraintCheckResult,
} from '../../types/ir';

export function getIrEvents(pipelineId: number) {
  return defHttp.get<IrEventRecord[]>({ url: `/api/studio/ir/${pipelineId}/events` });
}

export function getIrSnapshots(pipelineId: number) {
  return defHttp.get<IrFragmentSnapshot[]>({ url: `/api/studio/ir/${pipelineId}/snapshots` });
}

export function getIrSnapshotAtVersion(pipelineId: number, fragmentId: string, version?: number) {
  return defHttp.get<IrFragmentSnapshot>({
    url: `/api/studio/ir/${pipelineId}/snapshots/${encodeURIComponent(fragmentId)}`,
    params: version != null ? { version } : undefined,
  });
}

export function getIrStability(pipelineId: number) {
  return defHttp.get<IrStabilityStatus>({ url: `/api/studio/ir/${pipelineId}/stability` });
}

export function getIrDiagnostics(pipelineId: number) {
  return defHttp.get<IrProjectDiagnostics>({ url: `/api/studio/ir/${pipelineId}/diagnostics` });
}

export function rebuildIrProject(pipelineId: number) {
  return defHttp.post<IrRebuildResult>({ url: `/api/studio/ir/${pipelineId}/rebuild` });
}

export function simulateIrEvent(pipelineId: number, eventType: SimulateEventType, extra?: { saStepName?: string; useInvalidPayload?: boolean }) {
  return defHttp.post<IrEventRecord>({
    url: `/api/studio/ir/${pipelineId}/simulate`,
    data: { eventType, ...extra },
  });
}

/** ConstraintEngine 手动校验（P3-B06） */
export function checkIrConstraints(pipelineId: number, persist = true) {
  return defHttp.post<ConstraintCheckResult>({
    url: `/api/studio/ir/${pipelineId}/constraints/check`,
    data: { persist },
  });
}
