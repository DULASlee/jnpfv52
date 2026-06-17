<template>
  <div class="message-bubble" :class="[role, contentType]">
    <!-- Avatar -->
    <div class="avatar">{{ role === 'assistant' ? 'AI' : 'U' }}</div>
    <!-- Content -->
    <div class="body">
      <template v-if="contentType === 'text'">
        <div class="text-content" v-html="renderedMarkdown"></div>
      </template>
      <template v-else-if="contentType === 'ir'">
        <IrPreviewCard :ir-data="parsedIR" />
      </template>
      <template v-else-if="contentType === 'document'">
        <div class="doc-content" v-html="renderedMarkdown"></div>
      </template>
      <template v-else>
        <div class="text-content">{{ content }}</div>
      </template>
      <div class="meta">
        <span>{{ timestamp }}</span>
        <span v-if="stage" class="stage-tag">{{ stage }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed } from 'vue';
  import IrPreviewCard from './IrPreviewCard.vue';

  const props = defineProps<{
    role: 'user' | 'assistant' | 'system';
    content: string;
    contentType?: 'text' | 'ir' | 'document';
    stage?: string;
    timestamp?: string;
  }>();

  const parsedIR = computed(() => {
    if (props.contentType !== 'ir') return null;
    try {
      return typeof props.content === 'string' ? JSON.parse(props.content) : props.content;
    } catch {
      return null;
    }
  });

  const renderedMarkdown = computed(() => {
    // Simple Markdown → HTML (bold, italic, code blocks, lists)
    return props.content
      .replace(/```(\w*)\n([\s\S]*?)```/g, '<pre class="code-block"><code>$2</code></pre>')
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      .replace(/^- (.+)$/gm, '<li>$1</li>')
      .replace(/\n/g, '<br>');
  });
</script>

<style scoped lang="less">
  .message-bubble {
    display: flex;
    gap: 12px;
    padding: 12px 16px;
    animation: fadeIn 0.3s;

    .avatar {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 12px;
      font-weight: 600;
      flex-shrink: 0;
    }

    &.user .avatar {
      background: #1890ff;
      color: #fff;
    }

    &.assistant .avatar {
      background: #52c41a;
      color: #fff;
    }

    .body {
      flex: 1;
      min-width: 0;

      .text-content,
      .doc-content {
        line-height: 1.6;
        word-break: break-word;
      }

      :deep(.code-block) {
        background: #1e1e1e;
        color: #d4d4d4;
        padding: 12px;
        border-radius: 6px;
        overflow-x: auto;
        margin: 8px 0;
        font-size: 13px;
      }

      :deep(code) {
        background: #f0f0f0;
        padding: 2px 6px;
        border-radius: 4px;
        font-size: 13px;
      }

      .meta {
        display: flex;
        gap: 8px;
        margin-top: 6px;
        font-size: 11px;
        color: #999;

        .stage-tag {
          background: #e6f7ff;
          color: #1890ff;
          padding: 0 6px;
          border-radius: 4px;
        }
      }
    }
  }

  @keyframes fadeIn {
    from {
      opacity: 0;
      transform: translateY(8px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
</style>
