<template>
  <div v-if="visible" class="req-spec-confirm-card">
    <div class="card-header">
      <span class="badge">S2 需求分析说明书</span>
      <a-tag color="orange">待确认</a-tag>
    </div>
    <p v-if="documentTitle" class="doc-title">{{ documentTitle }}</p>
    <p class="hint">请你确认需求分析说明书，如果同意，推进到下一工作阶段，如果不满意，请在输入框继续提出你的问题和要求。</p>
    <div v-if="pmScore != null" class="pm-review" :class="{ weak: isLowScore }">
      <div class="pm-review-head">
        <span>PM 复审：{{ pmScore }} 分</span>
        <a-tag :color="isLowScore ? 'orange' : 'green'">{{ pmVerdict || (isLowScore ? '需补充' : '通过') }}</a-tag>
      </div>
      <ul v-if="isLowScore && pmGaps.length" class="pm-gaps">
        <li v-for="gap in pmGaps" :key="gap">{{ gap }}</li>
      </ul>
    </div>
    <div class="actions">
      <a-button size="small" :loading="previewLoading" @click="handlePreview">预览全文</a-button>
      <a-button size="small" @click="$emit('download')">下载 Markdown</a-button>
    </div>
    <div class="actions confirm-row">
      <a-button size="small" :loading="confirmLoading" type="primary" :disabled="isLowScore" @click="$emit('confirm', true)"> 确认并进入架构设计 </a-button>
      <a-button size="small" :loading="confirmLoading" :disabled="isLowScore" @click="$emit('confirm', false)"> 仅确认说明书 </a-button>
      <a-button v-if="isLowScore" size="small" danger :loading="confirmLoading" @click="handleForceConfirm"> 强制确认 </a-button>
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
  import { computed, ref } from 'vue';
  import { message as antMessage, Modal } from 'ant-design-vue';
  import { getPipelineDeliverableText } from '../../api/studio/pipeline';

  const props = defineProps<{
    visible: boolean;
    pipelineId: number;
    documentTitle?: string;
    relativePath?: string;
    confirmLoading?: boolean;
    pmScore?: number | null;
    pmGaps?: string[];
    pmVerdict?: string;
  }>();

  const emit = defineEmits<{ confirm: [autoRunDesign: boolean]; download: []; 'force-confirm': [autoRunDesign: boolean] }>();

  const previewOpen = ref(false);
  const previewContent = ref('');
  const previewLoading = ref(false);
  const pmGaps = computed(() => props.pmGaps ?? []);
  const isLowScore = computed(() => props.pmScore != null && props.pmScore < 85);

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

  function handleForceConfirm() {
    Modal.confirm({
      title: '确认强制推进？',
      content: 'PM 复审分数低于 85，建议先按缺口继续补充。强制确认后将进入下一工作阶段。',
      okText: '强制确认并进入架构设计',
      cancelText: '继续补充需求',
      onOk: () => emit('force-confirm', true),
    });
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

    .pm-review {
      margin: 0 0 10px;
      padding: 8px 10px;
      border: 1px solid #b7eb8f;
      border-radius: 6px;
      background: #f6ffed;

      &.weak {
        border-color: #ffd591;
        background: #fff7e6;
      }

      .pm-review-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 8px;
        font-size: 13px;
        font-weight: 600;
      }

      .pm-gaps {
        margin: 6px 0 0;
        padding-left: 18px;
        font-size: 12px;
        color: #874d00;
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
