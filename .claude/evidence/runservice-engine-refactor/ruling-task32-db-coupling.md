# 裁决记录 — Task 3.2 七方法迁移与编译层「零 DI」定性冲突（裁决 A）
# 日期：2026-08-24 ｜ 决策人：用户 ｜ 记录人：执行代理

## 冲突事实（实测盘点，Evidence Over Assumption）

Task 3.1 提交（03bffa77）时实测发现：计划列出的七方法含 40+ 处 DB 调用，
与规格 4.3「RunSqlCompiler=纯计算/构造零 DI」定性表面冲突：

| 方法 | DB 耦合实测 |
|------|------------|
| GetListQuerySql | 重：SqlQueryable 条件渲染×8、ChangeDataBase/AsTenant 库切换×4 组、IsAnyColumn/租户缓存×9；含递归自调用与 `_sqlSugarClient` 字段写回 |
| GetVisualDevModelDataConfig | 重：平台元数据查询×5（User/Organize/Position/UserRelation）+ `_userManager` |
| GetQueryJson | 中：USERSSELECT 分支 UserRelationEntity 查询×1 |
| GetInfoQuerySql | 纯（字符串拼接，零 DB） |
| GetSuperQueryJson | 纯（JSON 解析，零 DB） |
| GetSuperQueryInput | 纯（单行委托静态 Helper） |
| GetIConditionalModelListByTableName | 纯（单行委托静态 Helper） |

## 裁决结论（用户拍板：A）

**A：逐字移动 + 过渡承载。** 严格遵循计划 3.2→3.3 分段：

1. Task 3.2 方法体逐字迁入（红线 1 不破坏）；
2. DB/用户/缓存/租户依赖经**过渡上下文值对象** `RunSqlCompileContext`（方法参数，
   构造仍无参、零 DI 字段）随迁；`_sqlSugarClient` 字段写回语义改由
   `ctx.SqlSugarClient` 属性承载（原字段在方法外的唯一消费点即本方法自身，
   行为等价：每次调用末均 `ChangeDatabase("default")` 复位）；
3. 「零 SqlSugar 类型引用」验收断言推迟至 Task 3.3（参数化剥离）完成后执行——
   该验收点在计划中本就归属 3.3（grep 佐证在 3.3 Step 2），非豁免；
4. C-M3-RunSqlCompiler@v1 按计划在 3.2 物化登记（注明过渡形态，3.3 完成后重录）。

## 逐字纪律的机械适配清单（仅三类，全部行为等价）

1. 字段引用 → `ctx.*` 成员（`_userManager`/`_visualDevRepository`/`_databaseService`/
   `_sqlSugarClient`/`_tenant`/`_cacheManager` 六字段）；
2. 三个 DB 依赖方法（GetVisualDevModelDataConfig/GetListQuerySql/GetQueryJson）
   签名首部增 `RunSqlCompileContext ctx` 参数；四纯方法签名不动（仅 private→public）；
3. `FieldBindDefaultValue(...)` 单行薄包装改直调
   `FieldBindDefaultValueHelpers.Bind(..., ctx.UserManager.User.PositionId)`
   （原包装方法体=该单次调用，行为完全等价）。

方法体逻辑、字符串字面量、控制流：零改动。

## 施工方式

脚本化逐行抽取（RunService.cs 行号区间）+ 上述三类机械替换生成，杜绝手抄转录误差；
生成后经构建 + 路由快照零 diff + 存量测试验证。

## 否决方案

- B（3.2+3.3 合并）：特征单测（3.5）未建立前剥离 DB 无 SQL 等价性守护网，且正面冲突「逐字不改」红线。
- C（只迁纯方法）：偏离计划七方法口径，重构收益碎片化。
