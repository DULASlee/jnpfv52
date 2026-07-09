using System.Text;

namespace JNPF.InteAssistant.Entitys.Ir.Naming;

/// <summary>
/// Entity/table/column naming policy — 单一命名派生入口（契约主权）。
/// 所有消费端（ExtractTableNames/BuildAllFromSkillContext/EntityDesignProjector）MUST 调用此类，
/// 禁止自行实现 ToSnakeUpper/ToSnakeLower 等派生逻辑。
/// </summary>
public static class EntityNamingPolicy
{
    public static string ResolveTableName(string? tableName, string entityName)
    {
        if (!string.IsNullOrWhiteSpace(tableName))
            return tableName.Trim();

        return ToSnakeUpper(entityName);
    }

    public static string ToSnakeUpper(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        return ToSnakeLower(name).ToUpperInvariant();
    }

    public static string ToSnakeLower(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0 && sb.Length > 0 && sb[^1] != '_')
                sb.Append('_');

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    public static string ToPropertyName(string fieldOrColumnName)
    {
        if (string.IsNullOrWhiteSpace(fieldOrColumnName))
            return fieldOrColumnName;

        var raw = fieldOrColumnName.StartsWith("F_", StringComparison.OrdinalIgnoreCase)
            ? fieldOrColumnName[2..]
            : fieldOrColumnName;

        if (raw.Contains('_', StringComparison.Ordinal))
        {
            var parts = raw.Split('_', StringSplitOptions.RemoveEmptyEntries);
            raw = string.Concat(parts.Select(p =>
                p.Length == 1
                    ? char.ToUpperInvariant(p[0]).ToString()
                    : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
        }

        return raw.Length == 1
            ? char.ToUpperInvariant(raw[0]).ToString()
            : char.ToUpperInvariant(raw[0]) + raw[1..];
    }

    public static string ToDbColumnName(string fieldName)
    {
        var propertyName = ToPropertyName(fieldName);
        if (string.IsNullOrWhiteSpace(propertyName))
            return propertyName;

        return propertyName.StartsWith("F_", StringComparison.OrdinalIgnoreCase)
            ? propertyName
            : $"F_{propertyName}";
    }
}
