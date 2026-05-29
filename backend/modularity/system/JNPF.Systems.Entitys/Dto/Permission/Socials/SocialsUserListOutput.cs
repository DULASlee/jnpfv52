using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Socials;

/// <summary>
/// 用户授权列表输出.
/// </summary>
[SuppressSniffer]
public class SocialsUserListOutput
{
    /// <summary>
    /// 类型.
    /// </summary>
    public string enname { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string describetion { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public string since { get; set; }

    /// <summary>
    /// logo.
    /// </summary>
    public string logo { get; set; }

    /// <summary>
    /// 官网api文档.
    /// </summary>
    public string apiDoc { get; set; }

    /// <summary>
    /// 是否首页展示.
    /// </summary>
    public bool latest { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 绑定对象.
    /// </summary>
    public SocialsUserModel entity { get; set; }

    /// <summary>
    /// 获取登录地址.
    /// </summary>
    public string renderUrl { get; set; }
}

public class SocialsUserModel
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 系统用户id.
    /// </summary>
    public string userId { get; set; }

    /// <summary>
    /// 第三方类型.
    /// </summary>
    public string socialType { get; set; }

    /// <summary>
    /// 第三方uuid.
    /// </summary>
    public string socialId { get; set; }

    /// <summary>
    /// 第三方账号.
    /// </summary>
    public string socialName { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime creatorTime { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }
}