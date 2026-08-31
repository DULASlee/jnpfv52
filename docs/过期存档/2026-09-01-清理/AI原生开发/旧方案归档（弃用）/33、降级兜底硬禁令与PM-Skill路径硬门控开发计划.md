> ⛔ **已废止 · DEPRECATED（2026-07-17）**  
> 本文档为旧计划（25–33 号），**禁止**再作编码或验收依据。  
> **现行施工依据：** [1、阶段A.md](../1、多用户多任务并行/1、阶段A.md) · [2、阶段B.md](../1、多用户多任务并行/2、阶段B.md) · [3、阶段C.md](../1、多用户多任务并行/3、阶段C.md)

---
# 降级/兜底硬禁令 & PM Skill 路径硬门控 & 种子知识关联筛选

**Goal:** 彻底清除系统中所有 AI 大模型降级/兜底逻辑，硬性拦截新的降级实现，确保 PM Skill 调用路径无法绕过用户需求文本，所有种子/领域知识/DKEE 注入参数必须经过用户当前需求关联筛选。

**Architecture:** 四阶段递进——① Hook L13 硬拦截（防新增），② 清除现有降级代码（修存量），③ PM Skill 路径硬门控，④ 种子/DKEE 关联筛选。

**Tech Stack:** C# (.NET 8) + Node.js (Hook mjs) + SQL (SqlSugar) + 知识图谱 MCP

---

## Phase 1: Guard-Write Hook L13 — 降级/兜底硬拦截

### Task 1.1: 添加 L13 降级/兜底关键词硬拦截

**File:** `.claude/hooks/guard-write.mjs`

在 L12 (ADF 写入锁) 之后、`process.exit(0)` 之前插入 L13 层。

**Insert at line ~400 (before `process.exit(0)`):**

```javascript
// ═══════════════════════════════════════════════════════════════
// L13: 降级/兜底硬拦截 (exit 2) — 仅 .cs，仅 inteAssistant 模块
//     禁止新增任何 LLM 降级/兜底/fallback 代码
// ═══════════════════════════════════════════════════════════════
if (isCs && typeof content === 'string' && content.trim()) {
  const isInteAssistant = /inteAssistant/i.test(filePath);
  if (isInteAssistant) {
    const lines = content.split('\n');
    const violations = [];

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const trimmed = line.trim();
      if (trimmed.startsWith('//') || trimmed.startsWith('*') || trimmed.startsWith('/*')) continue;
      // 豁免标记
      if (/degradation-ok:/i.test(line)) continue;

      // B1: "降级" 关键词（中文）— 降级/兜底的字面标记
      if (/降级/.test(line)) {
        violations.push({
          line: i + 1,
          rule: 'L13-B1',
          detail: `包含"降级"关键词。禁止新增 LLM 降级/兜底逻辑。\n  LLM 失败 MUST 抛 Oops.Bah()，禁止返回降级结果。\n  存量降级代码须逐步转换为硬错误。`
        });
      }

      // B2: BuildFallback* 方法 — fallback 构建器
      if (/\bBuildFallback\w+\s*\(/.test(line) && !/\/\/\s*L13-allow/i.test(line)) {
        violations.push({
          line: i + 1,
          rule: 'L13-B2',
          detail: `新增 BuildFallback* 方法。禁止创建兜底构建器。\n  LLM 失败 MUST 抛异常，不允许用兜底数据替代真实 LLM 输出。`
        });
      }

      // B3: catch (JsonException) 后直接 return 兜底对象
      if (/catch\s*\(\s*JsonException\b/i.test(line)) {
        // 检查后续 3 行是否有 return 兜底
        const nextLines = lines.slice(i + 1, Math.min(i + 4, lines.length)).join(' ');
        if (/\breturn\s+(new\s+)?(?!throw\b)/.test(nextLines) && /BuildFallback|降级|fallback/i.test(nextLines)) {
          violations.push({
            line: i + 1,
            rule: 'L13-B3',
            detail: `JsonException 捕获后返回兜底对象。\n  JSON 解析失败 MUST 抛 Oops.Bah()，不允许以兜底数据替代。`
          });
        }
      }

      // B4: !response.IsSuccess → return "completed" 状态（伪成功）
      if (/!.*\.IsSuccess\b/.test(line.trim())) {
        const nextLines = lines.slice(i + 1, Math.min(i + 8, lines.length)).join(' ');
        if (/return\s+new\s+\w+\s*\{/.test(nextLines) &&
            /Status\s*=\s*"completed"/.test(nextLines) &&
            /降级|fallback|兜底|默认/.test(nextLines)) {
          violations.push({
            line: i + 1,
            rule: 'L13-B4',
            detail: `LLM 失败后返回 Status="completed" 的伪成功结果。\n  LLM 调用失败 MUST 抛异常，禁止伪装成功。`
          });
        }
      }
    }

    // B5: 整个 content 的跨行模式检测
    // 检测 try { ChatWithSchemaRetryAsync } catch { return with fallback }
    const tryCatchFallbackPattern = /try\s*\{[^}]*?\bChat\w+Async\b[^}]*\}\s*catch[^{]*\{[^}]*\b(return|降级|BuildFallback)\b[^}]*\}/s;
    if (tryCatchFallbackPattern.test(content)) {
      violations.push({
        line: 0,
        rule: 'L13-B5',
        detail: `LLM Chat 调用被 try-catch 包裹后返回兜底。\n  LLM 失败 MUST 让异常向上传播，禁止在调用层吞掉异常返回兜底数据。`
      });
    }

    if (violations.length > 0) {
      console.error(`BLOCKED: 降级/兜底代码 (L13) in ${filePath}`);
      for (const v of violations) {
        const loc = v.line > 0 ? `第 ${v.line} 行` : '跨行检测';
        console.error(`  [${v.rule}] ${loc}: ${v.detail}`);
      }
      console.error(`  规则: 需求分析子链铁律 §降级/兜底禁令。`);
      console.error(`  LLM 失败 MUST 抛 Oops.Bah()，绝对禁止返回降级/兜底/默认结果。`);
      console.error(`  存量降级代码须逐步转换；新增降级代码零容忍。`);
      process.exit(2);
    }
  }
}
```

---

## Phase 2: 清除现有降级/兜底代码（PmSkillService）

### Task 2.1: PmSkillService.EnhanceRequirementAsync — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~235-248

**Current (degradation):**
```csharp
if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
{
    _logger.LogWarning("pm-skill EnhanceRequirement LLM 失败...");
    // LLM 失败 → 降级：直接用原始需求作为 EnhancedText
    return new RequirementEnhanceResult
    {
        Status = "completed",
        EnhancedText = retrievalText,
        CompletenessNotes = new[] { "LLM 完善失败，降级使用原始需求" },
        SeedIds = seeds.Select(s => s.CaseId).ToList(),
        ClarificationTurns = previousTurns?.Count ?? 0,
    };
}
```

**Replace with:**
```csharp
if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
{
    throw Oops.Bah(
        $"PM Skill EnhanceRequirement LLM 调用失败: {response.Error ?? "(无错误详情)"}" +
        $" pipeline={pipelineId} tenantId={tenantId}");
}
```

---

### Task 2.2: PmSkillService.ParseEnhanceResponse — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~342-353

**Current (degradation):**
```csharp
catch (JsonException)
{
    return new RequirementEnhanceResult
    {
        Status = "completed",
        EnhancedText = fallbackText,
        CompletenessNotes = new[] { "LLM 响应 JSON 解析失败，降级使用原始需求" },
        // ...
    };
}
```

**Replace with:**
```csharp
catch (JsonException ex)
{
    throw Oops.Bah(
        $"PM Skill EnhanceRequirement LLM 响应 JSON 解析失败: {ex.Message}" +
        $" pipeline={pipelineId}",
        ex);
}
```

---

### Task 2.3: PmSkillService.RefineFromAnalysisAsync — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~636-647

**Current (degradation):** `!IsSuccess → EnhancedText=enhancedText, "步骤③ LLM 失败，降级使用步骤①文本"`

**Replace with:**
```csharp
if (!refineResponse.IsSuccess || string.IsNullOrWhiteSpace(refineResponse.Content))
{
    throw Oops.Bah(
        $"PM Skill RefineFromAnalysis 步骤③ LLM 失败: {refineResponse.Error ?? "(无错误详情)"}" +
        $" pipeline={context.PipelineId} tenantId={context.TenantId}");
}
```

---

### Task 2.4: PmSkillService.ParseRefineResponse — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~1146-1155

**Current (degradation):** `catch JsonException → return fallbackText, "降级使用步骤①文本"`

**Replace with:**
```csharp
catch (JsonException ex)
{
    throw Oops.Bah(
        $"PM Skill RefineFromAnalysis LLM 响应 JSON 解析失败: {ex.Message}" +
        $" pipeline={pipelineId}",
        ex);
}
```

---

### Task 2.5: PmSkillService.RefineFromAnalysisStreamAsync — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~745-752

**Current (degradation):** META parse failure → EnhancedText=bodyText, "降级使用流式正文"

**Replace with:**
```csharp
// 原: if (!parsedMeta.HasValue) { ... return with bodyText as fallback }
// 改为:
if (!parsedMeta.HasValue)
{
    throw Oops.Bah(
        $"PM Skill RefineFromAnalysis 流式响应 META 解析失败" +
        $" pipeline={context.PipelineId} tenantId={context.TenantId}");
}
```

---

### Task 2.6: PmSkillService.EnhanceRequirementStreamAsync — 移除降级（2处）

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~875-884, ~887-897

**Current (degradation):** META failure → null PendingClarificationSet; empty response → "降级使用原始需求"

**Replace both with:**
```csharp
// 原: META 失败 → return null PendingClarificationSet（降级静默）
// 改为:
throw Oops.Bah(
    $"PM Skill EnhanceRequirement 流式响应 META 解析失败" +
    $" pipeline={context.PipelineId} tenantId={context.TenantId}");

// 原: 空响应 → "降级使用原始需求"
// 改为:
throw Oops.Bah(
    $"PM Skill EnhanceRequirement 流式 LLM 返回空响应" +
    $" pipeline={context.PipelineId} tenantId={context.TenantId}");
```

---

### Task 2.7: PmSkillService.GenerateClarificationAsync — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~1249-1253

**Current (degradation):** `!IsSuccess → BuildEmptyClarificationSet`

**Replace with:**
```csharp
if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
{
    throw Oops.Bah(
        $"PM Skill GenerateClarification LLM 失败: {response.Error ?? "(无错误详情)"}" +
        $" pipeline={pipelineId} tenantId={tenantId}");
}
```

---

### Task 2.8: PmSkillService.AmendProposeAsync — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~1436-1439

**Current (degradation):** `!IsSuccess → BuildFallbackUnderstanding(userMessage)`

**Replace with:**
```csharp
if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
{
    throw Oops.Bah(
        $"PM Skill AmendPropose LLM 失败: {response.Error ?? "(无错误详情)"}" +
        $" pipeline={pipelineId} tenantId={tenantId}");
}
```

---

### Task 2.9: PmSkillService.ParseAmendmentUnderstanding — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~1985-1987

**Current (degradation):** `catch JsonException → BuildFallbackUnderstanding(fallbackText)`

**Replace with:**
```csharp
catch (JsonException ex)
{
    throw Oops.Bah(
        $"PM Skill AmendPropose LLM 响应 JSON 解析失败: {ex.Message}" +
        $" pipeline={pipelineId}",
        ex);
}
```

---

### Task 2.10: PmSkillService.BuildFallbackUnderstanding — 删除方法

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~1991-1997

This method becomes dead code once Task 2.8 and 2.9 are done.

**Delete:**
```csharp
private static AmendmentUnderstanding BuildFallbackUnderstanding(string userMessage)
    => new()
    {
        Features = new List<string> { userMessage.Trim() },
        SummaryMarkdown = userMessage.Trim(),
        Severity = "patch",
    };
```

**Also remove from `BuildEmptyClarificationSet` if it's similarly a fallback method —** check if this method becomes dead after Task 2.7, and delete if so.

---

### Task 2.11: PmSkillService.RefineSkeletonAsync — 移除降级（2处）

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~1557-1564

**Current (degradation):** LLM failure → silently skip, use deterministic only; catch Exception → log and skip

**Replace with:**
```csharp
// 原: else block with silent log → 改为抛异常
if (response.IsSuccess && !string.IsNullOrWhiteSpace(response.Content))
{
    using var doc = JsonDocument.Parse(ExtractJson(response.Content));
    llmPatches = AmendmentPatchApplier.ParsePatches(doc.RootElement);
}
else
{
    throw Oops.Bah(
        $"PM Skill RefineSkeleton LLM 失败: {response.Error ?? "(无错误详情)"}" +
        $" pipeline={context.PipelineId} tenantId={context.TenantId}");
}

// 移除整个 catch (Exception ex) when (ex is not OutOfMemoryException) { ... 降级 ... } 块
// 让异常自然向上传播
```

**Remove the entire try-catch wrapper** around the LLM call in RefineSkeletonAsync — let failures propagate naturally.

---

## Phase 3: 清除其他服务中的降级/兜底代码

### Task 3.1: RequirementGateService.AssessMaturityAsync — 移除降级（3处）

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/RequirementGateService.cs`
**Lines:** ~183-212

**Current (degradation):** Three fail-safe returns with Score=40, Mode="confirm" on LLM failure

**For each of the 3 return sites, replace with:**
```csharp
throw Oops.Bah(
    $"需求门控成熟度评估 LLM 失败: {error ?? "(无错误详情)"}" +
    $" pipeline={pipelineId} tenantId={tenantId}");
```

---

### Task 3.2: RequirementGateService.BuildClarificationSet — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/RequirementGateService.cs`
**Lines:** ~312-424

Multiple fallback paths to BuildFallbackSet. Replace each fallback return with:
```csharp
throw Oops.Bah(
    $"需求门控澄清集构建 LLM 失败: {error ?? "(无错误详情)"}" +
    $" pipeline={pipelineId} tenantId={tenantId}");
```

**Delete `BuildFallbackSet` method** once all call sites are converted.

---

### Task 3.3: ArchitectSkillService — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/ArchitectSkillService.cs`
**Lines:** ~257-340

**Current (degradation):** LLM failure → BuildFallbackArchitectureClarification() with hardcoded questions

**Replace with:**
```csharp
if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
{
    throw Oops.Bah(
        $"架构设计澄清问答 LLM 失败: {response.Error ?? "(无错误详情)"}" +
        $" pipeline={pipelineId} tenantId={tenantId}");
}
```

**Delete `BuildFallbackArchitectureClarification` method.**

---

### Task 3.4: SystemDesignClarificationSkill — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/SystemDesignClarificationSkill.cs`
**Lines:** ~317-399

**Current (degradation):** LLM failure → BuildFallbackSystemDesignClarification() with hardcoded questions

**Replace with:**
```csharp
if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
{
    throw Oops.Bah(
        $"总体设计澄清问答 LLM 失败: {response.Error ?? "(无错误详情)"}" +
        $" pipeline={pipelineId} tenantId={tenantId}");
}
```

**Delete `BuildFallbackSystemDesignClarification` method.**

---

### Task 3.5: AnalystSkillService — 移除降级（2处）

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/AnalystSkillService.cs`
**Lines:** ~612-629, ~366

**Replace both with:**
```csharp
// 原: ReviewClarificationAnswersAgainstSpecAsync failure → silently continue
// 改为: throw
throw Oops.Bah(
    $"分析师澄清答案审查 LLM 失败: {response.Error ?? "(无错误详情)"}" +
    $" pipeline={pipelineId} tenantId={tenantId}");

// 原: EnrichSkeletonViaSemanticAnalysisAsync "失败降级为空"
// 改为: throw
throw Oops.Bah(
    $"分析师骨架语义增强 LLM 失败: {response.Error ?? "(无错误详情)"}" +
    $" pipeline={pipelineId} tenantId={tenantId}");
```

---

### Task 3.6: LlmGatewayService — 移除降级（3处）

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/LlmGatewayService.cs`

**3 degradation patterns to fix:**

1. **Provider degradation chain** (~multi-level fallback: primary→fallback→degraded providers):
   - Remove secondary/fallback provider chain. Only use the configured provider.
   - If the primary provider fails: throw, don't fallback.

2. **ParseResponse** (~lines 1225-1241): JSON parse failure → returns IsSuccess=true with Content=body (raw)
   - Replace with: catch JsonException → throw Oops.Bah with the raw body for debugging

3. **TreeSearchAsync** (~lines 594-603): partial success accepted
   - Replace with: any branch failure → throw, don't accept partial results

---

### Task 3.7: SkillLlmBudgetGuard — 移除降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Llm/SkillLlmBudgetGuard.cs`
**Lines:** ~104-111

**Current (degradation):** red tier → silently downgrades to "fast" model

**Replace with:**
```csharp
// 原: silently use fast model
// 改为: throw Oops.Bah with budget info
throw Oops.Bah(
    $"Skill LLM 预算不足: 当前 tier={currentTier} 需要 minimal 预算，但余额不足。" +
    $" 请充值或等待结算周期。");
```

---

### Task 3.8: RequirementAnalysisOrchestrator — 移除澄清降级

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/RequirementAnalysisOrchestrator.cs`
**Lines:** ~668-678, ~774-782, ~1557-1558, ~2054-2057, ~2113-2124

Multiple 降级 patterns for empty questions, empty compile, JSON parse failures.

**Pattern replacement:** Every `if (xxx.IsNullOrEmpty) { ... return with default/empty; }` that follows an LLM call → `throw Oops.Bah(...)`.

---

## Phase 4: PM Skill 调用路径硬门控 — 确保 UserRequirement 不可绕过

### Task 4.1: SkillHarness.RunAsync — 移除 LoadUserRequirementAsync 兜底

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/SkillHarness.cs`
**Line:** 158

**Current (bypass route):**
```csharp
var requirement = options.UserRequirement ?? await LoadUserRequirementAsync(pipelineId, ct);
```

**Replace with:**
```csharp
if (string.IsNullOrWhiteSpace(options.UserRequirement))
{
    throw Oops.Bah(
        $"SkillHarness.RunAsync: UserRequirement 不能为空。" +
        $" 调用方 MUST 显式传入 options.UserRequirement，" +
        $" 不得依赖 DB 加载兜底。skillId={skillId} pipelineId={pipelineId}");
}
var requirement = options.UserRequirement;
```

**Delete `LoadUserRequirementAsync` method** — it becomes dead code.

---

### Task 4.2: RequirementAnalysisOrchestrator — 修补 3 处无 UserRequirement 的调用

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/RequirementAnalysisOrchestrator.cs`
**Lines:** ~313, ~344, ~885

**Current (all three):**
```csharp
var skillOptions = new SkillRunOptions { ProviderCode = options?.ProviderCode };
// NO UserRequirement passed!
```

**Replace all three with:**
```csharp
var skillOptions = new SkillRunOptions
{
    ProviderCode = options?.ProviderCode,
    UserRequirement = userRequirement,  // MUST resolve from context/options
};
```

**Resolution strategy for `userRequirement`:**
- At each call site, trace the available context/parameters to find the user requirement text
- If the call site is inside a method that has access to `options.UserRequirement` or `context.UserRequirement`, use that
- If no UserRequirement available in scope, add it as a method parameter upstream until it reaches the call site
- This may require signature changes on intermediate methods — document each signature change

---

### Task 4.3: PmSkillService 所有 8 个公开入口方法 — 添加统一门控

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`

In each of these 8 entry methods that accept pipelineId/tenantId/projectId + some form of requirement text, add an entry guard at the very top:

1. `EnhanceRequirementAsync` — already has retrievalText, but it could be empty
2. `EnhanceRequirementStreamAsync` — already has retrievalText
3. `RefineFromAnalysisAsync` — already has enhancedText
4. `RefineFromAnalysisStreamAsync` — already has enhancedText
5. `GenerateClarificationAsync` — needs guard
6. `AmendProposeAsync` — needs guard
7. `RefineSkeletonAsync` — needs guard
8. `GenerateSkeletonViaTotAsync` — already has guard (previous fix)

**Guard pattern (add to each method body start):**
```csharp
// 确保此方法接收到的需求文本非空
var requirementText = RequirementTextHelper.ForPmPrompt(context); // or relevant extraction
if (string.IsNullOrWhiteSpace(requirementText))
{
    throw Oops.Bah(
        $"PM Skill {MethodName}: 用户需求文本不能为空。" +
        $" pipeline={pipelineId} tenantId={tenantId}");
}
```

**For AmendProposeAsync** specifically: also guard `userMessage` parameter:
```csharp
if (string.IsNullOrWhiteSpace(userMessage))
{
    throw Oops.Bah(
        $"PM Skill AmendPropose: userMessage 不能为空。" +
        $" pipeline={pipelineId} tenantId={tenantId}");
}
```

---

## Phase 5: 种子/领域知识/DKEE 关联筛选

### Task 5.1: ContextBuilderService.FindSeedMatchesAsync — 强制要求非空 requirement

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/ContextBuilderService.cs`

**Current:** Calls `DomainSeedService.MatchAsync(requirement)` — already guards empty keyword, but doesn't enforce that the caller passes a meaningful requirement.

**After Task 4.1 (SkillHarness doesn't load from DB), this becomes naturally enforced.** Add explicit guard:
```csharp
public async Task<List<SeedCase>> FindSeedMatchesAsync(string requirement, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(requirement))
    {
        // 需求文本为空 → 无法做有意义的种子匹配 → 返回空，不抛异常
        // （这不是降级，是合理的不匹配）
        _logger.LogWarning("FindSeedMatchesAsync: requirement 为空，跳过种子匹配");
        return new List<SeedCase>();
    }
    return await _domainSeedService.MatchAsync(requirement, ct);
}
```

---

### Task 5.2: DomainSeedService.MatchAsync — 增强关联筛选

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/DomainSeedService.cs`

**Current:** Simple bidirectional `string.Contains` matching, capped at 20. The 40 hardcoded seeds are across 4 industries (hr, oa, manufacturing, engineering).

**Problem:** For "智能更衣柜系统", seeds from "oa" (工单/work orders) and "hr" (考勤/attendance) may match on generic keywords, injecting irrelevant business context.

**Fix — Add relevance gating:**

After the current keyword filtering, add a second pass:

```csharp
// 二阶段：关联评分 — 基于需求文本长度和关键词密度
// 如果需求文本较短 (< 50 chars)，只保留事件名称完全包含关键词的种子
// 如果需求文本较长，计算关键词命中密度并阈值过滤

var filtered = matchedSeeds
    .Where(seed =>
    {
        // 计算关联强度：种子的事件名/标签与需求文本的关键词重叠度
        var seedText = $"{seed.EventName} {string.Join(" ", seed.Tags ?? new List<string>())}";
        var overlapScore = keywords.Count(kw => seedText.Contains(kw, StringComparison.OrdinalIgnoreCase));
        // 至少需要 1 个关键词重叠（如果 keyword 非空）
        return keywords.Count == 0 || overlapScore >= 1;
    })
    .Take(20)
    .ToList();
```

**Also increase filtering strictness:**
- Current: `string.Contains` bidirectional (too loose)
- Change: require at least 1 full keyword match AND the seed's event type tags must intersect with extracted requirement terms

---

### Task 5.3: PmSkillService.ExtractSearchKeyword — 移除兜底

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Lines:** ~2433

**Current (degradation):**
```csharp
// fallback to context.SeedMatches.FirstOrDefault()?.EventNamePattern ?? "enterprise"
```

**Replace with:**
```csharp
// 如果无法从用户需求中提取搜索关键词，不返回兜底值
if (string.IsNullOrWhiteSpace(keyword))
{
    throw Oops.Bah(
        $"PM Skill 无法从用户需求中提取搜索关键词。" +
        $" pipeline={context.PipelineId} tenantId={context.TenantId}" +
        $" 需求文本前100字符: {userRequirement.Truncate(100)}");
}
```

---

### Task 5.4: PmSkillService — 过滤注入 prompt 的 SeedMatches

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/PmSkillService.cs`
**Line:** ~2229

**Current:**
```csharp
context.SeedMatches.Take(5) // injected into userPrompt
```

**Replace with relevance-filtered selection:**
```csharp
// 只注入与当前需求真正相关的种子
var relevantSeeds = context.SeedMatches
    .Where(s => s.EventNamePattern != null &&
                requirementText.Contains(s.EventNamePattern, StringComparison.OrdinalIgnoreCase))
    .Take(5)
    .ToList();
// 如果过滤后为空 → 不注入种子参考段（比注入不相关的种子好）
if (relevantSeeds.Any())
{
    userPrompt += $"\n\n参考种子（可复用模式）：\n{JsonSerializer.Serialize(relevantSeeds)}";
}
```

---

### Task 5.5: DomainKnowledgeRenderer — 添加关联筛选

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/DomainKnowledgeRenderer.cs`

**Current:** Render/RenderRules/RenderPitfalls capped at 3 seeds, 200 chars each, no filtering logic beyond the cap.

**Add requirement-based relevance filter:**
```csharp
// 在 Render 方法开头添加：
public string Render(List<SeedCase> seeds, string userRequirement)
{
    if (string.IsNullOrWhiteSpace(userRequirement))
        return string.Empty; // 无法筛选 → 返回空，不注入无关联种子

    var relevantSeeds = FilterRelevant(seeds, userRequirement)
        .Take(3)
        .ToList();
    // ... existing rendering logic
}

private static List<SeedCase> FilterRelevant(List<SeedCase> seeds, string requirement)
{
    var terms = requirement.Split(new[] { ' ', '，', '、', '\n', '\r' },
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return seeds.Where(s =>
        terms.Any(t => s.EventName.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                       (s.Tags?.Any(tag => terms.Any(t => tag.Contains(t, StringComparison.OrdinalIgnoreCase))) ?? false))
    ).ToList();
}
```

---

### Task 5.6: ArchitectSkillService — 使用 RequirementTextHelper 提取关键词而非 ExtractSearchKeyword 兜底

**File:** `backend/modularity/inteAssistant/JNPF.InteAssistant/Skills/ArchitectSkillService.cs`

**Current:** Uses `PmSkillService.ExtractSearchKeyword` for seed scoring, which has the fallback to `SeedMatches.FirstOrDefault()?.EventNamePattern ?? "enterprise"`.

After Task 5.3 removes that fallback, this file will naturally fail if keyword extraction fails — which is correct behavior. Verify no other fallback path exists in the seed scoring logic.

---

## Phase 6: Hook 验证 & 构建验证

### Task 6.1: 验证 guard-write.mjs L13 正常工作

```powershell
node scripts/test-hooks.mjs
```

Ensure the new L13 layer doesn't break existing hook tests and that the new patterns are correctly detected.

### Task 6.2: 后端构建验证

```powershell
cd backend && dotnet build --no-restore
```

Ensure all C# changes compile with 0 errors.

### Task 6.3: API 快速冒烟

```powershell
E2E_PIPELINE_ID=311 pnpm test:api
```

Verify no regression in PM Skill / SA 九步 compiler / 需求分析流程.

---

## Dependency Order

```
Phase 1 (Hook L13)           ← 必须先完成，防止有人在此期间新增降级代码
    ↓
Phase 2+3 (清除降级代码)      ← 可并行：PmSkillService + 其他服务
    ↓
Phase 4 (PM Skill 路径硬门控) ← 依赖 Phase 2 (PmSkillService 是主要目标)
    ↓
Phase 5 (种子关联筛选)        ← 依赖 Phase 4 (UserRequirement 路径打通后，筛选才有意义)
    ↓
Phase 6 (验证)                ← 最终门禁
```

---

## Risk Assessment

| 风险 | 缓解 |
|------|------|
| 移除降级后 LLM 失败导致 Skill 全链中断 | 正确行为：LLM 失败 = 系统故障，不应伪装成功。用户可在失败后手动重试。 |
| Phase 4 签名变更导致编译断裂 | 每次签名变更后立即 `dotnet build`，逐方法推进 |
| 种子筛选过于严格导致零种子注入 | Task 5.4 有 "过滤为空则跳过种子段" 逻辑，不阻断流程 |
| L13 Hook 误拦合理代码 | 豁免标记 `// degradation-ok:` 可用于合理场景 |
