好的，议题2开始。千问牵头，其他专家补充。

---

## 议题2：智能体与校验（千问牵头，30分钟）

### 千问预案回顾（3个P0 + 2个P1）

| ID   | 问题                             | 优先级 | 核心方案                                                     |
| ---- | -------------------------------- | ------ | ------------------------------------------------------------ |
| B-07 | 视图注册表与后端6 SubAgent不匹配 | P0     | 修正AgentViewRegistry，与后端6 SubAgent一一对应              |
| B-08 | 输入输出契约未定义               | P0     | 定义每个Agent的输入输出Schema，Batch 2明确依赖Batch 1的IR_JSON和Schema_Version |
| B-09 | DocumentMerger合并后无二次校验   | P0     | 强制插入校验链拦截器：cleanSchema → validateIR → vue-tsc     |
| I-07 | 批次2在批次1部分失败时的降级     | P1     | 定义partial_success临时状态，允许下载已生成代码              |
| I-08 | 知识注入AOP模式Prompt污染风险    | P1     | 增加EABCompliancePrecheck预检                                |

---

## 玛维思补充（数据流向 + 接口契约角度）

### B-07补充：动态加载方案
- **怎么改**：前端AgentViewRegistry不要写死，启动时调用`GET /api/founder/ai/agents/list`从后端动态拉取
- **接口补**：`GET /api/founder/ai/agents/list`，响应`{ agents: [{name, type, batch, capabilities, status}] }`
- **关键**：后端是single source of truth，前端只是镜像

### B-08补充：契约必须包含4个字段
1. **输入**：JSON Schema + 数据源 + 上游依赖Agent列表
2. **输出**：JSON Schema + 落点表 + 下游消费者
3. **校验点**：cleanSchema → validateIR → vue-tsc 三选哪些
4. **失败码**：schema_violation / version_mismatch / empty_output

**关键补丁**：加`IR_SCHEMA_VERSION`全局常量（`core/ir/ir-version.ts`），所有Agent启动时校验版本一致性

### B-09补充：短路逻辑 + 熔断集成
- 校验链必须**短路**（任一失败立即中断）
- 每步失败写`BASE_AI_PIPELINE_MESSAGE`（F_ROLE='system'）
- **纳入§4.0全局熔断**：连续3次validation_failure → blocked

### I-07补充：partial_success不是终态
- 明确下一步：重试失败子Agent 或 用户手动接受当前进度
- `BASE_AI_PIPELINE`加2列：`F_PARTIAL_STAGES JSON` + `F_DOWNLOADABLE_ARTIFACTS JSON`
- 新增接口：`GET /api/pipeline/{id}/partial-artifacts`

### I-08补充：双层校验
- **规则层**：硬关键词黑名单（PostgreSQL、Neo4j、microservice、mTLS等）
- **LLM层**：小模型分类器判断语义违规
- 违规时拒绝注入 + 写审计日志（建议新增`BASE_EAB_VIOLATION_LOG`表）

---

## 清言补充（可编码级细节）

### B-07-R1：IR NodeType映射表（强制锁定）

| SubAgent类            | AgentName         | ownedNodeTypes              | writableNodeTypes           | readableNodeTypes                              | IR现状                |
| --------------------- | ----------------- | --------------------------- | --------------------------- | ---------------------------------------------- | --------------------- |
| FunctionalModuleAgent | functional_module | ModuleDefinition            | ModuleDefinition            | Requirement                                    | ✅已有                 |
| BusinessProcessAgent  | business_process  | ProcessFlow                 | ProcessFlow                 | Requirement, ModuleDefinition                  | ⚠️ProcessFlow缺失      |
| DatabaseAgent         | database          | TableDefinition, DataSource | TableDefinition, DataSource | Requirement, ModuleDefinition                  | ✅已有                 |
| UIDesignAgent         | ui_design         | Component, PageLayout       | Component, PageLayout       | ModuleDefinition, TableDefinition, ProcessFlow | ⚠️PageLayout缺失       |
| PermissionAgent       | permission        | PermissionMatrix            | PermissionMatrix            | ModuleDefinition, TableDefinition, ProcessFlow | ❌PermissionMatrix缺失 |
| ApiDesignAgent        | api_design        | APIEndpoint                 | APIEndpoint                 | ModuleDefinition, TableDefinition              | ❌APIEndpoint缺失      |

**裁决**：`types.ts`必须新增3个IR NodeType：ProcessFlow、PageLayout、APIEndpoint。PermissionMatrix暂用RuleNode替代。

### B-08-R1：批次2强制依赖批次1全部输出
- 批次2的3个Agent**强制依赖**批次1全部输出
- 批次1任一Agent失败 → 批次2全部跳过（不进入partial_success，直接进入批次1重试）

**B-08-R2**：批次1完成后、批次2启动前，执行**中间校验**：验证modules/tables/processes不为空且schema合法。失败 → 不启动批次2。

### B-09-R1~R3：三级校验 + 分级处理

| 校验环节      | 失败性质 | 处理策略                  | 重试次数                       |
| ------------- | -------- | ------------------------- | ------------------------------ |
| cleanSchema() | 清洗问题 | 自动修复后重试            | 3次（指数退避）                |
| validateIR()  | 结构错误 | 回退到Agent重新生成该部分 | 2次（指定失败Agent重跑）       |
| vue-tsc       | 类型错误 | 人工介入                  | 0次（进入review态，非blocked） |

**关键**：vue-tsc失败时，IR版本快照仍需保存（标记status: invalid），供开发者手动排查。

### I-07-R1：降级简化规则
- **FunctionalModule和Database是硬依赖**，任一失败即blocked
- **BusinessProcess是软依赖**，失败可降级（processes输入为空数组）

### I-08-R1：预检规则从EAB Config动态读取
- 不从代码硬编码，从`BASE_AI_PROMPT_TEMPLATE.F_VARIABLES`的`eab_config`字段动态读取
- EAB变更时只需改Prompt模板，不改代码

---

## KIMI补充（跨议题一致性检查）

### 与议题1的接口契约联动

| 议题1接口                                            | 议题2影响                                 |
| ---------------------------------------------------- | ----------------------------------------- |
| `POST /api/pipeline/{id}/stages/{stageName}/confirm` | B-08的契约Schema决定confirm请求的body结构 |
| `POST /api/pipeline/{id}/preview-rollback`           | B-07的NodeType映射决定affectedNodes结构   |
| `GET /api/pipeline/stale`                            | I-07的partial_success影响stale列表展示    |

### 建议：B-08契约Schema作为独立章节

玛维思提议用`§3.2.5a-Agent-Contract-Schema`定义，议题1的6个接口引用此契约。我同意。

---

## 议题2裁决辅助单（整合版）

| ID   | 优先级 | 核心方案                                                     | 责任人            | 截止时间                               | 关键约束      |
| ---- | ------ | ------------------------------------------------------------ | ----------------- | -------------------------------------- | ------------- |
| B-07 | P0     | 后端动态提供Agent清单，前端镜像；types.ts新增3个NodeType     | 首席架构师+前后端 | 今天定契约，types.ts今天改，后端明天改 | B-07-R1/R2    |
| B-08 | P0     | 6 SubAgent输入输出Schema锁定；批次2强制依赖批次1全部输出；中间校验 | 首席架构师+后端   | 今天定Schema，明天改代码               | B-08-R1/R2    |
| B-09 | P0     | 校验链短路执行；三级校验分级处理；vue-tsc失败进review态非blocked | 后端              | 今天定策略，明天改代码                 | B-09-R1/R2/R3 |
| I-07 | P1     | partial_success过渡态；硬依赖失败blocked，软依赖可降级       | 首席架构师+后端   | 阶段五并行                             | I-07-R1       |
| I-08 | P1     | AOP切面EAB预检；规则从Prompt模板动态读取                     | 后端              | 阶段五并行，规则集今天定               | I-08-R1       |

### 必须今天锁定的3项契约

| #    | 契约项                    | 锁定内容                                                    |
| ---- | ------------------------- | ----------------------------------------------------------- |
| 1    | 6 SubAgent输入输出Schema  | 上方TypeScript接口定义                                      |
| 2    | IR类型系统新增3个NodeType | ProcessFlow、PageLayout、APIEndpoint                        |
| 3    | 校验链分级处理策略        | cleanSchema自动修复/validateIR指定Agent重跑/vue-tsc人工介入 |

---

千问，作为牵头人，请确认上述整合方案是否可接受？还是有调整？