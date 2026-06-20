# SQL Injection Defense — 强制规则

> 背景：架构审计发现 3 个 CRITICAL SQL 注入点（`ScreenDataSourceService:186`、`ConfigController:290`、`DataInterfaceService`），已修复。此文件确保永久不复发。

## Iron Rule: NEVER Concatenate User Input into SQL

```csharp
// ❌ BLOCKED — Hook guard-sql-injection.mjs 会拦截
string sql = $"SELECT * FROM Users WHERE Name = '{userInput}'";
string sql = "SELECT * FROM Users WHERE Name = '" + userInput + "'";
db.Ado.ExecuteCommand($"DROP TABLE {tableName}");
db.Ado.SqlQuery<dynamic>($"DELETE FROM Log WHERE Date < '{date}'");

// ✅ CORRECT — 参数化
db.Queryable<User>().Where(u => u.Name == userInput).ToList();
db.Ado.SqlQuery<dynamic>("SELECT * FROM Users WHERE Name = @name",
    new SqlSugarParameter("@name", userInput));
db.Ado.ExecuteCommand("DELETE FROM Log WHERE Date < @date",
    new SqlSugarParameter("@date", date));
```

## Dynamic Table/Column Names: Whitelist-Only

```csharp
// ❌ BLOCKED
db.Ado.SqlQuery<T>($"SELECT * FROM {userProvidedTable}");

// ✅ CORRECT — 白名单
private static readonly HashSet<string> AllowedTables = new()
    { "BASE_USER", "FLOW_TASK", "EXT_EMPLOYEE" };

if (!AllowedTables.Contains(tableName))
    throw Oops.Bah("Invalid table name");
// 此时 tableName 可安全使用
```

## Severity Classification

| 模式 | 严重程度 | Hook 行为 |
|------|----------|-----------|
| `$"DROP TABLE` / `$"DROP DATABASE` / `$"TRUNCATE` | CRITICAL | **BLOCK (exit 2)** |
| `$"DELETE FROM` | CRITICAL | **BLOCK (exit 2)** |
| `$"SELECT` / `$"INSERT` / `$"UPDATE` + string interpolation | CRITICAL | **BLOCK (exit 2)** |
| `string.Format(` + SQL keyword | CRITICAL | **BLOCK (exit 2)** |
| `Ado.SqlQuery` / `Ado.ExecuteCommand` + `$"` | CRITICAL | **BLOCK (exit 2)** |
| Unparameterized raw SQL without `$` | HIGH | **WARN (exit 1)** |

> Hook 文件：`.claude/hooks/guard-sql-injection.mjs` — 在 PreToolUse Write/Edit 阶段拦截所有 `.cs` 文件写入。
