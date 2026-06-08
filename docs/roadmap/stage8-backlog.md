# 阶段 8：长期演进任务看板

> 阶段 7 编码任务已全部完成。阶段 8 聚焦存量代码迁移和持续改进。
> 最后更新：2026-06-08

---

## P0（已完成 — 阶段 7 收尾）

| # | 任务 | 结果 |
|---|---|---|
| F | ImageSharp 安全升级 | 3.0.2 → 3.1.11, 0 vulnerabilities |
| G | async void 存量修复 | 3 处均为 IJobPersistence 接口实现（JNPF006 豁免），无需修改 |

---

## P1（下个 Sprint 必做）

| # | 任务 | 存量 | 目标 | 完成标准 |
|---|---|---|---|---|
| 1 | ImageSharp 持续监控 | 0 vuln | 保持零漏洞 | `dotnet list package --vulnerable` 无 ImageSharp |

---

## P1（每 Sprint 推进）

| # | 任务 | 存量 | 每 Sprint 目标 | 预计完成 |
|---|---|---|---|---|
| 2 | App.GetService 削弱 | ~37 处 | 迁移 1 个模块 (~5-8 处) | 7 Sprint |
| 3 | CreateScope 削弱 | ~24 处 | 迁移 1 个模块 (~4-6 处) | 5 Sprint |

### App.GetService 迁移策略

```
每 Sprint:
  1. 选定 1 个模块（如 JNPF.Message）
  2. grep "App.GetService" 找到所有调用点
  3. 逐个替换为构造函数注入
  4. JNPF001 分析器确认该模块零违规
  5. 提交 + 验证
```

### CreateScope 迁移策略

```
每 Sprint:
  1. 选定 1 个模块
  2. grep "CreateScope" 找到所有调用点
  3. 改为构造函数注入 Scoped 服务
  4. JNPF003 分析器确认该模块零违规
  5. 提交 + 验证
```

---

## P2（持续推进）

| # | 任务 | 策略 |
|---|---|---|
| 4 | 旧 AppStartup → JnpfModule | 新模块必须用 JnpfModule；老模块逐步绞杀，最终移除 LegacyModule 桥接 |
| 5 | CancellationToken 覆盖率 | 逐模块审查，所有 async 方法接受 CancellationToken |
| 6 | 测试基线建设 | 框架层目标 80% 覆盖率，业务核心模块 60% |
| 7 | 分析器严重级别提升 | JNPF001-JNPF006 suggestion → warning (存量清零后 → error) |

---

## P3（观察后执行）

| # | 任务 | 触发条件 | 说明 |
|---|---|---|---|
| 8 | MiniProfiler 移除 | OpenTelemetry 稳定运行 2 Sprint | 避免两套可观测工具并存 |
| 9 | `#pragma warning disable` 移除 | 对应存量代码已迁移 | 每条 pragma 对应一个已知问题，修完即移除 |
| 10 | `[SuppressSniffer]` 审计 | 分析器成熟后 | 检查是否隐藏了真正的代码问题 |

---

## 进度跟踪

| Sprint | 日期 | 完成任务 | 备注 |
|---|---|---|---|
| Sprint 1 | TBD | — | — |
| Sprint 2 | TBD | — | — |
| Sprint 3 | TBD | — | — |
| Sprint 4 | TBD | — | — |
| Sprint 5 | TBD | — | — |
| Sprint 6 | TBD | — | — |
| Sprint 7 | TBD | — | — |
| Sprint 8 | TBD | — | — |

---

## 阶段 7 完成基准

| 指标 | 值 |
|---|---|
| 架构文档 | 7 份新建（overview, tenant-context, outbox-pipeline, dev guide, deploy guide, ADR×16, stage8） |
| 代码分析器 | 6 规则 + 2 CodeFix + 11 测试 |
| 验证 | FluentValidation 5 核心验证器 |
| 可观测性 | OpenTelemetry Tracing + Metrics → Jaeger |
| 数据库迁移 | DbUp 2 幂等脚本 |
| CI/CD | 3 流水线 + 分析器门禁 + 安全扫描 |
| 安全升级 | ImageSharp 3.0.2 → 3.1.11 (0 vuln) |
