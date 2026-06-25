/**
 * 编译网关 + 打包 + 下载 一站式集成
 *
 * 串联 compileGateway → bundleToZip → downloadZip
 *
 * @jnpf-generated v5.2.0 type=bundler-index platform=universal
 */

import { compileGateway, type CompileRequest, type CompileResponse } from '../gateway';
import { bundleToZip, downloadZip } from './zip-bundler';
import type { CompileTarget } from '../targets';

export interface ExportRequest extends CompileRequest {
  /** 是否立即下载（默认 true） */
  autoDownload?: boolean;
}

export interface ExportResponse extends CompileResponse {
  /** ZIP Blob（仅成功时有值） */
  blob?: Blob;
  /** 文件名 */
  fileName?: string;
}

/**
 * 编译 + 打包 + 下载 一站式接口
 */
export async function compileExport(request: ExportRequest): Promise<ExportResponse> {
  const response = await compileGateway(request);

  if (!response.success || !response.project) {
    return { ...response };
  }

  const fileName = `jnpf-${request.config.entity}-${request.target}`;
  const blob = await bundleToZip(response.project, {
    fileName,
    includeReadme: true,
    includeGitignore: true,
  });

  if (request.autoDownload !== false) {
    downloadZip(blob, fileName);
  }

  return {
    ...response,
    blob,
    fileName,
  };
}

/**
 * 批量编译 + 打包
 */
export async function compileExportMulti(
  schema: unknown,
  targets: Array<{
    target: CompileTarget;
    config: Partial<{ entity: string }> & {
      entity: string;
    };
  }>,
): Promise<Map<string, ExportResponse>> {
  const results = new Map<string, ExportResponse>();
  for (const { target, config } of targets) {
    const response = await compileExport({
      schema,
      target,
      config,
      autoDownload: false,
    });
    results.set(target, response);
  }
  return results;
}
