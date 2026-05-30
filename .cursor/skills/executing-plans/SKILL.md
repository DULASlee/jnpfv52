---
name: executing-plans
description: Execute a construction package phase by phase, tracking progress with todo_write and verifying each step. Use when a construction package is approved and ready for implementation.
scope: JNPF-v52
tech-stack: [dotnet, pnpm]
---

# Executing Plans — 按施工包执行

严格按已批准的施工包分阶段实施，用 todo_write 追踪进度，每步验证。

## 前置条件

- 施工包已编写（`writing-plans`）
- 施工包已通过审核
- 明确当前要执行的阶段

## 工作流

### 1. 加载施工包

读取施工包文件，确认当前阶段和任务清单。

### 2. 用 todo_write 创建任务列表

将施工包当前阶段的任务转换为 todo 列表。任务 ID 使用施工包中的任务编号。

### 3. 逐任务执行

对每个任务：

1. **标记为 IN_PROGRESS**
2. **实施**：修改代码，遵循项目编码规范
3. **验证**：
   - 构建：`dotnet build`（C# 项目）
   - 功能：手动验证或运行测试
4. **标记为 COMPLETE**

### 4. 阶段验收

一个阶段全部完成后：

- 对照施工包验收标准逐项检查
- 记录验收结果
- 推进清单追加 LOG 条目

### 5. 异常处理

- 遇到施工包未预见的情况 → 暂停，回到 `brainstorming`
- 验证不通过 → 修复，重新验证，最多重试 3 次
- 3 次仍未通过 → 报告架构师，更新施工包

## 与 Serena 配合

跨模块 C# 改动时：
- 改 Service 前 → `find_referencing_symbols`
- 改接口签名 → `rename_symbol`（勿手改字符串）
- 大文件改方法体 → `replace_symbol_body`

## 铁律

- ❌ 禁止跳过施工包直接改代码
- ❌ 禁止同时执行多个阶段
- ✅ 每个任务完成后立即验证
- ✅ 遇到问题先查源码，不要猜测
