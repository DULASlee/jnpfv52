/**
 * 统一编译网关 — JNPF 编译平台统一入口
 *
 * 一个入口，多端输出。根据编译目标自动路由到对应编译器。
 *
 * 编译链路：Schema → cleanSchema → IR → validateIR → 编译器 → CompileResult
 *
 * @jnpf-generated v5.2.0 type=compile-gateway platform=universal
 */

import type { FormPageIR } from '../ir/types';
import type { CompileTarget, CompileTargetMeta } from './targets';
import { COMPILE_TARGETS } from './targets';
import type { CompilerConfig, CompileResult, GeneratedProject } from './vue3/types';
import { Vue3Compiler } from './vue3/compiler';
import { cleanSchema } from '../ir/schema-cleaner';
import { validateIR } from '../ir/validator';

// ─── 请求 / 响应类型 ───

export interface CompileRequest {
  /** 原始 JSON Schema（来自 JNPF 平台） */
  schema: unknown;
  /** 编译目标 */
  target: CompileTarget;
  /** 编译配置 */
  config: Partial<CompilerConfig> & { entity: string };
}

export interface CompileResponse {
  success: boolean;
  project?: GeneratedProject;
  issues?: Array<{
    level: string;
    path: string;
    message: string;
  }>;
  warnings?: string[];
  complexExpressions?: string[];
  error?: string;
  /** 编译耗时(ms) */
  duration?: number;
  /** 目标元数据 */
  targetMeta?: CompileTargetMeta | null;
}

// ─── 统一编译网关 ───

export async function compileGateway(request: CompileRequest): Promise<CompileResponse> {
  const startTime = Date.now();

  try {
    // Step 1: 验证编译目标
    const targetMeta = COMPILE_TARGETS[request.target];
    if (!targetMeta) {
      return {
        success: false,
        error: `未知编译目标: ${request.target}`,
        duration: Date.now() - startTime,
      };
    }

    // Step 2: 清洗 Schema → IR
    const ir = cleanSchema(request.schema);

    // Step 3: 验证 IR
    const issues = validateIR(ir);
    const errors = issues.filter(i => i.level === 'error');
    if (errors.length > 0) {
      return {
        success: false,
        issues,
        error: `IR 验证失败: ${errors.length} 个错误`,
        duration: Date.now() - startTime,
        targetMeta,
      };
    }

    // Step 4: 根据目标选择编译器
    let result: CompileResult;

    switch (request.target) {
      case 'vue3-web': {
        const compiler = new Vue3Compiler(request.config);
        result = compiler.compile(ir as FormPageIR);
        break;
      }

      case 'dashboard':
      case 'dashboard-3d': {
        const { DashboardCompiler } = await import('./dashboard/compiler');
        const compiler = new DashboardCompiler(ir as unknown as import('../ir/dashboard-types').DashboardIR);
        result = compiler.compile();
        break;
      }

      case 'uniapp-weixin':
      case 'uniapp-alipay':
      case 'uniapp-douyin':
      case 'uniapp-h5': {
        const { UniAppCompiler } = await import('./uniapp/compiler');
        const platform = uniappTargetToPlatform(request.target);
        const compiler = new UniAppCompiler(request.config, platform);
        result = compiler.compile(ir as unknown as import('./uniapp/types').FormPageIR);
        break;
      }

      case 'uniapp-x-app': {
        // UniApp X — v5.0 暂缓，暂未实现
        throw new Error('uniapp-x-app 编译目标暂未实现 (v5.0 暂缓)');
      }

      case 'workflow': {
        const { FlowCompiler } = await import('./flow/compiler');
        const compiler = new FlowCompiler();
        const flowResult = compiler.compile(ir as unknown as import('../ir/flow-types').FlowIR);
        const project: GeneratedProject = new Map();
        project.set('workflow-config.json', flowResult.config);
        result = {
          project,
          warnings: flowResult.warnings,
          complexExpressions: [],
        };
        break;
      }

      default:
        return {
          success: false,
          error: `编译目标 ${request.target} 尚未实现`,
          duration: Date.now() - startTime,
          targetMeta,
        };
    }

    return {
      success: true,
      project: result.project,
      issues,
      warnings: result.warnings,
      complexExpressions: result.complexExpressions,
      duration: Date.now() - startTime,
      targetMeta,
    };
  } catch (e) {
    return {
      success: false,
      error: `编译失败: ${(e as Error).message}`,
      duration: Date.now() - startTime,
    };
  }
}

// ─── 批量编译 ───

export async function compileMultiTarget(
  schema: unknown,
  targets: CompileTarget[],
  config: Partial<CompilerConfig> & { entity: string },
): Promise<Map<CompileTarget, CompileResponse>> {
  const results = new Map<CompileTarget, CompileResponse>();
  for (const target of targets) {
    const response = await compileGateway({
      schema,
      target,
      config,
    });
    results.set(target, response);
  }
  return results;
}

// ─── 工具函数 ───

export function getTargetMeta(target: CompileTarget): CompileTargetMeta | null {
  return COMPILE_TARGETS[target] ?? null;
}

export function getAvailableTargets(irType?: 'form' | 'dashboard'): CompileTarget[] {
  const entries = Object.values(COMPILE_TARGETS);
  if (irType) {
    return entries.filter(t => t.irType === irType).map(t => t.id);
  }
  return entries.map(t => t.id);
}

function uniappTargetToPlatform(target: CompileTarget): 'mp-weixin' | 'mp-alipay' | 'mp-douyin' | 'h5' {
  const map: Record<string, 'mp-weixin' | 'mp-alipay' | 'mp-douyin' | 'h5'> = {
    'uniapp-weixin': 'mp-weixin',
    'uniapp-alipay': 'mp-alipay',
    'uniapp-douyin': 'mp-douyin',
    'uniapp-h5': 'h5',
  };
  return map[target] ?? 'mp-weixin';
}
