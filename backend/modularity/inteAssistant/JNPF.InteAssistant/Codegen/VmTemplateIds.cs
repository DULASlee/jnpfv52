namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// 代码生成模板 ID 清单。
///
/// P9-S2 扩展：从 3 个后端模板扩展为全套后端（Entity/Service/IService/Mapper/DTO）。
/// 模板文件位于 backend/application/JNPF.API.Entry/wwwroot/Template/（6 个 profile 目录）。
/// 现阶段锁定 1-SingleTable profile（单体单表）。
/// </summary>
public static class VmTemplateIds
{
    public const string ProfileSingleTable = "1-SingleTable";

    // ─── 后端核心（P1-A5 原有）───
    public const string Entity = "1-SingleTable/Entity.cs.vm";
    public const string Service = "1-SingleTable/Service.cs.vm";
    public const string IService = "IService.cs.vm";

    // ─── 后端扩展（P9-S2 新解锁）───
    public const string Mapper = "1-SingleTable/Mapper.cs.vm";
    public const string CrInput = "1-SingleTable/CrInput.cs.vm";
    public const string ListQueryInput = "1-SingleTable/ListQueryInput.cs.vm";
    public const string InfoOutput = "1-SingleTable/InfoOutput.cs.vm";
    public const string ListOutput = "1-SingleTable/ListOutput.cs.vm";
    public const string DetailOutput = "1-SingleTable/DetailOutput.cs.vm";
    public const string UpInput = "UpInput.cs.vm";  // 共享模板（非 profile 目录）

    /// <summary>
    /// 全套后端模板（P9-S2 扩展）。
    /// 9 个模板覆盖一个实体的完整后端：Entity + Service + IService + Mapper + 5 个 DTO。
    /// </summary>
    public static readonly IReadOnlyList<string> LockedBackendTemplates = new[]
    {
        Entity,
        Service,
        IService,
        Mapper,
        CrInput,
        UpInput,
        ListQueryInput,
        InfoOutput,
        ListOutput,
        DetailOutput,
    };

    /// <summary>
    /// 前端模板（P9-S3 第三期启用，现阶段预留）。
    /// </summary>
    public static readonly IReadOnlyList<string> LockedFrontendTemplates = Array.Empty<string>();
}
