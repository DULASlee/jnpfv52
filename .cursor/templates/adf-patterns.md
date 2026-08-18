# ADF P2 — 设计模式先行

> 填完本表后 **STOP**，等待用户「继续/通过」再进入 P3。  
> 前置：P1 已批准。

## 选定模式（1–2 个）

| # | 模式名 | 解决的问题 |
|---|---|---|
| 1 | | |
| 2 | | |

## 映射到本仓抽象（必须）

| 模式角色 | 本仓类型/扩展点 | 路径 |
|---|---|---|
| 例：模板方法骨架 | SkillHarness / IBaseSkill | `.../Skills/` |
| 例：管道/门控 | *Gate* / RequirementGateService | `.../Gates/` |
| 例：编排 | *Orchestrator | `.../` |
| 例：事件溯源 | IR Event / IrEventStore | `.../Ir/` |
| 例：应用服务 API | IDynamicApiController Service | `.../` |

（按实际填写，删除无关行。）

## 为何不用替代模式

| 未选模式 | 不选原因 |
|---|---|
| | |

## 扩展点 vs 密封实现

- 可替换：
- 密封（禁止子类/旁路）：

## 反模式（本任务明确禁止）

- [ ] 字段第二源 / DDL 兜底冒充唯一源
- [ ] 编排器代问 / 旁路 PM
- [ ] Controller/页面直访 Repository
- [ ] 复活废止模块（ScannerValidator / sa_ddd / …）
- [ ] 其他：
