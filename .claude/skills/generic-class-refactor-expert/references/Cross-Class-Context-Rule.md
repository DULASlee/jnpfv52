# 跨类分析上下文规则（D11 — v6.0）

> generic-class-refactor-expert v6.0 reference.
> 排查 D11（Cross-Class Lifecycle）时使用本规则。

## 什么是跨类分析

v5.0 的 D1~D10 维度都在**单类内部**排查。D11 看的是**类与类之间的边界**。

典型场景：
- 类 A 调用类 B 的方法，B 返回了一个需要释放的资源，谁负责释放？
- 类 A 注入了类 B，B 的 DI 生命周期和 A 是否匹配？
- 类 B 返回了 1 万条数据，类 A 拿到后有没有限制？

这些问题**只看一个类的代码无法回答**，必须知道类之间的关系。

## 跨类分析需要的上下文

排查 D11 时，需要以下信息（称为"跨类上下文"）：

### 1. 调用关系
```
当前类的每个 public/internal 方法：
  - 调用了哪些其他类的方法？
  - 被哪些其他类调用？

当前类的每个 private 方法：
  - 如果它间接调用了其他类的方法，也需要列出
```

### 2. 依赖注入关系
```
当前类构造函数注入了哪些接口/服务？
  - 接口名称
  - 注册的生命周期（Singleton/Scoped/Transient）
  - 实际实现类（如果可知）
```

### 3. 数据传递关系
```
当前类的方法返回值：
  - 是否返回 IDisposable / Stream / byte[]？
  - 返回的集合类型是否有数量上限？

当前类的方法参数：
  - 是否接收 IDisposable / Stream？
  - 接收后是否接管了释放责任？
```

## 跨类上下文的来源

### Level 0（手动提供）
人工直接告诉 Skills：
"DownloadAll 调用了 DownloadFile，DownloadFile 返回 FileStreamResult"
→ 适合验证阶段，成本最高但最灵活

### Level 1（文本摘要）
一个 .md 文件，列出每个类的：
- public 方法签名列表
- 注入的依赖接口列表
- 已知的调用方/被调用方
→ 可手写或脚本生成

### Level 2（自动化分析产物）
Roslyn 编译后导出的 call-graph.json / di-registration.json
→ 第二期工具开发

## D11 检查清单

### 11.1 Stream/IDisposable 跨类传递

```
触发条件：当前类的方法调用了其他类的方法，且被调用方法返回了
          IDisposable / Stream / FileStreamResult / HttpResponseMessage 等

检查点：
  Q1: 被调用方法返回的资源，当前类是否用 using 管理？
  Q2: 如果当前类又把这个资源传给了第三方（如返回给 Controller），
      ownership 是否明确交接？
  Q3: 如果被调用方法内部已经 using，但返回了包装对象，
      调用方再次 using 是否会双重释放？

判定：
  ✅ PASS — ownership 链清晰，每层正确管理
  ❌ Finding — ownership 不明、泄漏、双重释放
  ⚠️ NEED_EVIDENCE — 链路过长，无法仅从代码确认
```

### 11.2 DI 生命周期跨层

```
触发条件：当前类注入了接口/服务

检查点：
  Q1: 当前类自身的 DI 注册生命周期是什么？
  Q2: 注入的依赖的生命周期是什么？
  Q3: 是否存在长生命周期持有短生命周期？
      Singleton → Scoped ❌
      Singleton → Transient ⚠️
      Scoped → Scoped ✅
      Scoped → Singleton ✅

判定：
  ✅ PASS — 生命周期对齐
  ❌ Finding — 生命周期违反（Singleton 持有 Scoped）
  ⚠️ STOP — 框架内部自动管理（如 SqlSugar Singleton 内部自动创建 Scope）
```

### 11.3 数据量跨类传递

```
触发条件：当前类调用了其他类的方法，返回值是集合类型

检查点：
  Q1: 被调用方法返回的数据是否有行数限制？
  Q2: 当前类拿到数据后，是否做了分页/限制？
  Q3: 如果被调用方法返回全量数据，当前类是否遍历全部？

判定：
  ✅ PASS — 有限制或数据量已知可控
  ❌ Finding — 无限制传递，可能膨胀
  ⚠️ NEED_EVIDENCE — 需要了解被调用方法的数据规模
```

## 输出字段

D11 维度的 Finding 必须附加：

| 字段 | 类型 | 说明 |
|---|---|---|
| `CrossClassChain` | string | 跨类调用链描述，如 `FileService.DownloadAll → DownloadFile → FileStreamResult` |
| `OwnershipOwner` | string | 谁负责释放/管理，如 `调用方（Controller/前端）` |
| `DILifecycleChain` | string | DI 生命周期链，如 `Singleton(FileService) → Scoped(IFileManager)` |
| `Resolution` | PASS / Finding / NEED_EVIDENCE / STOP | 判定结果 |

## 版本记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v6.0-alpha | 2026-08-28 | 初始版本，3 检查项 + Level 0~2 上下文来源 |
