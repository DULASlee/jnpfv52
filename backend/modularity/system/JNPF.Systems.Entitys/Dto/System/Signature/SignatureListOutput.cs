using JNPF.DependencyInjection;
using Newtonsoft.Json;

namespace JNPF.Systems.Entitys.Dto.Signature;

/// <summary>
/// 签章管理列表输出.
/// </summary>
[SuppressSniffer]
public class SignatureListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编号.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 授权集合.
    /// </summary>
    [JsonIgnore]
    public List<string> userIdList { get; set; }

    /// <summary>
    /// 授权人.
    /// </summary>
    public string userIds { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }
}