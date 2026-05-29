using JNPF.FriendlyException;

namespace JNPF.Common.Enums;

/// <summary>
/// 系统错误码.
/// </summary>
[ErrorCodeType]
public enum ErrorCode
{
    #region 系统类型

    #region Auth 1

    /// <summary>
    /// 账号或密码错误.
    /// </summary>
    [ErrorCodeItemMetadata("账号或密码错误")]
    D1000,

    /// <summary>
    /// 非法操作！禁止删除自己.
    /// </summary>
    [ErrorCodeItemMetadata("非法操作，禁止删除自己")]
    D1001,

    /// <summary>
    /// 记录不存在.
    /// </summary>
    [ErrorCodeItemMetadata("记录不存在")]
    D1002,

    /// <summary>
    /// 账号已存在.
    /// </summary>
    [ErrorCodeItemMetadata("账号已存在")]
    D1003,

    /// <summary>
    /// 旧密码不匹配.
    /// </summary>
    [ErrorCodeItemMetadata("旧密码输入错误")]
    D1004,

    /// <summary>
    /// 测试数据禁止更改admin密码.
    /// </summary>
    [ErrorCodeItemMetadata("测试数据禁止更改用户【admin】密码")]
    D1005,

    /// <summary>
    /// 数据已存在.
    /// </summary>
    [ErrorCodeItemMetadata("数据已存在")]
    D1006,

    /// <summary>
    /// 数据不存在或含有关联引用，禁止删除.
    /// </summary>
    [ErrorCodeItemMetadata("数据不存在或含有关联引用，禁止删除")]
    D1007,

    /// <summary>
    /// 禁止为管理员分配角色.
    /// </summary>
    [ErrorCodeItemMetadata("禁止为管理员分配角色")]
    D1008,

    /// <summary>
    /// 重复数据或记录含有不存在数据.
    /// </summary>
    [ErrorCodeItemMetadata("重复数据或记录含有不存在数据")]
    D1009,

    /// <summary>
    /// 禁止为超级管理员角色分配权限.
    /// </summary>
    [ErrorCodeItemMetadata("禁止为超级管理员角色分配权限")]
    D1010,

    /// <summary>
    /// 非法数据.
    /// </summary>
    [ErrorCodeItemMetadata("非法数据")]
    D1011,

    /// <summary>
    /// Id不能为空.
    /// </summary>
    [ErrorCodeItemMetadata("Id不能为空")]
    D1012,

    /// <summary>
    /// 操作失败！您没有权限操作.
    /// </summary>
    [ErrorCodeItemMetadata("操作失败！您没有权限操作")]
    D1013,

    /// <summary>
    /// 禁止删除超级管理员.
    /// </summary>
    [ErrorCodeItemMetadata("禁止删除超级管理员")]
    D1014,

    /// <summary>
    /// 禁止修改超级管理员状态.
    /// </summary>
    [ErrorCodeItemMetadata("无法禁用超级管理员")]
    D1015,

    /// <summary>
    /// 没有权限.
    /// </summary>
    [ErrorCodeItemMetadata("没有权限")]
    D1016,

    /// <summary>
    /// 账号被删除.
    /// </summary>
    [ErrorCodeItemMetadata("账号被删除")]
    D1017,

    /// <summary>
    /// 账号未被激活.
    /// </summary>
    [ErrorCodeItemMetadata("账号未被激活")]
    D1018,

    /// <summary>
    /// 账号已被禁用.
    /// </summary>
    [ErrorCodeItemMetadata("账号已被禁用")]
    D1019,

    /// <summary>
    /// 账号在其他地方登录.
    /// </summary>
    [ErrorCodeItemMetadata("账号在其他地方登录")]
    D1020,

    /// <summary>
    /// 直属主管不能为自己.
    /// </summary>
    [ErrorCodeItemMetadata("直属主管不能为自己")]
    D1021,

    /// <summary>
    /// 该用户未分配权限!.
    /// </summary>
    [ErrorCodeItemMetadata("该用户未分配权限!")]
    D1022,

    /// <summary>
    /// 租户已到期.
    /// </summary>
    [ErrorCodeItemMetadata("租户已到期")]
    D1023,

    /// <summary>
    /// 租户不存在.
    /// </summary>
    [ErrorCodeItemMetadata("租户不存在")]
    D1024,

    /// <summary>
    /// 租户登录失败，请用手机验证码登录.
    /// </summary>
    [ErrorCodeItemMetadata("租户登录失败，请用手机验证码登录")]
    D1025,

    /// <summary>
    /// 直属主管不能为自己的下属.
    /// </summary>
    [ErrorCodeItemMetadata("直属主管不能为自己的下属")]
    D1026,

    /// <summary>
    /// 账号被锁定提示.
    /// </summary>
    [ErrorCodeItemMetadata("请等待{0}分钟后再进行登录，或联系管理员解除锁定")]
    D1027,

    /// <summary>
    /// 验证码错误.
    /// </summary>
    [ErrorCodeItemMetadata("验证码错误")]
    D1029,

    /// <summary>
    /// 验证码已失效.
    /// </summary>
    [ErrorCodeItemMetadata("验证码已失效")]
    D1030,

    /// <summary>
    /// 账号已被锁定，请联系管理员解除锁定.
    /// </summary>
    [ErrorCodeItemMetadata("账号已被锁定，请联系管理员解除锁定")]
    D1031,

    /// <summary>
    /// 数据库异常，请联系管理员！.
    /// </summary>
    [ErrorCodeItemMetadata("数据库异常，请用手机验证登录！")]
    D1032,

    /// <summary>
    /// 非Admin账号不能编辑超管账号.
    /// </summary>
    [ErrorCodeItemMetadata("没有编辑超管账号的权限")]
    D1033,

    /// <summary>
    /// 管理员不能注销.
    /// </summary>
    [ErrorCodeItemMetadata("管理员不能注销")]
    D1034,

    /// <summary>
    /// 登录票据已失效.
    /// </summary>
    [ErrorCodeItemMetadata("登录票据已失效")]
    D1035,

    /// <summary>
    /// 主系统不允许禁用.
    /// </summary>
    [ErrorCodeItemMetadata("更新失败，主系统不允许禁用")]
    D1036,

    /// <summary>
    /// 主系统不允许更改应用编码.
    /// </summary>
    [ErrorCodeItemMetadata("更新失败，主系统不允许修改编码")]
    D1037,

    /// <summary>
    /// 该用户未分配权限.
    /// </summary>
    [ErrorCodeItemMetadata("该用户未分配权限")]
    D1038,

    /// <summary>
    /// 删除失败，请先删除子菜单.
    /// </summary>
    [ErrorCodeItemMetadata("删除失败，请先删除子菜单")]
    D1039,

    /// <summary>
    /// 接口请求失败，请联系管理员.
    /// </summary>
    [ErrorCodeItemMetadata("删除失败，请先删除子菜单")]
    D1040,

    /// <summary>
    /// 新增失败，已超过租户设定账号额度.
    /// </summary>
    [ErrorCodeItemMetadata("新增失败，已超过租户设定账号额度")]
    D1041,

    /// <summary>
    /// 应用不存在.
    /// </summary>
    [ErrorCodeItemMetadata("应用不存在")]
    D1042,

    /// <summary>
    /// 该应用已禁用.
    /// </summary>
    [ErrorCodeItemMetadata("该应用已禁用")]
    D1043,

    #endregion

    #region 机构 2

    /// <summary>
    /// 父机构不存在.
    /// </summary>
    [ErrorCodeItemMetadata("父机构不存在")]
    D2000,

    /// <summary>
    /// 当前机构Id不能与父机构Id相同.
    /// </summary>
    [ErrorCodeItemMetadata("当前机构Id不能与父机构Id相同")]
    D2001,

    /// <summary>
    /// 机构不存在.
    /// </summary>
    [ErrorCodeItemMetadata("机构不存在")]
    D2002,

    /// <summary>
    /// 机构主管不能删除.
    /// </summary>
    [ErrorCodeItemMetadata("机构主管不能删除")]
    D2003,

    /// <summary>
    /// 附属机构下有员工禁止删除.
    /// </summary>
    [ErrorCodeItemMetadata("附属机构下有员工禁止删除")]
    D2004,

    /// <summary>
    /// 该机构下有机构禁止删除.
    /// </summary>
    [ErrorCodeItemMetadata("该机构下有机构禁止删除")]
    D2005,

    /// <summary>
    /// 该机构下有岗位禁止删除.
    /// </summary>
    [ErrorCodeItemMetadata("该机构下有岗位禁止删除")]
    D2006,

    /// <summary>
    /// 只能增加下级机构.
    /// </summary>
    [ErrorCodeItemMetadata("只能增加下级机构")]
    D2007,

    /// <summary>
    /// 机构编码已存在.
    /// </summary>
    [ErrorCodeItemMetadata("机构编码已存在")]
    D2008,

    /// <summary>
    /// 机构名称已存在.
    /// </summary>
    [ErrorCodeItemMetadata("机构名称已存在")]
    D2009,

    /// <summary>
    /// 更新机构失败.
    /// </summary>
    [ErrorCodeItemMetadata("更新机构失败")]
    D2010,

    /// <summary>
    /// 更新机构状态失败.
    /// </summary>
    [ErrorCodeItemMetadata("更新机构状态失败")]
    D2011,

    /// <summary>
    /// 删除机构失败.
    /// </summary>
    [ErrorCodeItemMetadata("删除机构失败")]
    D2012,

    /// <summary>
    /// 新增机构失败.
    /// </summary>
    [ErrorCodeItemMetadata("新增机构失败")]
    D2013,

    /// <summary>
    /// 部门编码已存在.
    /// </summary>
    [ErrorCodeItemMetadata("部门编码已存在")]
    D2014,

    /// <summary>
    /// 部门名称已存在.
    /// </summary>
    [ErrorCodeItemMetadata("部门名称已存在")]
    D2019,

    /// <summary>
    /// 新增部门失败.
    /// </summary>
    [ErrorCodeItemMetadata("新增部门失败")]
    D2015,

    /// <summary>
    /// 更新部门状态失败.
    /// </summary>
    [ErrorCodeItemMetadata("更新部门状态失败")]
    D2016,

    /// <summary>
    /// 删除部门失败.
    /// </summary>
    [ErrorCodeItemMetadata("删除部门失败")]
    D2017,

    /// <summary>
    /// 更新部门失败.
    /// </summary>
    [ErrorCodeItemMetadata("更新部门失败")]
    D2018,

    /// <summary>
    /// 该机构下有角色禁止删除.
    /// </summary>
    [ErrorCodeItemMetadata("该机构下有角色禁止删除")]
    D2020,

    #endregion

    #region 数据字典 3

    /// <summary>
    /// 字典类型不存在.
    /// </summary>
    [ErrorCodeItemMetadata("字典类型不存在")]
    D3000,

    /// <summary>
    /// 字典类型已存在.
    /// </summary>
    [ErrorCodeItemMetadata("字典类型已存在,名称或编码重复")]
    D3001,

    /// <summary>
    /// 字典类型下面有字典值禁止删除.
    /// </summary>
    [ErrorCodeItemMetadata("字典类型下面有字典值禁止删除")]
    D3002,

    /// <summary>
    /// 字典值已存在.
    /// </summary>
    [ErrorCodeItemMetadata("字典值已存在,名称或编码重复")]
    D3003,

    /// <summary>
    /// 字典值不存在.
    /// </summary>
    [ErrorCodeItemMetadata("字典值不存在")]
    D3004,

    /// <summary>
    /// 字典状态错误.
    /// </summary>
    [ErrorCodeItemMetadata("字典状态错误")]
    D3005,

    /// <summary>
    /// 导入文件格式错误.
    /// </summary>
    [ErrorCodeItemMetadata("导入文件格式错误")]
    D3006,

    /// <summary>
    /// 上级分类不存在,无法导入.
    /// </summary>
    [ErrorCodeItemMetadata("上级分类不存在,无法导入")]
    D3007,

    /// <summary>
    /// 导入失败.
    /// </summary>
    [ErrorCodeItemMetadata("导入失败")]
    D3008,

    /// <summary>
    /// 导入文件功能类型错误.
    /// </summary>
    [ErrorCodeItemMetadata("导入文件功能类型错误")]
    D3009,

    #endregion

    #region 菜单 4

    /// <summary>
    /// 菜单已存在.
    /// </summary>
    [ErrorCodeItemMetadata("菜单已存在")]
    D4000,

    /// <summary>
    /// 路由地址为空.
    /// </summary>
    [ErrorCodeItemMetadata("路由地址为空")]
    D4001,

    /// <summary>
    /// 打开方式为空.
    /// </summary>
    [ErrorCodeItemMetadata("打开方式为空")]
    D4002,

    /// <summary>
    /// 权限标识格式为空.
    /// </summary>
    [ErrorCodeItemMetadata("权限标识格式为空")]
    D4003,

    /// <summary>
    /// 权限标识格式错误.
    /// </summary>
    [ErrorCodeItemMetadata("权限标识格式错误")]
    D4004,

    /// <summary>
    /// 权限不存在.
    /// </summary>
    [ErrorCodeItemMetadata("权限不存在")]
    D4005,

    /// <summary>
    /// 父级菜单不能为当前节点，请重新选择父级菜单.
    /// </summary>
    [ErrorCodeItemMetadata("父级菜单不能为当前节点，请重新选择父级菜单")]
    D4006,

    /// <summary>
    /// 不能移动根节点.
    /// </summary>
    [ErrorCodeItemMetadata("不能移动根节点")]
    D4007,

    /// <summary>
    /// 当前目录存在数据,不能修改.
    /// </summary>
    [ErrorCodeItemMetadata("当前目录存在数据,不能修改类型")]
    D4008,

    /// <summary>
    /// 该系统下菜单为空，系统切换失败.
    /// </summary>
    [ErrorCodeItemMetadata("该系统下菜单为空，系统切换失败")]
    D4009,

    /// <summary>
    /// 字段已被方案引用，请先删除{0}方案.
    /// </summary>
    [ErrorCodeItemMetadata("字段已被方案引用，请先删除{0}方案")]
    D4010,

    /// <summary>
    /// 菜单功能地址已存在.
    /// </summary>
    [ErrorCodeItemMetadata("菜单功能地址已存在")]
    D4011,

    /// <summary>
    /// 菜单功能地址已存在.
    /// </summary>
    [ErrorCodeItemMetadata("当前导入菜单为APP端菜单，请在对应模块下导入！")]
    D4012,

    /// <summary>
    /// 菜单功能地址已存在.
    /// </summary>
    [ErrorCodeItemMetadata("当前导入菜单为Web端菜单，请在对应模块下导入！")]
    D4013,

    /// <summary>
    /// 该应用已禁用
    /// </summary>
    [ErrorCodeItemMetadata("该应用已禁用")]
    D4014,

    /// <summary>
    /// 该组织下无应用权限.
    /// </summary>
    [ErrorCodeItemMetadata("该组织内权限为空，组织切换失败!")]
    D4015,

    /// <summary>
    /// 无该组织权限.
    /// </summary>
    [ErrorCodeItemMetadata("无该组织权限!")]
    D4019,

    /// <summary>
    /// 该应用已删除
    /// </summary>
    [ErrorCodeItemMetadata("该应用已删除")]
    D4016,

    /// <summary>
    /// 上级不能为空
    /// </summary>
    [ErrorCodeItemMetadata("上级不能为空")]
    D4017,

    /// <summary>
    /// 该系统下菜单为空，系统切换失败
    /// </summary>
    [ErrorCodeItemMetadata("该系统下菜单为空，系统切换失败")]
    D4018,

    /// <summary>
    /// 该组织无本应用权限,切换失败
    /// </summary>
    [ErrorCodeItemMetadata("该组织无本应用权限,切换失败")]
    D4020,

    /// <summary>
    /// 找不到该上级
    /// </summary>
    [ErrorCodeItemMetadata("找不到该上级")]
    D4021,

    /// <summary>
    /// 找不到该应用
    /// </summary>
    [ErrorCodeItemMetadata("找不到该应用")]
    D4022,

    /// <summary>
    /// 应用不能为空
    /// </summary>
    [ErrorCodeItemMetadata("应用不能为空")]
    D4023,

    #endregion

    #region 用户 5

    /// <summary>
    /// 账户已存在.
    /// </summary>
    [ErrorCodeItemMetadata("账户已存在")]
    D5000,

    /// <summary>
    /// 账户创建失败.
    /// </summary>
    [ErrorCodeItemMetadata("账户创建失败")]
    D5001,

    /// <summary>
    /// 用户不存在.
    /// </summary>
    [ErrorCodeItemMetadata("用户不存在")]
    D5002,

    /// <summary>
    /// 用户关系移除失败.
    /// </summary>
    [ErrorCodeItemMetadata("用户关系移除失败")]
    D5003,

    /// <summary>
    /// 账户更新失败.
    /// </summary>
    [ErrorCodeItemMetadata("账户更新失败")]
    D5004,

    /// <summary>
    /// 更新账户状态失败.
    /// </summary>
    [ErrorCodeItemMetadata("更新账户状态失败")]
    D5005,

    /// <summary>
    /// 密码不一致.
    /// </summary>
    [ErrorCodeItemMetadata("密码不一致")]
    D5006,

    /// <summary>
    /// 旧密码错误，请重新输入.
    /// </summary>
    [ErrorCodeItemMetadata("旧密码错误，请重新输入")]
    D5007,

    /// <summary>
    /// 修改密码失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改密码失败")]
    D5008,

    /// <summary>
    /// 修改密码失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改密码失败")]
    D5009,

    /// <summary>
    /// 修改系统主题失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改系统主题失败")]
    D5010,

    /// <summary>
    /// 修改系统语言失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改系统语言失败")]
    D5011,

    /// <summary>
    /// 修改个人头像失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改个人头像失败")]
    D5012,

    /// <summary>
    /// 上传失败，图片格式不允许上传.
    /// </summary>
    [ErrorCodeItemMetadata("上传失败，图片格式不允许上传")]
    D5013,

    /// <summary>
    /// 设计门户失败.
    /// </summary>
    [ErrorCodeItemMetadata("设计门户失败")]
    D5014,

    /// <summary>
    /// 验证码错误.
    /// </summary>
    [ErrorCodeItemMetadata("验证码输入错误，请重新输入！")]
    D5015,

    /// <summary>
    /// 导入文件格式错误.
    /// </summary>
    [ErrorCodeItemMetadata("导入文件格式错误")]
    D5018,

    /// <summary>
    /// 导入的文件无数据.
    /// </summary>
    [ErrorCodeItemMetadata("导入的文件是空数据")]
    D5019,

    /// <summary>
    /// 修改切换 组 织失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改切换组织失败")]
    D5020,

    /// <summary>
    /// 修改切换 岗位 失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改切换岗位失败")]
    D5021,

    /// <summary>
    /// 修改切换 角色 失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改切换角色失败")]
    D5022,

    /// <summary>
    /// 该组织下无角色或角色权限为空，组织切换失败.
    /// </summary>
    [ErrorCodeItemMetadata("该组织下无角色或角色权限为空，组织切换失败")]
    D5023,

    /// <summary>
    /// 无法修改管理员账户.
    /// </summary>
    [ErrorCodeItemMetadata("无法修改管理员账户")]
    D5024,

    /// <summary>
    /// 第三方登录未配置.
    /// </summary>
    [ErrorCodeItemMetadata("第三方登录未配置!")]
    D5025,

    /// <summary>
    /// 修改失败，新建密码不能与旧密码一样.
    /// </summary>
    [ErrorCodeItemMetadata("修改失败，新建密码不能与旧密码一样")]
    D5026,

    /// <summary>
    /// 工作交接无法转交给本人.
    /// </summary>
    [ErrorCodeItemMetadata("工作交接无法转交给本人")]
    D5027,

    /// <summary>
    /// 工作交接无法转交给管理员.
    /// </summary>
    [ErrorCodeItemMetadata("工作交接无法转交给管理员")]
    D5028,
    #endregion

    #region 岗位 6

    /// <summary>
    /// 岗位编码已存在.
    /// </summary>
    [ErrorCodeItemMetadata("岗位编码已存在")]
    D6000,

    /// <summary>
    /// 新增岗位失败.
    /// </summary>
    [ErrorCodeItemMetadata("新增岗位失败")]
    D6001,

    /// <summary>
    /// 删除岗位失败.
    /// </summary>
    [ErrorCodeItemMetadata("删除岗位失败")]
    D6002,

    /// <summary>
    /// 更新岗位失败.
    /// </summary>
    [ErrorCodeItemMetadata("更新岗位失败")]
    D6003,

    /// <summary>
    /// 更新岗位状态失败.
    /// </summary>
    [ErrorCodeItemMetadata("更新岗位状态失败")]
    D6004,

    /// <summary>
    /// 岗位名称已存在.
    /// </summary>
    [ErrorCodeItemMetadata("岗位名称已存在")]
    D6005,

    /// <summary>
    /// 该岗位不存在.
    /// </summary>
    [ErrorCodeItemMetadata("该岗位不存在")]
    D6006,

    /// <summary>
    /// 删除失败! 已绑定用户.
    /// </summary>
    [ErrorCodeItemMetadata("删除失败! 已绑定用户")]
    D6007,

    /// <summary>
    /// 该岗位下有用户,不允许变更组织.
    /// </summary>
    [ErrorCodeItemMetadata("该岗位下有用户,不允许变更组织")]
    D6008,

    #endregion 

    #region 消息 7

    /// <summary>
    /// 通知公告状态错误.
    /// </summary>
    [ErrorCodeItemMetadata("通知公告状态错误")]
    D7000,

    /// <summary>
    /// 通知公告删除失败.
    /// </summary>
    [ErrorCodeItemMetadata("通知公告删除失败")]
    D7001,

    /// <summary>
    /// 通知公告编辑失败.
    /// </summary>
    [ErrorCodeItemMetadata("通知公告编辑失败，类型必须为草稿")]
    D7002,

    /// <summary>
    /// 通知公告发布失败.
    /// </summary>
    [ErrorCodeItemMetadata("通知公告发布失败")]
    D7003,

    /// <summary>
    /// 获取模板失败.
    /// </summary>
    [ErrorCodeItemMetadata("获取模板失败")]
    D7004,

    /// <summary>
    /// 短信发送失败.
    /// </summary>
    [ErrorCodeItemMetadata("短信发送失败")]
    D7005,

    /// <summary>
    /// 发送失败,失败原因:发件人邮箱配置错误.
    /// </summary>
    [ErrorCodeItemMetadata("发送失败,失败原因:发件人邮箱配置错误")]
    D7006,

    /// <summary>
    /// 发送失败,失败原因:{0}邮箱账号为空.
    /// </summary>
    [ErrorCodeItemMetadata("发送失败,失败原因:{0}邮箱账号为空")]
    D7007,

    /// <summary>
    /// 发送失败,失败原因:{0}邮箱账号格式不正确.
    /// </summary>
    [ErrorCodeItemMetadata("发送失败,失败原因:{0}邮箱账号格式不正确")]
    D7008,

    /// <summary>
    /// 无效链接.
    /// </summary>
    [ErrorCodeItemMetadata("无效链接")]
    D7009,

    /// <summary>
    /// 无效链接.
    /// </summary>
    [ErrorCodeItemMetadata("链接已失效")]
    D7010,

    /// <summary>
    /// 自定义模板编码不能使用系统模板编码规则.
    /// </summary>
    [ErrorCodeItemMetadata("自定义模板编码不能使用系统模板编码规则")]
    D7011,

    /// <summary>
    /// 此记录与消息发送配置关联引用,不允许被删除.
    /// </summary>
    [ErrorCodeItemMetadata("此记录与消息发送配置关联引用,不允许被删除")]
    D7012,

    /// <summary>
    /// 此记录与消息发送配置关联引用,不允许被禁用.
    /// </summary>
    [ErrorCodeItemMetadata("此记录与消息发送配置关联引用,不允许被禁用")]
    D7013,

    /// <summary>
    /// {0}发送失败,失败原因:{0}邮箱账号格式不正确.
    /// </summary>
    [ErrorCodeItemMetadata("{0}发送失败,失败原因:{1}")]
    D7014,

    /// <summary>
    /// 未配置第三方关联引用.
    /// </summary>
    [ErrorCodeItemMetadata("未配置第三方关联引用")]
    D7015,

    /// <summary>
    /// 未关注公众号.
    /// </summary>
    [ErrorCodeItemMetadata("未关注公众号")]
    D7016,

    /// <summary>
    /// 暂无未读消息.
    /// </summary>
    [ErrorCodeItemMetadata("暂无未读消息")]
    D7017,

    /// <summary>
    /// 请先前往系统同步设置，配置钉钉账号.
    /// </summary>
    [ErrorCodeItemMetadata("请先前往系统同步设置，配置钉钉账号")]
    D7018,

    /// <summary>
    /// 请先前往系统同步设置，配置企业微信账号.
    /// </summary>
    [ErrorCodeItemMetadata("请先前往系统同步设置，配置企业微信账号")]
    D7019,

    #endregion

    #region 文件 8

    /// <summary>
    /// 文件不存在.
    /// </summary>
    [ErrorCodeItemMetadata("文件不存在")]
    D8000,

    /// <summary>
    /// 文件上传失败.
    /// </summary>
    [ErrorCodeItemMetadata("文件上传失败")]
    D8001,

    /// <summary>
    /// 已存在同名文件.
    /// </summary>
    [ErrorCodeItemMetadata("已存在同名文件")]
    D8002,

    /// <summary>
    /// 文件下载失败.
    /// </summary>
    [ErrorCodeItemMetadata("文件下载失败")]
    D8003,

    /// <summary>
    /// 上传失败，文件格式不允许上传.
    /// </summary>
    [ErrorCodeItemMetadata("上传失败，文件格式不允许上传")]
    D1800,

    /// <summary>
    /// 预览失败，文件数据错误.
    /// </summary>
    [ErrorCodeItemMetadata("预览失败，文件数据错误")]
    D1801,

    /// <summary>
    /// 预览失败，文件类型不支持.
    /// </summary>
    [ErrorCodeItemMetadata("预览失败，文件类型不支持")]
    D1802,

    /// <summary>
    /// 文件删除失败.
    /// </summary>
    [ErrorCodeItemMetadata("文件删除失败")]
    D1803,

    /// <summary>
    /// 文件删除失败.
    /// </summary>
    [ErrorCodeItemMetadata("文件流获取失败")]
    D1804,

    /// <summary>
    /// 文件删除失败.
    /// </summary>
    [ErrorCodeItemMetadata("文件下载失败,链接已失效")]
    D1805,

    /// <summary>
    /// 文件存储路径错误.
    /// </summary>
    [ErrorCodeItemMetadata("文件存储路径错误")]
    D1806,

    #endregion

    #region 系统配置 9

    /// <summary>
    /// 已存在同名或同编码参数配置.
    /// </summary>
    [ErrorCodeItemMetadata("已存在同名或同编码参数配置")]
    D9000,

    /// <summary>
    /// 禁止删除系统参数.
    /// </summary>
    [ErrorCodeItemMetadata("禁止删除系统参数")]
    D9001,

    /// <summary>
    /// 此IP未在白名单中.
    /// </summary>
    [ErrorCodeItemMetadata("此IP未在白名单中")]
    D9002,

    /// <summary>
    /// 测试连接失败.
    /// </summary>
    [ErrorCodeItemMetadata("测试连接失败")]
    D9003,

    /// <summary>
    /// 检测数据不存在,同步数据失败.
    /// </summary>
    [ErrorCodeItemMetadata("检测数据不存在,同步数据失败")]
    D9004,

    /// <summary>
    /// 同步数据保存失败.
    /// </summary>
    [ErrorCodeItemMetadata("同步数据保存失败")]
    D9005,

    /// <summary>
    /// 同步数据修改失败.
    /// </summary>
    [ErrorCodeItemMetadata("同步数据修改失败")]
    D9006,

    /// <summary>
    /// Token验证失败.
    /// </summary>
    [ErrorCodeItemMetadata("Token验证失败")]
    D9007,

    /// <summary>
    /// Token过期时间超过限定.
    /// </summary>
    [ErrorCodeItemMetadata("超时登出时间,不能超过8百万分钟")]
    D9008,

    /// <summary>
    /// 验证码限定范围:3 - 6位.
    /// </summary>
    [ErrorCodeItemMetadata("验证码限定范围:3 - 6位")]
    D9009,

    /// <summary>
    /// 打印模板不存在.
    /// </summary>
    [ErrorCodeItemMetadata("打印模板不存在")]
    D9010,
    #endregion

    #region 单据规则 10

    /// <summary>
    /// 单据规则已引用，无法删除.
    /// </summary>
    [ErrorCodeItemMetadata("单据规则已引用，无法删除")]
    BR0001,

    #endregion

    #region 任务调度 11

    /// <summary>
    /// 已存在同名任务调度.
    /// </summary>
    [ErrorCodeItemMetadata("已存在同名任务调度")]
    D1100,

    /// <summary>
    /// 任务调度不存在.
    /// </summary>
    [ErrorCodeItemMetadata("任务调度不存在")]
    D1101,

    /// <summary>
    /// 禁止修改任务编码.
    /// </summary>
    [ErrorCodeItemMetadata("禁止修改任务编码")]
    D1102,

    #endregion

    #region 在线开发 14

    /// <summary>
    /// 已存在相同功能.
    /// </summary>
    [ErrorCodeItemMetadata("已存在相同功能")]
    D1400,

    /// <summary>
    /// 错误的模板设计,表关系不能多对多.
    /// </summary>
    [ErrorCodeItemMetadata("错误的模板设计,表关系不能多对多")]
    D1401,

    /// <summary>
    /// 主表未设置主键字段.
    /// </summary>
    [ErrorCodeItemMetadata("主表未设置主键字段")]
    D1402,

    /// <summary>
    /// 复制模板 数据长度超过字段设定长度.
    /// </summary>
    [ErrorCodeItemMetadata("已到达该模板复制上限，请复制源模板")]
    D1403,

    /// <summary>
    /// 上传文件 控件 不填写文件大小.
    /// </summary>
    [ErrorCodeItemMetadata("文件或者图片上传控件的 [文件大小] 必须输入")]
    D1404,

    /// <summary>
    /// 该功能已停用无法同步菜单!
    /// </summary>
    [ErrorCodeItemMetadata("该功能已停用无法同步菜单!")]
    D1405,

    /// <summary>
    /// 该功能名称或编码已存在
    /// </summary>
    [ErrorCodeItemMetadata("该功能名称或编码已存在!")]
    D1406,

    /// <summary>
    /// 唯一校验失败.
    /// </summary>
    [ErrorCodeItemMetadata("{0} 不能重复")]
    D1407,

    /// <summary>
    /// 并发锁定.
    /// </summary>
    [ErrorCodeItemMetadata("当前表单原数据已被调整,请重新进入该页面编辑并提交数据")]
    D1408,

    /// <summary>
    /// 主键策略 表主键数据类型不支持.
    /// </summary>
    [ErrorCodeItemMetadata("主键策略:[{0}],表[ {1} ]主键设置不支持!")]
    D1409,

    /// <summary>
    /// 导入预览失败，表头名称不可更改，表头行不能删除.
    /// </summary>
    [ErrorCodeItemMetadata("预览失败 : 表头名称不可更改,表头行不能删除")]
    D1410,

    /// <summary>
    /// 操作失败，未配置导出模板.
    /// </summary>
    [ErrorCodeItemMetadata("操作失败，未配置导出模板")]
    D1411,

    /// <summary>
    /// 导入预览失败，数据读取错误.
    /// </summary>
    [ErrorCodeItemMetadata("预览失败 : 数据读取错误")]
    D1412,

    /// <summary>
    /// 导入预览失败，控件静态数据出现重复.
    /// </summary>
    [ErrorCodeItemMetadata("预览失败,【{0}】控件静态数据出现重复")]
    D1413,

    /// <summary>
    /// 无表转有表失败.
    /// </summary>
    [ErrorCodeItemMetadata("无表转有表失败")]
    D1414,

    /// <summary>
    /// 未能找到线上版本.
    /// </summary>
    [ErrorCodeItemMetadata("回滚失败,未能找到线上版本")]
    D1415,

    /// <summary>
    /// 请至少选择一个数据表.
    /// </summary>
    [ErrorCodeItemMetadata("请至少选择一个数据表")]
    D1416,

    /// <summary>
    /// 该流程已发起，无法删除.
    /// </summary>
    [ErrorCodeItemMetadata("该流程已发起，无法删除")]
    D1417,

    /// <summary>
    /// 密码错误.
    /// </summary>
    [ErrorCodeItemMetadata("密码错误")]
    D1418,

    /// <summary>
    /// 缺少租户信息.
    /// </summary>
    [ErrorCodeItemMetadata("缺少租户信息")]
    D1419,

    /// <summary>
    /// 无效链接.
    /// </summary>
    [ErrorCodeItemMetadata("无效链接")]
    D1420,

    /// <summary>
    /// 发布失败,流程未设计.
    /// </summary>
    [ErrorCodeItemMetadata("发布失败,流程未设计")]
    D1421,

    /// <summary>
    /// 该表单已删除.
    /// </summary>
    [ErrorCodeItemMetadata("该表单已删除")]
    D1422,

    #endregion

    #region 数据建模 15

    /// <summary>
    /// 删除表失败.
    /// </summary>
    [ErrorCodeItemMetadata("删除表失败")]
    D1500,

    /// <summary>
    /// 新增表失败.
    /// </summary>
    [ErrorCodeItemMetadata("新增表失败")]
    D1501,

    /// <summary>
    /// 修改表失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改表失败")]
    D1502,

    /// <summary>
    /// 表已存在.
    /// </summary>
    [ErrorCodeItemMetadata("表已存在")]
    D1503,

    /// <summary>
    /// 系统自带表,无法删除.
    /// </summary>
    [ErrorCodeItemMetadata("系统自带表,无法删除")]
    D1504,

    /// <summary>
    /// 数据库类型不支持.
    /// </summary>
    [ErrorCodeItemMetadata("数据库类型不支持")]
    D1505,

    /// <summary>
    /// 目的表存在数据，无法同步.
    /// </summary>
    [ErrorCodeItemMetadata("目的表存在数据，无法同步")]
    D1506,

    /// <summary>
    /// 数据库连接失败.
    /// </summary>
    [ErrorCodeItemMetadata("数据库连接失败")]
    D1507,

    /// <summary>
    /// 表存在数据,禁止操作.
    /// </summary>
    [ErrorCodeItemMetadata("表存在数据,禁止操作")]
    D1508,

    /// <summary>
    /// 主键不能为空.
    /// </summary>
    [ErrorCodeItemMetadata("主键不能为空")]
    D1509,

    /// <summary>
    /// 新增字段失败.
    /// </summary>
    [ErrorCodeItemMetadata("新增字段失败")]
    D1510,

    /// <summary>
    /// Sql语法错误.
    /// </summary>
    [ErrorCodeItemMetadata("Sql语法错误")]
    D1511,

    /// <summary>
    /// Sql语法错误.
    /// </summary>
    [ErrorCodeItemMetadata("Sql语法错误 : {0}")]
    D1512,

    /// <summary>
    /// 执行Sql 不能为空.
    /// </summary>
    [ErrorCodeItemMetadata("Sql不能为空")]
    D1513,

    /// <summary>
    /// 表名超过规定长度.
    /// </summary>
    [ErrorCodeItemMetadata("表名超过规定长度")]
    D1514,

    /// <summary>
    /// 列名超过规定长度.
    /// </summary>
    [ErrorCodeItemMetadata("列名超过规定长度")]
    D1515,

    /// <summary>
    /// 数据库连接不能相同.
    /// </summary>
    [ErrorCodeItemMetadata("数据库连接不能相同")]
    D1516,

    /// <summary>
    /// 系统自带表,无法删除.
    /// </summary>
    [ErrorCodeItemMetadata("系统自带表,无法编辑")]
    D1517,

    /// <summary>
    /// 自增长ID字段数据类型必须为整形或长整型.
    /// </summary>
    [ErrorCodeItemMetadata("自增长ID字段数据类型必须为整形或长整型")]
    D1518,

    /// <summary>
    /// 数据库类型不支持自增长ID.
    /// </summary>
    [ErrorCodeItemMetadata("数据库类型不支持自增长ID")]
    D1519,

    /// <summary>
    /// 接口请求失败.
    /// </summary>
    [ErrorCodeItemMetadata("接口请求失败")]
    D1520,

    /// <summary>
    /// 请在数据库中添加对应的数据表.
    /// </summary>
    [ErrorCodeItemMetadata("请在数据库中添加对应的数据表\r\n\r\n")]
    D1521,

    #endregion

    #region 角色 16

    /// <summary>
    /// 角色编码已存在.
    /// </summary>
    [ErrorCodeItemMetadata("角色编码已存在")]
    D1600,

    /// <summary>
    /// 角色名称已存在.
    /// </summary>
    [ErrorCodeItemMetadata("角色名称已存在")]
    D1601,

    /// <summary>
    /// 新建角色失败.
    /// </summary>
    [ErrorCodeItemMetadata("新建角色失败")]
    D1602,

    /// <summary>
    /// 该角色下有数据权限.
    /// </summary>
    [ErrorCodeItemMetadata("该角色下有数据权限")]
    D1603,

    /// <summary>
    /// 该角色下有模块按钮权限.
    /// </summary>
    [ErrorCodeItemMetadata("该角色下有模块按钮权限")]
    D1604,

    /// <summary>
    /// 该角色下有模块列表权限.
    /// </summary>
    [ErrorCodeItemMetadata("该角色下有模块列表权限")]
    D1605,

    /// <summary>
    /// 该角色下有模块列表权限.
    /// </summary>
    [ErrorCodeItemMetadata("该角色下有模块菜单权限")]
    D1606,

    /// <summary>
    /// 删除失败! 已绑定用户.
    /// </summary>
    [ErrorCodeItemMetadata("删除失败! 已绑定用户")]
    D1607,

    /// <summary>
    /// 该角色不存在.
    /// </summary>
    [ErrorCodeItemMetadata("该角色不存在")]
    D1608,

    /// <summary>
    /// 角色删除失败.
    /// </summary>
    [ErrorCodeItemMetadata("角色删除失败")]
    D1609,

    /// <summary>
    /// 角色更新失败.
    /// </summary>
    [ErrorCodeItemMetadata("角色更新失败")]
    D1610,

    /// <summary>
    /// 该角色下有模块表单权限.
    /// </summary>
    [ErrorCodeItemMetadata("该角色下有模块表单权限")]
    D1611,

    /// <summary>
    /// 操作失败！您没有权限操作.
    /// </summary>
    [ErrorCodeItemMetadata("操作失败！您没有权限操作")]
    D1612,

    /// <summary>
    /// 该角色下有用户,不允许变更组织.
    /// </summary>
    [ErrorCodeItemMetadata("该角色和组织下有用户,不允许变更组织")]
    D1613,

    /// <summary>
    /// 不是超管账号无法批量设置权限.
    /// </summary>
    [ErrorCodeItemMetadata("不是超管账号无法批量设置权限")]
    D1614,

    /// <summary>
    /// 该全局角色下有用户,不允许变更成组织角色.
    /// </summary>
    [ErrorCodeItemMetadata("该全局角色下有用户,不允许变更成组织角色")]
    D1615,

    #endregion

    #region 系统缓存 17

    /// <summary>
    /// 清除缓存失败.
    /// </summary>
    [ErrorCodeItemMetadata("清除缓存失败")]
    D1700,

    #endregion

    #region 公共 18

    /// <summary>
    /// 演示环境禁止修改数据.
    /// </summary>
    [ErrorCodeItemMetadata("演示环境禁止修改数据")]
    D1200,

    /// <summary>
    /// 已存在同名或同主机租户.
    /// </summary>
    [ErrorCodeItemMetadata("已存在同名或同主机租户")]
    D1300,

    /// <summary>
    /// 已存在同名或同编码项目.
    /// </summary>
    [ErrorCodeItemMetadata("已存在同名或同编码项目")]
    xg1000,

    /// <summary>
    /// 已存在相同证件号码人员.
    /// </summary>
    [ErrorCodeItemMetadata("已存在相同证件号码人员")]
    xg1001,

    /// <summary>
    /// 检测数据不存在.
    /// </summary>
    [ErrorCodeItemMetadata("检测数据不存在")]
    xg1002,

    /// <summary>
    /// 必要参数不能为空.
    /// </summary>
    [ErrorCodeItemMetadata("必要参数不能为空")]
    xg1003,

    /// <summary>
    /// 该字段不存在.
    /// </summary>
    [ErrorCodeItemMetadata("该字段不存在")]
    xg1004,

    /// <summary>
    /// SQL语句仅支持查询语句.
    /// </summary>
    [ErrorCodeItemMetadata("SQL语句仅支持查询语句")]
    xg1005,

    /// <summary>
    /// 变量名不能包含敏感字符
    /// </summary>
    [ErrorCodeItemMetadata("变量名不能包含敏感字符")]
    xg1006,
    #endregion

    #region 门户 19

    /// <summary>
    /// 您没有此门户使用权限，请重新设置.
    /// </summary>
    [ErrorCodeItemMetadata("您没有此门户使用权限，请重新设置")]
    D1900,

    /// <summary>
    /// 门户分类不能为空.
    /// </summary>
    [ErrorCodeItemMetadata("门户分类不能为空")]
    D1901,

    /// <summary>
    /// 门户名称不能为空.
    /// </summary>
    [ErrorCodeItemMetadata("门户名称不能为空")]
    D1902,

    /// <summary>
    /// 门户编码不能为空.
    /// </summary>
    [ErrorCodeItemMetadata("门户编码不能为空")]
    D1903,

    /// <summary>
    /// 远程请求发生错误.
    /// </summary>
    [ErrorCodeItemMetadata("远程请求发生错误")]
    D1904,

    /// <summary>
    /// 远程获取不到地图数据.
    /// </summary>
    [ErrorCodeItemMetadata("获取不到数据")]
    D1905,

    /// <summary>
    /// 更新门户布局失败.
    /// </summary>
    [ErrorCodeItemMetadata("更新门户布局失败")]
    D1906,

    /// <summary>
    /// 同步门户失败.
    /// </summary>
    [ErrorCodeItemMetadata("同步门户失败")]
    D1907,

    /// <summary>
    /// 添加日程失败.
    /// </summary>
    [ErrorCodeItemMetadata("添加日程失败")]
    D1908,

    /// <summary>
    /// 添加日程日志失败.
    /// </summary>
    [ErrorCodeItemMetadata("添加日程日志失败")]
    D1909,

    /// <summary>
    /// 修改日程失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改日程失败")]
    D1910,

    /// <summary>
    /// 修改日程日志失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改日程日志失败")]
    D1911,

    /// <summary>
    /// 删除日程失败.
    /// </summary>
    [ErrorCodeItemMetadata("删除日程失败")]
    D1912,

    /// <summary>
    /// 删除日程参与人失败.
    /// </summary>
    [ErrorCodeItemMetadata("删除日程参与人失败")]
    D1913,

    /// <summary>
    /// 删除日程日志失败.
    /// </summary>
    [ErrorCodeItemMetadata("删除日程日志失败")]
    D1914,

    /// <summary>
    /// 门户名称不能重复.
    /// </summary>
    [ErrorCodeItemMetadata("门户名称不能重复")]
    D1915,

    /// <summary>
    /// 门户编码不能重复.
    /// </summary>
    [ErrorCodeItemMetadata("门户编码不能重复")]
    D1916,

    /// <summary>
    /// 门户关联删除提示.
    /// </summary>
    [ErrorCodeItemMetadata("此记录被“【{0}】应用门户”关联引用，不允许被删除")]
    D1917,

    /// <summary>
    /// 该日程已被删除.
    /// </summary>
    [ErrorCodeItemMetadata("该日程已被删除")]
    D1918,

    /// <summary>
    /// 该门户已删除.
    /// </summary>
    [ErrorCodeItemMetadata("该门户已删除")]
    D1919,

    #endregion

    #region 代码生成 21

    /// <summary>
    /// 只能生成有表模板.
    /// </summary>
    [ErrorCodeItemMetadata("只能生成有表模板")]
    D2100,

    /// <summary>
    /// 子表名称不能重复.
    /// </summary>
    [ErrorCodeItemMetadata("子表名称不能重复")]
    D2101,

    /// <summary>
    /// 待功能完善.
    /// </summary>
    [ErrorCodeItemMetadata("待功能完善")]
    D2102,

    /// <summary>
    /// 预览失败，数据不存在.
    /// </summary>
    [ErrorCodeItemMetadata("预览失败，数据不存在")]
    D2103,

    /// <summary>
    /// 表缺失主键.
    /// </summary>
    [ErrorCodeItemMetadata("{0}表缺失主键")]
    D2104,

    /// <summary>
    /// 表缺失流程Id字段.
    /// </summary>
    [ErrorCodeItemMetadata("表缺失流程Id字段：f_flow_id")]
    D2105,

    /// <summary>
    /// 模板内表不存在.
    /// </summary>
    [ErrorCodeItemMetadata("模板内表不存在")]
    D2106,

    /// <summary>
    /// 表缺失乐观锁字段.
    /// </summary>
    [ErrorCodeItemMetadata("表缺失乐观锁字段：f_version")]
    D2107,

    /// <summary>
    /// 表缺失流程真实ID字段.
    /// </summary>
    [ErrorCodeItemMetadata("表缺失流程真实ID字段：f_flow_task_id")]
    D2108,

    /// <summary>
    /// 模板主键策略与表主键策略不同.
    /// </summary>
    [ErrorCodeItemMetadata("模板主键策略与表主键策略不同")]
    D2109,

    /// <summary>
    /// 表缺失逻辑删除字段.
    /// </summary>
    [ErrorCodeItemMetadata("表缺失逻辑删除字段：f_delete_mark, f_delete_time, f_delete_user_id")]
    D2110,

    #endregion

    #region 大屏 22

    /// <summary>
    /// 模块键值已存在.
    /// </summary>
    [ErrorCodeItemMetadata("模块键值已存在")]
    D2200,

    #endregion

    #region 分级管理 23

    /// <summary>
    /// 新增分级管理失败.
    /// </summary>
    [ErrorCodeItemMetadata("新增分级管理失败")]
    D2300,

    /// <summary>
    /// 编辑分级管理失败.
    /// </summary>
    [ErrorCodeItemMetadata("编辑分级管理失败")]
    D2301,

    /// <summary>
    /// 该分级管理不存在.
    /// </summary>
    [ErrorCodeItemMetadata("该分级管理不存在")]
    D2302,

    /// <summary>
    /// 删除分级失败.
    /// </summary>
    [ErrorCodeItemMetadata("删除分级失败")]
    D2303,

    /// <summary>
    /// 无法设置当前用户为分级管理员.
    /// </summary>
    [ErrorCodeItemMetadata("无法设置当前用户为分级管理员")]
    D2304,

    /// <summary>
    /// 无法设置超管为分级管理员.
    /// </summary>
    [ErrorCodeItemMetadata("无法设置超管为分级管理员")]
    D2305,
    #endregion

    #region 工作流 WF

    /// <summary>
    /// 委托人和被委托人相同，委托失败.
    /// </summary>
    [ErrorCodeItemMetadata("委托人和被委托人相同，委托失败")]
    WF0001,

    /// <summary>
    /// 复制流程引擎失败.
    /// </summary>
    [ErrorCodeItemMetadata("复制流程引擎失败")]
    WF0002,

    /// <summary>
    /// {0}为子流程不能删除.
    /// </summary>
    [ErrorCodeItemMetadata("{0}为子流程不能删除")]
    WF0003,

    /// <summary>
    /// 系统管理员无法操作流程任务.
    /// </summary>
    [ErrorCodeItemMetadata("系统管理员无法操作流程任务")]
    WF0004,

    /// <summary>
    /// 转向失败，转向节点未审批.
    /// </summary>
    [ErrorCodeItemMetadata("转向失败，转向节点未审批")]
    WF0005,

    /// <summary>
    /// 当前流程被处理，无法审批.
    /// </summary>
    [ErrorCodeItemMetadata("当前流程被处理，无法审批")]
    WF0006,

    /// <summary>
    /// 转办失败.
    /// </summary>
    [ErrorCodeItemMetadata("转办失败")]
    WF0007,

    /// <summary>
    /// 指派失败.
    /// </summary>
    [ErrorCodeItemMetadata("指派失败")]
    WF0008,

    /// <summary>
    /// 未找到催办人.
    /// </summary>
    [ErrorCodeItemMetadata("未找到催办人")]
    WF0009,

    /// <summary>
    /// 流程已处理，无法撤回.
    /// </summary>
    [ErrorCodeItemMetadata("流程已处理，无法撤回")]
    WF0010,

    /// <summary>
    /// 撤回失败，转向数据无法撤回.
    /// </summary>
    [ErrorCodeItemMetadata("撤回失败，转向数据无法撤回")]
    WF0011,

    /// <summary>
    /// {0}为功能流程不能删除.
    /// </summary>
    [ErrorCodeItemMetadata("{0}为功能流程不能删除")]
    WF0012,

    /// <summary>
    /// 转向节点不存在或配置错误.
    /// </summary>
    [ErrorCodeItemMetadata("转向节点不存在或配置错误")]
    WF0013,

    /// <summary>
    /// 子流程无法指派.
    /// </summary>
    [ErrorCodeItemMetadata("子流程无法指派")]
    WF0014,

    /// <summary>
    /// 子流程无法撤回.
    /// </summary>
    [ErrorCodeItemMetadata("子流程无法撤回")]
    WF0015,

    /// <summary>
    /// 功能流程无法终止.
    /// </summary>
    [ErrorCodeItemMetadata("功能流程无法终止")]
    WF0016,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("流程表单不一致，请重新选择")]
    WF0017,

    /// <summary>
    /// 当前流程包含子流程,无法撤回.
    /// </summary>
    [ErrorCodeItemMetadata("当前流程包含子流程,无法撤回")]
    WF0018,

    /// <summary>
    /// 退回节点包含子流程,退回失败.
    /// </summary>
    [ErrorCodeItemMetadata("退回节点包含子流程,退回失败")]
    WF0019,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0020,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0021,

    /// <summary>
    /// 所选流程包含条件候选人.
    /// </summary>
    [ErrorCodeItemMetadata("所选流程包含条件候选人")]
    WF0022,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0023,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0024,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0025,

    /// <summary>
    /// 该流程已删除
    /// </summary>
    [ErrorCodeItemMetadata("该流程已删除")]
    WF0026,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0027,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0028,

    /// <summary>
    /// 该流程无权限查看.
    /// </summary>
    [ErrorCodeItemMetadata("该流程无权限查看")]
    WF0029,

    /// <summary>
    /// 该流程无权限操作.
    /// </summary>
    [ErrorCodeItemMetadata("该流程无权限操作")]
    WF0030,

    /// <summary>
    /// 当前流程正在运行不能重复保存
    /// </summary>
    [ErrorCodeItemMetadata("当前流程正在运行不能重复保存")]
    WF0031,

    /// <summary>
    /// 当前流程不经过该驳回节点!
    /// </summary>
    [ErrorCodeItemMetadata("当前流程不经过该驳回节点!")]
    WF0032,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0033,

    /// <summary>
    /// 该流程无法复活
    /// </summary>
    [ErrorCodeItemMetadata("该流程无法复活")]
    WF0034,

    /// <summary>
    /// 下一节点无审批人员请联系管理员
    /// </summary>
    [ErrorCodeItemMetadata("下一节点无审批人员请联系管理员!")]
    WF0035,

    /// <summary>
    /// 当前节点无法操作
    /// </summary>
    [ErrorCodeItemMetadata("当前节点无法操作")]
    WF0036,

    /// <summary>
    /// 该流程内工单任务流转未结束，无法删除！
    /// </summary>
    [ErrorCodeItemMetadata("该流程内工单任务流转未结束，无法删除！")]
    WF0037,

    /// <summary>
    /// 启用失败，流程未配置！
    /// </summary>
    [ErrorCodeItemMetadata("启用失败，流程未配置！")]
    WF0038,

    /// <summary>
    /// 数据传递失败.
    /// </summary>
    [ErrorCodeItemMetadata("数据传递失败，流程表单模板未找到！")]
    WF0039,

    /// <summary>
    /// 父流程处于驳回状态,无法审批.
    /// </summary>
    [ErrorCodeItemMetadata("父流程处于驳回状态,无法审批！")]
    WF0040,

    /// <summary>
    /// 操作失败，同一时间内有相同流程的委托！.
    /// </summary>
    [ErrorCodeItemMetadata("操作失败，同一时间内有相同流程的委托！")]
    WF0041,

    /// <summary>
    /// 操作失败，同一时间内有相同流程的委托！.
    /// </summary>
    [ErrorCodeItemMetadata("操作失败，同一时间内有相同流程，不能相互委托！")]
    WF0042,

    /// <summary>
    /// 当前功能表单已存在关联引用.
    /// </summary>
    [ErrorCodeItemMetadata("该功能已被流程引用,请重新选择关联功能!")]
    WF0043,

    /// <summary>
    /// 此流程不支持变更.
    /// </summary>
    [ErrorCodeItemMetadata("此流程不支持变更!")]
    WF0044,

    /// <summary>
    /// 退回至您的审批,不能再退回审批.
    /// </summary>
    [ErrorCodeItemMetadata("退回至您的审批,不能再退回审批!")]
    WF0045,

    /// <summary>
    /// 此流程已被挂起,无法操作!.
    /// </summary>
    [ErrorCodeItemMetadata("流程处于挂起状态，不可操作!")]
    WF0046,

    /// <summary>
    /// {0}流程已被挂起,无法操作!.
    /// </summary>
    [ErrorCodeItemMetadata("{0}流程已被挂起不能删除!")]
    WF0047,

    /// <summary>
    ///  .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0048,

    /// <summary>
    ///  您没有该流程的发起权限.
    /// </summary>
    [ErrorCodeItemMetadata("您没有该流程的发起权限")]
    WF0049,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0050,

    /// <summary>
    /// .
    /// </summary>
    [ErrorCodeItemMetadata("")]
    WF0051,

    /// <summary>
    /// 该流程由在线开发生成的,无法直接删除,请在功能设计中删除相关功能!.
    /// </summary>
    [ErrorCodeItemMetadata("该流程由在线开发生成的,无法直接删除,请在功能设计中删除相关功能!")]
    WF0052,

    /// <summary>
    /// 表单信息不存在.
    /// </summary>
    [ErrorCodeItemMetadata("表单信息不存在")]
    WF0053,

    /// <summary>
    /// 退回节点未审批，退回失败.
    /// </summary>
    [ErrorCodeItemMetadata("退回节点未审批，退回失败")]
    WF0054,

    /// <summary>
    /// {0}没有删除权限.
    /// </summary>
    [ErrorCodeItemMetadata("{0}没有删除权限")]
    WF0055,

    /// <summary>
    /// 子流程自动发起审批失败.
    /// </summary>
    [ErrorCodeItemMetadata("子流程自动发起审批失败")]
    WF0056,

    /// <summary>
    /// 流程自动发起审批失败.
    /// </summary>
    [ErrorCodeItemMetadata("流程自动发起审批失败")]
    WF0057,
    #endregion

    #region 扩展 Ex

    /// <summary>
    /// 防止恶意创建过多数据.
    /// </summary>
    [ErrorCodeItemMetadata("防止恶意创建过多数据")]
    Ex0001,

    /// <summary>
    /// 操作失败,无法移动同一文件下.
    /// </summary>
    [ErrorCodeItemMetadata("操作失败,无法移动同一文件下")]
    Ex0002,

    /// <summary>
    /// 邮箱账户认证错误.
    /// </summary>
    [ErrorCodeItemMetadata("邮箱账户认证错误")]
    Ex0003,

    /// <summary>
    /// 未设置邮箱账户.
    /// </summary>
    [ErrorCodeItemMetadata("未设置邮箱账户")]
    Ex0004,

    /// <summary>
    /// 操作失败,无法移动子文件.
    /// </summary>
    [ErrorCodeItemMetadata("操作失败,父级文件夹无法移动到子级文件夹中")]
    Ex0005,

    /// <summary>
    /// 删除失败,该文件夹下存在数据.
    /// </summary>
    [ErrorCodeItemMetadata("删除失败,该文件夹下存在数据")]
    Ex0006,

    /// <summary>
    /// 还原失败,该文件父级不存在.
    /// </summary>
    [ErrorCodeItemMetadata("还原失败,该文件父级不存在")]
    Ex0007,
    #endregion

    #region 多租户 Zh

    /// <summary>
    /// 租户不存在.
    /// </summary>
    [ErrorCodeItemMetadata("租户不存在")]
    Zh10000,

    /// <summary>
    /// 租户已过期.
    /// </summary>
    [ErrorCodeItemMetadata("租户已过期")]
    Zh10001,

    /// <summary>
    /// 租户账户已存在.
    /// </summary>
    [ErrorCodeItemMetadata("租户账户已存在")]
    Zh10002,

    /// <summary>
    /// 无权限访问.
    /// </summary>
    [ErrorCodeItemMetadata("无权限访问")]
    Zh10003,

    #endregion

    #region 分组管理 24

    /// <summary>
    /// 新增分组失败.
    /// </summary>
    [ErrorCodeItemMetadata("新增分组失败")]
    D2400,

    /// <summary>
    /// 分组编码已存在.
    /// </summary>
    [ErrorCodeItemMetadata("分组编码已存在")]
    D2401,

    /// <summary>
    /// 分组名称已存在.
    /// </summary>
    [ErrorCodeItemMetadata("分组名称已存在")]
    D2402,

    /// <summary>
    /// 分组删除失败.
    /// </summary>
    [ErrorCodeItemMetadata("分组删除失败")]
    D2403,

    /// <summary>
    /// 分组更新失败.
    /// </summary>
    [ErrorCodeItemMetadata("分组更新失败")]
    D2404,

    /// <summary>
    /// 该分组不存在.
    /// </summary>
    [ErrorCodeItemMetadata("该分组不存在")]
    D2405,

    /// <summary>
    /// 删除失败! 已绑定用户.
    /// </summary>
    [ErrorCodeItemMetadata("删除失败! 已绑定用户")]
    D2406,
    #endregion

    #region 接口认证 IO
    /// <summary>
    /// 请求头部参数Authorization不能为空.
    /// </summary>
    [ErrorCodeItemMetadata("请求头部参数Authorization不能为空")]
    IO0001,

    /// <summary>
    /// 请求头部参数YmDate不能为空.
    /// </summary>
    [ErrorCodeItemMetadata("请求头部参数YmDate不能为空")]
    IO0002,

    /// <summary>
    /// 接口未授权.
    /// </summary>
    [ErrorCodeItemMetadata("接口未授权")]
    IO0003,

    /// <summary>
    /// 接口授权已过期.
    /// </summary>
    [ErrorCodeItemMetadata("接口授权已过期")]
    IO0004,

    /// <summary>
    /// 接口请求失败或返回结构不对.
    /// </summary>
    [ErrorCodeItemMetadata("接口请求失败或接口数据异常")]
    IO0005,

    /// <summary>
    /// 鉴权接口请求失败或返回结构不对.
    /// </summary>
    [ErrorCodeItemMetadata("鉴权接口请求失败或接口数据异常")]
    IO0006,

    /// <summary>
    /// 变量名已存在.
    /// </summary>
    [ErrorCodeItemMetadata("变量名已存在")]
    IO0007,

    /// <summary>
    /// 未填写userKey,请确认.
    /// </summary>
    [ErrorCodeItemMetadata("未填写userKey,请确认")]
    IO0008,

    /// <summary>
    /// userKey不匹配,请填写正确的userKey
    /// </summary>
    [ErrorCodeItemMetadata("userKey不匹配,请填写正确的userKey")]
    IO0009,

    /// <summary>
    /// 参数类型错误
    /// </summary>
    [ErrorCodeItemMetadata("参数类型错误")]
    IO0010,
    #endregion

    #region 权限管理 25

    /// <summary>
    /// 新增权限组失败.
    /// </summary>
    [ErrorCodeItemMetadata("新增权限组失败")]
    D2500,

    /// <summary>
    /// 编辑权限组失败.
    /// </summary>
    [ErrorCodeItemMetadata("编辑权限组失败")]
    D2501,

    /// <summary>
    /// 该权限组不存在.
    /// </summary>
    [ErrorCodeItemMetadata("该权限组不存在")]
    D2502,

    /// <summary>
    /// 删除权限组失败.
    /// </summary>
    [ErrorCodeItemMetadata("删除权限组失败")]
    D2503,

    /// <summary>
    /// 无法设置当前用户操作权限.
    /// </summary>
    [ErrorCodeItemMetadata("无法设置当前用户操作权限")]
    D2504,
    #endregion

    #region 集成助手 26

    /// <summary>
    /// 正在修复中.
    /// </summary>
    [ErrorCodeItemMetadata("正在修复中")]
    D2600,

    /// <summary>
    /// 路径错误.
    /// </summary>
    [ErrorCodeItemMetadata("路径错误")]
    D2601,

    /// <summary>
    /// 集成助手被禁用.
    /// </summary>
    [ErrorCodeItemMetadata("集成助手被禁用")]
    D2602,

    #endregion

    #endregion

    #region 公用

    /// <summary>
    /// 新增数据失败.
    /// </summary>
    [ErrorCodeItemMetadata("新增数据失败")]
    COM1000,

    /// <summary>
    /// 修改数据失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改数据失败")]
    COM1001,

    /// <summary>
    /// 删除数据失败.
    /// </summary>
    [ErrorCodeItemMetadata("删除数据失败")]
    COM1002,

    /// <summary>
    /// 修改状态失败.
    /// </summary>
    [ErrorCodeItemMetadata("修改状态失败")]
    COM1003,

    /// <summary>
    /// 已存在同名或同编码数据.
    /// </summary>
    [ErrorCodeItemMetadata("已存在同名或同编码数据")]
    COM1004,

    /// <summary>
    /// 检测数据不存在.
    /// </summary>
    [ErrorCodeItemMetadata("检测数据不存在")]
    COM1005,

    /// <summary>
    /// 同步失败.
    /// </summary>
    [ErrorCodeItemMetadata("同步失败")]
    COM1006,

    /// <summary>
    /// 数据不存在.
    /// </summary>
    [ErrorCodeItemMetadata("数据不存在")]
    COM1007,

    /// <summary>
    /// 操作失败.
    /// </summary>
    [ErrorCodeItemMetadata("操作失败")]
    COM1008,

    /// <summary>
    /// 已到达该模板复制上限，请复制源模板.
    /// </summary>
    [ErrorCodeItemMetadata("已到达该模板复制上限，请复制源模板")]
    COM1009,

    /// <summary>
    /// 当前表单原数据已被调整，请重新进入该页面编辑并提交数据
    /// </summary>
    [ErrorCodeItemMetadata("当前表单原数据已被调整，请重新进入该页面编辑并提交数据")]
    COM1010,

    /// <summary>
    /// 流程表单回滚失败.
    /// </summary>
    [ErrorCodeItemMetadata("该表单未发布，无法回滚表单内容！")]
    COM1011,

    /// <summary>
    /// 流程表单删除失败.
    /// </summary>
    [ErrorCodeItemMetadata("该表单已被流程引用，无法删除！")]
    COM1012,

    /// <summary>
    /// 无法发布空表单.
    /// </summary>
    [ErrorCodeItemMetadata("该模板内表单内容为空，无法发布！")]
    COM1013,

    /// <summary>
    /// 无法发布空列表.
    /// </summary>
    [ErrorCodeItemMetadata("该模板内列表内容为空，无法发布！")]
    COM1014,

    /// <summary>
    /// 同步失败,检查编码或名称是否重复.
    /// </summary>
    [ErrorCodeItemMetadata("同步失败,检查编码或名称是否重复")]
    COM1015,

    /// <summary>
    /// 流程未设计,请先设计流程.
    /// </summary>
    [ErrorCodeItemMetadata("流程未设计,请先设计流程")]
    COM1016,

    /// <summary>
    /// 该功能配置的流程处于停用状态.
    /// </summary>
    [ErrorCodeItemMetadata("该功能配置的流程处于停用状态")]
    COM1017,

    /// <summary>
    /// {0}.
    /// </summary>
    [ErrorCodeItemMetadata("{0}")]
    COM1018,

    /// <summary>
    /// 该功能未导入流程表单！.
    /// </summary>
    [ErrorCodeItemMetadata("该功能未导入流程表单！")]
    COM1019,

    /// <summary>
    /// 导入失败：{0}.
    /// </summary>
    [ErrorCodeItemMetadata("导入失败：{0}")]
    COM1020,

    /// <summary>
    /// 系统再审批常用中被使用,不允许删除！.
    /// </summary>
    [ErrorCodeItemMetadata("系统在审批常用语中被使用，不允许删除！")]
    COM1021,

    /// <summary>
    /// 同一数据库无法同步数据！.
    /// </summary>
    [ErrorCodeItemMetadata("同一数据库无法同步数据！")]
    COM1022,

    /// <summary>
    /// 控件唯一值.
    /// </summary>
    [ErrorCodeItemMetadata("{0}不能重复")]
    COM1023,

    /// <summary>
    /// 名称重复，请重新输入.
    /// </summary>
    [ErrorCodeItemMetadata("名称重复，请重新输入")]
    COM1024,

    /// <summary>
    /// 编码重复，请重新输入.
    /// </summary>
    [ErrorCodeItemMetadata("编码重复，请重新输入")]
    COM1025,

    /// <summary>
    /// 无法下载空表单.
    /// </summary>
    [ErrorCodeItemMetadata("该模板内表单内容为空，无法下载!")]
    COM1026,

    /// <summary>
    /// 无法下载空列表.
    /// </summary>
    [ErrorCodeItemMetadata("该模板内列表内容为空，无法下载!")]
    COM1027,

    /// <summary>
    /// 无法预览空表单.
    /// </summary>
    [ErrorCodeItemMetadata("该模板内表单内容为空，无法预览!")]
    COM1028,

    /// <summary>
    /// 无法预览空列表.
    /// </summary>
    [ErrorCodeItemMetadata("该模板内列表内容为空，无法预览!")]
    COM1029,

    /// <summary>
    /// 更新失败! 已绑定用户，无法修改状态.
    /// </summary>
    [ErrorCodeItemMetadata("更新失败! 已绑定用户，无法修改状态")]
    COM1030,

    /// <summary>
    /// 编码重复.
    /// </summary>
    [ErrorCodeItemMetadata("编码重复")]
    COM1031,

    /// <summary>
    /// 名称重复.
    /// </summary>
    [ErrorCodeItemMetadata("名称重复")]
    COM1032,

    #endregion
}