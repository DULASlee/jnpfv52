import { defineConfig } from 'vite';
import uni from '@dcloudio/vite-plugin-uni';

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
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        ws: true,
      },
      '/websocket': {
        target: 'ws://localhost:5000',
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
