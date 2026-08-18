/**
 * 流水线任务 API
 */
import { defHttp } from '/@/utils/http/axios';

export interface PipelineSummaryItem {
  id: number;
  name: string;
  currentStage: string;
  status: string;
  updatedAt?: string | number;
  createdAt?: string | number;
  creatorUserId?: string;
  creatorUserName?: string;
}

export function getPipelineList(pageIndex = 0, pageSize = 20) {
  return defHttp.get<PipelineSummaryItem[]>({
    url: '/api/studio/pipeline/execute/list',
    params: { pageIndex, pageSize },
  });
}

export function getDeliveryPackage(pipelineId: number) {
  return defHttp.get<{ downloadUrl: string; fileName: string }>({
    url: `/api/studio/pipeline/execute/${pipelineId}/delivery-package`,
  });
}

export function startPreview(pipelineId: number) {
  return defHttp.post<{ previewUrl: string; sandboxId: string }>({
    url: `/api/studio/pipeline/execute/${pipelineId}/preview`,
  });
}

export interface PipelineAttachmentItem {
  id?: string;
  fileName: string;
  processStatus: number;
  extractedLength?: number;
  error?: string;
}

export interface UploadMaterialsResult {
  pipelineId: number;
  registered: number;
  processed: number;
  failed: number;
  warnings?: string[];
  items?: PipelineAttachmentItem[];
}

/** 附件登记 + 解析入库（inte_assistant_attachment，ProcessStatus=2） */
export function uploadPipelineMaterials(pipelineId: number, attachments: Array<{ name: string; url: string }>) {
  return defHttp.post<UploadMaterialsResult>({
    url: `/api/studio/pipeline/execute/${pipelineId}/upload-materials`,
    data: {
      attachments: attachments.map(a => ({ name: a.name, url: a.url })),
    },
  });
}

export interface PipelineAttachmentListItem {
  id: string;
  fileName: string;
  fileUrl: string;
  fileType: string;
  fileSize: number;
  processStatus: number;
  extractedLength: number;
  processError?: string;
  createTime?: string;
  downloadOriginalUrl: string;
  downloadExtractedUrl: string;
}

export interface PipelineDeliverableItem {
  id: string;
  stageCode: string;
  fileName: string;
  relativePath: string;
  contentType: string;
  fileSize: number;
  createTime?: string;
  downloadUrl: string;
}

export function getPipelineAttachments(pipelineId: number) {
  return defHttp.get<{ pipelineId: number; count: number; items: PipelineAttachmentListItem[] }>({
    url: `/api/studio/pipeline/execute/${pipelineId}/attachments`,
  });
}

export function getPipelineDeliverables(pipelineId: number, stageCode?: string) {
  return defHttp.get<{ pipelineId: number; count: number; items: PipelineDeliverableItem[] }>({
    url: `/api/studio/pipeline/execute/${pipelineId}/deliverables`,
    params: stageCode ? { stageCode } : undefined,
  });
}

export async function downloadPipelineAttachmentBlob(pipelineId: number, attachmentId: string, kind: 'original' | 'extracted') {
  const suffix = kind === 'original' ? 'download' : 'extracted';
  return defHttp.get(
    { url: `/api/studio/pipeline/execute/${pipelineId}/attachments/${attachmentId}/${suffix}`, responseType: 'blob' },
    { isReturnNativeResponse: true },
  );
}

export async function downloadPipelineDeliverableBlob(pipelineId: number, relativePath: string) {
  return defHttp.get(
    {
      url: `/api/studio/pipeline/execute/${pipelineId}/deliverables/content`,
      params: { relativePath },
      responseType: 'blob',
    },
    { isReturnNativeResponse: true },
  );
}

/** 读取交付物 Markdown 文本（预览用） */
export async function getPipelineDeliverableText(pipelineId: number, relativePath: string) {
  const res = await downloadPipelineDeliverableBlob(pipelineId, relativePath);
  const blob: Blob = (res as any)?.data ?? res;
  return blob.text();
}

/** 触发 SA 需求门控（异步；结果通过 GET /events SSE 推送） */
export function triggerSaGate(pipelineId: number, userText: string, autoRunPm?: boolean, attachments?: Array<{ name: string; url: string }>) {
  return defHttp.post({
    url: `/api/studio/pipeline/execute/${pipelineId}/sa-gate`,
    data: {
      userText,
      autoRunPm,
      attachments: attachments?.map(a => ({ name: a.name, url: a.url })),
    },
  });
}

export interface CreatePipelinePayload {
  requirement?: string;
  name?: string;
  workMode?: 'greenfield' | 'bugfix' | 'enhancement';
  sourcePipelineId?: number;
  targetPageRoute?: string;
  targetPageLabel?: string;
}

export interface CreatePipelineResult {
  pipelineId?: number;
  PipelineId?: number;
  id?: number;
  Id?: number;
  data?: CreatePipelineResult;
}

export function createPipeline(data: CreatePipelinePayload) {
  return defHttp.post<CreatePipelineResult>({
    url: '/api/studio/pipeline/execute/create',
    data,
  });
}

export function getGeneratedProjectList(page = 1, pageSize = 50) {
  return defHttp.get<{ total: number; items: Array<{ id: number; projectName: string; sandboxUrl?: string }> }>({
    url: '/api/studio/ai/project/list',
    params: { page, pageSize },
  });
}

export function getPageRoutes(pipelineId: number) {
  return defHttp.get<{ items: Array<{ route: string; label: string }> }>({
    url: `/api/studio/pipeline/execute/${pipelineId}/page-routes`,
  });
}

export function quickBugfix(pipelineId: number, message: string) {
  return defHttp.post({
    url: `/api/studio/pipeline/execute/${pipelineId}/quick-bugfix`,
    data: { message },
  });
}

export function quickEnhancement(pipelineId: number, message: string) {
  return defHttp.post({
    url: `/api/studio/pipeline/execute/${pipelineId}/quick-enhancement`,
    data: { message },
  });
}

export function freezePipeline(pipelineId: number, reason?: string) {
  return defHttp.post({
    url: `/api/studio/pipeline/execute/${pipelineId}/freeze`,
    data: { reason: reason ?? '用户冻结' },
  });
}

export function resumePipeline(pipelineId: number) {
  return defHttp.post({
    url: `/api/studio/pipeline/execute/${pipelineId}/resume`,
  });
}

export function forkPipeline(pipelineId: number, data?: { name?: string; workMode?: string }) {
  return defHttp.post<{ pipelineId: number; projectId: string }>({
    url: `/api/studio/pipeline/execute/${pipelineId}/fork`,
    data: data ?? { workMode: 'enhancement' },
  });
}
