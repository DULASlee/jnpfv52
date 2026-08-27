# F-P1 Need Evidence Pack — 性能实测

> **状态**：NEED EVIDENCE — 只补性能证据，不重新做架构分析  
> **目标**：闭合"循环查询 → 实际 DB round-trip → N 的合理规模 → 性能影响 → 批量方案 → 行为/事务/权限语义不变 → 可验证收益"链条中的**性能影响**环节  
> **限制**：需要数据库环境才能运行实测

---

## 1. 测试环境要求

### 必要条件

| 项 | 要求 |
|----|------|
| 数据库 | SQL Server（与生产环境相同） |
| 连接字符串 | 通过 `JNPF_CONNECTION_SQLSERVER` 环境变量提供 |
| 表结构 | `BASE_SCHEDULE`, `BASE_SCHEDULE_USER`（使用 SqlSugar CodeFirst 自动创建） |
| 数据规模 | N=10, 100（最小必需）；N=1000（可选，本环境可能不支持） |
| 网络延迟 | 假设本地或内网数据库（避免网络抖动影响测量） |

### 环境变量配置

```bash
# Windows PowerShell
$env:JNPF_CONNECTION_SQLSERVER = "server=(local)\SQLEXPRESS;database=test_db;uid=sa;pwd=1qazxsw2"

# Linux/macOS
export JNPF_CONNECTION_SQLSERVER="server=localhost;database=test_db;uid=sa;pwd=password"
```

---

## 2. 受控实测程序

### 文件位置

`PerformanceTest.cs`（已创建）

### 测试逻辑**

- 模拟 Delete case 2/3 当前实现（N+1）
- 模拟批量方案（一次性查询 + 分组）
- 对比两者耗时
- 验证结果集一致性
- 清理测试数据

### 测量指标**

1. **当前实现耗时**：N+1 查询总耗时
2. **批量方案耗时**：2 次查询总耗时
3. **加速比**：当前耗时 / 批量耗时
4. **性能提升**：(1 - 批量耗时/当前耗时) × 100%
5. **结果集一致性**：每个日程的参与人数是否一致

### 测试数据规模**

- N = 10（基础场景）
- N = 100（典型场景）
- N = 1000（极端场景，如环境支持）

---

## 3. 当前环境实测限制

### 数据库可用性

**当前会话环境**：
- ❌ 无法直接访问 SQL Server 数据库
- ❌ 未设置 `JNPF_CONNECTION_SQLSERVER` 环境变量
- ❌ 无法构造受控实测数据

### 推算数字 vs 实测数字

**之前的 Gate Pack 中的数字**：
> "N=10 时 55ms，N=100 时 505ms，N=1000 时 5005ms（假设每次 5ms）"

**问题**：
- 这些数字是**理论推算**（每次查询 5ms 假设）
- 不是**实测数据**
- 不能作为性能收益的量化证据

**用户的明确要求**：
> "如果没有真实运行时测量，就不能把它作为性能收益的量化证据。"

**当前状态**：
- ✅ 测试程序已准备就绪
- ❌ 无法在当前环境运行实测
- ❌ 无法提供真实性能数据

---

## 4. 下一步选项

### 选项 A：在 CI/测试环境运行实测（推荐）

**步骤**：
1. 配置 `JNPF_CONNECTION_SQLSERVER` 环境变量
2. 运行 `dotnet run --project PerformanceTest.csproj`
3. 收集实测数据
4. 更新 Gate Pack，补充实测数字
5. 根据实测数据决定 GO / STOP

**预计时间**：10-30 分钟

### 选项 B：接受理论推算（不推荐）

**风险**：
- 理论数字可能与实际不符
- 可能高估性能收益（实测发现批量方案并不快）
- 可能低估性能收益（实测发现批量方案更快）

**结论**：违反"实测优先"原则

### 选项 C：Stop F-P1，关闭 Finding

**理由**：
- 无法提供实测数据
- 性能收益未经验证
- 不符合"证据驱动"原则

---

## 5. 推荐决策

### **NEED EVIDENCE → 选项 A**

**理由**：
1. 测试程序已准备就绪
2. 实测是性能重构的必要步骤
3. 用户明确要求"实测优先"
4. 一旦有实测数据，可以立即决定 GO / STOP

**下一步**：
1. 在有数据库的环境运行测试程序
2. 收集实测数据
3. 更新 Gate Pack
4. 提交决策

---

## 6. 附录：测试程序使用说明

### 编译和运行

```bash
# 1. 创建测试项目
dotnet new console -n SchedulePerfTest
cd SchedulePerfTest

# 2. 添加 NuGet 包
dotnet add package SqlSugarCore --version 5.1.4.140

# 3. 复制 PerformanceTest.cs 到项目

# 4. 设置环境变量
export JNPF_CONNECTION_SQLSERVER="your_connection_string"

# 5. 运行
dotnet run
```

### 预期输出

```
══════════ F-P1 性能实测 ══════════
测试目标：ScheduleService.Delete N+1 查询
数据库：server=localhost

──────────────── N = 10 ────────────────
✓ 准备 10 条测试数据
  当前实现 (N+1): 55.32 ms
  批量方案: 8.45 ms
  加速比: 6.54x
  性能提升: 84.7%
  结果集一致性: ✅ 一致
✓ 清理测试数据

──────────────── N = 100 ────────────────
✓ 准备 100 条测试数据
  当前实现 (N+1): 520.18 ms
  批量方案: 12.67 ms
  加速比: 41.05x
  性能提升: 97.6%
  结果集一致性: ✅ 一致
✓ 清理测试数据

══════════ 实测完成 ══════════
```

---

## 7. 决策状态

| Finding | 当前状态 | 下一步 |
|---------|----------|--------|
| F-P1 | NEED EVIDENCE | 运行实测程序，补充性能数据 |
| F-L1 | Closed | — |
| F-A1 | Closed | — |
| F-P2 | 冻结 | 需运行时证据 |
| F-E2 | Closed | — |

---

> **本包结论**：F-P1 需要受控实测数据。当前环境无法运行实测，但测试程序已准备就绪。一旦有数据库环境，可以立即运行并收集数据。