# 详细检查清单

> 铁律正文见 `docs/铁律/编程铁律-v3.0.md`。本文档为各变更类型的详细检查项，按需查阅，不常驻上下文。
> 使用方式：识别变更类型 → 找到对应清单 → 逐条打勾 → 附在 PR 描述中。

---

## 检查清单 A：日志字段变更

> 触发条件：修改了 SysLogEntity、LogContext.PushProperty、TechnicalLogService、Serilog Enricher 中的任何字段

```
□ 1. 字段名一致性检查
    □ 实体类属性名 → [SugarColumn(ColumnName = "F_XXX")] 映射是否正确？
    □ LogContext.PushProperty("Key", value) 的 Key 名 → TechnicalLogService 读取时 GetString(props, "Key") 是否一致？
    □ Serilog JSON 输出中的 Properties.Key → 前端 TechLogEntry 模型属性名 → 前端页面列绑定名，是否一致？
      （四环链路：实体 ↔ DB ↔ LogContext ↔ 前端，任何一环断裂都导致字段为空）

□ 2. 数据验证
    □ 运行时打开 Serilog JSON 文件，确认字段有值（非空字符串、非 null）
    □ 调用 /api/system/TechnicalLog/trace?traceId=xxx，确认返回结果中字段有值
    □ 查询数据库 SELECT F_XXX FROM base_sys_log TOP 1 ORDER BY f_creator_time DESC，确认列有值

□ 3. 空值处理
    □ 未登录请求（登录接口本身、健康检查）的该字段，代码中是否有 ?? string.Empty 或 null 处理？
    □ 数据库该列是否允许 NULL？与实体类的 IsNullable 设置是否一致？
```

---

## 检查清单 B：async void 修复

> 触发条件：修改了 async void 方法，或将 async void 改为 async Task

```
□ 1. 接口同步
    □ 接口定义（IXxxService.cs）的返回类型是否从 void 改为 Task？
    □ 所有实现类是否同步修改？（grep -rn "IXxxService" --include="*.cs" 找到所有实现）
    □ 如果保留 async void（如 IJobPersistence 接口约束），方法体内是否有 try-catch？

□ 2. 调用方同步
    □ 全局搜索该方法名：grep -rn "MethodName" --include="*.cs" .
    □ 所有调用方是否从同步调用改为 await 调用？
    □ 如果调用方本身是 fire-and-forget 场景，是否用了 _ = Task.Run(async () => await ...))？

□ 3. 异常处理
    □ try-catch 中的 catch 块是否有日志输出？（_logger.LogError / Trace.WriteLine / Console.Error）
    □ 日志输出在生产环境是否可见？（Trace.WriteLine 需 TraceListener；Debug.WriteLine 仅 Debug 构建）

□ 4. 全局清点
    □ 修复后执行：grep -rn "async void" --include="*.cs" modularity/ framework/ application/
    □ 确认剩余 async void 数量 = 预期数量（全部为接口约束 + 已有 try-catch）
    □ 贴出搜索输出
```

---

## 检查清单 C：配置变更（appsettings.json / SerilogBootstrap / 代码内硬编码配置）

> 触发条件：修改了 appsettings.json、SerilogBootstrap.cs 中的配置值、或任何硬编码的阈值/路径/开关

```
□ 1. 数值型配置
    □ 手写出计算过程并验证结果（如 50 * 1024 * 1024 = 52,428,800）
    □ 确认不是手误（如 *124 vs *1024，*1000 vs *1024）
    □ 如果 error sink 和 warning sink 各有一份配置，两份是否一致？

□ 2. 布尔型配置
    □ 确认 true/false 的方向：代码中的判断条件（if (config == true)）与预期行为是否对应？
    □ 配置缺失时的默认值是什么？代码中是否有 ?? 默认值处理？

□ 3. 路径型配置
    □ 目录是否存在？启动时是否自动创建？如果不会自动创建，是否需要手动创建？
    □ 路径分隔符是否正确？（Windows 用 \，Linux 用 /，代码中是否用了 Path.Combine？）

□ 4. 多环境同步
    □ appsettings.Development.json 是否同步修改？
    □ appsettings.Production.json 是否同步修改？
    □ Docker 环境变量 / K8s ConfigMap 是否需要同步？

□ 5. 运行时验证
    □ 启动项目，触发相关功能
    □ 确认新配置实际生效（日志文件按新路径写入 / 阈值触发行为符合预期 / 新开关生效）
```

---

## 检查清单 D：接口签名变更

> 触发条件：修改了 Interface 文件中的方法签名（参数、返回类型、新增/删除方法）

```
□ 1. 接口定义
    □ Interface 文件已修改

□ 2. 实现类（全部，不是部分）
    □ grep -rn "IClassName" --include="*.cs" . 找到所有实现
    □ 逐个修改，贴出每个实现类的修改

□ 3. 调用方（全部，不是部分）
    □ grep -rn ".MethodName(" --include="*.cs" . 找到所有调用
    □ 逐个更新（如 void→Task 需加 await，参数变更需更新传参）

□ 4. DI 注册
    □ 如果构造函数参数变了（新增依赖），DI 容器是否能自动解析？
    □ 如果是泛型接口，注册方式是否受影响？

□ 5. 全局确认
    □ 编译通过：grep -c "error CS" = 0
    □ 贴出完整文件清单（接口 + 实现 + 调用方，含文件路径和行号）
```

---

## 检查清单 E：中间件 / Filter 变更

> 触发条件：修改了 Middleware、ActionFilter、ExceptionFilter、或 Startup.cs/Program.cs 中的中间件注册

```
□ 1. 注册检查
    □ 中间件是否在 Startup.cs 或 Program.cs 中注册？（贴出注册代码行）
    □ 注册顺序是否正确？（ASP.NET Core 中间件顺序 = 执行顺序，靠前的先执行）
    □ 如果是 Filter，注册方式是全局注册还是按控制器/Action 注册？

□ 2. 执行路径检查
    □ 所有分支路径是否都有 await _next(context)？（if-else 的两个分支都要检查）
    □ 提前 return 的分支（如 IgnoreAll）是否正确跳过了后续逻辑？
    □ LogContext.PushProperty 的 using 作用域是否覆盖了 _next(context) 的调用？
      （如果 using 在 _next 之前就结束了，后续日志拿不到 PushProperty 的值）

□ 3. 性能检查
    □ 中间件中的同步操作（如 IP 查询、数据库查询）是否耗时？
    □ 是否应改为异步或缓存？（如 IP 查询结果可在请求生命周期内缓存）

□ 4. 异常安全
    □ 如果 _next(context) 抛异常，中间件中的资源是否正确释放？（using/try-finally）
    □ 异常是否会绕过中间件中的关键逻辑？（如 TraceId 的响应头设置）

□ 5. 运行时验证
    □ 发起一个 HTTP 请求
    □ 通过日志输出 / 断点 / 响应头，确认中间件确实执行了
    □ 确认中间件的执行时机正确（在请求处理前/后，按预期顺序）
```

---

## 检查清单 F：多租户相关变更

> 触发条件：修改涉及 TenantManager、ChangTenant、ITenantFilter、租户上下文、或任何与 F_TENANT_ID 相关的逻辑

```
□ 1. 租户上下文传递
    □ TenantManager.ChangTenant() 的调用位置是否在正确的上下文中？
    □ 如果是异步回调（EventBus/BackgroundService），租户上下文是否从请求上下文正确传递到后台线程？
    □ AsyncLocal 的租户上下文是否可能被意外覆盖？

□ 2. 并发安全
    □ 并发场景下 ChangTenant 是否线程安全？
    □ 如果操作 SqlSugarScope 单例，是否先 CopyNew() 创建独立副本？
    □ 两个不同租户的请求同时执行，日志/数据会不会串租户？

□ 3. 日志租户隔离
    □ 审计日志（SysLogEntity）的 TenantId 是否正确设置？
    □ Serilog 文件日志中的 TenantId 是否正确 enrich？（检查清单 A 的字段一致性）
    □ 平台管理员（IsAdministrator=1）是否能绕过租户过滤查看全部日志？

□ 4. 双模式兼容
    □ 独立库模式下，ChangTenant 是否正确切换数据库连接？
    □ 共享库模式下，ITenantFilter 是否正确追加 WHERE 条件？
    □ 修改是否同时兼容两种模式？

□ 5. 运行时验证
    □ 以租户 A 登录，触发操作，检查日志的 TenantId = A
    □ 以租户 B 登录，触发操作，检查日志的 TenantId = B
    □ 如果可能，模拟并发请求，检查日志不串租户
```

---

## 检查清单 G：数据库变更

> 触发条件：修改了实体类的属性（新增/删除/改名/改类型），或手动编写了 DDL 脚本

```
□ 1. 迁移脚本
    □ 实体类字段变更后，迁移脚本是否已生成？
    □ 迁移脚本是否已执行并验证表结构？（DESCRIBE table 或 INFORMATION_SCHEMA 查询）
    □ 如果使用 CodeFirst，是否确认自动同步已生效？

□ 2. 字段类型
    □ 字段类型变更（如 varchar(50) → nvarchar(max)）是否影响现有数据？
    □ 新增字段是否设置了合理的默认值？已有记录的新字段值是什么？
    □ [SugarColumn] 的 Length、IsNullable、ColumnDescription 是否正确？

□ 3. 跨模块一致性
    □ grep -rn "EntityClassName" --include="*.cs" 确认该实体类在所有模块中的引用
    □ 如果实体类被多个模块使用（如 SysLogEntity 被 Systems 模块和 OAuth 模块引用），所有模块是否兼容新字段？
    □ SqlSugar 的 IgnoreColumn / Insertable / Updateable 链是否需要调整？

□ 4. 运行时验证
    □ 启动项目，确认不报数据库异常
    □ 执行一条 INSERT + SELECT，确认新字段正确写入和读取
```

---

## 检查清单 H：移除 / 替换操作

> 触发条件：声称移除了某个功能、替换了某个依赖、禁用了某个注册

```
□ 1. 全局搜索（必须在项目根目录执行）
    □ 搜索被移除的类名：grep -rn "RemovedClassName" --include="*.cs" .
    □ 搜索被移除的方法名：grep -rn "RemovedMethodName" --include="*.cs" .
    □ 搜索被移除的配置键：grep -rn "RemovedConfigKey" --include="*.cs" . --include="*.json"
    □ 搜索被替换的旧依赖：grep -rn "OldPackageName" --include="*.csproj"
    □ 贴出所有搜索命令和终端输出

□ 2. 残留处理
    □ 如果搜索结果不为零，每个残留点必须标注：
      - 是有意保留（说明理由）
      - 还是遗漏（立即修复）
    □ using 引用是否清理？（using OldNamespace; 在移除旧依赖后应删除）

□ 3. 替换验证（如果是替换而非移除）
    □ 新依赖是否在 DI 容器中正确注册？
    □ 新依赖的 API 是否与旧依赖兼容？（方法名、参数、返回类型）
    □ 运行时验证：调用相关功能，确认新依赖生效

□ 4. 旧残留清理（如日志目录、缓存文件等）
    □ 旧日志目录是否可安全删除？（确认来源后决定）
    □ 是否需要在部署文档中注明清理步骤？
```

---

## 附录：各清单对应的 PR 模板片段

将以下内容嵌入 PR/MR 模板，工程师提交 PR 时按实际变更类型勾选：

```markdown
## 铁律自检

变更类型（勾选适用项）：
- [ ] A 日志字段变更
- [ ] B async void 修复
- [ ] C 配置变更
- [ ] D 接口签名变更
- [ ] E 中间件/Filter 变更
- [ ] F 多租户相关变更
- [ ] G 数据库变更
- [ ] H 移除/替换操作

影响等级：S / A / B / C（不确定时按更高级别）

验证级别：L0 / L1 / L2 / L3 / L4

交付物：
- [ ] 代码 before/after 已贴
- [ ] 外部依赖已核实（定义位置 + 实际值）
- [ ] grep 已执行（从根目录，附终端输出）
- [ ] 编译通过（error CS = 0，附输出）
- [ ] 运行时验证已执行（附日志/API响应/DB记录）

禁止用语自查：无"已确认""无需修复""编译通过""后续考虑"等表述
```
