# JNPF V5.2 MultiTenancy 影响分析报告 (A7)

> 编制日期: 2026-06-14 | 编制人: SmartAbp Developer
> 基于架构体检报告 V53 + Phase 5 AI 流水线代码

---

## 一、当前状态

| 配置项 | 当前值 | 位置 |
|---|---|---|
| MultiTenancy | `false` | `appsettings.json` |
| ITenantFilter | 已实现但未激活 | 框架层 |
| 租户隔离机制 | 接口就位，实际未执行 | `ITenantEntity` + SqlSugar GlobalFilter |

---

## 二、需要添加 TenantId 的表清单

### 2.1 已包含 TenantId 的表（从日志/事件/审计固化）

| 表名 | 项目 | 状态 |
|---|---|---|
| SYS_LOG | JNPF.Common | ✅ 已含 F_TENANT_ID |
| SYS_DIFF_LOG | JNPF.Common | ✅ 已含 F_TENANT_ID |
| SYS_EVENT_OUTBOX_MESSAGE | JNPF.Extras.EventBus.Outbox | ❌ 缺失 |
| PROCESSED_EVENT (幂等) | JNPF.Extras.EventBus.Outbox | ❌ 缺失 |
| BASE_AI_Call_LOG | JNPF.API.Entry (Phase 5) | ✅ 已含 F_TENANT_ID |

### 2.2 业务表（需排查）

| 模块 | 预计表数 | TenantId 状态 | 风险评估 |
|---|---|---|---|
| Base (系统基础) | ~25 | 部分含 | 中 |
| Message (消息) | ~8 | 部分含 | 低 |
| WorkFlow (工作流) | ~12 | 多数含 | 低 |
| DataVisualization (大屏) | ~5 | 未知 | 低 |
| VisualDev (在线开发) | ~15 | 依赖 CodeGen | 中 |
| OA (办公) | 禁用 | N/A | - |

---

## 三、ITenantFilter 模块覆盖

| 模块 | 查询层 | TenantFilter | 写入层 | Safe* 方法 |
|---|---|---|---|---|
| Base | ISqlSugarRepository | ❌ 未激活 | SqlSugar Insertable | 部分 |
| Message | ISqlSugarRepository | ❌ 未激活 | SqlSugar Insertable | 未覆盖 |
| WorkFlow | FlowEngine (Dapper) | ❌ 手动 | 手动 INSERT | 未覆盖 |
| DataVisualization | ISqlSugarRepository | ❌ 未激活 | 生成代码 | 未覆盖 |
| Phase 5 AI | ISqlSugarClient | N/A (新代码) | Insertable (含 TenantId) | N/A |

---

## 四、开启后可能破坏的功能

1. **现有 API 查询返回空结果**：未设置 TenantId 的旧数据在新过滤器下不可见
2. **FlowEngine Dapper 查询**：需手动添加 `WHERE F_TENANT_ID = @tenantId`
3. **SignalR 推送**：跨租户实时通知需重新设计
4. **定时任务**：Hangfire/SpareTime 执行时缺乏租户上下文
5. **数据接口**：DataInterfaceService 的跨库查询需租户感知

---

## 五、分模块逐步启用步骤

### Phase 1: 零风险（Week 1）
1. 创建 `BASE_TENANT` 表和种子数据
2. 执行现有表的 TenantId 回填脚本（默认 tenant = 'default'）
3. 添加 `ITenantProvider` 实现（从 JWT claims 提取 TenantId）

### Phase 2: 低风险（Week 2）
4. 按模块逐批激活 ITenantFilter：Message → DataVisualization → VisualDev
5. 每次激活后执行回归测试
6. WorkFlow 手动添加 TenantId 过滤

### Phase 3: 中风险（Week 3-4）
7. Base 模块激活 ITenantFilter
8. 修复所有 Dapper 查询缺少 TenantId 的问题
9. 定时任务注入 TenantContext

### Phase 4: 验证（Week 5）
10. 越权测试（10 条用例全部通过）
11. 生产级数据量性能测试
12. 多租户并发安全测试

---

## 六、Phase 5 AI 流水线的租户就绪

Phase 5 新增代码已在设计层面租户就绪：
- `AiCallLogEntity` 包含 `F_TENANT_ID`
- `ArchitectAgent` 自动注入 `F_TENANT_ID` 到所有生成表
- `DatabaseAgent` 强制包含租户和审计字段
- 前端 AI 层通过 JWT token 传递租户上下文
