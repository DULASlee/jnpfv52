# Data Ownership Profile v1.0（数据责任与模块边界档案）

**版本**：v1.0 定稿 ｜ **日期**：2026-08-26
**任务**：jnpf-v52-goal / T1.2 ｜ **政策依据**：MASTER 总体设计规格；NG-0 数据 Ownership 地图（DB-4）升格
**上游工件**：[platform-asset-inventory.v1](./platform-asset-inventory.v1.md)（disposition 口径）· ng1b provenance-matrix（289×26 溯源）
**配套复核**：《L1-表级螺旋执行手册-v1.0》§1 排序清单

## 1. 判定方法（继承 NG-0，证据升级）

1. 域级归属：表前缀聚类 + C# 实体模块归属 + 写路径 Service 目录（NG-0 DB-4 结论）；
2. 表级量化：ng1b `write_owner` 字段（代码扫描写路径命中）；
3. 冲突规则：**表级扫描缺省时以域级归属为准**，事实卡阶段（L1 SOP 第①步）逐表复核。

## 2. 域级归属总图（ENTER 层 157 张）

| 域 | Owner（现状模块） | 代表表 | 边界规则 |
|---|---|---|---|
| Identity | system | base_user / base_organize / base_role / base_position | 全平台最强耦合源；他域禁止直连，读取 API 化+审计快照 |
| Tenant | zxdev | zx_sys_db / zx_sys_config | 私有化注册链路 |
| Permission | system | base_authorize / base_module* / base_data_authorize* | GetCondition 双路径收敛后快照化 |
| Workflow | workflow | flow_task / flow_task_operator / wform_* 51 | 流程引擎域完整闭合 |
| Form/LowCode | visualdev | base_visualdev_* / mt* 5 动态表 | 动态表归属=低代码域独立性的核心裁决 |
| File / Message / Log | system / message | base_file / base_message / base_sys_log / base_api_log | 平台服务；日志独立存储评估 |
| AI 原生化 | inteAssistant | ai_ir_events / ai_entity_field / sa_* 13 | ai_entity_field=字段唯一源；sa_* 由 C# SaMaterializer 物化 |
| 可视化大屏 | visualdata | blade_visual* 8 | 见 §5 修正项 |

## 3. 表级 write_owner 量化（ENTER 层实测）

| write_owner | 张数 | 说明 |
|---|---:|---|
| inteAssistant | 26 | AI 原生域 |
| system | 22 | Identity/Permission/File/Log |
| workflow | 9 | 流程域单写 |
| message | 4 | 消息域单写 |
| visualdev | 2 | 低代码单写 |
| visualdev;workflow | 4 | **跨模块双写** → L1 MULTI_WRITER |
| 其他单写（infrastructure/common/report/zxdev…） | 6 | — |
| **（缺省）** | **81** | 扫描未命中；按 §1 规则以域级归属为准，L1 事实卡阶段强制复核 |

缺省构成：base_* 58、blade_* 8（实为 visualdata，见 §5）、flow_* 5、sa_* 4、ai_* 2、其他 4。
缺省率 81/157=52% 属已知工具边界（gen-access-map.ps1 对 Service 基类/仓储模式写路径不敏感），不阻塞主链，但 L1 首批事实卡必须双通道复核（Serena 符号检索 + 文本扫描）。

## 4. 多写者表 = 跨域裁决队列（L1 MULTI_WRITER 7 张全录）

| 表 | writers | score | 裁决方向 |
|---|---|---:|---|
| base_user | common\|oauth\|system\|visualdev | 12 | 四方写者=最强跨域耦合；Identity API 化优先级最高 |
| flow_template_json | visualdev\|workflow | 6 | 模板 JSON 归 workflow 单写，visualdev 只读 |
| flow_template | visualdev\|workflow | 5 | 同上 |
| ai_entity_field | inteAssistant\|other | 4 | other=迁移脚本；收敛为 inteAssistant 单写 |
| flow_form | visualdev\|workflow | 4 | 表单元数据归 workflow |
| flow_form_relation | visualdev\|workflow | 3 | 关系映射归 workflow |
| base_integrate_queue | common\|inteAssistant | 3 | 队列表归基础设施 |

7 张全部暂缓出 L1 首批（手册 §1 一致）；每张的归属收敛方案走 C 级裁决会。

## 5. 对 NG-0 结论的修正（以 ng1b 强溯源为准）

| NG-0 原结论 | v1 修正 | 依据 |
|---|---|---|
| blade_* 8 表 = BladeX 遗留，DEPRECATE 候选 | **实为 visualdata 大屏运行时表（P1/MANDATORY/ENTER）**，code_owner=visualdata，含真实数据（blade_visual 77 行） | ng1b provenance：creation_source/entity_mapped/db_rows 全链命中 |
| （维持）mt* 动态表租户/平台双归属待裁 | 维持，为低代码域独立成服务的核心裁决 | 无新反证 |

## 6. L1 排序清单复核（T1.2 第二验收项）

```
l1-batch-order.csv 实测：142 行 = ELIGIBLE 135 ＋ MULTI_WRITER 7
与《L1-表级螺旋执行手册》§1 记载一致 ✅（142 入围 / 135 ELIGIBLE / 7 暂缓）
资格门槛复算：157 ENTER − sa_* 13 − 框架表 2 = 142 ✅
排序分公式抽验：头部日志负分表（base_print_log/base_api_log=-1）符合「叶子优先」设计 ✅
142/142 与 platform-asset-inventory.v1 disposition=ENTER 相容 ✅
```

## 7. 待裁决清单（滚动）

1. mt* 动态表归属模型（§4 核心裁决）；
2. base_signature / base_signature_user 归属 File or Identity；
3. flow_form_authorize 无主键（Workflow×Permission 交叉）；
4. BASE_TENANT_GLOSSARY / BASE_TENANT_INDUSTRY 空表观察；
5. 81 张 write_owner 缺省表的 L1 事实卡逐表补证。

## 变更纪律

本档案为 S1 家底定稿工件。域级归属变更需 C 级裁决会决议并回写本档案；ENTER 层每张表的事实卡产出后应将表级 owner 补入 §3 缺省清单。
