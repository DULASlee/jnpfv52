# 测试任务：文件大小格式化工具

## 需求
在 `backend/modularity/system/JNPF.System/Utils/` 下新增 `FileSizeFormatter.cs`，提供一个静态方法：

```csharp
public static string Format(long bytes)
```

将字节数转为人类可读格式：
- 0 → "0 B"
- 1023 → "1023 B"
- 1024 → "1.00 KB"
- 1536 → "1.50 KB"
- 1048576 → "1.00 MB"
- 1073741824 → "1.00 GB"

## 约束
- 必须通过 `dotnet build` 编译
- 必须包含至少一个单元测试
- 必须继承 `ITenantFilter` 无关（纯工具类，无数据库操作）
- 不需要权限声明（非 API）

## 测试流水线
此任务用于验证：
1. 7 角色自动流转（Architect → Planner → Coder → Tester → Reviewer → Reporter）
2. 每个角色自动调用对应 SP 技能
3. 论断标签强制生效
4. 错题本加载生效
5. Debugger 中断机制（如遇编译错误）
6. 完成后自动归档 workspace → _completed/

## 启动
将此文件放入 workspace/requirements.md 后，发送任意消息即可启动。
