# JNPF V5.2 架构骨架

> 来源：graph.json (1497 nodes, 1616 edges) + 后端代码 AST 提取 + ADR 档案
> 用途：DKEE 知识图谱 — 架构骨架层
> 更新日期：2026-06-11

---

## 0. 部署架构声明（最关键）

**JNPF V5.2 是模块化单体架构，不是微服务。**

- 51 个 C# 项目编译为单一部署单元
- 入口：`JNPF.API.Entry`（主应用 :5000）+ `JNPF.OA.API.Entry`（OA :5001）
- 模块间通过 **DI 接口** 通信，数据库**共享**（通过租户隔离 + 模块表前缀区分）
- 部署形态：单一 Docker 容器，30 秒启动

> 架构决策：ADR-016 — 逻辑微服务 + 物理单体。不拆微服务，AI 升维不延迟。

---

## 1. 分层架构

```
application/                          ← 入口层
├── JNPF.API.Entry/                   ← 主应用 (:5000)
└── JNPF.OA.API.Entry/                ← OA 应用 (:5001, 禁用)

modularity/                           ← 业务模块层（19 个模块）
├── Base/       Common/      Systems/     OAuth/
├── Message/    WorkFlow/    VisualDev/   TaskScheduler/
├── CodeGen/    VisualData/  Extend/      Apps/
├── InteAssistant/  ZxDev/  SubDev/

infrastructure/                       ← 基础设施层（4 个模块）
├── RabbitMQ EventBus / WebSockets / Thirdparty / CollectiveOAuth

framework/                            ← 框架层（12 个模块）
├── JNPF core / JWT / SqlSugar / Dapper / Mapster / Serilog
├── CodeAnalysis / Xunit
```

**层间规则：**
- 上层可依赖下层，下层不可依赖上层
- 业务模块之间通过 DI 接口通信，不直接引用实现
- 框架层提供抽象接口（`ITenantFilter`、`IEventBus`、`ICache`），基础设施层提供具体实现

---

## 2. 中间件管线（15 步，按顺序）

| # | 中间件 | 职责 |
|---|--------|------|
| 1 | UnifyResult | 统一响应包装入口 |
| 2 | StaticFiles | 静态文件服务 |
| 3 | Senparc | 微信 SDK 中间件 |
| 4 | WebSocket | WebSocket 连接升级 |
| 5 | TraceId | TraceId/UserId/TenantId 注入 LogContext |
| 6 | Routing | 路由匹配 |
| 7 | CORS | 跨域策略 |
| 8 | Auth | JWT 认证 |
| 9 | Schedule | 定时任务调度 |
| 10 | Knife4jUI | API 文档 UI (/newapi) |
| 11 | Inject | 请求级依赖注入 |
| 12 | WebSocketManager | WebSocket 消息路由 |
| 13 | Endpoints | 端点执行 |
| 14 | SwaggerWarmup | Swagger 预热 |
| 15 | FriendlyExceptionFilter | 全局异常 → RESTfulResult |

---

## 3. API 层架构

### 3.1 Dynamic API Controller

- **118 个 Service 类** 实现 `IDynamicApiController`
- **0 个传统 Controller**，0 个 Minimal API
- 路由自动生成：`{ClassName}/{MethodName}`
- 响应自动包装：`RESTfulResult<T>` — `{ code, msg, data, extras, timestamp }`

### 3.2 异常处理

- `Oops.Bah("message")` → HTTP 200 + 业务错误码（用户可读）
- `Oops.Oh("message")` → HTTP 500 + 系统错误（用户不可读）
- 全局过滤：`FriendlyExceptionFilter` → `LogExceptionHandler` → EventBus 发布
- Oops 使用统计：Oh() 1008 次（系统），Bah() 8 次（业务）→ 比例失衡警告

### 3.3 OpenAPI Gateway

- 认证方式：AppId + HMACSHA256 签名
- `DataInterfaceService`：动态数据接口，支持参数化 SQL
- `OpenDataService`：外部 API 网关（Phase2 S4，Planned）

---

## 4. 数据访问架构

### 4.1 SqlSugar ORM

| 配置项 | 值 |
|--------|-----|
| 数据库 | SQL Server |
| IsAutoCloseConnection | true |
| CommandTimeOut | 30s |
| SqlServerCodeFirstNvarchar | true |
| AOP | MiniProfiler SQL 日志 + 慢 SQL 检测 (>1000ms Warning) + Serilog 错误日志 |

### 4.2 仓储模式

- `ISqlSugarRepository`：Scoped 生命周期
- `SqlSugarRepository`：通用 CRUD 实现
- `SqlSugarUnitOfWork`：事务管理
- 软删除：无全局过滤器配置（需手动过滤 `DeleteMark == null`）

### 4.3 审计字段自动填充

- 方式：`DataExecuting` 统一委托（非 SqlSugar AOP 全局自动填充）
- 业务 Service 调用 `entity.Creator()` / `entity.LastModify()` 填充
- `ConfigureGlobalDataExecuting` 统一注册入口，防止覆写
- 填充字段：`CreateTime`、`CreateUserId`、`TenantId`（Insert）等

---

## 5. 租户隔离架构

### 5.1 隔离模式

- 当前：`MultiTenancy=false`（多租户关闭）
- 模式：SCHEMA 隔离（`type=1`）
- 列级过滤：`ITenantFilter`（列 `F_TENANT_ID`），注册在 3 个位置

### 5.2 TenantContext 服务族

```
ITenantContext       → 当前租户上下文
TenantResolver       → 4 级回落解析：
  ① JWT claim
  ② Header (X-Tenant-Id)
  ③ QueryString (?tenantId)
  ④ Default Tenant
FallbackTenantResolver → 回落逻辑实现
```

### 5.3 已知风险

- `QueryFilter.Clear()` 风险：清除所有全局过滤器后重新添加租户过滤器（line 57），中间窗口期无过滤
- `Updateable/Deleteable` 不自动过滤 TenantId — 必须显式 `.Where(x => x.TenantId == tenantId)`
- 子查询不自动应用 `ITenantFilter`

---

## 6. 事件驱动架构

### 6.1 EventBus

| 传输 | 实现 | 模式 |
|------|------|------|
| 内存 | Channel（进程内） | 默认 |
| RabbitMQ | RabbitMQ.Client 6.8.1 | 跨进程 |
| Redis | CSRedisCore 3.8.670 | 可选 |
| Kafka | — | 可选 |

### 6.2 Outbox 模式

```
DB Transaction → EventOutboxMessage 写入
           → Dispatcher (Channel.Writer 实时唤醒 + 30s 轮询 BackgroundService)
           → Polly Retry (指数退避 2^n s + 断路器)
           → Idempotency (复合主键: EventId + HandlerName)
           → DeadLetter (失败最终归宿)
```

### 6.3 EventOutboxMessage 表

| 字段 | 说明 |
|------|------|
| F_ID | 主键 |
| F_EVENT_NAME | 事件名称 |
| F_EVENT_PAYLOAD | 事件负载（JSON） |
| F_STATUS | Pending / Processing / Completed / DeadLetter |

### 6.4 可靠性保证

- 事务原子性：业务操作 + Outbox 写入在同一 DB 事务
- 优雅关闭：Drain + 30s 超时（ADR-015）
- 幂等：ProcessedEvent 复合主键去重
- 死信 API：手动重放失败的 Outbox 消息

---

## 7. 缓存架构

### 7.1 接口抽象

- `ICache` 接口 → `RedisCache`（CSRedis，ISingleton）/ `MemoryCache`（ISingleton）
- 配置文件：`Cache.json` — 默认 MemoryCache，Redis 可选，PoolSize=500，DB=7

### 7.2 缓存 Key 模式（14 种）

`user`、`menu`、`permission`、`datascope`、`vercode`、`billrule`、
`online`、`position`、`role`、`visualdev`、`timerjob`、`schedule`、
`integrate`、`tenant`

### 7.3 已知风险

- 无缓存穿透保护（无 Bloom Filter）
- 无缓存雪崩保护（无互斥锁、随机 TTL 抖动）
- 权限变更后缓存失效不及时（H6 漏洞：角色变更后旧权限缓存残留）

---

## 8. 安全架构

### 8.1 认证

- JWT 认证：`JwtHandler` + `JwtEncryption`
- JWT 签名密钥：已从硬编码迁移至 `secrets.json`（JWT Key Migration，ADR-009）
- Token 无撤销机制：修改密码/禁用用户后旧 Token 仍有效

### 8.2 授权

- RBAC 核心链路：`BASE_USER → BASE_USER_RELATION → BASE_ROLE/BASE_ORGANIZE → BASE_AUTHORIZE → BASE_MODULE/BASE_MODULE_BUTTON`
- 数据权限：`IUserManager.DataScope` — 组织级数据权限注入 SqlSugar Conditions
- 已知漏洞：`JwtHandler.AuthorizeHandleAsync` 对所有权请求返回 true（权限检查被注释掉）

### 8.3 文件安全

- `FileService`：统一文件上传/下载（`/api/file/Uploader`）
- `FilePathSecurityHelper`：路径穿越防护
- `IFileManager`：多后端存储抽象（Local / Minio / Aliyun / QCloud / Qiniu / HuaweiCloud）

### 8.4 已知安全风险（3 Critical + 2 High）

| 风险 | 文件 | 严重度 |
|------|------|--------|
| SQL Injection | ScreenDataSourceService.cs:186 — 原始用户 SQL 直接执行 | CRITICAL |
| SQL Injection + DROP TABLE | ConfigController.cs:290 — 字符串拼接 DROP TABLE | CRITICAL |
| JWT 权限绕过 | JwtHandler.cs — AuthorizeHandleAsync 返回 true（权限检查注释） | CRITICAL |
| Swagger/Knife4j 生产未关闭 | 生产环境 `/newapi` 仍可访问 | HIGH |
| 无 Token 撤销 | 密码修改后旧 Token 有效期持续 | HIGH |

---

## 9. 依赖注入架构

### 9.1 注册统计

| 生命周期 | 数量 |
|----------|------|
| ITransient | 124 |
| IScoped | 4 |
| ISingleton | 11 |
| 手动注册 | 21（Startup.ConfigureServices） |

### 9.2 两层 DI

- **框架层**：Convention Scanning（自动扫描注册）
- **Host 层**：Explicit Registration（Startup 显式注册）

### 9.3 反模式

- **Service Locator**：86 处 `App.GetService<T>()` 跨 31 个文件
- 异常吞没：85 处 `catch(Exception)` 无变量，203 处有变量 → 潜在静默失败

---

## 10. 定时任务

- 基于 Cron 的 `Schedule Task Management`
- `DbJobPersistence`：任务持久化到 `BASE_TIMETASK` 表
- `CancellationToken` 覆盖率：18%（244/1351 async 方法）

---

## 11. WebSocket / 实时通信

- 协议：原生 WebSocket（非 SignalR），`/api/message/websocket` 端点
- `IMHandler`：消息处理器
- 消息下发服务：`MessageDeliveryService` → 钉钉/企业微信/短信/邮件（Phase2 S2，Planned）

---

## 12. 前端服务端口

| 服务 | 端口 | 技术栈 |
|------|------|--------|
| PC 管理后台 | :3100 | Vue3 + Vite4 + AntDV + WindiCSS |
| 数字大屏 | :3102 | Vue3 + Vite4 + DataV + ECharts |
| 移动端 H5 | 代理模式 | UniApp (Vue3) |
| 文件预览 | :30090 | kkFileView / YoZo |
| Univer 报表 | :32000 (API) + :8200 (Static) | 独立服务 |

---

## 13. 第三方依赖关键版本

| 包 | 版本 | 用途 |
|----|------|------|
| SqlSugarCore | 5.1.4.140 | ORM |
| Mapster | 7.4.0 | 对象映射 |
| Serilog | 8.0.3 | 结构化日志 |
| RabbitMQ.Client | 6.8.1 | 消息队列 |
| CSRedisCore | 3.8.670 | Redis 客户端 |
| Furion | — | 底层 .NET 脚手架 |

---

## 14. CI/CD 质量门禁

```
Build → Roslyn Analyzer Gate (JNPF001-JNPF006) → Test → Security Scan → Health Check
```

Roslyn Analyzers：6 条架构约束规则（JNPF001 ~ JNPF006），在编译期强制 Layer 合规性。

---

## 附录：与 ABP/MASA 对标

| 维度 | JNPF V5.2 | ABP vNext | MASA | Spring Cloud |
|------|-----------|-----------|------|-------------|
| 架构风格 | 模块化单体 | 模块化单体 / 微服务可选 | 微服务优先 | 微服务 |
| API 生成 | DynamicApiController (118 svc) | Auto API Controllers | Minimal API | @RestController |
| ORM | SqlSugar | EF Core | EF Core / Dapper | JPA / MyBatis |
| 租户隔离 | ITenantFilter (单列) | IMultiTenant (多策略) | MultiTenant | — |
| 事件总线 | Memory/RabbitMQ/Redis/Kafka | RabbitMQ/Kafka/Rebus | MASA Dapr | Spring Cloud Stream |
| Outbox | ✅ 自研 | ✅ EF Core Outbox | ✅ Dapr PubSub | ✅ Debezium |
| 成熟度评分 | 1.7/5 | 3.7/5 | 4.3/5 | 4.5/5 |
