/**
 * SSE 流式通信 composable (Sprint 2 - P-1a)
 * 封装 EventSource 接入、指数退避重连、消息解析
 */
import { ref, onUnmounted } from 'vue';
import { buildEventSourceUrl } from '/@/utils/http/sseUrl';

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
  onOpen?: () => void;
  onError?: (err: Event) => void;
  /** 重连次数耗尽且连接仍未恢复 */
  onGiveUp?: () => void;
  maxRetries?: number;
}

export function useSSE(opts: SSEOptions) {
  const connected = ref(false);
  const retryCount = ref(0);
  const lastEventId = ref('');

  let eventSource: EventSource | null = null;
  let retryTimer: ReturnType<typeof setTimeout> | null = null;
  // 是否已收到 done/error（正常结束），用于阻止 onerror 误触发重连
  let finished = false;
  const maxRetries = opts.maxRetries ?? 5;
  const backoff = [1000, 2000, 4000, 8000, 16000];

  function connect() {
    if (eventSource) eventSource.close();
    finished = false;

    // 与 axios 一致：开发环境需带 /dev 前缀，否则 EventSource 打到 Vite 而非后端
    const url = new URL(buildEventSourceUrl(opts.url));
    if (opts.headers) {
      Object.entries(opts.headers).forEach(([k, v]) => url.searchParams.set(k, v));
    }

    eventSource = new EventSource(url.toString());

    eventSource.onopen = () => {
      connected.value = true;
      retryCount.value = 0;
      opts.onOpen?.();
    };

    eventSource.onmessage = e => {
      lastEventId.value = e.lastEventId;
      try {
        const msg = JSON.parse(e.data) as SSEMessage;
        if (msg.type === 'done' || msg.type === 'error') finished = true;
        opts.onMessage?.(msg);
      } catch {
        // Non-JSON SSE messages (e.g., plain text chunks)
        if (e.data.startsWith('data:')) {
          try {
            const msg = JSON.parse(e.data.slice(5).trim()) as SSEMessage;
            if (msg.type === 'done' || msg.type === 'error') finished = true;
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
      finished = true;
      try {
        opts.onMessage?.(JSON.parse(e.data) as SSEMessage);
      } catch {
        /* ignore */
      }
      disconnect();
    }) as EventListener);

    eventSource.onerror = err => {
      connected.value = false;
      // 后端推送 done/error 后正常关闭连接会触发 onerror，此时不应重连
      if (finished) return;

      opts.onError?.(err);

      if (retryCount.value < maxRetries) {
        const delay = backoff[retryCount.value] || 16000;
        retryCount.value++;
        retryTimer = setTimeout(connect, delay);
      } else {
        opts.onGiveUp?.();
      }
    };
  }

  function disconnect() {
    if (retryTimer) clearTimeout(retryTimer);
    if (eventSource) eventSource.close();
    eventSource = null;
    connected.value = false;
  }

  onUnmounted(disconnect);

  return { connected, retryCount, connect, disconnect };
}
