# Cursor + OpenSpec + Superpowers + Serena 推荐工作流

> **注意：** 职责划分的权威定义见 `.cursor/rules/toolchain-division.mdc`。
> 本文仅保留 Serena 工具速查表、故障排除和会话模板等补充参考内容。

> 适用仓库：`liu202505v2`（JNPF 低代码平台，.NET 6 + SqlSugar）  
> **职责划分（2026-05-22 定稿）**：见 [`.cursor/rules/toolchain-division.mdc`](../../.cursor/rules/toolchain-division.mdc) — **OpenSpec 仅知识库；Superpowers 管开发推进；Serena 管 C# 模块改动。**

## 四层分工

| 层 | 工具 | 职责 |
|----|------|------|
| 执行 | **Cursor** | Agent、编辑、终端、`dotnet build/run` |
| 知识库 | **OpenSpec** | `openspec/specs/` 能力规格；changes 草稿 → archive 归档（**不写代码、不跟日常 tasks**） |
| 方法 | **Superpowers** | 方案/施工包/实现/测试/上下文/推进清单（**开发主流程**） |
| 语义 | **Serena** | C# 符号查找、引用分析、安全重构（`modularity/`、`framework/`） |

---

## Serena 23 个工具说明与精简策略

> **重要**：OpenSpec 和 Superpowers **不是 MCP 工具**，而是 Skills/Commands（工作流指令）。它们与 Serena 不抢同一个工具槽位；真正重叠的是 **Serena ↔ Cursor 内置工具**，以及 **Serena memory/onboarding ↔ OpenSpec 探索流程**。

### 为什么是 23 个？

Serena 内置约 51 个工具；`--context ide` 已排除 5 个与 Cursor 重复的基础工具（`read_file`、`list_dir`、`find_file`、`create_text_file`、`execute_shell_command`）。加上 `--project` 单项目模式与默认 modes（`interactive` + `editing`），Cursor MCP 面板显示 **23 个**。

### 23 工具分类与冲突审核

| 类别 | 工具 | 数量 | 与 OpenSpec / Superpowers / Cursor 关系 | 建议 |
|------|------|------|----------------------------------------|------|
| **语义检索** | `find_symbol`, `find_referencing_symbols`, `find_declaration`, `find_implementations`, `get_symbols_overview`, `get_diagnostics_for_file` | 6 | **核心价值**，Cursor Grep/语义搜索无法替代 | ✅ 保留 |
| **语义编辑** | `replace_symbol_body`, `insert_after_symbol`, `insert_before_symbol`, `rename_symbol`, `safe_delete_symbol`, `replace_content` | 6 | 跨文件重构优于 StrReplace；`replace_content` 用于方法内小改 | ✅ 保留 |
| **Memory** | `write_memory`, `read_memory`, `list_memories`, `delete_memory`, `rename_memory`, `edit_memory` | 6 | 与 **OpenSpec `openspec/specs/`**、**CLAUDE.md**、episodic-memory 插件职责重叠 | ❌ 已禁用（`no-memories` mode） |
| **Onboarding** | `onboarding` | 1 | 与 **`/opsx:explore`**、**`/opsx:propose`**、Superpowers brainstorming 重叠 | ❌ 已禁用（`no-memories` mode） |
| **项目/元** | `activate_project`, `get_current_config`, `initial_instructions` | 3 | `--project` 已绑定工作区；config 仅调试；instructions 首次有用 | `activate_project` 自动禁用；`get_current_config` 已排除；`initial_instructions` 保留 |
| **文本搜索** | `search_for_pattern` | 1 | 与 **Cursor Grep** 重叠；非 C# 符号搜索应用 Grep | ❌ 已排除 |

### 精简后约 14 个工具（推荐配置）

已在 `.serena/project.yml` 启用：

```yaml
added_modes:
  - no-memories          # -7：全部 memory* + onboarding
excluded_tools:
  - get_current_config   # -1
  - search_for_pattern   # -1
```

**保留的 Serena 职责（仅此范围）**：

```
find_symbol / find_referencing_symbols / find_declaration / find_implementations
get_symbols_overview / get_diagnostics_for_file
replace_symbol_body / insert_*_symbol / rename_symbol / safe_delete_symbol / replace_content
initial_instructions
```

### 三层分工（避免 Agent 选错工具）

| 需求 | 用谁 | 不用谁 |
|------|------|--------|
| 能力规格、归档知识 | OpenSpec `openspec/specs/`、`/opsx:archive` | `/opsx:apply` 写代码 |
| 施工包、架构方案、推进清单 | Superpowers `writing-plans` / `executing-plans` | OpenSpec `tasks.md` 跟日常开发 |
| TDD、调试、验证、评审 | Superpowers skills | — |
| 读文件、Grep、Shell、dotnet | Cursor 内置 | Serena read_file 等（ide context 已排除） |
| 跨模块找类/方法/引用 | Serena `find_*` | Cursor Grep 扫全库 |
| 重命名/改方法体 | Serena `rename_symbol` 等 | 手改 + StrReplace |
| 非代码文件、配置、脚本 | Cursor Read/Grep/StrReplace | Serena 符号工具 |
| 项目知识沉淀（定稿） | `openspec/specs/`、`CLAUDE.md` | Serena `write_memory` |

---

## 前置检查（每次新会话可选）

1. Cursor Settings → MCP → **serena** 显示绿色（已连接）
2. 若 Serena 报错，确认：
   - `C:\Users\admin\.local\bin\serena.exe` 存在
   - 本项目已注册：`d:\JNPF-v52\backend\.serena\project.yml`
3. 首次跨模块改动前，对 Agent 说：
   > 调用 Serena 的 `initial_instructions`，然后在本项目做符号级分析。

---

## 标准流程（推荐顺序）

> **说明**：以下 Phase 1–4 为 **Superpowers 开发主流程**。OpenSpec 仅在阶段结束后将**稳定结论**归档至 `openspec/specs/`（可选）。

### Phase 0 — 探索（可选）

**场景**：问题还不清楚，需要先摸清代码结构。

```text
Superpowers: brainstorming + 代码阅读
（可选）Serena: find_symbol / find_referencing_symbols
```

**不要用** `/opsx:explore` 替代施工包编写或日常开发探索。

配合 Serena（Agent 自动或你明确要求）：
- `find_symbol` — 定位 `Startup`、Service 类
- `find_referencing_symbols` — 看谁引用了异常类型/接口
- `get_symbols_overview` — 快速看单文件结构

**Superpowers**：若已有报错栈，走 **systematic-debugging**（先复现、再假设、再验证）。

---

### Phase 1 — 方案与施工包（必做，非 trivial 变更）

```text
Superpowers: writing-plans
产出: docs/架构迭代/.../施工包.md
```

或自然语言：

```text
按架构铁律编写前端 F0–F4 施工包，待架构师审核
```

**产出**（在 `docs/架构迭代/` 或 `docs/architecture/`）：
- 迭代意见 / 方案设计
- 施工包（分阶段任务、验收标准、禁止项）
- 推进清单 LOG 条目

**Superpowers**：走 **brainstorming**，确认范围边界。

**OpenSpec（可选）**：施工包定稿且架构师批准后，将能力摘要写入 `openspec/specs/<capability>/spec.md`，或建 change 草稿备 archive。

---

### Phase 2 — 实现

```text
Superpowers: executing-plans（严格按施工包阶段）
```

**执行约定**：

1. 严格按**施工包**阶段（如 F0–F4），勾选推进清单与施工包验收项
2. **Serena 优先**（跨文件/跨模块时）：
   - 改 Service 前 → `find_referencing_symbols`
   - 改接口签名 → `rename_symbol`（勿手改字符串）
   - 大文件 → `get_symbols_overview` + `replace_symbol_body`
3. **Cursor 原生**（构建/脚本/配置）：
   ```bash
   dotnet build
   dotnet run --project application/JNPF.OA.API.Entry/JNPF.OA.API.Entry.csproj
   ```
4. **Superpowers TDD**：能写测试的先写；框架题至少保留「复现步骤 + 修复后验证命令」

---

### Phase 3 — 验证（声称完成前必做）

**Superpowers verification-before-completion**：

```bash
dotnet build -c Release
# 按变更类型选手动验证，例如：
dotnet run --project application/JNPF.OA.API.Entry/JNPF.OA.API.Entry.csproj
```

集成助手/订阅类问题：
- 启动 API.Entry → 切换功能演示子系统 → 改学生记录 → 确认事件执行

全部通过后再进入归档。

---

### Phase 4 — 知识归档（可选）

施工包阶段完成且验证通过后：

```text
（可选）/opsx:archive <capability-change>  → 合并入 openspec/specs/
推进清单: 追加 LOG + node scripts/sync-project-progress.js
```

**不要用** `/opsx:archive` 替代 Superpowers 验证或推进清单记录。

**Superpowers**：大改动可走 **requesting-code-review** / **code-reviewer** 子代理。

---

## 命令速查

| 阶段 | 命令/技能 | 何时用 |
|------|------|--------|
| 探索 | Superpowers `brainstorming` + Serena/Cursor 读码 | 需求/根因不清 |
| 方案 | Superpowers `writing-plans` | 施工包、架构方案 |
| 实现 | Superpowers `executing-plans` | 施工包已批准 |
| 验证 | Superpowers `verification-before-completion` | 声称完成前 |
| 知识归档 | `/opsx:archive`（可选） | 定稿能力写入 `specs/` |
| C# 重构 | Serena `find_*` / `rename_symbol` | 跨模块改动 |

**不用于开发推进**：`/opsx:apply`、`/opsx:explore`（日常）

---

## 按任务类型选工具

| 任务 | OpenSpec | Superpowers | Serena | 说明 |
|------|----------|-------------|--------|------|
| 修 PowerShell 编码脚本 | 否 | 是 | 否 | Cursor + Superpowers 足够 |
| OA Startup / DI 扫描 | 归档可选 | **是** | **是** | 跨程序集、Convention DI |
| 集成助手订阅事件 | 归档可选 | **是** | **是** | 跨 modularity + engine |
| 单文件 typo | 否 | 否 | 否 | 直接改 |
| 重命名公共 Service 方法 | 否 | 验证 | **必须** | `rename_symbol` |
| 施工包/架构内参 | 定稿后 spec | **writing-plans** | 否 | `docs/架构迭代/` |

---

## 本仓库 Serena 关键路径

```
framework/JNPF/                    # 核心框架
application/JNPF.API.Entry/        # 主 API 宿主
application/JNPF.OA.API.Entry/     # OA 宿主（兼职题 Q2）
modularity/                        # 业务模块（system, oauth, engine…）
openspec/                          # OpenSpec 变更与 specs
.serena/project.yml                # Serena C# 项目配置
```

**C# 语言服务器**：Serena 使用内置 LSP；首次符号操作可能较慢（需索引）。大仓库首次可运行：

```bash
serena project index d:\JNPF-v52\backend
```

（耗时较长，可选）

---

## 会话模板（复制即用）

```text
【目标】修复 OA.API.Entry 启动异常

1. Superpowers brainstorming + Serena find_symbol(Startup)
2. Superpowers writing-plans → docs/架构迭代/.../施工包.md
3. 架构师审核施工包
4. Superpowers executing-plans 按施工包实施
5. dotnet build && 启动 OA 验证（verification-before-completion）
6. 推进清单 LOG + （可选）openspec archive 入 specs/
```

---

## 故障排除

| 现象 | 处理 |
|------|------|
| Serena MCP 红叉 | 重启 Cursor；检查 `mcp.json` 中 `serena.exe` 路径 |
| `serena` 命令找不到 | 终端执行 `uv tool update-shell` 或把 `C:\Users\admin\.local\bin` 加入 PATH |
| 符号找不到 | 确认 `--project d:\JNPF-v52\backend`；必要时 `serena project index` |
| OpenSpec 命令无效 | 重载窗口；确认 `.cursor/commands/opsx-*.md` 存在 |
