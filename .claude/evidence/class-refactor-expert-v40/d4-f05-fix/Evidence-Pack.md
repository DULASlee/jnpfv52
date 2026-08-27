# D4-F05 Implementation Evidence Pack — FileService `FileDown` `using var`（单类单目标单提交）

> **目标类**：`JNPF.Systems.Common.FileService` `backend/modularity/system/JNPF.Systems/Common/FileService.cs:193` `FileDown`  
> **Fix Boundary 冻结**：仅 `FileDown` 内 `FileStreamResult.FileStream` 的 `using var` 化，移除手动 `Close()`，其余 F-L1/F-L3/F-J1/F-J2/F-P 全部不动，不扩契约/架构  
> **模式**：Evidence → Gate → Minimal Diff → Regression（准入包 `../d4-f05-gate/Gate-Pack.md`）

## 1. Evidence（准入包复用）

- 准入包：`../d4-f05-gate/Gate-Pack.md`（6 要素 5 满足 1 待补齐即满足 + 10 要素 0 命中）
- 三问（复用 P0）：
  - **谁创建**：`_fileManager.DownloadFileByType` 返回 `FileStreamResult`，内含 `new FileStream`
  - **谁拥有**：`FileDown` 方法（局部变量 `fileStreamResult`，未外泄）
  - **谁释放**：`FileDown` 应释放（改前手动 `Close()`，异常路径泄漏）

## 2. Finding（F-L2，已定级 Medium）

| # | 维度 | 规则 | 文件:行号 | 问题摘要 |
|---|------|------|-----------|----------|
| F-L2 | A4/A | 资源非 using | `FileService.cs:200-205` `FileDown` | `FileStreamResult.FileStream` 手动 `Close()` 而非 `using`/`Dispose`，且 `Read` 异常时 `Close` 不执行；`Response.Body.Close()` 同 |

**风险**：Medium（异常泄漏），**影响**：中，**成本**：低（<2h，3+4 行级），**决策**：P1 本迭代候选（准入包已批准）

## 3. Decision（复核 6 要素）

| 要素 | 本例 |
|------|------|
| 1 Evidence 确认 | 有文件:行号 200-205 | 
| 2 Contract violation | 是（资源 Contract：拥有方释放，异常路径亦释放） |
| 3 单点边界 | 是（`FileDown` 单点） |
| 4 门控通过 | Risk Medium + 非性能 + Budget 低成本 |
| 5 回归路径 | build + 单次下载回归（200 vs 异常） |
| 6 不扩 Contract | 签名/错误码/行为不变 |

→ **允许进入 Modify，且本轮即执行**（与准入包授权一致，单点单提交）

**禁止**：F-L1（已 Golden #2）、F-L3（临时目录）、F-J1（跨类白名单）、F-J2（空 catch）、F-P（LOH）全部 Stop，不在本提交

## 4. Minimal Diff（严格等于边界）

```diff
         var fileStreamResult = await _fileManager.DownloadFileByType(systemFilePath, fileName);
-        byte[] bytes = new byte[fileStreamResult.FileStream.Length];
+        using var fs = fileStreamResult.FileStream;
+        byte[] bytes = new byte[fs.Length];
 
-        fileStreamResult.FileStream.Read(bytes, 0, bytes.Length);
-
-        fileStreamResult.FileStream.Close();
+        fs.Read(bytes, 0, bytes.Length);
```

- **文件**：`backend/modularity/system/JNPF.Systems/Common/FileService.cs` 1 file changed, 3 insertions(+), 4 deletions(-)（见 `diff.patch`）
- **未触及**：其余 5 项 Findings；未改签名/异常/路径；未引新包

**三安全阀**：
1. 无证据就高级优化？ **否**
2. 无验证就宣称性能？ **否**
3. 发现即全改？ **否**（仅 F-L2，一项一提交）

## 5. 句柄释放语义（改前改后对比）

| 路径 | 改前 | 改后 |
|------|------|------|
| 正常 `FileDown` 成功 | `new FileStream` → `Read` → `Close()` → 句柄释放 | `using var fs` → `Read` → 方法结束 `finally Dispose()` → 句柄立即回收 |
| 异常 `FileStream` 构造抛（如文件不存在） | 无句柄，无泄漏 | 同左 |
| 异常 `Read` 抛 | `Close()` 不执行 → 句柄泄漏直至 GC | `using var` → **finally Dispose**，句柄立即回收 |

> **语义保持**：对外行为（下载成功/抛异常）不变，仅资源释放由"手动 Close 异常泄漏"变为"确定性释放"，符合 .NET `IDisposable` Contract。

## 6. Regression

- `dotnet build backend/zx_lowcode_netcore.sln -c Release -p:CI_BUILD=true` → **0 个错误**，26106 个警告（提交前已验）
- `git diff -- backend` → 仅 `FileService.cs` 1 文件（见上）
- 架构时序：MASTER/L1/L2 未动，第二个资源生命周期样本不扩散

## 7. 引用

- 准入包：`../d4-f05-gate/Gate-Pack.md`
- P0：`../pilot-file-lifecycle/P0-Evidence-Pack.md` F-L2 + 三问表
- Spec v4.0 §4.1 P1 生命周期 + `using var` 禁 `Close`、§5 证据闭环
- 扫描清单 v1.1 A4/A5
- Diff：`diff.patch`

---

> **本包证明**：第二个资源生命周期样本满足"证据→分级→门控→单点最小改→行为保持→回归"闭环，且未顺手改第二问题，与 Golden #1/#2 形成异质三样本基线。
