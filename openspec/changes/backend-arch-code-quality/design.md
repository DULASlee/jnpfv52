# Design: backend-arch-code-quality

> **编写规范**：[`docs/architecture/ARCHITECTURE_DOC_RULES.md`](../../docs/architecture/ARCHITECTURE_DOC_RULES.md)  
> **父提案**：[`proposal.md`](./proposal.md)  
> **定位**：P1 架构 + P2 设计模式 + P3 接口契约（禁止方法体，遵循 ADF 三先行）

---

## 1. 层边界与依赖反转

### 1.1 现状违例

```
framework(JNPF)  ──801次──▶  inteAssistant
inteAssistant    ──626次──▶  framework(JNPF)
```

`JNPF`(framework) 作为核心层，fan-in 1697 / fan-out 821，被全仓依赖——但它**反向**调用 inteAssistant 626 次。核心层依赖业务模块 = 分层倒置。

### 1.2 目标拓扑

```
┌─────────────────────────────────────────┐
│  application (JNPF.API.Entry)           │  ← 组合根
├─────────────────────────────────────────┤
│  modularity/inteAssistant               │  实现 IInteAssistantBridge
│  modularity/system / visualdev / ...    │
├─────────────────────────────────────────┤
│  framework/JNPF                          │  依赖抽象，不依赖 inteAssistant
│    定义 IInteAssistantBridge (接口)      │
│  modularity/common                       │
└─────────────────────────────────────────┘
```

**依赖反转原则**：framework 定义 `IInteAssistantBridge` 接口（声明它需要 inteAssistant 提供的能力），inteAssistant 实现该接口，在 `application` 组合根注册。framework 只依赖接口，不再 `using JNPF.InteAssistant`。

### 1.3 failure_boundary

- **接口粒度**：桥接口按「能力域」聚合，非一个大接口。初版仅抽取 framework 反向调用最频繁的 inteAssistant 入口（前 10 个方法）。
- **渐进**：不一次性反转全部 626 条边。先切断 `framework → inteAssistant` 的直接 `using`，剩余通过事件总线/回调逐步解耦。

---

## 2. 复杂度拆解模式（UserManager 授权簇）

### 2.1 目标方法

| 方法 | 签名 | CC | 认知 | 行数 |
|------|------|---:|-----:|-----:|
| `GetConditionAsync` | `(string moduleId, string primaryKey="f_id", bool isDataPermissions=true, string tableNumber="")` → `List<IConditionalModel>` | 45 | 279 | 311 |
| `GetDataConditionAsync` | 同上 | 45 | 279 | 311 |
| `GetCondition` | (同步版) | 37 | 193 | — |
| `GetCodeGenAuthorizeModuleResource` | — | 38 | 209 | — |

> 源码：`modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs` · 接口 `IUserManager.cs`

### 2.2 内部逻辑解构（来自图 bt 字段，待源码验证）

`GetConditionAsync` 的 311 行混合了**至少 4 个独立职责**：

1. **管理员短路**：`IsAdministrator == true` 直接返回空条件（全权限）
2. **数据范围解析**：`dataScope = DataScope.Select(organizeId)` —— 解析用户的数据可见范围
3. **角色权限聚合**：`roles → PermissionGroup → roleAuthorizeList(AuthorizeEntity)` —— 聚合角色授权
4. **SqlSugar 条件组装**：`conModels.Add(ConditionalCollections...)` —— 把权限转成 `IConditionalModel`

### 2.3 拆解模式：Extract Method + 决策表

将 4 职责抽成 4 个私有方法，主方法降为编排：

```
GetConditionAsync(moduleId, ...)           // 编排，目标 CC < 8
  ├─ BuildAdminShortCircuit()              // 职责1: 管理员短路
  ├─ ResolveDataScope()                    // 职责2: 数据范围
  ├─ AggregateRolePermissions(moduleId)    // 职责3: 角色权限
  └─ ComposeSqlSugarConditions(...)        // 职责4: 条件组装
```

**对「角色权限 → 条件」的映射**（当前是 if/else 链），改用**决策表**（`AuthorizeEntity.ItemType` → 条件构造器策略）：

| ItemType | 条件构造 | 现状 |
|----------|---------|------|
| `a` (模块) | `FieldName = moduleId` | if 分支 |
| ... | ... | if 分支 |

→ 替换为 `Dictionary<string, IConditionStrategy>` + `strategy.Build(entity)`，消除 ItemType 的 switch/if 链。

### 2.4 不变量（拆解前后必须等价）

- **行为不变**：对相同 `(userId, moduleId, roles)`，输出的 `List<IConditionalModel>` 必须完全一致（测试断言）。
- **SQL 不变**：拆解后 `ToSql()` 输出的 WHERE 子句与拆解前**逐字符相同**——这是多租户数据隔离的红线。
- **先测后拆**：先写覆盖 5 种授权场景（管理员/全数据/本部门/本部门及下级/仅本人）的 xUnit，确认绿，再 extract method。

---

## 3. 静态门禁设计

### 3.1 ComplexityAnalyzer（Roslyn）

| 规则 | 阈值 | 级别 |
|------|------|------|
| `JNPF_Complexity_30` | CC > 30 | error（编译失败） |
| `JNPF_Complexity_20` | CC > 20 | warning |

实现：基于 Roslyn `CyclomaticComplexity` 计算，注册为 `DiagnosticAnalyzer`。位置：`backend/tools/JNPF.Analyzers/Analyzers/ComplexityAnalyzer.cs`。

> **【待源码验证】** 现有 `JNPF.Analyzers` 的 Analyzer 注册方式（`DiagnosticDescriptor` 约定），需 Read `backend/tools/JNPF.Analyzers/Analyzers/*.cs` 确认命名约定后对齐。

### 3.2 ArchUnit.NET 架构测试

```csharp
// backend/tests/JNPF.Architecture.Tests/LayerBoundaryTests.cs
var Framework = Types().That().ResideInNamespace("JNPF.*").And()...;
var InteAssistant = Types().That().ResideInNamespace("JNPF.InteAssistant.*");

// 断言：framework 不依赖 inteAssistant（反转后）
InteAssistant.Should().NotDependOn... // 或 Framework.Should().OnlyDependOn(接口)
```

位置：新建 `backend/tests/JNPF.Architecture.Tests/` 项目。

### 3.3 CI 接入

`dotnet build /p:CI_BUILD=true`（既有命令）已触发 analyzer。新增：架构测试纳入 `dotnet test backend/zx_lowcode_netcore.sln`。

---

## 4. P3 接口契约（签名级，禁止方法体）

### 4.1 IInteAssistantBridge（依赖反转）

```csharp
// framework/JNPF/.../IInteAssistantBridge.cs  【待源码验证：精确命名空间】
namespace JNPF.Bridges;

public interface IInteAssistantBridge
{
    // 【待源码验证】仅声明 framework 反向调用 inteAssistant 的前10个高频方法
    // 具体方法签名在 P4 实施时据 `framework→inteAssistant` 调用 Top10 确定
    Task<IReadOnlyList<AiProjectBrief>> GetActiveProjectsAsync(Guid tenantId);
    // ...
}
```

### 4.2 IConditionStrategy（决策表策略）

```csharp
// common/JNPF.Common.Core/Manager/User/Conditions/IConditionStrategy.cs
namespace JNPF.Common.Security;

public interface IConditionStrategy
{
    string ItemType { get; }
    IConditionalModel Build(AuthorizeEntity entity, string primaryKey);
}
```

### 4.3 测试契约

```csharp
// tests/.../UserManagerDataPermissionTests.cs
public class UserManagerDataPermissionTests {
    // 5 场景：管理员全权限 / 全数据范围 / 本部门 / 本部门及下级 / 仅本人
    [Theory] [InlineData(DataScope.All)] ...
    public async Task GetConditionAsync_数据范围_返回正确条件(DataScope scope) { ... }

    // 不变量：ToSql() 逐字符等价
    [Fact]
    public async Task 拆解前后_SqlSugar生成的WHERE完全一致() { ... }
}
```

---

## 5. 整改排序（与实施计划 tasks.md 对齐）

| 阶段 | 内容 | 前置依赖 |
|------|------|---------|
| S1 | ComplexityAnalyzer + ArchUnit + CI 门禁 | 无（纯新增） |
| S2 | UserManager 授权簇：补测 → 拆解 | S1（门禁验证拆解不回退） |
| S3 | 低代码主路径：RunService/VisualDevService 补测→拆解 | S1 |
| S4 | framework↔inteAssistant 依赖反转 | S2/S3 稳定后（降低风险） |

**铁律**：S2/S3 每个 god 方法「先补测（红→绿）→ 再 extract method（绿保绿）」；**禁止无测试拆分 CC≥30**。

---

## 6. 风险与缓解

| 风险 | 缓解 |
|------|------|
| 拆 `UserManager` 破坏数据隔离 → 越权 | ToSql() 等价断言 + 5 场景覆盖，任何 PR 不过此关不合并 |
| 依赖反转范围过大 | 渐进：先 Top10 高频方法，分多次 PR |
| 门禁 CC>30 阻塞现有构建 | 引入 baseline：现有 41 个方法暂豁免（`[Suppress] + TODO`），仅拦新增 |
| ArchUnit 测试慢 | 独立测试项目，不进单元测试跑批 |
