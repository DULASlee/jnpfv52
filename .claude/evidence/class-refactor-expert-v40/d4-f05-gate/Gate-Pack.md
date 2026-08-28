# D4-F05 (NoLeak) 准入门控包 — FileService `FileDown` `FileStreamResult` 未 `using`（P0 复用，不改代码）

> **状态**：**仅准入阶段，未批准实现**（按授权：先提交 6 要素+10 要素+验证方案，暂不改代码）  
> **目标类**：`JNPF.Systems.Common.FileService` `backend/modularity/system/JNPF.Systems/Common/FileService.cs:193` `FileDown`  
> **Finding**：`FileStreamResult.FileStream` 手动 `Close()`，异常路径泄漏，未 `using`/`Dispose`，`Response.Body.Close()` 同  
> **复用 P0**：`../pilot-file-lifecycle/P0-Evidence-Pack.md` F-L2（A4，Medium）

## 1. 6 要素准入（全满足才允许进入 Modify，任一缺即 Stop）

| # | 要素 | 本 Finding 是否满足 | 证据 |
|---|------|---------------------|------|
| 1 | Finding 已被证据确认 | ✅ 满足 | `FileService.cs:200-205` `fileStreamResult.FileStream.Read` + `Close()`，P0 三问已记录创建方 `_fileManager`、拥有方 `FileDown`、释放缺 `using` |
| 2 | 属于明确 Contract violation 或已批准类别 | ✅ 满足 | 资源 Contract：拥有方释放；异常路径亦释放（A4） |
| 3 | Fix Boundary 可单点定义 | ✅ 满足 | 单方法 `FileDown` 内 `FileStreamResult` 的 `using` 化，可单点描述，无需跨类 |
| 4 | 风险门控通过 | ✅ 满足 | Risk Medium + 非性能 + Budget 低成本（`using`/`await using`） |
| 5 | 回归验证路径存在 | ⚠️ 待补但可满足 | `dotnet build` 必过；行为特征：下载小文件的 200 vs 404/500 回归可手工验证；若需句柄泄漏观测，可在验证方案中定为“代码语义+Build”为主，运行时 Handle 计数为可选 |
| 6 | 不扩大公共 Contract | ✅ 满足 | 方法签名 `Task FileDown(string,[FromQuery]string)` 不变，调用方（前端直调）无感 |

→ **初步判定**：6 要素中 5 项明确满足，1 项（回归路径）需在下节验证方案中补齐细节后即满足；无硬性 Stop 要素。

## 2. 10 要素拒绝门（命中任一即 Stop）

| # | 拒绝条件 | 本 Finding 是否命中 | 判定 |
|---|----------|---------------------|------|
| 1 | 只有猜测无证据 | 否 | 有文件:行号 200-205 |
| 2 | 仅 Capability 缺失 | 否 | 属 Contract violation（A4） |
| 3 | 仅 Test gap | 否 | 非仅缺测试 |
| 4 | Not a defect | 否 | 异常路径 `Read` 抛时 `Close` 不执行，属实 |
| 5 | 需扩大公共 Contract | 否 | 不扩 |
| 6 | 需引入新架构 | 否 | 仅 `using` |
| 7 | 需高级优化但无性能证据 | 否 | 非性能 |
| 8 | 无法保持单点 | 否 | 单点 `FileDown` |
| 9 | 会牵连其他类/模块 | 否 | 不牵 |
| 10 | 回归无法验证 | 否 | 可 build + 下载回归（见下） |

→ **命中 0 项**，无拒绝。

## 3. 风险自适应验证方案（未批准实现前冻结边界与成功标准）

**Fix 边界冻结**（若批准实现，仅允许）：
```csharp
// 拟：FileDown 内
using var fs = fileStreamResult.FileStream; // 或 await using + using var result
try { Read → WriteAsync } finally { fs.Dispose() } // 由 using 展开
// 禁：改 Response.Body 语义、改 DownloadFileByType 返回类型、改异常体系、改路径越界逻辑
```
*成功标准*：正常下载小文件仍 200 且内容一致；异常（如文件不存在抛）后句柄确定释放（语义 via `using` finally）；对外错误码/状态码不变；`git diff -- backend` 仅本文件。

**验证预算（按 Medium 风险自适应）**：
| 验证项 | 方式 | 成本 | 是否必需 |
|--------|------|------|----------|
| Scope 纯净 | `git diff -- backend` 1 file | 低 | 必需 |
| Ownership 三问 | 代码实查（创建 `_fileManager` → 拥有 `FileDown` → 释放 `FileDown`） | 低 | 必需 |
| Normal/ Exception 语义 | 代码语义（`using` finally）+ 人工下载一次 200 | 低 | 必需 |
| Contract 不变 | 签名/错误码对比 | 低 | 必需 |
| Build | `dotnet build -c Release -p:CI_BUILD=true` 0 错 | 低 | 必需 |
| 运行时 Handle 计数 | 需句柄计数器/压测，非必需；若成本 > Fix 预算则记为验证限制，不扩大测试 | 中 | 可选，超预算则 Stop 记录 |

**三安全阀**（本 Finding）：
1. 无证据就高级优化？ **否**（非性能）
2. 无验证就宣称性能？ **否**
3. 发现即全改？ **否**（仅 F-L2，已与 F-L1/F-L3 隔离）

---

> **本包结论**：D4-F05（FileDown NoLeak）在 6 要素准入上**已满足/可补齐**，10 要素拒绝门 **0 命中**，验证方案已按风险自适应冻结为“代码语义+Build+一次下载回归”低成本闭环，无需为 2-3 行修复扩测试。**等待你批准实现**，批准后将按此边界单提交进入 `using` 化，仍保持 `Golden` 双样本冻结不变。
