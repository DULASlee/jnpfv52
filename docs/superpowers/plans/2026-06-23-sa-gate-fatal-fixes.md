# SA Gate 致命缺陷修正 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix D爷's 4 fatal defects (Fail-Open→Fail-Closed, JSON裸奔→ExtractJson, Record写操作→with表达式, 同步阻塞→异步SSE) + Excel header detection + integrate SemanticFitnessValidator into GatePipeline.

**Architecture:** Keep existing GatePipeline 5-step structure, insert step 4.5 (semantic assessment). New `SemanticFitnessValidator` class handles LLM-based semantic fitness evaluation with Fail-Closed strategy. DTOs extended with `SemanticFitnessResult`/`IdentifiedElement`/`MissingElement`/`FitnessLevel` in `GateResult.cs`. All DTO changes use `record` with `with` expression for immutability safety.

**Tech Stack:** .NET 8, System.Text.Json, xUnit (added to test project), Moq (for mocking ILlmGatewayService)

**Key constraint:** Only modify files under `Gates/` namespace + `AIDevelopmentPipelineService.cs` integration point. Zero interference with other modules.

---

## File Structure

| # | File | Action | Purpose |
|---|------|--------|---------|
| 1 | `Gates/GateResult.cs` | Modify | Add SemanticFitnessResult, IdentifiedElement, MissingElement, FitnessLevel, SemanticallyUnfit factory |
| 2 | `Gates/GatePipelineOptions.cs` | Modify | Add SemanticMinScore, MinBusinessEvents, MinRoles, MinDataEntities, MinFieldsPerEntity, SemanticProvider |
| 3 | `Gates/SemanticFitnessValidator.cs` | **Create** | Core: LLM-based semantic fitness evaluation with Fail-Closed, ExtractJson, PostProcess |
| 4 | `Gates/GatePipeline.cs` | Modify | Inject SemanticFitnessValidator, insert step 4.5, BuildMergedText with source markers, image-all-failed warning |
| 5 | `Gates/IGatePipeline.cs` | Modify | Add optional `GateContext?` parameter |
| 6 | `Gates/AttachmentProcessor.cs` | Modify | Fix Excel header row detection (DetectHeaderRow, CountNonEmptyCells) |
| 7 | `GatePipeline.json` | Modify | Add semantic assessment config values |

---

### Task 1: Update GatePipelineOptions

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipelineOptions.cs`
- Modify: `backend/application/JNPF.API.Entry/Configurations/GatePipeline.json`

- [ ] Add semantic assessment configuration properties

```csharp
// GatePipelineOptions.cs — 在现有属性后追加

/// <summary>语义评估最低分数（0-100），低于此值门控拦截</summary>
public int SemanticMinScore { get; set; } = 60;

/// <summary>语义评估最低业务事件数</summary>
public int MinBusinessEvents { get; set; } = 1;

/// <summary>语义评估最低角色数</summary>
public int MinRoles { get; set; } = 1;

/// <summary>语义评估最低数据实体数</summary>
public int MinDataEntities { get; set; } = 1;

/// <summary>每个实体最低字段数</summary>
public int MinFieldsPerEntity { get; set; } = 5;

/// <summary>语义评估使用的 LLM Provider（默认 deepseek）</summary>
public string SemanticProvider { get; set; } = "deepseek";
```

- [ ] Add config values to GatePipeline.json

```json
{
  "GatePipeline": {
    "MaxFileSizeBytes": 20971520,
    "MaxTotalSizeBytes": 52428800,
    "MaxAttachmentCount": 10,
    "PerFileTimeoutMinutes": 2,
    "MaxConcurrentFiles": 3,
    "SemanticMinScore": 60,
    "MinBusinessEvents": 1,
    "MinRoles": 1,
    "MinDataEntities": 1,
    "MinFieldsPerEntity": 5,
    "SemanticProvider": "deepseek",
    "AllowedExtensions": [...],
    "BlockedExtensions": [...]
  }
}
```

---

### Task 2: Extend GateResult DTOs

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GateResult.cs`

- [ ] Add SemanticFitness + SemanticallyUnfit to GateResult, add new DTO records

Add after the existing `GateResult` record (keeping existing properties):
```csharp
// 新增属性（追加到 GateResult record 内）
/// <summary>语义评估结果</summary>
public SemanticFitnessResult? SemanticFitness { get; init; }

// 新增工厂方法（追加到 GateResult record 内）
public static GateResult SemanticallyUnfit(SemanticFitnessResult fitness, List<string>? warnings = null) =>
    new()
    {
        Passed = false,
        SemanticFitness = fitness,
        Reason = fitness.BuildSummary(),
        Hint = fitness.BuildGuidance(),
        Warnings = warnings?.AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>()
    };
```

Then add the new DTO records at the bottom of the file:
- `SemanticFitnessResult` (with `BuildSummary()`, `BuildGuidance()`)
- `FitnessLevel` enum (Sufficient, Partial, Insufficient)
- `IdentifiedElement` record
- `MissingElement` record

Also add `using System.Text;` at the top.

---

### Task 3: Create SemanticFitnessValidator

**Files:**
- Create: `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/SemanticFitnessValidator.cs`

- [ ] Create the complete SemanticFitnessValidator class (~200 lines)

The full implementation file with:
- `ITransient` marker interface
- Constructor injecting `ILlmGatewayService` + `ILogger<SemanticFitnessValidator>`
- `EvaluateAsync(text, options, ct)` — main entry point with Fail-Closed strategy
- `FailClosed(message, errorCode)` — static helper for all error returns
- `BuildSystemPrompt(options)` — injects MinBusinessEvents/MinRoles/etc from options
- `ExtractJson(rawContent)` — tolerant extraction: strip markdown, find first{ to last}, JsonDocument.Parse pre-validate, fallback fix trailing commas
- `DeserializeAndValidate(json)` — deserialize + validate identified/missing/score fields
- `PostProcess(raw, options)` — hard threshold override using `.ToList()` copy + `with` expression
- Static `s_jsonOptions` with `PropertyNameCaseInsensitive`, `AllowTrailingCommas`, `ReadCommentHandling = Skip`

---

### Task 4: Integrate into GatePipeline

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/GatePipeline.cs`
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/IGatePipeline.cs`

- [ ] Add `SemanticFitnessValidator` to constructor
- [ ] Change step 3 (merge text) to `BuildMergedText()` with source markers `【用户输入】` / `【附件提取内容】`
- [ ] Add `imageAnalysisFailed` tracking in step 2
- [ ] Add all-images-failed warning in step 2
- [ ] Insert step 4.5: `await _semanticValidator.EvaluateAsync(fullText, options, ct)`
- [ ] If semantic fails: return `GateResult.SemanticallyUnfit()`
- [ ] If semantic passes: carry `SemanticFitness` in GateResult output
- [ ] Add `GateContext? gateContext = null` optional parameter to ExecuteAsync
- [ ] Add `BuildMergedText()` static helper method

---

### Task 5: Fix Excel Header Detection in AttachmentProcessor

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/Gates/AttachmentProcessor.cs`

- [ ] Replace current Excel extraction logic (lines 95-148) with header-aware version

Add two new helper methods and modify `ExtractExcel`:
- `DetectHeaderRow(worksheet, colCount)` — if row1 has ≤1 non-empty cells and row2 has >1, header is row2
- `CountNonEmptyCells(worksheet, row, colCount)` — count cells with non-whitespace Text
- Update `ExtractExcel` to use `headerRow` instead of hardcoded row 1, adjust data rows to start at `headerRow + 1`

---

### Task 6: Add xUnit Tests

**Files:**
- Modify: `backend/tests/JNPF.Tests.Gate/JNPF.Tests.Gate.csproj` — add xUnit packages
- Create: `backend/tests/JNPF.Tests.Gate/Gates/SemanticFitnessValidatorTests.cs`
- Create: `backend/tests/JNPF.Tests.Gate/Gates/GatePipelineIntegrationTests.cs`

- [ ] Add NuGet packages to csproj:
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
<PackageReference Include="xunit" Version="2.7.0" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
<PackageReference Include="Moq" Version="4.20.70" />
```

- [ ] Write 8 SemanticFitnessValidator unit tests (red-green):
  1. Green: 详细MES需求_应该通过
  2. Green: Excel含表头_应该识别为字段
  3. Red: 仅写管理系统_应该不通过
  4. Red: 有角色无事件_应该不通过
  5. Hard threshold: LLM返回通过但无业务事件_硬阈值覆盖
  6. Fail-Closed: LLM调用失败_应该FailClosed
  7. Fail-Closed: LLM返回乱码JSON_应该FailClosed
  8. Fail-Closed: CancellationToken取消_应该FailClosed

- [ ] Write 5 integration tests:
  1. 完整管道_文档加图片_语义评估通过
  2. 完整管道_垃圾输入_硬规则拦截
  3. 完整管道_空洞输入_语义拦截
  4. 完整管道_附件损坏_其他附件正常
  5. 完整管道_图片全部失败_警告用户

---

### Task 7: Build and Verify

- [ ] Run `dotnet build` on JNPF.InteAssistant project
- [ ] Run `dotnet build` on JNPF.API.Entry project
- [ ] Run `dotnet test` on JNPF.Tests.Gate project
- [ ] Fix any compilation errors

---

## Self-Review

1. **Spec coverage:** All 4 fatal defects covered (Task 3,4), Excel fix covered (Task 5), test plan covered (Task 6)
2. **Placeholder scan:** No TODOs, TBDs, or vague instructions — all code is explicit
3. **Type consistency:** `SemanticFitnessResult` defined in Task 2, consumed in Tasks 3,4,6. `GatePipelineOptions` properties defined in Task 1, used in Tasks 3,4. `SemanticFitnessValidator` created in Task 3, injected in Task 4.
4. **Deferred items (out of scope):** AIDevelopmentPipelineService async refactoring (defect 4) — requires SSE infrastructure changes that need separate testing cycle; Excel header fix includes DetectHeaderRow but keeps existing EPPlus dependency
