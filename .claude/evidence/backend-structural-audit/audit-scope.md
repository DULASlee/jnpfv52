# Backend Structural Audit — Audit Scope

**日期**：2026-08-25（S1-Final Step 0 实测）
**工具**：`tools/JNPF.Audit.Scanner`（Roslyn 语法级只读扫描，CC 口径与 JNPF009 同源；排除 bin/obj/tests/tools）

## 1. 扫描范围（实测）

| 层级 | 项目数 | 文件数 | 类数 | 方法数 | 主要职责 |
|------|-------:|-------:|-----:|-------:|---------|
| modularity | 48 | 1549 | — | — | 16 业务模块（业务/Entitys/Interfaces 三层） |
| framework | 11 | 568 | — | — | 框架（Furion 系/JNPF 大仓） |
| infrastructure | 5 | 124 | — | — | 事件总线/OAuth/WebSocket |
| application | 2 | 37 | — | — | 宿主（不列为候选） |
| **合计（业务扫描面）** | **66** | **2276** | **2457** | **6627** | 约 28 万行 |

> 类/方法数为扫描器实测（非猜测）；application 的 37 文件计入扫描但按宿主处理不列为重构候选。

## 2. 扫描器口径说明（2026-08-25 修正记录）

- CC 计算复制自 `CyclomaticComplexityWalker`（JNPF.Analyzers，JNPF009 门禁同源），**含 `VisitLocalFunctionStatement` 跳过局部函数**（局部函数由 JNPF009 单独分析，不并入外层方法）。
- **修正过程（F1 实证）**：初版扫描器漏抄局部函数跳过逻辑，导致含局部函数的方法 CC 虚高（如 `PortalService.GetList` 虚报 31，实测 24；`ScheduleUIMiddleware.InvokeAsync` 虚报 30，实测 29）。经临时探针测试（真实源码+真实台账，跑完即删）定位，修复后复扫。**门禁行为始终正确，无缺口**（见 complexity-inventory.md §3）。
- Cognitive Complexity：`[NOT MEASURED]`（工具不支持），以 NestingDepth+Branches 近似。

## 3. 指标字典

| 指标 | 含义 |
|------|------|
| CC | 圈复杂度（McCabe 口径，基数 1 + 决策点） |
| LOC | 方法体行数 |
| Params | 参数数量 |
| NestingDepth | 控制流最大嵌套深度 |
| IfCount / SwitchCases / TryCatch / Returns / Calls | 对应语法节点计数 |

## 4. 原始数据

- `complexity-inventory.csv`（6627 方法全量）
- `audit-scope-stats.txt`（汇总）
