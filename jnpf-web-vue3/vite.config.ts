import type { UserConfig, ConfigEnv } from 'vite';
import pkg from './package.json';
import dayjs from 'dayjs';
import { loadEnv } from 'vite';
import { resolve } from 'path';
import { generateModifyVars } from './build/generate/generateModifyVars';
import { createProxy } from './build/vite/proxy';
import { wrapperEnv } from './build/utils';
import { createVitePlugins } from './build/vite/plugin';
import { OUTPUT_DIR } from './build/constant';

function pathResolve(dir: string) {
  return resolve(process.cwd(), '.', dir);
}

const { dependencies, devDependencies, name, version } = pkg;
const __APP_INFO__ = {
  pkg: { dependencies, devDependencies, name, version },
  lastBuildTime: dayjs().format('YYYY-MM-DD HH:mm:ss'),
};

export default ({ command, mode }: ConfigEnv): UserConfig => {
  const root = process.cwd();

  const env = loadEnv(mode, root);

  // The boolean type read by loadEnv is a string. This function can be converted to boolean type
  const viteEnv = wrapperEnv(env);

  const { VITE_PORT, VITE_PUBLIC_PATH, VITE_PROXY, VITE_DROP_CONSOLE } = viteEnv;

  const isBuild = command === 'build';

  return {
    base: VITE_PUBLIC_PATH,
    root,
    resolve: {
      alias: [
        {
          find: 'vue-i18n',
          replacement: 'vue-i18n/dist/vue-i18n.cjs.js',
        },
        // /@/xxxx => src/xxxx
        {
          find: /\/@\//,
          replacement: pathResolve('src') + '/',
        },
        // /#/xxxx => types/xxxx
        {
          find: /\/#\//,
          replacement: pathResolve('types') + '/',
        },
      ],
    },
    server: {
      https: false,
      // Listening on all local IPs
      host: '0.0.0.0',
      port: VITE_PORT,
      strictPort: true,
      // Load proxy configuration from .env
      proxy: createProxy(VITE_PROXY),
      open: true,
      hmr: {
        overlay: true,
      },
      watch: {
        usePolling: false,
        interval: 1000,
      },
    },
    esbuild: {
      drop: VITE_DROP_CONSOLE ? ['console', 'debugger'] : [],
    },
    build: {
      target: 'es2015',
      cssTarget: 'chrome80',
      outDir: OUTPUT_DIR,
      // esbuild 压缩速度比 terser 快 10-100×，内存占用低一个数量级
      minify: 'esbuild',
      // 关闭 sourcemap — 生成 sourcemap 让 Rollup 内存翻倍
      sourcemap: false,
      // Turning off reportCompressedSize display can slightly reduce packaging time
      reportCompressedSize: false,
      chunkSizeWarningLimit: 500,
      rollupOptions: {
        input: {
          index: pathResolve('index.html'),
        },
        // 限制并行文件操作数，避免 IO 竞争导致内存峰值
        maxParallelFileOps: 8,
        output: {
          chunkFileNames: 'static/js/[name]-[hash].js',
          entryFileNames: 'static/js/[name]-[hash].js',
          assetFileNames: 'static/[ext]/[name]-[hash].[ext]',
          // 禁止 Rollup 生成大型内联 asset（如图片 base64），减少内存占用
          inlineDynamicImports: false,
          // 手动分包策略：将第三方库与业务代码分离，避免巨型 chunk 撑爆内存
          manualChunks: (id) => {
            if (id.includes('node_modules')) {
              // Vue 生态
              if (id.includes('/vue/') || id.includes('/pinia/') || id.includes('/vue-router/') || id.includes('/vue-i18n/') || id.includes('/@vue/')) {
                return 'vendor-vue';
              }
              // Ant Design Vue (核心组件库 ~600KB)
              if (id.includes('/ant-design-vue/')) {
                return 'vendor-antd';
              }
              // @ant-design/icons-vue (独立于 antd 的图标库，~500KB 打包后)
              if (id.includes('/@ant-design/')) {
                return 'vendor-icons';
              }
              // ECharts + ZRender (图表引擎，~1MB)
              if (id.includes('/echarts/') || id.includes('/zrender/')) {
                return 'vendor-echarts';
              }
              // 通用工具库
              if (id.includes('/lodash/') || id.includes('/dayjs/') || id.includes('/axios/') || id.includes('/moment/')) {
                return 'vendor-utils';
              }
              // VueUse (按需加载的大型工具库)
              if (id.includes('/@vueuse/')) {
                return 'vendor-vueuse';
              }
              // 其余 node_modules 适度收敛，避免碎片化
              return 'vendor-common';
            }
            // 业务代码由 Rollup 基于动态 import() 自动代码分割
          },
        },
      },
    },
    define: {
      // setting vue-i18-next
      // Suppress warning
      __INTLIFY_PROD_DEVTOOLS__: false,
      __APP_INFO__: JSON.stringify(__APP_INFO__),
    },
    css: {
      preprocessorOptions: {
        less: {
          modifyVars: generateModifyVars(),
          javascriptEnabled: true,
        },
      },
    },

    // The vite plugin used by the project. The quantity is large, so it is separately extracted and managed
    // visualizer 已由 configVisualizerConfig() 在 REPORT 模式下按需激活，不在此处常驻
    plugins: [...createVitePlugins(viteEnv, isBuild)],

    optimizeDeps: {
      esbuildOptions: {
        target: 'es2020',
      },
      include: [
        'vue',
        'vue-router',
        'pinia',
        'ant-design-vue',
        // 预构建 locale，避免 zh_CN.ts 动态 import 触发 504 Outdated Optimize Dep
        'ant-design-vue/es/locale/zh_CN',
        'ant-design-vue/es/locale/en_US',
        'ant-design-vue/es/locale/zh_TW',
      ],
    },
  };
};
