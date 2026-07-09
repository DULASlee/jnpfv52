# sa-service 三元组闭合 (R12) 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除 sa-service 全部三元组 `(tenantId, projectId, pipelineId)` 的 `?? projectId` fallback，让 R12 宪法级铁律在 sa-service 代码层闭合——bugfix/enhancement 模式下 pipeline≠project 时不再写错 `pipeline_id`。

**Architecture:** `SARequest`/`ValidationLogRecord` 的 `pipelineId` 改必填；删除 `SAOrchestrator.resolveContext` + `SqlServerSADatabase.bindTriple/logValidation` + `server.ts` 所有 fallback；`server.ts` 的 `/api/sa/run`、`/sa/run-step`、`runSingleStep` 补 pipelineId 透传。**类型驱动**：改必填后用 `tsc --noEmit` 定位所有调用点，编译器驱动修复，不靠记忆。

**Tech Stack:** TypeScript (strict), vitest (globals), mssql, express

---

## Scope（质量优先 · 显式边界）

**本计划只覆盖 A 组（三元组闭合，7 步合并为 1 个原子 Task）。**

未覆盖（各自另出 plan，不塞占位）：
- **C 组 Validator 注入**：探查发现 schema 设计债（Validator 期望 `dfd_levels`/`flows{from,to}` vs Agent 产出 `dfdLevels`/`dataFlows{name}`，字段名+结构都不匹配；现有测试全用 MockValidator 从未暴露）。需单独 brainstorm 对齐方向（改 Validator / 写 adapter / 改 Agent 产出）后另出 plan。
- B/D/G/I/F/E/H 组：内存泄漏 / runSingleStep / logValidation 字段 / Semaphore / JSON 解析 / 连接池 / fail-fast / 死代码 / 注释——后续各自 plan。

---

## File Structure

| 文件 | 职责 | 本计划改动 |
|---|---|---|
| `src/orchestrator/orchestrator-types.ts` | 类型定义 | `SARequest.pipelineId` + `ValidationLogRecord.pipelineId` 改必填 |
| `src/orchestrator/SAOrchestrator.ts` | 编排 | 删 `resolveContext:510` fallback；`runSingleStep:93` ctx 补 pipelineId + 参数 |
| `src/orchestrator/SqlServerSADatabase.ts` | SQL Server 持久化 | 删 `bindTriple:34` + `logValidation:175` 两处 fallback |
| `src/server.ts` | HTTP 入口 | `/api/sa/run:342` + `/sa/run-step:202` 补 pipelineId 透传 |
| `__tests__/SAOrchestrator.test.ts` | 集成测试 | 4 处 `SARequest`（L130/190/287/316）补 pipelineId |
| `__tests__/routes.integration.test.ts` | 路由测试 | 检查并补 SARequest pipelineId（tsc 驱动） |

---

## Task 1: 三元组闭合（唯一 Task，原子提交单元）

**Files:** 见上 File Structure

- [ ] **Step 1: 改 `SARequest.pipelineId` 必填**

`src/orchestrator/orchestrator-types.ts:15-29`，把 `pipelineId?: number` 改为 `pipelineId: number`，更新注释（删除"缺省时与 projectId 相同（历史兼容）"——历史兼容正是要消除的 fallback）：

```typescript
export interface SARequest {
  tenantId: string;
  projectId: number;
  /** Studio 流水线实例 ID（三元组，必填，R12 宪法级） */
  pipelineId: number;
  requirementId: number;
  requirementText: string;
  skeletonBusinessEvents?: SkeletonBusinessEvent[];
  eventId?: number;
  eventDescription?: string;
  assetLevel?: 'PROJECT' | 'EVENT' | 'PROCESS';
  userId: string;
  runId?: string;
}
```

- [ ] **Step 2: 改 `ValidationLogRecord.pipelineId` 必填**

同文件 `:254-267`，`pipelineId?: number` → `pipelineId: number`。

- [ ] **Step 3: tsc 定位所有破坏点（类型驱动）**

Run: `cd D:\JNPF-v52\sa-service && npx tsc --noEmit`
Expected: FAIL —— 列出所有缺 pipelineId 的 SARequest 构造点（预期：server.ts ×2、SAOrchestrator.ts runSingleStep ctx、测试 ×4+）。**记录每个错误位置，驱动后续步骤。**

- [ ] **Step 4: 删 `SAOrchestrator.resolveContext` fallback**

`src/orchestrator/SAOrchestrator.ts:510`：

```typescript
// 修改前
pipelineId: req.pipelineId ?? req.projectId,
// 修改后（req.pipelineId 现在必填，直接读）
pipelineId: req.pipelineId,
```

- [ ] **Step 5: 删 `SqlServerSADatabase` 两处 fallback**

`src/orchestrator/SqlServerSADatabase.ts:33-39`（bindTriple）：

```typescript
private bindTriple(req: sql.Request, ctx: SAContext): sql.Request {
  return req
    .input('tenant_id', sql.NVarChar(50), ctx.tenantId)
    .input('project_id', sql.BigInt, ctx.projectId)
    .input('pipeline_id', sql.BigInt, ctx.pipelineId);  // ctx.pipelineId 已是 number（SAContext 必填）
}
```

同文件 `logValidation:175`：

```typescript
// 修改前
const pipelineId = record.pipelineId ?? record.projectId;
// 修改后
const pipelineId = record.pipelineId;  // 必填，无 fallback
```

- [ ] **Step 6: `server.ts` /api/sa/run 补 pipelineId 透传**

`src/server.ts:295-348`。该端点从 `req.body` 解构未取 pipelineId，SARequest 构造（:342）也未传。补：

```typescript
// :297 解构补 pipelineId
const {
  tenantId, projectId, pipelineId, requirementId, requirementText,
  userId, industry, sseSessionId,
} = req.body;

// :307 入参校验补 pipelineId
if (!requirementText || !tenantId || !projectId || !pipelineId) {
  return res.status(400).json({
    error: '缺少必要参数: tenantId, projectId, pipelineId, requirementText',
  });
}

// :342 SARequest 构造补
const saRequest: SARequest = {
  tenantId,
  projectId,
  pipelineId: Number(pipelineId),  // 新增
  requirementId: requirementId || 0,
  requirementText,
  userId: userId || 'anonymous',
};
```

- [ ] **Step 7: `server.ts` /sa/run-step + runSingleStep 补 pipelineId**

`src/server.ts:202-282`（/sa/run-step）。解构补 pipelineId，传给 runSingleStep：

```typescript
// :205 解构补 pipelineId
const {
  tenantId, projectId, pipelineId, eventId, agentName, irStepName,
  requirementText, skeleton, previousSteps,
} = req.body;

// :210 校验补 pipelineId
if (!tenantId || !projectId || !pipelineId || !eventId || !agentName) {
  return res.status(400).json({ error: '缺少 tenantId/projectId/pipelineId/eventId/agentName' });
}

// :249 runSingleStep 调用补 pipelineId
const output = await orchestrator.runSingleStep({
  tenantId,
  projectId: String(projectId),
  pipelineId: Number(pipelineId),  // 新增
  eventId,
  agentName,
  // ...其余不变
});
```

`src/orchestrator/SAOrchestrator.ts:81-107`（runSingleStep）。签名补 pipelineId，ctx 补字段：

```typescript
async runSingleStep(params: {
  tenantId: string;
  projectId: string;
  pipelineId: number;  // 新增
  eventId: string;
  agentName: string;
  irStepName: string;
  requirementText: string;
  skeleton?: any;
  previousSteps?: Record<string, any>;
  runId?: string;
}): Promise<any> {
  const start = Date.now();
  const ctx: SAContext = {
    tenantId: params.tenantId,
    projectId: Number(params.projectId) || 0,
    pipelineId: params.pipelineId,  // 新增（SAContext.pipelineId 已必填）
    requirementId: 0,
    // ...其余不变
  };
```

- [ ] **Step 8: 修测试 4 处 SARequest**

`__tests__/SAOrchestrator.test.ts` L130、L190、L287、L316 的 `SARequest` 字面量各补 `pipelineId`：

```typescript
const req: SARequest = {
  tenantId: 't1',
  projectId: 1,
  pipelineId: 1,  // 新增（三元组，测试场景 pipelineId=projectId 合法）
  requirementId: 1,
  requirementText: '...',
  userId: '...',
};
```

- [ ] **Step 9: 检查 routes.integration.test.ts**

Run: `cd D:\JNPF-v52\sa-service && npx tsc --noEmit`
若该文件有 SARequest 构造或 run-async 调用缺 pipelineId → tsc 报错，按报错补。

- [ ] **Step 10: tsc 0 error（硬质量门）**

Run: `cd D:\JNPF-v52\sa-service && npx tsc --noEmit`
Expected: 0 errors。**非 0 = 本 Task 未完成，不许进 commit。**

- [ ] **Step 11: vitest 全绿（硬质量门）**

Run: `cd D:\JNPF-v52\sa-service && npx vitest run`
Expected: 所有现有测试 PASS（不应有回归）。**任一红 = 停下修到绿。**

- [ ] **Step 12: commit（待用户授权）**

```bash
git add sa-service/src/orchestrator/orchestrator-types.ts \
        sa-service/src/orchestrator/SAOrchestrator.ts \
        sa-service/src/orchestrator/SqlServerSADatabase.ts \
        sa-service/src/server.ts \
        sa-service/__tests__/
git commit -m "fix(sa-service): 三元组 pipelineId 必填，删除全部 ?? projectId fallback (R12)

- SARequest/ValidationLogRecord.pipelineId 改必填
- SAOrchestrator.resolveContext 删 fallback
- SqlServerSADatabase.bindTriple/logValidation 删 fallback
- server.ts /api/sa/run + /sa/run-step + runSingleStep 补 pipelineId 透传
- 测试 4 处 SARequest 补 pipelineId

闭合 R12 宪法级铁律：bugfix/enhancement 模式 pipeline≠project 时不再写错 pipeline_id。"
```

---

## Self-Review

**Spec coverage:** A 组 7 步（A1-A7）全部映射到 Task 1 的 Step 1-7。✓
**Placeholder scan:** 无 TBD/TODO；每步有具体代码或命令。✓
**Type consistency:** `pipelineId: number`（非 `?`）在 SARequest/SAContext/ValidationLogRecord/runSingleStep params 统一。✓
**Scope:** 单一原子 Task，产出可编译可测试的提交（tsc 0 + vitest 全绿）。✓

---

## 执行方式

Inline 执行（用户已授权"立即推进"）。下一动作：Step 1。
