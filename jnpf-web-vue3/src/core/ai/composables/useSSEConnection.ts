/**
 * SSE 断线重连 composable（D-7: 2026-06-20）。
 *
 * 功能：
 *   - 指数退避重连（1s→2s→4s→8s→16s，最多5次）
 *   - 3 色连接状态灯（绿/黄/红）
 *   - 4 级超时提示（L1-L4，与 AgentTimeoutMonitor 对齐）
 *   - Last-Event-ID 续接
 */
import { ref, onUnmounted } from 'vue';

export type SSELevel = 'L1_thinking' | 'L2_long_running' | 'L3_timeout_warning' | 'L4_timeout_hard';
export type SSEStatus = 'connected' | 'reconnecting' | 'disconnected';

export interface SSEOptions {
  url: string;
  expectedMs?: number;
  maxMs?: number;
  maxRetries?: number;
  onEvent?: (event: SSEEventData) => void;
  onLevelChange?: (level: SSELevel) => void;
}

export interface SSEEventData {
  type: string;
  agent?: string;
  stage?: string;
  delta?: string;
  elapsedMs?: number;
  estimatedRemainingMs?: number;
  progressPct?: number;
  [key: string]: any;
}

export function useSSEConnection(opts: SSEOptions) {
  const status = ref<SSEStatus>('disconnected');
  const retryCount = ref(0);
  const level = ref<SSELevel>('L1_thinking');
  const elapsedMs = ref(0);
  const lastEventId = ref('');

  const maxRetries = opts.maxRetries ?? 5;
  const expectedMs = opts.expectedMs ?? 30000;
  const maxMs = opts.maxMs ?? 120000;

  let eventSource: EventSource | null = null;
  let startTime = 0;
  let timer: ReturnType<typeof setInterval> | null = null;

  const backoffDelays = [1000, 2000, 4000, 8000, 16000];

  const levelLabelMap: Record<SSELevel, string> = {
    L1_thinking: 'green',
    L2_long_running: 'yellow',
    L3_timeout_warning: 'orange',
    L4_timeout_hard: 'red',
  };

  function connect() {
    dispose();
    status.value = 'reconnecting';
    startTime = Date.now();
    elapsedMs.value = 0;

    timer = setInterval(() => {
      elapsedMs.value = Date.now() - startTime;
      updateLevel();
    }, 1000);

    const url = new URL(opts.url, window.location.origin);
    if (lastEventId.value) {
      url.searchParams.set('lastEventId', lastEventId.value);
    }

    eventSource = new EventSource(url.toString());

    eventSource.onopen = () => {
      status.value = 'connected';
      retryCount.value = 0;
    };

    eventSource.onmessage = e => {
      lastEventId.value = e.lastEventId;
      try {
        const data = JSON.parse(e.data) as SSEEventData;
        opts.onEvent?.(data);
      } catch {
        opts.onEvent?.({ type: 'raw', delta: e.data });
      }
    };

    eventSource.addEventListener('stage_start', (e: any) => {
      const data = JSON.parse(e.data);
      opts.onEvent?.({ ...data, type: 'stage_start' });
    });
    eventSource.addEventListener('thinking', (e: any) => {
      const data = JSON.parse(e.data);
      opts.onEvent?.({ ...data, type: 'thinking' });
    });
    eventSource.addEventListener('stage_complete', (e: any) => {
      const data = JSON.parse(e.data);
      opts.onEvent?.({ ...data, type: 'stage_complete' });
    });
    eventSource.addEventListener('error', (e: any) => {
      const data = tryParse(e.data);
      opts.onEvent?.({ ...data, type: 'error' });
    });
    eventSource.addEventListener('timeout_warning', (e: any) => {
      const data = JSON.parse(e.data);
      opts.onEvent?.({ ...data, type: 'timeout_warning' });
    });
    eventSource.addEventListener('timeout_hard', (e: any) => {
      const data = JSON.parse(e.data);
      opts.onEvent?.({ ...data, type: 'timeout_hard' });
    });

    eventSource.onerror = () => {
      if (retryCount.value < maxRetries) {
        const delay = backoffDelays[retryCount.value] || 16000;
        retryCount.value++;
        status.value = 'reconnecting';
        setTimeout(connect, delay);
      } else {
        status.value = 'disconnected';
        dispose();
      }
    };
  }

  function updateLevel() {
    if (elapsedMs.value >= maxMs) {
      level.value = 'L4_timeout_hard';
    } else if (elapsedMs.value >= expectedMs * 1.5) {
      level.value = 'L3_timeout_warning';
    } else if (elapsedMs.value >= expectedMs) {
      level.value = 'L2_long_running';
    } else {
      level.value = 'L1_thinking';
    }
    opts.onLevelChange?.(level.value);
  }

  function dispose() {
    if (eventSource) {
      eventSource.close();
      eventSource = null;
    }
    if (timer) {
      clearInterval(timer);
      timer = null;
    }
  }

  function reconnect() {
    retryCount.value = 0;
    connect();
  }

  onUnmounted(dispose);

  return { status, retryCount, level, elapsedMs, levelLabelMap, connect, disconnect: dispose, reconnect };
}

function tryParse(text: string): Record<string, any> {
  try {
    return JSON.parse(text);
  } catch {
    return {};
  }
}
