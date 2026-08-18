using System.Collections.Generic;
using System.Text.Json;

namespace JNPF.InteAssistant.Entitys.Ir.Contracts;

/// <summary>
/// IR2_DDL 完整契约（阶段九 P9-S1）。
///
/// 修复缺口：
///   ⑥ DDL 从不透明 SQL 字符串升级为结构化 TableDefinition[]（含列/主键/外键/索引）
///   ② 外键关系从 DDL FK 子句解析（编译器可直接消费，不需猜）
///
/// 向后兼容：保留 RawSql 字段，结构化 Tables 为新增。
/// </summary>
public sealed class DdlPayload
{
    public string Dialect { get; set; } = "sqlserver";

    /// <summary>原始 SQL 字符串（向后兼容，保留）</summary>
    public string RawSql { get; set; } = "";

    /// <summary>结构化表定义（新增：编译器直接消费）</summary>
    public List<TableDefinition> Tables { get; set; } = new();

    /// <summary>从 DDLStabilized payload JSON 解析（双向兼容：结构化 tables 或 raw SQL）</summary>
    public static DdlPayload Parse(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new DdlPayload();

        var payload = new DdlPayload();

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            payload.Dialect = GetString(root, "dialect") ?? "sqlserver";
            payload.RawSql = GetString(root, "ddl") ?? GetString(root, "rawSql") ?? "";

            // 结构化 tables（新格式）
            if (root.TryGetProperty("tables", out var tablesEl) && tablesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in tablesEl.EnumerateArray())
                {
                    var table = new TableDefinition
                    {
                        TableName = GetString(t, "tableName") ?? GetString(t, "name") ?? "",
                        EntityName = GetString(t, "entityName") ?? "",
                        Description = GetString(t, "description") ?? "",
                    };

                    // 列定义
                    if (t.TryGetProperty("columns", out var colsEl) && colsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var c in colsEl.EnumerateArray())
                        {
                            table.Columns.Add(new ColumnDefinition
                            {
                                Name = GetString(c, "name") ?? "",
                                DataType = GetString(c, "dataType") ?? GetString(c, "type") ?? "NVARCHAR(255)",
                                IsPrimaryKey = ReadBool(c, "primaryKey") ?? ReadBool(c, "isPrimaryKey") ?? false,
                                IsNullable = ReadBool(c, "nullable") ?? ReadBool(c, "isNullable") ?? true,
                                IsIdentity = ReadBool(c, "identity") ?? ReadBool(c, "isIdentity") ?? false,
                                DefaultValue = GetString(c, "defaultValue"),
                                Description = GetString(c, "description") ?? GetString(c, "comment"),
                            });
                        }
                    }

                    // 外键定义（结构化，编译器直接消费）
                    if (t.TryGetProperty("foreignKeys", out var fkEl) && fkEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var fk in fkEl.EnumerateArray())
                        {
                            table.ForeignKeys.Add(new ForeignKeyDefinition
                            {
                                ColumnName = GetString(fk, "column") ?? GetString(fk, "columnName") ?? "",
                                ReferencesTable = GetString(fk, "referencesTable") ?? GetString(fk, "refTable") ?? "",
                                ReferencesColumn = GetString(fk, "referencesColumn") ?? GetString(fk, "refColumn") ?? "id",
                                OnDelete = GetString(fk, "onDelete") ?? "NO ACTION",
                                OnUpdate = GetString(fk, "onUpdate") ?? "NO ACTION",
                            });
                        }
                    }

                    // 索引定义
                    if (t.TryGetProperty("indexes", out var idxEl) && idxEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ix in idxEl.EnumerateArray())
                        {
                            table.Indexes.Add(new IndexDefinition
                            {
                                IndexName = GetString(ix, "name") ?? GetString(ix, "indexName") ?? "",
                                Columns = ParseStringArray(ix, "columns"),
                                IsUnique = ReadBool(ix, "unique") ?? ReadBool(ix, "isUnique") ?? false,
                            });
                        }
                    }

                    payload.Tables.Add(table);
                }
            }

            // 兜底：如果无结构化 tables 但有 rawSql，尝试解析 SQL（基础正则）
            if (payload.Tables.Count == 0 && !string.IsNullOrEmpty(payload.RawSql))
            {
                payload.Tables = SqlFallbackParser.ParseCreateTables(payload.RawSql);
            }
        }
        catch
        {
            // 解析失败，返回空（容错）
        }

        return payload;
    }

    /// <summary>校验：至少有 1 个表，每表至少有 1 列</summary>
    public void Validate()
    {
        if (Tables.Count == 0 && !string.IsNullOrEmpty(RawSql))
            return; // 有 raw SQL 但未结构化，放行（向后兼容）

        if (Tables.Count == 0)
            throw new System.InvalidOperationException("DDL 无表定义");

        foreach (var t in Tables)
        {
            if (string.IsNullOrWhiteSpace(t.TableName))
                throw new System.InvalidOperationException("DDL 表定义缺少 tableName");
            if (t.Columns.Count == 0)
                throw new System.InvalidOperationException($"表 {t.TableName} 无列定义");
        }
    }

    // ─── 辅助方法 ───
    internal static string? GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
    }

    internal static bool? ReadBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.True;
    }

    internal static List<string> ParseStringArray(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return new();
        if (prop.ValueKind == JsonValueKind.String) return new() { prop.GetString() ?? "" };
        if (prop.ValueKind != JsonValueKind.Array) return new();
        return prop.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
    }
}

public sealed class TableDefinition
{
    public string TableName { get; set; } = "";
    /// <summary>绑定的实体名（对应 EntityDraftContract.EntityName）</summary>
    public string EntityName { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ColumnDefinition> Columns { get; set; } = new();
    public List<ForeignKeyDefinition> ForeignKeys { get; set; } = new();
    public List<IndexDefinition> Indexes { get; set; } = new();
}

public sealed class ColumnDefinition
{
    public string Name { get; set; } = "";
    /// <summary>SQL 类型，如 "NVARCHAR(255)" / "BIGINT" / "DATETIME"</summary>
    public string DataType { get; set; } = "NVARCHAR(255)";
    public bool IsPrimaryKey { get; set; }
    public bool IsNullable { get; set; } = true;
    public bool IsIdentity { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}

public sealed class ForeignKeyDefinition
{
    public string ColumnName { get; set; } = "";
    public string ReferencesTable { get; set; } = "";
    public string ReferencesColumn { get; set; } = "id";
    public string OnDelete { get; set; } = "NO ACTION";
    public string OnUpdate { get; set; } = "NO ACTION";
}

public sealed class IndexDefinition
{
    public string IndexName { get; set; } = "";
    public List<string> Columns { get; set; } = new();
    public bool IsUnique { get; set; }
}

/// <summary>
/// SQL 兜底解析器：当 LLM 只产出 raw SQL 时，用正则提取表/列/FK。
/// 不如结构化精确，但比"0 列退化硬编码 stub"好得多。
/// </summary>
internal static class SqlFallbackParser
{
    /// <summary>从 CREATE TABLE 语句列表解析结构化表定义</summary>
    public static List<TableDefinition> ParseCreateTables(string sql)
    {
        var tables = new List<TableDefinition>();

        // 匹配 CREATE TABLE [schema.]TableName ( ... )
        var tableMatches = System.Text.RegularExpressions.Regex.Matches(
            sql,
            @"CREATE\s+TABLE\s+(?:\[?\w+\]?\.)?\[?(\w+)\]?\s*\((.*?)\)\s*(?:GO|;|$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        foreach (System.Text.RegularExpressions.Match m in tableMatches)
        {
            var tableName = m.Groups[1].Value;
            var body = m.Groups[2].Value;
            var table = new TableDefinition { TableName = tableName };

            // 按行/逗号拆分列定义
            var lines = body.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // 跳过约束子句（PRIMARY KEY, FOREIGN KEY, CONSTRAINT, UNIQUE, CHECK, INDEX）
                var upper = trimmed.ToUpperInvariant();
                if (upper.StartsWith("PRIMARY KEY") || upper.StartsWith("FOREIGN KEY") ||
                    upper.StartsWith("CONSTRAINT") || upper.StartsWith("UNIQUE") ||
                    upper.StartsWith("CHECK") || upper.StartsWith("INDEX"))
                {
                    // 解析 FK：FOREIGN KEY (Col) REFERENCES Table(Col)
                    if (upper.StartsWith("FOREIGN KEY"))
                    {
                        var fkMatch = System.Text.RegularExpressions.Regex.Match(trimmed,
                            @"FOREIGN\s+KEY\s*\(\[?(\w+)\]?\)\s*REFERENCES\s+\[?(\w+)\]?\s*\(\[?(\w+)\]?\)",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (fkMatch.Success)
                        {
                            table.ForeignKeys.Add(new ForeignKeyDefinition
                            {
                                ColumnName = fkMatch.Groups[1].Value,
                                ReferencesTable = fkMatch.Groups[2].Value,
                                ReferencesColumn = fkMatch.Groups[3].Value,
                            });
                        }
                    }
                    continue;
                }

                // 列定义：[F_Col] DataType ...
                var colMatch = System.Text.RegularExpressions.Regex.Match(trimmed,
                    @"^\[?(\w+)\]?\s+(\w+(?:\s*\([^)]*\))?)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (colMatch.Success)
                {
                    var colName = colMatch.Groups[1].Value;
                    var dataType = colMatch.Groups[2].Value.Trim();
                    table.Columns.Add(new ColumnDefinition
                    {
                        Name = colName,
                        DataType = dataType,
                        IsPrimaryKey = upper.Contains("PRIMARY KEY"),
                        IsNullable = !upper.Contains("NOT NULL"),
                        IsIdentity = upper.Contains("IDENTITY"),
                    });
                }
            }

            if (table.Columns.Count > 0)
                tables.Add(table);
        }

        return tables;
    }
}
