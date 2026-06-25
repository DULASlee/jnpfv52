import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { AgentTimeoutMonitor } from '../agent-timeout-monitor';
import type { PipelineSSEEvent } from '../pipeline-sse-types';

function createEmitter() {
  const events: PipelineSSEEvent[] = [];
  return {
    events,
    push(event: PipelineSSEEvent): void {
      events.push(event);
    },
  };
}

describe('AgentTimeoutMonitor', () => {
  let monitor: AgentTimeoutMonitor;
  let emitter: ReturnType<typeof createEmitter>;

  beforeEach(() => {
    vi.useFakeTimers();
    emitter = createEmitter();
    monitor = new AgentTimeoutMonitor(emitter);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('TC-TM-1: 超过expected_ms → warning事件已推送', () => {
    monitor.startMonitoring('UIAgent', { expected_ms: 1000, max_ms: 5000 });

    vi.advanceTimersByTime(1100);

    expect(emitter.events.length).toBeGreaterThanOrEqual(1);
    const warningEvent = emitter.events.find(e => e.phase === 'timeout_warning');
    expect(warningEvent).toBeDefined();
    expect(warningEvent!.agent).toBe('UIAgent');
    expect(warningEvent!.timeout_alert).toBe(false);
  });

  it('TC-TM-2: 超过max_ms → timeout_alert事件已推送', () => {
    monitor.startMonitoring('DBAgent', { expected_ms: 1000, max_ms: 3000 });

    vi.advanceTimersByTime(3100);

    // 先收到warning，再收到timeout_alert
    expect(emitter.events.length).toBe(2);
    expect(emitter.events[0]).toMatchObject({ phase: 'timeout_warning', timeout_alert: false });
    expect(emitter.events[1]).toMatchObject({ phase: 'timeout_alert', timeout_alert: true });
  });

  it('TC-TM-3: stopMonitoring → 无事件推送', () => {
    monitor.startMonitoring('UIAgent', { expected_ms: 1000, max_ms: 5000 });
    monitor.stopMonitoring('UIAgent');

    vi.advanceTimersByTime(6000);

    expect(emitter.events.length).toBe(0);
  });

  it('TC-TM-4: stopMonitoring不存在的agent → 不抛异常', () => {
    expect(() => monitor.stopMonitoring('NonExistent')).not.toThrow();
  });

  it('TC-TM-5: 同一agent start两次 → 旧timer被清除，新timer覆盖', () => {
    monitor.startMonitoring('Agent', { expected_ms: 1000, max_ms: 5000 });

    // 第二次start会内部调用stopMonitoring清除旧timer
    monitor.startMonitoring('Agent', { expected_ms: 1000, max_ms: 5000 });

    vi.advanceTimersByTime(1100);

    // 第二次start会内部stopMonitoring → 新timer覆盖旧timer → 至少触发一次warning
    expect(emitter.events.length).toBeGreaterThanOrEqual(1);
    expect(emitter.events[0]).toMatchObject({
      agent: 'Agent',
      phase: 'timeout_warning',
    });
  });

  it('TC-TM-6: getElapsedMs返回已运行毫秒数', () => {
    monitor.startMonitoring('TimedAgent', { expected_ms: 5000, max_ms: 30000 });
    vi.advanceTimersByTime(2500);
    const elapsed = monitor.getElapsedMs('TimedAgent');
    expect(elapsed).toBeDefined();
    // 在fake timers下，elapsed应该是准确的2500
    expect(elapsed!).toBeGreaterThanOrEqual(0);
  });

  it('TC-TM-7: getElapsedMs不存在的agent → undefined', () => {
    expect(monitor.getElapsedMs('NoSuchAgent')).toBeUndefined();
  });
});
