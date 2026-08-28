# 事实卡 - BASE_FOUNDER_AUTH_LOG

**批次**：L1 第二批 #1（score=-1）｜ **日期**：2026-08-27 ｜ **通道**：sqlcmd 实测 + 代码扫描

| 项 | 内容 |
|---|---|
| 列清单（16列实测） | 主键 F_ID nvarchar(100)；业务列 5（F_ACTION/F_RESULT/F_IP_ADDRESS/F_USER_AGENT/F_DEVICE_FINGERPRINT）；TenantCLDSEntityBase（EnabledMark）；基类审计 9 列；租户 F_TENANT_ID。**全部大写命名**（与前4张表小写 `f_xxx` 不同）；无 text 遗留类型；无 nvarchar(max)；F_DEVICE_FINGERPRINT 为 SHA256 设备指纹 |
| 索引现状 | 仅聚簇主键 PK__BASE_FOU__2C6EC7C3DF45AA92；零二级索引 |
| 物理外键 | 无 |
| 引用代码位置 | 实体 Entity/FounderAuthLogEntity.cs:14 `[SugarTable("BASE_FOUNDER_AUTH_LOG")]` extends TenantCLDSEntityBase；写入 FounderGuardMiddleware.cs L50/157/217/226（每次创始人认证拦截时写入）；读取 FounderService.cs:82 `[HttpGet("auth/logs")]` + 前端 api/founder/index.ts L52 `getAuthLogs` → `/api/founder/auth/logs` |
| 读写方模块 | 写=inteAssistant（FounderGuardMiddleware，中件层拦截）；读=inteAssistant（FounderService）+前端 founder 认证日志页 |
| 行数分布 | 13 行种子数据；全大写列名，与 system 模块日志表命名规范不一致 |
| 事务边界/慢查询 | query-hotspots 无登记；FounderGuardMiddleware 中件层写入，无显式事务 |

## 本表特殊性

| 维度 | BASE_FOUNDER_AUTH_LOG | 前5张表 |
|------|----------------------|--------|
| 命名 | **全大写**（F_ID/F_ACTION） | 小写（f_id/f_category） |
| 基类 | **TenantCLDSEntityBase** | CLDEntityBase/CLDSEntityBase |
| 业务域 | **inteAssistant**（创始人认证） | system/taskscheduler |
| 安全属性 | 设备指纹（SHA256(IP+UA+Salt)） | 无 |
| 用途 | 创始人身份认证审计（allow/deny/not_found） | 通用操作/系统日志 |

## 初判（供台账分级）

- **命名规范不一致**：全大写 vs 其他表小写 → C级（需统一，但涉及列名变更影响大）
- **F_DEVICE_FINGERPRINT 设计**：SHA256(IP+UA+Salt) 跨 Session 识别设备，异常设备告警 → 功能完整，无需变更
- 无其他结构性问题；小表（13行），查询过滤列（F_ACTION/F_RESULT）暂无索引但数据量小无需
