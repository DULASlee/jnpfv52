---
title: Sprint 7 更新 API 格式支持
priority: critical
assignee: claude
created: 2026-06-17
sprint: 7
task_id: S7-update
estimated_hours: 1
---

## 任务描述

更新模型供应商配置系统，支持 Anthropic 和 OpenAI 两种 API 格式。

## 关键变更

1. 新增 F_ApiFormat 字段（openai/anthropic/ollama）
2. MiMo 模型名：mimo-v2.5-pro
3. DeepSeek/MiMo 使用 Anthropic 格式
4. 更新种子数据（真实 API Key）
5. 更新测试连接方法
6. 更新 LlmGatewayService

## 验收标准

- [ ] 数据库表结构更新
- [ ] 种子数据更新
- [ ] 后端编译 0 errors
- [ ] 测试连接功能正常
- [ ] 前端页面更新
