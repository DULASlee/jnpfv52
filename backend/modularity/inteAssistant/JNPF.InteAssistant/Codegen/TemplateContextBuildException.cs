namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// TemplateContext 构建失败（严格模式）。
/// DeveloperSkillService 捕获后应 <c>throw Oops.Bah(ex.Message)</c> 返回统一 API 错误。
/// </summary>
public sealed class TemplateContextBuildException : Exception
{
    public TemplateContextBuildException(string message) : base(message)
    {
    }
}
