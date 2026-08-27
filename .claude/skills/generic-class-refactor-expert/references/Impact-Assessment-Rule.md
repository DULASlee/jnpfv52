# 影响面评估规则（P4 — v5.0）

> 本文件为 generic-class-refactor-expert v5.0 的 reference 文件。
> 对 D4（Security）、D5（Performance）、D10（Code Quality）维度的每个 Finding，在报告前必须执行本评估。

## 目的

区分"外部用户可触达的真实风险"和"内部硬编码数据的虚假风险"。
P4 的作用是精确化严重度，不是降低所有严重度。

## 数据来源分类

| 来源类别 | 判定条件 | 影响面 | 严重度影响 |
|---|---|---|---|
| `HARDCODED` | 变量直接赋值字面量字符串/数字 | 内部/测试，无外部输入 | 降级为 Low（Critical 除外） |
| `CONFIG` | 来自 `IConfiguration` / `appsettings.json` / 环境变量 | 运维可控 | 保持原级，备注 |
| `HTTP_REQUEST` | 来自 `[FromBody]` / `[FromQuery]` / `HttpContext.Request` | 外部用户可触达 | 保持原级 |
| `DATABASE_RESULT` | 来自数据库查询结果 | 间接外部输入 | 保持原级 |
| `REFLECTION` | 来自程序集类型/属性元数据反射 | 需管理员权限才能修改 | 降级为 Low（Critical 除外） |
| `NEED_DATA_SOURCE` | 赋值链 3 层内无法确定来源 | 需人工确认 | 保持原级，附加标注 |

## 评估步骤

```
Step 1: 找到"输入变量"
        找到问题代码中接收外部数据的那个变量。

        例: string.Format("update {0} set ...", mainTableName)
            输入变量 = mainTableName

Step 2: 追溯赋值链（最多 3 层）
        从输入变量出发，向上追溯是谁赋值的。

        Layer 0: mainTableName（当前问题代码中的变量）
        Layer 1: mainTableName ← 方法参数 tableName
        Layer 2: tableName ← 调用方传入 request.TableConfig.TableName
        Layer 3: request.TableConfig.TableName ← [FromBody] HTTP 请求

        如果 3 层内追溯到了明确来源，停止。
        如果 3 层内无法确定来源，标记 NEED_DATA_SOURCE。

Step 3: 判定数据来源类别（对照上方分类表）

Step 4: 严重度修正（见下方规则）
```

## 严重度修正规则

```
原始严重度 = Critical:
  → 不可降级，保持 Critical，备注数据来源

原始严重度 = High 或 Medium:
  来源 = HARDCODED    → 降级为 Low
  来源 = REFLECTION   → 降级为 Low
  来源 = CONFIG       → 保持原级，备注"运维可控"
  来源 = HTTP_REQUEST → 保持原级
  来源 = DATABASE_RESULT → 保持原级
  来源 = NEED_DATA_SOURCE → 保持原级，附加标注

原始严重度 = Low:
  → 不降级（已经很低了）

原始严重度 = Negligible:
  → 不降级
```

## 安全阀

- **宁可不降级，也不要把真实风险降为虚假安全**
- 如果不确定来源 → 不降级，标记 NEED_DATA_SOURCE
- Critical 任何情况都不可降级

## 输出字段

D4/D5/D10 维度的 Finding，除原有字段外，必须附加：

| 字段 | 类型 | 说明 | 示例 |
|---|---|---|---|
| `DataSource` | HARDCODED / CONFIG / HTTP_REQUEST / DATABASE_RESULT / REFLECTION / NEED_DATA_SOURCE | 数据来源类别 | `HTTP_REQUEST` |
| `DataSourceTrace` | string | 赋值链追溯路径 | `mainTableName ← param ← [FromBody] request.TableConfig.TableName` |
| `SeverityOriginal` | Critical/High/Medium/Low/Negligible | 评估前严重度 | `Medium` |
| `SeverityAdjusted` | Critical/High/Medium/Low/Negligible | 评估后严重度 | `Medium` |

## 版本记录

| 版本 | 日期 | 变更 |
|---|---|---|
| v5.0 | 2026-08-28 | 初始版本，6 源头分类 + 4 步评估 + 严重度修正规则 |
