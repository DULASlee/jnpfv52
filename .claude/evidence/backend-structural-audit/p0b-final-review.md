# P0-B Final Review — 数据权限路径 B 行为契约锁定

**日期**：2026-08-25
**结论**：**路径 B 行为契约已锁死为 S2 不可破坏基线**（零实现修改）；建议进入 P0-C（待人工批准）

## 一、交付内容

| 项 | 内容 | 证据 |
|----|------|------|
| 调用链清点 | 唯一消费者 `OrderService.GetList`（GetConditionAsync&lt;OrderListOutput&gt;(menu.Id,"F_ID",true,"a.")）；条件经 `.Where(authorizeWhere)` 进 SqlSugar | 规格 §1 |
| S1 特征金标准 | **43/43**（A 分支矩阵 12 / Registry 6 token 6 / 短路补 1 / GetConditionalModel 9 / ReplaceOp 10 / 路由契约 3 + 签名 2） | UserManagerPathBDataPermissionTests.cs |
| 行为不变量 | 12 权限语义 + 4 路由 + 2 租户 + 4 序列化 + 4 异常登记（12 页规格） | 数据权限路径B行为不变量规格-v1.0.md |
| 签名/序列化契约 | 接口签名反射断言（R1）；匿名对象 JSON 形态（A 组）；`a.F_ID`→`[a].[F_ID]` SQL 形态（R3） | 同上 §1/§4 |
| 路由/结果快照 | 1077/107 与 S0 **zero-diff** | p0b-routes.txt DIFF_EXIT=0 |

## 二、验收对照（用户指定门槛）

| 门槛 | 结果 |
|------|------|
| S1 特征金标准 | ✅ 43/43（当前实现上全绿后锁定） |
| 行为不变量 | ✅ 规格 §1-§5（12+4+2+4+4 项） |
| 全套件 | ✅ Common **94/94** + VisualDev **274/274** + Architecture **92/92** + Analyzers **23/23** |
| 路由/结果快照 | ✅ DIFF_EXIT=0 |
| 签名/序列化契约 | ✅ R1 反射 + A 组 JSON 断言 |
| CI | ✅ `/p:CI_BUILD=true` ExitCode=0，0 error |

## 三、F1 实测修正记录（均修用例/文档，不修实现）

1. **R3**：`ToSqlWhere` 将 `a.F_ID` 渲染为 `[a].[F_ID]`（SqlSugar 别名形态）——修断言；
2. **D7**：`GetConditionalModel(Between)` 无 case → 空模型（规格原假设 Between→Between 被实测推翻）——修用例 + 登记 E-PB3；
3. **构造不可用**：`new UserManager(null!,null!)` 触发 JNPF.App 静态初始化 NRE——改用 `FormatterServices.GetUninitializedObject`（纯函数反射，绕过构造函数）。

## 四、怪异行为登记（不修）

Q-PB1（and+isCurrentRole 首条 Or，L0 保真）；E-PB1（EnCode NRE，L2）；E-PB2（DenyAll 条件差异，L2）；E-PB3（Between 空模型，L2）；E-PB4（In 传匿名对象列表，L3）——已入 legacy-behavior-registry.md。

## 五、边界遵守声明

- ❌ 未重构任何业务实现；❌ 未改权限语义；❌ 未动 AppendTokenStrategy/ConditionStrategyRegistry；❌ 未动租户机制；❌ 未进 S2；❌ 未顺手修复任何怪异行为。

## 六、残余风险

- E-PB4（GetDataConditionAsync L810 `.In(it=>it.Id, roleAuthorizeList)` 传匿名对象列表）SqlSugar 实际行为未逐字验证（需 DB 集成环境）——L3 登记；
- GetConditionAsync 本体的 DB 依赖段（moduleId 查询/authorize 查询）未做 mock 级单测——以纯函数层+契约形态锁定，S2 设计时以本规格为等价基线。

## 七、提交

- 新增：`UserManagerPathBDataPermissionTests.cs`（43 用例）、`数据权限路径B行为不变量规格-v1.0.md`、`p0b-feature-matrix.md`、`p0b-routes.txt`、legacy-behavior-registry.md 更新
- 零业务代码/基线改动
