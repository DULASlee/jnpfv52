# JNPF V5.2 日志级别使用约定

> 框架使用 Serilog 结构化日志（`framework/JNPF/Logging/`），文件日志配置在 `Configurations/Logging.json`。

## 日志级别使用规则

| 级别 | 用途 | 示例 |
|------|------|------|
| Fatal | 系统不可用 | 数据库连接池耗尽、MQTT Broker 不可达 |
| Error | 功能故障需关注 | 设备指令下发失败、工单状态跃迁异常、第三方 API 调用超时 |
| Warning | 潜在问题 | 设备离线超时、Redis 缓存未命中率突增、配置缺失回退默认值 |
| Information | 关键业务节点 | 用户登录/登出、设备上线/下线、工单状态变更、租户创建 |
| Debug | 开发调试 | Service 入参/出参、SQL 语句、EventBus 消息体、HTTP 请求详情 |
| Verbose | 极细粒度 | MQTT 原始报文、HTTP 请求/响应头、SignalR 握手详情 |

## 环境级别设置

| 环境 | 最低级别 | 说明 |
|------|----------|------|
| 生产 | Warning | 减少日志量，关注异常和告警 |
| 预发布/测试 | Information | 关键业务节点可追踪 |
| 开发 | Debug | 完整调试信息 |

## 关键规则

1. **高频数据用 Debug**：设备遥测、传感器读数等高频数据日志用 Debug 级别，禁止用 Information（会淹没关键业务日志）
2. **异常必带上下文**：Error/Fatal 日志必须包含关键业务标识（tenantId / deviceId / userId / workOrderNo）
3. **敏感数据脱敏**：日志中禁止记录密码、Token 明文、健康数据明文
4. **文件滚动**：当前配置 10MB/文件、保留 30 个文件（见 `Logging.json`），不要改小

## 敏感信息脱敏规则

- 密钥、Token、密码等敏感值在日志/报告/对话中必须脱敏为 `<REDACTED>`
- 绝不在任何非加密存储中记录真实密钥值
- 违反此规则的日志条目应立即清除

## 日志模板

```csharp
// 正确 — 结构化参数
_logger.LogInformation("设备 {DeviceId} 上线，租户 {TenantId}", deviceId, tenantId);

// 错误 — 字符串拼接（Serilog 无法解析为结构化字段）
_logger.LogInformation($"设备 {deviceId} 上线，租户 {tenantId}");
```
