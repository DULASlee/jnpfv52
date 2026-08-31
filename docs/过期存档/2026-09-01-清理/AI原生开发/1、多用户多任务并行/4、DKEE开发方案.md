# DKEE 开发方案：领域知识按需检索增强

> **文档编号：** 4
> **版本：** v1.0
> **状态：** 已审批，开发中
> **创建日期：** 2026-07-15
> **性质：** sa-service 退役收尾 + DKEE 在 JNPF 低代码平台的正确落地方案
> **上游：** [25、需求分析子链重构方案书](./25、需求分析子链重构方案书（修订版）.md) · [阶段A/B/C](./3、阶段C.md)
> **参考：** RACG 综述（arXiv 2510.04905）· Agent Memory 设计模式（Trixly AI）

---

## 0. 背景与问题

### 0.1 sa-service 是什么

sa-service（端口 3001）是一个 Node.js 服务，做两件事：
1. SA 九步分析（从需求推导九张设计图）
2. DKEE——从分析结果中提炼可复用的领域知识

### 0.2 现状：已死

读完全部代码后确认：**sa-service 在 compile 模式（当前唯一生产路径）下不被任何代码调用。**

| 维度 | 事实 |
|------|------|
| SA 九步 | 已由 C# 实现：前 7 步 `SaNineViewCompiler`（零 LLM），后 2 步 `PmSkillService.EnhancePspecDecisionTableAsync`（调后端 LlmGateway）|
| 唯一调用入口 | `SaOrchestratorAdapter`，已标 `[Obsolete]`，仅 `S2Mode=agent`（非默认）时可达 |
| MCP `sa.run-step` | 死配置，compile 路径从不调用 |
| DKEE 能力 | **从未真正运行过**——SQL 查询引用的数据库列不存在，目标表 `kg_pattern` 未建 |

### 0.3 DKEE 之前的错误理解

DKEE 在 sa-service 里的设计是"从 sa_* 表的产出中统计提炼规律"。但 `SaNineViewCompiler` 产出的是**占位兜底内容**（`BUSINESS_DATA`、`BusinessEntity`、通用 `Draft→Submitted→Approved`），从垃圾产出里提炼统计规律 = **垃圾进垃圾出**。

### 0.4 正确理解：RAG（检索增强生成）

业界做"领域知识积累和复用"的主流方法是 RAG：给 AI 一个可检索的知识库，AI 在生成时按需检索，不是预塞提示词。DKEE 的本质应该是这个——**给 PM Skill 一个领域知识库，PM 在分析的不同环节按需检索对应知识。**

---

## 1. 方案总纲

### 1.1 核心思路

```
PM 分析需求时，像带着"行业手册"的专家：
  完善需求时 → 翻手册查同类系统的完整方案
  设计规则时 → 翻手册查规则惯例
  出追问问题时 → 翻手册查常见陷阱
```

手册（知识库）越来越丰富，PM 越来越专业。但每次只翻当前需要的那一页，不是把整本手册背下来。

### 1.2 三部分工作

| 部分 | 做什么 | 依赖关系 |
|------|-------|---------|
| **第一部分：sa-service 死代码清理** | 删掉死配置和废弃 C# 代码 | 独立，无依赖 |
| **第二部分：按需检索机制** | PM 的 3 个 LLM 调用点各接一个检索点 | 依赖现有 `IDomainSeedService` |
| **第三部分：丰富种子库** | 扩充 `ai_seed_templates` 的高质量领域方案 | 独立，和检索机制解耦 |

---

## 2. 第一部分：sa-service 死代码清理

### 2.1 死配置清理

| 文件 | 改动 | 原因 |
|------|------|------|
| `appsettings.json` | 删除 `"SA": { "ServiceUrl": "..." }` | 代码读 `SaService:BaseUrl` 走硬编码默认；compile 模式不调用 |
| `Configurations/McpTools.json` | 删除 `sa.run-step` 条目 | compile 路径从不调用，MCP 网关只路由 inproc 工具 |
| `sa-service/README.md`（新建）| 标注 compile 模式不启动、SA 已由 C# 实现 | 防止后续开发者困惑 |

### 2.2 删除废弃 C# 死代码（需 CR 审批 — 铁律六）

| 文件 | 改动 | 原因 |
|------|------|------|
| `Sa/SaOrchestratorAdapter.cs` | 整文件删除（含 `ISaOrchestratorAdapter` 接口）| `[Obsolete]`，仅 agent 模式可达 |
| `PipelineSchedulingModule.cs:21-24` | 删除 `services.AddHttpClient("SaService", ...)` | 配套死代码 |
| `Skills/AnalystSkillService.cs` | 删除 `ISaOrchestratorAdapter` 字段注入 + agent 分支 | 只保留 compile 分支 |

`SaPipelineOptions.IsCompileMode` 保留（保留模式判断能力），只删 agent 调用路径。

> **注意**：`AnalystAffectedStepsRerunService` 也依赖 `ISaOrchestratorAdapter`（`RunStepAsync`，用于 step 级重跑）。该服务的重构需单独 CR，本方案不含。

---

## 3. 第二部分：按需检索机制（核心）

### 3.1 PM 分析流程全景

读代码确认的 PM 流程（`RequirementAnalysisOrchestrator.RunPmPipelineAsync`）：

```
步骤① EnhanceRequirementAsync     PM LLM 完善需求文本（可能追问）
  │
步骤② RunStep2DecomposeAsync       SA 九步拆解
  ├─ 2a: SaNineViewCompiler 7步确定性编译（零 LLM）
  └─ 2b: EnhancePspecDecisionTableAsync（LLM 产规则语义）  ← 检索点 B
  │
步骤③ RefineFromAnalysisAsync      PM LLM 反向完善需求（可能追问）
  │
步骤④ RenderSpecAndWaitConfirm     渲染说明书，等确认
  │
步骤⑤ Finalize                     确认后落库
```

### 3.2 三个检索点

只有调 LLM 的步骤才需要检索（确定性步骤不需要）。3 个 LLM 调用点：

#### 检索点 A：步骤①③ 完善需求时 → 搜整体方案

**位置**：`PmSkillService.EnhanceRequirementAsync` / `RefineFromAnalysisAsync`

**现状**：
```csharp
// EnhanceRequirementAsync:145-201
var seeds = await RetrieveEvolutionSeedsAsync(...);      // 查进化种子（历史教训）
var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);
var systemPrompt = "你是PM专家..." + seedPrompt;          // 塞 systemPrompt
```

**改后**：在查进化种子的同时，加一次领域知识检索：
```csharp
var seeds = await RetrieveEvolutionSeedsAsync(...);
var seedPrompt = RequirementEvolutionContext.RenderPromptBlock(seeds);

// 新增：按需检索领域知识
var domainSeeds = await _seedService.MatchAsync(
    ExtractSearchKeyword(context), ct);                   // "请假" → 截取需求关键词
var knowledgePrompt = DomainKnowledgeRenderer.Render(domainSeeds);

var systemPrompt = "你是PM专家..." + seedPrompt + knowledgePrompt;
```

`_seedService.MatchAsync` 是**现有方法**，现在只查 40 条硬编码模板。只要高质量领域方案写进 `ai_seed_templates`，这里自动检索到。

#### 检索点 B：步骤②b 产出规则时 → 搜规则知识

**位置**：`PmSkillService.EnhancePspecDecisionTableAsync:386-463`

**现状**：
```csharp
var systemPrompt = "你是系统分析师。为每个事件产出 PSpec/DecisionTable...";
// 静态文本，没有任何知识注入
```

**改后**：按当前需求检索规则知识：
```csharp
var ruleSeeds = await _seedService.MatchAsync(
    $"{ExtractSearchKeyword(context)} 规则 审批", ct);
var rulePrompt = DomainKnowledgeRenderer.RenderRules(ruleSeeds);

var systemPrompt = "你是系统分析师..." + rulePrompt;
```

#### 检索点 C：追问出题时 → 搜易错点

**位置**：`PmSkillService.GenerateClarificationAsync`

**现状**：PM 一次 LLM 调用产出选择题。

**改后**：出题前检索这个行业的常见遗漏点：
```csharp
var pitfallSeeds = await _seedService.MatchAsync(
    $"{keyword} 注意事项 易错", ct);
var pitfallPrompt = DomainKnowledgeRenderer.RenderPitfalls(pitfallSeeds);

var systemPrompt = "你是PM专家，产出追问题..." + pitfallPrompt;
```

### 3.3 token 消耗控制

每个检索点独立，查不到就零消耗，查到了由渲染器控制：

```csharp
/// <summary>
/// 领域知识渲染器 — 把检索到的种子压缩成简洁的参考文本。
/// 查不到返回空字符串（零 token 消耗）。
/// </summary>
public static class DomainKnowledgeRenderer
{
    private const int MaxSeeds = 3;
    private const int MaxCharsPerSeed = 200;

    public static string Render(IReadOnlyList<SeedTemplateMatch> seeds)
    {
        if (seeds.Count == 0) return string.Empty;        // 查不到 → 零 token
        var sb = new StringBuilder();
        sb.AppendLine("参考方案（历史积累，仅供参考不要照抄）：");
        foreach (var s in seeds.Take(MaxSeeds))            // 最多 3 条
            sb.AppendLine($"- {s.Industry}/{s.EventNamePattern}: {Truncate(s.TemplateJson, MaxCharsPerSeed)}");
        return sb.ToString();
    }
}
```

**控制逻辑**：查不到 → 零 token。查到了 → 最多 3 条 × 200 字 = 600 字。每个检索点独立，步骤①不用规则知识就不查规则。

### 3.4 改动文件清单

| 文件 | 改动 | 风险 |
|------|------|------|
| `Skills/PmSkillService.cs` | 3 个检索点接入 + 构造函数注入 `IDomainSeedService` | 中（关键业务方法，需 CR）|
| 新建 `Skills/DomainKnowledgeRenderer.cs` | 领域知识渲染器（纯函数，3 个 Render 方法）| 低（新文件）|

---

## 4. 第三部分：丰富种子库

### 4.1 现状

`ai_seed_templates` 表现有 40 条硬编码模板（`DomainSeedService.BuildDefaultSeeds`），每条只有简单 JSON：
```json
{"eventId":"BE-001","eventName":"请假","complexityHint":"auto"}
```
信息量极低，对 PM 没有实质帮助。

### 4.2 扩充方向

把 `F_TemplateJson` 扩充成结构化的高质量领域方案：

```json
{
  "entities": [
    {"name": "请假申请", "fields": [
      {"name": "applicant", "type": "user", "required": true},
      {"name": "leave_type", "type": "enum", "options": ["事假","病假","年假"]},
      {"name": "start_date", "type": "date"},
      {"name": "end_date", "type": "date"},
      {"name": "days", "type": "decimal", "rule": "end_date - start_date"},
      {"name": "reason", "type": "text"},
      {"name": "status", "type": "enum", "options": ["草稿","审批中","通过","驳回"]}
    ]}
  ],
  "stateMachine": "草稿→提交→主管审批→(通过)HR备案→归档 / (驳回)→修改重提",
  "rules": [
    "事假需提前申请，病假可事后补",
    "3天以内主管审批，3天以上需总监加签",
    "年假余额不足自动驳回"
  ],
  "pitfalls": ["年假余额计算", "跨年请假处理", "节假日排除规则"]
}
```

### 4.3 种子来源

| 来源 | 方式 | 质量 |
|------|------|------|
| 人工编写 | 针对每个行业（hr/oa/manufacturing/engineering）的核心业务编写高质量方案 | 最高 |
| 真实项目沉淀 | 真正跑通的项目 Finalize 后，把高质量产出写入种子库（标记 `source=learned`）| 高（需验证）|

### 4.4 表结构扩展（可选）

给 `ai_seed_templates` 加列区分来源：

| 新列 | 类型 | 说明 |
|------|------|------|
| `F_Source` | NVARCHAR(20) | `builtin`（写死）/ `learned`（积累）|
| `F_ProjectId` | BIGINT NULL | learned 种子的来源项目 |

### 4.5 本方案不含

第三部分（丰富种子库内容）是**独立的、持续的工作**，和检索机制解耦。本开发方案只实现检索机制（第二部分）+ 清理（第一部分），种子内容的丰富化作为后续运营工作。

---

## 5. 实施计划（TDD · 逐 Task 推进）

> **铁律六**：PmSkillService 是关键业务方法，修改需 CR 审批。本文档即 CR，审批后才动手。

### 文件结构

| 文件 | 职责 | 操作 |
|------|------|------|
| `Skills/DomainKnowledgeRenderer.cs` | 纯函数渲染器：把 SeedTemplateMatch 列表压缩成简洁参考文本 | 新建 |
| `tests/JNPF.Tests.PhaseB/DomainKnowledgeRendererTests.cs` | 渲染器单元测试 | 新建 |
| `Skills/PmSkillService.cs` | PM Skill — 3 个检索点接入 + 构造函数注入 IDomainSeedService | 修改 |
| `appsettings.json` | 删除死配置 `SA:ServiceUrl` | 修改 |
| `Configurations/McpTools.json` | 删除死条目 `sa.run-step` | 修改 |
| `sa-service/README.md` | 标注 sa-service 已退役 | 新建 |

---

### Task 1: 领域知识渲染器（TDD）

**Files:**
- Create: `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/DomainKnowledgeRenderer.cs`
- Test: `backend/tests/JNPF.Tests.PhaseB/DomainKnowledgeRendererTests.cs`

**Step 1: 写失败测试**

```csharp
// backend/tests/JNPF.Tests.PhaseB/DomainKnowledgeRendererTests.cs
using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

public class DomainKnowledgeRendererTests
{
    [Fact]
    public void Render_EmptyList_ReturnsEmpty()
    {
        var result = DomainKnowledgeRenderer.Render(Array.Empty<SeedTemplateMatch>());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_NullList_ReturnsEmpty()
    {
        var result = DomainKnowledgeRenderer.Render(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_WithSeeds_ReturnsFormattedBlock()
    {
        var seeds = new List<SeedTemplateMatch>
        {
            new() { Industry = "hr", EventNamePattern = "请假", TemplateJson = """{"entities":["请假申请"],"rules":["3天以上需总监"]}""" },
        };
        var result = DomainKnowledgeRenderer.Render(seeds);
        Assert.Contains("参考方案", result);
        Assert.Contains("hr/请假", result);
    }

    [Fact]
    public void Render_MoreThan3Seeds_TakesTop3Only()
    {
        var seeds = Enumerable.Range(0, 5)
            .Select(i => new SeedTemplateMatch { Industry = "hr", EventNamePattern = $"事件{i}", TemplateJson = "{}" })
            .ToList();
        var result = DomainKnowledgeRenderer.Render(seeds);
        var lineCount = result.Count(c => c == '\n');
        Assert.True(lineCount <= 4); // 1 标题行 + 最多 3 条种子行
    }

    [Fact]
    public void Render_LongTemplateJson_TruncatedTo200Chars()
    {
        var longJson = new string('x', 500);
        var seeds = new List<SeedTemplateMatch>
        {
            new() { Industry = "hr", EventNamePattern = "请假", TemplateJson = longJson },
        };
        var result = DomainKnowledgeRenderer.Render(seeds);
        Assert.DoesNotContain(new string('x', 500), result);
    }

    [Fact]
    public void RenderRules_EmptyList_ReturnsEmpty()
    {
        var result = DomainKnowledgeRenderer.RenderRules(Array.Empty<SeedTemplateMatch>());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderPitfalls_EmptyList_ReturnsEmpty()
    {
        var result = DomainKnowledgeRenderer.RenderPitfalls(Array.Empty<SeedTemplateMatch>());
        Assert.Equal(string.Empty, result);
    }
}
```

**Step 2: 运行测试验证失败**

```bash
dotnet test backend/tests/JNPF.Tests.PhaseB/JNPF.Tests.PhaseB.csproj --filter "DomainKnowledgeRenderer" -v q
```
Expected: FAIL — `DomainKnowledgeRenderer` 不存在

**Step 3: 写实现**

```csharp
// backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/DomainKnowledgeRenderer.cs
using System.Text;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 领域知识渲染器 — 把检索到的种子压缩成简洁的参考文本注入 PM prompt。
/// 查不到返回空字符串（零 token 消耗）；查到了最多 3 条 × 200 字。
/// </summary>
public static class DomainKnowledgeRenderer
{
    private const int MaxSeeds = 3;
    private const int MaxCharsPerSeed = 200;

    /// <summary>渲染整体方案知识（用于 EnhanceRequirement / RefineFromAnalysis）。</summary>
    public static string Render(IReadOnlyList<SeedTemplateMatch> seeds)
    {
        if (seeds == null || seeds.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("参考方案（历史积累，仅供参考不要照抄）：");
        foreach (var s in seeds.Take(MaxSeeds))
            sb.AppendLine($"- {s.Industry}/{s.EventNamePattern}: {Truncate(s.TemplateJson, MaxCharsPerSeed)}");
        return sb.ToString();
    }

    /// <summary>渲染规则知识（用于 EnhancePspecDecisionTable）。</summary>
    public static string RenderRules(IReadOnlyList<SeedTemplateMatch> seeds)
    {
        if (seeds == null || seeds.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("规则参考（历史积累）：");
        foreach (var s in seeds.Take(MaxSeeds))
            sb.AppendLine($"- {s.EventNamePattern}: {Truncate(s.TemplateJson, MaxCharsPerSeed)}");
        return sb.ToString();
    }

    /// <summary>渲染易错点知识（用于 GenerateClarification 出题）。</summary>
    public static string RenderPitfalls(IReadOnlyList<SeedTemplateMatch> seeds)
    {
        if (seeds == null || seeds.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("此类系统的常见易错点（出题时重点关注）：");
        foreach (var s in seeds.Take(MaxSeeds))
            sb.AppendLine($"- {s.EventNamePattern}: {Truncate(s.TemplateJson, MaxCharsPerSeed)}");
        return sb.ToString();
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max] + "…");
}
```

**Step 4: 运行测试验证通过**

```bash
dotnet test backend/tests/JNPF.Tests.PhaseB/JNPF.Tests.PhaseB.csproj --filter "DomainKnowledgeRenderer" -v q
```
Expected: PASS — 7 个测试全绿

**Step 5: 提交**

```bash
git add backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/DomainKnowledgeRenderer.cs backend/tests/JNPF.Tests.PhaseB/DomainKnowledgeRendererTests.cs
git commit -m "feat(dkee): 新增 DomainKnowledgeRenderer 领域知识渲染器（纯函数 + 7 个单测）"
```

---

### Task 2: PmSkillService 构造函数注入 IDomainSeedService

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs:34-48`

**Step 1: 修改构造函数**

当前代码（PmSkillService.cs:34-48）：
```csharp
private readonly ILogger<PmSkillService> _logger;
private readonly IRequirementEvolutionContext? _evolutionContext;
private readonly RequirementGateService _gate;

public PmSkillService(
    ICognitiveSkillToolkit toolkit,
    ILogger<PmSkillService> logger,
    RequirementGateService gate,
    IRequirementEvolutionContext? evolutionContext = null)
    : base(toolkit)
{
    _logger = logger;
    _gate = gate;
    _evolutionContext = evolutionContext;
}
```

改为：
```csharp
private readonly ILogger<PmSkillService> _logger;
private readonly IRequirementEvolutionContext? _evolutionContext;
private readonly RequirementGateService _gate;
private readonly IDomainSeedService _seedService;

public PmSkillService(
    ICognitiveSkillToolkit toolkit,
    ILogger<PmSkillService> logger,
    RequirementGateService gate,
    IDomainSeedService seedService,
    IRequirementEvolutionContext? evolutionContext = null)
    : base(toolkit)
{
    _logger = logger;
    _gate = gate;
    _seedService = seedService;
    _evolutionContext = evolutionContext;
}
```

**Step 2: 编译验证**

```bash
dotnet build backend/modularity/inteAssistant/JNPF.InteAssistant/JNPF.InteAssistant.csproj -v q /nologo
```
Expected: 0 错误（IDomainSeedService 已有 DI 注册，ITransient 自动注入）

**Step 3: 提交**

```bash
git add backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs
git commit -m "refactor(pm): PmSkillService 注入 IDomainSeedService（为按需检索做准备）"
```

---

### Task 3: 检索点 A — EnhanceRequirementAsync

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs:145-201`

**Step 1: 在 EnhanceRequirementAsync 中加入检索点 A**

在现有 `RetrieveEvolutionSeedsAsync` 之后（`var seedPrompt = ...` 之后），加领域知识检索：

```csharp
// ── 1b. 按需检索领域知识（整体方案）──
var domainSeeds = await _seedService.MatchAsync(ExtractSearchKeyword(context), ct);
var knowledgePrompt = DomainKnowledgeRenderer.Render(domainSeeds);
```

然后修改 systemPrompt 拼接，把 `seedPrompt` 后面追加 `knowledgePrompt`：

```csharp
""" + "\n" + seedPrompt + knowledgePrompt;
```

**Step 2: 编译验证**

```bash
dotnet build backend/modularity/inteAssistant/JNPF.InteAssistant/JNPF.InteAssistant.csproj -v q /nologo
```
Expected: 0 错误

**Step 3: 运行现有 PM 测试确认无回归**

```bash
dotnet test backend/tests/JNPF.Tests.PhaseB/JNPF.Tests.PhaseB.csproj --filter "Pm" -v q
```
Expected: 全绿（IDomainSeedService.MatchAsync 查空时 Render 返回空字符串，不影响现有行为）

**Step 4: 提交**

```bash
git add backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs
git commit -m "feat(dkee): 检索点A — EnhanceRequirement 接入领域知识按需检索"
```

---

### Task 4: 检索点 B — EnhancePspecDecisionTableAsync

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs:386-420`

**Step 1: 在 EnhancePspecDecisionTableAsync 中加入检索点 B**

在 `var enhancedText = context.UserRequirement ?? string.Empty;` 之后，systemPrompt 定义之前，加入：

```csharp
// ── 按需检索规则知识 ──
var ruleSeeds = await _seedService.MatchAsync(
    $"{ExtractSearchKeyword(context)} 规则 审批", ct);
var rulePrompt = DomainKnowledgeRenderer.RenderRules(ruleSeeds);
```

然后修改 systemPrompt，在末尾追加 `rulePrompt`：

```csharp
只输出 JSON。
""" + "\n" + rulePrompt;
```

**Step 2: 编译验证**

```bash
dotnet build backend/modularity/inteAssistant/JNPF.InteAssistant/JNPF.InteAssistant.csproj -v q /nologo
```
Expected: 0 错误

**Step 3: 提交**

```bash
git add backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs
git commit -m "feat(dkee): 检索点B — EnhancePspecDecisionTable 接入规则知识按需检索"
```

---

### Task 5: 检索点 C — GenerateClarificationAsync

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`（GenerateClarificationAsync 方法，约 887-973 行）

**Step 1: 在 GenerateClarificationAsync 中加入检索点 C**

在该方法的 systemPrompt 定义之前，加入易错点检索：

```csharp
// ── 按需检索易错点知识 ──
var pitfallSeeds = await _seedService.MatchAsync(
    $"{ExtractSearchKeyword(context)} 注意事项 易错", ct);
var pitfallPrompt = DomainKnowledgeRenderer.RenderPitfalls(pitfallSeeds);
```

然后在 systemPrompt 拼接处，追加 `pitfallPrompt`（和已有的 `seedPrompt` + `slotsPrompt` 并列）。

**Step 2: 编译验证**

```bash
dotnet build backend/modularity/inteAssistant/JNPF.InteAssistant/JNPF.InteAssistant.csproj -v q /nologo
```
Expected: 0 错误

**Step 3: 运行 PM 测试确认无回归**

```bash
dotnet test backend/tests/JNPF.Tests.PhaseB/JNPF.Tests.PhaseB.csproj -v q
```
Expected: 全绿

**Step 4: 提交**

```bash
git add backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs
git commit -m "feat(dkee): 检索点C — GenerateClarification 接入易错点知识按需检索"
```

---

### Task 6: 死配置清理

**Files:**
- Modify: `backend/application/JNPF.API.Entry/appsettings.json`
- Modify: `backend/application/JNPF.API.Entry/Configurations/McpTools.json`
- Create: `sa-service/README.md`

**Step 1: 删除 appsettings.json 的 SA:ServiceUrl**

在 `appsettings.json` 中找到 `"SA": { "ServiceUrl": "http://localhost:3001" }` 段落，整段删除。

**Step 2: 删除 McpTools.json 的 sa.run-step 条目**

在 `McpTools.json` 中找到 `sa.run-step` 对象，整段删除。

**Step 3: 创建 sa-service/README.md**

```markdown
# sa-service（已退役）

> **状态：** 已退役（2026-07-15）
> **当前 Studio S2 模式：** compile（默认）— 不依赖 sa-service

sa-service 的 SA 九步分析能力已由 C# 实现：
- 前 7 步：`SaNineViewCompiler`（确定性编译，零 LLM）
- 后 2 步：`PmSkillService.EnhancePspecDecisionTableAsync`（调后端 LlmGateway）

compile 模式下 `start-dev.ps1` 不启动 sa-service。
DKEE 的领域知识按需检索能力已由 C# 的 `DomainKnowledgeRenderer` + `IDomainSeedService` 实现。
```

**Step 4: 编译验证**

```bash
dotnet build backend/modularity/inteAssistant/JNPF.InteAssistant/JNPF.InteAssistant.csproj -v q /nologo
```
Expected: 0 错误

**Step 5: 提交**

```bash
git add backend/application/JNPF.API.Entry/appsettings.json backend/application/JNPF.API.Entry/Configurations/McpTools.json sa-service/README.md
git commit -m "chore: 清理 sa-service 死配置（SA:ServiceUrl + McpTools sa.run-step）+ README"
```

---

### Task 7: 整体验收

**Step 1: 全量编译**

```bash
dotnet build backend/zx_lowcode_netcore.sln -v q /nologo
```
Expected: 0 错误

**Step 2: 全量测试**

```bash
dotnet test backend/zx_lowcode_netcore.sln -v q
```
Expected: 全绿

**Step 3: 死配置确认**

```bash
grep -r "SA:ServiceUrl" backend/application/
grep "sa.run-step" backend/application/JNPF.API.Entry/Configurations/McpTools.json
```
Expected: 零命中

**Step 4: 最终提交**

```bash
git add -A
git commit -m "test(dkee): 整体验收通过 — 编译0错误 + 测试全绿 + 死配置清理确认"
```

---

## 6. 验收标准汇总

### 6.1 检索机制验收

| 验收项 | 方式 | 通过标准 |
|--------|------|---------|
| 编译 | `dotnet build` | 0 错误 |
| 渲染器测试 | `dotnet test --filter DomainKnowledgeRenderer` | 7 个全绿 |
| token 控制 | 单测 `Render_EmptyList_ReturnsEmpty` | 返回空字符串 |
| 现有测试不回归 | `dotnet test --filter Pm` | 全绿 |
| 主链 | `E2E_PIPELINE_ID=311 pnpm test:api` | 现有用例全绿 |

### 6.2 清理验收

| 验收项 | 方式 | 通过标准 |
|--------|------|---------|
| 死配置 | grep | `SA:ServiceUrl` / `sa.run-step` 零命中 |

---

## 7. 风险

| 风险 | 等级 | 缓解 |
|------|------|------|
| PmSkillService 改动 | 中 | 铁律六要求 CR；只加检索调用，不改现有逻辑 |
| 种子质量低导致注入垃圾 | 低 | 第三部分持续丰富；渲染器做截断控制 |
| 检索无命中时行为 | 低 | `MatchAsync` 返回空列表 → 渲染器返回空字符串 → 零影响 |
| IDomainSeedService 注入 | 低 | 现有接口、现有注册，只加构造函数参数 |

---

## 附录 A：为什么不是向量检索

RACG 综述（arXiv 2510.04905）§4.1.4 结论：**BM25 关键词检索 + 简单注入，在代码生成场景效果最好**。现有的 `DomainSeedService.MatchAsync` 就是关键词子串匹配，够用。向量检索留给将来种子库规模到上千条时再考虑。

## 附录 B：与 32 号轨道 B 的关系

| | 32 号轨道 B（已落地）| 本方案（DKEE 落地）|
|---|---|---|
| 采集什么 | 过程信号（负反馈）：PM 低分、Amend 纠正 | 领域方案（正知识）：行业模板 |
| 注入方式 | `RenderPromptBlock` 预塞 3 条 | `DomainKnowledgeRenderer` 按需检索 |
| 数据源 | IR 事件流 | `ai_seed_templates` 知识库 |
| 互补性 | 教系统"哪里摔过跤" | 教系统"路该怎么走" |

两者共存，不冲突。
