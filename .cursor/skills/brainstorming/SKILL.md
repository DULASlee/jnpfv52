---
name: brainstorming
description: Explore codebase to understand problems, requirements, or root causes before coding. Use when the problem is ambiguous, the scope is unclear, or you need to trace code paths before writing a plan.
scope: JNPF-v52
---

# Brainstorming — 探索与根因分析

在开始编码或写方案之前，先深入理解问题和代码。

## 适用场景

- 需求描述模糊，需要先搞清楚现状
- 报错/问题不清楚根因
- 需要评估改动范围和影响面
- 写施工包前的预研

## 工作流

### 1. 明确问题边界

用一句话描述要解决的问题，列出已知条件和未知条件。

### 2. 代码探查

按优先级使用工具：

| 需求 | 工具 |
|------|------|
| 理解功能怎么实现的 | `search_codebase`（语义搜索） |
| 找类/方法定义 | `lsp` → `goToDefinition` / `findReferences` |
| 精确字符串匹配 | `grep_code` |
| 读关键文件 | `read_file` |

### 3. 追踪数据流

对于登录/加密/配置类问题，必须追踪完整链路：
- 前端 → API → Service → 数据库
- 每层记录：文件名、行号、关键逻辑

### 4. 输出

输出格式：

```
## 问题：[一句话]

### 根因
[从源码追踪得到的结论，附文件路径和行号]

### 影响范围
- 涉及文件：...
- 涉及模块：...

### 下一步建议
- 方案选项 A：...
- 方案选项 B：...
```

## 铁律

- ❌ 不要猜测，必须读源码
- ❌ 不要跳过中间层（前端→后端→DB 每层都看）
- ✅ 每个结论必须附源码路径+行号
