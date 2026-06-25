<template>
  <div class="confirm-bar">
    <div class="actions">
      <a-button v-if="showRollback" size="small" @click="$emit('rollback')"> 回退上一阶段 </a-button>
      <a-input v-if="showFeedback" v-model:value="feedbackText" size="small" placeholder="补充说明或追问..." style="flex: 1; max-width: 300px" />
      <a-button v-if="showAsk" size="small" @click="$emit('ask', feedbackText)"> 追问补充 </a-button>
      <a-button type="primary" size="small" @click="$emit('confirm', feedbackText)"> 确认并推进 </a-button>
    </div>
    <div v-if="waiting" class="waiting-hint"> <span class="dot"></span> AI 正在处理中... </div>
  </div>
</template>

<script setup lang="ts">
  import { ref } from 'vue';

  defineProps<{
    waiting?: boolean;
    showRollback?: boolean;
    showFeedback?: boolean;
    showAsk?: boolean;
  }>();

  defineEmits<{
    confirm: [feedback: string];
    rollback: [];
    ask: [feedback: string];
  }>();

  const feedbackText = ref('');
</script>

<style scoped lang="less">
  .confirm-bar {
    padding: 12px 16px;
    border-top: 1px solid #f0f0f0;
    background: #fafafa;

    .actions {
      display: flex;
      gap: 8px;
      align-items: center;
      justify-content: flex-end;
    }

    .waiting-hint {
      display: flex;
      align-items: center;
      gap: 6px;
      justify-content: center;
      margin-top: 8px;
      font-size: 13px;
      color: #1890ff;

      .dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: #1890ff;
        animation: pulse 1s infinite;
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
