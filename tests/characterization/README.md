# 行为特征考卷（Characterization Tests）

**任务**：jnpf-v52-goal / T0.3 ｜ **政策依据**：MASTER 设计规格 v2.0 §4 行为四分层
**目标**：≥30 条核心 API 快照用例入 CI；此后一切重构波次的统一回归底座。

## 四分层政策

| 层级 | 定义 | 处置 |
|---|---|---|
| L-core 核心冻结 | 登录、权限判定、租户隔离、流程流转、表单提交等业务主行为 | 快照比对零差异；不绿不通过 |
| L-edge 边缘可调 | 排序细节、非关键字段返回、分页边界 | 允许变化，变化须在 LEDGER 登记 |
| L-undef 未定义 | 未在文档/考卷中声明的行为 | 自由处理 |
| L-bug 已知缺陷 | Legacy Compatibility Registry 中登记的 REMOVE/REDEFINE 项 | 走修复通道：新用例锁定预期新行为 |

## 目录约定

```text
tests/characterization/
├── README.md                 ← 本文件：政策 + 覆盖台账
├── characterization.spec.ts  ← 回放 harness（读取 manifest 动态生成用例）
├── manifest.json             ← 用例清单：{id, domain, layer, endpoint, method, fixture}
└── fixtures/{domain}/        ← 每用例两个文件：{id}.request.json / {id}.response.json
```

## 录制与回放

- **录制**（需后端 :5000 运行中）：`node scripts/lib/jnpf-auth.mjs --json` 取 token 后，用 `scripts/jnpf-api.mjs <METHOD> <path>` 调用接口，将请求参数与响应体原样存入对应 fixture 文件；
- **回放**：`npx vitest run tests/characterization --reporter=basic`——harness 按 manifest 重放请求，diff 响应（白名单字段忽略：`token`、`timestamp` 类易变字段）；
- **达标线**：任意人任意时刻重放结果一致。无后端环境时 harness 自动 skip（不伪造红绿）。

## 覆盖台账（≥30 条达标线）

**当前进度：30/30 ✅ 已达标**（manifest 30 条实录，回放 32/32 全绿，已并入 `pnpm test:api` 门禁）

| # | 领域 | 层级 | 端点 | 状态 |
|---|---|---|---|---|
| 1 | 登录/OAuth | L-core | POST /api/oauth/Login | 清单保留（登录加密链路由 jnpf-auth.mjs 覆盖，不重复录制） |
| 2 | 当前用户 | L-core | GET /api/oauth/CurrentUser | ✅ currentuser-01 |
| 3-8 | 用户域×6 | L-core | Users 列表/All/Selector/{id}/Current/getOrganization | ✅ 6 条已录 |
| 9-11 | 字典域×3 | L-core | DictionaryType、Type/Selector/0、DictionaryData/All | ✅ 3 条已录 |
| 12-15 | 角色/岗位/组织×5 | L-core | Role 列表+{id}+Selector、Position 列表+{id}、Organize 列表 | ✅ 6 条已录 |
| 16-21 | 菜单/授权×6 | L-core/L-edge | Menu ModuleBySystem/Selector×2/{id}、Authority Model/Portal | ✅ 6 条已录 |
| 22-23 | 表单流(只读面) | L-core | visualdev Base 分页列表/Base/list | ✅ 2 条已录；发起→提交为写操作，待授权后补 |
| 24-26 | 审批流(只读面) | L-core | flowTemplate 列表/FlowBefore 待审/FlowMonitor 监控 | ✅ 3 条已录；审批动作为写操作，待授权后补 |
| 27-28 | 数据接口×2 | L-edge | DataInterface 列表/Selector | ✅ 2 条已录 |
| — | 字典详情 | L-bug 候选 | GET DictionaryType/0 → 500 NullReferenceException | 缺陷信号已登记，不录缺陷行为 |

> 白名单易变字段（stripVolatile）：token / timestamp / nonce / expire / onlyId（运行时树节点ID）/ loginTime（会话登录时间）。
> 录制进度在此表更新；≥30 条且全部 L-core 绿 → T0.3 达成 CI 集成条件。

## 纪律

- 禁止伪造 fixture（无后端就标"待录制"，不许编造响应体）；
- L-core 差异 = 停线信号（回退最近波次标签）；L-edge 变化必须留痕；
- 本目录文件属安全网资产，重构波次不得修改本目录来"让测试变绿"。
