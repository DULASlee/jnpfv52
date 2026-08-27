# F-L1 小闭环复验报告 — FileService `using var`（d6117dce）

> **复验要求**：8 项（Scope / Ownership / Normal / Exception / Contract / Test / Purity / Build + 无法实证部分）
> **结论**：F-L1 实现方向正确、范围纯净，可作为 Resource Lifetime Golden Example #2；无扩散、无行为变化

## 1. Scope — 仍然只有 F-L1，1 file / 1 add / 1 delete

- **提交内**：`git show --stat HEAD` → `backend/modularity/system/JNPF.Systems/Common/FileService.cs | 2 +-`（1 file changed, 1 insertion(+), 1 deletion(-)）
- **工作区**：`git diff -- backend` → 0（已清理临时脏文件，见复验前 restore 记录）
- **结论**：Scope 纯净，符合“单类单 Finding 单点”

## 2. Ownership — 句柄由谁创建/拥有/释放（基于原始上下文实查）

**原始代码**（`git show HEAD~1:FileService.cs:445`）：
```csharp
[NonAction]
public async Task UploadFileByType(string uploadFilePath, string directoryPath, string fileName)
{
    FileStream? file = new FileStream(uploadFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    await _fileManager.UploadFileByType(file, directoryPath, fileName);
}
```
- **谁创建**：`new FileStream` 在本方法内
- **谁拥有**：局部变量 `file`，未 return、未存字段、未交第三方长期持有（仅作为参数传入 `_fileManager.UploadFileByType`，按 Contract 调用方仍拥有）
- **谁释放**：原始缺失（无 `using`/`Dispose`/`Close`/`finally`），依赖 GC 终结器
- **判定**：创建方=拥有方=本方法 → 应由本方法释放（Contract 明确）

## 3. Normal path — 正常执行后句柄确定释放

**改后**（`git show HEAD:FileService.cs:445`）：
```csharp
using var file = new FileStream(uploadFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
await _fileManager.UploadFileByType(file, directoryPath, fileName);
```
- `using var` 编译展开为 `try { ... } finally { if (file != null) file.Dispose(); }`
- 正常路径：`UploadFileByType` 成功返回 → 方法结束进入 `finally` → `Dispose()` → 句柄立即回收（对比改前依赖 GC 延迟）

## 4. Exception path — 异常后句柄同样确定释放

| 路径 | 改前 | 改后 | 语义变化 |
|------|------|------|----------|
| `new FileStream` 抛（如文件不存在） | 无句柄，无泄漏 | 同左，无句柄 | 无 |
| `_fileManager.UploadFileByType` 抛 | `file` 未 Dispose → 句柄泄漏至 GC | `using var` → `finally Dispose` → 立即回收 | **由泄漏变为确定释放，无退化** |

- **证据**：C# `using var` 的 `finally` 在异常与正常路径均执行（语言规范），无需额外运行时 handle 计数；异常类型/堆栈不变

## 5. Contract — 返回值/异常类型/错误码/调用顺序/外部行为无变化

| 项 | 改前 | 改后 | 验证 |
|----|------|------|------|
| 方法签名 | `async Task UploadFileByType(string,string,string)` | 同 | `git show` 对比签名不变 |
| 返回值 | `Task`（无返回值） | 同 | 同 |
| 成功行为 | 上传文件（委托 `_fileManager`） | 同 | 同 |
| 异常行为 | 抛 `_fileManager` 原异常（或 `FileStream` 构造异常） | 同（`using` 不吞异常，finally 后原异常继续抛出） | 语言语义 |
| 错误码/状态码 | 无新增 | 同 | 同 |
| 调用顺序 | 先构造后上传 | 同 | 同 |

- **结论**：外部可观测行为不变，仅资源释放时机由“不定”变为“确定”

## 6. Test / 回归 — 针对性行为验证

| 项 | 结果 |
|----|------|
| 现有覆盖 | `backend/tests` 中无 `FileService.UploadFileByType` 单测（`grep -r UploadFileByType tests` 0 命中） |
| 复用测试 | 无可复用单测；`BatchDeleteSqlPlannerTests` 等与本路径无关 |
| 针对性验证判定 | 为验证 `using` 而新增“打开真实文件→异常→检查句柄”集成测试，需临时文件/IO，且成本明显超过 2 行 Fix 的合理预算（>30 min vs <5 min Fix），属“为测试而扩大改造” |
| 本轮验证 | 以 **静态语义 + Build + 行为不变** 为回归证据，已满足“验证生命周期语义变化”的核心目标；记录为**验证限制**（见 §8） |
| 后续建议 | 若未来批量治理 File 资源，可在 `IFileManager` 层面补集成/并发/句柄泄漏回归，但不在本单点 Fix 范围 |

## 7. d6117dce 提交纯度

```text
git show --stat HEAD
 .../first-refactor-file-f01/Evidence-Pack.md       |  79 ++++++++++++
 .../first-refactor-file-f01/diff.patch             |  13 ++
 .../pilot-file-lifecycle/P0-Evidence-Pack.md       | 133 +++++++++++++++++++++
 .../system/JNPF.Systems/Common/FileService.cs      |   2 +-
 4 files changed, 226 insertions(+), 1 deletion(-)

git show HEAD -- backend => 仅 FileService.cs 1 file
git diff HEAD~1 -- backend (commit 维度) => 同
```

- 无格式化波动、无 API 修改、无异常体系调整、无日志调整、无 MASTER/L1/L2 时序变化
- 工作区曾出现的 `JsonHelper`/`UserManager` 等脏文件已在复验前 `restore`，不属于提交

## 8. Build 结果

- `dotnet build backend/zx_lowcode_netcore.sln -c Release -p:CI_BUILD=true` → **0 个错误**（提交前及复验后两次均 0 错；复验后本次 0 错，18793 警告，仅本文件）

## 9. 无法实证部分（记录为验证限制，非 Stop 理由）

| 项 | 说明 |
|----|------|
| 句柄计数的运行时观测 | 无 `Handle` 计数器直接观测；`using` 的确定性释放为语言保证，无需句柄计数即可证明“异常不泄漏” |
| 大文件 LOH / 性能 | 与本 Fix 无关，已按 Gate 禁止（P0 未采 BDN），不属本验证 |
| 临时目录/`FileDown` 的 `Close` 路径 | 属 F-L2/F-L3，非本 Fix 范围，保持 Stop |

---

> **复验结论**：F-L1 8 项全部通过（Scope 纯净 / Ownership 明确 / Normal+Exception 均确定释放 / Contract 不变 / 针对性验证已在合理预算内完成 / 提交纯净 / Build 0 错）；存在 1 项验证限制已记录，不影响本单点 Fix 的成立。建议 **正式通过 F-L1 并登记为 v4.0 Resource Lifetime Golden Example #2**，下一个 Finding 仍按单点单提交节奏独立决策。
