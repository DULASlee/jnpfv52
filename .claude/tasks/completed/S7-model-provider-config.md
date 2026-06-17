---
title: Sprint 7 模型供应商配置系统
priority: critical
assignee: claude
created: 2026-06-17
sprint: 7
task_id: S7
estimated_hours: 2
---

## 任务描述

实现模型供应商配置系统，使 LLM 大模型可配置，不改源代码即可切换/新增供应商。

## 交付物

1. 数据库：V5.2_006_model_provider.sql
2. 后端：ModelProviderEntity.cs + ModelProviderService.cs
3. 后端修改：LlmGatewayService.cs（从数据库读取配置）
4. 前端：providers.vue
5. 路由注册 + 菜单注册

## 验收标准

- [ ] 数据库表创建成功
- [ ] 后端编译 0 errors
- [ ] API 端点全部 200
- [ ] 前端页面正常渲染
- [ ] 测试连接功能正常

## 执行步骤

1. 执行 V5.2_006_model_provider.sql
2. 创建 ModelProviderEntity.cs
3. 创建 ModelProviderService.cs
4. 修改 LlmGatewayService.cs
5. 创建 providers.vue
6. 注册路由
7. 编译验证
8. 启动验证
