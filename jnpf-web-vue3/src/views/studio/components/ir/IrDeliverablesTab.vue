<template>
  <div class="ir-deliverables-tab">
    <div v-if="!pipelineId" class="tab-empty">
      <span class="empty-icon">📦</span>
      <p>流水线启动后，需求附件与阶段交付物将在此展示</p>
    </div>
    <template v-else>
      <section v-if="attachments.length" class="block">
        <div class="block-title">需求附件</div>
        <div v-for="att in attachments" :key="att.id ?? att.Id" class="item-row">
          <span class="item-name" :title="att.fileName ?? att.FileName">📎 {{ att.fileName ?? att.FileName }}</span>
          <a-tag size="small" :color="materials.attachmentStatusColor(att.processStatus ?? att.ProcessStatus)">
            {{ materials.attachmentStatusText(att.processStatus ?? att.ProcessStatus) }}
          </a-tag>
          <a-button v-if="att.downloadOriginalUrl ?? att.DownloadOriginalUrl" size="small" type="link" @click="materials.downloadAttachment(att, 'original')"
            >原文件</a-button
          >
          <a-button v-if="att.downloadExtractedUrl ?? att.DownloadExtractedUrl" size="small" type="link" @click="materials.downloadAttachment(att, 'extracted')"
            >解析文本</a-button
          >
        </div>
      </section>

      <section v-for="group in deliverableGroups" :key="group.stageCode" class="block">
        <div class="block-title">{{ group.label }}</div>
        <div v-for="d in group.items" :key="d.id ?? d.Id" class="item-row">
          <span class="item-name" :title="d.fileName ?? d.FileName">📄 {{ d.fileName ?? d.FileName }}</span>
          <span v-if="d.fileSize ?? d.FileSize" class="item-meta">{{ materials.formatFileSize(d.fileSize ?? d.FileSize) }}</span>
          <a-button size="small" type="link" @click="materials.downloadDeliverable(d)">下载</a-button>
        </div>
      </section>

      <div v-if="!attachments.length && !deliverableGroups.length" class="tab-empty compact">
        <p>暂无附件或交付物</p>
      </div>

      <section v-if="currentStage >= 5" class="block delivery-block">
        <div class="block-title">交付验证</div>
        <a-space wrap>
          <a-button size="small" type="primary" :loading="deliveryLoading" @click="materials.triggerDeliveryArtifacts()">
            {{ deliveryArtifacts?.previewUrl ? '重新生成' : '生成预览与源码包' }}
          </a-button>
          <a-button v-if="deliveryArtifacts?.previewUrl" size="small" @click="materials.openUrl(deliveryArtifacts.previewUrl)">试用链接</a-button>
          <a-button v-if="deliveryArtifacts?.downloadUrl" size="small" @click="materials.openUrl(deliveryArtifacts.downloadUrl)">源码 ZIP</a-button>
        </a-space>
        <div v-if="deliveryError" class="delivery-error">{{ deliveryError }}</div>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
  import { computed, inject } from 'vue';
  import { IR_OBSERVATORY_KEY } from '../../composables/useIrObservatory';
  import { PIPELINE_MATERIALS_KEY, groupDeliverables } from '../../composables/usePipelineMaterials';

  const props = defineProps<{ currentStage?: number }>();

  const ir = inject(IR_OBSERVATORY_KEY)!;
  const materials = inject(PIPELINE_MATERIALS_KEY)!;

  const pipelineId = computed(() => ir.pipelineId.value);
  const attachments = computed(() => materials.attachments.value);
  const deliverableGroups = computed(() => groupDeliverables(materials.deliverables.value));
  const deliveryArtifacts = computed(() => materials.deliveryArtifacts.value);
  const deliveryLoading = computed(() => materials.deliveryLoading.value);
  const deliveryError = computed(() => materials.deliveryError.value);
  const currentStage = computed(() => props.currentStage ?? 1);
</script>

<style scoped lang="less">
  .ir-deliverables-tab {
    height: 100%;
    overflow-y: auto;
    padding: 4px 0;

    .tab-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 32px 16px;
      color: #999;
      text-align: center;

      &.compact {
        padding: 16px;
      }

      .empty-icon {
        font-size: 28px;
        margin-bottom: 8px;
      }

      p {
        margin: 0;
        font-size: 13px;
      }
    }

    .block {
      margin-bottom: 16px;

      .block-title {
        font-size: 12px;
        font-weight: 600;
        color: #666;
        margin-bottom: 8px;
      }
    }

    .item-row {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 6px;
      padding: 8px 10px;
      margin-bottom: 6px;
      background: #fafafa;
      border: 1px solid #f0f0f0;
      border-radius: 6px;
      font-size: 12px;

      .item-name {
        flex: 1;
        min-width: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .item-meta {
        color: #8c8c8c;
      }
    }

    .delivery-block {
      padding-top: 12px;
      border-top: 1px dashed #e8e8e8;

      .delivery-error {
        margin-top: 8px;
        font-size: 11px;
        color: #cf1322;
      }
    }
  }
</style>
