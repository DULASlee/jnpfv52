 议题3开始。玛维思牵头，其他专家补充。

---

## 议题3：增量修改与数据流（玛维思牵头，30分钟）

### 玛维思预案回顾（6个P1问题）

| ID   | 问题                                 | 优先级 | 核心方案                                                 |
| ---- | ------------------------------------ | ------ | -------------------------------------------------------- |
| B-03 | 全量替换IR vs"增量修改"语义矛盾      | P0     | 支持分治编辑（局部修改）+ 全量替换两种模式               |
| I-09 | BASE_IR_VERSION表缺失关键字段        | P1     | 补充F_TRIGGERED_BY、F_CHANGE_SUMMARY、F_PARENT_VERSION等 |
| I-10 | 版本清理策略存在竞争条件             | P1     | 明确定义物理删除vs标记删除 + 乐观锁控制                  |
| I-11 | admin/admin123自动创建的数据流没说清 | P1     | 补充分配租户、密码策略、权限约束的子流程                 |
| I-12 | blocked状态的数据落点三处不一致      | P0     | 统一到BASE_AI_PIPELINE表，增加F_FAILURE_COUNTS JSON列    |
| I-13 | stale状态没有触发数据流（幽灵流程）  | P1     | 定义StaleMonitorService定时任务 + 触发通知               |

---

## 玛维思详细预案（数据层修正方案）

### B-03：全量替换 vs 增量修改语义矛盾

**怎么改**：
- **双模式架构**：
  - **模式A：分治编辑（局部修改）**：用户选择具体节点（组件/字段/页面）→ 路由到对应Agent → 输出IREditPatch → applyPatch → validate → 编译 → 部署
  - **模式B：全量替换（大修改）**：回退到stage N → 重新执行目标阶段及后续全部Agent → 编译 → 部署

- **用户选择器UI**：
  ```
  用户点击"修改"
    ├─ 小修改（局部）→ 进入分治编辑模式
    │   └─ 选择节点 → 预览影响范围 → 确认 → 执行
    └─ 大修改（全量）→ 进入全量替换模式
        └─ 选择回退阶段 → 预览影响范围 → 确认 → 执行
  ```

- **分治编辑六步流程**（§4.7b已有，需细化）：
  1. `buildNodeSummaryList`：列出当前IR所有可编辑节点
  2. `routeToAgent`：根据节点类型路由到对应Agent（B-07的视图注册表）
  3. `extractSlice`：提取节点上下文（父节点、兄弟节点、引用关系）
  4. `Agent输出IREditPatch`：Agent生成JSON Patch格式编辑操作
  5. `applyPatch`：只允许修改scope.writableNodeIds范围内节点（B-07-R2边界校验）
  6. `validateAfterPatch`：cleanSchema → validateIR → vue-tsc

**谁来改**：首席架构师（流程定义）+ 后端工程师（分治编辑引擎）+ 前端工程师（节点选择器UI）
**什么时候改**：B-03是P0，但**分治编辑可拆分为两阶段**：
- 阶段五第1周：先做全量替换（已有代码，只需修文档语义）
- 阶段五第3-4周：分治编辑（新功能，不影响核心闭环）

---

### I-09：BASE_IR_VERSION表缺失关键字段

**怎么改**：
- **当前DDL**（§4.7.5）：
  ```sql
  CREATE TABLE BASE_IR_VERSION (
    F_ID, F_TENANT_ID, F_PIPELINE_ID, F_STAGE, F_VERSION,
    F_IR_CONTENT, F_STATUS, F_CREATED_AT
  );
  ```

- **补充字段**：
  | 字段               | 类型          | 用途                                                |
  | ------------------ | ------------- | --------------------------------------------------- |
  | `F_TRIGGERED_BY`   | NVARCHAR(50)  | 谁触发了这次版本（userId / agentName / 'system'）   |
  | `F_CHANGE_SUMMARY` | NVARCHAR(500) | 变更摘要（用户填写或AI自动生成）                    |
  | `F_PARENT_VERSION` | INT           | 父版本号（构建版本树）                              |
  | `F_DIFF`           | NVARCHAR(MAX) | 与上一版本的差异（JSON Patch格式）                  |
  | `F_ACTION_TYPE`    | NVARCHAR(20)  | 'full_rollback' / 'surgical_edit' / 'auto_snapshot' |
  | `F_AFFECTED_NODES` | NVARCHAR(MAX) | JSON数组，受影响节点ID列表（用于分治编辑）          |

- **版本树结构**：
  ```
  v1 (active)
    └─ v2 (superseded) ← 全量回退到stage3
         └─ v3 (active) ← 分治编辑修改按钮文字
              └─ v4 (superseded) ← 全量回退到stage2
  ```

**谁来改**：后端工程师（DDL + Entity类）
**什么时候改**：P1，阶段五并行。但**DDL今天定**，影响接口契约。

---

### I-10：版本清理策略竞争条件

**怎么改**：
- **清理规则**：
  - 每阶段每流水线最多保留10个版本
  - 超量时：**标记删除**（非物理删除），`F_STATUS = 'archived'`
  - 物理删除：由定时任务每周执行，只删除`created_at < 30天`的archived版本

- **并发控制**：
  - 新增版本时：先INSERT新版本 → 再UPDATE旧版本为superseded → 最后检查数量
  - 数量超限时：SELECT最旧的非active版本 → UPDATE为archived
  - 乐观锁：`F_VERSION`字段即版本号，利用SQL Server的自动递增或应用层控制

- **竞争条件处理**：
  ```
  线程A：创建v11
  线程B：同时创建v11（另一个用户的操作）
  → 应用层F_VERSION唯一约束冲突 → 第二个请求失败 → 客户端重试
  ```

**谁来改**：后端工程师（清理逻辑 + 定时任务）
**什么时候改**：P1，阶段五并行。

---

### I-11：admin/admin123自动创建的数据流

**怎么改**：
- **创建时序**（沙箱部署成功后）：
  1. SandboxDeploymentService检测到沙箱健康
  2. 调用`SeedDataService.Initialize(tenantId, sandboxDbConnection)`
  3. SeedDataService执行：
     - CREATE USER 'admin'（或复用BASE_USER表结构）
     - 密码生成策略：**不是硬编码admin123**
       - 方案A：随机密码 + 显示在UI上（用户首次登录后强制修改）
       - 方案B：统一密码 + 首次登录强制修改（当前文档的admin123是占位符）
     - 分配角色：超级管理员（该沙箱内）
     - 分配权限：全部菜单 + 全部按钮

- **密码策略**：
  - 最小8位，包含大小写+数字
  - 首次登录强制修改
  - 30天过期（可选）

- **数据流图**：
  ```
  沙箱健康检查通过
    → SeedDataService.Initialize
      → 创建admin用户
      → 分配角色权限
      → 写入BASE_SANDBOX表（F_ADMIN_USERNAME, F_ADMIN_PASSWORD_HASH）
      → 返回给前端（sandboxUrl + adminUsername + adminPassword）
  ```

**谁来改**：后端工程师（SeedDataService + 密码策略）
**什么时候改**：P1，阶段五并行。但**密码策略今天定**（安全相关）。

---

### I-12：blocked状态数据落点三处不一致

**怎么改**：
- **当前问题**：
  - 落点1：SignalR实时推送（内存，不落库）
  - 落点2：BASE_AI_PIPELINE_MESSAGE（F_ROLE='system'告警消息）
  - 落点3：BASE_AI_CALL_LOG（F_ERROR字段）

- **统一方案**：
  - **唯一真源**：BASE_AI_PIPELINE表增加`F_FAILURE_COUNTS JSON`列
  - **JSON结构**：
    ```json
    {
      "llm_timeout": {"count": 2, "lastAt": "2026-06-15T10:00:00Z", "details": [...]},
      "compile_failure": {"count": 0, "lastAt": null, "details": []},
      "sandbox_failure": {"count": 1, "lastAt": "2026-06-15T09:30:00Z", "details": [...]},
      "validation_failure": {"count": 0, "lastAt": null, "details": []}
    }
    ```

  - **SignalR推送**：从BASE_AI_PIPELINE实时读取（不额外存储）
  - **BASE_AI_PIPELINE_MESSAGE**：保留，但只存"人类可读"的告警摘要（不是原始计数）
  - **BASE_AI_CALL_LOG**：只存AI调用失败，不存流水线状态失败（职责分离）

**谁来改**：后端工程师（BASE_AI_PIPELINE表扩展 + 读写逻辑重构）
**什么时候改**：P0，今天定DDL，明天改代码。

---

### I-13：stale状态没有触发数据流（幽灵流程）

**怎么改**：
- **StaleMonitorService**（Quartz.NET定时任务）：
  - 执行频率：每1小时
  - 扫描条件：
    ```sql
    SELECT * FROM BASE_AI_PIPELINE 
    WHERE F_STAGE_STATUS = 'in_progress' 
    AND F_CURRENT_STAGE IN ('stage1', 'stage2', 'stage3', 'stage4', 'stage5')
    AND DATEDIFF(hour, F_MODIFY_TIME, GETDATE()) > 
      CASE F_CURRENT_STAGE
        WHEN 'stage1' THEN 168   -- 7天
        WHEN 'stage2' THEN 168   -- 7天
        WHEN 'stage3' THEN 72    -- 3天
        WHEN 'stage4' THEN 168   -- 7天
        WHEN 'stage5' THEN 336   -- 14天
      END
    ```

  - 扫描到stale → 更新`F_STAGE_STATUS = 'stale'` → SignalR推送 → 写入BASE_AI_PIPELINE_MESSAGE

- **接口契约**：
  - `GET /api/pipeline/stale`：管理员查看stale列表（议题1已锁定）
  - `POST /api/pipeline/{id}/resume`：恢复stale流水线（议题1已锁定）

**谁来改**：后端工程师（Quartz任务 + 扫描逻辑）
**什么时候改**：P0，与B-02同步（议题1已裁决）。

---

## 玛维思的跨议题提醒

| 议题                    | 议题3影响                                      |
| ----------------------- | ---------------------------------------------- |
| 议题1（B-02 stale）     | I-13是B-02的执行层实现，必须同步               |
| 议题1（I-05通知）       | I-12统一落点后，通知逻辑从BASE_AI_PIPELINE读取 |
| 议题2（B-07视图注册表） | B-03分治编辑的routeToAgent依赖B-07的映射       |
| 议题2（B-08契约Schema） | I-09的F_AFFECTED_NODES依赖B-08的NodeType定义   |

---

## 议题3裁决辅助单（整合版）

| ID   | 优先级 | 核心方案                                                     | 责任人            | 截止时间                                     | 关键约束                  |
| ---- | ------ | ------------------------------------------------------------ | ----------------- | -------------------------------------------- | ------------------------- |
| B-03 | P0     | 双模式：分治编辑（局部）+ 全量替换（大修改）；分治编辑拆两阶段 | 首席架构师+前后端 | 今天定流程，阶段五第1周做全量，第3-4周做分治 | 分治六步流程              |
| I-09 | P1     | BASE_IR_VERSION补充6个字段；构建版本树                       | 后端              | 今天定DDL，阶段五并行                        | F_PARENT_VERSION + F_DIFF |
| I-10 | P1     | 标记删除（非物理）+ 乐观锁 + 定时清理                        | 后端              | 阶段五并行                                   | F_STATUS = 'archived'     |
| I-11 | P1     | SeedDataService初始化admin；随机密码+强制修改                | 后端              | 今天定策略，阶段五并行                       | 密码策略                  |
| I-12 | P0     | BASE_AI_PIPELINE增加F_FAILURE_COUNTS JSON列；统一落点        | 后端              | 今天定DDL，明天改代码                        | 唯一真源                  |
| I-13 | P0     | StaleMonitorService定时任务；与B-02同步                      | 后端              | 与B-02同步                                   | 议题1已裁决               |

### 必须今天锁定的3项契约

| #    | 契约项                  | 锁定内容                       |
| ---- | ----------------------- | ------------------------------ |
| 1    | BASE_IR_VERSION扩展DDL  | 6个新增字段 + 版本树结构       |
| 2    | BASE_AI_PIPELINE扩展DDL | F_FAILURE_COUNTS JSON列        |
| 3    | admin自动创建数据流     | SeedDataService时序 + 密码策略 |

---

玛维思，作为牵头人，请确认上述整合方案是否可接受？还是有调整？