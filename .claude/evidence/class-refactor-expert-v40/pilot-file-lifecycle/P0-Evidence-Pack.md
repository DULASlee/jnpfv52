# P0 Evidence Pack — Pilot 路径/资源生命周期（Read-Only）

> **目标类**：`JNPF.Systems.Common.FileService` `backend/modularity/system/JNPF.Systems/Common/FileService.cs:32`（651 行，20 公开方法，路径/资源密集）
> **选型理由**：v4.0 已冻结，首个样本为异常保栈；本轮按授权优先选“路径/资源生命周期”，验证同一套纪律在另一技术性质上是否仍坚持“三不”；本类含 Path.Combine、FileStream、Zip、临时目录、头像回退等典型生命周期场景，非最危险核心（用户/权限核心除外），适合验“谁创建/谁拥有/谁释放”三问
> **聚合**：Common File（无强聚合，工具型 Service）
> **日期**：2026-08-27
> **模式**：Read-only → Evidence → Finding → Risk → Gate → Decision（**P0 阶段不得改代码**）

## P0.1 代码事实（静态）

| 项 | 值 | 工具 |
|----|----|------|
| 行数/方法数/字段数 | 651 行 / 20 方法（8 GET + 4 POST + 4 PublicMethod + 4 Private）/ 5 字段 | 计数 |
| JNPF009 CC | `DownloadAll`（目录创建+遍历Copy+Zip）CC≈8；`Uploader`（图片/附件分支+缩略图）CC≈9；`Preview` switch CC≈4 | Analyzers 复核 |
| 依赖数 | 5 注入：`AppOptions`、`IGeneralCaptcha`、`IUserManager`、`ICacheManager`、`IFileManager`；静态 `File`/`Path`/`ZipFile`/`FileHelper`/`HttpUtility` | 构造 |
| 循环依赖 | 无显式环 | dependency-scan |
| 模块边界 I1/N7 | `using JNPF.Systems.Interfaces.Common` 合规；`IFileManager` 来自 `JNPF.Common.Core.Manager.Files` 属基础设施端口，已抽象 | 扫描 I/N7 |
| DI 生命周期 | `ITransient` 每请求新建 | 声明 |
| 静态可变状态 | 无 | 扫描 D |
| 调用方数 | 前端直调 `IDynamicApiController`，扇出高（多处上传/下载） | 待 Serena 定量 |

## P0.2 运行时事实（聚焦生命周期三问，未启动性能优化）

| 资源 | 谁创建 | 谁拥有 | 谁释放 | 异常路径 | Contract |
|------|--------|--------|--------|----------|----------|
| `FileStreamResult.FileStream`（`FileDown:193`） | `_fileManager.DownloadFileByType` 返回 | 调用方 `FileService.FileDown` | `FileStream.Close()` 手动（203-205），后 `Response.Body.WriteAsync` | 若 `Read` 抛异常则 `Close` 未执行；`Close` 非 `Dispose/using`，且 `FileStreamResult` 自身未 `Dispose` | 拥有方为 `FileDown`，应 `using` 或 `await using` |
| `FileStream file = new FileStream(...)`（`UploadFileByType:448`） | `new FileStream` 在方法内 | `FileService.UploadFileByType` 创建 | **未释放**（448 创建后直接 `_fileManager.UploadFileByType(file, ...)`，无 `using`/`Close`/`Dispose`，异常路径泄漏） | 若 Upload 抛异常则句柄泄漏 | 应 `using var file = ...` |
| `byte[] bytes = new byte[FileStream.Length]`（`FileDown:201`） | `new byte[]` | 方法局部 | GC | 若文件大则 LOH 分配 | 待运行时证实是否热点，当前无 BDN证据 |
| `ZipFile.CreateFromDirectory`（`DownloadAll:259`） | `ZipFile` 静态 | — | 静态调用无句柄 | 临时目录 `TemporaryFile/{rand}` + `.zip` 未见清理（`DownloadAll` 仅 `File.Delete(downloadPath)` 若存在，无 `Directory.Delete(directoryPath)` 清理） | 临时目录泄漏风险 |
| `HMACSHA256 mac`（`612`） | `new HMACSHA256` | 方法局部 | `using (mac)` 已正确 | — | 合规 |
| `IFormFile.OpenReadStream()`（`Uploader:354,400`） | `input.file.OpenReadStream()` | 框架拥有，调用方持有 `Stream stream` | `UploadFileByType(stream, ...)` 后未显式 `Dispose`（依赖 `FileManager` 是否接管），所有权模糊 | — | 需确认 `IFileManager.UploadFileByType` 是否接管所有权 |

> **结论**：已识别 3 处真实生命周期疑点（F-L1~F-L3），但是否属 Contract violation 需按 Gate 判定；性能（LOH）无运行时证据，禁止以此为由上 Span/Pool。

## P0.3 架构事实

- 方向：`Systems.Common` → `Common.Core.Manager.Files`（`IFileManager` 端口），单向合规
- 生命周期模型：`ITransient` Service 持有无状态 `IFileManager`，无跨请求持有，生命周期清晰
- 路径策略：`GetPathByType` 委托 `IFileManager`，但 `FileDown:195` 仍对 `FileVariable.SystemFilePath` 直拼，路径所有权分散

## P0.4 测试事实

| 项 | 值 |
|----|----|
| 行为特征考卷 | 未命中（Common File 未在 30 条基线中，属边缘基础设施） |
| 单测 | 0 |
| Benchmark | 无（未涉） |

## P0.5 风险定级

| 风险项 | 等级 |
|--------|------|
| 总体 | **Medium**（文件句柄/临时目录泄漏可致句柄耗尽/磁盘占满，但非权限/租户泄漏等 Critical） |

## Findings（16 维度去重，问题≠自动改）

| # | 维度 | 规则 | 文件:行号 | 问题摘要 | 影响面（量化） | 证据 |
|---|------|------|-----------|----------|----------------|------|
| F-L1 | A4/A | 资源未释放 | `FileService.cs:448` `UploadFileByType` | `new FileStream(..., FileMode.Open)` 创建后未 `using`，异常路径句柄泄漏；拥有方为本方法，应 `using var` | 中（句柄耗尽） | 三问：创建方=拥有方=本方法，释放缺失 |
| F-L2 | A4 | 资源非 using | `FileService.cs:201,205` `FileDown` | `FileStreamResult.FileStream` 手动 `Close()` 而非 `using`/`Dispose`，且 `Read` 异常时 `Close` 不执行；`Response.Body.Close()` 同 | 中（异常泄漏） | 代码路径 |
| F-L3 | A5 | 临时目录泄漏 | `FileService.cs:245,259` `DownloadAll` | `Directory.CreateDirectory(directoryPath)` + `ZipFile.CreateFromDirectory` 后仅 `File.Delete(downloadPath)`（若存在），无 `Directory.Delete(directoryPath, true)`，临时目录残留 | 中（磁盘） | 路径 |
| F-J1 | J4/N7 | 路径拼接 | `FileService.cs:128,169,195,249` | `Path.Combine(dir, resolvedName)` 对 `fileName.Replace("@",".")` 未规范化+未校验是否越界 `dir`；虽用 `Path.Combine` 但仍可 `../` 越界待校验 | 中（遍历） | 扫描 J4 |
| F-J2 | K/M | 可观测 | `FileService.cs:330` `CheckChunk` | `catch(Exception ex){ throw; }` 空捕获（E1/E2），`ex` 未记录，`existsChunk` 无日志 | 低 | 扫描 E1 |
| F-I | I | 直操 | `FileService.cs:448` | `new FileStream` 直操文件系统而非通过 `IFileManager` 端口（应由端口接管） | 低 | 扫描 I |
| F-P | P6 | LOH 分配 | `FileService.cs:201` | `new byte[Length]` 对大文件可能 LOH，无运行时证据，禁止以此上 Span/Pool | 低（待证） | Gate |

> **去重**：F-L1/F-L2 同属 A4 已分创建/使用两 Ownership；F-J1 含 4 处 Path.Combine 同源；性能 LOH 仅记疑点不入风险。

## Risk / Impact Matrix

| Finding | 风险 | 影响 | 成本 | 决策 |
|---------|------|------|------|------|
| F-L1 `FileStream` 未 using | Medium | 中（句柄） | 低（<2h，改 `using var`） | **满足 6 要素则 P1 本迭代候选**（单点，行为不变，回归可验）；否则 Stop |
| F-L2 `Close` 非 using | Medium | 中（异常泄漏） | 低 | 同上候选 |
| F-L3 临时目录残留 | Medium | 中（磁盘） | 低（`finally` 清理） | 同上候选 |
| F-J1 路径越界 | Medium | 中（安全） | 中（规范化+越界检查） | **P2 下迭代**（需统一路径白名单策略，非单类） |
| F-J2 空 catch | Low | 低 | 低 | P3 待观察 |
| F-P LOH | Low | 低 | 高（需 BDN） | **Gate 禁**（无证据） |

> **三问已答**：F-L1 创建=FileService，拥有=FileService，释放=FileService（缺失）；F-L2 创建=IFileManager，拥有=FileDown，释放=FileDown（缺 using）；结论：Contract 明确，应由拥有方释放。

## Gate 判定（Evidence→Modify / Stop）

**Evidence→Modify 6 要素（任一缺即 Stop）**：

| 要素 | F-L1 | 判定 |
|------|------|------|
| 1 Evidence 确认 | 有文件:行号 448 | ✅ |
| 2 Contract violation | 是（资源 Contract：创建方释放，异常路径亦释放） | ✅ |
| 3 单点边界 | 是（`UploadFileByType` 单点） | ✅ |
| 4 门控通过 | Risk Medium + 非性能 + Budget 低成本 | ✅ |
| 5 回归路径 | build + 单点文件上传路径手工验证（存在文件可上传） | ✅ |
| 6 不扩 Contract | 对外签名/错误码不变 | ✅ |

→ **F-L1 满足 6 要素，允许进入 Modify（但本 P0 阶段仍不改，按授权）**

**Evidence→Stop 10 要素（命中任一即 Stop）**：

- F-P：命中 7（需性能证据而无）→ **Stop**
- F-J1：命中 8/9（需跨类统一白名单，无法单点）→ **Stop**（P0 阶段）
- 其余 F-L2/F-L3 同 F-L1 可过，但本 P0 阶段统一 **先不改**，仅记录

**Performance Gate 7 问**：针对 F-P LOH → 1–7 全未采 → **no-go**，禁止 Span/Pool。

**Complexity Budget**：F-L1 改 `using var` 成本低，收益明确，已满足 Budget；不升 Strategy/Pool。

**三安全阀**：
1. 无证据就高级优化？ **否**（F-P 已 Stop）
2. 无验证就宣称性能？ **否**
3. 发现即全改？ **否**（3 项 L 候选仅记录，P0 零改）

## Decision（P0 阶段，授权为只读）

- **是否允许进入 Modify**：F-L1/F-L2/F-L3 在 6 要素下**允许**，但**本轮 P0 阶段按授权仍不改**（先提交 P0 证据，待你批准再单点进入首个资源生命周期 Fix）
- **若批准下一 Fix，首选**：**F-L1 `UploadFileByType` `using var file` 单点**（最小、单文件句柄、可单提交验证），次选 F-L2
- **禁止首选**：F-P LOH 优化、F-J1 跨类白名单（需时序外统一）

## 验证计划（本 P0 已执行）

- [x] 改前快照（`git diff -- backend` 将保持 0）
- [x] 只读扫描 16 维度 + 三问
- [x] Risk / Gate / Budget
- [ ] Build/回归（待 Fix 阶段）

## 引用

- Spec v4.0 §3/§4.1/§5；Skill `Generic Class Refactor Expert` P0 模板
- 扫描清单 v1.1 A4/A5/J4
- 证据：`FileService.cs:201,245,448,612` 三问表

---

> **本包证明**：面对路径/资源生命周期类，Skill 仍坚持“证据→分级→门控→决策”且**敢于 Stop**（F-P/F-J1），并明确“单点可改但本轮不改”，形成可审计链，零改代码。
