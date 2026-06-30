/**
 * SA 门控 API（对齐后端 AIDevelopmentPipelineService.SaGate）
 *
 * 端点：
 *   POST /api/studio/pipeline/execute/{id}/upload-materials
 *   POST /api/studio/pipeline/execute/{id}/sa-gate
 */
import { defHttp } from '/@/utils/http/axios';

const baseUrl = '/api/studio/pipeline/execute';

/** 上传材料 */
export function uploadMaterials(pipelineId: string, data: UploadMaterialsRequest) {
  return defHttp.post({ url: `${baseUrl}/${pipelineId}/upload-materials`, data });
}

/** 触发门控评估 */
export function saGate(pipelineId: string, data: SaGateRequest) {
  return defHttp.post({ url: `${baseUrl}/${pipelineId}/sa-gate`, data });
}

// ─── 类型定义 ───

export interface UploadMaterialsRequest {
  files?: File[];
  textContent?: string;
  fileIds?: string[];
}

export interface SaGateRequest {
  materialId: string;
}
