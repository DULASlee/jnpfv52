using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.Integrate;

/// <summary>
/// 集成助手列表返回值模型.
/// </summary>
[SuppressSniffer]
public class InteAssisOnlyIdListResultModel
{
    /// <summary>
    /// 自然主键.
    /// </summary>
    public string id { get; set; }
}