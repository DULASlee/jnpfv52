<template>
  <div v-if="visible" class="req-spec-confirm-card">
    <div class="card-header">
      <span class="badge">S2 需求分析说明书</span>
      <a-tag color="orange">待确认</a-tag>
    </div>
    <p v-if="documentTitle" class="doc-title">{{ documentTitle }}</p>
    <p class="hint">
      请你确认需求分析说明书。赶进度演示可直接点「确认并进入架构设计」；PM 分数低于 85 时会留痕继续，不阻断全链条。
    </p>
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
      <a-button size="small" :loading="downloadLoading" @click="handleDownload">下载 Markdown</a-button>
    </div>
    <div class="actions confirm-row">
      <a-button size="small" :loading="confirmLoading" type="primary" @click="$emit('confirm', true)">
        确认并进入架构设计
      </a-button>
      <a-button size="small" :loading="confirmLoading" @click="$emit('confirm', false)"> 仅确认说明书 </a-button>
      <a-button v-if="isLowScore" size="small" danger :loading="confirmLoading" @click="handleForceConfirm">
        强制确认（低分留痕）
      </a-button>
    </div>

    <a-modal
      v-model:visible="previewOpen"
      :title="documentTitle || '需求分析规格说明书'"
      width="860px"
      :footer="null"
      destroy-on-close>
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
  import { getRequirementSpecContent } from '../../api/studio/skills';
  import { downloadByData } from '/@/utils/file/download';
  import {
    isFormalRequirementSpec,
    isRequirementSpecRendered,
    pickRequirementSpecMarkdown,
    REQUIREMENT_SPEC_PATH,
    unwrapStudioApi,
    type RequirementSpecContentPayload,
  } from '../../utils/requirementSpec';

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
  const downloadLoading = ref(false);
  const pmGaps = computed(() => props.pmGaps ?? []);
  const isLowScore = computed(() => props.pmScore != null && props.pmScore < 85);

  /** 刷新并返回正式版 Markdown（预览/下载共用 spec-content 接口） */
  async function loadFormalSpecText(): Promise<string> {
    if (!props.pipelineId) throw new Error('流水线 ID 无效');
    const payload = unwrapStudioApi<RequirementSpecContentPayload>(await getRequirementSpecContent(props.pipelineId));
    const text = pickRequirementSpecMarkdown(payload);
    if (!text) throw new Error('需求说明书尚未生成，请稍候重试');
    if (!isRequirementSpecRendered(payload) || !isFormalRequirementSpec(text)) {
      throw new Error('当前内容不是正式版需求说明书，请重新运行需求分析步骤④');
    }
    return text;
  }

  async function handlePreview() {
    if (!props.pipelineId) return;
    previewLoading.value = true;
    previewContent.value = '';
    previewOpen.value = true;
    try {
      previewContent.value = await loadFormalSpecText();
    } catch (e: any) {
      previewOpen.value = false;
      antMessage.error(e?.response?.data?.msg ?? e?.message ?? '预览失败');
    } finally {
      previewLoading.value = false;
    }
  }

  async function handleDownload() {
    if (!props.pipelineId) return;
    downloadLoading.value = true;
    try {
      const text = await loadFormalSpecText();
      const blob = new Blob([text], { type: 'text/markdown;charset=utf-8' });
      downloadByData(blob, REQUIREMENT_SPEC_PATH);
      emit('download');
    } catch (e: any) {
      antMessage.error(e?.response?.data?.msg ?? e?.message ?? '下载失败');
    } finally {
      downloadLoading.value = false;
    }
  }

  function handleForceConfirm() {
    Modal.confirm({
      title: '确认强制推进？',
      content: 'PM 复审分数低于 85。赶进度模式下仍可强制确认并进入架构设计（会写入 IR 留痕）。',
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
