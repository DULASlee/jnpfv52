/**
 * F-5 演示项目生成脚本
 * 用法: npx tsx scripts/generate-demo.ts
 * 输出: examples/generated-student/
 */

import { cleanSchema } from '../src/core/ir/schema-cleaner';
import { Vue3Compiler } from '../src/core/compiler/vue3/compiler';
import * as fs from 'node:fs';
import * as path from 'node:path';

const outputDir = path.resolve(__dirname, '../examples/generated-student');

if (fs.existsSync(outputDir)) {
  fs.rmSync(outputDir, { recursive: true, force: true });
}
fs.mkdirSync(outputDir, { recursive: true });

const schema = {
  data: {
    formData: JSON.stringify({
      fields: [
        {
          __vModel__: 'name',
          __config__: { label: '姓名', tag: 'JnpfInput', jnpfKey: 'JnpfInput', required: true },
          placeholder: '请输入姓名',
          on: {},
        },
        {
          __vModel__: 'age',
          __config__: { label: '年龄', tag: 'JnpfInputNumber', jnpfKey: 'JnpfInputNumber' },
          on: {},
        },
        {
          __vModel__: 'status',
          __config__: { label: '状态', tag: 'JnpfSelect', jnpfKey: 'JnpfSelect' },
          options: [
            { label: '启用', value: 1 },
            { label: '禁用', value: 0 },
          ],
          on: {},
        },
      ],
      funcs: {},
      virtualFieldList: [
        { field: 'name', type: 'varchar', length: 50 },
        { field: 'age', type: 'int' },
        { field: 'status', type: 'int' },
      ],
      labelWidth: 100,
      popupType: 'general',
      generalWidth: '800px',
    }),
  },
};

const ir = cleanSchema(schema);
const compiler = new Vue3Compiler({ entity: 'student', entityLabel: '学生管理' });
const result = compiler.compile(ir);

console.log('Generated files:');
for (const [filePath, content] of result.project) {
  const fullPath = path.join(outputDir, filePath);
  fs.mkdirSync(path.dirname(fullPath), { recursive: true });
  fs.writeFileSync(fullPath, content, 'utf-8');
  console.log(`  ✅ ${filePath} (${content.length} chars)`);
}

// companion files
const packageJson = {
  name: 'jnpf-generated-student',
  version: '1.0.0',
  private: true,
  scripts: {
    dev: 'vite',
    build: 'vue-tsc --noEmit && vite build',
    preview: 'vite preview',
  },
  dependencies: {
    vue: '^3.4.0',
    'ant-design-vue': '^4.2.0',
    axios: '^1.7.0',
    dayjs: '^1.11.0',
  },
  devDependencies: {
    vite: '^5.4.0',
    '@vitejs/plugin-vue': '^5.1.0',
    'vue-tsc': '^2.0.0',
    typescript: '^5.5.0',
  },
};

fs.writeFileSync(path.join(outputDir, 'package.json'), JSON.stringify(packageJson, null, 2));

fs.writeFileSync(
  path.join(outputDir, 'vite.config.ts'),
  `import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
export default defineConfig({
  plugins: [vue()],
  server: { port: 3100 }
})
`,
);

fs.writeFileSync(
  path.join(outputDir, 'tsconfig.json'),
  JSON.stringify(
    {
      compilerOptions: {
        target: 'ES2020',
        module: 'ESNext',
        moduleResolution: 'bundler',
        strict: true,
        jsx: 'preserve',
        resolveJsonModule: true,
        isolatedModules: true,
        paths: { '@/*': ['./src/*'] },
      },
      include: ['src/**/*.ts', 'src/**/*.vue'],
    },
    null,
    2,
  ),
);

fs.writeFileSync(
  path.join(outputDir, 'index.html'),
  `<!DOCTYPE html>
<html lang="zh-CN">
<head><meta charset="UTF-8" /><meta name="viewport" content="width=device-width, initial-scale=1.0" /><title>学生管理 - JNPF Generated</title></head>
<body><div id="app"></div><script type="module" src="/src/main.ts"></script></body>
</html>`,
);

fs.writeFileSync(
  path.join(outputDir, 'src/main.ts'),
  `import { createApp } from 'vue'
import Antd from 'ant-design-vue'
import App from './App.vue'
import 'ant-design-vue/dist/reset.css'
createApp(App).use(Antd).mount('#app')
`,
);

fs.writeFileSync(
  path.join(outputDir, 'src/App.vue'),
  `<template>
  <a-config-provider>
    <StudentIndex />
  </a-config-provider>
</template>
<script setup lang="ts">
import StudentIndex from './views/student/index.vue'
</script>
`,
);

fs.writeFileSync(
  path.join(outputDir, 'src/env.d.ts'),
  `/// <reference types="vite/client" />
declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<{}, {}, any>
  export default component
}
`,
);

console.log(`\n✅ Demo project generated: ${outputDir}`);
if (result.warnings.length > 0) {
  console.log(`\nWarnings (${result.warnings.length}):`);
  result.warnings.forEach(w => console.log(`  ⚠️  ${w}`));
}
console.log('\nTo run:');
console.log('  cd examples/generated-student');
console.log('  pnpm install');
console.log('  pnpm dev');
