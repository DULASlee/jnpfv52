# JNPF V5.2 命名约定

> 所有开发者（人与 AI）生成代码/表/文件时必须遵循。

## 数据库表名

| 模块 | 前缀 | 示例 |
|------|------|------|
| 系统/权限 | `BASE_` | `BASE_USER`、`BASE_ROLE` |
| 扩展（邮件/产品等） | `EXT_` | `EXT_EMAIL_CONFIG`、`EXT_EMPLOYEE` |
| 工作流 | `FLOW_` | `FLOW_TASK`、`FLOW_COMMENT` |
| 可视化开发 | `VISUAL_` | `VISUAL_DEV`（低代码表单） |
| 数据大屏 | `BLADE_VISUAL_` | `BLADE_VISUAL_DB` |
| 消息 | `BASE_MSG_` | `BASE_MSG_ACCOUNT`、`BASE_MSG_SMS_FIELD` |
| 定时任务 | `BASE_TIME_TASK` / `JobDetails` | — |
| IoT（待建） | `IOT_` | `IOT_DEVICE`、`IOT_PRODUCT` |
| MES（待建） | `MES_` | `MES_WORK_ORDER`、`MES_ROUTE` |

- 全大写，下划线分隔
- 新模块前缀需架构师审定

## 大小写风格说明

| 场景 | 风格 | 示例 |
|------|------|------|
| 数据库表名 | 全大写 + 下划线 | `BASE_USER`、`IOT_DEVICE` |
| 数据库字段名 | 全大写 + 下划线 | `USER_ID`、`CREATE_TIME` |
| C# 类名 | PascalCase | `UserService`、`IotDeviceEntity` |
| C# 方法名 | PascalCase | `GetListAsync()`、`AddAsync()` |
| C# 属性 | PascalCase | `UserName`、`DeviceId` |
| C# 局部变量 | camelCase | `userId`、`deviceList` |
| C# 私有字段 | _camelCase | `_userService`、 `_dbContext` |
| API 路径 | PascalCase（自动生成） | `/api/IotDevice/List` |
| 前端 Vue 组件 | PascalCase | `DevicePanel.vue` |
| 前端 Composable | camelCase（use 前缀） | `useDeviceSocket()` |
| 前端 Store | camelCase（use 前缀） | `useDeviceStore` |
| 前端 API 文件 | camelCase | `src/api/iot/device.ts` |
| Git 分支 | 全小写 + 连字符 | `feat/iot-device-crud` |

## C# 类名

| 类型 | 规则 | 示例 |
|------|------|------|
| 实体 | `{PascalCase}Entity` | `UserEntity`、`IotDeviceEntity` |
| Service | `{实体去Entity}Service` | `UserService`、`IotDeviceService` |
| 接口 | `I{Service}` | `IUserService` |
| DTO 输入 | `{实体}{操作}Input` | `IotDeviceAddInput`、`UserLoginInput` |
| DTO 输出 | `{实体}{操作}Output` | `IotDeviceListOutput` |
| EventBus 事件源 | `{描述}EventSource` | `DeviceTelemetryEventSource` |
| EventBus 订阅者 | `{描述}EventSubscriber` | `DeviceTelemetryEventSubscriber` |

- 实体基类继承 `CLDSEntityBase` / `TenantCLDSEntityBase`（需租户隔离时）
- Service 实现 `IDynamicApiController` 即自动暴露 API

## API 路径（DynamicApiController 自动生成）

```
格式：/api/{Service名去Service}/{方法名}
示例：/api/IotDevice/List → IotDeviceService.List()
      /api/MesWorkOrder/GetPage → MesWorkOrderService.GetPage()
```

**禁止手动创建 Controller**，所有 API 由 Service + IDynamicApiController 自动映射。

## 前端（jnpf-web-vue3）

| 类型 | 规则 | 示例 |
|------|------|------|
| Vue 组件 | PascalCase | `DevicePanel.vue` |
| Composable | `use{功能}` | `useDeviceSocket()` |
| Pinia Store | `use{模块}Store` | `useDeviceStore` |
| API 文件 | `src/api/{模块}/{实体}.ts` | `src/api/iot/device.ts` |
| 类型定义 | `src/api/{模块}/model/{实体}Model.ts` | `src/api/iot/model/deviceModel.ts` |

## 文件夹结构

```
backend/modularity/{模块名}/
  JNPF.{PascalCase模块名}/                   ← Service 层
  JNPF.{PascalCase模块名}.Entitys/           ← 实体/DTO
  JNPF.{PascalCase模块名}.Interfaces/        ← 接口

jnpf-web-vue3/src/
  api/{模块}/               ← API 调用
  views/{模块}/              ← 页面
  iot/                       ← 手写 IoT 组件（与 visualdev 低代码隔离）
  store/modules/{模块}.ts   ← Pinia Store
```

## 分支命名（见 Git 工作流）

```
feat/{模块}-{简述}     ← feat/iot-device-crud
fix/{问题描述}          ← fix/tenant-filter-leak
hotfix/{紧急修复}      ← hotfix/jwt-expire-check
```
