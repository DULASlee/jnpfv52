namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// IR → Codegen 编译上下文的默认值常量。
/// 集中管理硬编码兜底值，避免领域污染。
/// </summary>
public static class Ir2CodegenDefaults
{
    /// <summary>
    /// 业务名称未配置时的兜底值。
    /// 使用英文通用名 "BusinessEntity" 替代中文 "请假申请"，
    /// 避免将特定业务领域的默认值泄漏到其他项目。
    /// </summary>
    public const string FallbackBusName = "BusinessEntity";
}
