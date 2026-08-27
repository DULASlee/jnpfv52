# ScheduleService 初步 Gate — F-P2 / F-E2

> **状态**：只读分析，不改生产代码  
> **目标类**：`JNPF.Systems.System.ScheduleService`  
> **文件**：`backend/modularity/system/JNPF.Systems/System/ScheduleService.cs`

---

## F-P2：大结果集（GetList 无分页）

### 代码位置

1. **GetList**（91-127 行）
2. **GetAppList**（134-182 行）

### 初步证据

#### GetList（91-127 行）

```csharp
[HttpGet("")]
public async Task<dynamic> GetList([FromQuery] ScheduleListInput input)
{
    var list = await _repository.AsSugarClient().Queryable<ScheduleEntity, ScheduleUserEntity>((s, su) => new JoinQueryInfos(JoinType.Left, s.Id == su.ScheduleId))
        .WhereIF(!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty(), s => (s.StartDay >= input.startTime && s.StartDay <= input.endTime) || SqlFunc.Between(input.startTime, s.StartDay, s.EndDay))
        .Where(s => s.DeleteMark == null)
        .Where((s, su) => su.DeleteMark == null && su.EnabledMark == 1 && su.ToUserId.Equals(_userManager.UserId))
        .OrderBy(s => s.AllDay, OrderByType.Desc)
        .OrderBy(s => s.StartDay)
        .OrderBy(s => s.EndDay)
        .OrderBy(s => s.CreatorTime, OrderByType.Desc)
        .Select(s => new ScheduleListOutput { ... })
        .ToListAsync();  // ← 无分页，返回所有匹配结果

    return new { list = list };
}
```

**问题**：
- 使用 `ToListAsync()` 无分页
- 返回所有匹配的日程数据
- 如果用户有大量日程（如几年积累），可能返回大量数据

**N 的规模**：
- 取决于用户的日程数量
- 理论上可以很大（几年积累的日程）
- 但实际场景中，用户通常只查询特定时间范围的日程（`startTime` 和 `endTime` 过滤）

**是否存在缓存**：❌ 无缓存

**修复方案**：
- 引入分页参数（`currentPage`, `pageSize`）
- 使用 `ToPagedListAsync()` 替代 `ToListAsync()`

**收益**：
- 减少内存占用
- 减少网络传输
- 改善前端渲染性能

**风险**：
- 改变 API 契约（需要前端配合修改）
- 可能影响现有功能

### 初步 Gate Decision

**Decision**：**NEED EVIDENCE**

**理由**：
1. 虽然存在无分页查询，但实际场景中用户通常查询特定时间范围
2. 需要证据证明：
   - 实际返回数据量有多大？
   - 是否真的造成性能问题？
   - 前端是否有分页需求？
3. 修改会改变 API 契约，需要跨层协调

**下一步**：
- 收集运行时证据（实际返回数据量、响应时间）
- 确认前端是否有分页需求
- 如果有证据支持，再进入 Fix 设计

---

## F-E2：异常信息泄露

### 代码位置

全局扫描异常处理模式

### 初步证据

扫描所有 `throw` 语句：

```
232: throw Oops.Oh(ErrorCode.D1918);
389: throw Oops.Oh(ErrorCode.D1908);
392: throw Oops.Oh(ErrorCode.D1909);
704: throw Oops.Oh(ErrorCode.D1910);
711: throw Oops.Oh(ErrorCode.D1910);
716: throw Oops.Oh(ErrorCode.D1911);
881: throw Oops.Oh(ErrorCode.D1912);
896: throw Oops.Oh(ErrorCode.D1914);
```

**分析**：
- 所有异常都使用 `Oops.Oh(ErrorCode.XXXX)` 模式
- 这是 JNPF 框架的标准异常处理方式
- 不会泄露原始异常信息（`ex.Message`、`ex.StackTrace`）
- 错误码由框架统一处理，返回给前端的是友好错误信息

**是否存在异常吞没**：❌ 未发现 `catch (Exception) { }` 或 `catch (Exception ex) { throw; }` 模式

**是否存在异常信息泄露**：❌ 未发现 `throw ex` 或 `throw new Exception(ex.Message)` 模式

### 初步 Gate Decision

**Decision**：**STOP** — 无问题

**理由**：
1. 所有异常都使用标准的 `Oops.Oh(ErrorCode)` 模式
2. 不会泄露原始异常信息
3. 符合 JNPF 框架的异常处理规范
4. 不存在异常吞没或信息泄露

---

## 决策矩阵（更新）

| Finding | 技术性质 | 证据强度 | 改造半径 | 当前决定 |
|---------|----------|----------|----------|----------|
| F-L1 | Lifecycle | ✅ 已证明无问题 | — | **STOP**（无问题） |
| F-P1 | Performance | ✅ 已证明存在 N+1 | 单类单点 | **GO**（满足准入） |
| F-A1 | Architecture | ⚠️ 部分成立 | 跨类 | **STOP**（收益有限） |
| F-P2 | Performance | ⚠️ 初步证据 | 跨层 | **NEED EVIDENCE**（需运行时证据） |
| F-E2 | Security/Exception | ✅ 已证明无问题 | — | **STOP**（无问题） |

---

## 最终推荐

### 唯一值得进入 Fix 的 Finding：**F-P1（N+1 查询）**

**理由**：
1. 证据充分（3 处明确的 N+1 查询）
2. 改造半径为单类单点（Delete 方法）
3. 修复收益明确（N 次查询降为 1 次）
4. 验证成本低（build + 删除操作回归）
5. 不改变 API 契约

### 其他 Finding 的状态

- **F-L1**：无问题，关闭
- **F-A1**：收益有限，关闭
- **F-P2**：需要运行时证据，暂时冻结
- **F-E2**：无问题，关闭

---

> **本包证明**：ScheduleService 初步 Gate 完成，F-P2 需要运行时证据（NEED EVIDENCE），F-E2 无问题（STOP）。唯一值得进入 Fix 的是 F-P1（N+1 查询）。
