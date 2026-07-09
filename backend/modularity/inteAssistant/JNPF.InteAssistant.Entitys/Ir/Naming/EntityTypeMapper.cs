namespace JNPF.InteAssistant.Entitys.Ir.Naming;

/// <summary>
/// C# ↔ SQL type mapping — 单一类型映射入口（契约主权）。
/// 消灭 TemplateContextBuilder.MapSqlTypeToNetType 等分散副本。
/// </summary>
public static class EntityTypeMapper
{
    public static string ToSqlType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return "NVARCHAR(255)";

        var normalized = Normalize(type);
        return normalized switch
        {
            "int" or "integer" => "INT",
            "long" or "bigint" => "BIGINT",
            "bool" or "boolean" or "bit" => "BIT",
            "decimal" or "money" or "numeric" => "DECIMAL(18,2)",
            "double" or "float" => "FLOAT",
            "datetime" or "date" or "datetimeoffset" => "DATETIME",
            "guid" or "uuid" => "UNIQUEIDENTIFIER",
            "text" => "NVARCHAR(MAX)",
            _ => "NVARCHAR(255)",
        };
    }

    public static string ToNetType(string? sqlType)
    {
        if (string.IsNullOrWhiteSpace(sqlType))
            return "string";

        var upper = sqlType.Trim().ToUpperInvariant();
        var baseType = upper.Split('(', ' ', '\t', '\r', '\n')[0];
        return baseType switch
        {
            "INT" => "int",
            "BIGINT" => "long",
            "SMALLINT" => "short",
            "TINYINT" => "byte",
            "BIT" => "bool",
            "DECIMAL" or "NUMERIC" or "MONEY" or "SMALLMONEY" => "decimal",
            "FLOAT" or "REAL" => "double",
            "DATETIME" or "DATETIME2" or "DATE" or "SMALLDATETIME" or "DATETIMEOFFSET" => "DateTime?",
            "UNIQUEIDENTIFIER" => "string",
            _ => "string",
        };
    }

    public static string NormalizeNetType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return "string";

        return Normalize(type) switch
        {
            "integer" or "int32" => "int",
            "bigint" or "int64" => "long",
            "boolean" => "bool",
            "datetime" or "date" => "DateTime?",
            "guid" or "uuid" => "string",
            "float" => "double",
            "string" or "text" => "string",
            var other => other,
        };
    }

    private static string Normalize(string value) => value.Trim().TrimEnd('?').ToLowerInvariant();
}
