import { defineConfig } from 'vite';
import { createRequire } from 'module';

// @dcloudio/vite-plugin-uni 是 CJS 包（exports.default = uniPlugin）。
// Vite 打包 vite.config.js 时会 externalize node_modules 依赖，
// 导致运行时用 Node 原生 ESM→CJS interop 解析 `import uni from`，
// 此时 default 拿到的是整个 module.exports 对象而非函数（uni is not a function）。
// 改用 createRequire 取 .default，绕开 interop。
const require = createRequire(import.meta.url);
const uni = require('@dcloudio/vite-plugin-uni').default;

// 消除 vue-i18n esm-bundler 的 feature flag 警告，并启用正确的 tree-shaking
// 见 https://vue-i18n.intlify.dev/guide/advanced/optimization.html
export default defineConfig({
  plugins: [uni()],
  define: {
    __VUE_I18N_FULL_INSTALL__: true,
    __VUE_I18N_LEGACY_API__: false,
    __INTLIFY_PROD_DEVTOOLS__: false,
  },
  server: {
    port: 3800,
    strictPort: true,
    proxy: {
      // 注意：项目根目录有本地 api/ 源码目录（App.vue 等会 import '@/api/common.js'），
      // vite 会把这些本地模块以 /api/xxx.js 的 URL 提供。若 /api 代理无差别转发，
      // 这些本地模块请求会被打到后端并返回 403，导致白屏。
      // 因此用 filter 跳过源码类扩展名，让 vite 本地提供 api/*.js，
      // 仅转发真正的后端接口调用（/api/oauth/Login 等无扩展名）。
      '/api': {
        target: 'http://localhost:5002',
        changeOrigin: true,
        secure: false,
        // bypass：返回字符串则由 vite 本地提供该路径（不代理）。
        // 本地 api/ 源码模块（App.vue 等会 import '@/api/common.js'）会被 vite
        // 以 /api/xxx.js 提供，这里对源码类扩展名返回 req.url，让 vite 本地提供，
        // 避免被打到后端返回 403 导致白屏；真正的接口调用（无扩展名）继续代理。
        bypass(req) {
          const u = req.url || '';
          if (/\.(js|ts|jsx|tsx|mjs|cjs|vue|css|scss|json)(\?|$)/i.test(u)) {
            return u;
          }
        },
      },
      '/websocket': {
        target: 'ws://localhost:5002',
        ws: true,
      },
    },
    hmr: {
      host: 'localhost',
      port: 3800,
      protocol: 'ws',
    },
  },
});
