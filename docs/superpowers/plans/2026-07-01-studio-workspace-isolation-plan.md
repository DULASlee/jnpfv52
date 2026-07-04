# StudioWorkspace 多用户隔离 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `{SystemPath}/StudioWorkspace/{tenantId}/{pipelineId}/` 下建立完整的 AI 开发工作区，包含路径隔离、沙箱绑定、交付打包和 Hook 白名单。

**Architecture:** 新建纯函数静态工具类 `StudioWorkspaceHelper` 集中所有路径逻辑，在 `AIDevelopmentPipelineService` 的 development/delivery 阶段调用，`guard-write.mjs` 通过文件桥接获取 AI 开发上下文实现设计时路径拦截。

**Tech Stack:** C# (.NET 8) + Node.js (Hook) + SqlSugar + Docker Sandbox

---

## File Structure

```
backend/modularity/
├── common/JNPF.Common/Configuration/
│   └── KeyVariable.cs                    ← [MODIFY] +1 行常量
├── inteAssistant/JNPF.InteAssistant/
│   ├── StudioWorkspaceHelper.cs          ← [CREATE] ~120 行静态工具类
│   ├── AIDevelopmentPipelineService.cs   ← [MODIFY] +workspace 初始化/上传/打包
│   └── PipelineOrchestratorService.cs    ← [MODIFY] +放弃时清理 workspace

.claude/hooks/
└── guard-write.mjs                       ← [MODIFY] +L4 AI workspace 白名单
```

---

### Task 1: KeyVariable.cs — 新增 StudioWorkspaceRoot 常量

**Files:**
- Modify: `backend/modularity/common/JNPF.Common/Configuration/KeyVariable.cs`

- [ ] **Step 1: 在 KeyVariable 类中新增常量**

在 `KeyVariable.cs` 第 10 行（类定义 `public class KeyVariable` 下方）插入：

```csharp
    /// <summary>
    /// AI 开发工作区根目录名.
    /// </summary>
    public const string StudioWorkspaceRoot = "StudioWorkspace";
```

位置：放在 `MultiTenancyType` 属性之后、`SystemPath` 属性之前（约第 41 行），与现有属性保持一致的 XML 注释风格。

- [ ] **Step 2: 编译验证**

```bash
cd backend && dotnet build
```
Expected: 0 errors. 仅新增一个 `const string`，无破坏性变更。

- [ ] **Step 3: Commit**

```bash
git add backend/modularity/common/JNPF.Common/Configuration/KeyVariable.cs
git commit -m "feat(A1): add StudioWorkspaceRoot constant to KeyVariable"
```

---

### Task 2: StudioWorkspaceHelper.cs — 创建静态工具类（核心文件）

**Files:**
- Create: `backend/modularity/inteAssistant/JNPF.InteAssistant/StudioWorkspaceHelper.cs`

- [ ] **Step 1: 创建完整文件**

```csharp
using JNPF.Common.Configuration;
using JNPF.InteAssistant.Interfaces;

namespace JNPF.InteAssistant;

/// <summary>
/// AI 开发工作区路径工具类
/// 纯函数、零状态、零依赖。所有路径计算集中于此。
/// </summary>
public static class StudioWorkspaceHelper
{
    // ─── 子目录名常量 ───

    private const string IrDir = "ir";
    private const string GeneratedDir = "generated";
    private const string WorkspaceDir = "workspace";
    private const string ArtifactsDir = "artifacts";

    // ─── 路径计算 ───

    /// <summary>
    /// 获取流水线工作区根路径: {SystemPath}/StudioWorkspace/{tenantId}/{pipelineId}/
    /// </summary>
    public static string GetPipelinePath(string tenantId, string pipelineId)
    {
        return Path.Combine(
            KeyVariable.SystemPath,
            KeyVariable.StudioWorkspaceRoot,
            tenantId,
            pipelineId);
    }

    /// <summary>
    /// 获取四个子目录完整路径
    /// </summary>
    public static (string Ir, string Generated, string Workspace, string Artifacts)
        GetPipelineSubPaths(string tenantId, string pipelineId)
    {
        var root = GetPipelinePath(tenantId, pipelineId);
        return (
            Path.Combine(root, IrDir),
            Path.Combine(root, GeneratedDir),
            Path.Combine(root, WorkspaceDir),
            Path.Combine(root, ArtifactsDir)
        );
    }

    // ─── 目录生命周期 ───

    /// <summary>
    /// 创建四个子目录（幂等，已存在则跳过）
    /// </summary>
    public static void EnsureDirectories(string tenantId, string pipelineId)
    {
        var (ir, generated, workspace, artifacts) = GetPipelineSubPaths(tenantId, pipelineId);
        Directory.CreateDirectory(ir);
        Directory.CreateDirectory(generated);
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(artifacts);
    }

    // ─── 路径安全校验 ───

    /// <summary>
    /// 断言目标路径在工作区内，防止路径穿越。
    /// 使用 Path.GetFullPath 解析 ../ 后再做前缀匹配。
    /// </summary>
    /// <exception cref="InvalidOperationException">目标路径不在工作区内</exception>
    public static void AssertWithinWorkspace(string filePath, string tenantId, string pipelineId)
    {
        var workspaceRoot = GetPipelinePath(tenantId, pipelineId);
        var resolvedWorkspace = Path.GetFullPath(workspaceRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        var resolvedTarget = Path.GetFullPath(filePath);

        if (!resolvedTarget.StartsWith(resolvedWorkspace, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"AI 流水线禁止写入工作区外路径: {filePath}. 允许前缀: {resolvedWorkspace}");
        }
    }

    // ─── 目录→文件列表转换（供 SandboxManager 使用）───

    /// <summary>
    /// 递归读取目录下所有文件，转换为 GeneratedFile 列表。
    /// 返回空列表（非 null）当目录不存在或为空时。
    /// </summary>
    public static List<GeneratedFile> ReadFilesFromDirectory(string directoryPath)
    {
        var files = new List<GeneratedFile>();

        if (!Directory.Exists(directoryPath))
            return files;

        foreach (var filePath in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(directoryPath, filePath)
                .Replace(Path.DirectorySeparatorChar, '/');
            var bytes = File.ReadAllBytes(filePath);

            files.Add(new GeneratedFile
            {
                FilePath = relativePath,
                Content = System.Text.Encoding.UTF8.GetString(bytes),
                FileType = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant()
            });
        }

        return files;
    }

    // ─── 交付打包 ───

    /// <summary>
    /// 将 generated/ 目录打包为 zip，返回 zip 文件完整路径。
    /// zip 文件放在 artifacts/ 子目录中。
    /// </summary>
    /// <exception cref="InvalidOperationException">generated/ 目录为空时抛出</exception>
    public static string CreateDeliveryZip(string tenantId, string pipelineId)
    {
        var (_, generated, _, artifacts) = GetPipelineSubPaths(tenantId, pipelineId);

        if (!Directory.Exists(generated) || Directory.GetFiles(generated).Length == 0)
            throw new InvalidOperationException("无生成产物可交付");

        var zipFileName = $"delivery-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
        var zipPath = Path.Combine(artifacts, zipFileName);

        // 删除同名旧文件（幂等）
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        System.IO.Compression.ZipFile.CreateFromDirectory(generated, zipPath);
        return zipPath;
    }

    // ─── 清理 ───

    /// <summary>
    /// 删除整个工作区目录。异常安全：失败仅记录，不抛异常。
    /// </summary>
    public static void DeleteWorkspace(string tenantId, string pipelineId)
    {
        try
        {
            var root = GetPipelinePath(tenantId, pipelineId);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
        catch (Exception ex)
        {
            // 静默处理：删除失败不阻塞放弃流程
            System.Diagnostics.Debug.WriteLine(
                $"StudioWorkspace cleanup failed for {tenantId}/{pipelineId}: {ex.Message}");
        }
    }

    // ─── AI 开发上下文标记（文件桥接，供 guard-write.mjs 读取）───

    private static readonly string AiDevContextFilePath = Path.Combine(
        Directory.GetCurrentDirectory(), ".claude", "ai-dev-context.json");

    /// <summary>
    /// 写入 AI 开发上下文文件，供 guard-write.mjs 读取以激活 L4 白名单规则。
    /// </summary>
    public static void WriteAiDevContext(string tenantId, string pipelineId)
    {
        var contextDir = Path.GetDirectoryName(AiDevContextFilePath);
        if (!string.IsNullOrEmpty(contextDir) && !Directory.Exists(contextDir))
            Directory.CreateDirectory(contextDir);

        var workspacePath = GetPipelinePath(tenantId, pipelineId);
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            pipelineId,
            tenantId,
            workspacePath,
            timestamp = DateTime.UtcNow.ToString("o")
        });
        File.WriteAllText(AiDevContextFilePath, json);
    }

    /// <summary>
    /// 清除 AI 开发上下文文件，退出 L4 白名单模式。
    /// </summary>
    public static void ClearAiDevContext()
    {
        try
        {
            if (File.Exists(AiDevContextFilePath))
                File.Delete(AiDevContextFilePath);
        }
        catch
        {
            // 静默处理
        }
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
cd backend && dotnet build
```
Expected: 0 errors. `GeneratedFile` 类型在 `JNPF.InteAssistant.Interfaces` 命名空间，已通过 `using` 引入。

- [ ] **Step 3: Commit**

```bash
git add backend/modularity/inteAssistant/JNPF.InteAssistant/StudioWorkspaceHelper.cs
git commit -m "feat(A1): add StudioWorkspaceHelper — path calculation, validation, zip, cleanup"
```

---

### Task 3: AIDevelopmentPipelineService — development 阶段接入工作区

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`

- [ ] **Step 1: 在 CreateAsync 中初始化工作区目录**

找到 `CreateAsync` 方法（第 88 行），在 `entity.Create()` 调用和 `await _db.Insertable(entity).ExecuteCommandAsync()` 之后，`return result` 之前，加入：

```csharp
        // 初始化 AI 工作区目录
        try
        {
            StudioWorkspaceHelper.EnsureDirectories(tenantId.ToString(), result.PipelineId.ToString());
            _logger.LogInformation("工作区目录已创建: PipelineId={Id}", result.PipelineId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建工作区目录失败: PipelineId={Id}", result.PipelineId);
            // 不阻塞流水线创建——目录可在阶段执行时重试
        }
```

- [ ] **Step 2: 在 ExecuteStageAsync 的 development 阶段写入 AI 上下文标记**

找到 `ExecuteStageAsync` 方法（第 264 行），在启动后台任务 `_taskRunner.Run(...)` 之前（约第 304 行），`stageName` 确认后加入：

```csharp
        // development 阶段：写入 AI 开发上下文标记，激活 guard-write L4 白名单
        if (stageName == PipelineStage.Development)
        {
            var tenantId = TenantResolver.Resolve();
            StudioWorkspaceHelper.EnsureDirectories(tenantId.ToString(), pipelineId.ToString());
            StudioWorkspaceHelper.WriteAiDevContext(tenantId.ToString(), pipelineId.ToString());
            _logger.LogInformation("AI 开发上下文已激活: PipelineId={Id}", pipelineId);
        }
```

- [ ] **Step 3: 在 delivery 阶段添加打包逻辑**

在 `StreamLlmResponseAsync` 中，找到 delivery 阶段的 prompt 模板使用处（第 1088 行附近）。在 delivery 阶段的 LLM 响应完成后，需要触发打包。但由于当前架构是 LLM 流式响应模式，delivery 的实际"打包"操作应在独立 API 端点中完成。

新增一个 API 端点用于交付打包（追加到文件末尾，在 `MapStageName` 方法之后）：

```csharp
    /// <summary>
    /// 交付打包：将 generated/ 目录打包为 zip 并返回下载路径
    /// GET /api/studio/pipeline/execute/{pipelineId}/delivery-package
    /// </summary>
    [HttpGet("{pipelineId:long}/delivery-package")]
    public async Task<object> GetDeliveryPackageAsync(long pipelineId)
    {
        var tenantId = TenantResolver.Resolve();
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .FirstAsync();

        if (pipeline == null)
            throw Oops.Bah($"流水线 {pipelineId} 不存在");

        try
        {
            var zipPath = StudioWorkspaceHelper.CreateDeliveryZip(
                tenantId.ToString(), pipelineId.ToString());

            // 清除 AI 开发上下文（退出 L4 白名单）
            StudioWorkspaceHelper.ClearAiDevContext();

            _logger.LogInformation("交付包已生成: PipelineId={Id}, Path={Path}", pipelineId, zipPath);

            return new
            {
                downloadUrl = $"/api/file/download?path={Uri.EscapeDataString(zipPath)}",
                fileName = Path.GetFileName(zipPath),
                generatedAt = DateTime.Now
            };
        }
        catch (InvalidOperationException ex)
        {
            throw Oops.Bah(ex.Message);
        }
    }
```

- [ ] **Step 4: 在沙箱相关逻辑中添加文件上传（如已有沙箱创建）**

在 `StreamLlmResponseAsync` 中 development 阶段 LLM 流式响应结束后，如果 `_sandbox` 已有活跃沙箱，加入上传逻辑。找到 development 阶段的 prompt 发送完成后的位置，追加：

```csharp
        // development 阶段完成后：上传 generated/ 产物到沙箱
        if (stageName == PipelineStage.Development)
        {
            try
            {
                var tenantId = TenantResolver.Resolve();
                var (_, generatedDir, _, _) = StudioWorkspaceHelper.GetPipelineSubPaths(
                    tenantId.ToString(), pipelineId.ToString());
                var sandboxId = $"pipeline-{pipelineId}";
                var sandbox = await _sandbox.GetStatusAsync(sandboxId);
                if (sandbox != null && sandbox.Status == "ready")
                {
                    var files = StudioWorkspaceHelper.ReadFilesFromDirectory(generatedDir);
                    if (files.Count > 0)
                    {
                        sse.Token("📦 正在上传文件到沙箱...");
                        await _sandbox.UploadFilesAsync(sandboxId, files);
                        sse.Token($"✅ 已上传 {files.Count} 个文件到沙箱");
                        logger.LogInformation("沙箱上传完成: {SandboxId}, {Count} 文件", sandboxId, files.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "沙箱上传失败: PipelineId={Id}", pipelineId);
                sse.Token($"⚠️ 沙箱上传失败: {ex.Message}");
            }
        }
```

**注意**：此段代码在 `StreamLlmResponseAsync` 方法内部，需要访问 `stageName` 变量——确认该变量在当前作用域内。若不在，从 `pipelineEntity.CurrentStage` 获取。

- [ ] **Step 5: 编译验证**

```bash
cd backend && dotnet build
```
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs
git commit -m "feat(A2/A4/A5): wire workspace dirs, ai-dev-context, sandbox upload, delivery zip into pipeline service"
```

---

### Task 4: PipelineOrchestratorService — 放弃时清理工作区

**Files:**
- Modify: `backend/modularity/inteAssistant/JNPF.InteAssistant/PipelineOrchestratorService.cs`

- [ ] **Step 1: 在 AbandonAsync 中追加 workspace 清理**

找到 `AbandonAsync` 方法（第 342 行），在已有沙箱销毁调用之后：

当前代码（第 358 行）：
```csharp
        // 检查关联沙箱并销毁（如果存在）
        await DestroyAssociatedSandboxIfExists(pipelineId);
```

在其后追加 workspace 清理：
```csharp
        // 清理 AI 工作区目录
        var tenantId = TenantResolver.Resolve();
        StudioWorkspaceHelper.DeleteWorkspace(tenantId.ToString(), pipelineId.ToString());

        // 清除 AI 开发上下文标记
        StudioWorkspaceHelper.ClearAiDevContext();
```

完整上下文（第 356–360 行区域）：
```csharp
        var currentUserId = GetUserId().ToString();

        // 检查关联沙箱并销毁（如果存在）
        await DestroyAssociatedSandboxIfExists(pipelineId);

        // 清理 AI 工作区目录
        var tenantId = TenantResolver.Resolve();
        StudioWorkspaceHelper.DeleteWorkspace(tenantId.ToString(), pipelineId.ToString());

        // 清除 AI 开发上下文标记
        StudioWorkspaceHelper.ClearAiDevContext();

        // 状态设为 Abandoned
        pipeline.StageStatus = PipelineStatus.Abandoned;
```

- [ ] **Step 2: 编译验证**

```bash
cd backend && dotnet build
```
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add backend/modularity/inteAssistant/JNPF.InteAssistant/PipelineOrchestratorService.cs
git commit -m "feat(A3-s2): cleanup workspace and ai-dev-context on pipeline abandon"
```

---

### Task 5: guard-write.mjs — 新增 L4 AI 开发态工作区白名单

**Files:**
- Modify: `.claude/hooks/guard-write.mjs`

- [ ] **Step 1: 在现有 L3 安全扫描之后、`process.exit(0)` 之前，新增 L4 规则**

找到文件中 `process.exit(0)` 所在位置（当前第 136 行）。在其**之前**插入 L4 规则：

```javascript
// ═══════════════════════════════════════════════════════════════
// L4: AI 开发态工作区隔离 — 拦截写入主仓库路径 (exit 2)
// ═══════════════════════════════════════════════════════════════
// 通过文件桥接读取 AI 开发上下文（由 AIDevelopmentPipelineService 写入）
const AI_DEV_CONTEXT_PATH = '.claude/ai-dev-context.json';
let aiDevContext = null;
try {
  const fs = await import('fs');
  if (fs.existsSync(AI_DEV_CONTEXT_PATH)) {
    const raw = fs.readFileSync(AI_DEV_CONTEXT_PATH, 'utf-8');
    aiDevContext = JSON.parse(raw);
  }
} catch {
  aiDevContext = null;
}

if (aiDevContext && aiDevContext.pipelineId) {
  // AI 开发任务中：只允许写入白名单路径
  const workspacePrefix = (aiDevContext.workspacePath || '').replace(/\\/g, '/');

  const allowedPatterns = [
    /StudioWorkspace[/\\]/,           // 工作区文件
    /\.claude[/\\]/,                  // 项目配置 + ai-dev-context
    /docs[/\\]/,                      // 设计文档
    /workspace[/\\]/,                 // 流水线 workspace 目录
  ];

  // 额外允许：明确的 workspace 路径前缀
  const isAllowed = allowedPatterns.some(p => p.test(filePath))
    || (workspacePrefix && filePath.replace(/\\/g, '/').startsWith(workspacePrefix));

  if (!isAllowed) {
    console.error(`BLOCKED: AI 开发态禁止写入主仓库路径: ${filePath}`);
    console.error(`  当前 pipelineId: ${aiDevContext.pipelineId}`);
    console.error(`  工作区: ${aiDevContext.workspacePath}`);
    console.error(`  允许前缀: StudioWorkspace/, .claude/, docs/, workspace/, 工作区路径`);
    process.exit(2);
  }
}
```

**关键点**：
- 使用 `fs.existsSync` + `fs.readFileSync` 读取 `.claude/ai-dev-context.json`
- 文件不存在 → `aiDevContext = null` → L4 不激活，回退默认行为（保守安全）
- 文件存在且有 `pipelineId` → 激活白名单规则
- 白名单包含 `StudioWorkspace/`、`.claude/`、`docs/`、`workspace/` 以及明确的工作区路径前缀

- [ ] **Step 2: 手动验证 Hook 行为**

验证 1：无 ai-dev-context.json 时，Write 工具应正常工作（L4 不激活）
验证 2：有 ai-dev-context.json 时，写入 `backend/` 应被 BLOCK
验证 3：有 ai-dev-context.json 时，写入 `StudioWorkspace/1/123/generated/test.cs` 应被允许

- [ ] **Step 3: Commit**

```bash
git add .claude/hooks/guard-write.mjs
git commit -m "feat(A3-s1): add L4 AI workspace whitelist in guard-write hook"
```

---

### Task 6: 编译 + 回归验证

**Files:** None (verification only)

- [ ] **Step 1: 完整后端编译**

```bash
cd backend && dotnet build
```
Expected: 0 errors, 0 warnings.

- [ ] **Step 2: 验证传统代码生成不受影响**

```bash
git diff --name-only HEAD~5..HEAD
```
确认以下文件**不在**变更列表中：
- `backend/modularity/codegen/JNPF.CodeGen/CodeGenService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/SandboxManager.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/Interfaces/ISandboxManager.cs`

- [ ] **Step 3: 验证 CodeGenerate 路径未变**

```bash
grep -n "CodeGenerate" backend/modularity/codegen/JNPF.CodeGen/CodeGenService.cs
```
确认输出仍为 `Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName)`，未被修改。

- [ ] **Step 4: Commit（如无需修复）**

本任务为纯验证，无需 commit。

---

## Verification Summary

| # | 验证项 | 方法 | 预期 |
|---|--------|------|------|
| 1 | 编译 | `dotnet build` | 0 errors |
| 2 | KeyVariable 常量 | `grep "StudioWorkspaceRoot"` | 找到定义 |
| 3 | StudioWorkspaceHelper 完整 | 检查 7 个 public 方法均存在 | 7/7 |
| 4 | CreateAsync 初始化 | 读代码确认 `EnsureDirectories` 调用 | 存在 |
| 5 | ExecuteStageAsync 上下文标记 | 读代码确认 `WriteAiDevContext` 调用 | development 阶段存在 |
| 6 | GetDeliveryPackageAsync | 读代码确认新端点 | 存在 |
| 7 | AbandonAsync 清理 | 读代码确认 `DeleteWorkspace` + `ClearAiDevContext` | 存在 |
| 8 | guard-write L4 规则 | 读代码确认文件桥接 + 白名单逻辑 | 存在 |
| 9 | CodeGenService 零改动 | git diff | 无变更 |
| 10 | SandboxManager 零改动 | git diff | 无变更 |
