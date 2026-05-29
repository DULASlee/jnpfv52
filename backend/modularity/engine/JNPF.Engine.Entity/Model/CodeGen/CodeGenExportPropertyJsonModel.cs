using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

[SuppressSniffer]
public class CodeGenExportPropertyJsonModel
{
    public string filedName { get; set; }

    public string jnpfKey { get; set; }

    public string filedId { get; set; }

    /// <summary>
    /// 是否必填.
    /// </summary>
    public bool required { get; set; }

    /// <summary>
    /// 是否多选.
    /// </summary>
    public bool multiple { get; set; }
}