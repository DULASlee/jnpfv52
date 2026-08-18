namespace JNPF.InteAssistant.Sa;

/// <summary>
/// 实体关系推导工具（纯函数，无状态）。
/// 统一 SaNineViewCompiler / PreAnalysisModel 中重复的 FK 命名推导逻辑。
///
/// 规则：
/// 1. 字段名以 "Id" 结尾且非主键 → 推导为外键，尝试匹配目标实体。
/// 2. 优先使用显式 References 声明。
/// 3. ToField 从目标实体的主键列名推断，而非硬编码 "id"。
/// </summary>
public static class EntityRelationInferenceService
{
    /// <summary>
    /// 从字段名猜测引用的目标实体。
    /// 例如 "UserId" → 在实体列表中匹配 "User"。
    /// </summary>
    /// <param name="fieldName">字段名（如 "UserId"）</param>
    /// <param name="entities">候选实体列表</param>
    /// <returns>匹配到的实体名，或 null</returns>
    public static string? GuessRefEntity(string fieldName, IReadOnlyList<PreAnalysisEntityDraft> entities)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || entities.Count == 0)
            return null;

        var baseName = fieldName.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
            ? fieldName[..^2]
            : fieldName;

        if (string.IsNullOrWhiteSpace(baseName))
            return null;

        // 精确匹配优先
        var exact = entities.FirstOrDefault(e =>
            string.Equals(e.EntityName, baseName, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact.EntityName;

        // 模糊匹配：实体名包含 baseName 或 baseName 包含实体名
        var fuzzy = entities.FirstOrDefault(e =>
            e.EntityName.Contains(baseName, StringComparison.OrdinalIgnoreCase)
            || baseName.Contains(e.EntityName, StringComparison.OrdinalIgnoreCase));
        return fuzzy?.EntityName;
    }

    /// <summary>
    /// 解析字段的引用目标实体（优先读 References 契约，退化使用命名推导）。
    /// </summary>
    /// <param name="field">字段定义</param>
    /// <param name="entityNames">已知实体名集合</param>
    /// <param name="entities">实体列表（用于命名推导回退）</param>
    /// <returns>目标实体名，或 null</returns>
    public static string? ResolveRefEntity(
        PreAnalysisFieldDraft field,
        HashSet<string> entityNames,
        IReadOnlyList<PreAnalysisEntityDraft> entities)
    {
        // 优先：显式 References 契约（格式 "EntityName.FieldName"）
        if (!string.IsNullOrEmpty(field.References))
        {
            var parts = field.References.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 1 && entityNames.Contains(parts[0]))
                return parts[0];
        }

        // 退化：EndsWith("Id") 且非 PK → 命名推导
        if (field.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && !field.IsPrimaryKey)
        {
            return GuessRefEntity(field.Name, entities);
        }

        return null;
    }

    /// <summary>
    /// 解析 References 声明的目标字段名。
    /// 若 References 格式为 "EntityName.FieldName"，返回 FieldName；
    /// 否则从目标实体的主键列名推断。
    /// </summary>
    /// <param name="references">References 声明（如 "User.Id" 或 "User"）</param>
    /// <param name="targetEntity">目标实体（已通过 ResolveRefEntity 解析）</param>
    /// <returns>目标字段名（小写下划线格式）</returns>
    public static string ResolveToField(string? references, PreAnalysisEntityDraft? targetEntity)
    {
        // 若 References 包含 ".FieldName"，使用显式声明的字段
        if (!string.IsNullOrEmpty(references))
        {
            var parts = references.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 1)
                return ToSnakeLower(parts[1]);
        }

        // 从目标实体的主键列名推断
        if (targetEntity != null)
        {
            var pkField = targetEntity.Fields.FirstOrDefault(f => f.IsPrimaryKey);
            if (pkField != null && !string.IsNullOrWhiteSpace(pkField.Name))
                return ToSnakeLower(pkField.Name);
        }

        // 最终兜底（JNPF 约定）
        return "id";
    }

    /// <summary>
    /// 判断字段是否为推导的外键（以 Id 结尾、非主键、或有显式 References）。
    /// </summary>
    public static bool IsForeignKey(PreAnalysisFieldDraft field) =>
        !string.IsNullOrEmpty(field.References)
        || (field.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && !field.IsPrimaryKey);

    private static string ToSnakeLower(string name) =>
        string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
