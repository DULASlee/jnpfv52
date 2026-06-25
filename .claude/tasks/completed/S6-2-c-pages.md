---
title: Sprint 6-2 C档5个剩余页面
priority: critical
assignee: claude
created: 2026-06-17
sprint: 6
task_id: S6-2
estimated_hours: 2
---

## 任务描述

创建 5 个 C 档前端页面，补齐前端覆盖到 100%。

## 页面清单

| 序号 | 页面 | 路由 | 权限 |
|---|---|---|---|
| C-1 | 子智能体管理 | /studio/agent/sub-agents | platform_admin |
| C-2 | Skills 管理 | /studio/agent/skills | platform_admin |
| C-3 | MCP 配置 | /studio/agent/mcp | platform_admin |
| C-4 | 行业知识设置 | /studio/tenant/industry-knowledge | tenant_admin |
| C-5 | 业务术语表 | /studio/tenant/glossary | tenant_admin |

## 验收标准

- [ ] 5 个页面文件创建完成
- [ ] 路由注册完成
- [ ] 编译 0 errors
- [ ] 浏览器打开每个页面正常渲染

## 相关文件

- jnpf-web-vue3/src/views/studio/agent/
- jnpf-web-vue3/src/views/studio/tenant/
- jnpf-web-vue3/src/router/routes/index.ts
