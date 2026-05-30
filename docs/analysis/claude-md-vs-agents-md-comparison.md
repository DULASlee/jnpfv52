# CLAUDE.md vs AGENTS.md 对比分析报告

**分析日期：** 2026-05-30
**分析人：** 工程师
**目的：** 为架构师提供决策依据，确定两个文件的职责划分和去重方案

---

## 1. 文件基本信息

| 文件 | 目标运行时 | 当前行数 | 最后更新 |
|------|-----------|---------|---------|
| CLAUDE.md | `claude.ai/code` | 157 行 | 2026-05-30 |
| AGENTS.md | `Codex.ai/code` | 117 行 | 2026-05-30 |

---

## 2. 逐章节对比

### 2.1 完全相同或高度相似（>80%重叠）

| 章节 | CLAUDE.md | AGENTS.md | 重叠度 | 差异说明 |
|------|-----------|-----------|--------|---------|
| **Database** | ✅ 第95-100行 | ✅ 第92-96行 | **95%** | 仅表名示例略有不同 |
| **Conventions** | ✅ 第102-108行 | ✅ 第98-103行 | **90%** | 链接相同，措辞略异 |
| **Architecture Documentation** | ✅ 第110-112行 | ✅ 第105-107行 | **85%** | CLAUDE.md 引用更详细 |
| **Code Analysis** | ✅ 第81-93行 | ✅ 第86-89行 | **80%** | CLAUDE.md 多了 StyleCop 预留声明 |

### 2.2 语义相同但措辞不同（60-80%重叠）

| 章节 | CLAUDE.md | AGENTS.md | 重叠度 | 差异说明 |
|------|-----------|-----------|--------|---------|
| **Workspace** | ✅ 第6-8行 | ✅ 第7-16行 | **70%** | AGENTS.md 有 ASCII 树形图 |
| **Build & Run** | ✅ 第10-22行 | ✅ 第18-40行 | **75%** | AGENTS.md 更详细（含 Docker、Release） |
| **Architecture** | ✅ 第24-69行 | ✅ 第42-75行 | **65%** | CLAUDE.md 有模块层级总览 |
| **Key Patterns** | ✅ 第71-79行 | ✅ 第76-84行 | **70%** | CLAUDE.md 更详细（含路径） |
| **Agent Toolchain** | ✅ 第114-134行 | ✅ 第109-116行 | **70%** | CLAUDE.md 有职责矩阵 |

### 2.3 独占内容

| 文件 | 独占内容 | 行号 | 说明 |
|------|---------|------|------|
| **CLAUDE.md** | Runtime 声明 | 1-4 | `claude.ai/code` 运行时标识 |
| **CLAUDE.md** | OA 模块注意声明 | 33 | 未启用模块说明 |
| **CLAUDE.md** | 项目模块层级总览 | 37-61 | 层级说明 + 当前启用模块表 |
| **CLAUDE.md** | StyleCop 预留声明 | 85-93 | 代码风格预留说明 |
| **CLAUDE.md** | 工具链职责矩阵 | 118-123 | 可执行编码/运维/架构决策 |
| **CLAUDE.md** | 明确约束 | 125-132 | /opsx:apply 禁止编码 |
| **CLAUDE.md** | 文档索引 | 136-156 | 版本化文档索引 |
| **AGENTS.md** | ASCII 工作区树形图 | 7-16 | 可视化目录结构 |
| **AGENTS.md** | Docker 构建命令 | 27 | `docker build` 命令 |
| **AGENTS.md** | Release 构建命令 | 24 | `dotnet build -c Release` |

---

## 3. 职责判定

| 维度 | CLAUDE.md | AGENTS.md |
|------|-----------|-----------|
| **目标运行时** | `claude.ai/code` | `Codex.ai/code` |
| **主要用途** | Claude Code CLI/Web 的项目上下文 | GitHub Copilot Agents 的项目上下文 |
| **内容定位** | 完整项目上下文 + 架构约束 + 工具链规范 | 精简项目上下文 + 快速上手 |
| **维护频率** | 高（随架构演进更新） | 低（仅同步核心变更） |

---

## 4. 判定结论

### 情况判定：**B — 两者有重叠但各有独占内容**

- 约 **70% 内容重叠**（Database、Conventions、Architecture Documentation 等）
- 约 **20% 语义相同但措辞不同**（Workspace、Build & Run、Architecture 等）
- 约 **10% 独占内容**（CLAUDE.md 的模块层级、工具链矩阵；AGENTS.md 的 ASCII 树、Docker 命令）

### 建议方案：**股权式治理（引用式共享）**

```markdown
## 建议文件结构

CLAUDE.md          ← 主文件，包含完整的项目上下文（当前改进版）
AGENTS.md          ← 精简版，仅包含：
                      1. 运行时标识声明（区别于 claude.ai/code）
                      2. 指向 CLAUDE.md 的引用："完整项目上下文参见 CLAUDE.md"
                      3. AGENTS.md 独有的差异化指令（ASCII 树、Docker 命令）
```

---

## 5. 执行建议

### Phase 1：立即执行

1. **AGENTS.md 精简**：移除与 CLAUDE.md 重复的章节，改为引用
2. **保留独占内容**：ASCII 树形图、Docker 命令、Release 构建命令

### Phase 2：架构师审核后执行

1. **确定 AGENTS.md 是否需要保留**：如果 Codex 运行时不再使用，可考虑归档
2. **建立同步机制**：如果保留，需明确哪些变更需要同步到 AGENTS.md

---

## 6. 风险提示

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 两文件维护不同步 | AI 引用过时信息 | 建立主从关系，CLAUDE.md 为主 |
| AGENTS.md 内容丢失 | Codex 运行时缺少上下文 | 精简前备份，保留独占内容 |
| 运行时混淆 | AI 误用配置 | 明确运行时标识声明 |

---

**结论：** 建议执行**情况 B** 方案，将 AGENTS.md 精简为 CLAUDE.md 的引用式副本，保留独占内容。具体执行需架构师确认 AGENTS.md 的使用场景后决定。
