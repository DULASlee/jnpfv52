# S1 门禁证据 — 运行时基座与 RunService 引擎化（Task 3.6）

- 执行日期：2026-08-24
- 门禁基线 HEAD：`b45295779656904bcc05a95a75f458d974080639`（b4529577）
- 分支：main
- 结论：**S1 Gate Passed — Awaiting Approval**（未进入 S2）

## 门禁项 1：路由快照 zero diff — PASS

- 生成方式（既定）：`dotnet run --project tools/JNPF.Startup.Benchmarks -- --mode routes --filter "api/visualdev"`（工作目录 `backend/`）
- 实测指标：`[METRIC] route_total=1077 route_matched=107 filter=api/visualdev`
- 比对基线：`.claude/evidence/runservice-engine-refactor/s0-routes-visualdev-baseline.txt`（S0 登记，未修改）
- 比对结果：`git diff --no-index --quiet` → **DIFF_EXIT=0（zero diff）**
- 落盘快照：`.claude/evidence/runservice-engine-refactor/s1-routes.txt`（107 条路由 + METRIC 行，与基线逐行一致）

## 门禁项 2：契约测试全绿 — PASS

- 命令：`dotnet test backend/tests/JNPF.Tests.VisualDev/JNPF.Tests.VisualDev.csproj`
- 结果：**通过 218，失败 0，跳过 0，总计 218**
- 覆盖：IRunService 反向提取契约测试（3/3）、CompileConditionalEquivalenceTests（6/6）、RunSqlCompilerFeatureTests（8/8）、Query 辅助类测试（ListConditionalByTableNameFilter/ListSuperQueryInputRewriter/ListChildTableHelpers 等）、FlowFormDataMapper/ImportFirstVerifyHelpers/FieldBindDefaultValueHelpers 存量测试

## 门禁项 3：特征单测全绿 — PASS

- 命令：`dotnet test … --filter FullyQualifiedName~RunSqlCompilerFeatureTests`
- 结果：**通过 8，失败 0，总计 8**
- 快照基线：`feature-capture/` 8 张（均为剥离前实测捕获，非手写）
- 口径说明：`GetVisualDevModelDataConfig` 不纳入特征快照集，由契约测试 + 路由快照覆盖（Task 3.5 既定口径，未扩围）

## 门禁项 4：CI / Build 全绿 — PASS

- 命令：`dotnet build backend/zx_lowcode_netcore.sln /p:CI_BUILD=true`（含 JNPF009 复杂度门禁 + 全部分析器）
- 结果：**ExitCode=0，0 个错误**，25910 个警告（均为存量风格类警告，历史即存在，不门禁）
- 耗时：00:01:55

## 前序门禁链（可追溯）

| 节点 | 提交 | 结果 |
|------|------|------|
| Task 3.2 纯移动 | 074ab292 | 构建 0 错 + 路由零 diff + 204/204 |
| Task 3.3 Inc-1 平台条件模型 | d7b0da9a | 等价单测 6/6 |
| Task 3.3 Inc-2 特征捕获 | 26d3f784 | 8 快照基线 |
| Task 3.3 Inc-3 参数化剥离 | 54562b8e | grep 零 SqlSugar + 特征 8/8 逐字一致 + 218/218 + 路由零 diff |
| Task 3.4 基线随迁 + D1 立项 | b4529577 | CI 0 错 + 分析器测试 23/23 |

## 工作区说明（提交边界）

S1 gate commit 仅含本门禁证据文件；以下未提交变更**刻意排除**：
- 会话噪声：`.claude/.session-init-lock.json`、`.claude/.skill-load-state.json`、`.claude/memory/mistake-log.md`
- DLL 化战役（另一战役在途文件）：`docs/superpowers/specs/架构设计规格-后端物理拆分-DLL化*.md`、`docs/superpowers/plans/实施计划-后端物理拆分-DLL化.md`
- 未跟踪工具目录：`.agents/`
