# `.cursor/rules/` 导读

> **唯一 alwaysApply：** 根目录 [`00-constitution.mdc`](./00-constitution.mdc)  
> Cursor **支持子目录**组织规则（官方文档示例：`frontend/components.mdc`）。  
> **禁止**再把长文改回 alwaysApply。

```
.cursor/rules/
├── 00-constitution.mdc     ← 唯一常驻
├── README.md               ← 本导读（不注入 Agent）
├── iron-laws/              ← 宪法详规（按需）
├── domain/                 ← Studio / IoT 等专题（globs）
├── frontend/               ← 前端约定（globs）
├── toolchain/              ← 测试/搜索/工具短指针
└── docs/                   ← 写文档时
```

---

## 第 0 层 — 入口

| 文件 | 作用 |
|---|---|
| `00-constitution.mdc` | 指挥原则（机器验收/老板模式/底仓不废）+ Q1–Q3、ADF、四支柱、红线 |
| `../CURRENT-FOCUS.md` | **当前 SG/本 Chat 只做/老板模式**（开 Chat 优先 @；非 mdc） |
| `../templates/how-to-brief-agents.md` | 给人用：口令、重开、防假绿（不注入 Agent） |
| `README.md` | 本导读（给人看，不注入 Agent） |

## 第 1 层 — `iron-laws/`

| 文件 | 何时 |
|---|---|
| `business-first-iron-law.mdc` | 业务锚定 |
| `implementation-integrity-iron-law.mdc` | 禁改测试凑过 |
| `fullchain-sprint-iron-law.mdc` | SG / 四支柱 |
| `req-analysis-iron-law.mdc` | 需求分析 / CR |
| `architecture-design-interface-first.mdc` | ADF 全文 |
| `triple-key-iron-law.mdc` | 三元组 |

## 第 2 层 — `domain/` · `frontend/` · `docs/`

按改动路径 globs 自动挂；也可 `@domain/studio-s2-compile`。

## 第 3 层 — `toolchain/`

短指针为主：`testing-toolchain` → openspec；`needle-search`、`git-workflow` 等。

## 镜像关系

| 位置 | 角色 |
|---|---|
| 本目录 | Cursor Agent 规则 |
| `CLAUDE.md` / `AGENTS.md` | 总览 |
| `.cursorrules` | 短缓存（身份+端口） |
| `.claude/rules/` | Claude 镜像 |
