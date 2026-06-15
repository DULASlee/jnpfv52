/**
 * AgentTimeoutMonitor — 智能体超时监控器（P1）
 *
 * 每个 Agent 独立计时器。软超时 → warning 事件，硬超时 → timeout_alert + 熔断计数。
 * Agent 正常完成时必须调用 stopMonitoring 清除计时器。
 *
 * @module ai/services/agent-timeout-monitor
 * @version 1.0.0
 */

import type { PipelineSSEEvent, AgentTimeoutConfig } from './pipeline-sse-types';

// ============================================================
// SSE 事件发射器接口（轻量级，不依赖具体实现）
// ============================================================

export interface PipelineSSEEmitter {
  push(event: PipelineSSEEvent): void;
}

// ============================================================
// AgentTimeoutMonitor
// ============================================================

export class AgentTimeoutMonitor {
  private timers = new Map<string, { softTimer: ReturnType<typeof setTimeout>; hardTimer: ReturnType<typeof setTimeout> }>();
  private startTimes = new Map<string, number>();
  private emitter: PipelineSSEEmitter;

  constructor(emitter: PipelineSSEEmitter) {
    this.emitter = emitter;
  }

  startMonitoring(agentId: string, config: AgentTimeoutConfig): void {
    // 清除已有计时器（防御性编程）
    this.stopMonitoring(agentId);

    const startTime = Date.now();
    this.startTimes.set(agentId, startTime);

    const softTimer = setTimeout(() => {
      this.emitter.push({
        stage: 'design',
        phase: 'timeout_warning',
        progress: 50,
        thought: `Agent [${agentId}] 执行超过预期时间 ${config.expected_ms}ms`,
        agent: agentId,
        warning: `软超时：预期 ${config.expected_ms}ms，已耗时 ${Date.now() - startTime}ms`,
        timeout_alert: false,
        timestamp: new Date().toISOString(),
        elapsed_ms: Date.now() - startTime,
        estimated_remaining_ms: config.max_ms - config.expected_ms,
      });
    }, config.expected_ms);

    const hardTimer = setTimeout(() => {
      this.emitter.push({
        stage: 'design',
        phase: 'timeout_alert',
        progress: 0,
        thought: `Agent [${agentId}] 硬超时！超过最大允许时间 ${config.max_ms}ms`,
        agent: agentId,
        warning: `硬超时：最大 ${config.max_ms}ms，实际 ${Date.now() - startTime}ms`,
        timeout_alert: true,
        timestamp: new Date().toISOString(),
        elapsed_ms: Date.now() - startTime,
      });
    }, config.max_ms);

    this.timers.set(agentId, { softTimer, hardTimer });
  }

  stopMonitoring(agentId: string): void {
    const timers = this.timers.get(agentId);
    if (timers) {
      clearTimeout(timers.softTimer);
      clearTimeout(timers.hardTimer);
      this.timers.delete(agentId);
    }
    this.startTimes.delete(agentId);
  }

  getElapsedMs(agentId: string): number | undefined {
    const startTime = this.startTimes.get(agentId);
    if (startTime === undefined) return undefined;
    return Date.now() - startTime;
  }
}
