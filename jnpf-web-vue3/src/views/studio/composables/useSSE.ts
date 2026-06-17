/**
 * SSE 流式通信 composable (Sprint 2 - P-1a)
 * 封装 EventSource 接入、指数退避重连、消息解析
 */
import { ref, onUnmounted } from 'vue';

export interface SSEMessage {
  type: 'chunk' | 'ir_update' | 'stage_change' | 'error' | 'done';
  data: string;
  stage?: string;
  agent?: string;
}

export interface SSEOptions {
  url: string;
  headers?: Record<string, string>;
  onMessage?: (msg: SSEMessage) => void;
  onError?: (err: Event) => void;
  maxRetries?: number;
}

export function useSSE(opts: SSEOptions) {
  const connected = ref(false);
  const retryCount = ref(0);
  const lastEventId = ref('');

  let eventSource: EventSource | null = null;
  let retryTimer: ReturnType<typeof setTimeout> | null = null;
  const maxRetries = opts.maxRetries ?? 5;
  const backoff = [1000, 2000, 4000, 8000, 16000];

  function connect() {
    if (eventSource) eventSource.close();

    // Build URL with headers as query params (SSE doesn't support custom headers)
    const url = new URL(opts.url, window.location.origin);
    if (opts.headers) {
      Object.entries(opts.headers).forEach(([k, v]) => url.searchParams.set(k, v));
    }

    eventSource = new EventSource(url.toString());

    eventSource.onopen = () => {
      connected.value = true;
      retryCount.value = 0;
    };

    eventSource.onmessage = e => {
      lastEventId.value = e.lastEventId;
      try {
        const msg = JSON.parse(e.data) as SSEMessage;
        opts.onMessage?.(msg);
      } catch {
        // Non-JSON SSE messages (e.g., plain text chunks)
        if (e.data.startsWith('data:')) {
          try {
            const msg = JSON.parse(e.data.slice(5).trim()) as SSEMessage;
            opts.onMessage?.(msg);
          } catch {
            opts.onMessage?.({ type: 'chunk', data: e.data });
          }
        }
      }
    };

    eventSource.addEventListener('stage_change', ((e: MessageEvent) => {
      try {
        opts.onMessage?.(JSON.parse(e.data) as SSEMessage);
      } catch {
        /* ignore */
      }
    }) as EventListener);

    eventSource.addEventListener('ir_update', ((e: MessageEvent) => {
      try {
        opts.onMessage?.(JSON.parse(e.data) as SSEMessage);
      } catch {
        /* ignore */
      }
    }) as EventListener);

    eventSource.addEventListener('done', ((e: MessageEvent) => {
      try {
        opts.onMessage?.(JSON.parse(e.data) as SSEMessage);
      } catch {
        /* ignore */
      }
      disconnect();
    }) as EventListener);

    eventSource.onerror = err => {
      connected.value = false;
      opts.onError?.(err);

      if (retryCount.value < maxRetries) {
        const delay = backoff[retryCount.value] || 16000;
        retryCount.value++;
        retryTimer = setTimeout(connect, delay);
      }
    };
  }

  function disconnect() {
    if (retryTimer) clearTimeout(retryTimer);
    if (eventSource) eventSource.close();
    connected.value = false;
  }

  onUnmounted(disconnect);

  return { connected, retryCount, connect, disconnect };
}
