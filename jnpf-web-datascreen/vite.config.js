import { defineConfig, loadEnv } from 'vite'
import { resolve } from 'path'
import libConfig from './lib.config';
import createVitePlugins from './vite/plugins'
// https://vitejs.dev/config/
export default ({ mode, command }) => {
  const env = loadEnv(mode, process.cwd())
  const { VITE_APP_BASE, VITE_APP_ENV, VITE_PROXY } = env
  const isBuild = command === 'build'
  const isLib = VITE_APP_ENV === 'lib'
  return defineConfig({
    ...(() => {
      if (isLib) {
        return libConfig
      }
      return {}
    })(),
    base: VITE_APP_BASE,
    resolve: {
      alias: {
        'vue': 'vue/dist/vue.esm-bundler.js',
        '~': resolve(__dirname, './'),
        "@": resolve(__dirname, "./src"),
        "components": resolve(__dirname, "./src/components"),
        "styles": resolve(__dirname, "./src/styles"),
        "utils": resolve(__dirname, "./src/utils"),
      }
    },
    ...((isBuild && !isLib) ? {
      build: {
        target: 'es2015',
        // esbuild 压缩 — Vite 4 默认 terser，对大屏项目内存友好
        minify: 'esbuild',
        sourcemap: false,
        chunkSizeWarningLimit: 500,
        rollupOptions: {
          output: {
            chunkFileNames: 'static/js/[name]-[hash].js',
            entryFileNames: 'static/js/[name]-[hash].js',
            assetFileNames: 'static/[ext]/[name]-[hash].[ext]',
            manualChunks: (id) => {
              if (id.includes('node_modules')) {
                // element-plus (重型 UI 库 ~1MB)
                if (id.includes('/element-plus/') || id.includes('/@element-plus/')) {
                  return 'vendor-element';
                }
                // monaco-editor (代码编辑器 ~5MB — 最大的单个依赖)
                if (id.includes('/monaco-editor/')) {
                  return 'vendor-monaco';
                }
                // DataV 可视化组件
                if (id.includes('/@kjgl77/datav-vue3/') || id.includes('/echarts/') || id.includes('/zrender/')) {
                  return 'vendor-datav';
                }
                // AVUE 低代码框架
                if (id.includes('/@smallwei/avue/')) {
                  return 'vendor-avue';
                }
                // Vue 生态
                if (id.includes('/vue/') || id.includes('/vue-router/') || id.includes('/vue-i18n/') || id.includes('/pinia/')) {
                  return 'vendor-vue';
                }
                return 'vendor-common';
              }
            },
          },
        },
      },
    } : {}),
    plugins: createVitePlugins(env, isBuild),
    define: {
      'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV),
    },
    server: {
      https: false,
      host: true,
      port: 3102,
      strictPort: true,
      proxy: {
        "/dev": {
          target: VITE_PROXY,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/dev/, ""),
        },
      },
      open: true,
      // 演示/日常：预构建重型依赖，避免首次打开大屏白屏数分钟
      warmup: {
        clientFiles: ['./index.html', './src/main.js', './src/App.vue'],
      },
    },
    optimizeDeps: {
      include: [
        'vue',
        'vue-router',
        'vue-i18n',
        'element-plus',
        'element-plus/dist/locale/zh-cn.mjs',
        '@element-plus/icons-vue',
        '@smallwei/avue',
        '@kjgl77/datav-vue3',
        'axios',
        'dayjs',
        'mqtt',
        'highlight.js',
        'vue-json-viewer',
        'vuedraggable',
      ],
      // monaco 按需进编辑页再加载，不挡大屏首页
      exclude: ['monaco-editor'],
    },
  })
}
