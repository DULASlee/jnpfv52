---
name: test-driven-development
description: Write failing tests first, then implement code to make them pass, then refactor. Use when implementing new features, fixing bugs, or refactoring critical code.
scope: JNPF-v52
---

# Test-Driven Development — 测试驱动开发

红 → 绿 → 重构。先写测试，再写实现。

## 适用场景

- 新功能开发
- Bug 修复（先写复现测试）
- 核心逻辑重构
- API 接口实现

## 工作流

### 1. 红（Red）— 写失败测试

```csharp
// 示例：xUnit 测试
[Fact]
public void Login_WithValidCredentials_ReturnsToken()
{
    // Arrange
    var input = new LoginInput { account = "admin", password = "..." };
    
    // Act
    var result = await _service.Login(input);
    
    // Assert
    Assert.Equal(200, result.code);
    Assert.NotNull(result.data.token);
}
```

运行测试确认失败：
```bash
dotnet test
```

### 2. 绿（Green）— 最小实现

写刚好能让测试通过的代码。不要多写。

```bash
dotnet test  # 确认通过
```

### 3. 重构（Refactor）

测试通过后，优化代码结构：
- 消除重复
- 改善命名
- 提取方法

```bash
dotnet test  # 重构后再次确认通过
```

## 本项目的测试框架

| 项目 | 测试框架 | 运行命令 |
|------|----------|----------|
| C# 后端 | xUnit（`JNPF.Xunit`） | `dotnet test` |
| 前端 | Vitest | `pnpm test` |

## 什么时候可以不写测试

- 纯配置变更（JSON/XML）
- 前端 CSS 调整
- 文档编写

其他情况**原则上**都要有测试。

## 铁律

- ❌ 禁止先写实现后补测试
- ✅ 测试必须能独立运行
- ✅ 一个测试只验证一个行为
