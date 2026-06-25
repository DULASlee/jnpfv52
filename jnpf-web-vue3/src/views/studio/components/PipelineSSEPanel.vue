<!--
  PipelineSSEPanel — SSE 事件实时渲染面板（P2）
  在流水线运行期间展示 AI 思考过程和进度。
-->
<template>
  <div class="pipeline-sse-panel">
    <!-- 头部进度 -->
    <div class="sse-header">
      <span class="sse-stage">{{ stageLabel }}</span>
      <a-progress :percent="progressPercent" :show-info="false" :status="progressStatus" />
      <span class="sse-elapsed">{{ elapsedFormatted }}</span>
    </div>

    <!-- Agent 列表 -->
    <div v-if="agentStates.size === 0" class="sse-empty"> 等待流水线启动... </div>
    <div v-else class="sse-agent-list">
      <div
        v-for="[agentId, state] in agentStates"
        :key="agentId"
        class="sse-agent-item"
        :class="{ 'sse-agent-warning': state.warning, 'sse-agent-timeout': state.timeout }">
        <div class="sse-agent-header">
          <span class="sse-agent-icon">{{ agentIcon(state) }}</span>
          <span class="sse-agent-name">{{ agentId }}</span>
          <span class="sse-agent-time">[{{ formatMs(state.elapsedMs) }}]</span>
          <a-button v-if="state.timeout" size="small" danger type="link" @click="$emit('skipAgent', agentId)"> 跳过 </a-button>
        </div>
        <div v-if="state.thought" class="sse-agent-thought">
          {{ state.thought }}
        </div>
        <div v-if="state.warning && !state.timeout" class="sse-agent-warn-text"> ⚠️ {{ state.warning }} </div>
      </div>
    </div>

    <!-- 底部心跳 -->
    <div class="sse-footer">
      <span v-if="connected" class="sse-connected">🟢 已连接</span>
      <span v-else class="sse-disconnected">🔴 {{ error || '已断开' }} · 正在重连...</span>
      <span class="sse-heartbeat">—— {{ lastHeartbeat }} ——</span>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, reactive, ref, onUnmounted } from 'vue';
  import type { PipelineSSEEvent } from '../../../core/ai/services/pipeline-sse-types';
  import { buildEventSourceUrl } from '/@/utils/http/sseUrl';

  const props = defineProps<{
    pipelineId: number;
  }>();

  defineEmits<{
    skipAgent: [agentId: string];
  }>();

  // SSE 连接
  const connected = ref(false);
  const error = ref<string | null>(null);
  const lastHeartbeat = ref('');
  let eventSource: EventSource | null = null;
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  let retryCount = 0;
  const MAX_RETRIES = 5;

  // Agent 状态
  interface AgentState {
    agentId: string;
    phase: string;
    progress: number;
    thought: string;
    warning: string | null;
    timeout: boolean;
    elapsedMs: number;
  }

  const agentStates = reactive(new Map<string, AgentState>());
  const currentStage = ref('idle');
  const totalProgress = ref(0);

  const stageLabel = computed(() => {
    const labels: Record<string, string> = {
      idle: '等待中',
      requirement: '阶段 1: 需求分析',
      architecture: '阶段 2: 架构设计',
      design: '阶段 3: 总体设计',
      development: '阶段 4: 自动开发',
      delivery: '阶段 5: 交付',
    };
    return labels[currentStage.value] ?? currentStage.value;
  });

  const progressPercent = computed(() => totalProgress.value);
  const progressStatus = computed(() => (connected.value ? 'active' : 'exception'));
  const elapsedFormatted = computed(() => {
    const maxElapsed = Math.max(...[...agentStates.values()].map(s => s.elapsedMs), 0);
    return formatMs(maxElapsed);
  });

  let startTime = 0;

  function connect() {
    const url = buildEventSourceUrl(`/api/studio/pipeline/execute/${props.pipelineId}/events`);
    eventSource = new EventSource(url);
    startTime = Date.now();

    eventSource.onopen = () => {
      connected.value = true;
      error.value = null;
      retryCount = 0;
    };

    eventSource.onmessage = (e: MessageEvent<string>) => {
      if (e.data.startsWith(':')) {
        // 心跳
        lastHeartbeat.value = new Date().toLocaleTimeString();
        return;
      }
      try {
        const event: PipelineSSEEvent = JSON.parse(e.data);
        currentStage.value = event.stage;
        totalProgress.value = event.progress;

        const existing = agentStates.get(event.agent);
        agentStates.set(event.agent, {
          agentId: event.agent,
          phase: event.phase,
          progress: event.progress,
          thought: event.thought,
          warning: event.warning ?? null,
          timeout: event.timeout_alert ?? false,
          elapsedMs: event.elapsed_ms,
        });
      } catch {
        // 格式错误，跳过
      }
    };

    eventSource.onerror = () => {
      connected.value = false;
      eventSource?.close();
      eventSource = null;

      if (retryCount >= MAX_RETRIES) {
        error.value = `重连失败（已达上限 ${MAX_RETRIES} 次）`;
        return;
      }

      error.value = `连接中断，正在重连 (${retryCount + 1}/${MAX_RETRIES})...`;
      retryCount++;
      reconnectTimer = setTimeout(() => {
        reconnectTimer = null;
        connect();
      }, 5000);
    };
  }

  function disconnect() {
    if (reconnectTimer) {
      clearTimeout(reconnectTimer);
      reconnectTimer = null;
    }
    eventSource?.close();
    eventSource = null;
    connected.value = false;
  }

  connect();
  onUnmounted(() => disconnect());

  function agentIcon(state: AgentState): string {
    if (state.timeout) return '❌';
    if (state.warning) return '⚠️';
    return '▸';
  }

  function formatMs(ms: number): string {
    const s = Math.floor(ms / 1000);
    if (s < 60) return `${s}s`;
    return `${Math.floor(s / 60)}m${s % 60}s`;
  }
</script>

<style lang="less" scoped>
  .pipeline-sse-panel {
    border: 1px solid #e8e8e8;
    border-radius: 4px;
    overflow: hidden;
    font-size: 13px;
  }
  .sse-header {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 8px 12px;
    background: #fafafa;
    border-bottom: 1px solid #e8e8e8;
  }
  .sse-stage {
    font-weight: 600;
    white-space: nowrap;
  }
  .sse-elapsed {
    color: #8c8c8c;
    font-family: monospace;
    white-space: nowrap;
  }
  .sse-empty {
    padding: 32px;
    text-align: center;
    color: #8c8c8c;
  }
  .sse-agent-list {
    max-height: 400px;
    overflow-y: auto;
  }
  .sse-agent-item {
    padding: 10px 12px;
    border-bottom: 1px solid #f0f0f0;
    transition: background 0.1s;
  }
  .sse-agent-item:hover {
    background: #fafafa;
  }
  .sse-agent-warning {
    border-left: 3px solid #faad14;
    background: #fffbe6;
  }
  .sse-agent-timeout {
    border-left: 3px solid #ff4d4f;
    background: #fff2f0;
  }
  .sse-agent-header {
    display: flex;
    align-items: center;
    gap: 6px;
  }
  .sse-agent-icon {
    font-size: 14px;
    width: 20px;
  }
  .sse-agent-name {
    font-weight: 600;
  }
  .sse-agent-time {
    color: #8c8c8c;
    font-family: monospace;
    font-size: 12px;
  }
  .sse-agent-thought {
    margin: 4px 0 0 26px;
    color: #595959;
  }
  .sse-agent-warn-text {
    margin: 4px 0 0 26px;
    color: #d48806;
  }
  .sse-footer {
    display: flex;
    justify-content: space-between;
    padding: 6px 12px;
    background: #fafafa;
    border-top: 1px solid #e8e8e8;
    font-size: 12px;
    color: #8c8c8c;
  }
  .sse-connected {
    color: #52c41a;
  }
  .sse-disconnected {
    color: #ff4d4f;
  }
  .sse-heartbeat {
    color: #d9d9d9;
  }
</style>
