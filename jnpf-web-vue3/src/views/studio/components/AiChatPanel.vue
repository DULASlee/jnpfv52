<template>
  <div class="ai-chat-panel">
    <!-- Stage progress bar (P-4) -->
    <PipelineStageBar
      :stages="pipelineStages"
      :waiting="waiting"
      :can-rollback="currentStage > 0"
      :can-confirm="messages.length > 0 && !waiting"
      @confirm="handleConfirm"
      @rollback="handleRollback"
      @ask="handleAsk"
      @select-stage="handleSelectStage" />

    <!-- Message list -->
    <div ref="messageList" class="message-container">
      <div v-if="messages.length === 0" class="empty-state">
        <span class="empty-icon">💬</span>
        <p>开始对话，描述你的业务需求</p>
      </div>
      <MessageBubble
        v-for="(msg, i) in messages"
        :key="i"
        :role="msg.role"
        :content="msg.content"
        :content-type="msg.contentType"
        :stage="msg.stage"
        :timestamp="msg.timestamp" />
      <!-- Streaming placeholder -->
      <div v-if="streaming" class="streaming-hint">
        <span class="dot"></span> AI 思考中...
        <span class="streaming-text">{{ streamBuffer }}</span>
      </div>
    </div>

    <!-- Input area -->
    <div class="input-area">
      <AttachmentUpload @update:files="onFiles" />
      <div class="input-row">
        <a-textarea v-model:value="userInput" :auto-size="{ minRows: 1, maxRows: 4 }" placeholder="输入补充需求或追问..." @press-enter="handleSend" />
        <a-button type="primary" :disabled="!userInput.trim() || waiting" @click="handleSend"> 发送 </a-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive, nextTick, computed } from 'vue';
  import MessageBubble from './chat/MessageBubble.vue';
  import PipelineStageBar, { type StageInfo } from './PipelineStageBar.vue';
  import AttachmentUpload from './chat/AttachmentUpload.vue';
  import { useSSE } from '../composables/useSSE';
  import { defHttp } from '/@/utils/http/axios';

  const props = defineProps<{
    pipelineId: number;
    initialStage?: number;
  }>();

  interface ChatMessage {
    role: 'user' | 'assistant' | 'system';
    content: string;
    contentType: 'text' | 'ir' | 'document';
    stage?: string;
    timestamp?: string;
  }

  const messages = ref<ChatMessage[]>([]);
  const userInput = ref('');
  const waiting = ref(false);
  const streaming = ref(false);
  const streamBuffer = ref('');
  const currentStage = ref(props.initialStage ?? 0);
  const attachedFiles = ref<File[]>([]);
  const messageList = ref<HTMLElement | null>(null);

  const pipelineStages = reactive<StageInfo[]>([
    { key: 'requirement', label: '需求分析', status: 'pending' },
    { key: 'architecture', label: '架构设计', status: 'pending' },
    { key: 'design', label: '总体设计', status: 'pending' },
    { key: 'development', label: '自动开发', status: 'pending' },
    { key: 'delivery', label: '交付', status: 'pending' },
  ]);

  // Mark current stage as running
  function updateStageUI(stageKey: string) {
    const idx = pipelineStages.findIndex(s => s.key === stageKey);
    for (let i = 0; i < pipelineStages.length; i++) {
      if (i < idx) pipelineStages[i].status = 'completed';
      else if (i === idx) pipelineStages[i].status = 'running';
      else pipelineStages[i].status = 'pending';
    }
    currentStage.value = idx;
  }

  async function loadHistory() {
    try {
      const res: any = await defHttp.get({
        url: `/api/founder/ai/pipeline/${props.pipelineId}`,
      });
      const detail = res?.data || res;
      if (detail) {
        updateStageUI(detail.currentStage || 'requirement');
      }
    } catch {
      // Pipeline may not exist yet
    }
  }

  async function handleSend() {
    const text = userInput.value.trim();
    if (!text) return;

    messages.value.push({
      role: 'user',
      content: text,
      contentType: 'text',
      timestamp: new Date().toLocaleTimeString(),
    });
    userInput.value = '';
    waiting.value = true;
    scrollToBottom();

    // Call pipeline execute
    try {
      await defHttp.post({
        url: `/api/founder/ai/pipeline/${props.pipelineId}/execute`,
        data: { stageName: 'requirement' },
      });
    } catch {
      // Fallback: start SSE directly
    }

    // Connect SSE
    connectSSE();
  }

  let sseDisconnect: (() => void) | null = null;

  function connectSSE() {
    const { disconnect } = useSSE({
      url: `/api/founder/ai/pipeline/${props.pipelineId}/events`,
      headers: {},
      onMessage: msg => {
        streamBuffer.value += msg.data;
        streaming.value = true;
        if (msg.type === 'stage_change') {
          updateStageUI(msg.stage || 'requirement');
        }
      },
    });
    sseDisconnect = disconnect;
    setTimeout(() => {
      streaming.value = false;
      if (streamBuffer.value) {
        messages.value.push({
          role: 'assistant',
          content: streamBuffer.value,
          contentType: 'text',
          timestamp: new Date().toLocaleTimeString(),
        });
        streamBuffer.value = '';
      }
      waiting.value = false;
      scrollToBottom();
    }, 3000);
  }

  async function handleConfirm(feedback: string) {
    waiting.value = true;
    try {
      await defHttp.post({
        url: `/api/founder/ai/pipeline/${props.pipelineId}/execute`,
        data: { stageName: pipelineStages[currentStage.value].key },
      });
      const nextIdx = currentStage.value + 1;
      if (nextIdx < pipelineStages.length) updateStageUI(pipelineStages[nextIdx].key);
      if (feedback) {
        messages.value.push({
          role: 'user',
          content: feedback,
          contentType: 'text',
          timestamp: new Date().toLocaleTimeString(),
        });
      }
      connectSSE();
    } finally {
      waiting.value = false;
    }
  }

  function handleRollback() {
    const prev = Math.max(0, currentStage.value - 1);
    updateStageUI(pipelineStages[prev].key);
  }

  function handleAsk(feedback: string) {
    if (feedback) {
      userInput.value = feedback;
      handleSend();
    }
  }

  function handleSelectStage(_key: string) {
    /* View-only */
  }

  function onFiles(files: File[]) {
    attachedFiles.value = files;
  }

  function scrollToBottom() {
    nextTick(() => {
      if (messageList.value) {
        messageList.value.scrollTop = messageList.value.scrollHeight;
      }
    });
  }

  loadHistory();
</script>

<style scoped lang="less">
  .ai-chat-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    background: #fff;

    .message-container {
      flex: 1;
      overflow-y: auto;
      padding: 16px 0;

      .empty-state {
        text-align: center;
        padding: 60px 20px;
        color: #bbb;

        .empty-icon {
          font-size: 48px;
          display: block;
          margin-bottom: 12px;
        }
        p {
          font-size: 14px;
        }
      }
    }

    .streaming-hint {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 8px 16px;
      color: #1890ff;
      font-size: 13px;

      .dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: #1890ff;
        animation: pulse 1s infinite;
      }
      .streaming-text {
        color: #555;
      }
    }

    .input-area {
      border-top: 1px solid #f0f0f0;
      padding: 12px 16px;
      background: #fafafa;

      .input-row {
        display: flex;
        gap: 8px;
        margin-top: 8px;
      }
    }
  }

  @keyframes pulse {
    0%,
    100% {
      opacity: 0.3;
    }
    50% {
      opacity: 1;
    }
  }
</style>
