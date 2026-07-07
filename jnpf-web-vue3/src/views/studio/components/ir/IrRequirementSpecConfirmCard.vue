<template>
  <div v-if="visible" class="req-spec-confirm-card">
    <div class="card-header">
      <span class="badge">S2 需求分析说明书</span>
      <a-tag color="orange">待确认</a-tag>
    </div>
    <p v-if="documentTitle" class="doc-title">{{ documentTitle }}</p>
    <p class="hint">请审阅《需求分析说明书》，确认后可进入架构设计；如需修改请继续在对话中说明。</p>
    <div class="actions">
      <a-button size="small" :loading="previewLoading" @click="handlePreview">预览全文</a-button>
      <a-button size="small" @click="$emit('download')">下载 Markdown</a-button>
    </div>
    <div class="actions confirm-row">
      <a-button size="small" :loading="confirmLoading" type="primary" @click="$emit('confirm', true)"> 确认并进入架构设计 </a-button>
      <a-button size="small" :loading="confirmLoading" @click="$emit('confirm', false)"> 仅确认说明书 </a-button>
    </div>

    <a-modal v-model:open="previewOpen" :title="documentTitle || '需求分析说明书'" width="860px" :footer="null">
      <div class="preview-body">
        <pre v-if="previewContent">{{ previewContent }}</pre>
        <a-spin v-else />
      </div>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
  import { ref } from 'vue';
  import { message as antMessage } from 'ant-design-vue';
  import { getPipelineDeliverableText } from '../../api/studio/pipeline';

  const props = defineProps<{
    visible: boolean;
    pipelineId: number;
    documentTitle?: string;
    relativePath?: string;
    confirmLoading?: boolean;
  }>();

  defineEmits<{ confirm: [autoRunDesign: boolean]; download: [] }>();

  const previewOpen = ref(false);
  const previewContent = ref('');
  const previewLoading = ref(false);

  async function handlePreview() {
    if (!props.pipelineId) return;
    previewLoading.value = true;
    previewOpen.value = true;
    previewContent.value = '';
    try {
      const path = props.relativePath ?? 'deliverables/02-requirement-spec.md';
      previewContent.value = await getPipelineDeliverableText(props.pipelineId, path);
    } catch (e: any) {
      antMessage.error(e?.message ?? '预览失败');
      previewOpen.value = false;
    } finally {
      previewLoading.value = false;
    }
  }
</script>

<style scoped lang="less">
  .req-spec-confirm-card {
    margin: 12px 0;
    padding: 12px 14px;
    border: 1px solid #91d5ff;
    border-radius: 8px;
    background: #e6f7ff;

    .card-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 8px;

      .badge {
        font-weight: 600;
        color: #096dd9;
      }
    }

    .doc-title {
      margin: 0 0 6px;
      font-weight: 600;
      color: #262626;
    }

    .hint {
      margin: 0 0 10px;
      font-size: 13px;
      color: #595959;
    }

    .actions {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
      margin-bottom: 8px;

      &.confirm-row {
        margin-bottom: 0;
      }
    }

    .preview-body {
      max-height: 70vh;
      overflow: auto;

      pre {
        margin: 0;
        font-size: 12px;
        white-space: pre-wrap;
        word-break: break-word;
      }
    }
  }
</style>
