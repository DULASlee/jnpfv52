# NG-0 证据 7/11 — Aspire 运行架构评估

**原则**：Aspire 是候选运行/开发编排基础设施，**不是微服务前提**。评估 Aspire 承担哪些职责、哪些职责留在领域架构层。

## 1. 评估矩阵（Aspire 能力 × JNPF Next 需求）

| Aspire 能力 | JNPF Next 需求匹配 | 结论 |
|------------|-------------------|------|
| **AppHost 编排** | Modular Monolith 单进程 + 可选服务 → AppHost 声明式启动 | ✅ 适配（进程模型与形态 A/B 兼容） |
| Service Discovery | 进程内调用为主（A 形态）→ 发现机制需求低；独立服务（File/Log）需要 | ⚠️ 形态 B 时才充分使用 |
| Health Checks | 平台健康度（DB/Redis/外部 LLM 依赖） | ✅ 高价值（现无统一健康面） |
| OpenTelemetry | 日志三族写放大（DB-3）→ 结构化遥测替换文本日志 | ✅ **高价值**——Log 域独立存储的载体 |
| Dashboard | 本地开发可视化（连接/日志/指标） | ✅ 开发体验提升 |
| Redis | 缓存（权限快照/字典缓存——DB-3 §5 建议） | ✅ 权限快照的载体（与 Aspire 无关，但 AppHost 管理连接） |
| Message Broker | 事件化（Workflow/AI 事件）——现为出箱表（SYS_EVENT_OUTBOX_MESSAGE） | ⚠️ **不急于引入**：先出箱表+进程内总线，Broker 是形态 C 的触发条件 |
| Database | SQL Server 为主（289 表）——多库兼容（DB-1） | ✅ AppHost 管理连接串/迁移 |
| 本地开发体验 | 一键起（AppHost） | ✅ |
| Integration Testing | 服务级集成测试 | ✅（与现有 dotnet test 分层） |
| Deployment 模型 | 容器化（现状 docker-compose） | ✅ 演进兼容 |

## 2. 职责划分裁决

| 职责 | 归属 | 理由 |
|------|------|------|
| 进程编排/连接管理/本地开发 | **Aspire（AppHost）** | 声明式、多依赖管理 |
| 模块边界/接口契约/事务边界 | **领域架构层** | 证据 5 的 12 域边界与 Aspire 无关 |
| 事件传递 | **先进程内总线+出箱表**，Broker 延后 | 形态 A 不需要分布式消息；出箱表是现成种子（证据 5 D8） |
| 缓存 | **Redis（AppHost 管理）** | 权限快照/字典是硬需求（DB-3 §5） |
| 遥测 | **OpenTelemetry（Aspire 集成）** | 替代三族文本日志写放大（DB-3 §4） |
| 数据访问/租户过滤/条件注入 | **领域架构层**（P0-B 规格 + S2 设计包） | Aspire 不承载业务语义 |

## 3. 结论

1. **Aspire 采用**：作为 NG-1 起即引入的开发编排 + 遥测底座（与形态无关，Modular Monolith 同样受益）；
2. **不采用的**：Message Broker 首期不引入（出箱表+进程内总线先行）；Service Discovery 在形态 A 下仅作连接配置；
3. **触发条件**（何时升级 Broker/独立服务）：权限快照上线 → Identity API 化 → 动态表注册表 → 按域剥离（证据 6 §3 顺序一致）；
4. Aspire 不改变「先数据库、后领域、再形态」的 NG-0 分析顺序。
