namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// 阶段四 A5 锁定的 3 个后端 .vm 模板 ID（1-SingleTable 族 + 共享接口模板）。
/// </summary>
public static class VmTemplateIds
{
    public const string ProfileSingleTable = "1-SingleTable";

    public const string Entity = "1-SingleTable/Entity.cs.vm";
    public const string Service = "1-SingleTable/Service.cs.vm";
    public const string IService = "IService.cs.vm";

    public static readonly IReadOnlyList<string> LockedBackendTemplates = new[]
    {
        Entity,
        Service,
        IService,
    };
}
