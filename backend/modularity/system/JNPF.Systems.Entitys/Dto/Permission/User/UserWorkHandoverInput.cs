namespace JNPF.Systems.Entitys.Dto.Permission.User;

/// <summary>
/// 用户工作交接输入.
/// </summary>
public class UserWorkHandoverInput
{
    /// <summary>
    /// 移交人.
    /// </summary>
    public string fromId { get; set; }

    /// <summary>
    /// 交接人.
    /// </summary>
    public string toId { get; set; }

    /// <summary>
    /// 待办事宜.
    /// </summary>
    public List<string> waitList { get; set; }

    /// <summary>
    /// 负责流程.
    /// </summary>
    public List<string> flowList { get; set; }

    /// <summary>
    /// 权限组.
    /// </summary>
    public List<string> permissionList { get; set; }
}
