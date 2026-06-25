/**
 * useCompile — 工作台编译逻辑 composable
 *
 * 为 workbench.vue 提供编译状态管理和操作：
 *   - compile()  编译 AI IR → 代码
 *   - download() 下载 ZIP 包
 *   - state      编译状态（idle/compiling/success/failed）
 *
 * @module ai/integration/use-compile
 */

import { ref, reactive } from 'vue';
import type { CompileTarget } from '../../compiler/targets';
import {
  compileAgentOutput,
  compileAndDownload,
  compileAgentOutputMulti,
  summarizeResult,
  summarizeBatchResult,
  type AIGeneratedPage,
  type CompileBridgeResult,
  type BatchCompileResult,
} from './compile-bridge';

export function useCompile() {
  const isCompiling = ref(false);
  const lastResult = ref<CompileBridgeResult | null>(null);
  const batchResult = ref<BatchCompileResult | null>(null);
  const compileLog = reactive<Array<{ time: string; msg: string }>>([]);

  function log(msg: string) {
    compileLog.push({ time: new Date().toLocaleTimeString(), msg });
  }

  async function compile(page: AIGeneratedPage, target: CompileTarget = 'vue3-web'): Promise<CompileBridgeResult> {
    isCompiling.value = true;
    log(`开始编译: ${page.name} → ${target}`);

    try {
      const result = await compileAgentOutput(page, target);
      lastResult.value = result;

      if (result.success) {
        const summary = summarizeResult(result);
        log(`✅ 编译成功: ${summary.fileCount} 文件, ${summary.duration}ms`);
      } else {
        log(`❌ 编译失败: ${result.response.error ?? '未知错误'}`);
      }

      return result;
    } catch (e) {
      log(`❌ 编译异常: ${(e as Error).message}`);
      throw e;
    } finally {
      isCompiling.value = false;
    }
  }

  async function compileMulti(page: AIGeneratedPage, targets: CompileTarget[]): Promise<BatchCompileResult> {
    isCompiling.value = true;
    log(`批量编译: ${page.name} → ${targets.join(', ')}`);

    try {
      const result = await compileAgentOutputMulti(page, targets);
      batchResult.value = result;
      const summary = summarizeBatchResult(result);
      log(summary.summary);
      return result;
    } finally {
      isCompiling.value = false;
    }
  }

  async function download(page: AIGeneratedPage, targets: CompileTarget[] = ['vue3-web'], fileName?: string) {
    log(`准备下载: ${fileName ?? page.entity}.zip`);
    await compileAndDownload(page, targets, fileName);
  }

  return {
    isCompiling,
    lastResult,
    batchResult,
    compileLog,
    compile,
    compileMulti,
    download,
  };
}
