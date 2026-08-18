using JNPF.Engine.Entity.Model.CodeGen;

namespace JNPF.InteAssistant.Codegen.TemplateContext;

/// <summary>
/// IR-2 → JNPF .vm 强类型 TemplateContext（A5）。
/// 禁止 Dictionary 反射渲染；通过 <see cref="ToViewModel"/> 投影为模板 @Model。
/// </summary>
public sealed class Ir2CodegenContext
{
    public required string ProjectId { get; init; }
    public required string TenantId { get; init; }
    public required string SampleId { get; init; }

    /// <summary>模板族 ID，如 1-SingleTable。</summary>
    public required string TemplateProfileId { get; init; }

    public required string NameSpace { get; init; }
    public required string ClassName { get; init; }
    public required string BusName { get; init; }
    public required string OriginalMainTableName { get; init; }

    public string PrimaryKey { get; init; } = "Id";
    public string OriginalPrimaryKey { get; init; } = "F_Id";
    public int PrimaryKeyPolicy { get; init; } = 1;

    public int WebType { get; init; } = 2;
    public int Type { get; init; } = 1;
    public bool EnableFlow { get; init; }
    public bool IsTenantColumn { get; init; } = true;
    public bool IsMainTable { get; init; } = true;
    public bool IsInlineEditor { get; init; }
    public bool IsMapper { get; init; } = true;
    public bool hasPage { get; init; } = true;
    public bool IsExport { get; init; }
    public bool IsBatchRemove { get; init; } = true;
    public bool IsUploading { get; init; }
    public bool IsTableRelations { get; init; }
    public bool IsSystemControl { get; init; }
    public bool IsUpdate { get; init; } = true;
    public bool IsBillRule { get; init; }
    public bool UseDataPermission { get; init; }
    public bool IsConversion { get; init; }
    public bool IsDetailConversion { get; init; }
    public bool HasSuperQuery { get; init; }
    public bool ConcurrencyLock { get; init; }
    public bool IsUnique { get; init; }
    public bool IsImportData { get; init; }
    public bool IsSearchMultiple { get; init; }
    public bool IsTreeTable { get; init; }
    public bool IsLogicalDelete { get; init; }
    public int TableType { get; init; } = 1;
    public int SearchControlNum { get; init; }

    public IReadOnlyList<TableColumnConfigModel> TableField { get; init; } =
        Array.Empty<TableColumnConfigModel>();

    public IReadOnlyList<TableColumnConfigModel> RelationsField { get; init; } =
        Array.Empty<TableColumnConfigModel>();

    public IReadOnlyList<CodeGenFunctionModel> Function { get; init; } =
        Array.Empty<CodeGenFunctionModel>();

    public IReadOnlyList<CodeGenTableRelationsModel> TableRelations { get; init; } =
        Array.Empty<CodeGenTableRelationsModel>();

    public IReadOnlyList<string[]> ParsJnpfKeyConstList { get; init; } = Array.Empty<string[]>();
    public IReadOnlyList<string[]> ParsJnpfKeyConstListDetails { get; init; } = Array.Empty<string[]>();

    /// <summary>
    /// 投影为 JNPF ViewEngine @Model（与 CodeGenService.RunCompileFromCached 匿名对象字段对齐）。
    /// </summary>
    public object ToViewModel()
    {
        var mainTable = ClassName;
        var lowerMainTable = char.ToLowerInvariant(mainTable[0]) + mainTable[1..];
        var lowerPrimaryKey = char.ToLowerInvariant(PrimaryKey[0]) + PrimaryKey[1..];

        return new
        {
            IsInlineEditor,
            IsMainTable,
            NameSpace,
            BusName,
            ClassName,
            PrimaryKey,
            LowerPrimaryKey = lowerPrimaryKey,
            OriginalPrimaryKey,
            MainTable = mainTable,
            LowerMainTable = lowerMainTable,
            OriginalMainTableName,
            hasPage,
            Function,
            TableField,
            RelationsField,
            TableFieldCount = TableField.Count(c => !c.PrimaryKey && c.jnpfKey != null),
            DefaultSidx = PrimaryKey,
            IsExport,
            IsBatchRemove,
            IsUploading,
            IsTableRelations,
            IsMapper,
            IsSystemControl,
            IsUpdate,
            IsBillRule,
            DbLinkId = "0",
            FormId = ProjectId,
            WebType,
            Type,
            EnableFlow,
            EnCode = ClassName,
            UseDataPermission,
            SearchControlNum,
            ExportField = string.Empty,
            ConfigId = TenantId,
            DBName = "DevTest",
            PcUseDataPermission = "false",
            AppUseDataPermission = "false",
            FullName = BusName,
            IsConversion,
            IsDetailConversion,
            HasSuperQuery,
            PrimaryKeyPolicy,
            ConcurrencyLock,
            IsUnique,
            GroupField = string.Empty,
            GroupShowField = string.Empty,
            IsImportData,
            ImportColumnField = string.Empty,
            ParsJnpfKeyConstList,
            ParsJnpfKeyConstListDetails,
            ImportDataType = string.Empty,
            DataRuleJson = "[]",
            IsSearchMultiple,
            IsTreeTable,
            ParentField = string.Empty,
            TreeShowField = string.Empty,
            IsLogicalDelete,
            TableType,
            IsTenantColumn,
            PcKeywordSearchColumn = string.Empty,
            AppKeywordSearchColumn = string.Empty,
            PcDefaultSortConfig = false,
            AppDefaultSortConfig = false,
            TableRelations,
        };
    }
}
