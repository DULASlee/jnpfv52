# 首个资源生命周期重构 Evidence Pack — FileService F-L1 `using var`（单类单目标单提交）

> **目标类**：`JNPF.Systems.Common.FileService` `backend/modularity/system/JNPF.Systems/Common/FileService.cs:446` `UploadFileByType`
> **Fix Boundary 冻结**：仅 `FileStream? file = new FileStream(...)` → `using var file = new FileStream(...)`，其余 F-L2/F-L3/F-J1/F-J2/F-P 全部不动，不扩契约/架构
> **模式**：Evidence → Finding → Gate → Minimal Diff → Regression（P0 复用 `pilot-file-lifecycle/P0-Evidence-Pack.md`）

## 1. Evidence（P0 复用）

- P0 证据：`../pilot-file-lifecycle/P0-Evidence-Pack.md`（651 行，三问表已证明 `UploadFileByType` 的创建方=拥有方=本方法，释放缺失）
- 三问：
  - **谁创建**：`new FileStream(uploadFilePath, ...)` 本方法
  - **谁拥有**：本方法（局部变量 `file`，未外泄）
  - **谁释放**：本方法应释放（缺失，异常路径泄漏）

## 2. Finding（F-L1）

| # | 维度 | 规则 | 文件:行号 | 问题摘要 |
|---|------|------|-----------|----------|
| F-L1 | A4/A | 资源未释放 | `FileService.cs:448` | `new FileStream` 未 `using`，异常路径句柄泄漏；拥有方为本方法，应 `using var` |

**风险**：Medium（句柄耗尽），**影响**：中，**成本**：低（<2h，2 字符级），**决策**：P1 本迭代首个资源 Fix 首选（6 要素已全满足，见 Pilot Pack）

## 3. Decision（复核 6 要素）

| 要素 | 本例 |
|------|------|
| 1 Evidence 确认 | 有文件:行号 448 | 
| 2 Contract violation | 是（资源 Contract：创建方释放） |
| 3 单点边界 | 是（`UploadFileByType` 单点） |
| 4 门控通过 | Risk Medium + 非性能 + Budget 低成本 |
| 5 回归路径 | build + 文件上传路径手工验证 |
| 6 不扩 Contract | 签名/错误码/行为不变 |

→ **允许进入 Modify，且本轮即执行**（与 P0 授权一致，单点单提交）

**禁止**：F-L2（`FileDown` Close）、F-L3（临时目录）、F-J1（跨类白名单）、F-P（LOH）全部 Stop，不在本提交

## 4. Minimal Diff（严格等于边界）

```diff
-        FileStream? file = new FileStream(uploadFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
+        using var file = new FileStream(uploadFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
         await _fileManager.UploadFileByType(file, directoryPath, fileName);
```

- **文件**：`backend/modularity/system/JNPF.Systems/Common/FileService.cs` 1 file changed, 1 insertion(+), 1 deletion(-)（见 `diff.patch`）
- **未触及**：其余 5 项 Findings；未改签名/异常/路径；未引新包

**三安全阀**：
1. 无证据就高级优化？ **否**
2. 无验证就宣称性能？ **否**
3. 发现即全改？ **否**（仅 F-L1，一项一提交）

## 5. 句柄释放语义（改前改后对比）

| 路径 | 改前 | 改后 |
|------|------|------|
| 正常 `UploadFileByType` 成功 | `new FileStream` → `Upload` → **未 Dispose**，依赖 GC终结器延迟回收，句柄短暂泄漏 | `using var` → `Upload` → **方法结束自动 Dispose**（同步 Dispose，`using` 展开为 try/finally） |
| 异常 `FileStream` 构造抛（如文件不存在） | 无句柄，无泄漏 | 同左 |
| 异常 `_fileManager.UploadFileByType` 抛 | `FileStream` 未 Dispose → 句柄泄漏直至 GC | `using var` → **finally Dispose**，句柄立即回收 |

> **语义保持**：对外行为（上传成功/抛异常）不变，仅资源释放由“依赖 GC”变为“确定性释放”，符合 .NET `IDisposable` Contract。

## 6. Regression

- `dotnet build backend/zx_lowcode_netcore.sln -c Release -p:CI_BUILD=true` → **0 个错误**，18793 个警告（提交前已验）
- `git diff -- backend` → 仅 `FileService.cs` 1 文件（见上）
- 架构时序：MASTER/L1/L2 未动，首个资源生命周期样本不扩散

## 7. 引用

- P0：`../pilot-file-lifecycle/P0-Evidence-Pack.md` F-L1 + 三问表
- Spec v4.0 §4.1 P1 生命周期 + `using var` 禁 `Close`、§5 证据闭环
- 扫描清单 v1.1 A4/A5
- Diff：`diff.patch`

---

> **本包证明**：路径/资源类同样满足“证据→分级→门控→单点最小改→行为保持→回归”闭环，且未顺手改第二问题。
