# 端口分配规范

> 适用版本：>= V5.2 | 最后更新：2026-06-02

## 端口分配表

| 项目 | 端口 | 备注 |
|------|------|------|
| 后端 API | **5000** | 所有接口统一入口 |
| PC 前端（jnpf-web-vue3） | **3100** | strictPort=true |
| 数字大屏（jnpf-web-datascreen） | **3102** | strictPort=true |
| UniApp H5（jnpf-app-vue3） | **3800** | strictPort=true |

> 3101 不分配给任何项目，避免与 PC 前端可能的端口漂移冲突。

## 规则

1. **所有前端项目必须配置 `strictPort: true`** — 端口被占用时报错而非自动切换到相邻端口
2. **所有前端项目的 `/api` 请求通过 Vite proxy 转发到 `localhost:5000`**
3. **禁止在前端代码中硬编码后端地址**（H5 模式下 `baseURL` 留空，依赖 proxy）
4. **真机/App 环境的后端地址在 `define.js` 中单独配置**

## 配置位置速查

| 项目 | 配置文件 | 关键字段 |
|------|---------|---------|
| 后端 API | `backend/application/JNPF.API.Entry/Properties/launchSettings.json` | `applicationUrl` |
| PC 前端 | `jnpf-web-vue3/.env` | `VITE_PORT = 3100` |
| PC 前端 | `jnpf-web-vue3/vite.config.ts` | `server.strictPort` |
| PC 前端 | `jnpf-web-vue3/.env.development` | `VITE_PROXY` |
| 数字大屏 | `jnpf-web-datascreen/vite.config.js` | `server.port` / `server.strictPort` |
| 数字大屏 | `jnpf-web-datascreen/.env.development` | `VITE_PROXY` |
| UniApp H5 | `jnpf-app-vue3/vite.config.js` | `server.port` / `server.strictPort` / `server.proxy` |
| UniApp H5 | `jnpf-app-vue3/utils/define.js` | H5 `baseURL = ''`（走 proxy） |

## 验证清单

改完端口配置后，按以下顺序验证：

```
1. 启动后端 → http://localhost:5000/api/oauth/getLoginConfig 返回 200
2. 启动 PC 前端 → http://localhost:3100，登录正常
3. 启动数字大屏 → http://localhost:3102/DataV/，大屏正常
4. 启动 UniApp H5 → http://localhost:3800，登录正常
5. 四个项目同时运行，互不冲突
6. 浏览器 Network 面板确认所有 /api 请求都打到 5000
```

## 清理残留进程

端口被占用时：

```bash
# Windows
netstat -ano | findstr :3100
taskkill /PID <进程ID> /F

# 或杀掉所有 node 进程
taskkill /IM node.exe /F
```
