# MCP Code Search — 强制规则（宪法级）

> 背景：2026-07-17 复审确认 **三大 MCP 工具链已就绪**（Serena + Codebase-Memory + Knowledge-Graph）。逐文件 Grep/Read 遍历对大型 C# 代码库效率极低且容易漏引用——必须用 MCP 语义级搜索。
>
> 触发条件：**任何代码搜索/符号查找/引用追踪/架构分析任务。** 这不是建议，是铁律。

---

## Iron Rule: 代码搜索 MUST 用 MCP，禁止逐文件遍历

| 搜索目标 | 工具 | 何时用 |
|---------|------|--------|
| **C# 单符号**（类/方法/接口/属性/字段） | **Serena `find_symbol`** | 已知符号名，要定义/签名/body |
| **C# 文件结构概览** | **Serena `get_symbols_overview`** | 拿到一个文件，先看它有什么 |
| **C# 引用追踪**（精确到调用点） | **Serena `find_referencing_symbols`** | "谁调用了 X" / "X 是否被注入" / "X 是不是孤儿" |
| **C# 接口实现** | **Serena `find_implementations`** | "这个接口有哪些实现类" |
| **跨文件调用链**（callers/callees 多跳） | **Codebase-Memory `trace_path`** | "这条链路完整经过哪些函数" / 影响分析 |
| **项目架构/社区聚类** | **Codebase-Memory `get_architecture`** | "这个项目的 de-facto 模块边界在哪" |
| **复杂度热点** | **Codebase-Memory `query_graph`**（Cypher） | "哪些方法圈复杂度最高 / 有 loop nested-loop" |
| **领域知识/设计意图/历史决策** | **Knowledge Graph `search_nodes` / `read_graph`** | 子链使命、控制流、架构哲学 |
| 文本内容兜底 | Grep / `git grep` | **仅当三大 MCP 不适用时**（字符串字面量、配置值、日志文案） |

**禁止：** 在能使用 Serena/Codebase-Memory 的情况下用 Grep 搜 C# 符号名再逐文件 Read 验证——这是反模式，慢且漏。

---

## Serena 调用速查（精确到参数）

> **场景：** 单符号级精确查询（定义/引用/实现/重命名/修改）

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
- 返回只有"自身 implements 那一行" = **零接线（孤儿组件）**
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

## Codebase-Memory 调用速查

> **场景：** 跨文件调用链分析、项目架构总览、复杂度/热点查询。基于知识图谱（符号+调用边）。
>
> **前置条件：** 必须先 `index_repository` 一次。索引产物在 `.codebase-memory/graph.db.zst`。

### 1. 项目架构总览 → `get_architecture`
```
project: "jnpf-v52"
aspects: ["overview", "clusters", "hotspots"]  // 或 ["all"]
path: "backend/modularity/inteAssistant"  // 可选，限定子目录
```
返回：包结构、依赖关系、Leiden 社区检测的 de-facto 模块（clusters）、复杂度热点。

### 2. 跨文件调用链追踪 → `trace_path`
```
function_name: "GenerateSkeletonViaTotAsync"
project: "jnpf-v52"
direction: "both"        // inbound=调用方, outbound=被调用方, both=双向
depth: 3                 // 追踪深度
mode: "calls"            // calls=调用边, data_flow=数据流, cross_service=跨服务
risk_labels: true        // 添加 CRITICAL/HIGH/MEDIUM/LOW 距离标签
```
**用途：** 改一个方法时，快速知道影响面半径。

### 3. 复杂度/热点查询 → `query_graph`（Cypher）
```cypher
MATCH (f:Function)
WHERE f.transitive_loop_depth >= 3 OR f.linear_scan_in_loop >= 1
RETURN f.qualified_name, f.transitive_loop_depth, f.linear_scan_in_loop
ORDER BY f.transitive_loop_depth DESC
```
**用途：** 找 O(n²) 隐患、找嵌套循环、找递归无 base case。

### 4. 自然语言/关键词搜索 → `search_graph`
```
query: "降级 fallback degradation"   // BM25 全文检索
semantic_query: ["fallback", "degradation", "silent"]  // 向量余弦（跨词汇）
project: "jnpf-v52"
```
**用途：** 不知道符号名但知道概念时。`semantic_query` 能跨词汇匹配（搜"send"找到"publish"）。

### 5. 文件内 grep + 图增强 → `search_code`
```
pattern: "throw Oops.Bah"
project: "jnpf-v52"
mode: "compact"           // compact=签名, full=源码, files=仅路径
path_filter: "^backend/"  // 正则限定路径
```
**用途：** grep 结果按"定义 > 热门函数 > 测试"排序，比裸 grep 高效。

---

## Knowledge Graph 调用速查

> **场景：** 领域知识、设计意图、历史决策、子链使命。基于人工沉淀的 JSON 记忆。

| 操作 | 工具 | 用途 |
|------|------|------|
| 查某主题的已知知识 | `search_nodes({query})` | 输入关键词，返回相关实体 |
| 看全部已沉淀知识 | `read_graph()` | 全量实体+关系（适合开局摸底） |
| 沉淀新知识 | `create_entities` / `add_observations` | 完成一个子链/重大决策后 |
| 连接实体 | `create_relations` | "A 依赖 B" / "A 属于 B" |

**KG 持久化路径：** `D:\JNPF-v52\.ai-memory\knowledge-graph.json`（gitignored）

**注意：** KG 与 Codebase-Memory 不同。KG 是**人工沉淀的领域知识**（设计意图、控制流、哲学）；Codebase-Memory 是**自动索引的代码结构**（符号、调用边、复杂度）。查"这个方法被谁调用"用 Codebase-Memory；查"这个子链为什么这样设计"用 KG。

---

## 典型场景示例

### 场景 A："X 组件是否被接线？"
1. `find_symbol(X)` → 确认存在 + 拿到文件路径
2. `find_referencing_symbols(X)` → 看返回引用数
3. 只有自身声明行 → 孤儿；多处引用 → 已接线

### 场景 B："改这个方法会影响谁？"
1. `find_symbol(方法名, include_body:false)` → 确认签名
2. `trace_path(方法名, direction:"inbound", depth:3)` → 多跳调用链
3. 逐个评估影响半径

### 场景 C："这个子链的设计意图是什么？"
1. `search_nodes({query: "子链名"})` → 读 observations
2. 不要靠读代码反推设计意图——KG 里的 observations 是之前明确记录的

### 场景 D："这个项目的真实模块边界在哪？"
1. `get_architecture(aspects:["clusters"])` → 看 Leiden 社区检测
2. clusters 往往切穿文件夹布局，揭示 de-facto 模块

### 场景 E："找所有降级/兜底模式"
1. `search_code(pattern:"降级|fallback|IsSuccess = true", mode:"compact")` → grep + 图增强
2. `search_graph(query:"degradation fallback")` → 跨文件语义匹配
3. 双管齐下，不漏

---

## 配置位置

MCP 服务器配置在 `.zcode/config.json`（项目级，优先级高于用户级）：
- **serena**: `stdio` 启动，`--context desktop-app --project D:\JNPF-v52`，timeout 180s
- **codebase-memory**: `codebase-memory-mcp.exe`，timeout 300s，需先 `index_repository`
- **knowledge-graph**: `@modelcontextprotocol/server-memory`，持久化到 `.ai-memory/knowledge-graph.json`

**⚠️ context 参数铁律：** Serena 必须用 `--context desktop-app`。**禁止** `--context claude-code`（在 ZCode 客户端下会握手失败，服务器启动后 2ms 即被关闭）。`--project-from-cwd` 参数 Serena CLI 不支持，也禁止使用。

**冷启动注意：** ZCode 新会话首次调用 MCP 可能有几秒延迟（进程拉起）。Serena 首次启动还需加载 Roslyn LSP（约 2-5 秒）。若某次调用超时，重试一次即可——通常是冷启动，不是配置错。

**已验证可用（2026-07-17）：** Serena CLI 手动启动成功（51 工具，JNPF-v52 已注册，2349+2354 符号已缓存）；Knowledge Graph `read_graph` 返回 10 实体完整；Codebase-Memory 待索引。
