/**
 * F-5.2 演示项目生成器
 * 用法: node scripts/generate-demo.mjs (需先 build 或直接引用 test helper)
 */
import { fileURLToPath } from 'node:url';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { execSync } from 'node:child_process';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PROJECT_ROOT = path.resolve(__dirname, '..');
const FIXTURES_DIR = path.join(PROJECT_ROOT, 'src/core/ir/__tests__/fixtures');
const OUTPUT_DIR = path.join(PROJECT_ROOT, 'examples/generated-student');

// 使用 vitest 的 transform 能力执行 TypeScript 编译
// 通过 vitest 的 Node API 运行编译器
console.log('Generating demo project...');
console.log('  Fixtures:', FIXTURES_DIR);
console.log('  Output:', OUTPUT_DIR);

// 调用 vitest 来执行生成
const vitestArgs = ['vitest', 'run', '--config', 'vitest.config.ts', 'scripts/generate-demo.test.ts', '--reporter=verbose'];

try {
  execSync(`npx ${vitestArgs.join(' ')}`, {
    cwd: PROJECT_ROOT,
    stdio: 'inherit',
    env: { ...process.env, FORCE_COLOR: '1' },
  });
} catch (err) {
  console.error('Generation failed:', err.message);
  process.exit(1);
}
