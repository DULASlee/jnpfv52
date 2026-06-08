# CopyNew 行为验证报告

> 验证日期：2026-06-07
> 测试环境：SQLite 内存数据库

## 测试结果

| 验证项 | 结果 | 详情 |
|---|---|---|
| 1. 独立连接实例 | 通过 | parent 和 copy 并发查询互不阻塞 |
| 2. 连接字典继承 | 共享 | CopyNew 后连接字典可用 |
| 3. 性能开销 | 通过 | 平均单次 < 1ms |
| 4. Dispose 隔离 | 通过 | child.Dispose 后 parent 不受影响 |
| 5. GC 回收 | 通过 | GC 能正确回收未 Dispose 的 CopyNew 实例 |

## 对阶段 4 Repository 设计的影响

- 连接字典共享：Repository 不需要 AddConnection，直接 GetConnectionScope 即可
- CopyNew 性能开销极小，可在 EventBus 订阅者和后台任务中放心使用
- Dispose 隔离安全，子实例释放不影响父实例
