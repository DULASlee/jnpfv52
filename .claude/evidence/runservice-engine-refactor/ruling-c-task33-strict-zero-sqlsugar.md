# 裁决 C + 施工方案 — Task 3.3 严格口径「零 SqlSugar 类型引用」（含条件模型 DTO 平台化）

日期：2026-08-24 ｜ 决策人：用户（拍板严格口径）｜ 记录人：执行代理

## 0. 裁决事实

规格 4.3.8 验收①「grep RunSqlCompiler 零 SqlSugar 类型引用」与 4.3.2「七方法签名逐字不变」
存在固有矛盾（签名含 `List<IConditionalModel>` 等 SqlSugar 条件模型类型）。
用户裁决：**严格口径** — 条件模型 DTO 亦替换为平台自有类型，规格 4.3.2 口径修订为
「签名语义一致（参数/返回形态不变，SqlSugar 类型替换为平台条件模型类型）」。

## 1. SqlSugar 事实基线（反射实测，SqlSugar.dll）

- `WhereType`：And=0 / Or=1 / Null=-1
- `ConditionalType`：16 成员（Equal=0 … InLike=15）
- `IConditionalModel`：空标记接口
- `ConditionalModel`：FieldName/FieldValue/CSharpTypeName(string) + ConditionalType + FieldValueConvertFunc(Func<string,object>，实测修正)
  + CustomConditionalFunc/CustomParameterValue（**全仓零消费，grep 实证，不承载**）
- `ConditionalCollections`：ConditionalList = List<KeyValuePair<WhereType, ConditionalModel>>
- `ConditionalTree`：ConditionalList = List<KeyValuePair<WhereType, IConditionalModel>>
- `ToJsonStringOld` = Newtonsoft `JsonConvert.SerializeObject` 默认设置（PascalCase、枚举数值化）
  → 平台类型属性名/枚举值逐项对齐即 JSON 字节等价。
- **实测补充（Inc-1 单测实证）**：① `FieldValueConvertFunc` 实为 `Func<string,object>` 且带 `[JsonIgnore]`（置位与否均不入 JSON，平台类型同特性对齐）；
  ② 平台 JSON 经 `Utilities.JsonToConditionalModels` 回解析与 SqlSugar 原样一致（下游兼容硬证据，6/6 绿）。

## 2. 施工方案（三增量，逐步提交）

### Inc-1：平台条件模型层 + 转换器 + 往返等价单测（零行为变更）

➕ `Runtime/CompileConditionalModels.cs`（零 SqlSugar）：
- `CompileWhereType`（And=0/Or=1/Null=-1）、`CompileConditionalType`（16 成员值逐项对齐）
- `ICompileConditionalModel`（空标记）
- `CompileConditionalModel`（FieldName/FieldValue/CSharpTypeName/ConditionalType/FieldValueConvertFunc）
- `CompileConditionalCollections`（List<KV<CompileWhereType, CompileConditionalModel>>）
- `CompileConditionalTree`（List<KV<CompileWhereType, ICompileConditionalModel>>）

➕ `Runtime/CompileConditionalConverter.cs`（SqlSugar↔平台，双向递归深拷贝；
归属=RunService 边界适配，不在引擎类清单，不计入验收① grep 对象）

➕ 单测（JNPF.Tests.VisualDev）：
- JSON 往返等价：SqlSugar 三类形态 → ToJsonStringOld → 平台类型 → ToJsonStringOld 字节一致（双向）
- 转换器属性级等价（含 Tree 嵌套、Collections 混合）

### Inc-2：特征捕获前置（计划 3.5 提拉到剥离前，Evidence Over Assumption）

➕ `RunSqlCompilerFeatureTests.cs`：对当前（剥离前）实现以代表性输入捕获
GetQueryJson/GetSuperQueryJson/GetListQuerySql 主分支输出快照为期望值；
渲染用无连接 SqlSugarClient（纯 SQL 生成，不触库）。剥离后同一测试断言等价。

### Inc-3：参数化剥离（RunSqlCompileGateway 替换 RunSqlCompileContext）

`RunSqlCompileGateway`（RunService 侧构建，每请求/每模板一份）：
| 成员 | 职责 | 数据源（RunService 侧供数） |
|------|------|---------------------------|
| UserOrigin | pc/app 判定数据 | _userManager.UserOrigin |
| MultiTenancy / TenantCache | 租户隔离判定数据 | _tenant + _cacheManager（DbLink.Id 查找） |
| ColumnExists(table,column) | 列存在判定 | _databaseService.IsAnyColumn（绑 DbLink） |
| JsonToConditions(json) | 条件 JSON 解析（返回平台类型） | Utilities.JsonToConditionalModels + 转换 |
| RenderLinkWhere(平台条件) | 外部源条件渲染 | ChangeDataBase→SqlQueryable→ToSqlString→复位 default（语义逐字不变） |
| RenderDefaultWhere(平台条件) | 主库 "@" 条件渲染 | AsSugarClient().SqlQueryable<dynamic>("@") |
| RenderSqlWhere(sql,平台条件) | 已拼 SQL 追加条件 | SqlQueryable<dynamic>(sql) |
| ResolveUserRelations(ids) | USERSSELECT 关系查询 | Queryable<UserRelationEntity>（Replace("--user") 查询逻辑留调用侧） |
| UserSelectDefaults() | 惰性五项元数据 | User/Organize/Position/UserRelation 五查询 + PositionId |

编译层内改法：
- `GetIConditionalModelListByTableName`：平台类型过滤逻辑内置（与
  `ListConditionalByTableNameFilter` 语义逐句一致，含首子节点 WhereType.Null 置位与就地删减）；
  旧 SqlSugar 版 Helper 仅测试消费，保留。
- `GetSuperQueryInput`：不变（纯字符串委托，无 SqlSugar）。
- `GetVisualDevModelDataConfig`：惰性 UserSelectDefaults + FieldBindDefaultValueHelpers.Bind 直调。
- `GetQueryJson`：USERSSELECT 分支经 ResolveUserRelations 供数。
- `GetListQuerySql`：渲染/判定/解析全部经 gateway；内部类型切平台；
  `superCond.ToObject<List<ConditionalCollections>>()` → 平台对应类型（JSON 形态一致）。
- 删除 `RunSqlCompileContext`（裁决 A 过渡载体退役）。

RunService 侧变异语义保持（关键风险点，逐处核对）：
- `GetListChildTable` 入参（querList/dataRuleList/superQuerList）切平台类型；
  **dataRuleList 跨迭代就地删减语义**：入口处一次性转平台、单实例贯穿全程。
- `RewriteChildFieldNames`：增平台类型重载（逻辑逐句一致）。
- SqlQueryable 渲染点（子表 where/条件追加）：平台→SqlSugar 转换后只读渲染。
- `input.queryJson = GetQueryJson(...).ToJsonStringOld()`：JSON 字节等价（Inc-1 单测守护）。

## 3. 验收（严格口径）

① `grep -i sqlSugar RunSqlCompiler.cs` 零命中（含 using/类型/字符串外的注释亦清）
② Inc-2 特征测试全绿（剥离前后输出一致）
③ 全解构建 0 错误 + 路由快照与基线零 diff
④ 存量测试 204+ 全绿 + Inc-1 往返等价单测绿
⑤ C-M3-RunSqlCompiler@v1 台账重录（SHA256 更新，过渡注记移除）

## 4. 失败边界与回滚

- 往返等价不符（Newtonsoft 对 KVP/枚举形态差异）→ Inc-1 单测先红，停手核对，不入 Inc-3。
- 特征测试捕获不可行（模板构造受阻）→ 降级登记：覆盖主分支+残余风险台账，不伪造全覆盖。
- 剥离婚后输出差异 → 逐方法比对回滚（阶段级 `git revert`，回滚轴=重构轨，ADR-2）。

## 5. 规格/计划修订（随本裁决）

- 规格 4.3.2：「七方法签名逐字不变」→「签名语义一致：形态不变，SqlSugar 条件模型类型替换为平台自有类型（裁决 C）」。
- 规格 4.3.8 验收①：明确严格口径（含 DTO 类型）。
- 计划 Task 3.3：三增量拆解（Inc-1/2/3），3.5 特征捕获提拉到 Inc-2。
