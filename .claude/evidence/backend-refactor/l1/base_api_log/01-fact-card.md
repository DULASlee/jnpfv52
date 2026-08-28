# 事实卡 - BASE_API_LOG

**批次**：L1 首批 #2（score=-1）｜ **日期**：2026-08-26 ｜ **数据通道**：INFORMATION_SCHEMA 实测(sqlcmd) + ng1b 溯源 + 代码扫描

| 项 | 内容 |
|---|---|
| 列清单（38列实测） | 主键 f_id nvarchar(100)；业务列 25（实体 ApiLogEntity）；基类审计列 ~9（CLDEntityBase：creator/last_modify/delete_mark 三组+sort_code）；租户列 f_tenant_id nvarchar(100)；系统列 f_zx_system_id / f_inte_assistant。**异常**：6 列大写混排（F_REQUEST_Body_Type/F_REQUEST_Body/F_REQUEST_Headers/F_REQUEST_Result/F_Msg/F_Status）；4 列遗留 text 类型（F_REQUEST_Body/Headers/Result/F_Msg）；3 列 nvarchar(max)（f_json/f_request_param/f_request_target） |
| 索引现状 | 仅 1 个聚簇主键 PK__base_sys__2911CBED3C589CD7_**copy1**（名称表明本表由 base_sys_log 复制建表）；**零二级索引**；碎片率未测（39行无意义） |
| 物理外键 | 无（db-fks.tsv 无记录 ✓ 实测一致）；逻辑关系：f_user_id→BASE_USER、f_module_id→BASE_MODULE（弱引用，日志语义） |
| 引用代码位置 | 实体 backend/modularity/system/JNPF.Systems.Entitys/Entity/System/ApiLogEntity.cs:9 `[SugarTable("BASE_API_LOG")]`；写路径唯一：System/DataInterfaceService.cs L1646(new)+L1681/L1687(Insertable 两分支)；读路径：**全后端 0 处 Queryable**（Serena 通道超时，文本扫描双通道中 1/2 完成） |
| 读写方模块 | 写=system(DataInterfaceService，数据接口调用日志)；读=**无任何消费者**（read_consumers 空 + ui_menu 空 + 后端零查询） |
| 行数分布 | 39 行全部为 ZXAFINIT.sql 种子数据(seed_inserts=39)，**无运行时流量**；关键长列分布待有真实流量后再测 |
| 事务边界/慢查询 | query-hotspots.md 登记「审计日志双写」：base_sys_log+base_api_log+BASE_AI_CALL_LOG 三族写放大；DataInterfaceService 单次调用两 Insertable 分支各插一条 |

## 初判（供台账分级）

1. **只写不读僵尸表**：当前无查询方 → 加二级索引无收益（不加才是对的）；真正的动作是给消费方或退役决策；
2. text 4 列 → nvarchar(max)：属 C 级（ALTER COLUMN 语义变更），进裁决队列；
3. 列名大小写统一：C 级；
4. 写放大（每次接口调用双插）：A 级候选（异步化/合并），但需先明确消费方是否存在。
