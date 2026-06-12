# 多租户启用路线图

> Sprint 0-A Day 2 交付物
> 当前状态：MultiTenancy=false
> 目标：阶段六前切换为 true

---

## 1. 当前状态

```
App.json → MultiTenancy: false
隔离模式：SCHEMA (type=1)
ITenantFilter：已注册但未激活（3 个注册点）
TenantResolver：4 级回落（JWT → Header → QueryString → Default）
DataExecuting：自动填充 F_TENANT_ID（Insert 时已生效）
```

**为什么现在关闭：**
- 测试数据库（ZXAF_V1_DevTest1）无多租户数据
- 前端开发环境单租户模式更简单
- 当前无生产多租户客户

---

## 2. 启用风险清单

| 风险 ID | 描述 | 严重度 | 影响范围 |
|---------|------|--------|----------|
| MT-001 | QueryFilter.Clear() 清空全局过滤器后重新添加，中间窗口无过滤 | CRITICAL | 所有模块 |
| MT-002 | Updateable/Deleteable 不自动过滤 TenantId | HIGH | 写操作 |
| MT-003 | 子查询不应用 ITenantFilter | HIGH | 复杂查询 |
| MT-004 | 直接 SQL（Ado.SqlQuery）无租户隔离 | MEDIUM | Dapper 查询 |
| MT-005 | EventBus 消息可能跨租户 | MEDIUM | 异步处理 |
| MT-006 | 缓存 Key 不含 TenantId 前缀，跨租户数据污染 | MEDIUM | 权限/菜单缓存 |
| MT-007 | Schedule 任务无租户上下文 | LOW | 定时任务 |

---

## 3. 启用五步方案

### Step 1：修复 QueryFilter 竞争条件（1 天）

```csharp
// 当前代码 (SqlSugarConfigureExtensions.cs:57):
db.QueryFilter.Clear();
db.QueryFilter.Add(new TableFilterItem<...>(...)); // ITenantFilter

// 修复：使用 AddTableFilter 替代 Clear + Add
// 或在 Clear 和 Add 之间加锁，确保无查询插入中间窗口
```

### Step 2：添加 Updateable/Deleteable 自动租户（2 天）

```csharp
// 方案 A：SqlSugar AOP 拦截 Updateable/Deleteable
// 在 DataExecuting 委托中检查 TenantId 条件

// 方案 B：Repository 基类包装
// 所有 Updateable/Deleteable 调用必须经过 Repository 基类
// 基类自动添加 .Where(it => it.TenantId == tenantId)
```

**建议方案 B**，与现有 SqlSugarRepository 模式一致。

### Step 3：子查询租户审计（1 天）

```bash
# 扫描所有子查询，确保包含 TenantId 条件
grep -rn "Queryable<" backend/modularity/ | grep -v "\.Where"
```

### Step 4：缓存隔离（1 天）

```csharp
// 所有缓存 Key 必须包含租户前缀
// 修改 ICacheManager 接口，在 Get/Set 时自动拼接 TenantId
```

### Step 5：启用开关 + 集成测试（2 天）

```json
// App.json
{ "MultiTenancy": true }
```

```bash
# 集成测试：跨租户越权验证
dotnet test --filter "TenantIsolation"
```

---

## 4. 与阶段计划的对应关系

| 阶段 | 租户相关任务 | 启用到哪个级别 |
|------|-------------|---------------|
| Sprint 0-A (当前) | 登记路线图 | 无 |
| Sprint 0-B | 新增表强制 ITenantFilter | 建表规范 |
| 阶段一 | 知识图谱 Tenant 隔离 | BASE_KNOWLEDGE_* 按租户隔离 |
| 阶段二 | 大屏数据源 Tenant 隔离 | BLADE_VISUAL_* 按租户隔离 |
| 阶段三 | Prompt 模板 + AI 日志 Tenant | BASE_AI_* 按租户隔离 |
| 阶段四 | 工作流引擎 Tenant 回归 | FLOW_* 旧表租户补测 |
| 阶段五 | 全量租户回归测试 | QA 验收 |
| **阶段六（最终）** | **MultiTenancy=true 正式启动** | 生产上线 |

---

## 5. 为何不现在启用

1. **Step 1-5 共需 7 天**，当前 Sprint 0-A 无此带宽
2. **测试数据不足**：无多租户测试集，盲目启用 = 引入不确定行为
3. **风险敞口**：MT-001（QueryFilter 竞争）可能在实际多租户场景导致数据泄露
4. **渐进式策略**：Step 1-5 按阶段逐一消除，而非大爆炸式启动

---

## 6. 门禁条件

| 条件 | 状态 |
|------|------|
| Step 1-5 全部完成 | ⬜ |
| MT-001 ~ MT-007 全部修复或降级 | ⬜ |
| 多租户集成测试 ≥ 10 cases | ⬜ |
| ITenantFilter 覆盖率 100%（含子查询） | ⬜ |
| 灰度：先启用非生产环境 1 周 | ⬜ |

**预计完全启用时间：** 阶段六（从当前算起约 12-14 周）

---

## 7. 对当前开发的影响

- 所有**新创建的表**必须在 Sprint 0-B 开始时就包含 `F_TENANT_ID` 列
- 所有**新创建的 SqlSugar 查询**必须包含 TenantId 条件（即使当前 MultiTenancy=false）
- 所有**新事件**在 EventBus 发布时必须携带 TenantId（TenantPropagationFilter 已就位）
