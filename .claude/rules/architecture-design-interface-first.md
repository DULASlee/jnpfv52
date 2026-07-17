# ADF 三先行铁律（Architecture → Design pattern → Interface → Impl）

> **Cursor 镜像：** `.cursor/rules/architecture-design-interface-first.mdc`  
> **模板：** `.cursor/templates/adf-architecture.md` · `adf-patterns.md` · `adf-contracts.md` · `task-kickoff.md`  
> **层级：** 流程门控（与 Business First / 实现完整性 / architect-mode **衔接**，不另立重复宪法）

## 核心宣言

**S/A 级任务禁止直接写实现。** 必须先完成架构 → 设计模式 → 接口契约三阶段，每阶段经用户「继续/通过」后再进入下一阶段。

## 强制顺序

```
P0 业务锚定（Business First Q1–Q3）
 → P1 架构先行 → STOP 等「继续」
 → P2 设计模式先行 → STOP 等「继续」
 → P3 接口契约先行 → STOP 等「继续」
 → P4 实现（实现完整性节点审批）
```

## 任务分级

| 级别 | 条件 | ADF |
|---|---|---|
| S/A | 新功能、跨模块、接口/数据层、≥3 文件、架构决策 | 必须 P0–P3 |
| B | 单文件 ≤10 行 bugfix / 文案 / 注释 / 补既有单测 | 可豁免，须声明 |

```
ADF 豁免：B级 — <一句话理由>
```

**不可豁免：** 新 Skill、Gate/Validator、IR 契约、跨前后端、CR 保护方法。

## P1 架构先行

模板：`.cursor/templates/adf-architecture.md`

必须含：层边界、数据归属/唯一源、三元组、≥2 方案+不做/复用+failure_boundary、禁改清单、R1–R12 预检。

对齐：`.claude/souls/architect/soul.md`（多方案 + 失效边界）。

## P2 设计模式先行

模板：`.cursor/templates/adf-patterns.md`

必须含：1–2 主模式、映射到 `SkillHarness` / `IBaseSkill` / Gate / Orchestrator / IR / `IDynamicApiController`、为何不用替代模式、扩展点与反模式。

## P3 接口契约先行

模板：`.cursor/templates/adf-contracts.md`

必须含：签名/DTO/事件/错误契约、影响范围；**禁止方法体**。

## P4 实现

P1–P3 均批准后，按 `executing-plans`；节点完成仍交四件套等用户审批。

## 零占位符（硬失败）

- Claude：`guard-write.mjs` L11
- Cursor：`.cursor/hooks/guard-placeholder.mjs`（preToolUse）
- Git：`.githooks/pre-commit` → `node .claude/hooks/placeholder-scan.mjs --staged`
- 豁免：`// placeholder-ok: <理由>`

## 禁止

- S/A 直接编码再补文档
- P3 未批写方法体或占位符过编译
- 「先打通再重构」跳过 ADF
- 无豁免声明默示跳过
