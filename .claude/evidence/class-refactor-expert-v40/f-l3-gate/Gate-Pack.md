# F-L3 Gate Pack — DownloadAll 临时目录生命周期

> **状态**：准入门控阶段，不改生产代码  
> **Finding**：F-L3 — DownloadAll 方法创建临时目录和 zip 文件后未清理，导致磁盘空间持续占用  
> **原始 Stop 理由**：需跨类策略（临时文件清理机制）  
> **Decision Pack 结论**：Go（认为可单点修复）  
> **Gate Pack 结论**：**STOP**（重新评估后发现是跨层 ownership 问题）

---

## 1. 代码上下文分析

### DownloadAll 方法（240-264 行）

```csharp
public async Task<dynamic> DownloadAll(string type, [FromBody] List<FileControlsModel> input)
{
    var fileName = RandomExtensions.NextLetterAndNumberString(new Random(), 7);
    // 临时目录
    string directoryPath = Path.Combine(App.GetConfig<AppOptions>("JNPF_App", true).SystemPath, "TemporaryFile", fileName);
    Directory.CreateDirectory(directoryPath);  // ← 创建临时目录
    foreach (var item in input)
    {
        string filePath = Path.Combine(GetPathByType(type), item.fileId.Replace("@", "."));
        await _fileManager.CopyFile(filePath, Path.Combine(directoryPath, item.fileName));
    }
    // 压缩文件
    string downloadPath = directoryPath + ".zip";
    
    if (File.Exists(downloadPath))
        File.Delete(downloadPath);
    
    ZipFile.CreateFromDirectory(directoryPath, downloadPath);  // ← 创建 zip 文件
    if (!App.Configuration["OSS:Provider"].Equals("Invalid"))
        await UploadFileByType(downloadPath, "SystemPath", string.Format("文件{0}.zip", fileName));
    var downloadFileName = string.Format("{0}|{1}.zip|TemporaryFile", _userManager.UserId, fileName);
    _cacheManager.Set(fileName + ".zip", string.Empty);  // ← 设置缓存标记
    return new { downloadName = string.Format("文件{0}.zip", fileName), downloadVo = new { name = fileName, url = "/api/File/Download?encryption=" + DESCEncryption.Encrypt(downloadFileName, "JNPF") } };
}
```

### DownloadFile 方法（269-295 行）

```csharp
public async Task<dynamic> DownloadFile([FromQuery] string encryption, [FromQuery] string name)
{
    string decryptStr = DESCEncryption.Decrypt(encryption, "JNPF");
    List<string> paramsList = decryptStr.Split("|").ToList();
    if (paramsList.Count > 0)
    {
        string fileName = paramsList.Count > 1 ? paramsList[1] : string.Empty;
        if (_cacheManager.Exists(fileName))
        {
            _cacheManager.Del(fileName);  // ← 仅删除缓存标记，不清理临时文件
        }
        else
        {
            throw Oops.Oh(ErrorCode.D1805);
        }
        string type = paramsList.Count > 2 ? paramsList[2] : string.Empty;
        string filePath = Path.Combine(GetPathByType(type), fileName.Replace("@", "."));
        string fileDownloadName = name.IsNullOrEmpty() ? fileName : name;
        return await _fileManager.DownloadFileByType(filePath, fileDownloadName);  // ← 下载临时文件
    }
    else
    {
        throw Oops.Oh(ErrorCode.D8000);
    }
}
```

---

## 2. Ownership 分析（关键）

### 资源生命周期

```
DownloadAll（创建者）
  ↓
创建临时目录 + zip 文件
  ↓
返回下载链接（/api/File/Download?encryption=...）
  ↓
用户点击下载链接
  ↓
DownloadFile（使用方）
  ↓
下载临时文件
  ↓
删除缓存标记（但不清理临时文件）
  ↓
临时文件永久残留
```

### Ownership 判定

| 问题 | 答案 |
|------|------|
| 谁创建？ | DownloadAll 方法 |
| 谁使用？ | DownloadFile 方法（异步，不同时间执行） |
| 谁清理？ | **无人清理** |
| 能否在 DownloadAll 内清理？ | **不能**（会破坏 DownloadFile 的下载功能） |

### 结论

这是一个**跨层 ownership** 问题：
- **DownloadAll** 创建临时资源
- **DownloadFile** 使用临时资源（异步，不同时间执行）
- **两者都没有清理责任**
- 需要**全局临时文件清理机制**（如定时任务、后台服务）

---

## 3. 6 要素准入（重新评估）

| 要素 | 判定 | 理由 |
|------|------|------|
| 1. Evidence 确认 | ✅ 满足 | 代码实查有临时目录未清理 |
| 2. Contract violation | ✅ 满足 | 资源 Contract（创建方清理） |
| 3. 单点边界 | ❌ **不满足** | 无法在 DownloadAll 内单点修复，需要跨方法/跨层策略 |
| 4. 门控通过 | ❌ **不满足** | 改造半径超出单类单点 |
| 5. 回归路径 | ⚠️ 可满足 | build + 检查临时目录 |
| 6. 不扩 Contract | ✅ 满足 | 对外行为不变 |

**结论**：6 要素中 **2 项不满足**（单点边界、门控通过），**不满足准入条件**。

---

## 4. 10 要素门控

| 要素 | 判定 | 理由 |
|------|------|------|
| 1. 只有猜测无证据 | ❌ 不命中 | 有代码实查 |
| 2. 仅 Capability 缺失 | ❌ 不命中 | 属 Contract violation |
| 3. 仅 Test gap | ❌ 不命中 | 非仅缺测试 |
| 4. Not a defect | ❌ 不命中 | 临时文件确实未清理 |
| 5. 需扩大公共 Contract | ❌ 不命中 | 不扩 |
| 6. 需引入新架构 | ✅ **命中** | 需要全局临时文件清理机制（定时任务/后台服务） |
| 7. 需高级优化但无性能证据 | ❌ 不命中 | 非性能 |
| 8. 无法保持单点 | ✅ **命中** | 跨方法/跨层 ownership |
| 9. 会牵连其他类/模块 | ✅ **命中** | 需要 DownloadFile 配合或全局清理机制 |
| 10. 回归无法验证 | ❌ 不命中 | 可验证 |

**结论**：10 要素中 **3 项命中**（6、8、9），**应继续 Stop**。

---

## 5. 验证方案（假设性，不实施）

如果强行在 DownloadAll 内用 `try/finally` 清理：

```csharp
public async Task<dynamic> DownloadAll(string type, [FromBody] List<FileControlsModel> input)
{
    var fileName = RandomExtensions.NextLetterAndNumberString(new Random(), 7);
    string directoryPath = Path.Combine(App.GetConfig<AppOptions>("JNPF_App", true).SystemPath, "TemporaryFile", fileName);
    Directory.CreateDirectory(directoryPath);
    try
    {
        // ... 复制文件、压缩、上传 ...
        return new { ... };
    }
    finally
    {
        // ❌ 错误：如果在 finally 内清理，DownloadFile 就无法下载了
        if (Directory.Exists(directoryPath))
            Directory.Delete(directoryPath, true);
        if (File.Exists(downloadPath))
            File.Delete(downloadPath);
    }
}
```

**问题**：
- DownloadAll 返回下载链接后，用户可能**几分钟后**才点击下载
- 如果在 DownloadAll 内清理，DownloadFile 就无法下载了
- 这是一个**异步 ownership** 问题，不能用同步的 `try/finally` 解决

---

## 6. 改动预算

**不实施**，因为：
- 无法在单类单点内修复
- 需要全局临时文件清理机制
- 超出当前重构范围

---

## 7. 最终 Gate Decision

### **STOP**

**理由**：
1. **6 要素准入不满足**：单点边界、门控通过 2 项不满足
2. **10 要素门控命中 3 项**：需引入新架构、无法保持单点、会牵连其他类/模块
3. **跨层 ownership 问题**：DownloadAll 创建临时资源，DownloadFile 使用临时资源，两者都没有清理责任
4. **异步 ownership**：创建和使用在不同时间执行，不能用同步的 `try/finally` 解决
5. **原 Stop 理由正确**：Decision Pack 中的分析有误，错误地认为可以单点修复

**Decision Pack 结论修正**：
- Decision Pack 认为"可在 DownloadAll 方法内用 `try/finally` 清理"是**错误的**
- 正确的结论是：**需要全局临时文件清理机制**（如定时任务、后台服务）
- F-L3 应继续 Stop，不在当前重构范围内

---

## 8. 下一步建议

### 选项 A：继续 Stop（推荐）

F-L3 继续冻结，不在当前重构范围内处理。FileService 当前局部重构阶段暂时收敛。

### 选项 B：推荐换类

进入下一个候选类的类级专家审查，寻找更适合单点修复的 Finding。

### 选项 C：升级到架构层（不在当前范围）

如果未来需要处理临时文件清理问题，应作为**架构层任务**：
- 设计全局临时文件清理机制
- 引入定时任务/后台服务
- 明确临时文件的生命周期策略（如 24 小时后自动清理）

但这**不在当前类级重构范围内**。

---

## 9. 反思

### Decision Pack 的错误

Decision Pack 错误地认为 F-L3 可以单点修复，原因是：
- 没有充分分析临时目录的 ownership
- 没有考虑 DownloadAll 和 DownloadFile 的异步关系
- 机械地认为"创建者清理"可以在当前方法内完成

### Gate Pack 的价值

Gate Pack 通过深入分析代码上下文，发现了：
- **跨层 ownership** 问题
- **异步 ownership** 问题
- 原 Stop 理由是**正确的**

这证明了：
> **旧结论可以被新证据推翻；新结论仍必须经过独立门控。**

即使 Decision Pack 认为可以 Go，Gate Pack 仍然可以基于更深入的证据分析，给出 Stop 的结论。

---

> **本包结论**：F-L3 Gate Decision = **STOP**，原 Stop 理由正确，Decision Pack 分析有误。建议继续冻结 F-L3，不在当前重构范围内处理。
