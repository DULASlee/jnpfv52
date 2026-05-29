using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成表单真实控件.
/// </summary>
[SuppressSniffer]
public class CodeGenFormRealControlModel
{
    public string jnpfKey { get; set; }

    public string vModel { get; set; }

    public bool multiple { get; set; }

    public List<CodeGenFormRealControlModel> children { get; set; }
}