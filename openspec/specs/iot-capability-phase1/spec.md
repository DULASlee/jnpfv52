# Capability: iot-capability-phase1

> **状态**：知识库 v1.0（2026-05-22）· 评估 v1.1 **待架构师批准**  
> **架构决策**：在现有 JNPF/Fruit 框架上**升级扩展**，不另建 IoT 版低代码框架  
> **评估全文**：[`docs/架构迭代/1、系统架构设计说明/3、物联网项目开发能力扩展评估第一版.md`](../../../docs/架构迭代/1、系统架构设计说明/3、物联网项目开发能力扩展评估第一版.md)  
> **工期粗估**：v1.1 修订 **16–20 周**（含 Phase 0 PoC）；批准后再出 Superpowers 施工包

## Purpose

定义三期 IoT/MES 扩展的**能力边界、架构约束与 Phase 1 知识真相**，供后续 `modularity/mes/`、`modularity/iot/` 施工与 OpenSpec 归档引用。

## Scope (Phase 1 knowledge)

| 域 | 范围 |
|----|------|
| MES 业务 | ERP 导入、工单/BOM/进度、用工单、打印存档 |
| IoT 基础设施 | MQTT 接入、时序存储、SignalR 推送、设备管理 |
| 垂直场景 | 智慧工地、智能手环、智能更衣柜/售卖柜（v1.1 专项） |
| 不在 Phase 1 spec | 详细 DDL、API 列表、施工任务（→ Superpowers 施工包） |

## Requirements

### Requirement: Extend existing framework only

The platform SHALL extend the existing JNPF modularity pattern (`{Module}/`, `{Module}.Entitys/`, `{Module}.Interfaces/`) for MES and IoT capabilities and SHALL NOT fork a separate IoT low-code framework.

#### Scenario: MES module placement

- **WHEN** MES business capabilities are implemented
- **THEN** they reside under `modularity/mes/` as a standard three-project module
- **AND** reuse BillRule, PrintDev, Workflow, ExcelImportHelper without core framework rewrites

#### Scenario: IoT module placement

- **WHEN** device management is implemented
- **THEN** it resides under `modularity/iot/` as a standard three-project module

### Requirement: MQTT broker external EMQX

MQTT messaging SHALL use an external EMQX broker for non-development environments; MQTTnet SHALL be used as a **client** only, not as an embedded production broker.

#### Scenario: Production device scale

- **WHEN** deploying device connectivity beyond development self-test
- **THEN** EMQX is the designated broker
- **AND** MQTTnet embedded broker is limited to local/dev verification

### Requirement: Time-series store with PoC gate

Time-series telemetry SHALL use TDengine as the preferred store, with a mandatory PoC before Phase 2; InfluxDB is the documented fallback if PoC fails.

#### Scenario: Phase 2 entry blocked without PoC

- **WHEN** Phase 2 IoT storage work is scheduled
- **THEN** TDengine PoC results are recorded and approved
- **OR** fallback to InfluxDB is explicitly documented with migration notes

### Requirement: SignalR real-time channel

Real-time push to browsers/apps SHALL use the existing SignalR framework skeleton with full Hub implementation, JWT auth integration, and frontend subscription — estimated **2–3 weeks**, not a two-line Startup change alone.

#### Scenario: Live dashboard updates

- **WHEN** a telemetry alert must reach the UI within seconds
- **THEN** SignalR Hub delivers the event to authorized clients
- **AND** connection uses the same OAuth/JWT model as HTTP APIs

### Requirement: IoT security layers

IoT deployments SHALL implement the five-layer security model documented in evaluation §4A: device identity → transport encryption → ACL → command safety → data compliance.

#### Scenario: Device command authorization

- **WHEN** a control command is sent to a field device
- **THEN** the command path validates device identity and ACL before publish to MQTT

### Requirement: Smart locker vertical constraints

Smart locker / vending scenarios SHALL account for C-end users, payment integration, offline/weak-network operation, slot inventory, and concurrent MQTT commands as documented in evaluation §3A.

#### Scenario: Offline transaction

- **WHEN** network is unavailable at the cabinet
- **THEN** documented offline strategy applies (local queue / reconcile) before production sign-off

### Requirement: Event bus upgrade realism

Migration from existing event bus to RabbitMQ (or equivalent) SHALL be planned as consumer migration plus integration testing (**~1–2 weeks**), not configuration-only; Phase 0 includes RabbitMQ现状 audit.

#### Scenario: Phase 0 audit complete

- **WHEN** IoT phase execution starts
- **THEN** RabbitMQ/event consumer inventory exists from Phase 0 audit

## Architecture enhancements (v1.1 count: 16 items)

Knowledge reference only — implementation tasks live in future Superpowers plans:

- MES: incremental modules on SqlSugar + DynamicApiController pattern
- IoT: EMQX + MQTTnet client + TDengine + SignalR + `modularity/iot/`
- VisualData: device monitoring dashboards
- OA integration: six high-value OA+IoT scenarios (evaluation §8.3)
- Deployment: minimal 3-server + Docker Compose draft (§8.4)

## Key code paths (existing reuse)

| Path | Role |
|------|------|
| `framework/JNPF/` | DynamicApiController, DI, SignalR skeleton |
| `infrastructure/JNPF.Extras.EventBus.RabbitMQ/` | Event bus extension |
| `modularity/engine/` | VisualDev runtime |
| `modularity/visualdata/` | Dashboard / 大屏 |
| `application/JNPF.API.Entry/` | API host Startup |

## Core tables (MES/IoT — 【待 Phase 1 施工包 DDL】)

| Table | Role |
|-------|------|
| **BASE_* / workflow tables** | Reuse for MES approvals |
| **IoT device/telemetry tables** | To be defined in `modularity/iot/` Entitys 【待源码验证】 |

## Approval gate

No Superpowers construction package or `executing-plans` for IoT Phase 1 SHALL start until architecture approves evaluation v1.1 and records approval in the project progress log.

#### Scenario: Post-approval planning

- **WHEN** architecture approves IoT evaluation v1.1
- **THEN** Superpowers `writing-plans` produces phased construction packages
- **AND** optional OpenSpec change archives deltas into this spec
