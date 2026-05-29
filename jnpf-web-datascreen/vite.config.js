import { defineConfig, loadEnv } from 'vite'
import { resolve } from 'path'
import libConfig from './lib.config';
import createVitePlugins from './vite/plugins'
// https://vitejs.dev/config/
export default ({ mode, command }) => {
  const env = loadEnv(mode, process.cwd())
  const { VITE_APP_BASE, VITE_APP_ENV, VITE_PROXY } = env
  return defineConfig({
    ...(() => {
      if (VITE_APP_ENV == 'lib') {
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
    plugins: createVitePlugins(env, command === 'build'),
    define: {
      'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV),
    },
    server: {
      https: false,
      host: true,
      port: 8100,
      proxy: {
        "/dev": {
          target: VITE_PROXY,//代理接口
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/dev/, ""),
        },
      },
      open: true, //vite项目启动时自动打开浏览器
    },
  })
}
