using System.Collections.Generic;

namespace JNPF.InteAssistant.Sa;

/// <summary>
/// SA 九步编译器的默认值常量。
/// 集中管理硬编码字符串/数值，避免分散在 Compiler 各方法中。
/// </summary>
public static class SaCompilerDefaults
{
    /// <summary>系统名称未配置时的默认值。</summary>
    public const string DefaultSystemName = "业务系统";

    /// <summary>系统名称未配置时的简短默认值。</summary>
    public const string DefaultSystemNameShort = "系统";

    /// <summary>FK 推导假设的默认置信度（命名以 Id 结尾但无显式 References）。</summary>
    public const decimal DefaultForeignKeyConfidence = 0.6m;

    /// <summary>外部实体推断假设的默认置信度。</summary>
    public const decimal DefaultExternalEntityConfidence = 0.7m;

    /// <summary>
    /// IR 草案字段类型 → SQL 列类型映射表。
    /// 默认兜底为 NVARCHAR(255)。
    /// </summary>
    public static readonly Dictionary<string, string> SqlTypeMap = new()
    {
        ["string"] = "NVARCHAR(255)",
        ["text"] = "NVARCHAR(MAX)",
        ["datetime"] = "DATETIME",
        ["decimal"] = "DECIMAL(18,2)",
        ["int"] = "INT",
        ["bigint"] = "BIGINT",
        ["boolean"] = "BIT",
        ["bool"] = "BIT",
        ["file"] = "NVARCHAR(500)",
        ["json"] = "NVARCHAR(MAX)",
    };

    /// <summary>未知类型或已包含括号的类型（如 DECIMAL(10,2)）直接透传，否则兜底 NVARCHAR(255)。</summary>
    public const string DefaultSqlType = "NVARCHAR(255)";
}
