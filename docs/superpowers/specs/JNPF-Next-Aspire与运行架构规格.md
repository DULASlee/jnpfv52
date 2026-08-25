# JNPF-Next Aspire 与运行架构规格 v1.0（NG-0 产物 5/5）

**日期**：2026-08-25 ｜ **依据**：NG-0 证据 7（aspire-evaluation）+ 证据 6（形态裁决）+ 证据 9（迁移波次）
**状态**：设计规格（只读）

## 1. 定位裁决

> **Aspire = 下一代运行/开发编排基础设施（工具层）；不是微服务架构的前提。**
> 领域架构层（12 域边界/事务/契约）与 Aspire 无关——Aspire 服务领域架构，不替代领域架构。

## 2. 职责划分

| 职责 | 归属 | 引入时点 |
|------|------|---------|
| 进程编排/连接管理/一键起（AppHost） | Aspire | NG-1（原型即用） |
| OpenTelemetry 遥测（替代三族文本日志） | Aspire + D9 | NG-1（与日志域分离并行） |
| 健康检查（DB/Redis/LLM 依赖） | Aspire | NG-1 |
| 本地 Dashboard | Aspire | NG-1 |
| 缓存（权限快照/字典） | Redis（AppHost 管理） | NG-1（权限快照原型） |
| 事件传递 | **进程内总线 + 出箱表**（非 Broker） | NG-3（Workflow 事件化 W5） |
| 消息 Broker | **不引入**（触发条件：形态 C） | 远期 |
| 数据访问/租户过滤/条件注入 | 领域架构层（数据访问规格） | 不归 Aspire |
| 部署模型 | 容器化演进（docker-compose → AppHost 部署） | NG-3+ |

## 3. 运行拓扑（NG-1 目标形态）

```text
Aspire AppHost
 ├── JNPF.Next.Api（Modular Monolith 单进程）
 ├── SQL Server（Legacy 库 ZXAF_V1_DevTest1 只读 + Next 库演进）
 ├── Redis（权限快照/字典缓存）
 ├── Seq/OTel Collector（日志族独立——D9）
 └── (可选) File 独立进程原型（D7）
```

## 4. 与现有资产的衔接

| 现有 | Next 处置 |
|------|----------|
| start-dev.ps1（3100/5000 等端口编排） | NG-1 双轨并存（Legacy 不拆）；AppHost 管理 Next 侧 |
| docker-compose（staging/production） | 部署模型演进兼容 |
| 日志三族（base_sys_log/base_api_log/BASE_AI_CALL_LOG） | OTel 结构化 + 独立存储（写放大隔离） |
| SYS_EVENT_OUTBOX_MESSAGE（出箱表） | 进程内事件总线的持久化基础（KEEP） |

## 5. 待裁决（NG-1 输入）

| # | 事项 | 建议 |
|---|------|------|
| AS-D1 | 日志独立存储选型（Seq/ES/ClickHouse） | NG-1 原型对比（量级：base_sys_log 12615/月级观察） |
| AS-D2 | Redis 引入时点（权限快照原型捆绑） | 是 |
| AS-D3 | File 独立进程原型是否进 NG-1 | 是（零依赖验证——证据 6 B 形态） |
| AS-D4 | AppHost 与 start-dev 的并存策略 | 双轨（Legacy 不动） |
