# ADR-001：驳回本地 ChromaDB 向量 RAG，改用原生工具链知识库

| 字段 | 内容 |
|------|------|
| 状态 | 已接受 |
| 日期 | 2026-05-31 |
| 决策者 | 架构师 |

## 背景

`docs/架构迭代/1、系统架构设计说明/9、私属向量数据库.md` 提议用 bge-small-zh-v1.5 + ChromaDB + MCP 做代码语义检索。
经审查：与 Cursor SemanticSearch/Grep/Serena 重复；模型不适配 C# 代码；无可靠增量维护；检索结果截断 800 字符易误导 AI。

## 决策

1. **不实施** ChromaDB / Ollama / `.claude/vector_store` / 向量 MCP。
2. **采用** episodic-memory + OpenSpec specs + ADR + `.cursor/rules` + 新鲜度脚本。
3. 代码定位继续使用：Grep、SemanticSearch、Serena MCP（`find_symbol` 等）。

## 后果

### 正面

- 零 Python ML 运维；双环境配置不冲突；知识可人工审阅。

### 负面

- 模块 spec 与 ADR 仍需人工编写业务语义（无法从 git log 全自动生成）。

## 相关文件

- `docs/架构迭代/1、系统架构设计说明/10、向量数据库修订施工包.md`
- `.cursor/rules/knowledge-base.mdc`
