/**
 * AI ↔ 编译网关桥接层
 *
 * 将 AI 智能体产出的 IR 转换为编译网关可接受的 Schema，
 * 调用统一编译网关生成代码并打包 ZIP 下载。
 *
 * 编译链路：AgentIR → ir-to-schema → compileGateway → ZIP download
 *
 * @version 1.0.0
 * @module ai/integration/compile-bridge
 */

import type { FormPageIR } from '../../ir/types';
import { formIRToSchema } from '../../ir/ir-to-schema';
import type { CompileTarget } from '../../compiler/targets';
import { compileGateway, type CompileResponse } from '../../compiler/gateway';
import { compileExportMulti } from '../../compiler/bundler/index';
import { downloadZip } from '../../compiler/bundler/zip-bundler';

// ============================================================
// 类型
// ============================================================

export interface AIGeneratedPage {
  ir: Partial<FormPageIR>;
  entity: string;
  name: string;
}

export interface CompileBridgeResult {
  success: boolean;
  target: CompileTarget;
  response: CompileResponse;
  entity: string;
}

export interface BatchCompileResult {
  totalTargets: number;
  successCount: number;
  failureCount: number;
  results: CompileBridgeResult[];
  zipBlob?: Blob;
}

// ============================================================
// 编译桥接
// ============================================================

/**
 * 将 AI 生成的 IR 编译为单个目标代码。
 *
 * @param page - AI 生成的页面 IR
 * @param target - 编译目标
 * @returns 编译结果
 */
export async function compileAgentOutput(page: AIGeneratedPage, target: CompileTarget = 'vue3-web'): Promise<CompileBridgeResult> {
  // Step 1: IR → Schema（compileGateway 需要原始 JSON Schema 作为输入）
  const schema = formIRToSchema(page.ir as FormPageIR);

  // Step 2: 调用编译网关
  const response = await compileGateway({
    schema,
    target,
    config: {
      entity: page.entity,
    },
  });

  return {
    success: response.success,
    target,
    response,
    entity: page.entity,
  };
}

/**
 * 批量编译 AI 生成的 IR 到多个目标。
 *
 * @param page - AI 生成的页面 IR
 * @param targets - 编译目标列表（默认全部非VIP目标）
 * @returns 批量编译结果
 */
export async function compileAgentOutputMulti(
  page: AIGeneratedPage,
  targets: CompileTarget[] = ['vue3-web', 'uniapp-weixin', 'uniapp-h5'],
): Promise<BatchCompileResult> {
  const results: CompileBridgeResult[] = [];

  for (const target of targets) {
    const result = await compileAgentOutput(page, target);
    results.push(result);
  }

  const successCount = results.filter(r => r.success).length;
  const failureCount = results.filter(r => !r.success).length;

  return {
    totalTargets: targets.length,
    successCount,
    failureCount,
    results,
  };
}

/**
 * 编译并导出 ZIP 下载包。
 *
 * @param page - AI 生成的页面 IR
 * @param targets - 编译目标列表
 * @param fileName - 下载文件名
 */
export async function compileAndDownload(page: AIGeneratedPage, targets: CompileTarget[] = ['vue3-web'], fileName?: string): Promise<CompileBridgeResult> {
  const result = await compileAgentOutput(page, targets[0]);

  if (result.success && result.response.project) {
    // 单目标 ZIP 下载
    const { compileAndDownload: bundleDownload } = await import('../../compiler/bundler/zip-bundler');
    await bundleDownload(result.response.project, page.entity, targets[0]);
  }

  // 多目标时使用 compileExportMulti
  if (targets.length > 1) {
    const exportResponses = await compileExportMulti(
      page.ir,
      targets.map(t => ({ target: t, config: { entity: page.entity } })),
    );

    for (const exportResponse of exportResponses.values()) {
      if (exportResponse.success && exportResponse.blob) {
        downloadZip(exportResponse.blob, fileName ?? `${page.entity}_${new Date().toISOString().slice(0, 10)}.zip`);
        break;
      }
    }
  }

  return result;
}

/**
 * 将多个 AI 页面批量编译。
 */
export async function compileAgentPages(pages: AIGeneratedPage[], target: CompileTarget = 'vue3-web'): Promise<CompileBridgeResult[]> {
  const results = await Promise.all(pages.map(page => compileAgentOutput(page, target)));
  return results;
}

/**
 * 编译结果摘要（供工作台展示）。
 */
export function summarizeResult(result: CompileBridgeResult): {
  status: 'success' | 'failed';
  targetLabel: string;
  fileCount: number;
  issues: number;
  duration: number;
} {
  return {
    status: result.success ? 'success' : 'failed',
    targetLabel: result.target,
    fileCount: result.response.project?.size ?? 0,
    issues: result.response.issues?.length ?? 0,
    duration: result.response.duration ?? 0,
  };
}

/**
 * 批量编译结果摘要。
 */
export function summarizeBatchResult(result: BatchCompileResult): {
  summary: string;
  perTarget: Array<{ target: string; status: string; files: number }>;
} {
  return {
    summary: `${result.successCount}/${result.totalTargets} 目标编译成功`,
    perTarget: result.results.map(r => ({
      target: r.target,
      status: r.success ? '✅' : '❌',
      files: r.response.project?.size ?? 0,
    })),
  };
}
