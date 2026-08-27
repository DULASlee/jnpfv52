# Complexity Budget Scale（v4.0 calibrated M1）

> **从“能用多高级就用多高级”改为“逐级升级，收益>成本才升”**
> **Fix Budget = Semantic Scope + Physical Diff + Dependency Expansion**（非纯行数）。编译必需的 `using`/import 若不引入新依赖，属语义中性，但需显式获批（OrderService `CS0246` 案例）。

## 扩展性（P8）

| 方案 | 新增行数 | 新增生命周期/池化点 | 维护成本 | 适用 |
|------|----------|-------------------|----------|------|
| 简单 if/switch | +0 | 0 | 低 | 2 分支时 go |
| 策略表 / Dictionary 映射 | +10 | 0 | 低 | 3–5 分支时 go |
| Strategy + DI | +80 | +3 | 中 | >5 分支且需扩展时 go |
| Plugin + Assembly Scan | +200 | +5 | 高 | 仅开放平台时 go |

> **禁令**：2 分支上 Strategy/Factory/Scan = 过度架构，直接打回

## 性能（P6）

| 方案 | 复杂度 | 收益阈值 | 适用 |
|------|--------|----------|------|
| 缓存局部变量 / 消除 ToList() | 低 | 任意热点 | 直接 go |
| StringBuilder / 批量加载 | 低 | 分配热点 | 直接 go |
| Span/ArrayPool | 中 | BDN 证明分配显著下降 | 证据后 go |
| ObjectPool / 池化 | 高 | 长期压测内存持平 | 证据+所有权清晰才 go |
| SourceGenerator / Expression | 高 | 反射热点 + BDN 证明 | 证据后 go |

## 类型语义（P7）

| 类型 | 语义 | 推荐 |
|------|------|------|
| Entity（ORM 跟踪/Identity/Lifecycle） | 可变、身份 | class |
| DTO / Value Object | 不可变、值语义 | record / readonly record struct |
| Domain Entity | 行为+身份 | 视 ORM 定夺，优先 class |

> **禁令**：“能用 Record 就全用 Record” = 打回，需 Type Semantic Fit

## 可观测性（P9）

```
Observability = Trace + Metrics + Logs + Privacy + Cardinality + Cost
```

- 禁止 PII/高基数标签（如 user.name/email）
- 必须脱敏、采样、租户上下文必含
