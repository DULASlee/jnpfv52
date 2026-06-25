议题4开始。清言牵头，其他专家补充。

---

## 议题4：接口契约（清言牵头，30分钟）

### 清言预案回顾（4个P0 + 1个P1）

| ID   | 问题                                                | 优先级 | 核心方案                                         |
| ---- | --------------------------------------------------- | ------ | ------------------------------------------------ |
| B-04 | §4.7增量修改流程无任何接口支撑（7个接口全缺失）     | P0     | 补齐7个增量修改接口的完整契约                    |
| B-05 | 流水线管理API严重缺失（12个端点缺失）               | P0     | 补齐流水线CRUD + 确认/否决/取消/重置接口         |
| B-06 | SSE事件Schema缺失（前端无法开发）                   | P0     | 定义完整的事件类型枚举 + JSON Schema             |
| I-15 | 沙箱管理API不对称（只有查询，无创建/销毁/文件上传） | P1     | 补齐CREATE/DELETE/UploadFiles/ExecuteCommand接口 |

---

## 清言详细预案（可编码级接口契约）

### B-04：增量修改7个接口（§4.7支撑）

**接口清单（锁定）：**

| #    | 端点                                              | 方法 | 用途                                                 | 前置议题依赖                |
| ---- | ------------------------------------------------- | ---- | ---------------------------------------------------- | --------------------------- |
| 1    | `/api/pipeline/{id}/incremental-mode`             | GET  | 查询当前流水线是否支持增量修改（返回可编辑节点列表） | 议题2-B-07 NodeType映射     |
| 2    | `/api/pipeline/{id}/preview-rollback`             | POST | 预览回退/修改影响范围（议题1-I-04已锁定）            | 议题3-B-03双模式            |
| 3    | `/api/pipeline/{id}/surgical-edit`                | POST | 分治编辑：提交局部修改（议题3-B-03）                 | 议题2-B-08 Agent契约        |
| 4    | `/api/pipeline/{id}/rollback`                     | POST | 全量回退到指定阶段（议题1-B-02已锁定）               | 议题3-I-09版本树            |
| 5    | `/api/pipeline/{id}/ir-version`                   | GET  | 查询IR版本历史（议题3-I-09已锁定）                   | 议题3-I-10清理策略          |
| 6    | `/api/pipeline/{id}/ir-version/{version}`         | GET  | 获取指定版本IR快照                                   | 议题3-I-09 DDL              |
| 7    | `/api/pipeline/{id}/ir-version/{version}/restore` | POST | 恢复到指定版本                                       | 议题3-I-09 F_PARENT_VERSION |

**请求/响应契约（以surgical-edit为例）：**

```yaml
POST /api/pipeline/{id}/surgical-edit
Headers:
  Authorization: Bearer <jwt>
  Content-Type: application/json
Body:
  {
    "targetNodes": ["node-uuid-1", "node-uuid-2"],  // 议题2-B-07 ownedNodeTypes校验
    "changeDescription": "修改按钮文字为'提交'",
    "mode": "surgical",  // surgical | full_rollback
    "expectedStage": "stage3"  // 全量回退时指定
  }
Response:
  {
    "code": 200,
    "data": {
      "editId": "uuid",
      "affectedNodes": ["node-uuid-1", "node-uuid-3"],  // 实际受影响（含级联）
      "impactSummary": {
        "agentsToRerun": ["UIDesignAgent"],
        "stagesToRerun": ["stage3", "stage4"],
        "estimatedTime": "45s"
      },
      "requiresConfirmation": true,
      "previewUrl": "/api/pipeline/{id}/preview/{editId}"
    }
  }
```

**谁来改**：后端工程师（7个端点）+ 前端工程师（API层pipeline.ts）
**什么时候改**：P0，**今天定全部契约，明天开始编码**

---

### B-05：流水线管理12个端点（§5补齐）

**接口清单（锁定，与议题1已裁决接口合并）：**

| #    | 端点                                                       | 方法   | 用途                               | 议题来源 |
| ---- | ---------------------------------------------------------- | ------ | ---------------------------------- | -------- |
| 1    | `/api/founder/ai/pipeline`                                 | POST   | 创建流水线                         | 已有     |
| 2    | `/api/founder/ai/pipeline/{id}`                            | GET    | 查询流水线状态                     | 已有     |
| 3    | `/api/founder/ai/pipeline/{id}`                            | DELETE | 删除（软删）                       | 新增     |
| 4    | `/api/founder/ai/pipeline/{id}/start`                      | POST   | 启动流水线                         | 已有     |
| 5    | `/api/founder/ai/pipeline/{id}/stages/{stageName}/confirm` | POST   | 通用确认/否决（议题1已锁定）       | 议题1    |
| 6    | `/api/founder/ai/pipeline/{id}/resume`                     | POST   | stale恢复（议题1已锁定）           | 议题1    |
| 7    | `/api/founder/ai/pipeline/stale`                           | GET    | 管理员查看stale列表（议题1已锁定） | 议题1    |
| 8    | `/api/founder/ai/pipeline/{id}/abandon`                    | POST   | abandoned终止（议题1已锁定）       | 议题1    |
| 9    | `/api/founder/ai/pipeline/{id}/events`                     | GET    | SSE流（B-06，见下方）              | B-06     |
| 10   | `/api/founder/ai/pipeline/{id}/approval-summary`           | GET    | 审批阈值vs实际（议题1-I-03已锁定） | 议题1    |
| 11   | `/api/founder/ai/pipeline/{id}/partial-artifacts`          | GET    | 部分成功产物下载（议题2-I-07）     | 议题2    |
| 12   | `/api/founder/ai/pipeline/{id}/retry`                      | POST   | 重试当前阶段（blocked后人工重置）  | 新增     |

**统一分页规范（S-02建议）：**
- 采用JNPF V5.2风格：`{ page: 1, size: 20, sort: 'F_CREATE_TIME desc' }`
- 响应：`{ code: 200, data: { list: [], pagination: { page: 1, size: 20, total: 100 } } }`

**谁来改**：后端工程师（DynamicApiController自动生成）+ 前端工程师（pipeline.ts封装）
**什么时候改**：P0，**今天定契约，明天编码**

---

### B-06：SSE事件Schema（前端开发阻塞）

**SSE端点（锁定）：**

```yaml
GET /api/founder/ai/pipeline/{id}/events
Headers:
  Authorization: Bearer <jwt>
  Accept: text/event-stream
```

**事件类型枚举（锁定）：**

| 事件名                    | 触发时机                     | payload字段                                                  |
| ------------------------- | ---------------------------- | ------------------------------------------------------------ |
| `pipeline.created`        | 流水线创建                   | `{ pipelineId, stage, timestamp }`                           |
| `stage.started`           | 阶段开始                     | `{ stage, agent, timestamp }`                                |
| `stage.progress`          | 阶段进度（Agent checkpoint） | `{ stage, progress: 0-100, thought: string, agent: string }` |
| `stage.completed`         | 阶段完成                     | `{ stage, artifactUrl, timestamp }`                          |
| `stage.blocked`           | 阶段阻塞                     | `{ stage, reason, failureCount, retryable: boolean }`        |
| `stage.stale`             | 阶段超时                     | `{ stage, staleSince, canResumeTo }`                         |
| `stage.approval.required` | 需要人工确认                 | `{ stage, approverRole: 'expert'|'developer'|'admin'|'founder', deadline }` |
| `stage.approval.rejected` | 审批被否决                   | `{ stage, rejectedBy, reason, nextAction: 'modify'|'retry'|'escalate' }` |
| `stage.surgical.edit`     | 分治编辑进度                 | `{ editId, affectedNodes, currentStep: 1-6 }`                |
| `system.alert`            | 系统告警                     | `{ type: 'error'|'warning'|'info', message, actionable: boolean }` |

**标准payload结构（锁定）：**

```json
{
  "eventId": "uuid",
  "pipelineId": "uuid",
  "tenantId": "tenant_001",
  "timestamp": "2026-06-15T22:58:00Z",
  "eventType": "stage.progress",
  "payload": {
    "stage": "stage3",
    "progress": 67,
    "thought": "正在设计数据库表结构...",
    "agent": "DatabaseAgent",
    "estimatedRemainingMs": 15000
  }
}
```

**谁来改**：后端工程师（SignalR Hub扩展SSE支持）+ 前端工程师（EventSource消费）
**什么时候改**：P0，**今天定Schema，前端可并行开发**

---

### I-15：沙箱管理API补齐（P1）

**接口清单（与已有SandboxService扩展对齐）：**

| #    | 端点                        | 方法   | 用途                           | 状态                    |
| ---- | --------------------------- | ------ | ------------------------------ | ----------------------- |
| 1    | `/api/sandbox`              | POST   | 创建沙箱                       | 已有ISandboxManager扩展 |
| 2    | `/api/sandbox/{id}`         | DELETE | 销毁沙箱                       | 已有ISandboxManager扩展 |
| 3    | `/api/sandbox/{id}/files`   | POST   | 上传文件（docker cp）          | 已有ISandboxManager扩展 |
| 4    | `/api/sandbox/{id}/execute` | POST   | 执行命令（docker exec）        | 已有ISandboxManager扩展 |
| 5    | `/api/sandbox/{id}/script`  | POST   | 执行脚本（docker exec + 脚本） | 已有ISandboxManager扩展 |
| 6    | `/api/sandbox/{id}/info`    | GET    | 获取沙箱信息                   | 已有ISandboxManager扩展 |

**请求/响应契约（以创建为例）：**

```yaml
POST /api/sandbox
Body:
  {
    "tenantId": "tenant_001",
    "name": "ai-generated-进销存",
    "cpuCount": 1,
    "memoryMb": 1024,
    "timeoutSeconds": 300
  }
Response:
  {
    "code": 200,
    "data": {
      "sandboxId": "uuid",
      "containerId": "abc123...",
      "status": "pending",  // pending → running → ready
      "url": "http://host:5000",
      "apiUrl": "http://host:5000/api",
      "frontendUrl": "http://host:5000",
      "dbName": "JNPF_Sandbox_tenant_001",
      "createdAt": "2026-06-15T22:58:00Z"
    }
  }
```

**谁来改**：后端工程师（SandboxService已有扩展接口，只需补DynamicApiController）
**什么时候改**：P1，**阶段五并行**。但接口契约今天定（影响前端SandboxMonitor.vue）

---

## 清言的跨议题一致性检查

| 本议题接口                             | 依赖议题   | 检查点                  |
| -------------------------------------- | ---------- | ----------------------- |
| `/api/pipeline/{id}/surgical-edit`     | 议题3-B-03 | 双模式判定阈值30%且50个 |
| `/api/pipeline/{id}/preview-rollback`  | 议题1-I-04 | 影响范围计算            |
| `/api/pipeline/{id}/events`            | 议题1-I-05 | SignalR通知格式兼容     |
| `/api/pipeline/{id}/partial-artifacts` | 议题2-I-07 | partial_success状态     |
| `/api/sandbox/{id}/execute`            | 议题3-I-11 | admin密码注入时机       |

---

## 议题4裁决辅助单（清言版）

| ID   | 优先级 | 核心方案                             | 责任人    | 截止时间                 | 关键约束                    |
| ---- | ------ | ------------------------------------ | --------- | ------------------------ | --------------------------- |
| B-04 | P0     | 7个增量修改接口，surgical-edit为关键 | 后端+前端 | 今天定契约，明天编码     | 议题2-B-07/B-08，议题3-B-03 |
| B-05 | P0     | 12个流水线端点，统一JNPF分页风格     | 后端+前端 | 今天定契约，明天编码     | 议题1已锁定6个接口          |
| B-06 | P0     | SSE事件10类型，标准payload结构       | 后端+前端 | 今天定Schema，前端可并行 | SignalR Hub扩展             |
| I-15 | P1     | 6个沙箱端点，已有ISandboxManager扩展 | 后端      | 阶段五并行，今天定契约   | 议题3-I-11 admin注入        |

### 必须今天锁定的契约

| #    | 契约项             | 锁定内容                                    |
| ---- | ------------------ | ------------------------------------------- |
| 1    | 增量修改接口7个    | 端点路径+请求体+响应体（surgical-edit详细） |
| 2    | 流水线管理接口12个 | 端点清单+统一分页规范                       |
| 3    | SSE事件Schema      | 10个事件类型+标准payload结构                |
| 4    | 沙箱管理接口6个    | 端点路径+创建/销毁/文件上传/命令执行契约    |

---

清言，作为牵头人，请确认上述整合方案是否可接受？还是有调整？