# StudioWorkspace 多用户隔离 — 设计文档

> **日期**：2026-07-01
> **关联施工包**：Phase A — AI 原生开发平台多用户/多任务并行隔离
> **方案**：B — 轻量静态工具类

---

## 1. 背景与目标

JNPF v5.2 的 AI 开发流水线（`AIDevelopmentPipelineService`）目前缺少按租户和流水线隔离的文件工作区。所有 AI 生成的代码若直接落盘到主仓库路径，将带来：
- 多用户并发时的文件覆盖风险
- AI 代码与手写代码混杂，难以审计
- 沙箱部署缺少明确的文件来源目录

**目标**：在不动主仓库核心结构的前提下，1–2 周内补齐 `StudioWorkspace/{tenantId}/{pipelineId}/` 的完整隔离能力。

---

## 2. 架构概览

```
{SystemPath}/
├── CodeGenerate/           ← 传统 VisualDev 代码生成（不动）
│   └── {fileName}.zip
│
└── StudioWorkspace/        ← [新增] AI 开发工作区根目录
    └── {tenantId}/
        └── {pipelineId}/
            ├── ir/          ← IR 中间表示
            ├── generated/   ← AI 生成代码产物
            ├── workspace/   ← 工作临时文件
            └── artifacts/   ← 交付物（zip 等）
```

**核心原则**：
- 传统 `CodeGenerate/` 路径零改动
- `SandboxManager` 接口零改动
- 所有路径逻辑集中在 `StudioWorkspaceHelper` 静态类

---

## 3. 组件设计

### 3.1 `KeyVariable` 常量（改动：+1 行）

```csharp
// KeyVariable.cs — 新增
public const string StudioWorkspaceRoot = "StudioWorkspace";
```

### 3.2 `StudioWorkspaceHelper` 静态工具类（新建，~120 行）

**命名空间**：`JNPF.InteAssistant`（与 `PipelineOrchestratorService` 同模块）

**零依赖**：不注入、不读配置（除 `KeyVariable.SystemPath`）、不碰数据库。

#### API 清单

| 方法 | 签名 | 职责 |
|------|------|------|
| `GetPipelinePath` | `(string tenantId, string pipelineId) → string` | `{SystemPath}/StudioWorkspace/{tenantId}/{pipelineId}/` |
| `GetPipelineSubPaths` | `(string tenantId, string pipelineId) → (string Ir, string Generated, string Workspace, string Artifacts)` | 返回四个子目录完整路径 |
| `EnsureDirectories` | `(string tenantId, string pipelineId) → void` | 创建四个子目录（幂等） |
| `AssertWithinWorkspace` | `(string filePath, string tenantId, string pipelineId) → void` | 路径校验，穿越则抛 `InvalidOperationException` |
| `ReadFilesFromDirectory` | `(string directoryPath) → List<GeneratedFile>` | 递归读取目录文件为 `GeneratedFile` 列表 |
| `CreateDeliveryZip` | `(string tenantId, string pipelineId) → string` | 打包 `generated/` → 返回 zip 路径 |
| `DeleteWorkspace` | `(string tenantId, string pipelineId) → void` | 删除整个工作区目录（try-catch，不抛异常） |

#### `AssertWithinWorkspace` 安全设计

```csharp
public static void AssertWithinWorkspace(string filePath, string tenantId, string pipelineId)
{
    var workspaceRoot = GetPipelinePath(tenantId, pipelineId);
    var resolvedWorkspace = Path.GetFullPath(workspaceRoot)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    var resolvedTarget = Path.GetFullPath(filePath);

    if (!resolvedTarget.StartsWith(resolvedWorkspace + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"AI 流水线禁止写入工作区外路径: {filePath}. 允许前缀: {resolvedWorkspace}");
    }
}
```

**关键点**：用 `Path.GetFullPath` 解析 `..`，再做前缀匹配，防止路径穿越。

---

### 3.3 `AIDevelopmentPipelineService` 改动（~30 行）

**development 阶段**（现有代码中负责生成代码的路径）：
```csharp
// Before: 直接拼路径或调用 CodeGenService 默认路径
// After:
var (ir, generated, workspace, artifacts) = StudioWorkspaceHelper.GetPipelineSubPaths(tenantId, pipelineId);
StudioWorkspaceHelper.EnsureDirectories(tenantId, pipelineId);
// 代码生成输出到 `generated/`
```

**delivery 阶段**（用户触发交付）：
```csharp
var zipPath = StudioWorkspaceHelper.CreateDeliveryZip(tenantId, pipelineId);
// 返回下载链接给前端（复用现有下载机制）
```

**沙箱上传**（进入 development 时）：
```csharp
var files = StudioWorkspaceHelper.ReadFilesFromDirectory(generatedDir);
await _sandboxManager.UploadFilesAsync(sandboxId, files);
```

---

### 3.4 `PipelineOrchestratorService` 改动（~10 行）

**放弃流水线**（`AbandonAsync`）— 在已有 `DestroyAssociatedSandboxIfExists` 后追加：
```csharp
// 已有: await DestroyAssociatedSandboxIfExists(pipelineId);
// 新增:
var tenantId = TenantResolver.Resolve();
StudioWorkspaceHelper.DeleteWorkspace(tenantId, pipelineId.ToString());
```

**注意**：`DeleteWorkspace` 内部 try-catch，删除失败不阻塞放弃流程。

**运行时路径校验**（场景 2）：在 development 阶段任何写文件操作前：
```csharp
StudioWorkspaceHelper.AssertWithinWorkspace(targetPath, tenantId, pipelineId);
```

---

### 3.5 `guard-write.mjs` 改动（场景 1，~20 行）

在现有 L1/L2/L3 之后，新增 L4 规则：

```javascript
// ═══════════════════════════════════════════════════════════════
// L4: AI 开发态工作区隔离 — 拦截写入主仓库路径 (exit 2)
// ═══════════════════════════════════════════════════════════════
// 从环境变量或会话元数据读取当前 pipelineId
const pipelineId = process.env.JNPF_PIPELINE_ID || null;

if (pipelineId) {
  // AI 开发任务中：只允许写入 StudioWorkspace 和 .claude/ 目录
  const allowedPatterns = [
    /StudioWorkspace[/\\]/,           // 工作区文件
    /\.claude[/\\]/,                  // 项目配置
    /docs[/\\]/,                      // 文档
    /workspace[/\\]/,                 // 流水线 workspace 目录
  ];

  const isAllowed = allowedPatterns.some(p => p.test(filePath));

  if (!isAllowed) {
    console.error(`BLOCKED: AI 开发态禁止写入主仓库路径: ${filePath}`);
    console.error(`  当前 pipelineId: ${pipelineId}`);
    console.error(`  允许前缀: StudioWorkspace/, .claude/, docs/, workspace/`);
    process.exit(2);
  }
}
```

**上下文传递机制**（文件桥接）：
- `AIDevelopmentPipelineService` 进入 development 阶段时，写入 `.claude/ai-dev-context.json`：
  ```json
  { "pipelineId": "123456", "tenantId": "xxx", "workspacePath": "{SystemPath}/StudioWorkspace/xxx/123456/" }
  ```
- `guard-write.mjs` 在 L4 规则中读取该文件（若存在），获取当前 `pipelineId`
- `PipelineOrchestratorService` 退出/放弃 development 阶段时，删除 `.claude/ai-dev-context.json`
- 文件不存在 → 不激活 AI 白名单规则，回退到默认 Hook 行为（保守安全）

**为什么不用环境变量**：Claude Code hook 运行在独立子进程中，C# 运行时无法直接注入 env var 到 hook 进程。文件桥接是同一文件系统内的可靠共享机制。

---

## 4. 数据流

```
用户创建流水线
  │
  ▼
AIDevelopmentPipelineService.CreateAsync()
  │  调用 StudioWorkspaceHelper.EnsureDirectories()
  │  创建 {SystemPath}/StudioWorkspace/{tenantId}/{pipelineId}/
  │
  ▼
进入 development 阶段
  │  1. AssertWithinWorkspace() — 校验所有输出路径
  │  2. 代码生成 → generated/
  │  3. ReadFilesFromDirectory(generated/) → List<GeneratedFile>
  │  4. SandboxManager.UploadFilesAsync(sandboxId, files)
  │  5. SSE 通知前端 "sandbox_ready"
  │
  ▼
用户触发交付
  │  1. SandboxManager.DestroyAsync(sandboxId)
  │  2. CreateDeliveryZip() → 打包 generated/ → .zip
  │  3. 返回下载 URL
  │
  ▼
用户放弃流水线
  │  1. DestroyAssociatedSandboxIfExists()
  │  2. DeleteWorkspace() — 清理磁盘
```

---

## 5. 错误处理

| 场景 | 策略 |
|------|------|
| `EnsureDirectories` 失败（磁盘满/无权限） | 抛异常，流水线状态 → Failed |
| `AssertWithinWorkspace` 触发 | 抛 `InvalidOperationException`，流水线当前操作失败，不落盘 |
| 沙箱创建超时 | 流水线标记为故障，提示重试（已有逻辑） |
| `DeleteWorkspace` 失败 | 仅 `_logger.LogWarning`，不阻塞放弃流程 |
| `ReadFilesFromDirectory` 目录为空 | 返回空列表，正常继续（development 可能尚未产出文件） |
| `CreateDeliveryZip` 无文件 | 抛 `Oops.Bah("无生成产物可交付")` |
| `guard-write.mjs` 无 `pipelineId` 上下文 | 不激活 AI 规则，回退到默认 Hook 行为（保守安全） |

---

## 6. 与传统代码生成的隔离验证

| 验证项 | 方法 |
|--------|------|
| 传统 VisualDev 代码生成不受影响 | 执行一次代码生成 → 下载 zip，确认路径仍为 `{SystemPath}/CodeGenerate/{fileName}.zip` |
| `CodeGenService` 代码零改动 | git diff 确认 |
| `SandboxManager` 接口零改动 | git diff 确认 |

---

## 7. 文件变更清单

| 文件 | 动作 | 行数 |
|------|------|------|
| `backend/modularity/common/JNPF.Common/Configuration/KeyVariable.cs` | +1 行常量 | +1 |
| `backend/modularity/inteAssistant/JNPF.InteAssistant/StudioWorkspaceHelper.cs` | **新建** | ~120 |
| `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs` | 修改 development/delivery 阶段 | ~30 |
| `backend/modularity/inteAssistant/JNPF.InteAssistant/PipelineOrchestratorService.cs` | 放弃时补 workspace 清理 | ~10 |
| `.claude/hooks/guard-write.mjs` | 新增 L4 AI workspace 白名单 | ~30 |

**总计：~190 行，5 个文件。**

---

## 8. 测试策略

| 层 | 验证内容 | 命令 |
|----|---------|------|
| 编译 | 后端零错误 | `dotnet build` |
| 单元 | `StudioWorkspaceHelper` 路径计算 + 穿越防御 | 手动构造边界用例 |
| 集成 | 创建流水线 → development → 确认 `generated/` 生成 → 沙箱上传 → 交付 zip | 端到端 |
| 回归 | 传统代码生成路径不受影响 | VisualDev 代码生成 → 下载 zip |
| Hook | `guard-write.mjs` 在有/无 `pipelineId` 时的行为 | 手动触发 Write 工具调用 |
