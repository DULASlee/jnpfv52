using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Position;

/// <summary>
/// 更新岗位输入.
/// </summary>
[SuppressSniffer]
public class PositionUpInput : PositionCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}