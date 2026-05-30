---
name: verification-before-completion
description: Run verification checks before claiming work is complete. Use at the end of any implementation task to ensure build passes, tests pass, and manual verification is documented.
scope: JNPF-v52
tech-stack: [dotnet, pnpm]
---

# Verification Before Completion — 完成前验证

在声称"做完了"之前，必须执行以下验证清单。

## 适用场景

- 任何代码改动完成后
- 施工包阶段完成后
- 系统启动/配置变更后
- 提交代码前

## 验证清单

### 1. 构建验证

```bash
# C# 项目
dotnet build

# 前端项目
pnpm build
```

构建必须零错误通过。Warning 需评估是否需要修复。

### 2. 功能验证

根据改动类型选择验证方式：

| 改动类型 | 验证方式 |
|----------|----------|
| API 接口 | curl / Invoke-WebRequest 测试端点 |
| 前端页面 | 浏览器打开页面操作 |
| 数据库 | sqlcmd 查询确认 |
| 配置变更 | 读取配置文件 + 服务重启验证 |

### 3. 回归验证

- 确认改动没有破坏已有功能
- 至少验证一个相关的已有功能仍正常工作

### 4. 日志检查

启动服务后检查日志，确认没有异常报错：

```powershell
# 方式 A：在仓库根目录下使用相对路径（推荐）
Get-Content "backend\application\JNPF.API.Entry\logs\*.log" -Tail 20

# 方式 B：跨目录或 CI 环境，先设项目根（替换为实际路径）
# $env:JNPF_ROOT = (git rev-parse --show-toplevel)  # 或在 shell profile 中持久化
Get-Content "$env:JNPF_ROOT\backend\application\JNPF.API.Entry\logs\*.log" -Tail 20
```

### 5. 输出验证报告

```
## 验证报告

### 构建
- [ ] dotnet build: [PASS/FAIL]
- [ ] pnpm build: [PASS/FAIL]

### 功能
- [ ] [功能点1]: [PASS/FAIL] — [验证方法]
- [ ] [功能点2]: [PASS/FAIL] — [验证方法]

### 回归
- [ ] [已有功能]: [PASS/FAIL]

### 日志
- [ ] 无异常报错: [YES/NO]

### 结论
[PASS] 可以交付 / [FAIL] 需要修复：[具体问题]
```

## 铁律

- ❌ 构建不通过绝不说"完成了"
- ❌ 功能没实际跑过绝不说"验证通过"
- ✅ 验证报告必须写具体，不能只说"OK"
