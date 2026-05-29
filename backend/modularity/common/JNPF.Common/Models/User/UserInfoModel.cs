using JNPF.DependencyInjection;

namespace JNPF.Common.Models.User
{
    /// <summary>
    /// 登录者信息
    /// 版 本：V3.2.0
    /// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
    /// 作 者：JNPF开发平台组.
    /// </summary>
    [SuppressSniffer]
    public class UserInfoModel
    {
        /// <summary>
        /// 用户主键.
        /// </summary>
        public string userId { get; set; }

        /// <summary>
        /// 用户账户.
        /// </summary>
        public string userAccount { get; set; }

        /// <summary>
        /// 用户姓名.
        /// </summary>
        public string userName { get; set; }

        /// <summary>
        /// 用户头像.
        /// </summary>
        public string headIcon { get; set; }

        /// <summary>
        /// 用户性别.
        /// </summary>
        public string gender { get; set; }

        /// <summary>
        /// 座机号.
        /// </summary>
        public string landline { get; set; }

        /// <summary>
        /// 电话.
        /// </summary>
        public string telePhone { get; set; }

        /// <summary>
        /// 所属组织.
        /// </summary>
        public string organizeId { get; set; }

        /// <summary>
        /// 所属组织 Id 树.
        /// </summary>
        public List<string> organizeIdList { get; set; }

        /// <summary>
        /// 组织名称.
        /// </summary>
        public string organizeName { get; set; }

        /// <summary>
        /// 我的主管.
        /// </summary>
        public string managerId { get; set; }

        /// <summary>
        /// 下属机构.
        /// </summary>
        public string[] subsidiary { get; set; }

        /// <summary>
        /// 我的下属.
        /// </summary>
        public string[] subordinates { get; set; }

        /// <summary>
        /// 岗位信息.
        /// </summary>
        public List<PositionInfoModel> positionIds { get; set; }

        /// <summary>
        /// 岗位名称.
        /// </summary>
        public string positionName { get; set; }

        /// <summary>
        /// 岗位主键.
        /// </summary>
        public string positionId { get; set; }

        /// <summary>
        /// 角色主键.
        /// </summary>
        public string roleId { get; set; }

        /// <summary>
        /// 分组Ids.
        /// </summary>
        public List<string> groupIds { get; set; }

        /// <summary>
        /// 分组名称.
        /// </summary>
        public List<string> groupNames { get; set; }

        /// <summary>
        /// 角色主键名称.
        /// </summary>
        public string roleName { get; set; }

        /// <summary>
        /// 角色数组.
        /// </summary>
        public string[] roleIds { get; set; }

        /// <summary>
        /// 登录时间.
        /// </summary>
        public DateTime? loginTime { get; set; }

        /// <summary>
        /// 登录IP地址.
        /// </summary>
        public string loginIPAddress { get; set; }

        /// <summary>
        /// 登录IP地址所在城市.
        /// </summary>
        public string loginIPAddressName { get; set; }

        /// <summary>
        /// 登录MAC地址.
        /// </summary>
        public string MACAddress { get; set; }

        /// <summary>
        /// 登录平台设备.
        /// </summary>
        public string loginPlatForm { get; set; }

        /// <summary>
        /// 上次登录开启（0:未开启,1:已开启）.
        /// </summary>
        /// <returns></returns>
        public int? prevLogin { get; set; }

        /// <summary>
        /// 上次登录时间.
        /// </summary>
        /// <returns></returns>
        public DateTime? prevLoginTime { get; set; }

        /// <summary>
        /// 上次登录IP地址.
        /// </summary>
        /// <returns></returns>
        public string prevLoginIPAddress { get; set; }

        /// <summary>
        /// 上次登录IP地址所在城市.
        /// </summary>
        /// <returns></returns>
        public string prevLoginIPAddressName { get; set; }

        /// <summary>
        /// 是否超级管理员.
        /// </summary>
        public bool isAdministrator { get; set; }

        /// <summary>
        /// 过期时间.
        /// </summary>
        public TimeSpan? overdueTime { get; set; }

        /// <summary>
        /// 租户编码.
        /// </summary>
        public string tenantId { get; set; }

        /// <summary>
        /// 租户数据库类型.
        /// </summary>
        public string tenantDbType { get; set; }

        /// <summary>
        /// 门户id.
        /// </summary>
        public string portalId { get; set; }

        /// <summary>
        /// app门户id.
        /// </summary>
        public string? appPortalId { get; set; }

        /// <summary>
        /// 数据范围.
        /// </summary>
        public List<UserDataScopeModel> dataScope { get; set; }

        /// <summary>
        /// 直属主管.
        /// </summary>
        public string manager { get; set; }

        /// <summary>
        /// 手机号.
        /// </summary>
        public string mobilePhone { get; set; }

        /// <summary>
        /// 邮箱.
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// 生日.
        /// </summary>
        public DateTime? birthday { get; set; }

        /// <summary>
        /// 部门Id.
        /// </summary>
        public string departmentId { get; set; }

        /// <summary>
        /// 部门名称 结构树.
        /// </summary>
        public string departmentName { get; set; }

        /// <summary>
        /// 当前系统Id.
        /// </summary>
        public string systemId { get; set; }

        /// <summary>
        /// app当前系统Id.
        /// </summary>
        public string appSystemId { get; set; }

        /// <summary>
        /// 默认签名.
        /// </summary>
        public string signImg { get; set; }

        /// <summary>
        /// 默认签名.
        /// </summary>
        public DateTime? changePasswordDate { get; set; }

        /// <summary>
        /// 系统集合.
        /// </summary>
        public List<UserSystemModel> systemIds { get; set; }

        /// <summary>
        /// 协同办公（0-关闭，1-开启）.
        /// </summary>
        public int? workflowEnabled { get; set; }
    }
	
	    public class Loginer
    {
        public string DataSetID { get; set; }
        public string DBName { get; set; }
        public string Account { get; set; }
        public string ID { get; set; }
        public string AccountName { get; set; }
    }
}