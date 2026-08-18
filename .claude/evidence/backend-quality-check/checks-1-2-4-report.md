# 后端质量检查 1-2-4 汇总

> 生成时间：2026-08-06T14:52:54.125Z
> 证据目录：`.claude/evidence/backend-quality-check/`

## 结论（老板一眼）

| # | 项 | 结果 | 含义 |
|---|----|------|------|
| 1 | 架构（NetArchTest ARCH-01） | **框架层通过；Common.Core 清单失败（预期）** | 核心框架未依赖 InteAssistant；公共层仍挂着 InteAssistant 引用，待拆 Contracts |
| 2 | 复杂度 | **盘点完成；硬门未上** | CC>29 共 **41** 个方法；Analyzer+baseline 尚未落地 |
| 4 | 安全扫描 | **1 条警告** | Security Code Scan：见下方明细 |

## 1. 架构检查

- 工具：NetArchTest + csproj ProjectReference 扫描
- 命令：`dotnet test backend/tests/JNPF.Tests.Architecture` → **3/3 通过**（Common.Core 为清单模式，不阻断）
- `JNPF` 框架程序集 → InteAssistant*：**无依赖（PASS）**
- `JNPF.Common.Core` → InteAssistant*：**有依赖（INVENTORY FAIL，预期）**；失败类型样本数：1
- 非 InteAssistant 工程中含 InteAssistant 字样的 csproj：**3** 个

## 2. 复杂度检查

- 数据源：Codebase-Memory `jnpf-v52` Method.complexity
- 分档：CC>29 = **41**；20–29 = **29**；10–19 = **171**；<10 = **7441**（方法总数 7682）
- 最高：`ImportDataAssemble` CC=138 / 认知=834（VisualDevModelDataService）
- 硬门状态：**未实现**（设计见 backend-quality-remediation W0）
- 明细：`check02-complexity-inventory.json`；业务排序见 `docs/architecture/v52/design-quality-hotspot-top20.md`

## 4. 安全扫描

- 工具：`security-scan` 5.6.7（Security Code Scan）
- 范围：`backend/zx_lowcode_netcore.sln`（排除 tests/tools）
- 结果：**1** 条

- SCS0006 @ file:///D:/JNPF-v52/backend/infrastructure/JNPF.Extras.CollectiveOAuth/Request/AuthRequests/ElemeAuthRequest.cs:256 — Weak hashing function.

## 产物清单

- `check01-architecture-summary.json`
- `arch01-jnpf-framework.json` / `arch01-common-core.json` / `arch01-project-references.json`
- `check02-complexity-inventory.json`
- `check04-security-scan-summary.json` / `security-scan.sarif` / `security-scan.log`
- `checks-1-2-4-summary.json` / `checks-1-2-4-report.md`

## 建议下一步（不自动开干）

1. W0：落地 ComplexityAnalyzer + baseline（只统计/冻结，不立刻 error）
2. ARCH-01：把 Common.Core→InteAssistant.Entitys 抽到 Contracts 后，把清单改成硬失败
3. SCS0006：评估是否可迁到 SHA-256；若为兼容遗留，记豁免理由
