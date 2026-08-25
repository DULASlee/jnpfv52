# 实施计划 — 后端物理拆分（framework/JNPF DLL 化）（规格 v1.0 配套）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 按纯移动纪律把 `framework/JNPF` 巨无霸的 9 个零依赖功能区拆为 3 个独立 DLL（B1/B2 批次），随后建立 NuGet 本地源消费闭环（S4）；全程零行为变更，五件套证据门禁（构建 0 错 / 测试全绿 / 路由快照零 diff / Public API 面冻结 / 文件纯移动守恒）。

**Architecture:** 结构重构型战役——批次级 git commit 作回滚轴；与 RunService 战役（S0~S5）文件面零交集（规格 §3.3），节点错窗推进。

**Tech Stack:** .NET 8 / MSBuild / xUnit / NuGet 本地源

**设计事实源：** `docs/superpowers/specs/架构设计规格-后端物理拆分-DLL化.md`（v1.0，下称规格）

---

## 红线纪律（违反任一=停工）

1. **纯移动纪律**：方法体逐字不改；仅允许新增工程文件、csproj 引用行、必要的 using 行（S0 Task 0.2 预扫描后白名单化）。
2. **命名空间冻结**：迁移区命名空间一字不改，业务工程 `.cs` 零改动。
3. **路由快照零 diff**：每批次门禁，基线复用 RunService 战役机制。
4. **Public API 面冻结**：迁移前 public 类型/成员清单 diff = 0（规格 4 件套之 4）。
5. **零 schema 变更、零数据迁移。**
6. **战役错窗**：RunService 战役路由快照采集/比对窗口内本战役不提交；sln 写入与战役节点错窗。
7. **疑点未溯源不动工**：规格 §5.4 A-1/A-2/A-3 在 S0 闭环前，禁止任何文件迁移。
8. **节点审批门禁**：每阶段（S0~S4）完成后暂停，提交「实现+自检+证据+验收对照」，未经用户批准不得进入下一阶段。

## 命令速查

| 用途 | 命令 | 工作目录 |
|------|------|---------|
| 后端构建 | `dotnet build` | `backend/` |
| 全量测试 | `dotnet test zx_lowcode_netcore.sln` | `backend/` |
| 路由快照 | `dotnet run --project tools/JNPF.Startup.Benchmarks -- --mode routes` | `backend/` |
| Public API 清单 | `grep -rhoE "public [A-Za-z<>\[\],. ]+ [A-Za-z0-9_]+\(" framework/JNPF/{区} --include="*.cs" \| sort > api-{区}.txt` | `backend/` |
| 依赖扫描 | `grep -rhoE "using JNPF[A-Za-z0-9_.]*" framework/JNPF/{区} --include="*.cs" \| sort -u` | `backend/` |

---

## S0 核验与基线（S1 开工前必须全绿）

### Task 0.1：三疑点溯源（2h）

**Files:** ➕ `docs/superpowers/specs/附录-拆分疑点溯源.md`（结论落盘）

- [ ] Step 1：A-1 定位 `JNPF.Extensions` 命名空间物理目录（`grep -rl "namespace JNPF.Extensions" framework/ --include="*.cs"`），确认归内核还是档 A
- [ ] Step 2：A-2 定位 `JNPF.Common.Cache` 命名空间来源，确认是否框架反向依赖业务层；结论写入 B3 设计约束
- [ ] Step 3：A-3 定位 `JNPF.Extensitions.EventBus`（拼写）物理位置
- [ ] Step 4：三结论回填规格 §5.4 假设状态；提交

### Task 0.2：迁移区预扫描（2h）

**Files:** ➕ `docs/superpowers/specs/附录-拆分预扫描.md`

- [ ] Step 1：四区+五区非 .cs 资源清点（嵌入资源/配置文件必须随迁）
- [ ] Step 2：GlobalUsings.cs 逐行分析，标记哪些全局 using 被迁移区依赖 → 生成"允许新增 using 白名单"
- [ ] Step 3：LangVersion/AllowUnsafeBlocks 需求确认（对照 JNPF.csproj `preview`/`true`）；提交

### Task 0.3：基线采集（2h）

- [ ] Step 1：路由快照落盘 `split-s0-routes-baseline.txt`
- [ ] Step 2：九区 Public API 清单生成冻结（api-{区}.txt × 9）
- [ ] Step 3：`dotnet build` + `dotnet test` 全绿基线；构建时间 × 3 取中位落盘；提交

### Task 0.4：NuGet 本地源写法核验（1h）

- [ ] Step 1：确认 nuget.config 相对路径锚点（backend/nuget.config 中相对路径相对于该文件解析），确定本地源 value 正确写法；只写结论，不改文件

**S0 节点审批**：三疑点结论 + 白名单 + 基线文件，等待批准进入 S1。

---

## S1 B1 批次：两个工具 DLL（B1 完成后框架层即物理瘦身）

### Task 1.1：JNPF.Extensions.Cryptography（3h）｜依赖：S0 批准

**Files:** ➕ `framework/JNPF.Extensions.Cryptography/JNPF.Extensions.Cryptography.csproj` ｜ `git mv framework/JNPF/DataEncryption → framework/JNPF.Extensions.Cryptography/DataEncryption` ｜ ✏️ `framework/JNPF/JNPF.csproj`（+ProjectReference）

- [ ] Step 1：建 csproj（继承 framework/Directory.Build.props；按 Task 0.2 结论设 LangVersion/AllowUnsafeBlocks）
- [ ] Step 2：git mv 迁移；JNPF.csproj 加引用（方向：JNPF → 新工程）
- [ ] Step 3：`dotnet build` 0 错误（缺 using 按白名单补）
- [ ] Step 4：五件套证据采集（API 清单 diff=0、路由快照零 diff、测试绿、`git status` 纯 rename）
- [ ] Step 5：契约台账追加 `C-SPLIT-DataEncryption@v1`（public API 清单 SHA256）；提交

### Task 1.2：JNPF.Extensions.Utils（3h）｜依赖：1.1

**Files:** 同构 × 3 区（TimeCrontab / DistributedIDGenerator / LinqBuilder）

- [ ] Step 1~5：同 Task 1.1；额外确认内核消费方（Schedule/TaskQueue/Options）经传递引用编译通过

**S1 节点审批**：两 DLL 证据包 + 台账条目，等待批准。

---

## S2 B2 批次：JNPF.Abstractions（4h）｜依赖：S1 批准

**Files:** ➕ `framework/JNPF.Abstractions/` ｜ git mv × 5 区（Reflection/Modules/Authorization/Configuration/VirtualFileServer）｜ ✏️ JNPF.csproj

- [ ] Step 1~5：同 Task 1.1 节奏；额外确认内核区（App/DependencyInjection/Localization/ObjectMapper/SpecificationDocument）编译通过
- [ ] Step 6：`dotnet sln add` 两批次三工程（与 RunService 战役节点错窗后执行）

**S2 节点审批**。

---

## S3（可选，规格 D6）：JNPF.Caching 试点

- [ ] 前置：A-2 溯源结论落地；消费方 csproj 接线清单生成（哪些业务工程 using JNPF.Cache）
- [ ] 拆分 + 消费方 csproj 批量加引用行（`.cs` 仍零改动）+ 五件套证据；节点审批

## S4 NuGet 消费闭环（4h）

- [ ] Step 1：`nuget.config` 追加本地源（Task 0.4 结论写法）
- [ ] Step 2：`JNPF_PACK=true dotnet build -c Release` 产出 3.5.0 三包
- [ ] Step 3：试点单业务工程切 PackageReference，restore + 构建 + 测试绿
- [ ] Step 4：三工程从 sln 摘除；全量构建时间 × 3 对比 S0 基线，落盘 evidence
- [ ] Step 5：规格 §6 SLO 回填实测数字；**G5 终审**（依赖无环核验 + 契约台账核对 + 时间收益报告）
