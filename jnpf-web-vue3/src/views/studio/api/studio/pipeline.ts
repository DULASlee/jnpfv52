/**
 * 流水线任务 API
 */
import { defHttp } from '/@/utils/http/axios';

export interface PipelineSummaryItem {
  id: number;
  name: string;
  currentStage: string;
  status: string;
  updatedAt?: string;
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
