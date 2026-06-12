/**
 * F-5.2 演示项目生成器（vitest runner）
 *
 * 用法: npx vitest run scripts/generate-demo.test.ts
 */
import { describe, it, expect } from 'vitest';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { cleanSchema } from '../ir/schema-cleaner';
import { Vue3Compiler } from '../compiler/vue3/compiler';

const FIXTURES_DIR = path.resolve(__dirname, '../ir/__tests__/fixtures');
const OUTPUT_DIR = path.resolve(__dirname, '../../../examples/generated-student');

function writeIfChanged(filePath: string, content: string): void {
  const fullPath = path.join(OUTPUT_DIR, filePath);
  if (!fs.existsSync(path.dirname(fullPath))) {
    fs.mkdirSync(path.dirname(fullPath), { recursive: true });
  }
  fs.writeFileSync(fullPath, content, 'utf-8');
}

describe('F-5.2 Generate Demo Project', () => {
  it('generates student management demo from schema-multi-field.json', () => {
    // 1. Load schema
    const raw = JSON.parse(fs.readFileSync(path.join(FIXTURES_DIR, 'schema-multi-field.json'), 'utf-8'));

    // 2. Clean → IR
    const ir = cleanSchema(raw);
    expect(ir.fields.length).toBe(14);

    // 3. Compile
    const compiler = new Vue3Compiler({ entity: 'student', entityLabel: '学生管理' });
    const result = compiler.compile(ir);
    expect(result.project.size).toBe(7);

    // 4. Clean output directory
    if (fs.existsSync(OUTPUT_DIR)) {
      fs.rmSync(OUTPUT_DIR, { recursive: true, force: true });
    }

    // 5. Write generated files
    for (const [filePath, content] of result.project) {
      writeIfChanged(filePath, content);
    }

    // 6. Verify generated files
    expect(fs.existsSync(path.join(OUTPUT_DIR, 'src/types/student.ts'))).toBe(true);
    expect(fs.existsSync(path.join(OUTPUT_DIR, 'src/api/student.ts'))).toBe(true);
    expect(fs.existsSync(path.join(OUTPUT_DIR, 'src/views/student/index.vue'))).toBe(true);
    expect(fs.existsSync(path.join(OUTPUT_DIR, 'src/views/student/form.vue'))).toBe(true);
    expect(fs.existsSync(path.join(OUTPUT_DIR, 'src/views/student/columns.ts'))).toBe(true);
    expect(fs.existsSync(path.join(OUTPUT_DIR, 'src/views/student/search.ts'))).toBe(true);
    expect(fs.existsSync(path.join(OUTPUT_DIR, 'src/composables/useStudent.ts'))).toBe(true);

    // 7. Write wrapper files
    const pkg = {
      name: 'jnpf-generated-student',
      version: '1.0.0',
      private: true,
      type: 'module',
      scripts: {
        dev: 'vite --port 3200',
        build: 'vue-tsc --noEmit && vite build',
        preview: 'vite preview',
      },
      dependencies: {
        vue: '^3.4.0',
        'vue-router': '^4.3.0',
        'ant-design-vue': '^4.0.0',
        '@ant-design/icons-vue': '^7.0.0',
        axios: '^1.7.0',
      },
      devDependencies: {
        '@vitejs/plugin-vue': '^5.0.0',
        typescript: '^5.4.0',
        vite: '^5.2.0',
        'vue-tsc': '^2.0.0',
        less: '^4.2.0',
      },
    };
    writeIfChanged('package.json', JSON.stringify(pkg, null, 2) + '\n');

    writeIfChanged(
      'vite.config.ts',
      `import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  plugins: [vue()],
  server: { port: 3200, open: true },
});
`,
    );

    const tsconfig = {
      compilerOptions: {
        target: 'ES2020',
        module: 'ESNext',
        moduleResolution: 'bundler',
        strict: true,
        jsx: 'preserve',
        resolveJsonModule: true,
        isolatedModules: true,
        esModuleInterop: true,
        lib: ['ES2020', 'DOM', 'DOM.Iterable'],
        skipLibCheck: true,
        noEval: true,
      },
      include: ['src/**/*.ts', 'src/**/*.vue'],
    };
    writeIfChanged('tsconfig.json', JSON.stringify(tsconfig, null, 2) + '\n');

    writeIfChanged(
      'src/App.vue',
      `<template>
  <div id="app">
    <router-view />
  </div>
</template>

<script lang="ts" setup>
</script>

<style lang="less">
#app {
  height: 100vh;
  padding: 16px;
  background: #f0f2f5;
}
</style>
`,
    );

    writeIfChanged(
      'src/main.ts',
      `import { createApp } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import App from './App.vue';
import StudentList from './views/student/index.vue';

const routes = [
  { path: '/', redirect: '/student' },
  { path: '/student', name: 'StudentList', component: StudentList },
];

const router = createRouter({ history: createWebHistory(), routes });
const app = createApp(App);
app.use(router);
app.mount('#app');
`,
    );

    writeIfChanged(
      'index.html',
      `<!DOCTYPE html>
<html lang="zh-CN">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>学生管理 — JNPF Generated</title>
  </head>
  <body>
    <div id="app"></div>
    <script type="module" src="/src/main.ts"></script>
  </body>
</html>
`,
    );

    // 8. Verify wrapper files
    const wrapperFiles = ['package.json', 'vite.config.ts', 'tsconfig.json', 'src/App.vue', 'src/main.ts', 'index.html'];
    for (const f of wrapperFiles) {
      expect(fs.existsSync(path.join(OUTPUT_DIR, f))).toBe(true);
    }

    console.log(`\n✅ Demo project generated: ${OUTPUT_DIR}`);
    console.log('   cd examples/generated-student && pnpm install && pnpm dev\n');

    // 9. Quality: zero eval in all files
    const allFiles = fs.readdirSync(OUTPUT_DIR, { recursive: true }) as string[];
    for (const f of allFiles) {
      const fullPath = path.join(OUTPUT_DIR, f);
      if (fs.statSync(fullPath).isFile()) {
        const content = fs.readFileSync(fullPath, 'utf-8');
        expect(content).not.toMatch(/\beval\s*\(/);
        expect(content).not.toMatch(/new\s+Function\s*\(/);
      }
    }
  });
});
