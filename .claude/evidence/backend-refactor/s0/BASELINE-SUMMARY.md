# S0 Build/Test Baseline 快照（T0.1）

**日期**：2026-08-26 ｜ **任务卡**：jnpf-v52-goal / todo_11b523c7f36c ｜ **执行者**：opencode-refactor-01

## 结果总览

| 检查项 | 命令 | 结果 | 日志 |
|---|---|---|---|
| 工具链校验 | node scripts/verify-toolchain.mjs | ✅ 30 passed / 1 warned / 0 failed | verify-toolchain.log |
| Hook 拦截能力 | node scripts/test-hooks.mjs | ✅ 44 PASS / 0 FAIL | test-hooks.log |
| Release 构建（含 CI_BUILD 分析器门禁） | dotnet build backend/zx_lowcode_netcore.sln -c Release -p:CI_BUILD=true | ✅ 0 错误 / 26099 警告 / 1m26s | build-release.log |
| 全量测试 | dotnet test ... -c Release --no-build | ✅ 529 通过 / 0 失败（6 个测试工程） | test-release.log |

## 测试明细

JNPF.Tests.CodeGen 27 ｜ JNPF.Tests.OAuth 20 ｜ JNPF.Tests.VisualDev 272 ｜ JNPF.Tests.Common 94 ｜ JNPF.Tests.Architecture 93 ｜ JNPF.Analyzers.Tests 23

## 结论

Build/Test Baseline 建立。此后所有重构波次以此为回归底座：任何改动后重跑同命令集，0 错误 + 529 绿不得回退。

## 遗留

警告量 26099 条为存量技术债基线值，不在本任务治理范围；后续物理拆分波次按模块观测其增减。
