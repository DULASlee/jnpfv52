using JNPF.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace JNPF.Systems.Entitys.Dto.ComFields;

/// <summary>
/// 常用字段修改输入.
/// </summary>
[SuppressSniffer]
public class ComFieldsUpInput : ComFieldsCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    [Required(ErrorMessage = "id不能为空")]
    public string id { get; set; }
}