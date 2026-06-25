/**
 * usePipelineSSE — Pipeline SSE 事件订阅（P2）
 *
 * 连接到 GET /api/studio/pipeline/execute/{id}/events 的 SSE 流，
 * 解析 PipelineSSEEvent 并暴露响应式状态。
 *
 * @module ai/composables/usePipelineSSE
 * @version 1.0.0
 */

import { ref, onUnmounted, type Ref } from 'vue';
import { buildEventSourceUrl } from '/@/utils/http/sseUrl';
import type { PipelineSSEEvent } from '../services/pipeline-sse-types';

export interface UsePipelineSSEReturn {
  /** 最新事件 */
  currentEvent: Ref<PipelineSSEEvent | null>;
  /** 所有已接收事件 */
  events: Ref<PipelineSSEEvent[]>;
  /** 连接状态 */
  connected: Ref<boolean>;
  /** 错误信息 */
  error: Ref<string | null>;
  /** 手动关闭连接 */
  close: () => void;
}

/**
 * 订阅流水线 SSE 事件流。
 * 组件卸载时自动关闭连接。
 */
export function usePipelineSSE(pipelineId: number): UsePipelineSSEReturn {
  const currentEvent = ref<PipelineSSEEvent | null>(null);
  const events = ref<PipelineSSEEvent[]>([]);
  const connected = ref(false);
  const error = ref<string | null>(null);
  let eventSource: EventSource | null = null;
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  let retryCount = 0;
  const MAX_RETRIES = 5;

  function connect() {
    const url = buildEventSourceUrl(`/api/studio/pipeline/execute/${pipelineId}/events`);
    eventSource = new EventSource(url);

    eventSource.onopen = () => {
      connected.value = true;
      error.value = null;
      retryCount = 0;
    };

    eventSource.onmessage = (e: MessageEvent<string>) => {
      try {
        const data = JSON.parse(e.data) as PipelineSSEEvent;
        currentEvent.value = data;
        events.value.push(data);
      } catch {
        // 心跳或格式错误，跳过
      }
    };

    eventSource.onerror = () => {
      connected.value = false;
      eventSource?.close();
      eventSource = null;

      if (retryCount >= MAX_RETRIES) {
        error.value = `SSE 连接中断（已达重连上限 ${MAX_RETRIES} 次）`;
        return;
      }

      error.value = `SSE 连接中断，正在重连 (${retryCount + 1}/${MAX_RETRIES})...`;
      retryCount++;
      // 5 秒后自动重连
      reconnectTimer = setTimeout(() => {
        reconnectTimer = null;
        connect();
      }, 5000);
    };
  }

  function close() {
    if (reconnectTimer) {
      clearTimeout(reconnectTimer);
      reconnectTimer = null;
    }
    eventSource?.close();
    eventSource = null;
    connected.value = false;
  }

  connect();

  onUnmounted(() => close());

  return { currentEvent, events, connected, error, close };
}
