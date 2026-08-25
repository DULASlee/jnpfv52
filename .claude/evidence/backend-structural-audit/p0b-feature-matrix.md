# P0-B 特征矩阵（S1-Final → P0-B）— 路径 B 行为契约锁定

**日期**：2026-08-25 ｜ **目标**：`GetConditionAsync/GetDataConditionAsync` 链路行为锁死为 S2 不可破坏基线（非重构）
**链路事实**：唯一外部消费者 `OrderService.cs:83`（`GetConditionAsync<OrderListOutput>(menu.Id, "F_ID", true, "a.")`）；条件以 `List<IConditionalModel>` 进 SqlSugar `.Where(authorizeWhere)`（OrderService.cs:85）
**测试基建**：JNPF.Tests.Common（xunit+SqlSugarCore，无 mock 框架）→ 纯函数层直测 + private 方法反射测；本体（DB 依赖）以契约形态锁定

## 6 类不变量 → 用例映射

| 不变量类 | 锁定点 | 用例组 |
|---------|--------|--------|
| 1 调用路由 | 接口签名（IUserManager L139/L150）不变；OrderService 参数形态（tableNumber="a."）→ fieldName 前缀 | R-1..R-3 |
| 2 权限语义 | `ConditionClauseAppender.AppendIds` 全分支（and/or × isCurrentRole × 单/多 id × NotEqual/NotIncluded/其他 × 空 Ids）；Registry 6 token；GetConditionalModel 23 case；ReplaceOp 9 符号 | A-1..A-12, B-1..B-6, D-1..D-8, E-1..E-2 |
| 3 租户 | 路径 B 无显式租户参数——隔离完全依赖 ITenantFilter 会话层（P0-C 固化，本组登记不变量） | T-1（登记项） |
| 4 序列化契约 | 匿名对象 `{Key,Value={FieldName,FieldValue,ConditionalType}}` JSON 形态 + 枚举数值（与 D1.5 同款断言）；短路层 FieldValueConvertFunc | S-1..S-3 |
| 5 异常/怪异 | **Q-PB1**：and+isCurrentRole=true 首条 Key=**Or**（怪异保真）；**E-PB1**：`GetDataConditionAsync` L812 `x.EnCode.Equals("jnpf_alldata")` 无 null 保护（EnCode=null → NRE，登记 L2 不修）；**E-PB2**：`GetConditionAsync` L781 `resourceList.Count==0 \|\| !Roles.Any()` 与 B L1028 仅 `resourceList.Count==0` 差异（登记） | Q-1, E-1..E-3 |
| 6 DB 消费契约 | 产出进 `.Where(IConditionalModel)` 的形态（短路层 ToSqlWhere 快照已有）；tableNumber 前缀透传 | S-3, R-3 |

## 用例清单（S1 金标准，约 30 个）

### A. AppendIds 分支矩阵（ConditionClauseAppender 直测）
- A-1 空 Ids → 不追加
- A-2 单 id / and / isCurrentRole=true → Key=[Or]（Q-PB1）
- A-3 单 id / and / isCurrentRole=false → Key=[And]
- A-4 单 id / or → Key=[Or]
- A-5 多 id(3) / and / isCurrentRole=true / Equal → [Or,Or,Or]
- A-6 多 id(3) / and / isCurrentRole=false / Equal → [And,Or,Or]
- A-7 多 id(3) / NotEqual / and / isCurrentRole=true → [Or,And,And]
- A-8 多 id(3) / NotEqual / or → [Or,And,And]
- A-9 多 id(3) / NotIncluded / and / isCurrentRole=false → [And,And,And]
- A-10 多 id(3) / Included / or → [Or,Or,Or]
- A-11 ctx.IsCurrentRole 首条后回写 false
- A-12 字段名/值/条件类型透传形态

### B. Registry 6 token
- B-1..B-6：UserId/UserAndSubordinates/OrganizeId/OrganizationAndSub/BranchManageOrganize/BranchManageOrganizeAndSub → TryGet=true + ItemType + Append 委托 AppendIds

### C. 短路层（既有 W1 覆盖 Admin/AllowAll/DenyAll 基础形态）
- C-1 DenyAll primaryKeyAsInt → int 转换函数
- C-2 AllowAll/DenyAll string 形态（既有，不重复）

### D. GetConditionalModel（private 反射）
- D-1 Contains → Like
- D-2 Equal+Int32 → Equal + int ConvertFunc
- D-3 Equal+Double → Equal + double
- D-4 Equal+string → Equal 无转换
- D-5 NotEqual+Int32 → NoEqual + int
- D-6 GreaterThan → GreaterThan
- D-7 Between → Between
- D-8 默认 dataType → string 形态

### E. ReplaceOp（private 反射）
- E-1 九符号映射（==/between/>/</<>/>=/<=/like/notLike → Equal/Between/GreaterThan/LessThan/NotEqual/GreaterThanOrEqual/LessThanOrEqual/Included/NotIncluded）
- E-2 未知符号原样返回

### R. 调用路由/消费契约
- R-1 接口签名契约（IUserManager L139/L150 反射断言，与 D1 同款）
- R-2 OrderService 参数形态：tableNumber="a." → fieldName="a.F_ID"（通过 AppendIds 直测前缀拼接逻辑）
- R-3 短路层 ToSqlWhere 快照稳定（AllowAll/DenyAll，既有+补）

### Q/E. 怪异登记
- Q-1 Q-PB1 断言（A-2 覆盖）
- E-1..E-3 E-PB1/E-PB2 登记（legacy-behavior-registry 补登，不修）

**验收**：S1 金标准全绿 → 行为不变量文档 → 全套件 → 路由/结果快照 → 签名契约 → CI → Final Review
