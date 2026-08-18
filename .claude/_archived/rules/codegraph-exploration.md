# CodeGraph 调用链探索 — Phase 2.5 动态深度引擎

> **CodeGraph 已索引 JNPF 后端：** 2,045 文件 → 33,094 符号 → 78,558 关系边。
> MCP 工具：`codegraph_explore`、`codegraph_node`。CLI：`callers`、`callees`、`impact`、`explore`、`node`、`affected`。
>
> **设计原则：动态预算替代硬限制。** 不同符号类型需要不同探索深度——Private Method 改名 1 级就够了，Interface 加方法需 3 级才能看到所有实现类。一刀切的"≤5次"既浪费简单场景的 Token，又不够覆盖复杂场景。

---

## Phase 2.5: 调用链探索（插入到 Phase 2 Brainstorm 和 Phase 3 Plan 之间）

---

## 变更类型路由表（动态深度引擎核心）

| 变更符号类型 | 最大深度 | 最大符号数 | Token预算 | 典型场景 |
|---|---|---|---|---|
| `private` method / `private` field | 1 级 | 10 个 | 800 tokens | 内部重构，无对外影响 |
| `public` method / `internal` class | 2 级 | 25 个 | 2,000 tokens | API 签名变更，需检查直接+间接调用方 |
| Entity / Aggregate Root / DTO | 2 级 | 40 个 | 3,200 tokens | 数据模型变更，影响 Service+Repository+前端 |
| `interface` / `abstract class` | 3 级 | 50 个 | 4,000 tokens | 契约变更，需看所有实现类+DI注册+Mock |
| 跨模块 API（REST/gRPC） | 3 级 | 50 个 | 4,000 tokens | 路由变更，需覆盖 Controller→Service→Repo→前端 |

> **JNPF 特定规则：** Entity 变更 MUST 覆盖到前端 DTO 层（`impact` 查所有引用方后，额外 `grep` 对应的 `.vue` / `.ts` 文件）。元数据驱动的表单可能通过 `[DisplayName]` / `[JsonProperty]` 关联，`impact` 已包含。

---

## 动态截断策略（优先级从高到低）

### 1. Token 预算耗尽 → 立即停止

```
当 CodeGraph 调用累计返回 > Token预算 时：
  → 停止新调用
  → 返回已收集结果
  → 附加标记: ⚠️ 存在未探索路径（已探索 N 符号 / 估计总量 M 符号）
  → AI 判断：是否请求人工扩展预算，或基于已有信息继续
```

### 2. 符号数达上限 → 按调用频次排序，保留高频

```
当 codegraph impact 返回 > 最大符号数 时：
  → 按"调用频次"降序排列
  → 保留前 N 个（N = 最大符号数）
  → 丢弃低频符号
  → 附加标记: ✂️ 已截断（丢弃 K 个低频符号，完整列表见 impact 原始输出）
```

### 3. 深度达上限 → 记录边界符号

```
当探索深度 = 最大深度 时：
  → 停止递归探索
  → 记录当前层级的所有符号为「边界符号」
  → 边界符号列表附在探索报告中
  → AI 判断：是否需人工深入其中某些边界符号
```

### 4. 发现循环依赖 → 人工确认

```
当发现 A → B → C → A 循环时：
  → 立即暂停
  → 输出循环依赖图 + 涉及的 3 个符号
  → 请求人工确认：这是设计如此（如事件总线回写）还是架构缺陷？
```

---

## 人机协作接口

触发以下任一条件时，AI MUST 输出明确请求，不得自行决定继续：

| 触发条件 | AI 动作 |
|---|---|
| Token 预算耗尽，关键路径未覆盖 | 输出 "预算耗尽，建议扩展至 X tokens 以覆盖路径 P" |
| 发现循环依赖 | 输出循环依赖图，询问"这是设计意图还是需修复的架构问题？" |
| 单次 `impact` 返回 > 100 个符号 | 输出警告 "可能存在过度耦合，建议架构审视" |
| 探索深度达上限，边界符号含 `interface`/`abstract` | 输出 "边界处存在待探索的接口实现" |

---

## 强制执行流程

### Step 1: 分类

```
📋 变更分类声明
- 目标符号: [方法名/类名/文件名]
- 符号类型: [private method / public method / interface / Entity / ...]
- 适用路由: [从路由表中查询 → 深度N级 / 符号M个 / Token预算T]
- 变更性质: [新增 / 修改签名 / 修改逻辑 / 删除 / 重构]
```

### Step 2: 探索（按路由表参数执行）

```bash
# 必做：查上游（谁调我）— 仅直接调用方
codegraph callers "<目标符号>"

# 必做：查下游（我调谁）— 仅直接依赖
codegraph callees "<目标符号>"

# 条件必做：深度 ≥ 2 级时查影响面
[深度 ≥ 2] → codegraph impact "<目标符号>"

# 条件必做：新增功能时查相似实现
[新增功能] → codegraph explore "<业务概念>"
```

### Step 3: 深度探索（仅当路由表允许 depth ≥ 2）

```
对 impact 返回的符号中，仅对以下类型递归探索：
  - interface / abstract class 的实现类
  - public API 的调用方
  - Entity 的 Repository/Service 引用

禁止对以下类型递归：
  - private method / field（已经是叶子节点）
  - DTO / ViewModel（数据载体，无行为）
  - 日志/度量/遥测调用（非业务逻辑）
```

### Step 4: 截断检查

```
每完成一个 CodeGraph 调用后，检查：
  [ ] Token 消耗 < 预算？
  [ ] 符号数 < 上限？
  [ ] 深度 < 上限？
  任一为否 → 执行截断策略 → 记录截断原因
```

### Step 5: 输出探索报告

```
✅ Phase 2.5 完成
  变更类型: [符号类型]  深度: [实际/上限]  符号: [实际/上限]  Token: [实际/预算]
  调用方: [N个]  被调用: [N个]  影响文件: [N个]  测试影响: [N个]
  截断标记: [无 / ✂️符号截断 / ⚠️预算耗尽 / 🔄循环依赖暂停]
  边界符号: [如需人工深入，列出符号名]
  相似实现: [参考文件]
  风险点: [跨模块断裂 / 签名不兼容 / 过度耦合 / 测试遗漏]
```

---

## 反降级铁律

遇到以下情况 MUS 通过 CodeGraph 完整探索后完整实现，禁止简化：

| 信号 | 路由类型 | 禁止行为 |
|---|---|---|
| 需求涉及跨模块调用 | Public Method (2级) | 只改当前文件，假设"调用方自己会适配" |
| 涉及 Entity/数据模型变更 | Entity (2级+前端) | 只改 Entity，不管 Service/DTO/前端 |
| 修改已有 API 签名 | Interface (3级) | 直接改签名，让实现类编译失败 |
| 复杂业务逻辑 | Explore 查相似模式 | 简化成 if-else，丢弃边界条件 |

---

## 索引维护

### 自动同步（Git Hook）

```bash
# .git/hooks/post-commit → codegraph-auto-sync.sh
# 每次提交后自动同步（仅 .cs 变更时，30s 防抖）
```

### 手动维护

```bash
cd D:/JNPF-v52/backend && codegraph sync .     # 增量同步
cd D:/JNPF-v52/backend && codegraph index .    # 全量重建
cd D:/JNPF-v52/backend && codegraph status .   # 查看状态
```

---

## 关联规则

- 工作流流水线 → `workflow-pipeline.md`
- 工程铁律（Law 4: No Shortcuts）→ `engineering-laws.md`
- 调试纪律（Phase 1: 数据流追踪）→ `debugging.md`
- 论断纪律（[COMPUTED] 标签）→ `assertion-discipline.md`
- 规则加载协议 → `rules-loader.md`
