/**
 * IREditPatch — 分治编辑（Surgical Edit）类型定义与引擎
 *
 * Agent 输出 JSON Patch 格式的编辑指令，applyPatch 逐条应用。
 * 支持 replace / add / remove 三种操作，每条记录 oldValue 用于回滚。
 * 纯原生 JavaScript 实现，零外部依赖。
 *
 * @module ir/edit-patch
 * @version 1.0.0 — P0 审判令二
 */

import type { AgentSlice } from '../ai/agents/agent-scope';
import type { FormPageIR } from './types';

// ============================================================
// 类型定义
// ============================================================

export interface IREditOperation {
  op: 'replace' | 'add' | 'remove';
  path: string;
  value?: unknown;
  oldValue?: unknown;
  reason: string;
}

export interface IREditPatch {
  targetNodeIds: string[];
  operations: IREditOperation[];
  explanation: string;
}

export interface ApplyPatchResult {
  result: FormPageIR;
  applied: IREditOperation[];
  failed: FailedOperation[];
}

export interface FailedOperation {
  operation: IREditOperation;
  failureReason: string;
}

// ============================================================
// 引擎函数
// ============================================================

export function applyPatch(ir: FormPageIR, patch: IREditPatch, slice: AgentSlice): ApplyPatchResult {
  const result = deepClone(ir);
  const applied: IREditOperation[] = [];
  const failed: FailedOperation[] = [];

  for (const op of patch.operations) {
    try {
      const nodeId = extractNodeId(op.path);
      if (nodeId && !slice.writableNodeIds.includes(nodeId)) {
        failed.push({
          operation: { ...op },
          failureReason: `节点 "${nodeId}" 不在 Agent 的可写范围内`,
        });
        continue;
      }

      const segments = parsePath(op.path);

      switch (op.op) {
        case 'replace': {
          const oldVal = getByPath(result, segments);
          if (oldVal === undefined || oldVal === null) {
            failed.push({ operation: { ...op }, failureReason: `路径 "${op.path}" 不存在` });
            continue;
          }
          const savedOld = deepClone(oldVal);
          setByPath(result, segments, deepClone(op.value));
          applied.push({ ...op, oldValue: savedOld });
          break;
        }
        case 'add': {
          setByPath(result, segments, deepClone(op.value));
          applied.push({ ...op });
          break;
        }
        case 'remove': {
          const oldVal = getByPath(result, segments);
          if (oldVal === undefined || oldVal === null) {
            failed.push({ operation: { ...op }, failureReason: `路径 "${op.path}" 不存在，无法删除` });
            continue;
          }
          deleteByPath(result, segments);
          applied.push({ ...op, oldValue: deepClone(oldVal) });
          break;
        }
        default:
          failed.push({ operation: { ...op }, failureReason: `不支持的操作类型: "${op.op}"` });
      }
    } catch (e) {
      failed.push({ operation: { ...op }, failureReason: (e as Error).message });
    }
  }

  return { result, applied, failed };
}

export function rollbackPatch(ir: FormPageIR, appliedOperations: IREditOperation[]): FormPageIR {
  const result = deepClone(ir);
  const reversed = [...appliedOperations].reverse();

  for (const op of reversed) {
    const segments = parsePath(op.path);
    if (op.op === 'add') {
      deleteByPath(result, segments);
    } else if (op.oldValue !== undefined && op.oldValue !== null) {
      setByPath(result, segments, deepClone(op.oldValue));
    }
  }

  return result;
}

// ============================================================
// 原生路径工具（零依赖）
// ============================================================

function parsePath(path: string): string[] {
  return path
    .replace(/^\$\./, '')
    .split(/\.|\[|\]\.?/)
    .filter(Boolean)
    .map(s => s.replace(/^"|"$/g, ''));
}

function resolveSegment(current: unknown, seg: string): unknown {
  if (current === null || current === undefined) return undefined;
  // 如果当前值是数组，按 id 字段查找
  if (Array.isArray(current)) {
    return (current as Array<Record<string, unknown>>).find(item => item.id === seg);
  }
  return (current as Record<string, unknown>)[seg];
}

function getByPath(obj: unknown, segments: string[]): unknown {
  let current = obj;
  for (const seg of segments) {
    current = resolveSegment(current, seg);
    if (current === undefined) return undefined;
  }
  return current;
}

function setByPath(obj: unknown, segments: string[], value: unknown): void {
  let current: Record<string, unknown> = obj as Record<string, unknown>;
  for (let i = 0; i < segments.length - 1; i++) {
    const next = resolveSegment(current, segments[i]);
    if (next === undefined) {
      // 如果当前是数组且找不到目标 id，追加新元素
      if (Array.isArray(current)) {
        const newItem: Record<string, unknown> = { id: segments[i] };
        (current as Array<Record<string, unknown>>).push(newItem);
        current = newItem;
      } else {
        current[segments[i]] = {} as Record<string, unknown>;
        current = current[segments[i]] as Record<string, unknown>;
      }
    } else if (typeof next !== 'object' || next === null) {
      current[segments[i]] = {} as Record<string, unknown>;
      current = current[segments[i]] as Record<string, unknown>;
    } else {
      current = next as Record<string, unknown>;
    }
  }
  current[segments[segments.length - 1]] = value;
}

function deleteByPath(obj: unknown, segments: string[]): void {
  let current = obj;

  for (let i = 0; i < segments.length - 1; i++) {
    current = resolveSegment(current, segments[i]);
    if (current === undefined) return;
  }

  const lastKey = segments[segments.length - 1];
  if (Array.isArray(current)) {
    const idx = (current as Array<Record<string, unknown>>).findIndex(item => item.id === lastKey);
    if (idx >= 0) (current as Array<Record<string, unknown>>).splice(idx, 1);
  } else {
    delete (current as Record<string, unknown>)[lastKey];
  }
}

function deepClone<T>(obj: T): T {
  return JSON.parse(JSON.stringify(obj)) as T;
}

function extractNodeId(path: string): string | null {
  const match = path.match(/\[([^\]]+)\]/);
  return match ? match[1] : null;
}
