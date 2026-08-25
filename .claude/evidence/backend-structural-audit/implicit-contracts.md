# Backend Structural Audit — Implicit Contract Inventory

**日期**：2026-08-25 ｜ 方法：grep 实测（modularity）+ D1 证据继承

## 1. JSON/字符串承载结构化数据（P5 模式热点）

| 文件 | ToJsonString 次数 | 说明 |
|------|------------------:|------|
| `engine/…/CodeGenFormControlDesignHelper.cs` | **117** | 代码生成前端脚本（FormScriptDesign CC593 同文件）——最密集隐式契约点 |
| `visualdev/JNPF.VisualDev/RunService.cs` | 36 | 数据权限/表单数据链 |
| `inteAssistant/…/InteAssistantRun.cs` | 32 | 节点数据传递 |
| `engine/…/CodeGenWay.cs` | 31 | 代码生成 |
| `engine/…/FormDataParsing.cs` | 29 | 表单解析 |
| `system/…/DataInterfaceService.cs` | 25 | 数据接口 |
| `oauth/…/OAuthService.cs` | 24 | 登录/用户 |
| `visualdev/…/VisualDevService.cs` | 21 | — |
| `workflow/…/FlowTaskManager.cs` | 20 | 流程任务 |
| `workflow/…/FlowTemplateService.cs` | 18 | 流程模板 |

## 2. 关键隐式契约清单（S2 相关）

| Contract ID | Producer | Consumer | Transport | Test Protection | Breaking Risk |
|-------------|----------|----------|-----------|-----------------|---------------|
| IC-01 | `UserManager.GetCondition` | `RunService` | 匿名对象 → ToJsonString → `JsonToConditionalModels` | ✅ D1.5 特征 33/33 | 高 |
| IC-02 | `UserManager.GetConditionAsync/GetDataConditionAsync` | `OrderService` | 同上（`AppendTokenStrategy` 独立构造） | ❌ 无 | **高（P0-2）** |
| IC-03 | `ConditionalType/WhereType` 枚举数值 | 全部条件消费 | int 序列化 | ✅ D1.5 特征 | 高 |
| IC-04 | `CodeGenFormControlDesignHelper` 脚本生成 | 前端运行时 | 字符串脚本（117 处 ToJsonString） | ❌ 无特征 | 中（前端契约） |
| IC-05 | 控件 `__vModel__/__config__` 字典键 | 表单/列表引擎 | `Dictionary<string, object>` | ✅ D1.3/D1.4 部分 | 高 |

## 3. 其他隐式机制（登记）

- **Magic String/Number**：`"2"` 保留最后行模式（D1.4 特征锁定）、`"null"` 字符串 IsNot（D1.5 锁定）——已有测试保护
- **约定式注册**：DynamicApiController（IDynamicApiController 自动映射，路由快照 1077/107 守护）
- **`-` 分隔键豁免**（D1.3 Q7 特征锁定）

## 4. 审计结论

- 已保护契约：IC-01/03/05（D1 战役成果）
- **未保护契约：IC-02（P0-2）、IC-04（P1 关联）**——S2 前需按 D1 协议补特征或显式化
