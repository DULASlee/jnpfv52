<template>
  <div class="ai-chat-panel" tabindex="0" @keydown="handleKeydown">
    <div class="chat-messages" ref="msgContainer">
      <div v-if="messages.length === 0" class="chat-empty">
        <a-empty description="开始与 AI 对话" :image="Empty.PRESENTED_IMAGE_SIMPLE" />
      </div>
      <div v-for="(msg, i) in messages" :key="i" class="chat-msg" :class="`msg-${msg.role}`">
        <div class="msg-content">{{ msg.content }}</div>
        <div class="msg-time">{{ msg.time }}</div>
      </div>
      <div v-if="streaming" class="chat-msg msg-assistant">
        <div class="msg-content">{{ streamText }}<span class="cursor-blink">|</span></div>
      </div>
    </div>
    <div class="chat-input">
      <a-textarea
        v-model:value="input"
        :auto-size="{ minRows: 2, maxRows: 5 }"
        placeholder="输入消息… Shift+Enter 换行，Enter 发送"
        :disabled="streaming"
        @press-enter="handleSend" />
      <a-button type="primary" :disabled="!input.trim() || streaming" @click="handleSend">发送</a-button>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, nextTick } from 'vue';
  import { Empty } from 'ant-design-vue';

  defineOptions({ name: 'AiChatPanel' });
  defineProps({ pipelineId: { type: Number, default: undefined } });
  const emit = defineEmits<{ message: [content: string] }>();

  const messages = ref<Array<{ role: string; content: string; time: string }>>([]);
  const input = ref('');
  const streaming = ref(false);
  const streamText = ref('');

  const handleSend = async () => {
    const text = input.value.trim();
    if (!text) return;
    messages.value.push({ role: 'user', content: text, time: new Date().toLocaleTimeString() });
    input.value = '';
    emit('message', text);
    await nextTick();
  };

  // D-10: 快捷键（Ctrl+L / Ctrl+Shift+R / Esc）
  const handleKeydown = (e: KeyboardEvent) => {
    const isFocus = document.activeElement?.closest('.ai-chat-panel');
    if (!isFocus) return;

    if (e.ctrlKey && e.key === 'l') {
      e.preventDefault();
      e.stopPropagation();
      messages.value = [];
      streamText.value = '';
    } else if (e.ctrlKey && e.shiftKey && e.key === 'R') {
      e.preventDefault();
      e.stopPropagation();
      if (messages.value.length > 0) {
        const last = messages.value.pop();
        messages.value.pop();
        input.value = last?.content || '';
        handleSend();
      }
    } else if (e.key === 'Escape') {
      e.preventDefault();
      e.stopPropagation();
      streaming.value = false;
      streamText.value = '';
    }
  };
</script>

<style lang="less" scoped>
  .ai-chat-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    .chat-messages {
      flex: 1;
      overflow-y: auto;
      padding: 16px;
      .chat-empty {
        display: flex;
        align-items: center;
        justify-content: center;
        height: 100%;
      }
      .chat-msg {
        margin-bottom: 16px;
        .msg-content {
          line-height: 1.6;
          white-space: pre-wrap;
        }
        .msg-time {
          font-size: 11px;
          color: #bbb;
          margin-top: 4px;
        }
      }
      .msg-user {
        .msg-content {
          background: #e6f7ff;
          padding: 10px 14px;
          border-radius: 8px 8px 0 8px;
        }
        text-align: right;
      }
      .msg-assistant {
        .msg-content {
          background: #f5f5f5;
          padding: 10px 14px;
          border-radius: 8px 8px 8px 0;
        }
      }
    }
    .chat-input {
      display: flex;
      gap: 8px;
      padding: 12px 16px;
      border-top: 1px solid #f0f0f0;
      background: #fff;
      align-items: flex-end;
      :deep(.ant-input) {
        flex: 1;
      }
    }
    .cursor-blink {
      animation: blink 1s infinite;
    }
  }
  @keyframes blink {
    50% {
      opacity: 0;
    }
  }
</style>
