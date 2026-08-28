# 事实卡 - base_data_interface_log

**批次**：L1 第二批 #2（score=-1）｜ **日期**：2026-08-27 ｜ **通道**：sqlcmd 实测 + 代码扫描

| 项 | 内容 |
|---|---|
| 列清单（19列实测） | 主键 f_id nvarchar(100)；业务列 8（f_invok_id/f_invok_time/f_user_id/f_invok_ip/f_invok_device/f_invok_type/f_invok_waste_time/f_oauth_app_id）；基类审计 9 列；租户 f_tenant_id；系统 f_zx_system_id。**全部小写规范**；无 text 遗留类型；无 nvarchar(max) |
| 索引现状 | 仅聚簇主键 PK__base_dat__2911CBEDD9B8DA97；零二级索引；f_invok_id（接口ID）是主查询过滤列但无索引 |
| 物理外键 | 无；逻辑关系：f_invok_id→DataInterfaceEntity、f_oauth_app_id→InterfaceOauthEntity |
| 引用代码位置 | 实体 Entity/System/DataInterfaceLogEntity.cs:14 `[SugarTable("BASE_DATA_INTERFACE_LOG")]`；写入 System/DataInterfaceService.cs:1742；读取 DataInterfaceLogService.cs:22 `[HttpGet]` GetList + InterfaceOauthService.cs:151 GetList（JOIN DataInterfaceEntity） |
| 读写方模块 | 写=system（数据接口调用时写入）；读=system（DataInterfaceLogService + InterfaceOauthService 两处查询） |
| 行数分布 | 0 行空表；与 BASE_SCHEDULE_LOG 同为 L1 首批中唯一的 2 张空表 |
| 事务边界/慢查询 | query-hotspots 无登记；单条 Insertable 无显式事务 |

## 初判

- 空表+有读写链路但无数据 → 功能完整但流量为零（数据接口未被调用或日志未持久化）
- f_invok_id 索引：主查询过滤列无索引，空表时无感，接口调用量增长后需评估 → A级候选（待请示）
- 无其他结构性问题
