using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DictionaryType;

/// <summary>
/// 字典类型修改输入.
/// </summary>
[SuppressSniffer]
public class DictionaryTypeUpInput : DictionaryTypeCrInput
{
    /// <summary>
    /// 字典id.
    /// </summary>
    public string Id { get; set; }
}