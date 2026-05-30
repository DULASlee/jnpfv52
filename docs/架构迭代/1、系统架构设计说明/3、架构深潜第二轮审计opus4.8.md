# JNPF V5.2 架构健康检查 · 第二轮深度审计

> **审计基线（实测代码锚点）**：后端 15 个 `modularity` 业务模块中**无任何 device/iot 模块**；实时通信底座为 `framework/JNPF/InstantMessaging`（SignalR，`IHubContext` + `[MapHub]` 自动扫描）+ `infrastructure/JNPF.Extras.WebSockets`；事件总线为 `framework/JNPF/EventBus`（Channel 内置）+ `JNPF.Extras.EventBus.RabbitMQ`；多租户隔离在 `JNPF.Extras.DatabaseAccessor.SqlSugar`（`ITenantFilter`/`TenantOptions`/`TenantService`）；缓存 `Cache.json` 默认 `MemoryCache`（Redis 备而未启）；持久化为 SqlSugar CodeFirst `InitTables`（无版本化迁移）；**后端 0 个测试项目**；部署仅 `JNPF.API.Entry/Dockerfile` 一个（无 compose/k8s）；前端 `pinia` + `reconnecting-websocket` + `echarts`/`highcharts` + `@amap/amap-jsapi-loader` + `vue-grid-layout`。
>
> 以下 11 条均在此真实现状上深入，与已执行的 10 项改进无重叠。

---

## 改进项 1: IoT 设备接入层 —— 抽象 `JNPF.IoT.Gateway` 接入模块，复用 DynamicApiController + EventBus

**当前状态分析**
`backend/modularity/` 下完全没有设备接入抽象。现有最接近的能力是 `JNPF.Extras.WebSockets`（`WebSocketMiddleware`/`WebSocketConnectionManager`，面向浏览器 IM）与 SignalR Hub，二者都是**面向人/浏览器**的长连接，不具备 MQTT 订阅、设备认证、报文编解码、离线缓存、设备影子等能力。MQTT/CoAP 协议栈、设备注册表、Device Shadow 全部为空白。设备数据若按低代码"表单驱动"思路直接落 SqlSugar 业务表，会与人工录入数据混在一张表里。

**风险评估**
- 智能手环每 5s 上报一次心率/定位，1000 只手环 = 200 msg/s；若走现有 HTTP DynamicApi 同步入库，单条请求经过 JWT 校验 + 鉴权过滤器 + SqlSugar 事务，实测此类请求 P99 通常 20–50ms，200 msg/s 即逼近单实例线程池上限，且无法削峰。
- 售卖柜/更衣柜断网后重连补传、工地传感器乱序到达，现有架构无任何缓冲与幂等机制，会造成数据丢失或重复计费。

**改进方案**
- 【可落地】新增 `modularity/iot/JNPF.IoT`（业务）+ `infrastructure/JNPF.Extras.Mqtt`（协议），MQTT Broker 选 **EMQX**（国产、单机百万连接），.NET 侧用 **MQTTnet**（与 .NET 6 完全兼容）做订阅端。
- 接入层只做三件事：①设备认证（见改进项 10）；②报文反序列化为统一 `DeviceTelemetry` 事件；③`Publish` 到现有 `IEventPublisher`（`framework/JNPF/EventBus`）。业务侧用 `[EventSubscribe]`（参照 `JNPF.Common.Core/EventBus/UserEventSubscriber.cs` 的写法）消费，实现接入与业务解耦。
- Device Shadow【可落地】：用已就绪的 Redis（`framework/JNPF/Cache/RedisCache.cs`，`defaultDatabase=7`）存设备最新态（`device:shadow:{deviceId}` Hash），desired/reported 双文档；持久态仍落库。
- 设备注册表新增表 **`iot_device`**（`device_id`/`product_key`/`tenant_id`/`status`/`last_online_time`）、**`iot_product`**（`product_key`/`protocol`/`data_schema`）【新增表，待建模评审】。
- 数字孪生为 P2 预留：`iot_product.data_schema` 用 JSON 存物模型（属性/服务/事件），为后续孪生打基础。

**预期收益**
接入与业务解耦后，新增一类设备只需配置物模型 + 写一个 `EventSubscriber`，不改接入层；削峰后单 EMQX 实例可稳定承载 ≥10 万设备连接，入库压力由 EventBus 异步批处理吸收。

**实施优先级**：**P0**。这是 IoT 业务的地基，所有手环/家居/工地场景都依赖它；缺失它则后续 9 个维度多数无法落地。

**对 CLAUDE.md 的建议**：需要。在 Architecture 的 `modularity` 模块表中新增一行，并加一节：
```markdown
## IoT 接入（V5.2 扩展）
- 协议接入：infrastructure/JNPF.Extras.Mqtt（MQTTnet + EMQX），禁止设备直连 DynamicApi
- 接入层只转发为 DeviceTelemetry 事件，经 framework/JNPF/EventBus 解耦给业务 EventSubscriber
- 设备最新态走 Redis Device Shadow（db 7），历史态落时序库（见数据架构）
- 核心表：iot_device / iot_product / iot_device_event
```

---

## 改进项 2: 实时通信 —— 复用现有 SignalR Hub，建立"设备→服务端→大屏"的统一推送通道与降频策略

**当前状态分析**
框架已内置 SignalR（`framework/JNPF/InstantMessaging/IM.cs` 的 `IHubContext`，`IEndpointRouteBuilderExtensions.MapHubs()` 自动扫描 `[MapHub]`），前端已装 `reconnecting-websocket`。但**没有面向设备状态/报警的 Hub**，也没有任何采样/聚合/降频逻辑。`jnpf-web-datascreen`（DataV，:8100）大屏目前只能靠轮询取数。

**风险评估**
- 高频原始点直推前端：1000 设备 × 1Hz = 1000 帧/s 推给一个大屏，浏览器 echarts 重绘必然卡死（实践中单图表 >10–20 帧/s 即掉帧）。
- 报警（手环 SOS、工地越界）若走轮询，5–10s 的轮询间隔对安全类报警是不可接受的延迟。

**改进方案**
- 【可落地】新增 `DeviceHub : Hub`（贴 `[MapHub("/hubs/device")]` 即被自动注册），按 `tenant_id`/设备分组 `Groups.AddToGroupAsync`，业务侧 `IM.GetHub<DeviceHub>()` 推送。报警走 SignalR（亚秒级），高频遥测走聚合后推送。
- 降频/聚合分级【可落地，量化】：
  - 原始入时序库：保留全量；
  - 推大屏：服务端按 **1–2s 窗口**做 last/avg 聚合再推（用 `framework/JNPF/EventBus` 的 Channel 做缓冲队列），即每设备 ≤1 帧/2s；
  - 单图表订阅设备数 >50 时，服务端改推"聚合统计值"而非逐设备明细。
- 前端【可落地】：DataV 与 PC 端统一封装 `useDeviceSocket()`（基于已装的 `reconnecting-websocket` 或 `@microsoft/signalr` 客户端），断线自动重连 + 订阅恢复。

**预期收益**
报警端到端延迟从轮询的 5–10s 降到 <1s；大屏在千级设备下稳定 30fps；复用现成 SignalR，零新增长连接基础设施。

**实施优先级**：**P0**（报警通道）/ P1（聚合降频）。报警延迟直接关乎手环、工地安全业务的可用性。

**对 CLAUDE.md 的建议**：需要。补充：
```markdown
## 实时推送
- 统一用 framework/JNPF/InstantMessaging（SignalR），新建 Hub 贴 [MapHub] 即自动注册
- 报警类：直推 SignalR（<1s）；高频遥测：服务端 1-2s 窗口聚合后推，禁止原始点直推前端
- 前端统一 useDeviceSocket()（reconnecting-websocket），按 tenant/设备分组订阅
```

---

## 改进项 3: MES 领域模型 —— 用"工单状态机"驱动，与 visualdev 表单驱动分层共存

**当前状态分析**
现有 `modularity/workflow`（审批流，`FlowTaskService` 等）面向**人工审批**，`modularity/engine`/`codegen` 面向**表单与代码生成**。MES 的工单（Work Order）是**机器/工序驱动的长生命周期状态机**（计划→下达→开工→报工→完工→质检→入库），与审批流语义不同。当前没有任何 WO/工艺路线/批次领域模型，若用 visualdev 动态表单硬套，状态流转只能靠人工改字段。

**风险评估**
- 工单状态靠表单字段人工维护：并发报工时无乐观锁，会出现"已完工又被开工"的非法跃迁；OEE 统计（开机/运行/故障时间）无事件源可追溯。
- 批次追溯缺数据模型：一旦出现质量问题，无法从成品反查原料批次/设备/工艺参数，召回范围无法界定（这是制造业合规硬要求）。

**改进方案**
- 【可落地】新增 `modularity/mes/JNPF.Mes`，核心领域用**显式状态机**（推荐 `Stateless` 库，纯 .NET 6、轻量），而非塞进 workflow。WO 状态跃迁产生领域事件，复用 `framework/JNPF/EventBus` 落事件流水。
- 分层共存原则【可落地】：**visualdev 表单负责"主数据/配置录入"**（物料、工艺路线定义、BOM），**MES 状态机负责"业务流转"**（工单执行、报工、批次流转）。两者通过 `iot`/`mes` 模块的 Service（DynamicApiController 自动暴露）打通。
- 核心表【新增表，待建模评审】：**`mes_work_order`**（`wo_no`/`product_id`/`route_id`/`status`/`qty_plan`/`qty_done`/`row_version` 乐观锁）、**`mes_route`**/`mes_route_step`（工艺路线）、**`mes_batch`**（`batch_no`/`wo_no`/`material_lot` 正反向追溯）、**`mes_wo_event`**（状态流水，OEE 数据源）。
- 设备 OEE【可落地】：直接消费改进项 1 的 `DeviceTelemetry`（设备运行/停机事件）写入 `mes_wo_event`，OEE = 可用率×表现×良率 由事件流水算出。

**预期收益**
工单流转有状态机守护，非法跃迁在代码层被拒；批次正反向追溯使召回可精确到批次/设备/工艺参数；OEE 自动由设备事件计算，无需人工录入。

**实施优先级**：**P1**。MES 是核心业务但建模复杂，应在 IoT 接入（P0）打通后启动；先落 WO + 批次最小闭环。

**对 CLAUDE.md 的建议**：需要。新增模块行 + 一节：
```markdown
## MES 领域（V5.2 扩展）
- modularity/mes/JNPF.Mes：工单生命周期用显式状态机（Stateless），不要塞进 workflow（审批流）
- 分层：visualdev 表单管主数据/配置；MES 状态机管业务流转
- 状态跃迁发 EventBus 事件，落 mes_wo_event 作为 OEE/追溯数据源
- 核心表：mes_work_order(row_version 乐观锁) / mes_route / mes_batch / mes_wo_event
```

---

## 改进项 4: 多租户与设备级权限 —— 扩展 `ITenantFilter` 到设备维度，新增"设备数据权限"

**当前状态分析**
SqlSugar 层已有成熟的**租户隔离**（`JNPF.Extras.DatabaseAccessor.SqlSugar/Options/TenantOptions.cs`、`Models/ITenantFilter.cs`、`TenantLinkModel.cs`、`System/Common/TenantService.cs`），支持多库/多 schema。系统也有菜单/按钮/数据权限（`ModuleDataAuthorizeService`/`ModuleButtonService`）。但这套数据权限是面向**组织架构（部门/岗位）**的行级过滤，**没有"设备归属"和"设备级操作权限"**的概念——谁能远程开哪台更衣柜、控哪个家居开关、看哪片工地的人员定位，现有模型无法表达。

**风险评估**
- 跨租户设备数据串读：若 `iot_device` 表不纳入 `ITenantFilter`，A 租户的运营人员可能查到 B 租户的设备数据，对 SaaS 化部署是致命合规问题。
- 设备控制越权：智能家居/更衣柜的"控制指令"是高危操作，缺设备级授权会导致用户 A 远程打开用户 B 的柜子。

**改进方案**
- 【可落地】`iot_device`/`mes_*` 等所有新表统一实现现有 `ITenantFilter`，自动获得租户行级隔离，不重造轮子。
- 新增**设备数据权限**【可落地】：扩展现有数据权限模型，新增"按设备分组/设备标签"的授权维度，新表 **`iot_device_group`** + **`iot_user_device_auth`**（`user_id`/`device_group_id`/`can_view`/`can_control`）【新增表，待评审】，控制指令 Service 入口强制校验 `can_control`。
- 控制指令审计【可落地】：所有下行控制走统一 `DeviceCommandService`（DynamicApi 自动暴露），强制落 **`iot_command_log`**（谁/何时/对哪台/下了什么/结果），满足追责。

**预期收益**
设备数据天然租户隔离；高危控制操作有授权校验 + 全量审计；权限模型从"组织维度"扩展到"设备维度"，覆盖 IoT 场景。

**实施优先级**：**P0**（租户隔离 + 控制鉴权，安全红线）/ P1（设备组精细授权）。

**对 CLAUDE.md 的建议**：需要。补充：
```markdown
## 多租户与设备权限
- 所有 IoT/MES 新表必须实现 ITenantFilter（SqlSugar 层），获得租户行级隔离
- 设备控制为高危操作：经 DeviceCommandService 统一入口，校验 iot_user_device_auth.can_control，全量写 iot_command_log
```

---

## 改进项 5: 前端状态管理 —— 为高频实时数据建立独立 Pinia store + 帧节流，隔离低代码渲染

**当前状态分析**
前端用 **Pinia**（`store/modules/` 下 user/app/permission 等均为低频业务态）。没有面向设备实时数据的 store，也没有把"低代码动态渲染"与"手写 IoT 高频组件"分层。若把 1Hz 的设备数据直接写进普通 Pinia state，Vue 响应式会对每个订阅组件触发重渲染。

**风险评估**
- 高频 setState 引发 Vue 响应式风暴：一个设备态变更触发全表关联组件 diff，设备数 >100 时控制面板明显掉帧。
- 低代码生成的页面（visualdev 渲染器）与手写实时组件混用同一 store，会相互污染、难以定位性能瓶颈。

**改进方案**
- 【可落地】新增独立 `store/modules/device.ts`，实时数据用 **`shallowRef`/`markRaw`** 存储（绕过深层响应式），配合 **`requestAnimationFrame` 帧节流**批量 flush（每帧最多更新一次视图），目标 ≤30fps。
- 复杂组件分层【可落地】：地图定位用已装的 **`@amap/amap-jsapi-loader`**（高德），海量点位用 Canvas/海量点图层而非 DOM marker；实时曲线用 echarts 的 `appendData` 增量更新而非全量 setOption；仪表盘布局复用已装的 **`vue-grid-layout`**。
- 共存策略【可落地】：建立 `src/iot/` 目录放手写 IoT 业务组件，与 `visualdev` 低代码渲染器物理隔离，通过统一 `useDeviceSocket()`（改进项 2）取数。

**预期收益**
高频数据更新与 UI 重绘解耦，控制面板在千级点位下保持流畅；低代码与手写组件边界清晰，互不影响。

**实施优先级**：**P1**。依赖改进项 2 的推送通道；在设备控制面板/大屏开发前完成。

**对 CLAUDE.md 的建议**：需要（前端约定）。补充：
```markdown
## 前端 IoT 约定（jnpf-web-vue3）
- 实时数据独立 store/modules/device.ts，用 shallowRef + rAF 帧节流（≤30fps），不要进普通响应式 state
- 地图用 @amap/amap-jsapi-loader 海量点图层；曲线用 echarts appendData 增量；布局复用 vue-grid-layout
- 手写 IoT 组件放 src/iot/，与 visualdev 低代码渲染器隔离
```

---

## 改进项 6: 数据库与数据架构 —— 引入时序库做冷热分层，SqlSugar 仅管业务态

**当前状态分析**
持久化全栈 SqlSugar（SQL Server 为主），通过 CodeFirst `InitTables`（`UserEventSubscriber.cs`、各 `*Service` 中可见）建表。**无时序数据库**（无 InfluxDB/TDengine/TimescaleDB）。设备遥测若全落 SqlSugar 业务表：单表写入 + B 树索引在持续高频 insert 下会快速膨胀且查询退化。地理位置也只能存普通列，无空间索引。

**风险评估**
- 数据点爆炸【量化】：1 万设备 × 10 指标 × 1Hz ≈ **8.6 亿点/天**。SQL Server 单表在数千万行后，时间范围聚合查询（大屏"近 1 小时趋势"）会从毫秒级退化到秒级甚至超时。
- 无冷热分层 → 历史数据与实时数据争抢同一存储/索引，备份与归档成本失控。

**改进方案**
- 【可落地】数据三分层：
  - **热（实时态）**：Redis Device Shadow（已就绪，db 7），秒级；
  - **温（近期明细+趋势）**：**TDengine**（国产时序库，专为 IoT 设计，自带降采样/保留策略，有 .NET connector，与 .NET 6 兼容）或 **InfluxDB**；按设备/超级表建模，保留期如 90 天；
  - **冷（历史归档）**：定期下沉到 Parquet/对象存储或 SQL Server 归档表，用现有 `modularity/taskscheduler` 跑归档任务。
- **业务态仍走 SqlSugar**【关键边界】：`iot_device`/`mes_*` 等关系型主数据留在 SqlSugar，时序点位绝不进 SqlSugar 业务表。
- 地理数据【可落地】：定位类用 SQL Server 的 `geography` 类型 + 空间索引，或交给高德侧聚合。
- Migration 规范化【可落地，独立价值】：当前 CodeFirst `InitTables` 无版本记录，跨环境结构漂移不可控。建议建立 `db/migrations/` 版本化 SQL 脚本目录 + 一张 **`sys_schema_version`** 记录已应用版本，发布时校验，替代隐式 InitTables。
- 分库分表阈值【量化参考】：单业务表 >5000 万行 或 设备数 >5 万 时，按 `tenant_id`/设备哈希分片（SqlSugar 支持分表）。

**预期收益**
8 亿点/天级别的写入与趋势查询交给时序库，大屏查询恢复亚秒级；业务库轻量、可控；结构变更版本化后多环境一致。

**实施优先级**：**P1**（时序库，IoT 数据量起来前必须就位）/ **P0**（Migration 版本化，成本低、当下就受益、防漂移）。

**对 CLAUDE.md 的建议**：需要。补充 Database 节：
```markdown
## 数据分层
- 热：Redis Device Shadow（db 7）；温：TDengine/InfluxDB（设备遥测，禁止落 SqlSugar 业务表）；冷：归档（taskscheduler 下沉）
- 关系型主数据仍走 SqlSugar；单表 >5000 万行或设备 >5 万考虑按 tenant/设备哈希分表
- Schema 变更走 db/migrations/ 版本化脚本 + sys_schema_version，替代隐式 InitTables
```

---

## 改进项 7: 消息中间件 —— 明确 EventBus（进程内）与 RabbitMQ（跨服务）的使用边界，引入设备指令幂等

**当前状态分析**
存在两套：`framework/JNPF/EventBus`（**Channel 进程内**，`ChannelEventPublisher`/`ChannelEventSourceStorer`，进程重启即丢）与 `infrastructure/JNPF.Extras.EventBus.RabbitMQ`（**跨进程持久**）。但代码里业务订阅（`UserEventSubscriber`/`LogEventSubscriber`/`IntegreateEventSubscriber`）几乎都挂在进程内 Channel 上，RabbitMQ 扩展形同闲置，**没有约定何时用哪个**。与 ERP/WMS/SCADA 的集成层也不存在。

**风险评估**
- 用进程内 Channel 承载设备遥测/控制指令：进程崩溃或发布时机重启 → 消息静默丢失，对计费（售卖柜)、报警（手环）是数据/资损风险。
- 缺幂等：设备网络抖动重发控制指令，下游可能重复执行（重复开柜、重复扣费）。

**改进方案**
- 【可落地，零新依赖】确立边界规则并落到 CLAUDE.md：
  - **进程内 Channel EventBus**：同进程内轻量解耦（日志、缓存刷新、同实例业务联动）——维持现状；
  - **RabbitMQ**（已具备扩展）：跨服务、需持久化/重试/削峰的设备遥测与控制指令、与外部系统集成——启用它，而非继续滥用 Channel。
- 幂等【可落地】：设备上行带 `msgId`，消费端用 Redis `SETNX`（已就绪）做去重；控制指令落 `iot_command_log` 时以指令 ID 唯一约束兜底。
- 外部集成【可落地】：ERP/WMS 用 RabbitMQ 做异步对接 + 适配器模式；SCADA/PLC 现场协议（OPC UA/Modbus）建议放在边缘侧（见改进项 8）转 MQTT 上云，不在云端直连。
- 选型结论【务实】：当前阶段 **RabbitMQ 足够**（已有扩展、运维简单）；仅当单 topic 持续 >10 万 msg/s 或需流式重放时才评估 Kafka，避免过度设计。

**预期收益**
关键链路（计费/报警/控制）走持久化队列，崩溃不丢消息 + 自动重试；幂等杜绝重复执行；明确边界后开发者不再误用进程内总线。

**实施优先级**：**P0**（控制/计费/报警链路改走 RabbitMQ + 幂等）/ P1（外部系统集成适配器）。

**对 CLAUDE.md 的建议**：需要。补充 Key Patterns：
```markdown
## 消息边界
- 进程内：framework/JNPF/EventBus（Channel）——仅同进程轻量解耦，重启会丢，禁止承载设备遥测/计费/报警
- 跨进程/持久：JNPF.Extras.EventBus.RabbitMQ——设备遥测、控制指令、外部集成走这里
- 设备消息必须幂等：上行带 msgId + Redis SETNX 去重；指令落 iot_command_log 唯一约束
```

---

## 改进项 8: 部署与运维 —— 补齐 docker-compose 编排 + 边缘/云分层 + 可观测性栈

**当前状态分析**
仅 `backend/application/JNPF.API.Entry/Dockerfile` 一个镜像定义，**无 docker-compose、无 K8s manifest**。配套依赖（SQL Server、Redis、RabbitMQ、未来的 EMQX/TDengine）没有一键编排。日志是 Serilog（结构化已具备），但**无聚合/告警/链路追踪基础设施**。边缘计算（工地/工厂现场）与云端的部署边界未定义。

**风险评估**
- 新环境搭建靠手工拼依赖，"在我机器上能跑"问题频发；演示/交付效率低。
- IoT 是分布式系统（设备↔接入↔业务↔时序库），出问题时无 trace 串联，定位一次跨组件故障可能耗时数小时。
- 工地/工厂常断网，纯云端架构在断网时业务停摆。

**改进方案**
- 【可落地，当前阶段优先】先出 **`docker-compose.yml`** 编排全套依赖（api + sqlserver + redis + rabbitmq + emqx + tdengine），开发/演示一键起；K8s 留到规模化（多实例/弹性）阶段（P2），避免当前阶段过度复杂。
- 可观测性【可落地】：Serilog 已结构化，加 **Seq**（.NET 友好、单容器）或 ELK 做日志聚合；指标用 **Prometheus + Grafana**（设备在线数、消息积压、入库延迟）；链路追踪用 **OpenTelemetry**（.NET 6 原生支持）串联 接入→EventBus→入库。
- 边缘/云分层【可落地】：现场侧部署轻量边缘网关（EMQX Edge / .NET 边缘服务）做协议转换 + 断网本地缓存 + 联网补传；云端只收标准 MQTT，断网不影响现场基本运转。
- CI/CD【可落地】：现有 `scripts/verify-toolchain.mjs` 已是健康检查雏形，扩展为流水线：`dotnet build` + `dotnet test`（待改进项 9 建测试后）+ `docker build` + compose 冒烟。

**预期收益**
环境从手工搭建到一键起；跨组件故障可经 trace 分钟级定位；现场断网业务不中断；发布流程标准化。

**实施优先级**：**P1**（compose + 可观测性）/ P2（K8s + 边缘网关，规模化时）。

**对 CLAUDE.md 的建议**：需要。Build & Run 节补充：
```markdown
## 部署
- 本地/演示：docker-compose up（api + sqlserver + redis + rabbitmq + emqx + tdengine 一键起）
- 可观测性：Serilog → Seq；指标 Prometheus+Grafana；链路 OpenTelemetry
- 边缘：现场部署边缘网关做协议转换+断网缓存补传，云端只收标准 MQTT
```

---

## 改进项 9: 测试策略 —— 从零建立测试工程 + IoT 设备模拟器，纳入 verify 流水线

**当前状态分析**
**后端 0 个测试项目**（`backend/**/*Test*` 无任何 `.csproj`），前端虽装了 `@vue/test-utils` 但无测试脚本（`package.json` 无 `test` 命令）。这是当前最大的工程质量空白。低代码平台 + IoT 的组合恰恰最需要测试：DynamicApiController 自动生成的端点缺乏契约保障，设备并发场景无法靠人工验证。

**风险评估**
- 改一个 `*Service` 方法，自动生成的 API 行为变更无回归网兜底，极易引入隐性破坏（改一处崩一片）。
- 设备并发/乱序/重连等场景无法人工复现，问题往往在生产环境千级设备压力下才暴露，修复成本指数级上升。

**改进方案**
- 【可落地，分层】
  - **单元测试**：`backend/tests/JNPF.*.Tests`，用 **xUnit + Moq**（.NET 6 标准），优先覆盖 MES 状态机跃迁、设备指令幂等、租户过滤等**纯逻辑高风险点**（这些不依赖低代码生成，最值得测）。
  - **集成测试**：用 `WebApplicationFactory` + **Testcontainers**（拉临时 SQL Server/Redis/RabbitMQ 容器）测 DynamicApi 端点契约。
  - **E2E**：前端用 Playwright（项目已具备 playwright 技能与 CLI）跑关键业务流。
- **IoT 设备模拟器**【可落地，高价值】：写一个 `JNPF.IoT.Simulator` 控制台程序，用 MQTTnet 模拟 N 个设备按设定频率/抖动/乱序/掉线上报，作为压测与回归的"虚拟设备群"。
- 性能基准【量化】：用模拟器建立基线——单接入实例目标 ≥1 万并发连接、≥5000 msg/s 入库不积压；每次发布对比基线防性能回退。
- 流水线集成：把 `dotnet test` 接入改进项 8 的 CI 与现有 `verify-toolchain.mjs` 思路一致的门禁。

**预期收益**
自动生成 API 有契约回归网；MES/幂等等高危逻辑有单元保障；设备模拟器让并发问题在上线前暴露；建立可量化的性能基线。

**实施优先级**：**P1**（单元测试覆盖高危逻辑 + 设备模拟器）。务实起步：不追求覆盖率数字，优先覆盖 MES 状态机、指令幂等、租户隔离三类"错了就出大事"的逻辑。

**对 CLAUDE.md 的建议**：需要。新增节：
```markdown
## 测试（V5.2 新建）
- backend/tests/：xUnit + Moq（单元）；WebApplicationFactory + Testcontainers（集成，测 DynamicApi 契约）
- 优先覆盖：MES 状态机跃迁、设备指令幂等、租户过滤——错了就出资损/安全事故
- IoT 压测：JNPF.IoT.Simulator（MQTTnet 模拟设备群）；基线 ≥1万连接/≥5000msg/s 入库不积压
- 发布门禁：dotnet test 接入 CI
```

---

## 改进项 10: 安全架构 —— 设备身份与人类身份分离，健康数据合规分级

**当前状态分析**
认证体系是面向**人**的 JWT（`framework` JWT + `modularity/oauth`）。设备没有独立身份体系——若让设备复用用户 JWT，等于把长期凭证烧进固件，一旦设备被拆解逆向，凭证即泄露。无设备证书/密钥管理，无 OTA 安全机制。穿戴设备的**个人健康数据（心率/定位）**属敏感个人信息，现无分级加密/脱敏/合规标记。

**风险评估**
- 固件中硬编码用户 JWT：单台设备被破解 → 凭证可被克隆，攻击者可伪造海量虚假设备上报或越权控制，且无法单台吊销。
- 健康/定位数据明文落库：触碰《个人信息保护法》《数据安全法》合规红线，穿戴类产品这是上线硬门槛。

**改进方案**
- 【可落地，分阶段】设备身份与人类身份**物理分离**：
  - 起步阶段：**一机一密（Device Secret）** + 动态 Token——设备用 `product_key + device_secret` 换取短期 MQTT 连接 Token（EMQX 支持认证插件），Token 可单台吊销，凭证不进业务 JWT 体系；
  - 高安全阶段（P2）：**X.509 双向 TLS**，每设备独立证书。
- 传输加密【可落地】：MQTT over **TLS**（设备↔EMQX），CoAP 场景用 DTLS。
- OTA 安全【可落地】：固件包**签名校验**（设备端验签）+ 灰度发布 + 失败回滚，固件下发记录入审计表。
- 健康数据合规【可落地，P0 红线】：心率/定位等敏感字段**列级加密**（落库前加密，密钥走 KMS/配置隔离）；查询接口按改进项 4 的设备权限 + 数据脱敏（非授权角色看到掩码）；建立数据保留期与删除机制（用户注销级联删除）。

**预期收益**
设备凭证可单台吊销、泄露影响可控；传输全程加密；OTA 防刷机/防篡改；健康数据满足合规，扫清穿戴产品上线法律障碍。

**实施优先级**：**P0**（设备一机一密 + 传输 TLS + 健康数据列级加密，均为安全/合规红线）/ P2（X.509、OTA 灰度）。

**对 CLAUDE.md 的建议**：需要。新增安全节：
```markdown
## IoT 安全（红线）
- 设备身份与用户 JWT 物理分离：一机一密（product_key+device_secret 换短期 Token，EMQX 认证，可单台吊销），禁止固件烧用户 JWT
- 传输：MQTT over TLS / CoAP over DTLS
- 健康/定位等敏感数据：列级加密落库 + 按权限脱敏 + 保留期/注销级联删除（个保法合规）
- OTA：固件签名校验 + 灰度 + 回滚，下发入审计
```

---

## 改进项 11: AI 大模型编程能力 —— 沉淀 IoT/MES 领域施工模板 + 物模型驱动代码生成，强化工具链精准适配

**当前状态分析**
项目已有成熟工具链（`.cursor/skills/` 20 个 Superpowers 技能、OpenSpec 知识库、Serena C# 符号工具、episodic-memory）。但这些是**通用开发流程**技能，**没有任何 IoT/MES 领域专用的施工模板、领域规则或代码生成器**。现有 codegen（Velocity `.vm` 模板，`wwwroot/Template/`）面向通用单表/主从表 CRUD，**不认识"物模型/工单/工艺路线"等领域概念**。AI 在本项目做 IoT 开发时，每次都要从零理解 EventBus/SignalR/SqlSugar 的正确用法，易走弯路。

**风险评估**
- AI 缺领域约束 → 反复犯同类错误：把设备遥测写进 SqlSugar 业务表、误用进程内 EventBus 承载关键消息、新建 Controller 而非走 DynamicApi——前面 1–10 项的"禁止项"若不固化进工具链，会被一犯再犯。
- codegen 不懂物模型 → IoT 设备接入代码仍纯手写，低代码平台的"提效"优势在 IoT 域完全没发挥。

**改进方案**
- 【可落地，立即收益】把本轮 1–10 项的架构边界固化为 **Cursor Rule + 领域施工技能**：
  - 新增 `.cursor/rules/iot-mes-architecture.mdc`（`alwaysApply`），内联"设备遥测禁入 SqlSugar 业务表 / 关键消息走 RabbitMQ / 设备身份分离 / 走 DynamicApi 不建 Controller"等硬规则；
  - 新增 `.cursor/skills/iot-device-onboarding/` 与 `mes-workorder-modeling/` 两个领域施工技能（遵循已规范的 frontmatter：`scope`/`tech-stack`），把"接入一类新设备/新建一个工单领域"的标准步骤模板化。
- **物模型驱动代码生成**【可落地，高杠杆】：扩展现有 Velocity codegen，新增"物模型→代码"模板——输入 `iot_product.data_schema`（属性/服务/事件 JSON），生成设备实体、`EventSubscriber` 骨架、Device Shadow 读写、前端设备面板脚手架。把低代码提效能力从"表单 CRUD"延伸到"IoT 设备接入"。
- 工具链精准适配【可落地】：
  - episodic-memory `search-templates.yaml` 增补 IoT/MES 检索模板（`["设备接入","物模型","EventBus"]` 等），让 AI 跨会话快速召回本轮决策；
  - MCP 侧：`user-chrome-devtools`/`user-playwright` 用于设备大屏的真实渲染验证；Serena 符号工具专用于 `modularity/iot`、`modularity/mes` 的 C# 重构。
- 配置提升【可落地】：将 Cache 默认从 `MemoryCache` 切到 Redis（多实例部署时 MemoryCache 不共享，会导致 Device Shadow/会话不一致）——这是当前配置的一个隐性坑。

**预期收益**
架构边界固化进规则后，AI 不再重复踩 1–10 项的坑；物模型驱动生成把 IoT 接入开发从"纯手写"变为"配置物模型 + 补业务"，提效显著；工具链对本项目领域精准适配，跨会话决策可召回。

**实施优先级**：**P0**（架构规则固化进 `.cursor/rules/`，成本极低、立即防错）/ P1（物模型代码生成器、领域施工技能）。

**对 CLAUDE.md 的建议**：需要。在 Agent Toolchain 节补充：
```markdown
## IoT/MES 工具链适配
- 架构硬规则固化于 .cursor/rules/iot-mes-architecture.mdc（alwaysApply）：遥测禁入 SqlSugar 业务表 / 关键消息走 RabbitMQ / 设备身份分离 / 只用 DynamicApi
- 领域施工技能：.cursor/skills/iot-device-onboarding、mes-workorder-modeling
- codegen 扩展物模型驱动模板（iot_product.data_schema → 实体+EventSubscriber+Shadow+前端面板）
- Cache 默认改 Redis（多实例下 MemoryCache 不共享，Device Shadow 会不一致）
```

---

## 审计汇总

| #    | 维度        | 改进项                                           | 优先级            | 关键依据（实测）                            |
| ---- | ----------- | ------------------------------------------------ | ----------------- | ------------------------------------------- |
| 1    | IoT 接入    | `JNPF.IoT.Gateway` + MQTTnet/EMQX，复用 EventBus | **P0**            | 无 device 模块；WebSockets/SignalR 仅面向人 |
| 2    | 实时通信    | 复用 SignalR Hub + 聚合降频                      | P0/P1             | `InstantMessaging`=SignalR 已就绪           |
| 3    | MES 模型    | 工单状态机(Stateless)，与表单驱动分层            | P1                | workflow=审批流，无 WO/批次模型             |
| 4    | 多租户/权限 | 扩展 `ITenantFilter` 到设备 + 控制鉴权           | P0/P1             | 租户隔离已有，无设备级权限                  |
| 5    | 前端状态    | device store + shallowRef/rAF 节流               | P1                | Pinia + reconnecting-websocket 已具         |
| 6    | 数据架构    | 时序库冷热分层 + Migration 版本化                | P0(迁移)/P1(时序) | 全 SqlSugar，无时序库，InitTables 无版本    |
| 7    | 消息中间件  | 明确 Channel vs RabbitMQ 边界 + 幂等             | P0/P1             | 两套总线并存但 RabbitMQ 闲置                |
| 8    | 部署运维    | docker-compose + 可观测性 + 边缘分层             | P1/P2             | 仅 1 个 Dockerfile                          |
| 9    | 测试        | 从零建测试 + 设备模拟器                          | P1                | 后端 0 测试项目                             |
| 10   | 安全        | 设备身份分离 + 健康数据合规                      | P0/P2             | 仅人类 JWT，无设备身份/数据加密             |
| 11   | AI 工具链   | 架构规则固化 + 物模型代码生成                    | P0/P1             | 工具链通用，无 IoT/MES 领域适配             |

**P0 立即执行（6 项核心）**：①IoT 接入层、②报警通道、④租户隔离+控制鉴权、⑥Migration 版本化、⑦关键消息 RabbitMQ+幂等、⑩设备身份分离+健康数据加密、⑪架构规则固化。这些要么是地基、要么是安全/资损红线。

**务实建议**：第一个落地里程碑建议聚焦"**1 类设备打通最小闭环**"——以智能手环为例，串起 接入(P0-1) → SignalR 报警(P0-2) → 租户隔离+设备鉴权(P0-4) → 设备身份/健康数据加密(P0-10) → 架构规则固化(P0-11)，形成一条可验证、可演示、可复制到其他设备类型的样板链路，再横向铺开家居/更衣柜/工地/MES。

---

需要我把这份审计报告落盘到 `docs/architecture/v52/`（符合 `docs-structure.mdc` 约定），或针对其中某条（如 P0 的 IoT 接入层）进一步出可执行的施工包吗？