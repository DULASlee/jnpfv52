# JNPF V5.2 开发规范

> 目标：新工程师入职后 30 分钟内掌握编码规范。
> 配套：`../../.claude/rules/jnpf-expert-traps.md` (必须熟读的 14 个陷阱)

---

## 1. 新模块开发

### 创建 JnpfModule

```csharp
using JNPF.Modules;

[JNPF.Modules.DependsOn(typeof(DatabaseModule))]
public class MyFeatureModule : JnpfModule
{
    public override void ConfigureServices(
        IServiceCollection services, IConfiguration configuration)
    {
        // 注册服务、配置选项
        services.AddScoped<IMyService, MyService>();
    }

    public override void OnApplicationInitialization(
        IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 注册中间件（如有需要）
    }
}
```

### 模块注册规则

1. 继承 `JnpfModule` 基类
2. 添加 `[DependsOn]` 声明依赖
3. 放在 `application/JNPF.API.Entry/Modules/` 下
4. 通过 `services.AddJnpfModules()` 自动发现（无需手动注册）

### 依赖声明注意事项

- 拓扑排序自动解决加载顺序
- 循环依赖在 Kahn 排序阶段报错
- 无需显式调用依赖模块的初始化方法

---

## 2. 数据访问规范

### 读操作

```csharp
public class UserService : IUserService, IDynamicApiController
{
    private readonly ISqlSugarRepository<UserEntity> _repo;

    public UserService(ISqlSugarRepository<UserEntity> repo)
    {
        _repo = repo;  // 构造函数 ≤ 5 行
    }

    public async Task<List<UserEntity>> GetActiveUsers()
    {
        // Queryable 自动受 QueryFilter 保护（TenantId + 软删除）
        return await _repo.Queryable()
            .Where(u => u.EnabledMark == 1)
            .ToListAsync();
    }
}
```

### 写操作（重要）

**新代码必须使用 Safe* 方法：**

| 方法 | 用途 | 替代 |
|---|---|---|
| `SafeUpdateAsync(entity)` | 更新单条 | `UpdateAsync` |
| `SafeDeleteAsync(entity)` | 删除单条 | `DeleteAsync` |
| `SafeInsertAsync(entity)` | 插入单条 | `InsertAsync` |
| `SafeUpdateRangeAsync(entities)` | 批量更新 | `UpdateRangeAsync` |
| `SafeDeleteRangeAsync(entities)` | 批量删除 | `DeleteRangeAsync` |
| `SafeInsertRangeAsync(entities)` | 批量插入 | `InsertRangeAsync` |

```csharp
// ✅ 正确
await _repo.SafeUpdateAsync(user);
await _repo.SafeDeleteAsync(record);

// ❌ 错误 — 触发 ADR-012 兜底 + WARNING 日志
await _repo.UpdateAsync(user);
```

**安全保证：** Safe* 方法自动在 WHERE 子句中包含 `TenantId`，防止跨租户修改。

---

## 3. 事件发布规范

### 发布事件

```csharp
private readonly IEventBus _eventBus;

public async Task CreateUser(UserCrInput input)
{
    var entity = input.Adapt<UserEntity>();
    await _repo.SafeInsertAsync(entity);
    
    // 发事件 — 自动走 Outbox 管道
    await _eventBus.PublishAsync(new UserCreatedEvent(entity.Id));
}
```

### 消费事件

```csharp
public class UserCreatedEventHandler : IEventSubscriber<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent @event)
    {
        // TenantPropagationFilter 自动恢复租户上下文
        // 无需手动设置 TenantId
        await DoSomething(@event.UserId);
    }
}
```

### Outbox 规则

- 默认全部走 Outbox 管道（可靠性保证）
- `[BypassOutbox]` 仅限系统心跳使用，且必须在注释中说明理由
- 事件处理器必须幂等（ProcessedEvent 表自动检查）

---

## 4. 验证规则开发

### 创建 Validator

```csharp
public class UserCrInputValidator : AbstractValidator<UserCrInput>
{
    public UserCrInputValidator()
    {
        RuleFor(x => x.account)
            .NotEmpty().WithMessage("账号不能为空")
            .Length(3, 50).WithMessage("账号长度 3-50 字符");

        RuleFor(x => x.password)
            .NotEmpty().WithMessage("密码不能为空")
            .Length(6, 32).WithMessage("密码长度 6-32 字符");

        RuleFor(x => x.mobilePhone)
            .Matches(@"^1[3-9]\d{9}$")
            .WithMessage("手机号格式不正确");
    }
}
```

### 注册

- ValidationModule 自动扫描注册（`AddValidatorsFromAssemblyContaining<UserCrInputValidator>()`）
- 错误响应自动格式化，前端直接展示
- 中文验证消息

---

## 5. DTO 命名规范

| 后缀 | 用途 | 示例 |
|---|---|---|
| `CrInput` | 创建输入 | `UserCrInput` |
| `UpInput : CrInput` | 更新输入 (加 `id` 字段) | `UserUpInput : UserCrInput` |
| `ListOutput` | 列表输出 | `UserListOutput` |
| `InfoOutput` | 详情输出 | `UserInfoOutput` |

```csharp
// 创建 DTO
public class UserCrInput
{
    public string account { get; set; }
    public string realName { get; set; }
    public string organizeId { get; set; }
}

// 更新 DTO — 继承创建 DTO，追加 id
public class UserUpInput : UserCrInput
{
    public string id { get; set; }
}
```

---

## 6. API 开发规范

### 方法命名（不可重命名！）

```csharp
public class UserService : IDynamicApiController
{
    // 路由自动生成为: GET /api/User/GetPageList
    public Task<PageResult<UserListOutput>> GetPageList(PageInput input) { }

    // GET /api/User/GetInfo?id=xxx
    public Task<UserInfoOutput> GetInfo(string id) { }

    // POST /api/User/Add
    public Task Add(UserCrInput input) { }

    // PUT /api/User/Update
    public Task Update(UserUpInput input) { }

    // DELETE /api/User/Delete?id=xxx
    public Task Delete(string id) { }
}
```

**铁律：** 方法名 = API 路由的一部分。重命名方法 = 改变 URL = 前端 404。

### 异常抛出

```csharp
// ✅ 业务异常 — 前端展示错误消息
if (await _repo.Queryable().AnyAsync(u => u.account == input.account))
    throw Oops.Bah("账号已存在");

// ✅ 系统异常 — HTTP 500 + 日志
if (dbConnectionFailed)
    throw Oops.Oh("数据库连接失败");

// ❌ 禁止
throw new Exception("something wrong");
```

### 返回值

```csharp
// ✅ 自动包装为 RESTfulResult<T>
public async Task<UserInfoOutput> GetInfo(string id) { ... }

// ✅ 分页
public async Task<PageResult<UserListOutput>> GetPageList(PageInput input) { ... }

// ✅ 列表
public async Task<List<UserListOutput>> GetList() { ... }

// ❌ 禁止手动包装
public RESTfulResult<UserInfoOutput> GetInfo(string id) { ... }
```

---

## 7. 代码质量防线

### Analyzer 规则速查

| 规则 | 说明 | 违反示例 |
|---|---|---|
| JNPF001 | 禁止 App.GetService | `var svc = App.GetService<T>()` |
| JNPF002 | 禁止直接覆盖 DataExecuting | `db.Aop.DataExecuting = (...) => {}` |
| JNPF003 | 禁止 CreateScope | `provider.CreateScope()` |
| JNPF004 | BypassOutbox 需注释 | `[BypassOutbox]` 无注释 |
| JNPF005 | 禁止直接注入 ISqlSugarClient | `ctor(ISqlSugarClient client)` |
| JNPF006 | 禁止 async void | `async void OnClick()` |

### 修复优先级

```
新代码: 所有规则 suggestion → error（必须修复）
存量代码: 逐步迁移，每 Sprint 处理 1 个模块
```

---

## 8. 常见陷阱速查

> 详细版见 `../../.claude/rules/jnpf-expert-traps.md`

| # | 陷阱 | 症状 | 修复 |
|---|---|---|---|
| 1 | 重命名 Service 方法 | 前端 404 | 同步更新前端 URL |
| 2 | Mapster Adapt 覆盖审计字段 | CreateTime 被清空 | `input.Adapt(entity)` 到已查询实体 |
| 3 | 导航属性 N+1 | 300 queries for 100 rows | `Includes()` 或 DTO 投影 |
| 4 | Oops.Bah vs Oops.Oh | 用户看到 500 | 业务异常用 Bah |
| 5 | 手动包装 RESTfulResult | 双层 data 嵌套 | 只 return 值 |
| 6 | 方法名 Async 后缀 | 路由多出 Async | 接口方法不用后缀 |
| 7 | 子查询无租户过滤 | 跨租户泄露 | 手动加 TenantId |
| 8 | Updateable 无 TenantId | 修改全部租户 | 使用 SafeUpdateAsync |

---

## 9. 已封存文件

以下文件已完成架构迭代使命。修改需技术 Lead 审批：

| # | 文件 | 封存原因 |
|---|---|---|
| 1 | `JwtHandler.cs` | 认证处理器终态 |
| 2 | `SqlSugarConfigureExtensions.cs` | ORM 配置入口终态 |
| 3 | `Program.cs` | WebComponent.Load 启动终态 |
| 4 | `AppServiceCollectionExtensions.cs` | 模块系统入口终态 |
| 5 | `Startup.cs` | 中间件编排终态 |
| 6 | `SqlSugarRepository.cs` | 仓储基类终态 |
| 7 | `Service.cs.vm` | 代码生成模板终态 |
| 8 | `LogEventSubscriber.cs` | 事件日志终态 |

---

## 10. 开发环境

```bash
# 后端
cd d:\JNPF-v52\backend
dotnet build
dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj

# 前端 PC
cd d:\JNPF-v52\jnpf-web-vue3
pnpm run dev  # → localhost:3100

# 前端 DataV
cd d:\JNPF-v52\jnpf-web-datascreen
pnpm run dev  # → localhost:3102

# 测试
dotnet test tools/JNPF.Analyzers/JNPF.Analyzers.Tests/
```
