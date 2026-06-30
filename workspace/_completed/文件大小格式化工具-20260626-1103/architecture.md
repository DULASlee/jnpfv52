# 架构方案 — FileSizeFormatter

## 需求摘要
- 来源：测试任务——文件大小格式化工具
- 业务价值：将字节数转为人类可读格式（B/KB/MB/GB）
- 技术约束：无数据库操作（`ITenantFilter` 无关）、非 API（无需权限声明）

## 方案 A — 静态工具类（选定）

- 描述：`public static class FileSizeFormatter` 含单一方法 `Format(long bytes)`
- 优点：零依赖、纯数学运算、符合项目现有模式
- 缺点：未来本地化需重构
- **失效边界**：若需求扩展到 TB/PB 或本地化，需引入格式化接口
- 红线检查：R1(N/A) R2(N/A) R3(N/A) R4(N/A) R5(✅ system 模块合法) R6(N/A) R7(N/A) R8(N/A)

## 方案 B — long 扩展方法（未选中）
- 优点：调用简练（`123456L.ToFileSize()`）
- 缺点：污染 `long` 全局命名空间，不符合项目现有惯例
- **失效边界**：多模块定义同名扩展→歧义冲突

## 方案 C — Humanizer NuGet（未选中）
- 优点：一行代码，久经测试
- 缺点：为单一方法引入外部依赖
- **失效边界**：版本升级可能引入破坏性变更

## 影响评估
- 变更类型：新增工具类
- 涉及文件：1 个新增 .cs + 1 个测试 .cs
- 命名空间：`JNPF.Systems.Common`
- 目标路径：`backend/modularity/system/JNPF.Systems/Common/FileSizeFormatter.cs`
- 测试路径：`backend/tests/JNPF.Tests.Systems/FileSizeFormatterTests.cs`
