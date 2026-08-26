# Performance Change Gate Checklist（v4.0 宪法级）

> **任何 Span/Memory/ArrayPool/ObjectPool/ValueTask/SourceGenerator/Expression.Compile/SIMD 引入前必须全答，否则阻断**

| # | 问题 | 回答 | 证据路径 |
|---|------|------|----------|
| 1 | 当前性能是多少？（基线） |  | BDN 基线报告 |
| 2 | 热点在哪里？（P0.2） |  | counters/trace |
| 3 | Allocation 是多少？ |  | counters/BDN Allocation |
| 4 | GC 影响是多少？（Gen2 频率/暂停） |  | counters |
| 5 | 优化后是多少？（对比） |  | BDN 对比 |
| 6 | 复杂度增加多少？（行数/生命周期/池化点） |  | 代码 diff |
| 7 | 是否值得？（收益 > 成本 ? go : no-go） |  | 决策 |

**判定**：7 问任一缺失或“不值得”却仍实施 = 阻断，视为过度优化。

## 附加禁令

- **ValueTask** 仅当同时满足：高频调用 + 大量同步完成 + Benchmark 证明收益显著，否则默认 Task
- **ArrayPool/池化** 归入 Ownership（P1）统一分析，明确所有权转移与归还责任，禁止返回后 Return
- **无 Benchmark 不得以“性能优化已完成”交付**
