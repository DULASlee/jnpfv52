# JNPF v5.2 后端性能基线（阶段 0 正式产出）

> **状态**：基线已冻结（2026-08-19）· 本文档为阶段 0 唯一权威产出（施工包 v2.0 任务 0-7）
> **数据文件**：`backend/tools/JNPF.Startup.Benchmarks/baseline.json`（v2，机器可读）
> **测量工具**：`backend/tools/JNPF.Startup.Benchmarks`（`--mode process` 进程级 / `--mode inproc` 组合级）
> **PR 门控**：`scripts/bench-startup-gate.ps1`（DI 描述符增幅 >10% 阻断）
> 前身 `docs/benchmark-baseline.md` 已指针化归档至本文档。

---

## 1. 决策门结论（任务 2.5）

**冷启动中位数 2719ms ≤ 3s → 命中「关闭该议题，仅保留监控」分支。**

「启动优化」不立项为独立战役，降级为持续监控项（PR gate + 发布前冷启动抽测）。

## 2. 测量环境

| 项 | 值 |
|----|----|
| CPU | 12th Gen Intel Core i7-12700H |
| 内存 | 16 GB |
| OS | Windows 11 专业版（开发机） |
| .NET SDK | 10.0.301（`global.json` 钉扎 8.0.x，目标框架 net8.0） |
| 构建配置 | Debug（基线口径；Release 预计更优） |
| 运行环境 | `ASPNETCORE_ENVIRONMENT=Production`（基线采集时 Development 下 Scope 校验暴露存量 DI 违规致崩溃；**2026-08-20 战役 0 已修复至 0 违规并全绿启动**，诊断开关 `JNPF_VALIDATE_DI=1` 可用，见 `runservice-refactor-di-constraints.md`） |

## 3. 进程级指标（`--mode process`，5 轮取中位数，真实 HTTP 请求）

| 指标 | 中位数 | 说明 |
|------|-------:|------|
| **冷启动**（进程拉起 → `/health/live` 200） | **2719 ms** | 含 JIT + 配置 + DI + 路由生成 |
| 首请求延迟（健康检查） | 571 ms | 含管道 JIT 预热 |
| 动态 API 首请求 | 80 ms | 动态路由懒构建成本 |
| Swagger 首次生成 | 7 ms | 反射税无证据 |
| 工作集内存 | 502 MB | RSS |

## 4. 组合级指标（`--mode inproc`，任务 0-3/0-5）

| 指标 | 值 | 说明 |
|------|-----:|------|
| 程序集数 / 有效类型数 | 49 / 1645 | `App.GetAssemblies()` DependencyContext 扫描 |
| App 静态初始化 | 45 ms | 含程序集扫描 |
| AddApp（全量反射注册） | 159 ms | `AppServiceCollectionExtensions.AddApp` |
| **14 个 JnpfModule 注册总计** | **151 ms** | 逐模块计时见下表 |
| AddInject（动态 API/Swagger/校验） | <10 ms | |
| **DI 描述符总数** | **956** | Transient 496 / Singleton 425 / Scoped 35 |

**已知偏差**：`DatabaseModule` 在 inproc 下因缺 `IWebHostEnvironment` 注册失败（缺陷 F2），
真实描述符数约 +30~50；门控以趋势为准。

### 4.1 逐模块加载耗时（任务 0-5，阶段 2 安全网）

| 模块 | 耗时 | 模块 | 耗时 |
|------|-----:|------|-----:|
| ObservabilityModule | 45 ms | ValidationModule | 5 ms |
| PipelineSchedulingModule | 42 ms | LegacyModule / DiffLogPublishModule / EventBusModule | 0 ms |
| AuthenticationModule | 17 ms | ForwardedHeadersModule / RateLimitingModule / SandboxModule | 0 ms |
| JsonSettingsModule | 14 ms | DatabaseModule | FAIL（见 F2） |
| HealthCheckModule | 10 ms | **总计** | **151 ms** |
| WeixinModule | 7 ms | | |

## 5. 判定表（原方案假设 vs 实测）

| 原方案假设 | 实测 | 判定 |
|-----------|------|------|
| 冷启动 15-30s，需启动优化战役 | 2719 ms | ❌ 伪需求，降级监控 |
| DI 注册数千、失控 | 956 | ⚠️ 规模正常，守护增长 |
| 反射税致运行时缓慢 | 动态 API 80ms / Swagger 7ms | ❌ 无证据 |
| 模块加载是启动大头 | 151ms / 2719ms ≈ 5.5% | ❌ 非瓶颈 |

## 6. 复测方法与门控

```powershell
# PR 快速门控（inproc，~30s）：DI 描述符增幅 >10% 即失败
powershell -File scripts\bench-startup-gate.ps1

# 全量进程级测量（发布前，~3min）：5 轮冷启动，中位数 >3500ms 告警
cd backend\tools\JNPF.Startup.Benchmarks
dotnet run -- --mode process --rounds 5 --environment Production
```

**更新基线的纪律**：结构性变更（模块增删/框架改造）后必须重跑并更新 `baseline.json` + 本文档，
且须在 CR/PR 说明中陈述理由（施工包 §十 度量节奏）。
