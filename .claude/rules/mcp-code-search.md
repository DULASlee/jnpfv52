# MCP Code Search — 强制规则（宪法级）

> 背景：2026-07-10 复核需求分析子链 26/27/28 时确认 Serena MCP 已可用。逐文件 Grep/Read 遍历对大型 C# 代码库效率极低且容易漏引用——必须用 MCP 语义级搜索。
>
> 触发条件：**任何代码搜索/符号查找/引用追踪任务。** 这不是建议，是铁律。

---

## Iron Rule: 代码搜索 MUST 用 MCP，禁止逐文件遍历

| 搜索目标 | 工具 | 何时用 |
|---------|------|--------|
| **C# 符号**（类/方法/接口/属性/字段） | **Serena `find_symbol`** | 已知符号名，要定义/签名/body |
| **C# 符号结构概览** | **Serena `get_symbols_overview`** | 拿到一个文件，先看它有什么 |
| **C# 引用追踪** | **Serena `find_referencing_symbols`** | "谁调用了 X" / "X 是否被注入" / "X 是不是孤儿" |
| **C# 接口实现** | **Serena `find_implementations`** | "这个接口有哪些实现类" |
| **架构/领域知识** | **Knowledge Graph `search_nodes` / `read_graph`** | 子链使命、控制流、设计哲学、历史决策 |
| 文本内容兜底 | Grep / `git grep` | **仅当 Serena/KG 不适用时**（如查字符串字面量、配置值、日志文案） |

**禁止：** 在能使用 Serena 的情况下用 Grep 搜 C# 符号名再逐文件 Read 验证引用——这是反模式，慢且漏。

---

## Serena 调用速查（精确到参数）

### 1. 看一个文件有什么 → `get_symbols_overview`
```
relative_path: "backend/modularity/inteAssistant/JNPF.InteAssistant/LlmGatewayService.cs"
depth: 1   // 1=展开到方法/字段级；0=只看顶层类
```
返回：类下的所有 Method/Field/Property + 精确 body_location（起止行号）。

### 2. 查符号定义/签名 → `find_symbol`
```
name_path_pattern: "RequirementAnalysisOrchestrator/RunRoundAsync"
relative_path: "backend/.../RequirementAnalysisOrchestrator.cs"  // 可选，限定范围
include_body: false   // true 才返回源码（慎用，占 token）
depth: 0              // 1=连子方法一起返回
substring_matching: false  // true 用于模糊（如 "Async" 找所有异步方法）
```
返回 `[]` = 符号不存在。**这比 Grep "方法名" 然后人肉判断准得多。**

### 3. 追引用（判断孤儿/接线） → `find_referencing_symbols`
```
name_path: "JNPF.InteAssistant.Gates/IConsistencyChecker"
relative_path: "backend/.../Gates/ConsistencyChecker.cs"
```
**判读规则：**
- 返回只有"自身 implements 那一行" = **零接线（孤儿组件）** — 见 Task 28 四组件复核
- 返回多个文件 = 有消费方
- 返回 `{}` = 全仓库零引用（连声明都没有，可能是接口未定义）

### 4. 查接口实现 → `find_implementations`
```
name_path: "ILlmCircuitBreaker"
relative_path: "backend/.../Llm/LlmCircuitBreaker.cs"
```

### 5. 符号级修改（替代手 Edit） → Serena 的 replace 系列
- `replace_symbol_body` — 替换整个方法体
- `insert_before_symbol` / `insert_after_symbol` — 按符号定位插入
- 比手 Edit 按文本匹配更可靠（不怕重复字符串）

---

## Knowledge Graph 调用速查

| 操作 | 工具 | 用途 |
|------|------|------|
| 查某主题的已知知识 | `search_nodes({query})` | 输入关键词，返回相关实体 |
| 看全部已沉淀知识 | `read_graph()` | 全量实体+关系（适合开局摸底） |
| 沉淀新知识 | `create_entities` / `add_observations` | 完成一个子链/重大决策后 |
| 连接实体 | `create_relations` | "A 依赖 B" / "A 属于 B" |

**KG 持久化路径：** `D:\JNPF-v52\.ai-memory\knowledge-graph.json`（gitignored）

---

## 典型场景示例

### 场景 A："X 组件是否被接线？"
1. `find_symbol(X)` → 确认存在 + 拿到文件路径
2. `find_referencing_symbols(X)` → 看返回引用数
3. 只有自身声明行 → 孤儿；多处引用 → 已接线

### 场景 B："改这个方法会影响谁？"
1. `find_symbol(方法名, include_body:false)` → 确认签名
2. `find_referencing_symbols(方法名)` → 列出所有调用方
3. 逐个评估影响

### 场景 C："这个子链的设计意图是什么？"
1. `search_nodes({query: "子链名"})` → 读 observations
2. 不要靠读代码反推设计意图——KG 里的 observations 是之前明确记录的

---

## 配置位置

MCP 服务器配置在 `.zcode/config.json`：
- **serena**: `stdio` 启动，`--context claude-code --project D:\JNPF-v52`，timeout 180s
- **knowledge-graph**: `@modelcontextprotocol/server-memory`，持久化到 `.ai-memory/knowledge-graph.json`

**冷启动注意：** ZCode 新会话首次调用 MCP 可能有几秒延迟（进程拉起）。若某次调用超时，重试一次即可——通常是冷启动，不是配置错。

**已验证可用（2026-07-10）：** Serena 的 `get_symbols_overview` / `find_symbol` / `find_referencing_symbols` 全部返回精确结果（行号级）；Knowledge Graph 的 `read_graph` 返回 8 个实体完整。
