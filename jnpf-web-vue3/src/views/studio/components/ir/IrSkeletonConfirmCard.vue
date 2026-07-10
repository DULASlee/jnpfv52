<template>
  <div v-if="visible" class="skeleton-confirm-card">
    <div class="card-header">
      <span class="badge">IR-0 骨架审阅</span>
      <a-tag color="orange">待确认</a-tag>
    </div>
    <p class="hint">请审阅业务事件与角色矩阵，确认后进入三轮需求分析。</p>
    <div v-if="summary" class="summary">{{ summary }}</div>
    <div v-if="expanded" class="payload-preview">
      <pre>{{ prettyPayload }}</pre>
    </div>
    <a-button type="link" size="small" @click="expanded = !expanded">
      {{ expanded ? '收起详情' : '展开 JSON' }}
    </a-button>
    <div class="actions">
      <a-button size="small" :loading="confirmLoading" type="primary" @click="$emit('confirm', true)"> 确认骨架并进入需求分析 </a-button>
      <a-button size="small" :loading="confirmLoading" @click="$emit('confirm', false)"> 仅确认骨架 </a-button>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed, ref } from 'vue';

  const props = defineProps<{
    visible: boolean;
    payload?: unknown;
    confirmLoading?: boolean;
  }>();

  defineEmits<{ confirm: [autoRunAnalyst: boolean] }>();

  const expanded = ref(false);

  const prettyPayload = computed(() => {
    if (!props.payload) return '';
    if (typeof props.payload === 'string') {
      try {
        return JSON.stringify(JSON.parse(props.payload), null, 2);
      } catch {
        return props.payload;
      }
    }
    return JSON.stringify(props.payload, null, 2);
  });

  const summary = computed(() => {
    let data: any = props.payload;
    if (typeof data === 'string') {
      try {
        data = JSON.parse(data);
      } catch {
        return '';
      }
    }
    if (!data || typeof data !== 'object') return '';
    const parts: string[] = [];
    if (Array.isArray(data.businessEvents)) parts.push(`${data.businessEvents.length} 个业务事件`);
    if (Array.isArray(data.roleMatrix)) parts.push(`${data.roleMatrix.length} 个角色`);
    if (Array.isArray(data.entityDrafts)) parts.push(`${data.entityDrafts.length} 个实体草案`);
    return parts.join(' · ');
  });
</script>

<style scoped lang="less">
  .skeleton-confirm-card {
    margin: 12px 0;
    padding: 12px 14px;
    border: 1px solid #ffd591;
    border-radius: 8px;
    background: #fffbe6;

    .card-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 8px;

      .badge {
        font-weight: 600;
        color: #d48806;
      }
    }

    .hint {
      margin: 0 0 8px;
      font-size: 13px;
      color: #595959;
    }

    .summary {
      font-size: 12px;
      color: #8c8c8c;
      margin-bottom: 8px;
    }

    .payload-preview {
      max-height: 200px;
      overflow: auto;
      background: #fafafa;
      border-radius: 4px;
      padding: 8px;
      margin-bottom: 8px;

      pre {
        margin: 0;
        font-size: 11px;
      }
    }

    .actions {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }
  }
</style>
