# 数据量敏感度评估规则（P3 — v5.0）

> 本文件为 generic-class-refactor-expert v5.0 的 reference 文件。
> 排查 D5（Performance）维度时，对触发以下模式的代码，必须附加数据量评估。

## 触发模式

| 模式编号 | 模式名称 | 代码特征 | 说明 |
|---|---|---|---|
| P3-1 | 全量加载 | `.ToListAsync()` / `.ToList()` 无 `.Take()` / `.Skip()` / 无 `WHERE` | 可能全表加载 |
| P3-2 | N+1 查询 | `foreach` 内嵌 `await Queryable/DbSet/Execute` | 循环内每次发起 DB round-trip |
| P3-3 | 全量内存读取 | `new byte[stream.Length]` / `ReadToEnd()` / `ReadAllBytes()` | 一次性全量进内存 |
| P3-4 | 全目录扫描 | `Directory.GetFiles()` / `FileHelper.GetAllFiles()` 无数量限制 | 目录文件数未知时风险高 |
| P3-5 | DataTable 全行操作 | `DataTable.Select()` / `DataRow[]` 无预过滤 | 内存表全量过滤 |

## 评估步骤（对每个触发的模式执行）

```
Q1: 涉及哪个表/资源？
    → 直接看 FROM/表名/文件路径

Q2: 该表/资源的业务数据量级？
    → 按以下经验值估算：

    用户相关表（Sys_User / Sys_Role 等）：1K ~ 100K
    业务主表（Order / Schedule 等）：10K ~ 1M
    业务子表（OrderEntry / ScheduleUser 等）：10K ~ 5M
    配置表（Sys_Config / Sys_Dict 等）：< 1K
    文件系统：取决于部署，可能 GB 级
    内存硬编码数据：通常 < 100 行
    不确定：标注 DATA_VOLUME_UNKNOWN

Q3: 是否有 WHERE 条件限制返回行数？
    → 看 SQL 生成链路，是否有 .Where() 且条件列是否有索引
    → WHERE 主键/唯一键 → 大幅缩小范围
    → WHERE 非索引列 → 可能全表扫描
    → 无 WHERE → 全表

Q4: 综合判断
    DATA_VOLUME_LOW:
      配置表级别（< 1K）或有精确 WHERE + 索引
      → 不报 Finding，标记为 PASS with note

    DATA_VOLUME_MEDIUM:
      数据量不确定或 WHERE 条件非索引
      → 报 Finding，严重度 Medium，标记 NEED_DATA_VOLUME_REVIEW

    DATA_VOLUME_HIGH:
      业务大表（> 10K）且无行数限制
      → 报 Finding，严重度 High，标记 NEEDS_FIX
```

## 输出字段

D5 维度的 Finding，除原有字段外，必须附加：

| 字段 | 类型 | 说明 | 示例 |
|---|---|---|---|
| `DataVolume` | LOW / MEDIUM / HIGH / UNKNOWN | 数据量风险级别 | `MEDIUM` |
| `DataVolumeReason` | string | 一句话说清为什么是这个级别 | `ScheduleUser 表，WHERE ScheduleId 单值过滤，循环 N 次，N=同组日程数 5~365` |
| `DataVolumeDecision` | ACCEPTABLE / NEED_REVIEW / NEEDS_FIX | 综合决策 | `NEED_REVIEW` |

## 重要边界

- **有 WHERE ≠ 安全**：WHERE 条件匹配行数可能仍然很大（如 `WHERE Status=1` 匹配 10 万行），需评估条件选择性
- **有 `.Take()` 但 Take 值很大**：`.Take(10000)` 仍可能触发性能问题
- **数据量 UNKNOWN 不代表无风险**：标记 UNKNOWN 时必须附加 `NEED_DATA_VOLUME_REVIEW`，不得直接判 PASS

## 版本记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v5.0 | 2026-08-28 | 初始版本，5 模式 + 4 步评估 |
