# Skill 学习点 — 资源生命周期 Ownership 判定规则

> **来源**：F-L3 Gate Pack 分析  
> **状态**：已沉淀为 Skill 专家规则  
> **适用范围**：所有资源生命周期问题（FileStream、临时文件、数据库连接、网络资源等）

---

## 核心规则

**生命周期问题不能仅依据"创建者是谁"判断 ownership。必须追踪资源跨方法、跨异步边界的实际消费生命周期。**

---

## 判定流程

```
Create（谁创建？）
  ↓
Who consumes?（谁消费？）
  ↓
When does consumption end?（消费何时结束？）
  ↓
Who owns lifetime?（谁拥有生命周期？）
  ↓
Who can safely dispose?（谁能安全释放？）
```

**而不是简单**：

```
谁 new
↓
谁 finally
```

---

## 三种典型模式

### 模式 1：同步局部 ownership（Golden #2/#3）

```
当前方法创建 → 当前方法使用 → 当前方法结束
```

**示例**：
- `UploadFileByType`：`new FileStream` → `_fileManager.UploadFileByType(file)` → 方法结束
- `FileDown`：`_fileManager.DownloadFileByType` → `fs.Read` → 方法结束

**特征**：
- 创建和使用在同一方法内
- 使用完成后立即可以释放
- 可以用 `using var` 局部确定性释放

**修复方案**：`using var` 或 `try/finally`

---

### 模式 2：异步跨方法 ownership（F-L3）

```
当前方法创建 → 后续异步方法使用 → 用户行为决定结束时间
```

**示例**：
- `DownloadAll`：创建临时目录/zip → 返回下载链接 → 用户几分钟后下载 → `DownloadFile` 消费

**特征**：
- 创建和使用在不同方法
- 使用完成时间由外部行为决定（用户何时下载）
- 不能在当前创建方法中释放

**修复方案**：
- ❌ 不能在创建方法内 `try/finally`（会破坏消费方）
- ✅ 需要全局清理机制（定时任务/后台服务）
- ✅ 或引入资源生命周期管理器

**当前类级重构边界内不能安全修复** → **STOP**

---

### 模式 3：跨层 ownership（未遇到）

```
当前方法创建 → 跨层传递给其他组件 → 其他组件负责释放
```

**示例**：
- 创建资源后传递给第三方库
- 第三方库负责释放

**特征**：
- 创建方不拥有完整生命周期
- 释放责任在接收方

**修复方案**：
- 明确文档化 ownership 转移
- 接收方必须有释放机制

---

## 判定检查清单

在判断资源生命周期问题时，必须回答：

| 问题 | 答案 | 影响 |
|------|------|------|
| 谁创建资源？ | | 确定起点 |
| 谁消费资源？ | | 确定使用方 |
| 消费何时结束？ | 同步/异步/外部行为 | 确定生命周期边界 |
| 谁拥有完整生命周期？ | 创建方/消费方/第三方 | 确定 ownership |
| 能否在当前方法内安全释放？ | 是/否 | 决定修复方案 |
| 释放是否会破坏消费方？ | 是/否 | 决定是否可以局部修复 |

**如果"释放会破坏消费方" = 是** → **STOP，需要跨层策略**

---

## 实际应用

### Golden #2（UploadFileByType）

```csharp
// 创建：new FileStream
// 消费：_fileManager.UploadFileByType(file)
// 结束：方法返回
// ownership：当前方法
// 可以安全释放：✅ 是
// 修复：using var
```

### Golden #3（FileDown）

```csharp
// 创建：_fileManager.DownloadFileByType
// 消费：fs.Read + Response.Body.WriteAsync
// 结束：方法返回
// ownership：当前方法
// 可以安全释放：✅ 是
// 修复：using var
```

### F-L3（DownloadAll）

```csharp
// 创建：Directory.CreateDirectory + ZipFile.CreateFromDirectory
// 消费：DownloadFile（异步，用户行为决定时间）
// 结束：用户下载完成后（外部行为）
// ownership：跨方法、跨异步边界
// 可以安全释放：❌ 否（会破坏 DownloadFile）
// 修复：需要全局清理机制 → STOP
```

---

## 总结

**专家级资源生命周期重构的核心能力**：

> 不是机械寻找 `new FileStream` 并加 `using`，而是进行**生命周期建模**，追踪资源从创建到消费的完整路径，判断 ownership 边界，决定修复方案。

**三个样本共同证明**：
- Golden #2/#3：同步局部 ownership → 可以局部确定性释放
- F-L3：异步跨方法 ownership → 不能在当前创建方法中释放 → STOP

**这比连续修三个 `using var` 更能证明 Skill 是在做生命周期建模，而不是机械寻找资源泄漏。**

---

## 引用

- Golden #2：`Golden-Example-02.md`
- Golden #3：`Golden-Example-03.md`
- F-L3 Gate Pack：`../../evidence/class-refactor-expert-v40/f-l3-gate/Gate-Pack.md`
