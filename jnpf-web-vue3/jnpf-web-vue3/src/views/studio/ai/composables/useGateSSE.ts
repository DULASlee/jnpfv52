/**
 * SA门控 SSE 连接管理器
 *
 * 职责：
 *   1. 使用 fetch-event-source 替代原生 EventSource，支持 Authorization Header
 *   2. connect() 返回 Promise，调用方 await 确认就绪后再触发门控API（防竞态）
 *   3. 指数退避自动重连（2s/4s/6s），MAX_RETRY=3 后才判定真失败
 *   4. 组件卸载时自动断开连接，防止内存泄漏
 *
 * 严格禁止修改：
 *   - onerror 中的重试逻辑和返回值语义
 *   - connect() 的 Promise resolve/reject 时机
 *   - disconnect() 的 abort 逻辑
 *
 * @author SA-Studio Team
 * @date 2026-06-23
 */

import { fetchEventSource } from '@microsoft/fetch-event-source';
import { ref, onUnmounted } from 'vue';
import { getAuthHeader } from '/@/utils/auth';

// ═══════════════════════════════════════
// 类型定义
// ═══════════════════════════════════════

export interface IdentifiedElement {
  category: string;
  description: string;
  evidence: string;
}

export interface MissingElement {
  category: string;
  description: string;
  severity: string; // 'critical' | 'warning'
  howToFix: string;
}

export interface SemanticFitnessResult {
  passed: boolean;
  score: number;
  level: string; // 'sufficient' | 'partial' | 'insufficient'
  identified: IdentifiedElement[];
  missing: MissingElement[];
  nextStepGuidance: string;
}

export interface GateError {
  message: string;
  errorCode: string;
}

export type GateStatus = 'idle' | 'processing' | 'passed' | 'failed' | 'error';

export type GateEventType = 'gate_started' | 'gate_passed' | 'gate_failed' | 'gate_error';

// ═══════════════════════════════════════
// 核心 Composable
// ═══════════════════════════════════════

const MAX_RETRY = 3;

export function useGateSSE() {
  // ── 响应式状态（只读暴露给模板） ──
  const gateStatus = ref<GateStatus>('idle');
  const gateResult = ref<SemanticFitnessResult | null>(null);
  const gateError = ref<GateError | null>(null);

  // ── 内部状态（非响应式，不触发视图更新） ──
  let abortController: AbortController | null = null;
  let traceId = '';
  let retryCount = 0;
  let currentPipelineId = '';

  /**
   * 建立 SSE 连接
   *
   * 返回 Promise：
   *   - resolve：连接成功，通道就绪，可以触发门控API
   *   - reject：连接失败（鉴权失败、网络不通等）
   *
   * 调用方必须 await 此方法后再调用后端门控API！
   *
   * @param pipelineId 流水线ID
   * @param onEvent 事件回调（eventType, data）=> void
   */
  function connect(pipelineId: string, onEvent: (eventType: string, data: any) => void): Promise<void> {
    return new Promise((resolve, reject) => {
      // 清理旧连接
      if (abortController) {
        abortController.abort();
        abortController = null;
      }

      abortController = new AbortController();
      retryCount = 0;
      currentPipelineId = pipelineId;

      const url = `/api/ai/pipeline/${pipelineId}/events`;

      fetchEventSource(url, {
        // ★ 致命缺陷1修正：携带鉴权Header
        headers: {
          Authorization: getAuthHeader() || '',
          Accept: 'text/event-stream',
          'X-Trace-Id': traceId,
        },
        signal: abortController.signal,

        // ★ 连接成功——通知调用方通道就绪
        onopen: async response => {
          const contentType = response.headers.get('content-type') || '';

          if (response.ok && contentType.includes('text/event-stream')) {
            console.log(`[Gate SSE] ✅ 连接就绪 traceId=${traceId}`);
            retryCount = 0;
            resolve();
            return;
          }

          // 非预期响应（401/403/404/500等）
          const errorMsg = `SSE连接失败: HTTP ${response.status}`;
          console.error(`[Gate SSE] ❌ ${errorMsg} traceId=${traceId}`);

          // 鉴权失败直接报错，不重试
          if (response.status === 401 || response.status === 403) {
            gateStatus.value = 'error';
            gateError.value = {
              message: response.status === 401 ? '登录已过期，请重新登录' : '无权访问此资源',
              errorCode: response.status === 401 ? 'AUTH_EXPIRED' : 'FORBIDDEN',
            };
          }

          reject(new Error(errorMsg));
        },

        // ★ 收到消息——分发给调用方
        onmessage: ev => {
          console.log(`[Gate SSE] 📨 事件: ${ev.event} traceId=${traceId}`);
          try {
            const data = JSON.parse(ev.data);
            onEvent(ev.event, data);
          } catch (parseErr) {
            console.error(`[Gate SSE] JSON解析失败: ${ev.data}`, parseErr);
          }
        },

        // ★ 致命缺陷3修正：带退避的自动重连
        onerror: err => {
          console.warn(`[Gate SSE] ⚠️ 连接断开 retry=${retryCount}/${MAX_RETRY} traceId=${traceId}`, err);

          if (retryCount < MAX_RETRY) {
            retryCount++;
            const delay = retryCount * 2000; // 2s → 4s → 6s
            console.log(`[Gate SSE] ⏳ ${delay}ms 后自动重连 (${retryCount}/${MAX_RETRY})`);
            // 返回延迟毫秒数——fetch-event-source 库会自动重连
            return delay;
          }

          // ★ 重试耗尽，判定为致命错误
          console.error(`[Gate SSE] ❌ 重试耗尽，判定为致命错误 traceId=${traceId}`);
          gateStatus.value = 'error';
          gateError.value = {
            message: '网络连接不稳定，需求评估中断。请检查网络后重试。',
            errorCode: 'SSE_CONN_FATAL',
          };

          // 主动断开，阻止库继续重试
          if (abortController) {
            abortController.abort();
            abortController = null;
          }
        },
      }).catch(err => {
        // AbortError 是用户主动取消，不算错误
        if (err.name === 'AbortError') {
          console.log(`[Gate SSE] 连接被主动取消 traceId=${traceId}`);
          return;
        }
        // SSE_CONN_FATAL 是 onerror 中我们主动抛的
        if (err.message === 'SSE_CONN_FATAL') {
          return;
        }
        // 其他未预期错误
        console.error(`[Gate SSE] 未预期异常 traceId=${traceId}`, err);
        reject(err);
      });
    });
  }

  /**
   * 断开 SSE 连接
   */
  function disconnect() {
    if (abortController) {
      abortController.abort();
      abortController = null;
    }
  }

  /**
   * 重置所有状态（用于"补充材料后重新提交"）
   *
   * 注意：disconnect 会触发 AbortError，
   * connect 中的 catch 已处理，不会影响用户
   */
  function reset() {
    disconnect();
    gateStatus.value = 'idle';
    gateResult.value = null;
    gateError.value = null;
    traceId = '';
    retryCount = 0;
  }

  /**
   * 设置追踪ID（调用方在 submitMaterials 开始时设置）
   */
  function setTraceId(id: string) {
    traceId = id;
  }

  // ── 组件卸载时自动清理 ──
  onUnmounted(() => {
    disconnect();
  });

  return {
    // 只读状态
    gateStatus,
    gateResult,
    gateError,

    // 方法
    connect,
    disconnect,
    reset,
    setTraceId,
  };
}
