/**
 * Pipeline state machine
 * @module ai/pipeline/state-machine
 */

import type { PipelineStage } from './stages';

export type PipelineStatus = 'idle' | 'running' | 'waiting_confirmation' | 'completed' | 'failed' | 'expert_mode';

export interface StageHistoryEntry {
  stage: PipelineStage;
  action: string;
  timestamp: string;
  confidence?: number;
}

export interface PipelineState {
  currentStage: PipelineStage;
  status: PipelineStatus;
  error?: string;
  requirement?: unknown;
  architecture?: unknown;
  design?: { ui: unknown; database: unknown };
  development?: { code: string; target: string };
  delivery?: { url: string; zip: string };
  history: StageHistoryEntry[];
  confidence: number;
}

export function createInitialState(): PipelineState {
  return { currentStage: 'requirement', status: 'idle', history: [], confidence: 1.0 };
}

const VALID_TRANSITIONS: Record<PipelineStatus, PipelineStatus[]> = {
  idle: ['running', 'expert_mode'],
  running: ['waiting_confirmation', 'failed', 'expert_mode'],
  waiting_confirmation: ['running', 'completed', 'expert_mode'],
  completed: ['running'],
  failed: ['running', 'expert_mode'],
  expert_mode: ['running'],
};

export function canTransition(from: PipelineStatus, to: PipelineStatus): boolean {
  return VALID_TRANSITIONS[from]?.includes(to) ?? false;
}

export function transition(state: PipelineState, to: PipelineStatus, reason?: string): PipelineState {
  if (!canTransition(state.status, to)) return state;
  state.status = to;
  state.history.push({ stage: state.currentStage, action: `→ ${to}${reason ? `: ${reason}` : ''}`, timestamp: new Date().toISOString() });
  return state;
}

export function updateConfidence(state: PipelineState, confidence: number): PipelineState {
  state.confidence = Math.max(0, Math.min(1, confidence));
  if (confidence < 0.6 && state.status !== 'expert_mode') {
    state.status = 'expert_mode';
    state.history.push({
      stage: state.currentStage,
      action: `expert_mode: low confidence ${(confidence * 100).toFixed(0)}%`,
      timestamp: new Date().toISOString(),
    });
  }
  return state;
}

export function advanceStage(state: PipelineState, next: PipelineStage): PipelineState {
  state.history.push({ stage: state.currentStage, action: `advance: ${state.currentStage} → ${next}`, timestamp: new Date().toISOString() });
  state.currentStage = next;
  state.status = 'running';
  return state;
}
