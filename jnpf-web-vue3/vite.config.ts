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
import { visualizer } from 'rollup-plugin-visualizer';

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
      minify: 'terser',
      terserOptions: {
        compress: {
          keep_infinity: true,
          // Used to delete console in production environment
          drop_console: VITE_DROP_CONSOLE,
          drop_debugger: true,
        },
      },
      // Turning off reportCompressedSize display can slightly reduce packaging time
      reportCompressedSize: false,
      chunkSizeWarningLimit: 2000,
      rollupOptions: {
        input: {
          index: pathResolve('index.html'),
        },
        // 静态资源分类打包
        output: {
          chunkFileNames: 'static/js/[name]-[hash].js',
          entryFileNames: 'static/js/[name]-[hash].js',
          assetFileNames: 'static/[ext]/[name]-[hash].[ext]',
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
    plugins: [
      ...createVitePlugins(viteEnv, isBuild),
      // P0-3: 分析 entry chunk 组成
      visualizer({
        open: false,
        filename: 'stats.json',
        gzipSize: true,
        json: true,
      }),
    ],

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
