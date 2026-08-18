// RetryLoop - "生成→验证→自修复"闭环
// 每个 SA 步骤都用这个循环跑

import { ValidationError, ISADatabase, SAContext, ValidationLogRecord } from './orchestrator-types';

export interface RetryLoopConfig {
  maxRetries: number;
  retryDelayMs: number;
}

export interface RetryResult<T> {
  output: T;
  attempts: number;
  validationPassed: boolean;
  errors: ValidationError[];
}

/**
 * 生成 → 验证 → 错误回灌 → 重试
 */
export async function runWithRetry<T>(
  stepName: string,
  ctx: SAContext,
  db: ISADatabase,
  config: RetryLoopConfig,
  generate: () => Promise<T>,
  validate: (output: T) => { passed: boolean; errors: ValidationError[] }
): Promise<RetryResult<T>> {
  let lastOutput: T;
  let lastResult: { passed: boolean; errors: ValidationError[] } = { passed: false, errors: [] };

  for (let attempt = 1; attempt <= config.maxRetries; attempt++) {
    const startTime = Date.now();

    // Step 1: LLM 生成
    lastOutput = await generate();

    // Step 2: Validator 校验
    lastResult = validate(lastOutput);
    const duration = Date.now() - startTime;

    // Step 3: 写 sa_validation_log(含重试闭环)
    const logRecord: ValidationLogRecord = {
      tenantId: ctx.tenantId,
      projectId: ctx.projectId,
      pipelineId: ctx.pipelineId,
      saTableName: stepName,
      validatorName: stepName,
      retryCount: attempt - 1,
      previousErrors: ctx.lastErrors || null,
      isConverged: lastResult.passed,
      validationStatus: lastResult.passed ? 'PASS' : 'FAIL',
      errors: lastResult.errors,
      durationMs: duration,
    };
    await db.logValidation(logRecord);

    if (lastResult.passed) {
      return {
        output: lastOutput!,
        attempts: attempt,
        validationPassed: true,
        errors: [],
      };
    }

    // Step 4: 错误回灌到 ctx.lastErrors(下一轮 LLM 看到)
    ctx.lastErrors = lastResult.errors.map(e => `[${e.code}] ${e.message}`);
    console.warn(`[${stepName}] Attempt ${attempt}/${config.maxRetries} failed:`, lastResult.errors.length, 'errors');

    // 等待重试延迟(避免过快打 LLM)
    if (attempt < config.maxRetries) {
      await sleep(config.retryDelayMs);
    }
  }

  // 所有重试都失败
  throw new Error(
    `[${stepName}] 失败 ${config.maxRetries} 次仍未收敛。最后错误:\n` +
    (lastResult?.errors || []).map(e => `  [${e.code}] ${e.message}`).join('\n')
  );
}

function sleep(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}
