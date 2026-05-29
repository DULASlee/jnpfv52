# jnpf-v52-workspace

> **状态**：active（2026-05-28）  
> **工作区**：`d:\JNPF-v52`  
> **episodic project**：`D--JNPF-v52`

## 范围

| 纳入 | 排除 |
|------|------|
| v5.2 干净全栈：backend + jnpf-web-vue3 + jnpf-web-datascreen + jnpf-app-vue3 | `d:\liu202505v2` 内 3.6 残留 |
| 演示手册 `docs/v52-demo-manual.md` | 未归档的临时脚本 |

## 运行锚点

| 服务 | 地址 |
|------|------|
| API | `http://localhost:5000` |
| PC | `http://localhost:3100` |
| 大屏 | `http://localhost:8100/DataV/`（须 PC 登录后从「在线开发→大屏设计」进入） |
| 移动 H5 | `http://localhost:3800` |

## 工具链

- Superpowers：`.cursor/skills/`
- OpenSpec：本目录及 `openspec/changes/`
- episodic：`D--JNPF-v52`，见 `docs/toolchain/SETUP.md`

## 本节关键代码路径索引

| 路径 | 说明 |
|------|------|
| `backend/application/JNPF.API.Entry/` | API 宿主 |
| `jnpf-web-vue3/` | PC 前端 |
| `jnpf-web-datascreen/` | 大屏前端 |
| `docs/v52-demo-manual.md` | 客户演示脚本 |
