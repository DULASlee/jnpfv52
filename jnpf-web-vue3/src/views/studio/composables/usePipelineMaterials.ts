import { ref, watch, type ComputedRef, type InjectionKey, type Ref } from 'vue';
import {
  downloadPipelineAttachmentBlob,
  downloadPipelineDeliverableBlob,
  getDeliveryPackage,
  getPipelineAttachments,
  getPipelineDeliverables,
  startPreview,
} from '../api/studio/pipeline';
import { downloadByData } from '/@/utils/file/download';
import { message as antMessage } from 'ant-design-vue';

export interface PipelineMaterialsContext {
  attachments: Ref<any[]>;
  deliverables: Ref<any[]>;
  deliveryArtifacts: Ref<{ previewUrl?: string; downloadUrl?: string; fileName?: string } | null>;
  deliveryLoading: Ref<boolean>;
  deliveryError: Ref<string>;
  refresh: () => Promise<void>;
  downloadAttachment: (att: any, kind: 'original' | 'extracted') => Promise<void>;
  downloadDeliverable: (d: any) => Promise<void>;
  triggerDeliveryArtifacts: (showToast?: boolean) => Promise<void>;
  openUrl: (url: string) => void;
  attachmentStatusText: (status: number) => string;
  attachmentStatusColor: (status: number) => string;
  formatFileSize: (bytes: number) => string;
}

export const PIPELINE_MATERIALS_KEY: InjectionKey<PipelineMaterialsContext> = Symbol('pipelineMaterials');

const DELIVERABLE_STAGE_LABELS: Record<string, string> = {
  S0: 'S0 门控交付物',
  S1: 'S1 产品骨架',
  S2: 'S2 需求分析说明书',
  S3: 'S3 架构设计',
  S4: 'S4 详细设计',
  S5: 'S5 开发交付',
  S6: 'S6 测试报告',
  S7: 'S7 部署包',
};

export function groupDeliverables(items: any[]) {
  const map = new Map<string, any[]>();
  for (const d of items) {
    const stage = d.stageCode ?? d.StageCode ?? 'S0';
    if (!map.has(stage)) map.set(stage, []);
    map.get(stage)!.push(d);
  }
  return [...map.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([stageCode, groupItems]) => ({
      stageCode,
      label: DELIVERABLE_STAGE_LABELS[stageCode] ?? `${stageCode} 交付物`,
      items: groupItems,
    }));
}

export function usePipelineMaterials(
  pipelineId: Ref<number> | ComputedRef<number>,
  currentStage?: Ref<number> | ComputedRef<number>,
): PipelineMaterialsContext {
  const attachments = ref<any[]>([]);
  const deliverables = ref<any[]>([]);
  const deliveryArtifacts = ref<{ previewUrl?: string; downloadUrl?: string; fileName?: string } | null>(null);
  const deliveryLoading = ref(false);
  const deliveryError = ref('');

  async function refresh() {
    const pid = pipelineId.value;
    if (!pid) {
      attachments.value = [];
      deliverables.value = [];
      return;
    }
    try {
      const attRes: any = await getPipelineAttachments(pid);
      const attData = attRes?.data ?? attRes;
      attachments.value = attData?.items ?? attData?.Items ?? [];
    } catch {
      attachments.value = [];
    }
    try {
      const delRes: any = await getPipelineDeliverables(pid);
      const delData = delRes?.data ?? delRes;
      deliverables.value = delData?.items ?? delData?.Items ?? [];
    } catch {
      deliverables.value = [];
    }
  }

  function attachmentStatusText(status: number) {
    return ({ 0: '待处理', 1: '解析中', 2: '已解析', 3: '失败' } as Record<number, string>)[status] ?? '未知';
  }

  function attachmentStatusColor(status: number) {
    return ({ 0: 'default', 1: 'processing', 2: 'success', 3: 'error' } as Record<number, string>)[status] ?? 'default';
  }

  function formatFileSize(bytes: number) {
    if (!bytes) return '';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  async function downloadAttachment(att: any, kind: 'original' | 'extracted') {
    const pid = pipelineId.value;
    if (!pid) return;
    try {
      const res = await downloadPipelineAttachmentBlob(pid, att.id ?? att.Id, kind);
      const blob = res?.data ?? res;
      const name =
        kind === 'original' ? att.fileName ?? att.FileName : `${(att.fileName ?? att.FileName ?? 'attachment').replace(/\.[^.]+$/, '')}-extracted.txt`;
      downloadByData(blob, name);
    } catch (e: any) {
      antMessage.error(e?.message ?? '下载失败');
    }
  }

  async function downloadDeliverable(d: any) {
    const pid = pipelineId.value;
    if (!pid) return;
    try {
      const relativePath = d.relativePath ?? d.RelativePath;
      const res = await downloadPipelineDeliverableBlob(pid, relativePath);
      const blob = res?.data ?? res;
      downloadByData(blob, d.fileName ?? d.FileName ?? 'deliverable');
    } catch (e: any) {
      antMessage.error(e?.message ?? '下载失败');
    }
  }

  function openUrl(url: string) {
    if (!url) return;
    window.open(url.startsWith('http') ? url : window.location.origin + url, '_blank');
  }

  async function triggerDeliveryArtifacts(showToast = true) {
    const pid = pipelineId.value;
    if (!pid) return;
    deliveryLoading.value = true;
    deliveryError.value = '';
    try {
      let previewUrl = '';
      let downloadUrl = '';
      let fileName = '';
      try {
        const preview = await startPreview(pid);
        const p = (preview as any)?.data ?? preview;
        previewUrl = p?.previewUrl || '';
      } catch (e: any) {
        deliveryError.value = '预览：' + (e?.message || 'generated/ 目录可能为空，需先完成自动开发');
      }
      try {
        const pkg = await getDeliveryPackage(pid);
        const d = (pkg as any)?.data ?? pkg;
        downloadUrl = d?.downloadUrl || '';
        fileName = d?.fileName || 'delivery.zip';
      } catch (e: any) {
        deliveryError.value = (deliveryError.value ? deliveryError.value + '；' : '') + 'ZIP：' + (e?.message || '打包失败');
      }
      if (previewUrl || downloadUrl) {
        deliveryArtifacts.value = { previewUrl, downloadUrl, fileName };
        if (showToast) antMessage.success('交付物已生成');
      }
    } finally {
      deliveryLoading.value = false;
    }
  }

  watch(
    pipelineId,
    id => {
      deliveryArtifacts.value = null;
      deliveryError.value = '';
      if (id > 0) {
        void refresh();
        if (currentStage && currentStage.value >= 5) {
          void triggerDeliveryArtifacts(false);
        }
      } else {
        attachments.value = [];
        deliverables.value = [];
      }
    },
    { immediate: true },
  );

  return {
    attachments,
    deliverables,
    deliveryArtifacts,
    deliveryLoading,
    deliveryError,
    refresh,
    downloadAttachment,
    downloadDeliverable,
    triggerDeliveryArtifacts,
    openUrl,
    attachmentStatusText,
    attachmentStatusColor,
    formatFileSize,
  };
}
