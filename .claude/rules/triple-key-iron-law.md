# 三元组铁律（Triple-Key Iron Law）

> **层级：宪法级，与 Business First Iron Law 并列，凌驾于 R1–R12 架构红线之上。**
> **本铁律是 AI 原生开发「多用户 / 多项目 / 多对话 + 冻结拉起 + 二次开发」存在的根基。**
> 没有 it，所谓"多用户多项目多对话"就是 1:1:1 退化的笑话。
> **主文件（Cursor 镜像）：** `.cursor/rules/triple-key-iron-law.mdc`（`alwaysApply: true`）
> **架构登记：** `architecture-redlines.md` §R12 · `CLAUDE.md` §Architecture Redlines

---

## 核心宣言（刻进骨子里）

**JNPF AI 原生开发的一切数据实体、IR 事件、文件路径、Skill 上下文，MUST 同时携带三元组：**

```
tenantId    — 租户 ID（多租户隔离边界，对应 BASE_AI_PIPELINE.F_TENANT_ID）
projectId   — 项目 ID（业务实体边界，对应 BASE_AI_PIPELINE.F_PROJECT_ID）
pipelineId  — 流水线/对话 ID（会话边界，对应 BASE_AI_PIPELINE.F_ID）
```

**三者 MUST 完整、独立、可分离。**

- **禁止三元组缩写为二元组**（如路径只剩 `tenantId/pipelineId`）。
- **禁止三元组退化为 1:1:1**（如 `projectId == pipelineId` 当作默认，但代码不再支持解耦）。
- **禁止以 `projectId` 当 `pipelineId` 使用**（fallback = 隐藏 1:N bug）。
- **禁止以 `pipelineId` 当 `projectId` 使用**（如 `ResolveProjectAsync` 返回 pipelineId 而非真实 ProjectId）。

**无例外。任何缺失 projectId 的层（DB 索引、IR 投影查询、Studio 路径、SkillContext）= 架构性缺陷，MUST 修复。**

---

## 三元组的语义（业务含义）

| 键 | 业务含义 | 用户视角 | 关系 |
|---|---|---|---|
| `tenantId` | 租户隔离边界 | "我公司的所有数据别人看不到" | 1 tenant → N projects |
| `projectId` | 项目（业务系统）边界 | "我创建的请假系统" | 1 project → N pipelines（迭代/bugfix/enhancement） |
| `pipelineId` | 单次开发会话边界 | "我做的一个开发任务"（可冻结/恢复/fork） | 1 pipeline = 1 个连续对话 + 1 套 IR 事件流 |

### 1:N:N 关系（铁律，不可逆）

```
tenant (A 公司)
 └─ project (请假系统 P1)
     ├─ pipeline-100 (原始需求 → 部署，已 frozen)
     ├─ pipeline-101 (bugfix: 修复审批 bug，从 P1 fork)
     └─ pipeline-102 (enhancement: 加图表，从 P1 fork)
```

- 同 project 下多 pipeline **共享业务实体**（实体表/接口骨架）但 **独立 IR 事件流**（每个 pipeline 自己的事件链）。
- fork 时新 pipeline 继承源 pipeline 的 IR 快照（fragment snapshots），但 fork 后各自演进。
- frozen pipeline 可恢复（resume），不丢失上下文。

---

## 各数据层的不变量（Mandatory）

### Layer 1 — DB Schema

| 表 | 三元组字段 | 唯一索引（强制） |
|---|---|---|
| `BASE_AI_PIPELINE` | `F_TENANT_ID, F_PROJECT_ID, F_ID` | `PK (F_ID)` |
| `ai_projects` | `F_TenantId, F_Id` | `PK (F_Id)` |
| `ai_ir_events` | `F_TenantId, F_ProjectId, F_PIPELINE_ID` | 无（事件追加模型，但每行三元组 MUST 非空） |
| `ai_ir_fragment_snapshots` | `F_TenantId, F_ProjectId, F_PIPELINE_ID` | `UQ_fragment_current (F_ProjectId, F_PIPELINE_ID, F_FragmentId)` |
| `BASE_AI_GENERATED_PROJECT` | `F_TENANT_ID, F_PROJECT_ID, F_PIPELINE_ID` | 同上模式 |

**禁止**：唯一索引只含 `(ProjectId, FragmentId)` — 多 pipeline 写同名 fragment 会撞键。

### Layer 2 — IR 事件存储（IrEventStoreService）

事件写入时三元组 MUST 全部从 `SkillExecutionScope` 透传：

```csharp
var evt = new AiIrEventEntity
{
    ProjectId  = SkillExecutionScope.CurrentScope?.ProjectId  ?? throw,  // NEVER fallback 到 pipelineId
    PipelineId = SkillExecutionScope.CurrentScope?.PipelineId ?? throw,  // NEVER fallback 到 projectId
    TenantId   = SkillExecutionScope.CurrentScope?.TenantId   ?? throw,
    ...
};
```

**禁止**：`PipelineId = scope?.PipelineId ?? projectId`（fallback 用 projectId）。

### Layer 3 — IR 投影引擎（IrProjectionEngine）

所有 fragment 查询 MUST 用三元组定位：

```csharp
// ✅ 正确
var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
    .Where(x => x.TenantId == evt.TenantId
        && x.ProjectId == evt.ProjectId
        && x.PipelineId == evt.PipelineId
        && x.FragmentId == fragmentId
        && !x.DeleteMark)
    .FirstAsync(ct);

// ❌ 违反铁律 — 缺 PipelineId（同 project 多 pipeline 会互相覆盖）
var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
    .Where(x => x.ProjectId == evt.ProjectId
        && x.FragmentId == fragmentId)
    .FirstAsync(ct);
```

### Layer 4 — Studio 文件路径（StudioWorkspaceHelper）

路径公式 MUST 是四层：

```
{SystemPath}/StudioWorkspace/{tenantId}/{projectId}/{pipelineId}/
                                          │          │
                                          │          └─ 会话边界（独立 generated/ir/deliverables）
                                          └─ 项目边界（共享业务实体定义）
```

**禁止**：路径只有 `{tenantId}/{pipelineId}`（缺 projectId 层）。

**向后兼容策略（仅适用于历史数据迁移）**：
- 历史路径 `{tenantId}/{pipelineId}/`（1:1 退化期）→ 通过 `AiPipelineEntity.F_PROJECT_ID == F_ID` 检测
- 检测到自锚定时，路径解析退回老路径（不破坏 pipeline 311 等历史数据）
- 新建 pipeline（`F_PROJECT_ID != F_ID` 或 `WorkMode != greenfield`）MUST 走新路径

### Layer 5 — Skill API 入口（*SkillsApiService.ResolveProjectAsync）

所有 Skill Service API MUST 返回真实 ProjectId（从 `AiPipelineEntity.F_PROJECT_ID` 读取），**禁止**返回 pipelineId：

```csharp
// ✅ 正确
private async Task<(string projectId, string tenantId)> ResolveProjectAsync(long pipelineId)
{
    var pipeline = await _db.Queryable<AiPipelineEntity>()
        .FirstAsync(x => x.Id == pipelineId.ToString());
    return (pipeline.ProjectId, pipeline.TenantId);
}

// ❌ 违反铁律 — 用 pipelineId 当 projectId
return (pipelineId.ToString(), pipeline.TenantId);
```

### Layer 6 — Skill 执行上下文（SkillContext / SkillExecutionScope）

SkillContext 的 `ProjectId` 与 `PipelineId` MUST 来自不同的源，且语义不可混淆：

```csharp
public sealed class SkillContext
{
    public string TenantId   { get; init; }  // 来自 RequestContext.TenantId
    public string ProjectId  { get; init; }  // 来自 AiPipelineEntity.F_PROJECT_ID
    public long   PipelineId { get; init; }  // 来自 URL 路径参数
    ...
}
```

**禁止**：在 Skill 实现里写 `context.ProjectId == context.PipelineId.ToString()` 做任何判断 — 它们是不同的语义实体。

---

## WorkMode 与三元组的关系

`AiPipelineEntity.F_WORK_MODE` 决定三元组的具体形态：

| WorkMode | ProjectId 来源 | PipelineId 来源 | 典型场景 |
|---|---|---|---|
| `greenfield`（默认） | 自锚定（= pipelineId） | 新建 | 首次需求 → 部署 |
| `bugfix` | **继承**自 `F_SOURCE_PIPELINE_ID` 的 projectId | 新建 | 修生产 bug |
| `enhancement` | **继承**自 `F_SOURCE_PIPELINE_ID` 的 projectId | 新建 | 加新功能 |

**greenfield 自锚定 ≠ 1:1 退化**：自锚定是 projectId 字段的初始值约定，**不**意味着其他层（路径/IR 投影）可以省略 projectId。

---

## Fork / Freeze / Resume（必须支持）

### Fork（二次开发）

```
POST /api/studio/pipeline/{sourcePipelineId}/fork
  → 新建 pipeline（新 id, WorkMode=bugfix/enhancement, SourcePipelineId=源 id）
  → 复制源 pipeline 的 IR fragment snapshots 到新 pipeline（ProjectId 继承）
  → 复制源 pipeline 的 generated/ 目录到新 pipeline（如需继承代码）
```

### Freeze（冻结会话）

```
POST /api/studio/pipeline/{pipelineId}/freeze
  → 序列化当前内存状态（对话上下文 + Skill 状态 + 进度）到 F_CHECKPOINT
  → F_FROZEN = 1, F_FROZEN_AT = NOW, F_FROZEN_REASON = "用户离开"
  → 释放沙箱资源（可选）
```

### Resume（恢复会话）

```
POST /api/studio/pipeline/{pipelineId}/resume
  → 反序列化 F_CHECKPOINT 重建内存状态
  → F_FROZEN = 0, F_RESUME_COUNT += 1, F_LAST_RESUMED_AT = NOW
  → 重新分配沙箱（如需要）
```

---

## 多用户隔离（必须支持）

`BASE_AI_PIPELINE.F_CREATOR_USER_ID` MUST 在创建时写入当前用户 ID。

- 同租户下不同用户**不能互看**对方的 pipeline（除非显式授权）。
- Skill API 入口 MUST 校验 `pipeline.F_CREATOR_USER_ID == 当前用户 ID`（superadmin 例外）。

---

## Agent 强制行为

1. **新增任何数据实体（表/DTO/事件）**：三元组字段 MUST 同时存在，且**字段类型一致**（`string` for tenantId/projectId，`long` or `string` for pipelineId）。
2. **写 IR 投影查询**：WHERE 条件 MUST 含三元组 + FragmentId，缺一即违规。
3. **写文件路径相关代码**：MUST 通过 `StudioWorkspaceHelper.GetPipelinePath(tenantId, projectId, pipelineId)`，禁止直接拼字符串。
4. **写 Skill API 入口**：`ResolveProjectAsync` MUST 返回真实 ProjectId。
5. **写 Skill 实现**：MUST 通过 `SkillContext.ProjectId / PipelineId / TenantId` 三个独立字段访问，禁止混用。
6. **创建新 pipeline**：MUST 写入 `F_CREATOR_USER_ID`。
7. **fork / freeze / resume**：MUST 走上述标准 API，禁止绕过三元组直接改 DB。

---

## 违反后果（生产事故级）

| 违反 | 后果 |
|---|---|
| IR 投影缺 PipelineId | bugfix 时新 pipeline 写 fragment 撞唯一键 / 覆盖源 pipeline 数据 |
| Studio 路径缺 projectId 层 | fork 出的 pipeline 看不到源 pipeline 的 generated/，无法继承代码 |
| ResolveProjectAsync 返 pipelineId | Skill 写错 IR ProjectId，三元组血缘断裂 |
| SkillContext.ProjectId == PipelineId 假设 | bugfix 模式下 Skill 写错 fragment，覆盖源 pipeline |
| 缺 F_CREATOR_USER_ID | 同租户用户互看 pipeline = 越权数据泄露 |
| 缺 fork API | 用户无法做二次开发，"多对话"功能形同虚设 |
| 缺 freeze/resume API | 用户离开浏览器后无法恢复，"对话冻结"功能形同虚设 |

---

## 验收命令（铁律配套）

```powershell
# ① DB 三元组健康检查（应有 N>0 条 bugfix/enhancement pipeline）
node scripts/diagnose-triple-key.mjs

# ② 同 project 多 pipeline IR 不互相覆盖（fork 后 IR 互不影响）
node scripts/phase5-fullchain-e2e.mjs --from-step fork --pipeline-id <new>

# ③ frozen pipeline 能恢复（resume 后状态一致）
node scripts/test-freeze-resume.mjs
```

---

## 与现有铁律的关系

| 铁律 | 职责 |
|---|---|
| **Business First Iron Law** | 定 **做什么** — 业务功能 + 客户操作 |
| **本铁律（Triple-Key）** | 定 **数据骨架** — 多用户多项目多对话的存在前提 |
| 架构红线 R1–R12 | 定 **怎么做不出事** |
| Supreme Iron Law / auto-test-fix-loop | 定 **怎么证明做过** |

**四者同时满足才算交付。** 缺三元组 = "多用户多项目多对话"是 PPT 口号，不是真功能。

---

## 禁止清单（NEVER）

- ❌ 三元组缩写为二元组（任何层）
- ❌ `PipelineId ?? projectId` 类 fallback
- ❌ `ResolveProjectAsync` 返回 pipelineId 当 projectId
- ❌ 唯一索引不含 PipelineId
- ❌ 路径层跳过 projectId（除非显式向后兼容检测）
- ❌ 假设 `projectId == pipelineId`（任何业务代码）
- ❌ 创建 pipeline 不写 F_CREATOR_USER_ID
- ❌ 绕过 fork API 手改 DB 创建二次开发 pipeline
- ❌ "1:1 退化能跑就行"作为不修 projectId 的借口
- ❌ 任何"projectId 字段存在但实际不读不写"的僵尸字段
